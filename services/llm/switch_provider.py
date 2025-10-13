#!/usr/bin/env python3
"""
Simple script to switch between LLM providers
"""
import os
import sys

def switch_provider(provider):
    """Switch LLM provider by setting environment variable"""
    if provider.lower() not in ["ollama", "openai"]:
        print("❌ Invalid provider. Use 'ollama' or 'openai'")
        return False
    
    # Set environment variable for current session
    os.environ["LLM_PROVIDER"] = provider.lower()
    
    print(f"✅ Switched to {provider.upper()} provider")
    
    if provider.lower() == "openai":
        if not os.getenv("OPENAI_API_KEY"):
            print("⚠️  Warning: OPENAI_API_KEY not set. Set it with:")
            print("   export OPENAI_API_KEY=your_api_key_here")
        else:
            print(f"🤖 Using OpenAI model: {os.getenv('OPENAI_MODEL', 'gpt-3.5-turbo')}")
    else:
        print(f"🤖 Using Ollama model: {os.getenv('OLLAMA_MODEL', 'llama2')}")
    
    return True

def main():
    if len(sys.argv) != 2:
        print("Usage: python switch_provider.py <ollama|openai>")
        print("\nExamples:")
        print("  python switch_provider.py ollama")
        print("  python switch_provider.py openai")
        return
    
    provider = sys.argv[1]
    switch_provider(provider)

if __name__ == "__main__":
    main()
