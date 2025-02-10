# WeAssist: LLM-Driven Document System

## Overview
WeAssist is an LLM-driven document search system developed for **WeSuite**. It leverages **Elasticsearch** for vector and full-text search, enabling efficient retrieval of relevant document chunks based on semantic similarity and keyword matching.

## Technology Stack
- **Elasticsearch**: Used for storing and retrieving document embeddings and full-text search.
- **Hugging Face Transformers / Sentence-Transformers**: Used for generating embeddings.
- **Ollama**: Used for running and managing LLMs locally.
- **Python**: Core programming language (must be installed).

## System Workflow
1. **Document Ingestion**: The system processes input documents and chunks them into smaller segments.
2. **Embedding Generation**: Each document chunk is converted into a vector representation.
3. **Storage in Elasticsearch**: The chunk embeddings and metadata are stored in Elasticsearch.
4. **Query Processing**: The system retrieves relevant document chunks based on a user query.
5. **Hybrid Search**: Elasticsearch returns the most relevant chunks using a combination of BM25 keyword search and vector similarity.

## Setup and Installation

Before proceeding, ensure Elasticsearch, Python, and Ollama are installed and running. See [Elasticsearch Installation](#elasticsearch-install) and [Ollama Installation](#ollama-install) for setup instructions.

Ensure you have the necessary dependencies installed:
```bash
pip install elasticsearch sentence-transformers
```

## Step 1: Initialize Elasticsearch Client
```python
from elasticsearch import Elasticsearch

# Initialize Elasticsearch client
es = Elasticsearch(["http://localhost:9200"])
```

## Step 2: Chunking and Embedding Documents
```python
from sentence_transformers import SentenceTransformer

# Load embedding model
model = SentenceTransformer("all-MiniLM-L6-v2")

def generate_embeddings(chunks):
    return model.encode(chunks).tolist()
```

## Step 3: Indexing Chunks in Elasticsearch
```python
def index_chunks_with_elasticsearch(chunks, embeddings, doc_id):
    """Index document chunks with embeddings in Elasticsearch."""
    for i, (chunk, vector) in enumerate(zip(chunks, embeddings)):
        es.index(
            index="weassist_documents",
            id=f"{doc_id}_{i}",
            body={
                "document_id": doc_id,
                "chunk_id": i,
                "text": chunk,
                "embedding": vector
            }
        )
```

## Step 4: Querying Elasticsearch for Relevant Chunks
```python
def search_documents_elasticsearch(query):
    """Retrieve relevant document chunks using Elasticsearch hybrid search."""
    query_vector = model.encode(query).tolist()
    
    search_body = {
        "size": 5,
        "query": {
            "script_score": {
                "query": { "match": { "text": query } },
                "script": {
                    "source": "cosineSimilarity(params.query_vector, 'embedding') + 1.0",
                    "params": {"query_vector": query_vector}
                }
            }
        }
    }
    
    results = es.search(index="weassist_documents", body=search_body)
    return [hit["_source"]["text"] for hit in results["hits"]["hits"]]

# Example usage
results = search_documents_elasticsearch("Explain LLMs in simple terms")
print(results)
```

## When to Use Elasticsearch vs. ChromaDB
| Feature               | Elasticsearch | ChromaDB |
|----------------------|--------------|----------|
| Full-text search (BM25) | ✅ Yes | ❌ No |
| Vector search (kNN) | ✅ Yes | ✅ Yes |
| Hybrid search (BM25 + vectors) | ✅ Yes | ✅ Yes (via metadata) |
| Scalability | ✅ High | ⚠️ Limited |
| Ease of use | ⚠️ Requires cluster setup | ✅ Easy setup |

## Conclusion
- **Use Elasticsearch** if you need **both** full-text search and vector search for hybrid retrieval.
- **Use ChromaDB** if you only need vector search for semantic similarity and want a lightweight solution.

WeAssist provides an efficient way to perform document retrieval using **LLMs, Elasticsearch, and hybrid search capabilities**, improving document access and search capabilities for **WeSuite**.

## Elasticsearch Install

- Elastic needs a network.
```bash
docker network create elastic
```

- Pull the package.
```bash
docker pull docker.elastic.co/elasticsearch/elasticsearch:8.17.1
```

- Run it.
```bash
docker run --name es01 --net elastic -p 9200:9200 -it -m 1GB docker.elastic.co/elasticsearch/elasticsearch:8.17.1
```

- Get the username and password needed to connect.
```bash
docker exec -it es01 /usr/share/elasticsearch/bin/elasticsearch-reset-password -u elastic
```

- Get certificate in place for elastic search communications.
```bash
docker cp es01:/usr/share/elasticsearch/config/certs/http_ca.crt .
```

- Bypass Certificate Verification (For Testing Only)
If you trust the server and just want to bypass the verification temporarily, add the -k or --insecure flag:
```
curl -k -u elastic:+dlEp8ExVQM3mZ+DNH5Q https://localhost:9200
```
⚠️ Warning: This disables SSL verification, so use it only for testing.

## Ollama Install

- Download and install Ollama from [Ollama's official site](https://ollama.ai/).
- Verify the installation by running:
```bash
ollama --version
```
- Pull a language model (e.g., Llama 3):
```bash
ollama pull llama3
```
- Run a local LLM instance:
```bash
ollama run llama3
```
- Integrate with Python:
```python
import ollama
response = ollama.chat(model='llama3', messages=[{"role": "user", "content": "Hello, what can you do?"}])
print(response["message"]["content"]) 
```

