import os

# Elastic search settings
HOST_URL = "https://localhost:9200"
USERNAME = "elastic"
R_PASSWORD = "+NK2NaX=8XRVKMC3EUa7"
M_PASSWORD = "+dlEp8ExVQM3mZ+DNH5Q"
PASSWORD = M_PASSWORD  # default password

# Model settings
EMBEDDING_MODEL_NAME = "all-MiniLM-L6-v2"  # Sentence Transformer model for embeddings
OLLAMA_MODEL = "llama3"  # Ollama model name
OLLAMA_URL = "http://localhost:11434/v1/chat/completions"  # URL for Ollama chat API endpoint

# Database Constants
INDEX_NAME = "documents"
CHUNK_SIZE = 500
CHUNK_OVERLAP = 50
MODEL_NAME = "all-MiniLM-L6-v2"
DATADIR = "data"
DOCUMENT = os.path.join(DATADIR, "WeEstimatePeopleManager.pdf")