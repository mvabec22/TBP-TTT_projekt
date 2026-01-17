from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel
from app.database import get_db

router = APIRouter(
    prefix="/ljestvica",
    tags=["ljestvica"]
)

@router.get("/{kor_ime}")
def ljestvica(kor_ime: str, cursor = Depends(get_db)):
    cursor.execute(
        "SELECT * FROM fn_ljestvica(%s)",
        (kor_ime,)
    )
    rezultat = cursor.fetchall()

    return {
        "igrac": kor_ime,
        "ljestvica": rezultat
    }
    
@router.get("/stats/{kor_ime}")
def stats(kor_ime: str, cursor = Depends(get_db)):
    cursor.execute(
        "SELECT pobjede, gubitci, izjednaceno FROM Igrac WHERE kor_ime = %s",
        (kor_ime,)
    )
    result = cursor.fetchone()
    if not result:
        raise HTTPException(status_code=404, detail="Korisnik ne postoji")
    
    return {
        "kor_ime": kor_ime,
        "pobjede": result["pobjede"],
        "gubitci": result["gubitci"],
        "izjednaceno": result["izjednaceno"]
    }