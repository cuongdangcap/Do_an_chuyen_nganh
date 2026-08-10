from __future__ import annotations

import hashlib
import logging
import math
import os
import re
from functools import lru_cache
from typing import Protocol

logger = logging.getLogger(__name__)
TOKEN_RE = re.compile(r"[\w]+", re.UNICODE)
DEFAULT_DIMENSION = int(os.getenv("EMBEDDING_DIMENSION", "384"))
DEFAULT_MODEL = os.getenv("EMBEDDING_MODEL", "intfloat/multilingual-e5-small")
DEFAULT_PROVIDER = os.getenv("EMBEDDING_PROVIDER", "auto").strip().lower()


class EmbeddingBackend(Protocol):
    name: str
    model: str
    dimension: int

    def encode(self, text: str, *, is_query: bool) -> list[float]: ...


class HashingEmbeddingBackend:
    """Deterministic offline fallback used only when no semantic model is available."""

    name = "hashing-fallback"
    model = "sha256-feature-hashing"

    def __init__(self, dimension: int = DEFAULT_DIMENSION) -> None:
        self.dimension = dimension

    def encode(self, text: str, *, is_query: bool) -> list[float]:
        del is_query
        vector = [0.0] * self.dimension
        tokens = TOKEN_RE.findall(text.lower())
        for token in tokens:
            digest = hashlib.sha256(token.encode("utf-8")).digest()
            bucket = int.from_bytes(digest[:4], "big") % self.dimension
            sign = 1.0 if digest[4] % 2 == 0 else -1.0
            vector[bucket] += sign
        return normalize_vector(vector)


class SentenceTransformerBackend:
    name = "sentence-transformers"

    def __init__(self, model_name: str = DEFAULT_MODEL) -> None:
        from sentence_transformers import SentenceTransformer

        self.model = model_name
        self._model = SentenceTransformer(model_name)
        dimension = self._model.get_sentence_embedding_dimension()
        if not dimension:
            raise RuntimeError(f"Embedding model {model_name!r} did not report a dimension")
        self.dimension = int(dimension)

    def encode(self, text: str, *, is_query: bool) -> list[float]:
        prefix = "query: " if is_query else "passage: "
        encoded = self._model.encode(
            [prefix + text.strip()],
            normalize_embeddings=True,
            show_progress_bar=False,
        )[0]
        return [float(value) for value in encoded.tolist()]


def normalize_vector(vector: list[float]) -> list[float]:
    norm = math.sqrt(sum(value * value for value in vector))
    if norm == 0:
        return vector
    return [value / norm for value in vector]


@lru_cache(maxsize=1)
def get_embedding_backend() -> EmbeddingBackend:
    if DEFAULT_PROVIDER in {"auto", "sentence-transformers", "sentence_transformers", "semantic"}:
        try:
            backend = SentenceTransformerBackend(DEFAULT_MODEL)
            logger.info("Using semantic embedding model %s (%s dimensions)", backend.model, backend.dimension)
            return backend
        except Exception as exc:  # noqa: BLE001
            if DEFAULT_PROVIDER not in {"auto"}:
                raise RuntimeError(f"Unable to load semantic embedding provider: {exc}") from exc
            logger.warning("Semantic embedding unavailable; using deterministic fallback: %s", exc)

    if DEFAULT_PROVIDER not in {"auto", "hash", "hashing", "fallback"}:
        raise ValueError(f"Unsupported EMBEDDING_PROVIDER={DEFAULT_PROVIDER!r}")
    return HashingEmbeddingBackend(DEFAULT_DIMENSION)


def embedding_status() -> dict[str, object]:
    backend = get_embedding_backend()
    return {
        "provider": backend.name,
        "model": backend.model,
        "dimension": backend.dimension,
        "semantic": backend.name == "sentence-transformers",
    }


def embed_text(text: str, dimension: int | None = None, *, is_query: bool = False) -> list[float]:
    if not text or not text.strip():
        target_dimension = dimension or get_embedding_backend().dimension
        return [0.0] * target_dimension

    backend = get_embedding_backend()
    vector = backend.encode(text, is_query=is_query)
    if dimension is not None and len(vector) != dimension:
        raise ValueError(
            f"Embedding dimension mismatch: model produced {len(vector)}, expected {dimension}. "
            "Recreate the Qdrant collection or update EMBEDDING_DIMENSION."
        )
    return vector


def cosine_similarity(left: list[float], right: list[float]) -> float:
    if not left or not right or len(left) != len(right):
        return 0.0
    return sum(a * b for a, b in zip(left, right, strict=True))
