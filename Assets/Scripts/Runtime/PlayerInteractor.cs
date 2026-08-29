using UnityEngine;

public sealed class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3.2f;
    [SerializeField] private LayerMask interactionMask = ~0;

    private Interactable focused;

    public FirstPersonController Controller { get; private set; }
    public Camera PlayerCamera => playerCamera;

    private void Awake()
    {
        Controller = GetComponent<FirstPersonController>();
        if (playerCamera == null && Controller != null)
            playerCamera = Controller.PlayerCamera;
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        focused = null;
        if (Day1Game.Instance == null || Day1Game.Instance.InputCaptured || playerCamera == null)
            return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore))
            focused = hit.collider.GetComponentInParent<Interactable>();

        Day1Game.Instance.SetInteractionPrompt(focused == null ? string.Empty : focused.GetPrompt(Day1Game.Instance));

        if (focused != null && Input.GetKeyDown(KeyCode.E))
            focused.Interact(this, Day1Game.Instance);
    }
}
