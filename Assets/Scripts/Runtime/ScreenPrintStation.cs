using UnityEngine;

public sealed class ScreenPrintStation : Interactable
{
    private enum PrintPhase
    {
        Idle,
        Aligning,
        Printing
    }

    [SerializeField] private Transform screenFrame;
    [SerializeField] private Transform squeegee;
    [SerializeField] private Transform focusPose;
    [SerializeField] private Transform inkPass;
    [SerializeField] private Renderer screenMesh;
    [SerializeField] private GameObject shirtObject;
    [SerializeField] private Renderer printedDesign;

    private PrintPhase phase;
    private PlayerInteractor activePlayer;
    private Vector3 cameraPositionBeforeFocus;
    private Quaternion cameraRotationBeforeFocus;
    private Vector3 alignedFramePosition;
    private float alignmentX = 0.18f;
    private float alignmentZ = -0.14f;
    private float alignmentRotation = 8f;
    private float squeegeeAngle = 35f;
    private float pullProgress;
    private float angleScoreTotal;
    private float pullSamples;

    public void Configure(Transform frame, Transform tool, Transform cameraPose, Transform inkSpread,
        Renderer mesh, GameObject shirt, Renderer design)
    {
        screenFrame = frame;
        squeegee = tool;
        focusPose = cameraPose;
        inkPass = inkSpread;
        screenMesh = mesh;
        shirtObject = shirt;
        printedDesign = design;
        if (screenFrame != null)
            alignedFramePosition = screenFrame.localPosition;
        SetShirtVisible(false);
        if (inkPass != null)
            inkPass.gameObject.SetActive(false);
    }

    public override string GetPrompt(Day1Game game)
    {
        return game.Stage switch
        {
            Day1Game.DayStage.LoadPress => "[E] Load shirt and work at the press",
            Day1Game.DayStage.AlignAndPrint => "Printing in progress",
            Day1Game.DayStage.CollectFinishedShirt => "[E] Lift screen and pick up finished shirt",
            _ => "Manual screen-printing press"
        };
    }

    public override void Interact(PlayerInteractor player, Day1Game game)
    {
        if (game.Stage == Day1Game.DayStage.CollectFinishedShirt && phase == PrintPhase.Idle)
        {
            SetShirtVisible(false);
            game.CollectFinishedShirt();
            return;
        }

        if (game.Stage != Day1Game.DayStage.LoadPress || phase != PrintPhase.Idle)
            return;

        activePlayer = player;
        game.BeginPrinting();
        SetShirtVisible(true);
        EnterFocus();
        phase = PrintPhase.Aligning;
        ResetAlignment();
    }

    private void Update()
    {
        if (phase == PrintPhase.Idle || activePlayer == null)
            return;

        if (phase == PrintPhase.Aligning)
            UpdateAlignment();
        else
            UpdatePrinting();
    }

    private void UpdateAlignment()
    {
        alignmentX = Mathf.Clamp(alignmentX + Input.GetAxis("Mouse X") * 0.004f, -0.28f, 0.28f);
        alignmentZ = Mathf.Clamp(alignmentZ + Input.GetAxis("Mouse Y") * 0.004f, -0.24f, 0.24f);
        if (Input.GetKey(KeyCode.Q))
            alignmentRotation += 35f * Time.deltaTime;
        if (Input.GetKey(KeyCode.E))
            alignmentRotation -= 35f * Time.deltaTime;
        alignmentRotation = Mathf.Clamp(alignmentRotation, -15f, 15f);

        screenFrame.localPosition = alignedFramePosition + new Vector3(alignmentX, 0f, alignmentZ);
        screenFrame.localRotation = Quaternion.Euler(0f, alignmentRotation, 0f);

        if (Input.GetMouseButtonDown(0))
        {
            phase = PrintPhase.Printing;
            if (inkPass != null)
                inkPass.gameObject.SetActive(true);
        }
    }

    private void UpdatePrinting()
    {
        float scroll = Input.mouseScrollDelta.y;
        squeegeeAngle = Mathf.Clamp(squeegeeAngle + scroll * 2.5f, 25f, 65f);
        squeegee.localRotation = Quaternion.Euler(squeegeeAngle, 0f, 0f);

        if (!Input.GetMouseButton(0))
            return;

        float pull = Mathf.Max(0f, -Input.GetAxis("Mouse Y"));
        if (pull <= 0.001f)
            return;

        pullProgress = Mathf.Clamp01(pullProgress + pull * 0.018f);
        angleScoreTotal += Mathf.Clamp01(1f - Mathf.Abs(squeegeeAngle - 45f) / 20f);
        pullSamples += 1f;
        squeegee.localPosition = Vector3.Lerp(new Vector3(0f, 0.12f, 0.43f), new Vector3(0f, 0.12f, -0.43f), pullProgress);
        UpdateInkPass();

        if (pullProgress >= 1f)
            CompletePrint();
    }

