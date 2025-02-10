import streamlit as st


def display_json(header, data):
    """Display JSON data conditionally based on debug mode."""
    if st.session_state.get("debug_mode", False):
        st.subheader(header)
        st.json(data)
