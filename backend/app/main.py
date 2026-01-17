from fastapi import FastAPI, Depends, APIRouter, HTTPException
from app.database import get_db
from app.routers import auth, igra, ljestvica

app = FastAPI(
    title="Tic-Tac-Toe API",
    description="Backend server za Tic-Tac-Toe projekt",
)

@app.get("/db")
def health_db(cursor = Depends(get_db)):
    cursor.execute("SELECT 1 AS ok;")
    return cursor.fetchone()

app.include_router(auth.router)
app.include_router(igra.router)
app.include_router(ljestvica.router)
