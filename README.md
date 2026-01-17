# TBP-Mobilna aplikacija za igru Križić kružić  
Poveznica na .apk za mobilnu aplikaciju:  
https://drive.google.com/file/d/1CmE_k79Irt7aKKlu8-76TWCdT4FoifZ2/view?usp=sharing  
  
## Upute za pokretanje projekta
U slučaju da instalacijske skripte ne rade ispravno, projekt se može pokrenuti ručno.  
  
### Instalacija pythona  
FastAPI server je osnovan na pythonu, preporučena je verzija 3.10 ili više.  
Windows instalacija pythona:  
https://www.python.org/downloads/ ili na Microsoft Store  
Linux instalacija:  
sudo apt install python3 python3-pip python3-venv  
Instalacija ovisnosti za server:  
pip install -r requirements.txt  
  
### Instalacija psql
Instalacija PostgreSQLa, preporučeno: verzija 14+  
Windows: https://www.postgresql.org/download/  
Linux: sudo apt install postgresql postgresql-contrib  

### Postavljanje baze podataka  
CREATE DATABASE tic_tac_toe_db;  
CREATE USER ttt_user WITH PASSWORD 'ttt_pass';  
GRANT ALL PRIVILEGES ON DATABASE tic_tac_toe_db TO ttt_user;  
\i init_db.sql  
INSERT INTO admin (kor_ime, lozinka)  
VALUES ('admin1', '1234');  

Datoteka init_db.sql se nalazi u backend/db.  

### Pokretanje servera  
Iz mape app:  
uvicorn main:app --host 0.0.0.0 --port 8000  
Provjeriti IP adresu računala, na Windows pomoću ipconfig  
a na Linux pomoću ip addr show ili ifconfig.

### Pokretanje igre na mobilnom uređaju  
Pokrenuti .apk kako bi se aplikacija instalirala.  
Kada je aplikacija instalirana i pokrenuta,  
upisati IP servera na način: http://[IP]:8000  
BITNO: mobilni uređaj i uređaj servera moraju biti spojeni na istu mrežu.  
Preporuča se korištenje hotspot mreže preko mobilnog uređaja, ako je moguće.
