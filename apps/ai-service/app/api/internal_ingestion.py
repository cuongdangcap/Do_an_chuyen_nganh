from pathlib import Path
from tempfile import TemporaryDirectory

from fastapi import APIRouter, File, Form, HTTPException, UploadFile

from app.ingestion.chunker import chunk_segments
from app.ingestion.extractors import clean_text, extract_segments
from app.models.schemas import IngestionResponse
from app.rag.vector_store import DEFAULT_COLLECTION

router = APIRouter(prefix="/internal/ingestion", tags=["internal-ingestion"])


@router.post("/process", response_model=IngestionResponse)
async def process_document(
    document_id: str = Form(...),
    document_version_id: str = Form(...),
    title: str = Form(...),
    document_type: str = Form(...),
    file: UploadFile = File(...),
) -> IngestionResponse:
    with TemporaryDirectory() as tmp_dir:
        suffix = Path(file.filename or "uploaded.bin").suffix
        temp_path = Path(tmp_dir) / f"input{suffix}"
        content = await file.read()
        temp_path.write_bytes(content)

        try:
            segments = extract_segments(temp_path, file.filename or temp_path.name)
            extracted_text = clean_text("\n\n".join(segment.text for segment in segments))
            chunks = chunk_segments(
                segments,
                document_id=document_id,
                document_version_id=document_version_id,
                title=title,
                document_type=document_type,
            )
        except ValueError as exc:
            raise HTTPException(status_code=422, detail=str(exc)) from exc

        if not chunks:
            raise HTTPException(status_code=422, detail="No chunks were produced.")

        return IngestionResponse(
            success=True,
            message="Document parsed and chunked.",
            extracted_text=extracted_text,
            vector_collection=DEFAULT_COLLECTION,
            chunks=chunks,
        )
