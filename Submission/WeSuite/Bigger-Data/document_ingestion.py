"""
This module provides functionality to ingest PDF documents into a single Elasticsearch index.

**Strategy**:
- We maintain a single index (named in config) for all documents.
- Each document gets a unique doc_id (based on its file name or another naming strategy).
- We chunk each document, embed those chunks with SentenceTransformer, then store them.

This approach allows all documents in the index to be searched together. Elasticsearch can
return matching chunks from any ingested document. If the user wants to isolate documents
by doc_id, they can filter on that field in search queries.
"""

import logging
import os
from typing import List
from pathlib import Path

from elasticsearch.helpers import bulk
from langchain.text_splitter import RecursiveCharacterTextSplitter
from sentence_transformers import SentenceTransformer

from config import INDEX_NAME, CHUNK_OVERLAP, CHUNK_SIZE, DOCUMENT, MODEL_NAME
from pdf_to_text import load_pdf_text
from es_connection import get_elasticsearch_connection

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

# Obtain Elasticsearch client from the connection module
es = get_elasticsearch_connection()

# Load model lazily
model = SentenceTransformer(MODEL_NAME)


def wipe_out_index() -> None:
    """Wipe out the Elasticsearch index (and all data) if it exists."""
    if es.indices.exists(index=INDEX_NAME):
        es.indices.delete(index=INDEX_NAME)
        logging.info(f"Index {INDEX_NAME} deleted.")
    else:
        logging.info(f"Index {INDEX_NAME} does not exist, nothing to delete.")


def setup_index() -> None:
    """Create index in Elasticsearch if it does not exist."""
    if not es.indices.exists(index=INDEX_NAME):
        es.indices.create(
            index=INDEX_NAME,
            body={
                "mappings": {
                    "properties": {
                        "document_id": {"type": "keyword"},
                        "chunk_id": {"type": "integer"},
                        "text": {"type": "text"},
                        "vector": {"type": "dense_vector", "dims": 384}
                    }
                }
            }
        )
        logging.info(f"Index {INDEX_NAME} created.")
    else:
        logging.info(f"Index {INDEX_NAME} already exists.")


def chunk_text(text: str) -> List[str]:
    """Chunk text into manageable pieces."""
    text_splitter = RecursiveCharacterTextSplitter(
        chunk_size=CHUNK_SIZE,
        chunk_overlap=CHUNK_OVERLAP
    )
    return text_splitter.split_text(text)


def generate_embeddings(chunks: List[str]) -> List[List[float]]:
    """Generate embedding vectors for a list of text chunks."""
    return model.encode(chunks, convert_to_list=True)


def document_exists(doc_id: str) -> bool:
    """Check if a document already exists in Elasticsearch."""
    query = {"query": {"term": {"document_id": doc_id}}}
    response = es.search(index=INDEX_NAME, body=query)
    return response["hits"]["total"]["value"] > 0


def delete_document(doc_id: str) -> None:
    """Delete a document from Elasticsearch by document_id."""
    if document_exists(doc_id):
        query = {"query": {"term": {"document_id": doc_id}}}
        es.delete_by_query(index=INDEX_NAME, body=query)
        logging.info(f"Deleted document {doc_id} from Elasticsearch.")
    else:
        logging.info(f"Document {doc_id} not found in Elasticsearch.")


def index_chunks_bulk(chunks: List[str], embeddings: List[List[float]], doc_id: str) -> None:
    """Bulk index document chunks with embeddings in Elasticsearch."""
    if document_exists(doc_id):
        logging.info(f"Document {doc_id} already exists. Skipping indexing.")
        return

    actions = [
        {
            "_index": INDEX_NAME,
            "_source": {
                "document_id": doc_id,
                "chunk_id": i,
                "text": chunk,
                "vector": vector
            }
        }
        for i, (chunk, vector) in enumerate(zip(chunks, embeddings))
    ]
    bulk(es, actions)
    logging.info(f"Indexed document {doc_id} with {len(chunks)} chunks.")

    # Force a refresh so new documents become immediately searchable
    es.indices.refresh(index=INDEX_NAME)


def process_and_store_document(doc_id: str, file_path: str) -> None:
    """Complete pipeline: Load, chunk, embed, and store document in Elasticsearch."""
    logging.info(f"Processing document {doc_id} from {file_path}")
    pdf_text = load_pdf_text(file_path)
    # logging.info(f"Extracted text: {pdf_text[:20]}...")
    chunks = chunk_text(pdf_text)
    # logging.info(f"Generated {len(chunks)} chunks.")
    embeddings = generate_embeddings(chunks)
    # logging.info(f"Generated embeddings for {len(embeddings)} chunks.")
    index_chunks_bulk(chunks, embeddings, doc_id)
    logging.info(f"Processing completed for {doc_id}")


def ingest_all_documents_from_data_dir(data_dir: str) -> None:
    """Ingest all PDF files from the given directory into the Elasticsearch index."""
    data_path = Path(data_dir)
    if not data_path.is_dir():
        logging.error(f"Data directory '{data_dir}' does not exist or is not a directory.")
        return

    # Loop over all PDF files in the data directory
    for pdf_file in data_path.glob('*.pdf'):
        # Create a unique doc_id, for example by combining the file stem with a suffix
        doc_id = pdf_file.stem

        # Ingest the document
        process_and_store_document(doc_id, str(pdf_file))


def main_process():
    # Example usage:
    # 1. Wipe out any existing index (dangerous in production!)
    wipe_out_index()

    # 2. Create a fresh index
    setup_index()

    # 3. Ingest all PDFs from 'data' directory
    ingest_all_documents_from_data_dir("data")


if __name__ == "__main__":
    main_process()
