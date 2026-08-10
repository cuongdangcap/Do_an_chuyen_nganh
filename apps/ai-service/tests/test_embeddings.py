from __future__ import annotations

import importlib
import math
import os
import sys
import types
import unittest


class _FakeArray:
    def __init__(self, values: list[float]) -> None:
        self._values = values

    def tolist(self) -> list[float]:
        return self._values


class _FakeSentenceTransformer:
    last_inputs: list[str] = []

    def __init__(self, model_name: str) -> None:
        self.model_name = model_name

    def get_sentence_embedding_dimension(self) -> int:
        return 384

    def encode(self, values: list[str], *, normalize_embeddings: bool, show_progress_bar: bool) -> list[_FakeArray]:
        del normalize_embeddings, show_progress_bar
        type(self).last_inputs = list(values)
        return [_FakeArray([1.0] + [0.0] * 383)]


class EmbeddingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        os.environ["EMBEDDING_PROVIDER"] = "hashing"
        cls.embeddings = importlib.import_module("app.rag.embeddings")
        cls.embeddings.get_embedding_backend.cache_clear()

    def tearDown(self) -> None:
        self.embeddings.get_embedding_backend.cache_clear()
        os.environ["EMBEDDING_PROVIDER"] = "hashing"
        sys.modules.pop("sentence_transformers", None)

    def test_hashing_embedding_is_normalized_and_deterministic(self) -> None:
        first = self.embeddings.embed_text("hoc phi tuyen sinh CMC", is_query=True)
        second = self.embeddings.embed_text("hoc phi tuyen sinh CMC", is_query=True)

        self.assertEqual(first, second)
        self.assertEqual(len(first), 384)
        self.assertAlmostEqual(math.sqrt(sum(value * value for value in first)), 1.0, places=6)

    def test_empty_text_returns_zero_vector(self) -> None:
        vector = self.embeddings.embed_text("   ")
        self.assertEqual(len(vector), 384)
        self.assertTrue(all(value == 0.0 for value in vector))

    def test_cosine_similarity_rejects_dimension_mismatch(self) -> None:
        self.assertEqual(self.embeddings.cosine_similarity([1.0], [1.0, 2.0]), 0.0)

    def test_semantic_backend_uses_e5_query_and_passage_prefixes(self) -> None:
        fake_module = types.ModuleType("sentence_transformers")
        fake_module.SentenceTransformer = _FakeSentenceTransformer
        sys.modules["sentence_transformers"] = fake_module

        backend = self.embeddings.SentenceTransformerBackend("fake-e5")
        passage = backend.encode("Thong tin hoc phi", is_query=False)
        self.assertEqual(_FakeSentenceTransformer.last_inputs, ["passage: Thong tin hoc phi"])
        self.assertEqual(len(passage), 384)

        query = backend.encode("Hoc phi bao nhieu?", is_query=True)
        self.assertEqual(_FakeSentenceTransformer.last_inputs, ["query: Hoc phi bao nhieu?"])
        self.assertEqual(len(query), 384)

    def test_semantic_status_reports_provider_model_and_dimension(self) -> None:
        fake_module = types.ModuleType("sentence_transformers")
        fake_module.SentenceTransformer = _FakeSentenceTransformer
        sys.modules["sentence_transformers"] = fake_module

        original_provider = self.embeddings.DEFAULT_PROVIDER
        original_model = self.embeddings.DEFAULT_MODEL
        try:
            self.embeddings.DEFAULT_PROVIDER = "sentence-transformers"
            self.embeddings.DEFAULT_MODEL = "fake-e5"
            self.embeddings.get_embedding_backend.cache_clear()
            status = self.embeddings.embedding_status()
        finally:
            self.embeddings.DEFAULT_PROVIDER = original_provider
            self.embeddings.DEFAULT_MODEL = original_model

        self.assertEqual(status["provider"], "sentence-transformers")
        self.assertEqual(status["model"], "fake-e5")
        self.assertEqual(status["dimension"], 384)
        self.assertTrue(status["semantic"])

    def test_strict_semantic_provider_does_not_silently_fallback(self) -> None:
        fake_module = types.ModuleType("sentence_transformers")

        class FailingSentenceTransformer:
            def __init__(self, model_name: str) -> None:
                raise OSError(f"cannot load {model_name}")

        fake_module.SentenceTransformer = FailingSentenceTransformer
        sys.modules["sentence_transformers"] = fake_module

        original_provider = self.embeddings.DEFAULT_PROVIDER
        original_model = self.embeddings.DEFAULT_MODEL
        try:
            self.embeddings.DEFAULT_PROVIDER = "sentence-transformers"
            self.embeddings.DEFAULT_MODEL = "missing-model"
            self.embeddings.get_embedding_backend.cache_clear()
            with self.assertRaisesRegex(RuntimeError, "Unable to load semantic embedding provider"):
                self.embeddings.get_embedding_backend()
        finally:
            self.embeddings.DEFAULT_PROVIDER = original_provider
            self.embeddings.DEFAULT_MODEL = original_model


if __name__ == "__main__":
    unittest.main()
