import logging
from typing import List, Dict

from sentence_transformers import SentenceTransformer

from config import MODEL_NAME, INDEX_NAME
from es_connection import get_elasticsearch_connection

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

# Obtain an Elasticsearch client via the new module
es = get_elasticsearch_connection()

# Load model lazily
model = SentenceTransformer(MODEL_NAME)

def search_similar_text(query: str, top_n: int = 5) -> List[str]:
    """Retrieve similar text chunks based on a user query."""
    query_vector = model.encode(query).tolist()
    search_query = {
        "size": top_n,
        "query": {
            "script_score": {
                "query": {"match": {"text": query}},
                "script": {
                    "source": "cosineSimilarity(params.query_vector, 'vector') + 1.0",
                    "params": {"query_vector": query_vector}
                }
            }
        }
    }
    response = es.search(index=INDEX_NAME, body=search_query)
    return [hit["_source"]["text"] for hit in response["hits"]["hits"]]

def list_documents() -> List[Dict[str, str]]:
    """Retrieve a list of document IDs stored in Elasticsearch."""
    query = {
        "size": 1000,
        "_source": ["document_id"],
        "query": {"match_all": {}}
    }
    response = es.search(index=INDEX_NAME, body=query)
    return [{"document_id": hit["_source"]["document_id"]} for hit in response["hits"]["hits"]]

if __name__ == "__main__":
    results = search_similar_text("What are splits?")
    logging.info(f"Search results: {results}")
