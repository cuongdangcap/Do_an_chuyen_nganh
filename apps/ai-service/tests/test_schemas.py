from __future__ import annotations

import os
import unittest
from unittest.mock import patch

from app.models.schemas import IngestionResponse


class IngestionSchemaTests(unittest.TestCase):
    def test_vector_collection_uses_runtime_environment(self) -> None:
        with patch.dict(os.environ, {"QDRANT_COLLECTION": " admissions_docs_e5_v1 "}, clear=False):
            response = IngestionResponse(extracted_text="ok", chunks=[])

        self.assertEqual(response.vector_collection, "admissions_docs_e5_v1")

    def test_vector_collection_falls_back_when_missing(self) -> None:
        with patch.dict(os.environ, {}, clear=True):
            response = IngestionResponse(extracted_text="ok", chunks=[])

        self.assertEqual(response.vector_collection, "admissions_docs")

    def test_vector_collection_falls_back_when_blank(self) -> None:
        with patch.dict(os.environ, {"QDRANT_COLLECTION": "   "}, clear=False):
            response = IngestionResponse(extracted_text="ok", chunks=[])

        self.assertEqual(response.vector_collection, "admissions_docs")


if __name__ == "__main__":
    unittest.main()
