using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class FirstPersonController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float mouseSensitivity = 2.2f;
    [SerializeField] private float gravity = -20f;

    private CharacterController characterController;
    private float pitch;
    private float verticalVelocity;
    private bool inputEnabled = true;

    public Camera PlayerCamera => playerCamera;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        LockCursor(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            LockCursor(Cursor.lockState != CursorLockMode.Locked);

        if (!inputEnabled || Cursor.lockState != CursorLockMode.Locked)
            return;

        float yaw = Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -82f, 82f);
        transform.Rotate(Vector3.up * yaw);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        input = Vector3.ClampMagnitude(input, 1f);
        Vector3 motion = transform.TransformDirection(input) * moveSpeed;

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        motion.y = verticalVelocity;
        characterController.Move(motion * Time.deltaTime);
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (enabled)
            LockCursor(true);
    }

    private static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
