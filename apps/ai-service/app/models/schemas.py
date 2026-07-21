from pydantic import BaseModel, Field


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
    vector_collection: str = "admissions_docs"
    chunks: list[IngestionChunk]
