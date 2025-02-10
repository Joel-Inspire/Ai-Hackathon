from typing import List, Dict

import requests

from config import OLLAMA_MODEL, OLLAMA_URL
from database_search import search_similar_text
from logger import display_json


def send_to_ollama(messages: List[Dict[str, str]], stream: bool = False) -> str:
    """Retrieve relevant documents, add them to chat history, and send to Ollama using a POST request with the correct payload format."""

    # Extract the latest user query (from the last message in the list)
    user_query = messages[-1]["content"] if messages else ""
    messages.pop()

    # Retrieve relevant documents from ChromaDB
    retrieved_documents = search_similar_text(user_query)

    # Format retrieved documents into a structured list of facts using list comprehension
    if retrieved_documents:
        retrieved_text = "\n".join([f"- {doc.strip()}" for doc in retrieved_documents])
    else:
        retrieved_text = ""

    # Format the combined message clearly, ensuring it is understandable by the model
    combined_message = f"Here are some known facts:\n{retrieved_text}\n\nUser query: {user_query}"

    # Append the combined message as the last message in the list
    messages.append({"role": "user", "content": combined_message})

    try:
        # Prepare the payload in the format expected by Ollama
        payload = {
            "model": OLLAMA_MODEL,  # Use the model specified in the config
            "stream": stream,       # Set to `True` if you want to stream the response
            "messages": messages    # Wrap the list of messages inside the "messages" field
        }

        # Log the payload being sent
        display_json("Payload Sent to Ollama", payload)

        # Send the POST request to Ollama
        response = requests.post(OLLAMA_URL, json=payload)

        # Extract the assistant's content from the updated response structure
        if "choices" in response.json() and len(response.json()["choices"]) > 0:
            return response.json()["choices"][0]["message"]["content"]
        else:
            raise Exception("Empty or invalid response from Ollama.")
    except Exception as e:
        raise Exception(f"Error communicating with Ollama: {e}")
