using UnityEngine;
using UnityEngine.InputSystem; // NEW input system

[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    [Header("Speeds")]
    public float moveSpeed = 10f;
    public float ascendSpeed = 6f;
    public float yawSpeed = 120f;
    public float accel = 8f;

    [Header("Optional visuals")]
    public Transform visual;
    public float tiltAmount = 15f;
    public float tiltLerp = 10f;

    private Rigidbody rb;
    private Vector3 velTarget;

    // New Input System actions
    private InputAction moveAction;     // WASD/Arrows or gamepad left stick (Vector2)
    private InputAction ascendPos;      // Space = +1
    private InputAction ascendNeg;      // LeftShift = -1
    private InputAction yawLeft;        // Q
    private InputAction yawRight;       // E

    void OnEnable()
    {
        // Move (WASD/arrows + gamepad stick)
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");
        moveAction.Enable();

        // Ascend / Descend (Space / LeftShift)
        ascendPos = new InputAction("AscendPos", binding: "<Keyboard>/space");
        ascendNeg = new InputAction("AscendNeg", binding: "<Keyboard>/leftShift");
        ascendPos.Enable(); ascendNeg.Enable();

        // Yaw (Q/E)
        yawLeft  = new InputAction("YawLeft",  binding: "<Keyboard>/q");
        yawRight = new InputAction("YawRight", binding: "<Keyboard>/e");
        yawLeft.Enable(); yawRight.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable(); ascendPos?.Disable(); ascendNeg?.Disable();
        yawLeft?.Disable(); yawRight?.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        Vector2 mv = moveAction.ReadValue<Vector2>();
        float upDown = (ascendPos.IsPressed() ? 1f : 0f) + (ascendNeg.IsPressed() ? -1f : 0f);

        Vector3 planar   = transform.forward * mv.y * moveSpeed + transform.right * mv.x * moveSpeed;
        Vector3 vertical = Vector3.up * upDown * ascendSpeed;
        velTarget = planar + vertical;

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, velTarget, Time.fixedDeltaTime * accel);

        float yaw = (yawLeft.IsPressed() ? -1f : 0f) + (yawRight.IsPressed() ? 1f : 0f);
        if (Mathf.Abs(yaw) > 0.01f)
            transform.Rotate(Vector3.up, yaw * yawSpeed * Time.fixedDeltaTime, Space.World);

        if (visual)
        {
            float roll  =  mv.x *  tiltAmount;
            float pitch = -mv.y *  tiltAmount;
            Quaternion target = Quaternion.Euler(pitch, 0f, roll);
            visual.localRotation = Quaternion.Slerp(visual.localRotation, target, Time.fixedDeltaTime * tiltLerp);
        }
    }
}
