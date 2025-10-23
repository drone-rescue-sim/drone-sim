using System.Collections.Generic;
using UnityEngine;

public class PeopleSpawner : MonoBehaviour
{
    [Tooltip("Prefabs or model variants to spawn (accepts GameObject or Component references)")]
    public List<Object> peoplePrefabs = new List<Object>();

    [Tooltip("Total number of people to spawn")] 
    public int count = 15;

    [Tooltip("Center of the spawn area (world space)")]
    public Vector3 areaCenter = new Vector3(80f, -0.5f, -110f);

    [Tooltip("Size of the spawn area (X,Z used; Y ignored)")]
    public Vector3 areaSize = new Vector3(25f, 0f, 25f);

    [Tooltip("If true, uses fixedY for all instances; else keeps prefab Y")]
    public bool useFixedY = true;

    [Tooltip("Y position used when useFixedY is true")] 
    public float fixedY = -0.5f;

    [Tooltip("Randomize Y rotation of each spawned person")]
    public bool randomYRotation = true;

    void Start()
    {
        if (peoplePrefabs == null || peoplePrefabs.Count == 0 || count <= 0)
            return;

        var halfX = Mathf.Max(0f, areaSize.x * 0.5f);
        var halfZ = Mathf.Max(0f, areaSize.z * 0.5f);

        for (int i = 0; i < count; i++)
        {
            var prefab = peoplePrefabs[Random.Range(0, peoplePrefabs.Count)];
            if (prefab == null) continue;

            float x = Random.Range(-halfX, halfX) + areaCenter.x;
            float z = Random.Range(-halfZ, halfZ) + areaCenter.z;
            float y = useFixedY ? fixedY : areaCenter.y;

            var pos = new Vector3(x, y, z);
            var rot = randomYRotation ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;

            try
            {
                GameObject template = null;
                if (prefab is GameObject pgo)
                {
                    template = pgo;
                }
                else if (prefab is Component pc)
                {
                    template = pc.gameObject;
                }
                else
                {
                    Debug.LogWarning($"PeopleSpawner: Unsupported prefab type {prefab.GetType().Name} for '{prefab.name}'. Skipping.");
                    continue;
                }

                var inst = Instantiate(template, pos, rot, this.transform);
                if (inst == null)
                {
                    Debug.LogWarning($"PeopleSpawner: Instantiate returned null for '{template.name}'.");
                    continue;
                }
            }
            catch (System.Exception ex)
            {
                var n = prefab != null ? prefab.name : "<null>";
                var t = prefab != null ? prefab.GetType().Name : "<unknown>";
                Debug.LogError($"PeopleSpawner: Instantiate error for '{n}' of type {t} => {ex}");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        var y = useFixedY ? fixedY : areaCenter.y;
        var center = new Vector3(areaCenter.x, y, areaCenter.z);
        var size = new Vector3(Mathf.Max(0.1f, areaSize.x), 0.1f, Mathf.Max(0.1f, areaSize.z));
        Gizmos.DrawWireCube(center, size);
    }
}
