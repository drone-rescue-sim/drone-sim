using System.Collections.Generic;
using UnityEngine;

public class PeopleWanderManager : MonoBehaviour
{
    [Tooltip("Name prefix to detect people to move (matches GameObject.name startsWith). If empty and namePrefixes provided, those are used instead.")]
    public string actorNamePrefix = "";

    [Tooltip("Optional set of name prefixes to include (startsWith). If non-empty, used instead of actorNamePrefix.")]
    public List<string> namePrefixes = new List<string>();

    [Tooltip("Center of the walkable area (world space)")]
    public Vector3 areaCenter = new Vector3(-2.4f, -0.5f, -27.1f);

    [Tooltip("Size of the walkable area (X/Z used)")]
    public Vector3 areaSize = new Vector3(20f, 0f, 20f);

    [Tooltip("Ground Y height to keep actors on")] 
    public float groundY = -0.5f;

    [Tooltip("Min movement speed (m/s)")]
    public float minSpeed = 0.7f;

    [Tooltip("Max movement speed (m/s)")]
    public float maxSpeed = 1.4f;

    [Tooltip("Seconds before force retarget")] 
    public float maxTargetTime = 8f;

    [Tooltip("Rotate to face movement direction")]
    public bool faceMovement = true;

    class Agent
    {
        public Transform t;
        public Vector3 target;
        public float speed;
        public float timer;
    }

    readonly List<Agent> agents = new List<Agent>();

    void Start()
    {
        DiscoverActors();
        // Ensure pink (Standard) materials are upgraded to URP/Lit at runtime
        foreach (var a in agents)
        {
            UpgradeMaterials(a.t);
        }
    }

    void DiscoverActors()
    {
        agents.Clear();
        var added = new HashSet<Transform>();
        var anims = FindObjectsOfType<Animator>();
        foreach (var anim in anims)
        {
            if (anim == null) continue;
            // use the top-most transform of this character
            var rootT = anim.transform;
            while (rootT.parent != null) rootT = rootT.parent;

            string n = rootT.name;
            bool match = false;
            if (namePrefixes != null && namePrefixes.Count > 0)
            {
                foreach (var prefix in namePrefixes)
                {
                    if (!string.IsNullOrEmpty(prefix) && n.StartsWith(prefix)) { match = true; break; }
                }
            }
            else if (!string.IsNullOrEmpty(actorNamePrefix) && n.StartsWith(actorNamePrefix))
            {
                match = true;
            }
            if (!match) continue;
            if (added.Contains(rootT)) continue;
            added.Add(rootT);

            var a = new Agent
            {
                t = rootT,
                speed = Random.Range(minSpeed, maxSpeed),
                timer = 0f,
            };
            // Snap to ground
            var pos = rootT.position; pos.y = groundY; rootT.position = pos;
            a.target = NextTarget();
            agents.Add(a);
        }
        // Debug.Log($"PeopleWanderManager: agents discovered = {agents.Count}");
    }

    void UpgradeMaterials(Transform root)
    {
        var rends = root.GetComponentsInChildren<Renderer>(true);
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        foreach (var r in rends)
        {
            if (r == null) continue;
            var mats = r.materials; // instanced copy to avoid editing shared asset
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;
                var sn = m.shader != null ? m.shader.name : string.Empty;
                if (urpLit != null && (sn == "Standard" || sn.StartsWith("Legacy Shaders/")))
                {
                    Texture main = null;
                    if (m.HasProperty("_BaseMap")) main = m.GetTexture("_BaseMap");
                    if (main == null && m.HasProperty("_MainTex")) main = m.GetTexture("_MainTex");
                    Color col = Color.white;
                    if (m.HasProperty("_BaseColor")) col = m.GetColor("_BaseColor");
                    else if (m.HasProperty("_Color")) col = m.GetColor("_Color");

                    m.shader = urpLit;
                    if (main != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", main);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                    changed = true;
                }
            }
            if (changed) r.materials = mats;
        }

        // Make sure Animator root motion doesn't fight our movement
        var anim = root.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    Vector3 NextTarget()
    {
        float hx = Mathf.Max(0f, areaSize.x * 0.5f);
        float hz = Mathf.Max(0f, areaSize.z * 0.5f);
        var x = Random.Range(areaCenter.x - hx, areaCenter.x + hx);
        var z = Random.Range(areaCenter.z - hz, areaCenter.z + hz);
        return new Vector3(x, groundY, z);
    }

    void Update()
    {
        foreach (var a in agents)
        {
            a.timer += Time.deltaTime;
            var pos = a.t.position;
            var to = a.target - pos; to.y = 0f;
            if (to.sqrMagnitude < 0.05f || a.timer > maxTargetTime)
            {
                a.target = NextTarget();
                a.speed = Random.Range(minSpeed, maxSpeed);
                a.timer = 0f;
                to = a.target - pos; to.y = 0f;
            }
            // move towards
            var dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector3.forward;
            var step = Mathf.Max(0.01f, a.speed) * Time.deltaTime;
            var newPos = Vector3.MoveTowards(pos, new Vector3(a.target.x, groundY, a.target.z), step);
            newPos.y = groundY;
            a.t.position = newPos;

            if (faceMovement && dir.sqrMagnitude > 0.001f)
            {
                var targetRot = Quaternion.LookRotation(dir, Vector3.up);
                a.t.rotation = Quaternion.Slerp(a.t.rotation, targetRot, 8f * Time.deltaTime);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.25f);
        var center = new Vector3(areaCenter.x, groundY, areaCenter.z);
        Gizmos.DrawWireCube(center, new Vector3(areaSize.x, 0.1f, areaSize.z));
    }
}
