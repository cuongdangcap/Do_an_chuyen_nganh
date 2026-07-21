from __future__ import annotations

import json
import os
from pathlib import Path
from threading import Lock
from typing import Any

import httpx

from app.rag.embeddings import cosine_similarity, embed_text, embedding_status, get_embedding_backend

DEFAULT_COLLECTION = os.getenv("QDRANT_COLLECTION", "admissions_docs")
QDRANT_TIMEOUT_SECONDS = float(os.getenv("QDRANT_TIMEOUT_SECONDS", "8"))
DEFAULT_SCORE_THRESHOLD = float(os.getenv("QDRANT_SCORE_THRESHOLD", "0.20"))


class VectorStore:
    def __init__(self) -> None:
        self.qdrant_url = os.environ.get("QDRANT_URL", "http://localhost:6333").rstrip("/")
        self.local_path = Path(os.environ.get("LOCAL_VECTOR_STORE", "storage/local_vectors.json"))
        self.lock = Lock()

    async def status(self) -> dict[str, object]:
        qdrant_available = await self._qdrant_available()
        return {
            "backend": "qdrant" if qdrant_available else "local",
            "qdrant_available": qdrant_available,
            "qdrant_url": self.qdrant_url,
            "collection": DEFAULT_COLLECTION,
            "local_path": str(self.local_path),
            "embedding": embedding_status(),
        }

    async def upsert(self, chunks: list[dict[str, object]], collection: str = DEFAULT_COLLECTION) -> dict[str, object]:
        if not chunks:
            return {"backend": "qdrant" if await self._qdrant_available() else "local", "count": 0}

        backend = get_embedding_backend()
        points: list[dict[str, object]] = []
        for chunk in chunks:
            content = str(chunk["content"]).strip()
            if not content:
                continue
            point_id = str(chunk["point_id"])
            metadata = dict(chunk.get("metadata") or {})
            metadata["content"] = content
            metadata["embedding_provider"] = backend.name
            metadata["embedding_model"] = backend.model
            vector = embed_text(content, backend.dimension, is_query=False)
            points.append({"id": point_id, "vector": vector, "payload": metadata})

        if await self._qdrant_available():
            await self._ensure_collection(collection, backend.dimension)
            async with httpx.AsyncClient(timeout=QDRANT_TIMEOUT_SECONDS) as client:
                for start in range(0, len(points), 128):
                    response = await client.put(
                        f"{self.qdrant_url}/collections/{collection}/points?wait=true",
                        json={"points": points[start : start + 128]},
                    )
                    response.raise_for_status()
            return {"backend": "qdrant", "count": len(points), "embedding": embedding_status()}

        self._local_upsert(collection, points)
        return {"backend": "local", "count": len(points), "embedding": embedding_status()}

    async def search(
        self,
        query: str,
        top_k: int = 5,
        collection: str = DEFAULT_COLLECTION,
        score_threshold: float | None = DEFAULT_SCORE_THRESHOLD,
    ) -> dict[str, object]:
        backend = get_embedding_backend()
        vector = embed_text(query, backend.dimension, is_query=True)
        top_k = max(1, min(int(top_k), 25))

        if await self._qdrant_available():
            await self._ensure_collection(collection, backend.dimension)
            results = await self._qdrant_search(collection, vector, top_k, score_threshold)
            return {"backend": "qdrant", "results": results, "embedding": embedding_status()}

        points = self._local_read(collection)
        scored = []
        for point in points:
            score = cosine_similarity(vector, point["vector"])
            if score_threshold is not None and score < score_threshold:
                continue
            payload = point.get("payload") or {}
            scored.append(
                {
                    "point_id": point["id"],
                    "score": score,
                    "content": payload.get("content", ""),
                    "metadata": payload,
                }
            )
        scored.sort(key=lambda item: item["score"], reverse=True)
        return {"backend": "local", "results": scored[:top_k], "embedding": embedding_status()}

    async def _qdrant_search(
        self,
        collection: str,
        vector: list[float],
        top_k: int,
        score_threshold: float | None,
    ) -> list[dict[str, Any]]:
        payload: dict[str, object] = {
            "query": vector,
            "limit": top_k,
            "with_payload": True,
            "with_vector": False,
        }
        if score_threshold is not None:
            payload["score_threshold"] = score_threshold

        async with httpx.AsyncClient(timeout=QDRANT_TIMEOUT_SECONDS) as client:
            response = await client.post(f"{self.qdrant_url}/collections/{collection}/points/query", json=payload)
            if response.status_code == 404:
                legacy_payload = {
                    "vector": vector,
                    "limit": top_k,
                    "with_payload": True,
                    **({"score_threshold": score_threshold} if score_threshold is not None else {}),
                }
                response = await client.post(
                    f"{self.qdrant_url}/collections/{collection}/points/search",
                    json=legacy_payload,
                )
            response.raise_for_status()
            data = response.json()

        raw = data.get("result", {})
        items = raw.get("points", []) if isinstance(raw, dict) else raw
        return [
            {
                "point_id": item["id"],
                "score": item.get("score", 0.0),
                "content": (item.get("payload") or {}).get("content", ""),
                "metadata": item.get("payload") or {},
            }
            for item in items
        ]

    async def _qdrant_available(self) -> bool:
        try:
            async with httpx.AsyncClient(timeout=2.0) as client:
                response = await client.get(f"{self.qdrant_url}/healthz")
                if response.status_code == 404:
                    response = await client.get(f"{self.qdrant_url}/")
            return response.status_code == 200
        except (httpx.HTTPError, OSError):
            return False

    async def _ensure_collection(self, collection: str, dimension: int) -> None:
        async with httpx.AsyncClient(timeout=QDRANT_TIMEOUT_SECONDS) as client:
            response = await client.get(f"{self.qdrant_url}/collections/{collection}")
            if response.status_code == 200:
                config = response.json().get("result", {}).get("config", {}).get("params", {}).get("vectors", {})
                existing_size = config.get("size") if isinstance(config, dict) else None
                if existing_size and int(existing_size) != dimension:
                    raise RuntimeError(
                        f"Qdrant collection {collection!r} uses {existing_size} dimensions, "
                        f"but the active embedding model uses {dimension}. Re-index the collection."
                    )
                return
            if response.status_code != 404:
                response.raise_for_status()
            create = await client.put(
                f"{self.qdrant_url}/collections/{collection}",
                json={
                    "vectors": {"size": dimension, "distance": "Cosine"},
                    "optimizers_config": {"default_segment_number": 2},
                    "on_disk_payload": True,
                },
            )
            create.raise_for_status()

    def _local_upsert(self, collection: str, points: list[dict[str, object]]) -> None:
        with self.lock:
            data = self._local_load()
            existing = {point["id"]: point for point in data.get(collection, [])}
            for point in points:
                existing[point["id"]] = point
            data[collection] = list(existing.values())
            self.local_path.parent.mkdir(parents=True, exist_ok=True)
            temp_path = self.local_path.with_suffix(self.local_path.suffix + ".tmp")
            temp_path.write_text(json.dumps(data, ensure_ascii=False), encoding="utf-8")
            temp_path.replace(self.local_path)

    def _local_read(self, collection: str) -> list[dict[str, object]]:
        with self.lock:
            return list(self._local_load().get(collection, []))

    def _local_load(self) -> dict[str, list[dict[str, object]]]:
        if not self.local_path.exists():
            return {}
        try:
            return json.loads(self.local_path.read_text(encoding="utf-8"))
        except (json.JSONDecodeError, OSError):
            return {}


vector_store = VectorStore()
