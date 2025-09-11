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

# Start the HTTP service
python services/llm/http_service.py
