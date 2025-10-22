using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CityPeopleSpawner
{
    private const string BaseFolder = "Assets/DenysAlmaral/CityPeople-FREE/Prefabs";
    private const string DummyPropsFolderName = "z_dummyProps";
    private const string ParentPeopleOnly = "CityPeople_All";
    private const string ParentPeople = "CityPeople_People";
    private const string ParentProps = "CityPeople_Props";

    [MenuItem("Tools/CityPeople/Spawn People Only")] // original behavior, top-level people prefabs
    public static void SpawnPeopleOnly()
    {
        if (!AssetDatabase.IsValidFolder(BaseFolder))
        {
            EditorUtility.DisplayDialog("CityPeople", $"Folder not found: {BaseFolder}", "OK");
            return;
        }

        var prefabPaths = CollectTopLevelPeoplePrefabs();

        if (prefabPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("CityPeople", "No CityPeople prefabs found.", "OK");
            return;
        }

        // Create or reuse parent container
        var parent = GameObject.Find(ParentPeopleOnly);
        if (parent == null)
        {
            parent = new GameObject(ParentPeopleOnly);
            Undo.RegisterCreatedObjectUndo(parent, "Create CityPeople parent");
        }
        else
        {
            // Clear existing children so this is idempotent
            var toDelete = new List<GameObject>();
            foreach (Transform child in parent.transform)
                toDelete.Add(child.gameObject);
            foreach (var go in toDelete)
                Undo.DestroyObjectImmediate(go);
        }

        // Layout parameters
        const int columns = 4;
        const float spacing = 1.5f;

        for (int i = 0; i < prefabPaths.Count; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
            if (prefab == null) continue;

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) continue;
            Undo.RegisterCreatedObjectUndo(instance, $"Spawn {prefab.name}");
            instance.transform.SetParent(parent.transform);

            // Grid position
            int row = i / columns;
            int col = i % columns;
            instance.transform.position = new Vector3(col * spacing, 0f, row * spacing);
            instance.name = prefab.name;
        }

        Selection.activeGameObject = parent;
        EditorGUIUtility.PingObject(parent);
    }

    [MenuItem("Tools/CityPeople/Spawn Everything (People + Props)")]
    public static void SpawnEverything()
    {
        if (!AssetDatabase.IsValidFolder(BaseFolder))
        {
            EditorUtility.DisplayDialog("CityPeople", $"Folder not found: {BaseFolder}", "OK");
            return;
        }

        var all = CollectAllPrefabsSplit(out var people, out var props);
        if (all == 0)
        {
            EditorUtility.DisplayDialog("CityPeople", "No prefabs found in pack.", "OK");
            return;
        }

        // People container
        var peopleParent = CreateOrClearParent(ParentPeople);
        LayoutPrefabs(people, peopleParent, columns: 6, spacing: 1.5f);

        // Props container
        var propsParent = CreateOrClearParent(ParentProps);
        LayoutPrefabs(props, propsParent, columns: 8, spacing: 2.0f);

        Selection.activeGameObject = peopleParent;
        EditorGUIUtility.PingObject(peopleParent);
    }

    private static List<string> CollectTopLevelPeoplePrefabs()
    {
        // Find only top-level prefabs inside BaseFolder (exclude dummy props subfolder)
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { BaseFolder });
        var prefabPaths = new List<string>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".prefab")) continue;

            // Exclude dummy props and subfolder 'z_dummyProps'
            if (path.Replace('\\', '/').Contains($"/{DummyPropsFolderName}/")) continue;

            // Keep only prefabs exactly under BaseFolder
            if (Path.GetDirectoryName(path).Replace('\\', '/') != BaseFolder) continue;

            prefabPaths.Add(path);
        }
        return prefabPaths;
    }

    private static int CollectAllPrefabsSplit(out List<string> people, out List<string> props)
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { BaseFolder });
        people = new List<string>();
        props = new List<string>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".prefab")) continue;
            var normalized = path.Replace('\\', '/');
            if (normalized.Contains($"/{DummyPropsFolderName}/")) props.Add(path); else people.Add(path);
        }
        people.Sort();
        props.Sort();
        return people.Count + props.Count;
    }

    private static GameObject CreateOrClearParent(string name)
    {
        var parent = GameObject.Find(name);
        if (parent == null)
        {
            parent = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(parent, $"Create {name}");
        }
        else
        {
            var toDelete = new List<GameObject>();
            foreach (Transform child in parent.transform)
                toDelete.Add(child.gameObject);
            foreach (var go in toDelete)
                Undo.DestroyObjectImmediate(go);
        }
        return parent;
    }

    private static void LayoutPrefabs(List<string> paths, GameObject parent, int columns, float spacing)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (prefab == null) continue;
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) continue;
            Undo.RegisterCreatedObjectUndo(instance, $"Spawn {prefab.name}");
            instance.transform.SetParent(parent.transform);
            int row = i / columns;
            int col = i % columns;
            instance.transform.position = new Vector3(col * spacing, 0f, row * spacing);
            instance.name = prefab.name;
        }
    }
}
