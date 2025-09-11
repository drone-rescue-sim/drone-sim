#!/bin/bash
# Drone LLM HTTP Service Startup Script

echo "🤖 Starting Drone LLM HTTP Service..."
echo "📡 Service will be available at http://127.0.0.1:5006"
echo ""

# Navigate to the correct directory
cd "$(dirname "$0")"

# Activate virtual environment if it exists
if [ -f "venv/bin/activate" ]; then
    source venv/bin/activate
    echo "✅ Virtual environment activated"
fi

# Start the HTTP service with anaconda Python (has all dependencies)
PYTHON_PATH="/Users/jacob/anaconda3/bin/python"
if [ -f "$PYTHON_PATH" ]; then
    $PYTHON_PATH services/llm/http_service.py
else
    echo "Anaconda Python not found at $PYTHON_PATH, trying system Python..."
    python services/llm/http_service.py
fi
