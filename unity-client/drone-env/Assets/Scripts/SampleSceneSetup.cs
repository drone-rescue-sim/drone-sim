using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

/// <summary>
/// Sample scene bootstrap to place CityPeople prefabs into the scene.
/// - At runtime, spawns from the serialized list (safe for builds).
/// - In Editor, use context menu to auto-fill the list from the DenysAlmaral pack.
/// </summary>
public class SampleSceneSetup : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Spawn people when the scene starts")]
    public bool spawnOnStart = true;

    [Tooltip("Parent object name for spawned people")]
    public string parentName = "CityPeople_All";

    [Tooltip("Columns in the placement grid")]
    public int columns = 4;

    [Tooltip("Spacing between characters in meters")]
    public float spacing = 1.5f;

    [Tooltip("Prefabs to spawn (filled in Inspector or via Editor context menu)")]
    public List<GameObject> peoplePrefabs = new List<GameObject>();

    private void Start()
    {
#if UNITY_EDITOR
        if ((peoplePrefabs == null || peoplePrefabs.Count == 0) &&
            TryPopulateFromPack(logWarnings: true, markDirty: false))
        {
            Debug.Log($"[SampleSceneSetup] Auto-filled {peoplePrefabs.Count} people prefabs from pack.");
        }
#endif
        if (spawnOnStart)
        {
            SpawnPeople();
        }
    }

    public void SpawnPeople()
    {
        if (peoplePrefabs == null || peoplePrefabs.Count == 0)
        {
            Debug.LogWarning("[SampleSceneSetup] No people prefabs assigned.");
            return;
        }

        var parent = GameObject.Find(parentName);
        if (parent == null)
        {
            parent = new GameObject(parentName);
        }
        else
        {
            // Clear existing children for idempotency
            var toDelete = new List<GameObject>();
            foreach (Transform child in parent.transform)
                toDelete.Add(child.gameObject);
            foreach (var go in toDelete)
                DestroyImmediate(go);
        }

        for (int i = 0; i < peoplePrefabs.Count; i++)
        {
            var prefab = peoplePrefabs[i];
            if (prefab == null) continue;
            var instance = Instantiate(prefab);
            instance.name = prefab.name;
            instance.transform.SetParent(parent.transform);
            int row = i / Mathf.Max(1, columns);
            int col = i % Mathf.Max(1, columns);
            instance.transform.position = new Vector3(col * spacing, 0f, row * spacing);
            FixMaterialsForRenderPipeline(instance);
        }
        Debug.Log($"[SampleSceneSetup] Spawned {peoplePrefabs.Count} people under '{parentName}'.");
    }

#if UNITY_EDITOR
    private const string BaseFolder = "Assets/DenysAlmaral/CityPeople-FREE/Prefabs";
    private const string DummyPropsFolderName = "z_dummyProps";

    [ContextMenu("Fill People From Pack (Editor)")]
    private void FillPeopleFromPack()
    {
        if (TryPopulateFromPack(logWarnings: true, markDirty: true))
        {
            Debug.Log($"[SampleSceneSetup] Filled {peoplePrefabs.Count} people prefabs from pack.");
        }
    }

    [ContextMenu("Spawn Now (Editor)")]
    private void EditorSpawnNow()
    {
        SpawnPeople();
    }

    private bool TryPopulateFromPack(bool logWarnings, bool markDirty)
    {
        if (!AssetDatabase.IsValidFolder(BaseFolder))
        {
            if (logWarnings)
            {
                Debug.LogError($"[SampleSceneSetup] Folder not found: {BaseFolder}");
            }
            return false;
        }

        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { BaseFolder });
        var list = new List<GameObject>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".prefab")) continue;
            var normalized = path.Replace('\\', '/');
            // Exclude dummy props
            if (normalized.Contains($"/{DummyPropsFolderName}/")) continue;
            // Keep only top-level prefabs directly in BaseFolder
            if (Path.GetDirectoryName(normalized).Replace('\\', '/') != BaseFolder) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) list.Add(prefab);
        }

        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        if (list.Count == 0)
        {
            if (logWarnings)
            {
                Debug.LogWarning("[SampleSceneSetup] No people prefabs found in pack.");
            }
            return false;
        }

        peoplePrefabs = list;
        if (markDirty)
        {
            EditorUtility.SetDirty(this);
        }
        return true;
    }
