from __future__ import annotations

import importlib
import math
import os
import unittest


class EmbeddingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        os.environ["EMBEDDING_PROVIDER"] = "hashing"
        cls.embeddings = importlib.import_module("app.rag.embeddings")
        cls.embeddings.get_embedding_backend.cache_clear()

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


if __name__ == "__main__":
    unittest.main()
