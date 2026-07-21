from datetime import datetime, timezone

from fastapi import APIRouter

from app.rag.vector_store import vector_store

router = APIRouter(prefix="/health", tags=["health"])


@router.get("")
async def get_health() -> dict[str, object]:
    vector_status = await vector_store.status()
    return {
        "success": True,
        "service": "Admissions AI Service",
        "status": "ok",
        "vector": vector_status,
        "utc_now": datetime.now(timezone.utc).isoformat(),
    }
