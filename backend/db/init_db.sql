BEGIN;
DROP FUNCTION IF EXISTS odigraj;
DROP FUNCTION IF EXISTS provjeri_pobjedu;
DROP FUNCTION IF EXISTS nakon_poteza;
DROP TRIGGER IF EXISTS trg_nakon_poteza ON Potez;
DROP FUNCTION IF EXISTS fn_povijest_igraca;
DROP FUNCTION IF EXISTS registriraj_igraca;
DROP FUNCTION IF EXISTS fn_ljestvica;
DROP FUNCTION IF EXISTS nova_igra;
DROP FUNCTION IF EXISTS predaja_igre;
DROP FUNCTION IF EXISTS fn_provjeri_status_igre;
DROP FUNCTION IF EXISTS fn_obrisi_igraca;

DROP VIEW IF EXISTS view_povijest_igara; 
DROP VIEW IF EXISTS view_potezi;
DROP VIEW IF EXISTS view_ljestvica;
DROP VIEW IF EXISTS view_igre;
DROP VIEW IF EXISTS view_login;

DROP TABLE IF EXISTS Potez CASCADE;
DROP TABLE IF EXISTS Comp_igra CASCADE;
DROP TABLE IF EXISTS Local_igra CASCADE;
DROP TABLE IF EXISTS Igra CASCADE;
DROP TABLE IF EXISTS Admin CASCADE;
DROP TABLE IF EXISTS Igrac CASCADE;

DROP TYPE IF EXISTS turn_enum;
DROP TYPE IF EXISTS win_enum;

CREATE TYPE turn_enum AS ENUM ('X', 'O', 'DONE');

CREATE TYPE win_enum AS ENUM ('X', 'O', 'TIE', 'TRAJE');

