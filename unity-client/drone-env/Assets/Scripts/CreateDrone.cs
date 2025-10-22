using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateDrone : MonoBehaviour
{
    [ContextMenu("Create Drone with Custom Controller")]
    public void CreateCustomDrone()
    {
        // Create drone GameObject
        GameObject drone = new GameObject("CustomDrone");
        drone.transform.position = new Vector3(0, 2, 0);

        // Add components
        var meshFilter = drone.AddComponent<MeshFilter>();
        var meshRenderer = drone.AddComponent<MeshRenderer>();
        var rigidbody = drone.AddComponent<Rigidbody>();
        var collider = drone.AddComponent<BoxCollider>();

        // Add the custom DroneController
        var droneController = drone.AddComponent<DroneController>();

        // Create a simple drone mesh
        meshFilter.mesh = CreateDroneMesh();
        meshRenderer.material = CreateDroneMaterial();

        // Configure collider
        collider.size = new Vector3(1, 0.3f, 1);
        collider.center = new Vector3(0, 0, 0);

        // Configure Rigidbody
        rigidbody.mass = 1f;
        rigidbody.linearDamping = 0.5f;
        rigidbody.angularDamping = 2f;
        rigidbody.useGravity = false;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Create visual child object for tilt effects
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(drone.transform);
        visual.transform.localPosition = Vector3.zero;
        droneController.visual = visual.transform;

        // Add visual mesh to the visual object
        var visualMeshFilter = visual.AddComponent<MeshFilter>();
        var visualMeshRenderer = visual.AddComponent<MeshRenderer>();
        visualMeshFilter.mesh = CreateDroneMesh();
        visualMeshRenderer.material = CreateDroneMaterial();

        Debug.Log("Custom drone created with DroneController!");
        Debug.Log("Controls:");
        Debug.Log("• WASD/Arrows: Move forward/back/left/right");
        Debug.Log("• Space: Ascend");
        Debug.Log("• Left Shift: Descend");
        Debug.Log("• Q/E: Rotate left/right");
        Debug.Log("• The drone will tilt visually based on movement");
    }

    private Mesh CreateDroneMesh()
    {
        Mesh mesh = new Mesh();

        // Create a simple quadcopter-like mesh
        Vector3[] vertices = new Vector3[]
        {
            // Main body
            new Vector3(-0.5f, -0.1f, -0.5f), // 0
            new Vector3(0.5f, -0.1f, -0.5f),  // 1
            new Vector3(0.5f, -0.1f, 0.5f),   // 2
            new Vector3(-0.5f, -0.1f, 0.5f),  // 3
            new Vector3(-0.5f, 0.1f, -0.5f),  // 4
            new Vector3(0.5f, 0.1f, -0.5f),   // 5
            new Vector3(0.5f, 0.1f, 0.5f),    // 6
            new Vector3(-0.5f, 0.1f, 0.5f),   // 7

            // Arms
            new Vector3(-1.2f, 0f, 0f),       // 8 - left arm
            new Vector3(1.2f, 0f, 0f),        // 9 - right arm
            new Vector3(0f, 0f, -1.2f),       // 10 - front arm
            new Vector3(0f, 0f, 1.2f),        // 11 - back arm
        };

        int[] triangles = new int[]
        {
            // Main body - bottom
            0, 2, 1, 0, 3, 2,
            // Main body - top
            4, 5, 6, 4, 6, 7,
            // Main body - front
            0, 1, 5, 0, 5, 4,
            // Main body - back
            3, 7, 6, 3, 6, 2,
            // Main body - left
            0, 4, 7, 0, 7, 3,
            // Main body - right
            1, 2, 6, 1, 6, 5,
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private Material CreateDroneMaterial()
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = new Color(0.8f, 0.2f, 0.2f); // Red color
        material.SetFloat("_Glossiness", 0.3f);
        return material;
    }
}
