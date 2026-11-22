using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Attach to a Player root with a CharacterController. Put its camera underneath it.
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class FirstPersonController : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private Transform playerCamera;
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
    [SerializeField, Range(1f, 89f)] private float lookLimit = 85f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 4f;
    [SerializeField, Min(0f)] private float sprintSpeed = 7f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private Quaternion initialCameraRotation;
    private float pitch;
    private float verticalSpeed;

    private void Reset()
    {
        CharacterController capsule = GetComponent<CharacterController>();
        capsule.height = 1.8f;
        capsule.radius = 0.3f;
        capsule.center = new Vector3(0f, 0.9f, 0f);
        capsule.stepOffset = 0.3f;
        Camera childCamera = GetComponentInChildren<Camera>();
        if (childCamera != null) playerCamera = childCamera.transform;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null) playerCamera = childCamera.transform;
        }

        if (playerCamera == null || playerCamera == transform ||
            !playerCamera.IsChildOf(transform))
        {
            Debug.LogError("Assign a camera that is a child of this Player.", this);
            enabled = false;
            return;
        }

        initialCameraRotation = playerCamera.localRotation;
    }

    private void OnEnable()
    {
        SetCursorLocked(true);
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) SetCursorLocked(false);
    }

    private void Update()
    {
        ReadInput(out Vector2 move, out Vector2 look, out bool jump,
            out bool sprint, out bool escape, out bool click);

        if (escape) SetCursorLocked(false);
        else if (click && Application.isFocused) SetCursorLocked(true);

        bool controlling = Cursor.lockState == CursorLockMode.Locked && Application.isFocused;
        if (controlling)
        {
            // Mouse delta is already a per-frame displacement; don't multiply by deltaTime.
            transform.Rotate(0f, look.x * mouseSensitivity, 0f);
            pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, -lookLimit, lookLimit);
            playerCamera.localRotation = initialCameraRotation * Quaternion.Euler(pitch, 0f, 0f);
        }
        else
        {
            move = Vector2.zero;
            jump = false;
            sprint = false;
        }

        if (!controller.enabled) return;
        float downwardGravity = -Mathf.Max(0.01f, Mathf.Abs(gravity));
        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        if (controller.isGrounded && jump)
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * downwardGravity);

        verticalSpeed += downwardGravity * Time.deltaTime;
        Vector2 clampedMove = Vector2.ClampMagnitude(move, 1f);
        Vector3 direction = transform.right * clampedMove.x + transform.forward * clampedMove.y;
        Vector3 velocity = direction * (sprint ? sprintSpeed : walkSpeed);
        velocity.y = verticalSpeed;
        CollisionFlags collisions = controller.Move(velocity * Time.deltaTime);
        if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
            verticalSpeed = 0f;
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private static void ReadInput(out Vector2 move, out Vector2 look,
        out bool jump, out bool sprint, out bool escape, out bool click)
    {
        move = Vector2.zero;
        look = Vector2.zero;
        jump = sprint = escape = click = false;
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard != null)
        {
            move.x = ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1f : 0f)
                   - ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1f : 0f);
            move.y = ((keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1f : 0f)
                   - ((keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1f : 0f);
            jump = keyboard.spaceKey.wasPressedThisFrame;
            sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            escape = keyboard.escapeKey.wasPressedThisFrame;
        }
        if (mouse != null)
        {
            look = mouse.delta.ReadValue();
            click = mouse.leftButton.wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        move.x = ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ? 1f : 0f)
               - ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) ? 1f : 0f);
        move.y = ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ? 1f : 0f)
               - ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ? 1f : 0f);
        // Approximate pixel-like sensitivity with Unity's default legacy mouse axes.
        look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 20f;
        jump = Input.GetKeyDown(KeyCode.Space);
        sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        escape = Input.GetKeyDown(KeyCode.Escape);
        click = Input.GetMouseButtonDown(0);
#endif
    }
}
