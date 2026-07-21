from __future__ import annotations

import json
import os
from pathlib import Path
from threading import Lock

import httpx

from app.rag.embeddings import cosine_similarity, embed_text


DEFAULT_COLLECTION = "admissions_docs"
DEFAULT_DIMENSION = 384


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
            "local_path": str(self.local_path),
        }

    async def upsert(self, chunks: list[dict[str, object]], collection: str = DEFAULT_COLLECTION) -> dict[str, object]:
        points = []
        local_points = []
        for chunk in chunks:
            content = str(chunk["content"])
            point_id = str(chunk["point_id"])
            metadata = dict(chunk.get("metadata") or {})
            metadata["content"] = content
            vector = embed_text(content, DEFAULT_DIMENSION)
            points.append({"id": point_id, "vector": vector, "payload": metadata})
            local_points.append({"id": point_id, "vector": vector, "payload": metadata})

        if await self._qdrant_available():
            await self._ensure_collection(collection)
            async with httpx.AsyncClient(timeout=30) as client:
                response = await client.put(
                    f"{self.qdrant_url}/collections/{collection}/points?wait=true",
                    json={"points": points},
                )
                response.raise_for_status()
            return {"backend": "qdrant", "count": len(points)}

        self._local_upsert(collection, local_points)
        return {"backend": "local", "count": len(points)}

    async def search(self, query: str, top_k: int = 5, collection: str = DEFAULT_COLLECTION) -> dict[str, object]:
        vector = embed_text(query, DEFAULT_DIMENSION)
        if await self._qdrant_available():
            await self._ensure_collection(collection)
            async with httpx.AsyncClient(timeout=30) as client:
                response = await client.post(
                    f"{self.qdrant_url}/collections/{collection}/points/search",
                    json={"vector": vector, "limit": top_k, "with_payload": True},
                )
                response.raise_for_status()
                data = response.json()
            results = [
                {
                    "point_id": item["id"],
                    "score": item["score"],
                    "content": (item.get("payload") or {}).get("content", ""),
                    "metadata": item.get("payload") or {},
                }
                for item in data.get("result", [])
            ]
            return {"backend": "qdrant", "results": results}

        points = self._local_read(collection)
        scored = []
        for point in points:
            score = cosine_similarity(vector, point["vector"])
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
        return {"backend": "local", "results": scored[:top_k]}

    async def _qdrant_available(self) -> bool:
        try:
            async with httpx.AsyncClient(timeout=1.5) as client:
                response = await client.get(f"{self.qdrant_url}/")
            return response.status_code < 500
        except Exception:  # noqa: BLE001
            return False

    async def _ensure_collection(self, collection: str) -> None:
        async with httpx.AsyncClient(timeout=30) as client:
            response = await client.get(f"{self.qdrant_url}/collections/{collection}")
            if response.status_code == 200:
                return
            create = await client.put(
                f"{self.qdrant_url}/collections/{collection}",
                json={"vectors": {"size": DEFAULT_DIMENSION, "distance": "Cosine"}},
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
            self.local_path.write_text(json.dumps(data, ensure_ascii=False), encoding="utf-8")

    def _local_read(self, collection: str) -> list[dict[str, object]]:
        with self.lock:
            return list(self._local_load().get(collection, []))

    def _local_load(self) -> dict[str, list[dict[str, object]]]:
        if not self.local_path.exists():
            return {}
        return json.loads(self.local_path.read_text(encoding="utf-8"))


vector_store = VectorStore()
