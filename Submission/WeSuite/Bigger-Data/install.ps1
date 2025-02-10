param (
    [string]$pythonInterpreter = "py"  # Default to 'py' if no path is provided
)

# Check if the 'venv' directory exists
if (Test-Path ".venv") {
    # Prompt user for confirmation
    $response = Read-Host "The '.venv' directory already exists. Do you want to delete and recreate it? (yes/no)"

    if ($response -eq "yes") {
        Write-Host "Removing existing virtual environment..." -ForegroundColor Yellow
        Remove-Item -Recurse -Force ".venv"
    } else {
        Write-Host "Aborting installation. Virtual environment remains unchanged." -ForegroundColor Red
        exit
    }
}

# Create a new virtual environment using the provided Python interpreter or default
Write-Host "Creating a new virtual environment..." -ForegroundColor Cyan
& $pythonInterpreter -m venv .venv

# Activate the virtual environment
Write-Host "Activating virtual environment..." -ForegroundColor Cyan
& .venv\Scripts\Activate

# Upgrade pip
Write-Host "Upgrading pip..." -ForegroundColor Cyan
python -m pip install --upgrade pip

# Install requirements
pip install -r requirements.txt

# Completion message
Write-Host "`nInstallation complete." -ForegroundColor Green
