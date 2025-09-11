using UnityEngine;

/// <summary>
/// Simple manager that ensures CommandUISetup exists in the scene.
/// This component can be safely removed if you prefer to manually add CommandUISetup to a GameObject.
/// </summary>
public class CommandUIManager : MonoBehaviour
{
    void Start()
    {
        // Check if CommandUISetup already exists
        CommandUISetup uiSetup = FindFirstObjectByType<CommandUISetup>();
        if (uiSetup == null)
        {
            // Create a new GameObject for UI management
            GameObject uiManager = new GameObject("CommandUISetup");
            uiSetup = uiManager.AddComponent<CommandUISetup>();

            // Don't destroy on scene load to persist across scenes
            DontDestroyOnLoad(uiManager);

            Debug.Log("CommandUISetup created automatically by CommandUIManager");
        }
    }

    // Update method removed as it's not needed for this simple functionality
}
