import ollama
import requests  # for HTTP requests

# Unity HTTP endpoint
UNITY_URL = "http://127.0.0.1:5005/receive_command"  # Unity must listen at this URL, could use ports or other methods 

def get_drone_instructions(user_input): 
    """
    Process user input using an LLM to generate drone instructions.
    """
    try:
        response = ollama.chat(
            model="llama2",  # Or another model of choicé
            messages=[
                {"role": "system",
                 "content": "You are a drone control assistant for Unity. Translate user instructions into simple drone movement commands (e.g., move_forward, turn_left, ascend, descend), and just give the command."},
                 # Could change the system prompt to fit the use case ^
                {"role": "user", "content": user_input}
            ]
        )
        instructions = response['message']['content']
        return instructions.strip()
    
    except Exception as e:
        print(f"Error generating instructions: {e}")
        return None

def send_to_unity(message):
    """
    Send the processed instructions to Unity via HTTP POST.
    """
    try:
        payload = {"command": message}  # JSON 
        r = requests.post(UNITY_URL, json=payload)
        if r.status_code == 200:
            print(f"Sent to Unity: {message}")
        else:
            print(f"Unity responded with status code {r.status_code}")
    except Exception as e:
        print(f"Error sending to Unity: {e}")

def main():
    while True:
        # Get user input
        user_input = input("Enter instructions for the drone: ")
        if user_input.lower() in ['exit', 'quit']:
            print("Exiting...")
            break

        # Process input with LLM
        instructions = get_drone_instructions(user_input)
        if instructions:
            print(f"Generated Instructions: {instructions}")
            # Send instructions to Unity
            send_to_unity(instructions)
        else:
            print("Failed to generate instructions. Please try again.")

if __name__ == "__main__":
    main()
