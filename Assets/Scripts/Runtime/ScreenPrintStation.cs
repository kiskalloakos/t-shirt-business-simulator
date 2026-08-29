using UnityEngine;

public sealed class ScreenPrintStation : Interactable
{
    private enum PrintPhase
    {
        Idle,
        LoweringScreen,
        Aligning,
        Printing,
        LiftingScreen
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
    private float screenMotion;
    private float pendingQuality;

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
            Day1Game.DayStage.CollectFinishedShirt => "[E] Pick up finished shirt",
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
        ResetAlignment();
        SetScreenLift(1f);
        screenMotion = 0f;
        phase = PrintPhase.LoweringScreen;
    }

    private void Update()
    {
        if (phase == PrintPhase.Idle || activePlayer == null)
            return;

        switch (phase)
        {
            case PrintPhase.LoweringScreen:
                UpdateScreenMotion(lowering: true);
                break;
            case PrintPhase.Aligning:
                UpdateAlignment();
                break;
            case PrintPhase.Printing:
                UpdatePrinting();
                break;
            case PrintPhase.LiftingScreen:
                UpdateScreenMotion(lowering: false);
                break;
        }
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

        SetScreenLift(0f);

        if (Input.GetMouseButtonDown(0))
        {
            phase = PrintPhase.Printing;
            if (inkPass != null)
                inkPass.gameObject.SetActive(true);
        }
    }

    private void UpdatePrinting()
    {
        float keyboardTilt = 0f;
        if (Input.GetKey(KeyCode.Q))
            keyboardTilt -= 30f * Time.deltaTime;
        if (Input.GetKey(KeyCode.E))
            keyboardTilt += 30f * Time.deltaTime;
        float scroll = Input.mouseScrollDelta.y * 2.5f;
        squeegeeAngle = Mathf.Clamp(squeegeeAngle + scroll + keyboardTilt, 25f, 65f);
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

        pendingQuality = quality;
        screenMotion = 0f;
        phase = PrintPhase.LiftingScreen;
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

    private void UpdateScreenMotion(bool lowering)
    {
        screenMotion = Mathf.Clamp01(screenMotion + Time.deltaTime / 0.85f);
        float lift = lowering ? 1f - screenMotion : screenMotion;
        SetScreenLift(lift);

        if (screenMotion < 1f)
            return;

        if (lowering)
        {
            phase = PrintPhase.Aligning;
            return;
        }

        if (inkPass != null)
            inkPass.gameObject.SetActive(false);
        float quality = pendingQuality;
        ExitFocus();
        phase = PrintPhase.Idle;
        Day1Game.Instance.ResolvePrint(quality);
        if (quality < 70f)
            SetShirtVisible(false);
    }

    private void SetScreenLift(float lift)
    {
        Vector3 downPosition = alignedFramePosition + new Vector3(alignmentX, 0f, alignmentZ);
        Vector3 upPosition = downPosition + new Vector3(0f, 0.78f, 0.38f);
        Quaternion downRotation = Quaternion.Euler(0f, alignmentRotation, 0f);
        Quaternion upRotation = Quaternion.Euler(-36f, alignmentRotation, 0f);
        screenFrame.localPosition = Vector3.Lerp(downPosition, upPosition, lift);
        screenFrame.localRotation = Quaternion.Slerp(downRotation, upRotation, lift);
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

        const float width = 680f;
        float x = Screen.width * 0.5f - width * 0.5f;
        const float y = 175f;
        GUI.Box(new Rect(x, y, width, 150f), GUIContent.none);

        if (phase == PrintPhase.LoweringScreen)
        {
            GUI.Label(new Rect(x + 20, y + 20, width - 40, 42), "LOWERING THE PREPARED SCREEN OVER THE SHIRT",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 19, fontStyle = FontStyle.Bold });
            GUI.Label(new Rect(x + 20, y + 70, width - 40, 35), "Mesh + stencil → shirt substrate",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15 });
        }
        else if (phase == PrintPhase.Aligning)
        {
            float accuracy = Mathf.Clamp01(1f - new Vector2(alignmentX, alignmentZ).magnitude / 0.37f) * 100f;
            GUI.Label(new Rect(x + 20, y + 18, width - 40, 30), "ALIGN THE STENCIL OVER THE SHIRT");
            GUI.Label(new Rect(x + 20, y + 55, width - 40, 70),
                $"Mouse: position   Q / E: rotate   Click: confirm\nCurrent centring: {accuracy:0}%   Rotation: {alignmentRotation:+0.0;-0.0;0}°");
        }
        else if (phase == PrintPhase.Printing)
        {
            GUI.Label(new Rect(x + 20, y + 12, width - 40, 30), "FORCE INK THROUGH THE MESH · PULL TOWARD YOU");
            GUI.Label(new Rect(x + 20, y + 43, width - 40, 23), "Q/E or scroll: set tilt · Hold click and pull mouse down");
            DrawAngleGauge(new Rect(x + 35, y + 96, width - 70, 30));
        }
        else
        {
            GUI.Label(new Rect(x + 20, y + 24, width - 40, 42), "LIFTING SCREEN · REVEALING PRINT",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 21, fontStyle = FontStyle.Bold });
            GUI.Label(new Rect(x + 20, y + 76, width - 40, 32), "The mesh snaps away, leaving ink on the shirt.",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15 });
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