    private void CompletePrint()
    {
        float positionError = new Vector2(alignmentX, alignmentZ).magnitude / 0.37f;
        float rotationError = Mathf.Abs(alignmentRotation) / 15f;
        float alignmentScore = Mathf.Clamp01(1f - (positionError * 0.72f + rotationError * 0.28f));
        float angleScore = pullSamples > 0f ? angleScoreTotal / pullSamples : 0f;
        float quality = Mathf.Clamp01(alignmentScore * 0.62f + angleScore * 0.38f) * 100f;

        if (printedDesign != null)
        {
            printedDesign.enabled = true;
            printedDesign.transform.localPosition = new Vector3(alignmentX, printedDesign.transform.localPosition.y, alignmentZ);
            printedDesign.transform.localRotation = Quaternion.Euler(0f, alignmentRotation, 0f);
        }

        ExitFocus();
        phase = PrintPhase.Idle;
        Day1Game.Instance.ResolvePrint(quality);

        if (quality < 70f)
            SetShirtVisible(false);
        else
        {
            screenFrame.localPosition = alignedFramePosition + new Vector3(alignmentX, 0.62f, alignmentZ + 0.25f);
            screenFrame.localRotation = Quaternion.Euler(-28f, alignmentRotation, 0f);
        }
    }

    private void EnterFocus()
    {
        Camera camera = activePlayer.PlayerCamera;
        cameraPositionBeforeFocus = camera.transform.position;
        cameraRotationBeforeFocus = camera.transform.rotation;
        activePlayer.Controller.SetInputEnabled(false);
        camera.transform.SetPositionAndRotation(focusPose.position, focusPose.rotation);
    }

    private void ExitFocus()
    {
        Camera camera = activePlayer.PlayerCamera;
        camera.transform.SetPositionAndRotation(cameraPositionBeforeFocus, cameraRotationBeforeFocus);
        activePlayer.Controller.SetInputEnabled(true);
        activePlayer = null;
    }

    private void ResetAlignment()
    {
        alignmentX = 0.18f;
        alignmentZ = -0.14f;
        alignmentRotation = 8f;
        squeegeeAngle = 35f;
        pullProgress = 0f;
        angleScoreTotal = 0f;
        pullSamples = 0f;
        if (squeegee != null)
        {
            squeegee.localPosition = new Vector3(0f, 0.12f, 0.43f);
            squeegee.localRotation = Quaternion.Euler(squeegeeAngle, 0f, 0f);
        }
        if (printedDesign != null)
            printedDesign.enabled = false;
        if (inkPass != null)
        {
            inkPass.gameObject.SetActive(false);
            UpdateInkPass();
        }
    }

    private void SetShirtVisible(bool visible)
    {
        if (shirtObject != null)
            shirtObject.SetActive(visible);
        if (!visible && printedDesign != null)
            printedDesign.enabled = false;
    }

    private void OnGUI()
    {
        if (phase == PrintPhase.Idle)
            return;

        float width = 540f;
        float x = Screen.width * 0.5f - width * 0.5f;
        GUI.Box(new Rect(x, Screen.height - 155f, width, 116f), GUIContent.none);

        if (phase == PrintPhase.Aligning)
        {
            float accuracy = Mathf.Clamp01(1f - new Vector2(alignmentX, alignmentZ).magnitude / 0.37f) * 100f;
            GUI.Label(new Rect(x + 18, Screen.height - 140f, width - 36, 28), "ALIGN THE SCREEN");
            GUI.Label(new Rect(x + 18, Screen.height - 110f, width - 36, 55),
                $"Mouse: position   Q / E: rotate   Click: confirm\nCurrent centring: {accuracy:0}%   Rotation: {alignmentRotation:+0.0;-0.0;0}°");
        }
        else
        {
            GUI.Label(new Rect(x + 18, Screen.height - 140f, width - 36, 28), "PULL THE SQUEEGEE TOWARD YOU");
            GUI.Label(new Rect(x + 18, Screen.height - 112f, width - 36, 24), "Scroll to set tilt · Hold click and pull mouse down");
            DrawAngleGauge(new Rect(x + 20, Screen.height - 82f, width - 40, 26));
        }
    }

    private void UpdateInkPass()
    {
        if (inkPass == null)
            return;

        float length = Mathf.Lerp(0.015f, 0.88f, pullProgress);
        inkPass.localScale = new Vector3(1.72f, 0.018f, length);
        inkPass.localPosition = new Vector3(0f, 0.045f, 0.43f - length * 0.5f);
    }

    private void DrawAngleGauge(Rect rect)
    {
        GUI.Box(rect, GUIContent.none);
        float targetX = Mathf.Lerp(rect.x, rect.xMax, (45f - 25f) / 40f);
        Color previous = GUI.color;
        GUI.color = new Color(0.2f, 0.85f, 0.38f, 0.75f);
        GUI.DrawTexture(new Rect(targetX - 28f, rect.y + 3f, 56f, rect.height - 6f), Texture2D.whiteTexture);

        float markerX = Mathf.Lerp(rect.x, rect.xMax, (squeegeeAngle - 25f) / 40f);
        bool perfect = Mathf.Abs(squeegeeAngle - 45f) <= 2f;
        GUI.color = perfect ? Color.white : new Color(1f, 0.38f, 0.2f);
        GUI.DrawTexture(new Rect(markerX - 3f, rect.y - 4f, 6f, rect.height + 8f), Texture2D.whiteTexture);
        GUI.color = previous;

        string status = perfect ? "PERFECT" : squeegeeAngle < 43f ? "TILT MORE" : "TILT LESS";
        GUI.Label(new Rect(rect.x, rect.y - 29f, rect.width, 25f),
            $"ANGLE  {squeegeeAngle:0}° / 45°  ·  {status}  ·  PULL {pullProgress * 100f:0}%",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
        GUI.Label(new Rect(rect.x, rect.yMax + 1f, rect.width, 20f), "25°                         45°                         65°",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11 });
    }
}