CREATE TABLE Igrac (
    kor_ime VARCHAR(50) PRIMARY KEY,
    lozinka VARCHAR(255) NOT NULL,
    pobjede INT NOT NULL DEFAULT 0,
    gubitci INT NOT NULL DEFAULT 0,
    izjednaceno INT NOT NULL DEFAULT 0,
    vrijeme_stvaranja TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Admin (
    LIKE Igrac INCLUDING ALL,
    admin_level INTEGER DEFAULT 1,
    permissions TEXT[] DEFAULT '{"read", "write", "delete"}'::TEXT[],
    last_login TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Igra (
    id SERIAL PRIMARY KEY,
    red turn_enum NOT NULL DEFAULT 'X',
    pobjednik win_enum NOT NULL DEFAULT 'TRAJE',
    pocetak_igre TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    kraj_igre TIMESTAMP
);

CREATE TABLE Comp_igra (
    igracX VARCHAR(50) NOT NULL,
    igracO VARCHAR(50),
    CONSTRAINT fk_comp_igracX FOREIGN KEY (igracX) REFERENCES Igrac (kor_ime) ON DELETE SET NULL,
    CONSTRAINT fk_comp_igracO FOREIGN KEY (igracO) REFERENCES Igrac (kor_ime) ON DELETE SET NULL,
    CONSTRAINT chk_razliciti_igraci CHECK (igracX <> igracO)
) INHERITS (Igra);

CREATE TABLE Local_igra (
    host VARCHAR(50) NOT NULL,
    CONSTRAINT fk_local_host FOREIGN KEY (host) REFERENCES Igrac (kor_ime) ON DELETE CASCADE
) INHERITS (Igra);

CREATE TABLE Potez (
    rb               INT NOT NULL,
    igra             INT NOT NULL,
    igrac            VARCHAR(50) NOT NULL,
    polje            POINT NOT NULL,
    vrijeme_igranja  TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_potez_igra FOREIGN KEY (igra)
        REFERENCES Igra(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_potez_igrac FOREIGN KEY (igrac)
        REFERENCES Igrac(kor_ime),

    CONSTRAINT chk_polje_granice CHECK (
        polje[0] BETWEEN 0 AND 2
        AND
        polje[1] BETWEEN 0 AND 2
    )
);

CREATE OR REPLACE FUNCTION odigraj(
    p_igra_id INT,
    p_igrac VARCHAR,
    p_x INT,
    p_y INT
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
DECLARE
    v_red turn_enum;
    v_pobjednik win_enum;
    v_igrac_x VARCHAR;
    v_igrac_o VARCHAR;
    v_rb INT;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Igra WHERE id = p_igra_id) THEN
        RAISE EXCEPTION 'Igra s ID % ne postoji', p_igra_id;
    END IF;

    SELECT red, pobjednik INTO v_red, v_pobjednik
    FROM Igra WHERE id = p_igra_id;

    SELECT igracX, igracO INTO v_igrac_x, v_igrac_o
    FROM Comp_igra WHERE id = p_igra_id;

    IF v_igrac_x IS NULL AND v_igrac_o IS NULL THEN
        SELECT host INTO v_igrac_x
        FROM Local_igra WHERE id = p_igra_id;
        v_igrac_o := v_igrac_x;
    END IF;

    IF p_igrac NOT IN (v_igrac_x, v_igrac_o) THEN
        RAISE EXCEPTION 'Igrac % ne sudjeluje u ovoj igri', p_igrac;
    END IF;

    IF (v_red = 'X' AND p_igrac <> v_igrac_x)
       OR
       (v_red = 'O' AND p_igrac <> v_igrac_o) THEN
        RAISE EXCEPTION 'Nije red na igraca %', p_igrac;
    END IF;

    IF p_x < 0 OR p_x > 2 OR p_y < 0 OR p_y > 2 THEN
        RAISE EXCEPTION 'Neispravno polje (% , %)', p_x, p_y;
    END IF;

    IF v_pobjednik <> 'TRAJE' THEN
        RAISE EXCEPTION 'Igra % je zavrsena', p_igra_id;
    END IF;

    IF EXISTS (
        SELECT 1 FROM Potez
        WHERE igra = p_igra_id
          AND polje[0] = p_x
          AND polje[1] = p_y
    ) THEN
        RAISE EXCEPTION 'Polje (% , %) je vec zauzeto', p_x, p_y;
    END IF;

    SELECT COALESCE(MAX(rb), 0) + 1 INTO v_rb
    FROM Potez WHERE igra = p_igra_id;

    INSERT INTO Potez (rb, igra, igrac, polje)
    VALUES (v_rb, p_igra_id, p_igrac, POINT(p_x, p_y));

    UPDATE Igra
    SET red = CASE
        WHEN v_red = 'X' THEN 'O'
        WHEN v_red = 'O' THEN 'X'
        ELSE red
    END
    WHERE id = p_igra_id;
END;
$$;

CREATE OR REPLACE FUNCTION provjeri_pobjedu(p_igra_id INT)
RETURNS win_enum
LANGUAGE plpgsql
AS $$
DECLARE
    v_broj_poteza INT;
BEGIN
    SELECT COUNT(*) INTO v_broj_poteza
    FROM Potez WHERE igra = p_igra_id;

    IF EXISTS (
        SELECT 1 FROM Potez
        WHERE igra = p_igra_id AND (rb % 2) = 1
        GROUP BY polje[1] HAVING COUNT(*) = 3
        UNION
        SELECT 1 FROM Potez
        WHERE igra = p_igra_id AND (rb % 2) = 1
        GROUP BY polje[0] HAVING COUNT(*) = 3
        UNION
        SELECT 1 FROM Potez
        WHERE igra = p_igra_id AND (rb % 2) = 1
          AND polje[0] = polje[1]
        HAVING COUNT(*) = 3
        UNION
        SELECT 1 FROM Potez
        WHERE igra = p_igra_id AND (rb % 2) = 1
          AND polje[0] + polje[1] = 2
        HAVING COUNT(*) = 3
    ) THEN
        RETURN 'X';
    END IF;

    IF EXISTS (
        SELECT 1 FROM Potez
        WHERE igra = p_igra_id AND (rb % 2) = 0
        GROUP BY polje[1] HAVING COUNT(*) = 3
        UNION
        SELECT 1 FROM Potez
        WHERE igra = p_igra_id AND (rb % 2) = 0
        GROUP BY polje[0] HAVING COUNT(*) = 3
        UNION
        SELECT 1 FROM Potez
        WHERE igra = p_igra_id AND (rb % 2) = 0
          AND polje[0] = polje[1]
        HAVING COUNT(*) = 3
        UNION
        SELECT 1 FROM Potez
        WHERE igra = p_igra_id AND (rb % 2) = 0
          AND polje[0] + polje[1] = 2
        HAVING COUNT(*) = 3
    ) THEN
        RETURN 'O';
    END IF;

    IF v_broj_poteza = 9 THEN
        RETURN 'TIE';
    END IF;

    RETURN 'TRAJE';
END;
$$;

CREATE OR REPLACE FUNCTION nakon_poteza()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_pobjednik win_enum;
    v_igrac_x VARCHAR;
    v_igrac_o VARCHAR;
BEGIN
    SELECT provjeri_pobjedu(NEW.igra) INTO v_pobjednik;

    IF v_pobjednik = 'TRAJE' THEN
        RETURN NEW;
    END IF;

    SELECT igracX, igracO INTO v_igrac_x, v_igrac_o
    FROM Comp_igra WHERE id = NEW.igra;

    UPDATE Igra
    SET pobjednik = v_pobjednik,
        red = 'DONE',
        kraj_igre = CURRENT_TIMESTAMP
    WHERE id = NEW.igra;

    IF v_igrac_x IS NOT NULL THEN
        IF v_pobjednik = 'X' THEN
            UPDATE Igrac SET pobjede = pobjede + 1 WHERE kor_ime = v_igrac_x;
            UPDATE Igrac SET gubitci = gubitci + 1 WHERE kor_ime = v_igrac_o;
        ELSIF v_pobjednik = 'O' THEN
            UPDATE Igrac SET pobjede = pobjede + 1 WHERE kor_ime = v_igrac_o;
            UPDATE Igrac SET gubitci = gubitci + 1 WHERE kor_ime = v_igrac_x;
        ELSE
            UPDATE Igrac SET izjednaceno = izjednaceno + 1 WHERE kor_ime IN (v_igrac_x, v_igrac_o);
        END IF;
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_nakon_poteza
AFTER INSERT ON Potez
FOR EACH ROW
EXECUTE FUNCTION nakon_poteza();



CREATE OR REPLACE FUNCTION nova_igra(
    p_tip TEXT,
    p_igracX VARCHAR DEFAULT NULL,
    p_igracO VARCHAR DEFAULT NULL,
    p_host   VARCHAR DEFAULT NULL
)
RETURNS INT
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INT;
    v_postojeca_id INT;
    v_trenutni_igrac VARCHAR;
BEGIN

    IF p_tip = 'comp' AND p_igracO IS NULL AND p_igracX IS NOT NULL  THEN

        SELECT c.id INTO v_postojeca_id
        FROM Comp_igra c
        INNER JOIN Igra i ON c.id = i.id 
        WHERE c.igracO IS NULL
        AND i.pobjednik = 'TRAJE'::win_enum
        AND c.igracX != p_igracX
        ORDER BY i.pocetak_igre ASC
        LIMIT 1;

        IF v_postojeca_id IS NOT NULL THEN
            UPDATE Comp_igra
            SET igracO = p_igracX
            WHERE id = v_postojeca_id;
            
            RETURN v_postojeca_id;
        END IF;
    END IF;
    INSERT INTO Igra DEFAULT VALUES
    RETURNING id INTO v_id;

    IF p_tip = 'comp' THEN
        INSERT INTO Comp_igra (id, igracX, igracO)
        VALUES (v_id, p_igracX, p_igracO);
    ELSIF p_tip = 'local' THEN
        IF p_host IS NULL THEN
            RAISE EXCEPTION 'host je obavezan za local igru';
        END IF;

        INSERT INTO Local_igra (id, host)
        VALUES (v_id, p_host);
    ELSE
        RAISE EXCEPTION 'Nepoznat tip igre: %', p_tip;
    END IF;
    RETURN v_id;
END;
$$;

CREATE OR REPLACE FUNCTION registriraj_igraca(
    p_kor_ime VARCHAR,
    p_lozinka VARCHAR
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    IF EXISTS (SELECT 1 FROM Igrac WHERE kor_ime = p_kor_ime) THEN
        RAISE EXCEPTION 'Korisnik već postoji';
    END IF;

    INSERT INTO Igrac (kor_ime, lozinka)
    VALUES (p_kor_ime, p_lozinka);
END;
$$;

CREATE OR REPLACE VIEW view_login AS
SELECT
    a.kor_ime,
    a.lozinka,
    TRUE AS is_admin,
    a.admin_level,
    a.permissions
FROM Admin a

UNION ALL

SELECT
    i.kor_ime,
    i.lozinka,
    FALSE AS is_admin,
    NULL AS admin_level,
    NULL AS permissions
FROM Igrac i
WHERE NOT EXISTS (
    SELECT 1 FROM Admin a WHERE a.kor_ime = i.kor_ime
);


CREATE OR REPLACE VIEW view_potezi AS
SELECT 
    rb,
    igra AS igra_id,
    igrac,
    polje[0] AS x,
    polje[1] AS y,
    vrijeme_igranja
FROM Potez;

CREATE OR REPLACE VIEW view_ljestvica AS
SELECT
    kor_ime,
    pobjede,
    gubitci,
    izjednaceno,
    (pobjede - gubitci) AS score,
    CAST(RANK() OVER (
        ORDER BY (pobjede - gubitci) DESC, pobjede DESC
    ) AS INT) AS rang
FROM Igrac;

CREATE OR REPLACE FUNCTION fn_ljestvica(p_igrac VARCHAR)
RETURNS TABLE (
    kor_ime VARCHAR,
    pobjede INT,
    gubitci INT,
    izjednaceno INT,
    score INT,
    rang INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    (
        SELECT
            v.kor_ime,
            v.pobjede,
            v.gubitci,
            v.izjednaceno,
            v.score,
            v.rang
        FROM view_ljestvica v
        WHERE v.rang <= 10
    )
    UNION
    (
        SELECT
            v.kor_ime,
            v.pobjede,
            v.gubitci,
            v.izjednaceno,
            v.score,
            v.rang
        FROM view_ljestvica v
        WHERE v.kor_ime = p_igrac
    )
    ORDER BY rang;
END;
$$;

CREATE OR REPLACE VIEW view_povijest_igara AS
SELECT
    i.id AS igra_id,
    CASE 
        WHEN c.id IS NOT NULL THEN 'komp'::VARCHAR(10)
        WHEN l.id IS NOT NULL THEN 'local'::VARCHAR(10)
        ELSE 'nepoznato'::VARCHAR(10)
    END AS tip_igre,
    COALESCE(c.igracX, l.host) AS igracX,
    COALESCE(c.igracO, l.host) AS igracO,
    i.red,
    i.pobjednik,
    i.pocetak_igre,
    i.kraj_igre
FROM
    Igra i
    LEFT JOIN Comp_igra c ON i.id = c.id
    LEFT JOIN Local_igra l ON i.id = l.id;
    
CREATE OR REPLACE FUNCTION fn_povijest_igraca(p_igrac VARCHAR)
RETURNS TABLE(
    tip_igre VARCHAR(10),
    igracX VARCHAR,
    igracO VARCHAR,
    pobjednik VARCHAR(20),
    trajanje INTERVAL
) 
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        v.tip_igre::VARCHAR(10) AS tip_igre,
        v.igracX,
        v.igracO,
        CASE v.pobjednik::TEXT
            WHEN 'X' THEN v.igracX
            WHEN 'O' THEN v.igracO 
            WHEN 'TIE' THEN 'nerijeseno'
            WHEN 'TRAJE' THEN 'u tijeku'
            ELSE 'nepoznato'
        END AS pobjednik,
        v.kraj_igre - v.pocetak_igre AS trajanje
    FROM view_povijest_igara v
    WHERE (v.igracX = p_igrac OR v.igracO = p_igrac)
      AND v.kraj_igre IS NOT NULL
    GROUP BY 
        v.tip_igre,
        v.igracX,
        v.igracO,
        v.pobjednik,
        v.kraj_igre,
        v.pocetak_igre
    ORDER BY v.kraj_igre DESC;
END;
$$;

CREATE OR REPLACE VIEW view_igre AS
SELECT
    i.id AS igra_id,
    i.red,
    CASE
        WHEN i.pobjednik = 'X' THEN
            COALESCE(c.igracX, l.host)
        WHEN i.pobjednik = 'O' THEN
            COALESCE(c.igracO, l.host)
        WHEN i.pobjednik = 'TIE' THEN
            'Neriješeno'
        ELSE
            'TRAJE'
    END AS pobjednik,
    c.igracx AS igracX,
    c.igraco AS igracO,
    l.host AS host
FROM Igra i
LEFT JOIN Comp_igra c ON c.id = i.id
LEFT JOIN Local_igra l ON l.id = i.id;

CREATE OR REPLACE FUNCTION predaja_igre(p_igra_id INT, p_igrac_predao VARCHAR)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_igrac_x VARCHAR;
    v_igrac_o VARCHAR;
    v_host VARCHAR;
BEGIN
    SELECT igracX, igracO INTO v_igrac_x, v_igrac_o
    FROM Comp_igra
    WHERE id = p_igra_id;

    IF v_igrac_x IS NOT NULL THEN
        IF p_igrac_predao = v_igrac_x THEN
            UPDATE Igra
            SET pobjednik = 'O',
                red = 'DONE',
                kraj_igre = CURRENT_TIMESTAMP
            WHERE id = p_igra_id;
        ELSE
            UPDATE Igra
            SET pobjednik = 'X',
                red = 'DONE',
                kraj_igre = CURRENT_TIMESTAMP
            WHERE id = p_igra_id;
        END IF;

    ELSE
        SELECT host INTO v_host
        FROM Local_igra
        WHERE id = p_igra_id;

        IF p_igrac_predao = v_host THEN
            UPDATE Igra
                SET pobjednik = 'X',
                    red = 'DONE',
                    kraj_igre = CURRENT_TIMESTAMP
                WHERE id = p_igra_id;
        END IF;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION fn_obrisi_igraca(p_igrac VARCHAR)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM Igrac WHERE kor_ime = p_igrac;
END;
$$;


COMMIT;