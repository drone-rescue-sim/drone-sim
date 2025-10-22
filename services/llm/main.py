import os
import json
import requests
import ollama
from dotenv import load_dotenv

# Load environment variables from config.env
load_dotenv('config.env')

# Unity HTTP endpoint
UNITY_URL = "http://127.0.0.1:5005/"  # Unity listens on this endpoint

# LLM Configuration
LLM_PROVIDER = os.getenv("LLM_PROVIDER", "ollama")  # "ollama" or "openai"
OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
OPENAI_MODEL = os.getenv("OPENAI_MODEL", "gpt-3.5-turbo")
OLLAMA_MODEL = os.getenv("OLLAMA_MODEL", "llama2")


def call_ollama(system_prompt: str, user_input: str) -> str | None:
    """Call Ollama chat API and return the model's content string."""
    try:
        res = ollama.chat(
            model=OLLAMA_MODEL,
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_input},
            ],
        )
        # Newer ollama returns {'message': {'content': ...}}
        if isinstance(res, dict):
            msg = res.get("message") or {}
            content = msg.get("content")
            if content:
                return content
        # Fallback: if raw string
        if isinstance(res, str):
            return res
        return None
    except Exception as e:
        print(f"❌ Error calling Ollama: {e}")
        print("💡 Make sure Ollama is running and the model is available:")
        print("   ollama serve")
        print(f"   ollama pull {OLLAMA_MODEL}")
        return None


def call_openai(system_prompt: str, user_input: str) -> str | None:
    """Call OpenAI Chat Completions API and return content string."""
    try:
        if not OPENAI_API_KEY:
            print("❌ OpenAI API key not found. Set OPENAI_API_KEY environment variable.")
            return None

        headers = {
            "Authorization": f"Bearer {OPENAI_API_KEY}",
            "Content-Type": "application/json",
        }

        payload = {
            "model": OPENAI_MODEL,
            "messages": [
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_input},
            ],
            "stream": False,
        }

        # Model-specific parameter naming
        if OPENAI_MODEL.startswith("gpt-5"):
            payload["max_completion_tokens"] = 50
        else:
            payload["max_tokens"] = 50
            payload["temperature"] = 0.1

        resp = requests.post(
            "https://api.openai.com/v1/chat/completions",
            headers=headers,
            json=payload,
            timeout=15,
        )
        if resp.status_code == 200:
            data = resp.json()
            return data.get("choices", [{}])[0].get("message", {}).get("content")
        else:
            print(f"❌ OpenAI API error: {resp.status_code}")
            print(f"📄 Response: {resp.text}")
            return None
    except Exception as e:
        print(f"Error calling OpenAI: {e}")
        return None


