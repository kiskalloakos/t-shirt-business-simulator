using UnityEngine;

public sealed class ScreenPreparationStation : Interactable
{
    private enum PreparationPhase
    {
        Idle,
        MixInk,
        CoatScreen
    }

    [SerializeField] private Transform focusPose;
    [SerializeField] private Transform mixingTool;
    [SerializeField] private Transform coatingTool;
    [SerializeField] private Transform coatingFill;

    private PreparationPhase phase;
    private PlayerInteractor activePlayer;
    private Vector3 cameraPositionBeforeFocus;
    private Quaternion cameraRotationBeforeFocus;
    private float mixProgress;
    private float coatProgress;
    private float stirAngle;

    public void Configure(Transform cameraPose, Transform stirrer, Transform coater, Transform fill)
    {
        focusPose = cameraPose;
        mixingTool = stirrer;
        coatingTool = coater;
        coatingFill = fill;
        SetToolVisibility(false, false);
        UpdateCoatingVisual();
    }

    public override string GetPrompt(Day1Game game)
    {
        return game.Stage == Day1Game.DayStage.PrepareScreen
            ? "[E] Prepare screen and mix cream ink"
            : "Screen and ink preparation bench";
    }

    public override void Interact(PlayerInteractor player, Day1Game game)
    {
        if (game.Stage != Day1Game.DayStage.PrepareScreen || phase != PreparationPhase.Idle)
            return;

        activePlayer = player;
        mixProgress = 0f;
        coatProgress = 0f;
        stirAngle = 0f;
        phase = PreparationPhase.MixInk;
        EnterFocus();
        SetToolVisibility(true, false);
        UpdateCoatingVisual();
    }

    private void Update()
    {
        if (phase == PreparationPhase.Idle || activePlayer == null)
            return;

        if (phase == PreparationPhase.MixInk)
            UpdateInkMixing();
        else
            UpdateScreenCoating();
    }

    private void UpdateInkMixing()
    {
        if (!Input.GetMouseButton(0))
            return;

        Vector2 mouse = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        float motion = mouse.magnitude;
        if (motion <= 0.01f)
            return;

        mixProgress = Mathf.Clamp01(mixProgress + motion * 0.012f);
        stirAngle += motion * 14f;
        if (mixingTool != null)
        {
            mixingTool.localPosition = new Vector3(Mathf.Cos(stirAngle * Mathf.Deg2Rad) * 0.13f, 0.18f,
                Mathf.Sin(stirAngle * Mathf.Deg2Rad) * 0.13f);
            mixingTool.localRotation = Quaternion.Euler(12f, stirAngle, 0f);
        }

        if (mixProgress >= 1f)
        {
            phase = PreparationPhase.CoatScreen;
            SetToolVisibility(false, true);
        }
    }

    private void UpdateScreenCoating()
    {
        if (!Input.GetMouseButton(0))
            return;

        float horizontal = Input.GetAxis("Mouse X");
        if (Mathf.Abs(horizontal) <= 0.01f)
            return;

        coatProgress = Mathf.Clamp01(coatProgress + Mathf.Abs(horizontal) * 0.011f);
        if (coatingTool != null)
        {
            Vector3 position = coatingTool.localPosition;
            position.x = Mathf.Clamp(position.x + horizontal * 0.025f, -0.62f, 0.62f);
            coatingTool.localPosition = position;
        }
        UpdateCoatingVisual();

        if (coatProgress >= 1f)
            CompletePreparation();
    }

    private void CompletePreparation()
    {
        phase = PreparationPhase.Idle;
        SetToolVisibility(false, false);
        ExitFocus();
        Day1Game.Instance.PrepareScreen();
    }

    private void EnterFocus()
    {
        Camera camera = activePlayer.PlayerCamera;
        cameraPositionBeforeFocus = camera.transform.position;
        cameraRotationBeforeFocus = camera.transform.rotation;
        activePlayer.Controller.SetInputEnabled(false);
        Day1Game.Instance.SetInputCaptured(true);
        camera.transform.SetPositionAndRotation(focusPose.position, focusPose.rotation);
    }

    private void ExitFocus()
    {
        Camera camera = activePlayer.PlayerCamera;
        camera.transform.SetPositionAndRotation(cameraPositionBeforeFocus, cameraRotationBeforeFocus);
        activePlayer.Controller.SetInputEnabled(true);
        Day1Game.Instance.SetInputCaptured(false);
        activePlayer = null;
    }

    private void SetToolVisibility(bool stirrer, bool coater)
    {
        if (mixingTool != null)
            mixingTool.gameObject.SetActive(stirrer);
        if (coatingTool != null)
            coatingTool.gameObject.SetActive(coater);
    }

    private void UpdateCoatingVisual()
    {
        if (coatingFill == null)
            return;

        float width = Mathf.Lerp(0.02f, 1.25f, coatProgress);
        coatingFill.localScale = new Vector3(width, coatingFill.localScale.y, coatingFill.localScale.z);
        coatingFill.localPosition = new Vector3(Mathf.Lerp(-0.615f, 0f, coatProgress), coatingFill.localPosition.y, coatingFill.localPosition.z);
    }

    private void OnGUI()
    {
        if (phase == PreparationPhase.Idle)
            return;

        const float width = 560f;
        float x = Screen.width * 0.5f - width * 0.5f;
        GUI.Box(new Rect(x, Screen.height - 160f, width, 120f), GUIContent.none);

        if (phase == PreparationPhase.MixInk)
        {
            GUI.Label(new Rect(x + 20, Screen.height - 145f, width - 40, 28), "MIX THE CREAM INK");
            GUI.Label(new Rect(x + 20, Screen.height - 113f, width - 40, 25), "Hold click and move the mouse in circles.");
            DrawProgress(new Rect(x + 20, Screen.height - 78f, width - 40, 20), mixProgress, new Color(0.94f, 0.82f, 0.48f));
        }
        else
        {
            GUI.Label(new Rect(x + 20, Screen.height - 145f, width - 40, 28), "COAT THE SCREEN EVENLY");
            GUI.Label(new Rect(x + 20, Screen.height - 113f, width - 40, 25), "Hold click and sweep the mouse left and right.");
            DrawProgress(new Rect(x + 20, Screen.height - 78f, width - 40, 20), coatProgress, new Color(0.1f, 0.58f, 0.86f));
        }
    }

    private static void DrawProgress(Rect rect, float progress, Color color)
    {
        GUI.Box(rect, GUIContent.none);
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x + 3, rect.y + 3, (rect.width - 6) * progress, rect.height - 6), Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.Label(rect, $"{progress * 100f:0}%", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
    }
}
