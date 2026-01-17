from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel
from app.database import get_db

router = APIRouter(
    prefix="/auth",
    tags=["auth"]
)

class RegisterModel(BaseModel):
    kor_ime: str
    lozinka: str

class LoginModel(BaseModel):
    kor_ime: str
    lozinka: str

@router.post("/register")
def register(data: RegisterModel, cursor = Depends(get_db)):

    try:
        cursor.execute(
            "SELECT registriraj_igraca(%s, %s)",
            (data.kor_ime, data.lozinka)
        )
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

    return {
        "status": "ok",
        "message": "Korisnik registriran"
    }

@router.post("/login")
def login(data: LoginModel, cursor = Depends(get_db)):
    cursor.execute("""
        SELECT kor_ime, lozinka, is_admin, admin_level, permissions
        FROM view_login
        WHERE kor_ime = %s
    """, (data.kor_ime,))
    result = cursor.fetchone()
    if not result:
        raise HTTPException(status_code=401, detail="Korisnik ne postoji")

    if data.lozinka != result["lozinka"]:
        raise HTTPException(status_code=401, detail="Pogrešna lozinka")

    if result["is_admin"]:
        cursor.execute("""UPDATE admin
                    SET last_login = CURRENT_TIMESTAMP
                    WHERE kor_ime = %s; """,
                    (data.kor_ime,))
    return {
        "success": True,
        "kor_ime": result["kor_ime"],
        "isAdmin": True if result["is_admin"] else False,
        "admin_level": result["admin_level"],
        "permissions": result["permissions"]
    }
    
@router.post("/delete/{kor_ime}")
def delete(kor_ime: str, cursor = Depends(get_db)):
    try:
        cursor.execute("SELECT * FROM fn_obrisi_igraca(%s)", (kor_ime,))
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))
    return {"status": "ok"}