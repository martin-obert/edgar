import os

from fastapi import FastAPI
from dotenv import load_dotenv

load_dotenv()  # reads variables from a .env file and sets them in os.environ

app = FastAPI()


@app.get("/")
async def root():
    return {"message": "Hello World"}


@app.get("/hello/{name}")
async def say_hello(name: str):
    return {"message": f"Hello {name}"}
