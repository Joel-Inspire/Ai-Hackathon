import logging
import os

from elasticsearch import Elasticsearch
from config import HOST_URL, USERNAME, PASSWORD

# Define the path to your certificate in the home directory
HOME_DIR = os.path.expanduser("~")  # This gets your home directory
CA_CERT_PATH = os.path.join(HOME_DIR, "http_ca.crt")

def get_elasticsearch_connection() -> Elasticsearch:
    """Establish and return a connection to Elasticsearch."""
    try:
        es = Elasticsearch(
            HOST_URL,
            basic_auth=(USERNAME, PASSWORD),
            verify_certs=False,  # Set to True if you'd like SSL verification
            ca_certs=CA_CERT_PATH
        )
        if not es.ping():
            raise ConnectionError("Failed to connect to Elasticsearch.")
        logging.info("Connected to Elasticsearch!")
        return es
    except Exception as e:
        logging.error(f"Elasticsearch connection error: {e}")
        raise