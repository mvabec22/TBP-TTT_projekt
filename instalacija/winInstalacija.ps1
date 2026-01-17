Write-Host "=== TIC TAC TOE BACKEND INSTALLER ===" -ForegroundColor Cyan

# -------------------------------------
# 0. Putanje projekta
# -------------------------------------
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path
$PROJECT_ROOT = Resolve-Path "$SCRIPT_DIR\.."
$BACKEND_DIR = "$PROJECT_ROOT\backend"
$APP_DIR = "$BACKEND_DIR\app"
$DB_DIR = "$BACKEND_DIR\db"

Write-Host "Projekt root: $PROJECT_ROOT"
Write-Host "Backend dir:  $BACKEND_DIR"

# -------------------------------------
# 1. Python
# -------------------------------------
if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    Write-Host "Python nije instaliran. Instaliram Python 3.11..." -ForegroundColor Yellow
    Invoke-WebRequest `
        https://www.python.org/ftp/python/3.11.7/python-3.11.7-amd64.exe `
        -OutFile python-installer.exe

    Start-Process python-installer.exe `
        -ArgumentList "/quiet InstallAllUsers=1 PrependPath=1" -Wait
}

# -------------------------------------
# 2. PostgreSQL
# -------------------------------------
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    Write-Host "PostgreSQL nije instaliran. Preuzimam installer..." -ForegroundColor Yellow
    Invoke-WebRequest `
        https://get.enterprisedb.com/postgresql/postgresql-15.5-1-windows-x64.exe `
        -OutFile postgres-installer.exe

    Start-Process postgres-installer.exe -Wait
    Write-Host "Zapamti postgres lozinku!" -ForegroundColor Red
    Pause
}

# -------------------------------------
# 3. PostgreSQL baza i korisnik
# -------------------------------------
$env:PGPASSWORD = Read-Host "Unesi postgres lozinku"

psql -U postgres -c "CREATE DATABASE tic_tac_toe_db;" 2>$null
psql -U postgres -c "CREATE USER ttt_user WITH PASSWORD 'ttt_pass';" 2>$null
psql -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE tic_tac_toe_db TO ttt_user;"

# -------------------------------------
# 4. Uvoz SQL sheme
# -------------------------------------
Write-Host "Uvozim init_db.sql..." -ForegroundColor Cyan
psql -U postgres -d tic_tac_toe_db -f "$DB_DIR\init_db.sql"

# -------------------------------------
# 5. Admin korisnik
# -------------------------------------
psql -U postgres -d tic_tac_toe_db -c `
"INSERT INTO admin (kor_ime, lozinka)
 VALUES ('admin1', '1234')
 ON CONFLICT (kor_ime) DO NOTHING;"

# -------------------------------------
# 6. Python venv u backendu
# -------------------------------------
Set-Location $BACKEND_DIR

if (-not (Test-Path "venv")) {
    Write-Host "Kreiram virtualno okruženje..." -ForegroundColor Cyan
    python -m venv venv
}

.\venv\Scripts\Activate.ps1

pip install --upgrade pip
pip install -r requirements.txt

# -------------------------------------
# 7. IP adresa
# -------------------------------------
$ip = (Get-NetIPAddress -AddressFamily IPv4 |
       Where-Object {
           $_.InterfaceAlias -notlike "*Loopback*" -and
           $_.IPAddress -notlike "169.*"
       } |
       Select-Object -First 1).IPAddress

Write-Host ""
Write-Host "====================================="
Write-Host "FASTAPI SERVER SE POKREĆE"
Write-Host "MOBILNI UREĐAJ SE SPAJA NA:"
Write-Host "http://$ip:8000" -ForegroundColor Green
Write-Host "====================================="

# -------------------------------------
# 8. Pokretanje FastAPI
# -------------------------------------
Set-Location $APP_DIR
uvicorn main:app --host 0.0.0.0 --port 8000