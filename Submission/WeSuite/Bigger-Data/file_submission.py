import os
import streamlit as st
from config import DATADIR

# New function for file submission button logic (now only accepts PDFs and floats at the bottom)
def handle_file_submission():
    if st.sidebar.button("Submit Document"):
        st.session_state.show_file_uploader = True

    if st.session_state.get("show_file_uploader", False):
        # Inject custom CSS to position the uploader container at the bottom of the viewport.
        st.markdown(
            """
            <style>
            .floating-uploader {
                position: fixed;
                bottom: 20px;
                left: 50%;
                transform: translateX(-50%);
                z-index: 9999;
                background: #fff;
                padding: 10px;
                box-shadow: 0 0 10px rgba(0,0,0,0.3);
            }
            </style>
            """,
            unsafe_allow_html=True
        )
        # Wrap the uploader in a div that uses the floating-uploader CSS class.
        st.markdown('<div class="floating-uploader">', unsafe_allow_html=True)
        uploaded_file = st.file_uploader("Upload your PDF document", type=["pdf"])
        st.markdown('</div>', unsafe_allow_html=True)

        if uploaded_file is not None:
            file_path = os.path.join(DATADIR, uploaded_file.name)
            with open(file_path, "wb") as f:
                f.write(uploaded_file.getbuffer())
            st.success(f"File saved to {file_path}")
            st.session_state.show_file_uploader = False
