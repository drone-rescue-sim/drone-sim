import subprocess
import os

NEW_MODEL = "drone-llama3.2:1b"
# NEW_MODEL = "test"
MODEFILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Modelfile")


def run_finetune():
    if not os.path.exists(MODEFILE):
        print(f"Error: {MODEFILE} not found.")
        return

    print("Starting fine-tuning...")
    try:
        subprocess.run(
            ["ollama", "create", NEW_MODEL, "-f", MODEFILE],
            check=True
        )
        print(f"Done. New model is named: {NEW_MODEL}")
    except subprocess.CalledProcessError as e:
        print("Error during fine-tuning:", e)

if __name__ == "__main__":
    run_finetune()
