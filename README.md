# TBP-TTT_projekt  
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

### Instalacija psql
Instalacija PostgreSQLa, preporučeno: verzija 14+  
Windows: https://www.postgresql.org/download/  
Linux: sudo apt install postgresql postgresql-contrib  

### Postavljanje baze podataka  
'''  
CREATE DATABASE tic_tac_toe_db;  
\i init_db.sql  
'''  
Datoteka init_db.sql se nalazi u backend/db.  
