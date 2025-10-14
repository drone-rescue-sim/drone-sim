// unity/Assets/Scripts/SimpleCameraController.cs
using UnityEngine;

public class SimpleCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float fastMoveSpeed = 20f;
    public float mouseSensitivity = 2f;
    
    [Header("Controls")]
    public bool enableInPlayMode = true;
    
    private float rotationX = 0f;
    private float rotationY = 0f;
    
    void Start()
    {
        // Initialize rotation from current camera rotation
        Vector3 rot = transform.localRotation.eulerAngles;
        rotationY = rot.y;
        rotationX = rot.x;
    }
    
    void Update()
    {
        if (!enableInPlayMode)
            return;
        
        // Mouse look (hold right mouse button)
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            rotationY += Input.GetAxis("Mouse X") * mouseSensitivity;
            
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            
            transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Movement with WASD or Arrow Keys
        float speed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;
        
        Vector3 movement = Vector3.zero;
        
        // WASD controls
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            movement += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            movement -= transform.forward;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            movement -= transform.right;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            movement += transform.right;
        if (Input.GetKey(KeyCode.E))
            movement += Vector3.up;
        if (Input.GetKey(KeyCode.Q))
            movement -= Vector3.up;
        
        transform.position += movement.normalized * speed * Time.deltaTime;
    }
    
    void OnGUI()
    {
        if (enableInPlayMode)
        {
            GUI.Label(new Rect(10, 120, 400, 60), 
                "Camera Controls:\n" +
                "WASD = Move, E/Q = Up/Down\n" +
                "Hold Right Mouse = Look Around\n" +
                "Shift = Fast Move");
        }
    }
}