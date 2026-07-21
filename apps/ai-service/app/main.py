from fastapi import FastAPI

from app.api.health import router as health_router
from app.api.internal_ingestion import router as internal_ingestion_router
from app.api.internal_rag import router as internal_rag_router


def create_app() -> FastAPI:
    app = FastAPI(title="Admissions AI Service", version="0.1.0")
    app.include_router(health_router)
    app.include_router(internal_ingestion_router)
    app.include_router(internal_rag_router)
    return app


app = create_app()
