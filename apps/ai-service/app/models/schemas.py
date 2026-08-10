import os

from pydantic import BaseModel, Field


def _default_vector_collection() -> str:
    return os.getenv("QDRANT_COLLECTION", "admissions_docs").strip() or "admissions_docs"


class ExtractedSegment(BaseModel):
    text: str
    page_number: int | None = None
    section_title: str | None = None


class IngestionChunk(BaseModel):
    chunk_index: int
    page_number: int | None = None
    section_title: str | None = None
    content: str
    token_count: int
    point_id: str
    metadata: dict[str, object | None] = Field(default_factory=dict)


class IngestionResponse(BaseModel):
    success: bool = True
    message: str = "OK"
    extracted_text: str
    vector_collection: str = Field(default_factory=_default_vector_collection)
    chunks: list[IngestionChunk]
