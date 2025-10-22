using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Auto-populates the scene with prefabs from the project (by GUID)
/// after the scene loads. Runs only inside the Unity Editor so it's
/// safe for experimentation without affecting builds.
///
/// What it does:
/// - Ensures a "Drone" root with Rigidbody + DroneController exists.
/// - Instantiates the "drone Black" prefab as a child of the Drone root.
/// - Optionally drops a Lemon Tree prefab into the scene if not present.
///
/// Notes:
/// - This uses AssetDatabase (Editor-only) via GUIDs, matching assets you already have.
/// - If you later want this to work in builds, place prefabs under a Resources folder
///   and swap the loading to Resources.Load / Resources.LoadAll, or migrate to Addressables.
/// </summary>
public static class AutoPopulateFromAssets
{
    // GUIDs from existing project assets (do not change unless assets are replaced):
    // drone Black.prefab -> Assets/Drone/prefab/drone Black.prefab
    private const string DroneBlackPrefabGuid = "c53e58c59cba20e4ab6f2080fbdafb5f";
    // Lemon Tree 1.prefab -> Assets/Numena/Plants/Lemon/Lemon Tree 1.prefab
    private const string LemonTreePrefabGuid = "3c98bb4bee2f2ba4b92a2fbca7b8831f";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Populate()
    {
#if UNITY_EDITOR
        try
        {
            // Only run in Play mode in the Editor
            if (!Application.isPlaying)
                return;

            // Create or find Drone root
            GameObject droneRoot = GameObject.Find("Drone");
            if (droneRoot == null)
            {
                droneRoot = new GameObject("Drone");
                var rb = droneRoot.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

                // Attach DroneController if available in project
                var controller = droneRoot.AddComponent<DroneController>();
                controller.moveSpeed = 10f;
                controller.ascendSpeed = 6f;
                controller.yawSpeed = 120f;
                controller.accel = 8f;
                controller.tiltAmount = 15f;
                controller.tiltLerp = 10f;
                droneRoot.transform.position = new Vector3(0f, 1f, 0f);

                Debug.Log("[AutoPopulate] Created Drone root with Rigidbody + DroneController");
            }

            // Ensure visual/model under Drone by instantiating the drone Black prefab as a child
            bool hasDroneChild = droneRoot.transform.childCount > 0;
            if (!hasDroneChild)
            {
                var dronePrefab = LoadPrefabByGuid(DroneBlackPrefabGuid);
                if (dronePrefab != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(dronePrefab);
                    instance.name = "drone Black"; // keep consistent naming
                    instance.transform.SetParent(droneRoot.transform, false);
                    instance.transform.localPosition = new Vector3(0.35f, 0.515f, -0.395f);
                    Debug.Log("[AutoPopulate] Spawned drone Black prefab under Drone root");
                }
                else
                {
                    Debug.LogWarning("[AutoPopulate] Could not load drone prefab by GUID; visual child not added.");
                }
            }

            // Drop a Lemon Tree into the scene if one isn't present already
            if (GameObject.Find("Lemon Tree 1") == null && GameObject.Find("Lemon Tree 1 (1)") == null)
            {
                var lemonPrefab = LoadPrefabByGuid(LemonTreePrefabGuid);
                if (lemonPrefab != null)
                {
                    var lemon = (GameObject)PrefabUtility.InstantiatePrefab(lemonPrefab);
                    lemon.name = "Lemon Tree 1";
                    lemon.transform.position = new Vector3(0.87f, -0.515f, 0f);
                    lemon.transform.rotation = Quaternion.Euler(0f, -15f, 0f);
                    lemon.transform.localScale = Vector3.one * 1.1f;
                    Debug.Log("[AutoPopulate] Spawned Lemon Tree 1 prefab in scene");
                }
                else
                {
                    Debug.LogWarning("[AutoPopulate] Could not load Lemon Tree prefab by GUID.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AutoPopulate] Failed: {e.Message}\n{e}");
        }
#else
        // No-op in player builds. Use Resources/Addressables if you need runtime population in builds.
#endif
    }

#if UNITY_EDITOR
    private static GameObject LoadPrefabByGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return null;
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
#endif
}

