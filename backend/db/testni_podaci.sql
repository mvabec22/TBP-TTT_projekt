BEGIN;

SELECT registriraj_igraca('ana', 'pass1');
SELECT registriraj_igraca('marko', 'pass2');
SELECT registriraj_igraca('iva', 'pass3');
SELECT registriraj_igraca('luka', 'pass4');

SELECT nova_igra('comp', 'ana', 'marko') AS igra1 \gset

SELECT odigraj(:igra1, 'ana',   0, 0);
SELECT odigraj(:igra1, 'marko', 1, 0);
SELECT odigraj(:igra1, 'ana',   0, 1);
SELECT odigraj(:igra1, 'marko', 1, 1);
SELECT odigraj(:igra1, 'ana',   0, 2);

SELECT nova_igra('comp', 'iva', 'luka') AS igra2 \gset

SELECT odigraj(:igra2, 'iva',  0, 0);
SELECT odigraj(:igra2, 'luka', 1, 1);
SELECT odigraj(:igra2, 'iva',  2, 2);
SELECT odigraj(:igra2, 'luka', 0, 1);
SELECT odigraj(:igra2, 'iva',  0, 2);
SELECT odigraj(:igra2, 'luka', 2, 0);
SELECT odigraj(:igra2, 'iva',  1, 0);
SELECT odigraj(:igra2, 'luka', 1, 2);
SELECT odigraj(:igra2, 'iva',  2, 1);

SELECT nova_igra('local', NULL, NULL, 'ana') AS igra3 \gset

SELECT odigraj(:igra3, 'ana', 0, 0);
SELECT odigraj(:igra3, 'ana', 1, 1);
SELECT odigraj(:igra3, 'ana', 2, 2);

COMMIT;
