from __future__ import annotations

import re
import uuid

from app.models.schemas import ExtractedSegment, IngestionChunk


def estimate_tokens(text: str) -> int:
    return max(1, int(len(re.findall(r"\S+", text)) * 1.25))


def detect_section_title(text: str) -> str | None:
    for line in text.splitlines():
        line = line.strip()
        if not line:
            continue
        if len(line) <= 120 and (
            line.isupper()
            or re.match(r"^(chuong|muc|dieu|phan|i+\.|\d+[\).])\s+", line, flags=re.IGNORECASE)
        ):
            return line
        return None
    return None


def chunk_segments(
    segments: list[ExtractedSegment],
    document_id: str,
    document_version_id: str,
    title: str,
    document_type: str,
    chunk_size: int = 2400,
    overlap: int = 320,
) -> list[IngestionChunk]:
    chunks: list[IngestionChunk] = []
    index = 0
    namespace = uuid.uuid5(uuid.NAMESPACE_URL, f"admissions:{document_version_id}")

    for segment in segments:
        text = segment.text.strip()
        if not text:
            continue
        section_title = segment.section_title or detect_section_title(text)
        start = 0
        while start < len(text):
            end = min(len(text), start + chunk_size)
            if end < len(text):
                paragraph_break = text.rfind("\n\n", start, end)
                sentence_break = text.rfind(". ", start, end)
                cut = max(paragraph_break, sentence_break)
                if cut > start + int(chunk_size * 0.55):
                    end = cut + 1
            content = text[start:end].strip()
            if content:
                point_id = str(uuid.uuid5(namespace, f"chunk:{index}"))
                chunks.append(
                    IngestionChunk(
                        chunk_index=index,
                        page_number=segment.page_number,
                        section_title=section_title,
                        content=content,
                        token_count=estimate_tokens(content),
                        point_id=point_id,
                        metadata={
                            "document_id": document_id,
                            "document_version_id": document_version_id,
                            "title": title,
                            "document_type": document_type,
                            "page_number": segment.page_number,
                            "section_title": section_title,
                            "embedding_status": "pending",
                            "vector_backend": "qdrant",
                        },
                    )
                )
                index += 1
            if end >= len(text):
                break
            start = max(end - overlap, start + 1)

    return chunks