#endif

    private void FixMaterialsForRenderPipeline(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            var materials = renderer.sharedMaterials;
            var changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                var source = materials[i];
                var replacement = ConvertStandardToUrp(source);
                if (replacement != null && replacement != source)
                {
                    materials[i] = replacement;
                    changed = true;
                }
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }
        }
    }

    private static readonly Dictionary<Material, Material> s_ConvertedMaterialCache = new();

    private static Material ConvertStandardToUrp(Material source)
    {
        if (source == null)
        {
            return null;
        }

        if (source.shader == null || source.shader.name != "Standard")
        {
            return source;
        }

        if (s_ConvertedMaterialCache.TryGetValue(source, out var cached) && cached != null)
        {
            return cached;
        }

        var urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null)
        {
            return source;
        }

        var material = new Material(urpShader)
        {
            name = source.name,
            hideFlags = HideFlags.DontSave
        };

        CopyStandardProperties(source, material);
        s_ConvertedMaterialCache[source] = material;
        return material;
    }

    private static void CopyStandardProperties(Material from, Material to)
    {
        if (from.HasProperty("_MainTex"))
        {
            to.SetTexture("_BaseMap", from.GetTexture("_MainTex"));
            to.SetTextureScale("_BaseMap", from.GetTextureScale("_MainTex"));
            to.SetTextureOffset("_BaseMap", from.GetTextureOffset("_MainTex"));
        }

        if (from.HasProperty("_Color"))
        {
            to.SetColor("_BaseColor", from.GetColor("_Color"));
        }

        if (from.HasProperty("_MetallicGlossMap"))
        {
            to.SetTexture("_MetallicGlossMap", from.GetTexture("_MetallicGlossMap"));
        }

        if (from.HasProperty("_Metallic"))
        {
            to.SetFloat("_Metallic", from.GetFloat("_Metallic"));
        }

        if (from.HasProperty("_Glossiness"))
        {
            to.SetFloat("_Smoothness", from.GetFloat("_Glossiness"));
        }

        if (from.HasProperty("_BumpMap"))
        {
            to.SetTexture("_BumpMap", from.GetTexture("_BumpMap"));
            to.SetTextureScale("_BumpMap", from.GetTextureScale("_BumpMap"));
            to.SetTextureOffset("_BumpMap", from.GetTextureOffset("_BumpMap"));
        }

        if (from.HasProperty("_BumpScale"))
        {
            to.SetFloat("_BumpScale", from.GetFloat("_BumpScale"));
        }

        if (from.HasProperty("_OcclusionMap"))
        {
            to.SetTexture("_OcclusionMap", from.GetTexture("_OcclusionMap"));
        }

        if (from.HasProperty("_OcclusionStrength"))
        {
            to.SetFloat("_OcclusionStrength", from.GetFloat("_OcclusionStrength"));
        }

        if (from.HasProperty("_ParallaxMap"))
        {
            to.SetTexture("_ParallaxMap", from.GetTexture("_ParallaxMap"));
        }

        if (from.HasProperty("_Parallax"))
        {
            to.SetFloat("_Parallax", from.GetFloat("_Parallax"));
        }

        if (from.HasProperty("_EmissionMap"))
        {
            var tex = from.GetTexture("_EmissionMap");
            if (tex != null)
            {
                to.SetTexture("_EmissionMap", tex);
                to.EnableKeyword("_EMISSION");
            }
        }

        if (from.HasProperty("_EmissionColor"))
        {
            var color = from.GetColor("_EmissionColor");
            if (color.maxColorComponent > 0f)
            {
                to.SetColor("_EmissionColor", color);
                to.EnableKeyword("_EMISSION");
            }
        }

        if (from.HasProperty("_Cutoff"))
        {
            to.SetFloat("_Cutoff", from.GetFloat("_Cutoff"));
        }

        if (from.IsKeywordEnabled("_ALPHATEST_ON"))
        {
            to.EnableKeyword("_ALPHATEST_ON");
            to.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }

        if (from.IsKeywordEnabled("_ALPHABLEND_ON"))
        {
            to.EnableKeyword("_ALPHABLEND_ON");
            to.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        if (from.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"))
        {
            to.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            to.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
