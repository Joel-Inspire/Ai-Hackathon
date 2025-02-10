# Check if the virtual environment exists
if (-Not (Test-Path ".\.venv\Scripts\Activate")) {
    Write-Host "Virtual environment not found! Run the setup script first." -ForegroundColor Red
    exit
}

# Activate the virtual environment
Write-Host "Activating virtual environment..." -ForegroundColor Cyan
& .\.venv\Scripts\Activate

# Start app
Write-Host "Starting streamlit app..." -ForegroundColor Green
streamlit run app.py
# streamlit run app.py --server.runOnSave=false --global.developmentMode=true
