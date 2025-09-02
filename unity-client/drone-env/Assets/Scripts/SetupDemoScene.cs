using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SetupDemoScene : MonoBehaviour
{
    [ContextMenu("Setup Complete Demo Scene")]
    public void SetupScene()
    {
        // Create UI
        CreateDroneUI uiCreator = gameObject.AddComponent<CreateDroneUI>();
        uiCreator.CreateUI();

        // Find existing drone in the scene or create one from professional pack
        GameObject drone = FindExistingDrone();
        GameObject canvas = GameObject.Find("DroneCommandCanvas");

        if (drone != null && canvas != null)
        {
            DroneCommandUI commandUI = canvas.GetComponent<DroneCommandUI>();
            if (commandUI != null)
            {
                commandUI.droneObject = drone;
                Debug.Log("Demo scene setup complete! Professional drone and UI are connected.");
            }
        }
        else
        {
            Debug.LogWarning("Could not find drone or canvas. Please ensure you have a drone in the scene.");
        }

        // Add some basic lighting if not present
        if (FindObjectOfType<Light>() == null)
        {
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        // Add ground plane
        CreateGround();

        Debug.Log("Demo scene setup complete!");
        Debug.Log("1. Start the LLM service: python services/llm/main.py");
        Debug.Log("2. Press Play in Unity");
        Debug.Log("3. Press Tab to show command input");
        Debug.Log("4. Type commands like 'fly forward' or 'turn left'");
        Debug.Log("5. Make sure your drone has PA_DroneController component from the ProfessionalAssets/DronePack!");
    }

    private GameObject FindExistingDrone()
    {
        // Look for existing drones in the scene
        var drones = FindObjectsOfType<PA_DronePack.PA_DroneController>();
        if (drones.Length > 0)
        {
            Debug.Log($"Found existing drone with PA_DroneController: {drones[0].gameObject.name}");
            return drones[0].gameObject;
        }

        // If no drone found, try to find any GameObject that might be a drone
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("drone") || obj.name.ToLower().Contains("quad"))
            {
                Debug.Log($"Found potential drone object: {obj.name}");
                return obj;
            }
        }

        // If still no drone found, create a simple placeholder
        Debug.LogWarning("No drone found in scene. Creating a placeholder. Please replace with professional drone.");
        GameObject placeholder = new GameObject("DronePlaceholder");
        placeholder.AddComponent<PA_DronePack.PA_DroneController>(); // This will fail if the DLL isn't loaded, but gives a hint
        return placeholder;
    }

    private void CreateGround()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10);

            // Make ground green
            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.green;
            }
        }
    }

    [ContextMenu("Test LLM Connection")]
    public void TestLLMConnection()
    {
        GameObject canvas = GameObject.Find("DroneCommandCanvas");
        if (canvas != null)
        {
            DroneCommandUI commandUI = canvas.GetComponent<DroneCommandUI>();
            if (commandUI != null)
            {
                // Test with a simple command
                commandUI.SendCommand("move_forward");
                Debug.Log("Test command sent to LLM");
            }
        }
    }
}
