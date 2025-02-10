import threading
import time

from watchdog.events import FileSystemEventHandler
from watchdog.observers import Observer

import document_ingestion  # Import your ingestion module
from config import DATADIR

# Debounce settings
INGESTION_DEBOUNCE_DELAY = 3  # seconds
ingestion_timer = None  # Global timer variable

def run_ingestion():
    """This function is called after the debounce delay expires."""
    global ingestion_timer
    ingestion_timer = None  # Reset the timer
    print("Running ingestion process...")
    document_ingestion.main_process()

def trigger_ingestion():
    """Reset the debounce timer; ingestion will run after the delay."""
    global ingestion_timer
    if ingestion_timer is not None:
        ingestion_timer.cancel()  # Cancel the previous timer if it exists
    ingestion_timer = threading.Timer(INGESTION_DEBOUNCE_DELAY, run_ingestion)
    ingestion_timer.start()

class DataDirHandler(FileSystemEventHandler):
    def on_created(self, event):
        # Only process if it's a file (not a directory)
        if not event.is_directory:
            print(f"File created: {event.src_path}")
            trigger_ingestion()

    def on_deleted(self, event):
        if not event.is_directory:
            print(f"File deleted: {event.src_path}")
            trigger_ingestion()

def start_datadir_watcher():
    event_handler = DataDirHandler()
    observer = Observer()
    observer.schedule(event_handler, path=DATADIR, recursive=False)
    observer.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()
    observer.join()

def start_background_watcher():
    """
    Starts the directory watcher in a background daemon thread.
    Call this function once from your main app.
    """
    watcher_thread = threading.Thread(target=start_datadir_watcher, daemon=True)
    watcher_thread.start()
