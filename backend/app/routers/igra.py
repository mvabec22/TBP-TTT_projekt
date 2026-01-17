from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel
from typing import List, Optional
from app.database import get_db

router = APIRouter(
    prefix="/igra",
    tags=["igra"]
)

class NoviPotez(BaseModel):
    igrac: str
    x: int
    y: int

class NovaIgra(BaseModel):
    tip: str
    igracX: Optional[str] = None
    igracO: Optional[str] = None
    host: Optional[str] = None

class StanjePoteza(BaseModel):
    rb: int
    igrac: str
    x: int
    y: int
    vrijeme_igranja: str

@router.post("/nova")
def nova_igra(data: NovaIgra, cursor = Depends(get_db)):
    data.igracO = None
    print(f"Nova igra request: tip={data.tip}, X={data.igracX}, O={data.igracO}, host={data.host}")
    try:
        if data.tip not in ['comp', 'local']:
            raise HTTPException(status_code=400, detail="Tip igre mora biti 'comp' ili 'local'")
        if data.tip == 'comp':
            if data.igracX is None:
                raise HTTPException(status_code=400, detail="Za comp igru je obavezan igracX")
        elif data.tip == 'local':
            if data.host is None:
                raise HTTPException(status_code=400, detail="Za local igru je obavezan host")
        
        cursor.execute(
            """
            SELECT nova_igra(%s, %s, %s, %s) AS id
            """,
            (
                data.tip,
                data.igracX,
                data.igracO,
                data.host
            )
        )
        
        result = cursor.fetchone() 
        if not result:
            raise HTTPException(status_code=500, detail="Greška pri kreiranju igre")
        
        id_igre = result["id"]
        
        return {
            "success": True,
            "message": "Igra uspješno kreirana",
            "id_igre": id_igre,
            "tip": data.tip,
            "igracX": data.igracX,
            "igracO": data.igracO,
            "host": data.host
        }

    except HTTPException:
        raise
    except Exception as e:
        error_msg = str(e)
        if "exception" in error_msg.lower():
            start = error_msg.find("EXCEPTION: ") + 11
            end = error_msg.find("\n", start)
            if start != -1 and end != -1:
                error_msg = error_msg[start:end].strip()
        
        raise HTTPException(status_code=500, detail=f"Greška: {error_msg}")
    
    

@router.get("/{igra_id}")
def stanje_igre(igra_id: int, cursor = Depends(get_db)):
    cursor.execute(
        "SELECT * FROM view_igre WHERE igra_id=%s",
        (igra_id,)
    )
    igra = cursor.fetchone()
    if not igra:
        raise HTTPException(status_code=404, detail="Igra ne postoji")

    cursor.execute(
        "SELECT * FROM view_potezi WHERE igra_id=%s ORDER BY rb",
        (igra_id,)
    )
    potezi = cursor.fetchall()

    return {
        "id": igra_id,
        "red": igra["red"],
        "pobjednik": igra["pobjednik"],
        "igracx": igra["igracx"],
        "igraco": igra["igraco"],
        "host": igra["host"],
        "potezi": potezi
    }



@router.post("/move/{igra_id}")
def predaj(igra_id: int, potez: NoviPotez, cursor = Depends(get_db)):
    try:
        cursor.execute(
            "SELECT odigraj(%s, %s, %s, %s)",
            (igra_id, potez.igrac, potez.x, potez.y)
        )
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))
    return {"status": "ok"}


@router.post("/predaja/{igra_id}/{igrac}")
def odigraj_potez(igra_id: int, igrac: str, cursor = Depends(get_db)):
    try:
        cursor.execute(
            "SELECT predaja_igre(%s, %s)",
            (igra_id, igrac)
        )
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))
    return {"status": "ok"}


@router.get("/history/{igrac}")
def povijest_igara(igrac: str, cursor = Depends(get_db)):
    cursor.execute("SELECT * FROM fn_povijest_igraca(%s)", (igrac,))
    return cursor.fetchall()