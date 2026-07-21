from pydantic import BaseModel, Field
from fastapi import APIRouter

from app.rag.vector_store import DEFAULT_COLLECTION, vector_store

router = APIRouter(prefix="/internal/rag", tags=["internal-rag"])


class UpsertChunk(BaseModel):
    point_id: str
    content: str
    metadata: dict[str, object | None] = Field(default_factory=dict)


class UpsertRequest(BaseModel):
    collection: str = DEFAULT_COLLECTION
    chunks: list[UpsertChunk]


class SearchRequest(BaseModel):
    query: str
    top_k: int = 5
    collection: str = DEFAULT_COLLECTION


@router.post("/upsert")
async def upsert(request: UpsertRequest) -> dict[str, object]:
    result = await vector_store.upsert(
        [chunk.model_dump() for chunk in request.chunks],
        collection=request.collection,
    )
    return {"success": True, "message": "Vectors upserted.", **result}


@router.post("/search")
async def search(request: SearchRequest) -> dict[str, object]:
    result = await vector_store.search(request.query, request.top_k, request.collection)
    return {"success": True, "message": "Search completed.", **result}
