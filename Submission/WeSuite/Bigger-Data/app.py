import streamlit as st

from chat_history import initialize_chat_history, display_chat_history, update_chat_history
from file_submission import handle_file_submission
from ollama_integration import send_to_ollama
from file_watcher import start_background_watcher  # Import the file watcher module

# Start the background file watcher if it hasn't been started already
if "datadir_watcher_started" not in st.session_state:
    start_background_watcher()
    st.session_state["datadir_watcher_started"] = True

# `st.set_page_config()` MUST be the first Streamlit command!
st.set_page_config(
    page_title="WeSuite Assist",
    page_icon="tree.ico",  # Replace with your icon path or URL
    layout="centered",
    initial_sidebar_state="auto"
)

# Display logo in the sidebar
st.sidebar.image("wesuitelogo.png", width=200, use_container_width=False)  # Adjust height as needed

# Initialize debug mode safely
st.session_state.setdefault("debug_mode", False)

# Sidebar options
st.session_state.debug_mode = st.sidebar.checkbox("Enable Debug Mode", value=st.session_state.debug_mode)

# Clear chat history button
if st.sidebar.button("Clear Chat History"):
    st.session_state.messages = []  # Reset chat history
    st.rerun()  # Use st.rerun() instead of the deprecated st.experimental_rerun()

# Call the file submission button function (only accepts PDF files, as defined in file_submission)
handle_file_submission()

# Initialize chat history
initialize_chat_history()

# Display chat history
display_chat_history()

# Chat input
if user_input := st.chat_input("Type a message..."):
    # Immediately display the user's message in the chat as soon as it's submitted
    update_chat_history(user_input, "waiting for AI response...")

    # Prepare structured messages for Ollama (each message is an object)
    messages = [
        {"role": msg["role"], "content": msg["content"]}
        for msg in st.session_state.messages
        if msg["role"] != "assistant" or msg["content"] != "waiting for AI response..."
    ]

    # Display the user message in the chat while waiting for the assistant's response
    with st.chat_message("user"):
        st.markdown(user_input)

    # Send chat history to LLM
    ai_response = send_to_ollama(messages)

    if ai_response:
        # Replace the "waiting" response with the actual AI response
        st.session_state.messages[-1]["content"] = ai_response

        # Update chat history and display the assistant's response
        with st.chat_message("assistant"):
            st.markdown(ai_response)
