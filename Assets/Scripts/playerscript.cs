
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class playerscript : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.12f;
    public float minLookAngle = -85f;
    public float maxLookAngle = 85f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch;
    private bool cursorLocked = true;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();

            if (cam != null)
                playerCamera = cam.transform;
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                "[FPS] No camera found. Assign Player Camera in Inspector.",
                this
            );

            enabled = false;
            return;
        }

        // Initialize pitch from the camera's current local rotation.
        cameraPitch = playerCamera.localEulerAngles.x;

        if (cameraPitch > 180f)
            cameraPitch -= 360f;

        LockCursor(true);

        Log("First-person controller initialized.");
        Log("WASD = move | Mouse = look | Space = jump");
        Log("Shift = sprint | Escape = release cursor");
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null)
            return;

        // Escape releases the mouse.
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            LockCursor(false);
        }

        // Click to recapture the mouse.
        if (!cursorLocked && mouse != null &&
            mouse.leftButton.wasPressedThisFrame)
        {
            LockCursor(true);
        }

        if (cursorLocked && mouse != null)
        {
            HandleMouseLook(mouse);
        }

        HandleMovement(keyboard);
    }

    private void HandleMouseLook(Mouse mouse)
    {
        Vector2 mouseDelta = mouse.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        // Rotate the player horizontally.
        transform.Rotate(
            Vector3.up * mouseX,
            Space.Self
        );

        // Rotate the camera vertically.
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(
            cameraPitch,
            minLookAngle,
            maxLookAngle
        );

        playerCamera.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleMovement(Keyboard keyboard)
    {
        bool grounded = controller.isGrounded;

        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // WASD movement relative to where the player looks.
        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed) horizontal -= 1f;
        if (keyboard.dKey.isPressed) horizontal += 1f;
        if (keyboard.sKey.isPressed) vertical -= 1f;
        if (keyboard.wKey.isPressed) vertical += 1f;

        Vector3 move =
            transform.right * horizontal +
            transform.forward * vertical;

        move = Vector3.ClampMagnitude(move, 1f);

        float speed = keyboard.leftShiftKey.isPressed ||
                      keyboard.rightShiftKey.isPressed
            ? sprintSpeed
            : walkSpeed;

        controller.Move(move * speed * Time.deltaTime);

        // Jump.
        if (grounded && keyboard.spaceKey.wasPressedThisFrame)
        {
            verticalVelocity = Mathf.Sqrt(
                jumpHeight * -2f * gravity
            );

            Log("Jump!");
        }

        // Gravity.
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move(
            Vector3.up * verticalVelocity * Time.deltaTime
        );
    }

    private void LockCursor(bool shouldLock)
    {
        cursorLocked = shouldLock;

        Cursor.lockState = shouldLock
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !shouldLock;

        if (enableDebugLogs)
        {
            Debug.Log(
                "[FPS] Mouse " +
                (shouldLock ? "locked." : "released."),
                this
            );
        }
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log("[FPS] " + message, this);
    }
}