def get_drone_instructions(user_input: str, gaze_history: list | None = None) -> list[str]:
    """Generate a list of drone commands from user_input. Optionally use gaze_history context."""
    try:
        # Build optional gaze context
        gaze_context = ""
        if gaze_history:
            lines = []
            for idx, obj in enumerate(gaze_history[:30], 1):
                pos = obj.get('position', {})
                rot = obj.get('rotation', {})
                name = obj.get('name', 'Unknown')
                tag = obj.get('tag', 'Untagged')
                dist = obj.get('distance', 'Unknown')
                ts = obj.get('timestamp', 'Unknown')
                lines.append(
                    f"{idx:02d}. name={name} tag={tag} pos=({pos.get('x',0):.2f},{pos.get('y',0):.2f},{pos.get('z',0):.2f}) "
                    f"rot=({rot.get('x',0):.2f},{rot.get('y',0):.2f},{rot.get('z',0):.2f},{rot.get('w',0):.2f}) dist={dist} ts={ts}"
                )
            gaze_context = "\n\nRecent gaze objects (max 30):\n" + "\n".join(lines) + "\n"

        system_prompt = f"""Drone control assistant. Return a JSON array of commands.
{gaze_context}
Commands: move_forward, move_backward, move_left, move_right, ascend, descend, turn_left, turn_right, stop, navigate_to_previous, navigate_to_object
Rules:
- Use navigate_to_object for specific names from objects list
- Use navigate_to_previous for object types (e.g., Person)
- Return JSON array like ["move_forward"]
Examples:
"move forward" -> ["move_forward"]
"move to Tree 1" -> ["navigate_to_object", "Tree 1"]
"go to person" -> ["navigate_to_previous", "Person"]
"""

        # Select provider
        if LLM_PROVIDER.lower() == "openai":
            print(f"🤖 Using OpenAI ({OPENAI_MODEL})")
            instructions = call_openai(system_prompt, user_input)
        else:
            print(f"🤖 Using Ollama ({OLLAMA_MODEL})")
            instructions = call_ollama(system_prompt, user_input)

        if not instructions:
            print("❌ Failed to get response from LLM; using fallback 'stop'")
            return ["stop"]

        command_text = instructions.strip()

        # Try parse as JSON array
        try:
            commands = json.loads(command_text)
            if not isinstance(commands, list):
                commands = [commands]
            valid = [
                "move_forward", "move_backward", "move_left", "move_right",
                "ascend", "go_up", "descend", "go_down",
                "turn_left", "turn_right", "stop", "navigate_to_previous", "navigate_to_object",
            ]
            out: list[str] = []
            for i, cmd in enumerate(commands):
                if not isinstance(cmd, str):
                    continue
                low = cmd.strip().lower()
                if low in valid:
                    out.append(low)
                elif i > 0 and out and out[-1] in ["navigate_to_previous", "navigate_to_object"]:
                    # Treat as argument (object name/type), keep original as-is
                    out.append(cmd.strip())
            return out if out else ["stop"]
        except json.JSONDecodeError:
            # Fallback: treat as single command string
            low = command_text.lower()
            valid = [
                "move_forward", "move_backward", "move_left", "move_right",
                "ascend", "go_up", "descend", "go_down",
                "turn_left", "turn_right", "stop", "navigate_to_previous", "navigate_to_object",
            ]
            return [low] if low in valid else ["stop"]

    except Exception as e:
        print(f"Error generating instructions: {e}")
        return ["stop"]


def send_to_unity(commands: str | list[str]) -> bool:
    """Send processed instructions to Unity via HTTP POST (root endpoint)."""
    try:
        if isinstance(commands, str):
            commands = [commands]
        payload = {"commands": commands}
        headers = {"Content-Type": "application/json"}
        r = requests.post(UNITY_URL, json=payload, headers=headers, timeout=5.0)
        if r.status_code == 200:
            print(f"✓ Sent commands to Unity: {commands}")
            return True
        print(f"✗ Unity responded with status code {r.status_code}: {r.text}")
        return False
    except requests.exceptions.ConnectionError:
        print("✗ Cannot connect to Unity. Make sure Unity is running and the HTTP server is started.")
        return False
    except requests.exceptions.Timeout:
        print("✗ Request to Unity timed out.")
        return False
    except Exception as e:
        print(f"✗ Error sending to Unity: {e}")
        return False


def test_llm_connection() -> bool:
    print("🧪 Testing LLM connection...")
    result = get_drone_instructions("move forward")
    ok = bool(result and result != ["stop"])
    print("✅ LLM connection successful!" if ok else "❌ LLM connection failed!")
    return ok


def main():
    print("🤖 Drone Control LLM Service Started")
    print(f"🔧 LLM Provider: {LLM_PROVIDER.upper()}")
    if LLM_PROVIDER.lower() == "openai":
        print(f"🤖 OpenAI Model: {OPENAI_MODEL}")
        if not OPENAI_API_KEY:
            print("⚠️  Warning: OPENAI_API_KEY not set. Set it as environment variable.")
    else:
        print(f"🤖 Ollama Model: {OLLAMA_MODEL}")

    if not test_llm_connection():
        print("💡 Please check your LLM configuration and try again.")


if __name__ == "__main__":
    main()
