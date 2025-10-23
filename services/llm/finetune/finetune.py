import subprocess
import os

NEW_MODEL = "drone-llama3.2:1b"
MODEFILE = "Modelfile"  
DATASET = "drone_commands.jsonl"  

number_of_epochs = 10

def run_finetune():
    print("Finetuning...")

    if not os.path.exists(MODEFILE):
        print(f"Error: {MODEFILE} not found.")
        return

    for epoch in range(number_of_epochs):
        print(f"Epoch {epoch + 1}/{number_of_epochs}")

        try:
            subprocess.run(
                ["ollama", "create", NEW_MODEL, "-f", MODEFILE],
                check=True
            )
            print(f"Done. New model is named: {NEW_MODEL}")
        except subprocess.CalledProcessError as e:
            print("Error during finetuning:", e)
    

if __name__ == "__main__":
    run_finetune()
