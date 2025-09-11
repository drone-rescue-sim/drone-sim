#!/bin/bash
# Drone Simulation Services Startup Script
# This script starts all necessary services for the Unity UI command integration

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}🤖 DRONE SIMULATION SERVICES STARTUP${NC}"
    echo -e "${BLUE}========================================${NC}"
    echo ""
}

# Function to check if a port is in use
check_port() {
    local port=$1
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1; then
        return 0  # Port is in use
    else
        return 1  # Port is free
    fi
}

# Function to check if Ollama is running
check_ollama() {
    if curl -s http://127.0.0.1:11434/api/tags >/dev/null 2>&1; then
        return 0  # Ollama is running
    else
        return 1  # Ollama is not running
    fi
}

# Function to check Python dependencies
check_dependencies() {
    print_status "Checking Python dependencies..."

    # Check if virtual environment exists and activate it
    if [ -f "venv/bin/activate" ]; then
        source venv/bin/activate
        print_status "Virtual environment activated"
    else
        print_warning "No virtual environment found, using system Python"
    fi

    # Check required packages
    python -c "
import sys
try:
    import flask
    import flask_cors
    import ollama
    import requests
    print('✅ All Python dependencies are available')
except ImportError as e:
    print(f'❌ Missing dependency: {e}')
    print('Run: pip install -r requirements.txt')
    sys.exit(1)
"
}

# Function to start the LLM HTTP service
start_llm_service() {
    print_status "Starting LLM HTTP Service on port 5006..."

    # Check if port 5006 is already in use
    if check_port 5006; then
        print_error "Port 5006 is already in use!"
        print_error "Please stop the service that's using port 5006 or choose a different port"
        return 1
    fi

    # Start the service in background
    nohup python services/llm/http_service.py > llm_service.log 2>&1 &
    LLM_PID=$!

    # Wait a moment and check if it's still running
    sleep 2
    if kill -0 $LLM_PID 2>/dev/null; then
        print_status "✅ LLM HTTP Service started successfully (PID: $LLM_PID)"
        echo $LLM_PID > llm_service.pid
        return 0
    else
        print_error "❌ Failed to start LLM HTTP Service"
        return 1
    fi
}

# Function to stop services
stop_services() {
    print_status "Stopping all services..."

    # Stop LLM service
    if [ -f "llm_service.pid" ]; then
        LLM_PID=$(cat llm_service.pid)
        if kill -0 $LLM_PID 2>/dev/null; then
            kill $LLM_PID
            print_status "✅ LLM HTTP Service stopped"
        fi
        rm -f llm_service.pid
    fi

    # Clean up any other Python processes on our ports
    for port in 5006; do
        if check_port $port; then
            PIDS=$(lsof -ti :$port)
            if [ ! -z "$PIDS" ]; then
                echo $PIDS | xargs kill -9 2>/dev/null || true
                print_status "✅ Cleaned up processes on port $port"
            fi
        fi
    done
}

# Function to show status
show_status() {
    echo ""
    print_status "Service Status:"
    echo "----------------"

    # Check LLM service
    if [ -f "llm_service.pid" ]; then
        LLM_PID=$(cat llm_service.pid)
        if kill -0 $LLM_PID 2>/dev/null; then
            echo -e "🤖 LLM HTTP Service: ${GREEN}RUNNING${NC} (PID: $LLM_PID)"
        else
            echo -e "🤖 LLM HTTP Service: ${RED}STOPPED${NC} (PID file exists but process not running)"
        fi
    else
        echo -e "🤖 LLM HTTP Service: ${RED}NOT STARTED${NC}"
    fi

    # Check ports
    if check_port 5006; then
        echo -e "📡 Port 5006 (LLM): ${GREEN}IN USE${NC}"
    else
        echo -e "📡 Port 5006 (LLM): ${RED}FREE${NC}"
    fi

    # Check Ollama
    if check_ollama; then
        echo -e "🧠 Ollama: ${GREEN}RUNNING${NC}"
    else
        echo -e "🧠 Ollama: ${RED}NOT RUNNING${NC}"
        print_warning "Make sure Ollama is running with the llama2 model"
    fi

    echo ""
    print_status "Unity should connect to:"
    echo "  📡 LLM Service: http://127.0.0.1:5006"
    echo "  🎮 Drone Control: http://127.0.0.1:5005 (Unity internal)"
}

# Function to show usage
show_usage() {
    echo ""
    echo "Usage: $0 [start|stop|status|restart]"
    echo ""
    echo "Commands:"
    echo "  start   - Start all services"
    echo "  stop    - Stop all services"
    echo "  status  - Show service status"
    echo "  restart - Restart all services"
    echo "  help    - Show this help"
    echo ""
}

# Main function
main() {
    print_header

    case "${1:-start}" in
        "start")
            print_status "Starting Drone Simulation Services..."
            echo ""

            # Check dependencies
            check_dependencies
            if [ $? -ne 0 ]; then
                exit 1
            fi

            # Start services
            start_llm_service
            if [ $? -ne 0 ]; then
                exit 1
            fi

            echo ""
            print_status "🎉 All services started successfully!"
            show_status

            echo ""
            print_status "Next steps:"
            echo "1. Open Unity and load your drone scene"
            echo "2. Make sure CommandUIManager is attached to a GameObject"
            echo "3. Press TAB in Unity to open the command interface"
            echo "4. Enjoy controlling your drone with natural language!"
            echo ""
            print_status "Press Ctrl+C to stop all services"
            echo ""

            # Wait for user input or keep running
            trap stop_services EXIT
            wait
            ;;

        "stop")
            stop_services
            print_status "All services stopped"
            ;;

        "status")
            show_status
            ;;

        "restart")
            print_status "Restarting all services..."
            stop_services
            sleep 2
            main "start"
            ;;

        "help"|"-h"|"--help")
            show_usage
            ;;

        *)
            print_error "Unknown command: $1"
            show_usage
            exit 1
            ;;
    esac
}

# Run main function with all arguments
main "$@"
