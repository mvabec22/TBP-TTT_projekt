#!/bin/bash

echo "=== TIC TAC TOE BACKEND INSTALLER (LINUX) ==="

# -------------------------------------
# 0. Putanje projekta
# -------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(realpath "$SCRIPT_DIR/..")"
BACKEND_DIR="$PROJECT_ROOT/backend"
APP_DIR="$BACKEND_DIR/app"
DB_DIR="$BACKEND_DIR/db"

echo "Projekt root: $PROJECT_ROOT"
echo "Backend dir:  $BACKEND_DIR"

# -------------------------------------
# 1. Python 3
# -------------------------------------
if ! command -v python3 &> /dev/null; then
    echo "Python3 nije instaliran. Instaliram..."
    sudo apt update
    sudo apt install -y python3 python3-venv python3-pip
fi

# -------------------------------------
# 2. PostgreSQL
# -------------------------------------
if ! command -v psql &> /dev/null; then
    echo "PostgreSQL nije instaliran. Instaliram..."
    sudo apt update
    sudo apt install -y postgresql postgresql-contrib
fi

# -------------------------------------
# 3. PostgreSQL baza i korisnik
# -------------------------------------
echo "Unesi postgres lozinku (ako se traži)..."

sudo -u postgres psql <<EOF
CREATE DATABASE tic_tac_toe_db;
CREATE USER ttt_user WITH PASSWORD 'ttt_pass';
GRANT ALL PRIVILEGES ON DATABASE tic_tac_toe_db TO ttt_user;
EOF

# -------------------------------------
# 4. Uvoz SQL sheme
# -------------------------------------
echo "Uvozim init_db.sql..."
sudo -u postgres psql -d tic_tac_toe_db -f "$DB_DIR/init_db.sql"

# -------------------------------------
# 5. Admin korisnik
# -------------------------------------
sudo -u postgres psql -d tic_tac_toe_db <<EOF
INSERT INTO admin (kor_ime, lozinka)
VALUES ('admin1', '1234')
ON CONFLICT (kor_ime) DO NOTHING;
EOF

# -------------------------------------
# 6. Python venv u backendu
# -------------------------------------
cd "$BACKEND_DIR" || exit 1

if [ ! -d "venv" ]; then
    echo "Kreiram virtualno okruženje..."
    python3 -m venv venv
fi

source venv/bin/activate

pip install --upgrade pip
pip install -r requirements.txt

# -------------------------------------
# 7. IP adresa
# -------------------------------------
IP_ADDR=$(hostname -I | awk '{print $1}')

echo ""
echo "====================================="
echo "FASTAPI SERVER SE POKREĆE"
echo "MOBILNI UREĐAJ SE SPAJA NA:"
echo "http://$IP_ADDR:8000"
echo "====================================="

# -------------------------------------
# 8. Pokretanje FastAPI
# -------------------------------------
cd "$APP_DIR" || exit 1
uvicorn main:app --host 0.0.0.0 --port 8000
