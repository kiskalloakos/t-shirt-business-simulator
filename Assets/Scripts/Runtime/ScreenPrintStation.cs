using UnityEngine;

public sealed class ScreenPrintStation : Interactable
{
    private enum PrintPhase
    {
        Idle,
        AligningRaisedScreen,
        LoweringScreen,
        SettingSqueegeeAngle,
        Printing,
        LiftingScreen
    }

    [SerializeField] private Transform screenFrame;
    [SerializeField] private Transform squeegee;
    [SerializeField] private Transform focusPose;
    [SerializeField] private Transform angleFocusPose;
    [SerializeField] private Renderer screenMesh;
    [SerializeField] private GameObject shirtObject;
    [SerializeField] private Renderer printedDesign;

    private PrintPhase phase;
    private PlayerInteractor activePlayer;
    private Vector3 cameraPositionBeforeFocus;
    private Quaternion cameraRotationBeforeFocus;
    private float cameraFieldOfViewBeforeFocus;
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

    public void Configure(Transform frame, Transform tool, Transform cameraPose, Transform closeAnglePose,
        Renderer mesh, GameObject shirt, Renderer design)
    {
        screenFrame = frame;
        squeegee = tool;
        focusPose = cameraPose;
        angleFocusPose = closeAnglePose;
        screenMesh = mesh;
        shirtObject = shirt;
        printedDesign = design;
        if (screenFrame != null)
        {
            alignedFramePosition = screenFrame.localPosition;
            screenFrame.gameObject.SetActive(false);
        }
        SetShirtVisible(false);
    }

    private void Awake()
    {
        // Configure runs in the editor when the scene is generated. Runtime-only fields
        // must be restored from the serialized transforms when Play Mode begins.
        if (screenFrame != null)
        {
            alignedFramePosition = screenFrame.localPosition;
            screenFrame.gameObject.SetActive(false);
        }
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
            screenFrame.gameObject.SetActive(false);
            game.CollectFinishedShirt();
            return;
        }

        if (game.Stage != Day1Game.DayStage.LoadPress || phase != PrintPhase.Idle)
            return;

        activePlayer = player;
        game.BeginPrinting();
        SetShirtVisible(true);
        screenFrame.gameObject.SetActive(true);
        EnterFocus();
        ResetAlignment();
        SetScreenLift(1f);
        phase = PrintPhase.AligningRaisedScreen;
    }

    private void Update()
    {
        if (phase == PrintPhase.Idle || activePlayer == null)
            return;

        switch (phase)
        {
            case PrintPhase.AligningRaisedScreen:
                UpdateAlignment();
                break;
            case PrintPhase.LoweringScreen:
                UpdateScreenMotion(lowering: true);
                break;
            case PrintPhase.SettingSqueegeeAngle:
                UpdateSqueegeeAngle();
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

        SetScreenLift(1f);

        if (Input.GetMouseButtonDown(0))
        {
            screenMotion = 0f;
            phase = PrintPhase.LoweringScreen;
        }
    }

    private void UpdateSqueegeeAngle()
    {
        // This is a physical forward/back setup: push the mouse forward to move
        // the handle away, pull it backward to lean the handle toward the player.
        float forwardBack = Input.GetAxisRaw("Mouse Y");
        squeegeeAngle = Mathf.Clamp(squeegeeAngle - forwardBack * 0.32f, 25f, 65f);
        // The handle leans toward the player (negative local Z) while the rubber
        // contact edge remains planted on the mesh.
        squeegee.localRotation = Quaternion.Euler(-squeegeeAngle, 0f, 0f);

        if (!Input.GetMouseButtonDown(0))
            return;

        MoveCameraTo(focusPose);
        activePlayer.PlayerCamera.fieldOfView = 54f;
        pullProgress = 0f;
        angleScoreTotal = Mathf.Clamp01(1f - Mathf.Abs(squeegeeAngle - 45f) / 8f);
        pullSamples = 1f;
        phase = PrintPhase.Printing;
    }

    private void UpdatePrinting()
    {
        if (!Input.GetMouseButton(0))
            return;

        float pull = Mathf.Max(0f, -Input.GetAxisRaw("Mouse Y"));
        if (pull <= 0.001f)
            return;

        pullProgress = Mathf.Clamp01(pullProgress + pull * 0.035f);
        squeegee.localPosition = Vector3.Lerp(new Vector3(0f, 0.055f, 0.48f), new Vector3(0f, 0.055f, -0.48f), pullProgress);
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
        cameraFieldOfViewBeforeFocus = camera.fieldOfView;
        activePlayer.Controller.SetInputEnabled(false);
        MoveCameraTo(focusPose);
        camera.fieldOfView = 54f;
    }

    private void ExitFocus()
    {
        Camera camera = activePlayer.PlayerCamera;
        camera.transform.SetPositionAndRotation(cameraPositionBeforeFocus, cameraRotationBeforeFocus);
        camera.fieldOfView = cameraFieldOfViewBeforeFocus;
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
            squeegee.localPosition = new Vector3(0f, 0.055f, 0.48f);
            squeegee.localRotation = Quaternion.Euler(-squeegeeAngle, 0f, 0f);
        }
        if (printedDesign != null)
            printedDesign.enabled = false;
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
            MoveCameraTo(angleFocusPose);
            activePlayer.PlayerCamera.fieldOfView = 46f;
            phase = PrintPhase.SettingSqueegeeAngle;
            return;
        }

        float quality = pendingQuality;
        ExitFocus();
        phase = PrintPhase.Idle;
        Day1Game.Instance.ResolvePrint(quality);
        if (quality < 70f)
        {
            SetShirtVisible(false);
            screenFrame.gameObject.SetActive(false);
        }
    }

    private void SetScreenLift(float lift)
    {
        Vector3 downPosition = alignedFramePosition + new Vector3(alignmentX, 0f, alignmentZ);
        Vector3 upPosition = downPosition + new Vector3(0f, 0.68f, 0f);
        Quaternion downRotation = Quaternion.Euler(0f, alignmentRotation, 0f);
        Quaternion upRotation = downRotation;
        screenFrame.localPosition = Vector3.Lerp(downPosition, upPosition, lift);
        screenFrame.localRotation = Quaternion.Slerp(downRotation, upRotation, lift);
    }

    private void MoveCameraTo(Transform pose)
    {
        if (activePlayer == null || pose == null)
            return;

        activePlayer.PlayerCamera.transform.SetPositionAndRotation(pose.position, pose.rotation);
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

        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        const float width = 350f;
        float x = Screen.width - width - 22f;
        const float y = 22f;
        GUI.Box(new Rect(x, y, width, 210f), GUIContent.none);

        if (phase == PrintPhase.AligningRaisedScreen)
        {
            float accuracy = Mathf.Clamp01(1f - new Vector2(alignmentX, alignmentZ).magnitude / 0.37f) * 100f;
            GUI.Label(new Rect(x + 18, y + 14, width - 36, 30), "1 · ALIGN RAISED SCREEN");
            GUI.Label(new Rect(x + 18, y + 48, width - 36, 92),
                $"Mouse: move stencil over shirt\nQ / E: rotate screen\nCentring: {accuracy:0}%   Rotation: {alignmentRotation:+0.0;-0.0;0}°\n\nCLICK TO LOWER SCREEN");
        }
        else if (phase == PrintPhase.LoweringScreen)
        {
            GUI.Label(new Rect(x + 18, y + 28, width - 36, 40), "LOWERING SCREEN ONTO SHIRT",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 17, fontStyle = FontStyle.Bold });
            GUI.Label(new Rect(x + 18, y + 78, width - 36, 40), "The stencil stays aligned as the mesh meets the fabric.",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13, wordWrap = true });
        }
        else if (phase == PrintPhase.SettingSqueegeeAngle)
        {
            GUI.Label(new Rect(x + 18, y + 10, width - 36, 27), "2 · SET SQUEEGEE TO 45°");
            GUI.Label(new Rect(x + 18, y + 39, width - 36, 42), "Tilt the WOODEN HAND TOOL only\nScreen and metal holder stay fixed");
            GUI.Label(new Rect(x + 18, y + 80, width - 36, 20), "Mouse forward / backward only");
            DrawAngleGauge(new Rect(x + 22, y + 132, width - 44, 34));
            GUI.Label(new Rect(x + 18, y + 188, width - 36, 20), "CLICK TO LOCK THE ANGLE");
        }
        else if (phase == PrintPhase.Printing)
        {
            GUI.Label(new Rect(x + 18, y + 10, width - 36, 27), "3 · PULL TOP → BOTTOM");
            GUI.Label(new Rect(x + 18, y + 42, width - 36, 48), "Hold left click and drag the mouse DOWN toward yourself. Keep moving downward.",
                new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true });
            DrawPullGauge(new Rect(x + 25, y + 112, width - 50, 32));
            GUI.Label(new Rect(x + 18, y + 149, width - 36, 20), $"Locked angle: {squeegeeAngle:0}°");
        }
        else
        {
            GUI.Label(new Rect(x + 20, y + 24, width - 40, 42), "LIFTING SCREEN · REVEALING PRINT",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 21, fontStyle = FontStyle.Bold });
            GUI.Label(new Rect(x + 20, y + 76, width - 40, 32), "The mesh snaps away, leaving ink on the shirt.",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15 });
        }

        GUI.matrix = previousMatrix;
    }

    private void DrawAngleGauge(Rect rect)
    {
        GUI.Box(rect, GUIContent.none);
        float targetX = Mathf.Lerp(rect.x, rect.xMax, (45f - 25f) / 40f);
        Color previous = GUI.color;
        GUI.color = new Color(0.2f, 0.85f, 0.38f, 0.75f);
        GUI.DrawTexture(new Rect(targetX - 8f, rect.y + 3f, 16f, rect.height - 6f), Texture2D.whiteTexture);

        float markerX = Mathf.Lerp(rect.x, rect.xMax, (squeegeeAngle - 25f) / 40f);
        bool perfect = Mathf.Abs(squeegeeAngle - 45f) <= 1f;
        GUI.color = perfect ? Color.white : new Color(1f, 0.38f, 0.2f);
        GUI.DrawTexture(new Rect(markerX - 3f, rect.y - 4f, 6f, rect.height + 8f), Texture2D.whiteTexture);
        GUI.color = previous;

        string status = perfect ? "PERFECT — 45°" : squeegeeAngle < 44f ? "PULL BACK" : "PUSH FORWARD";
        GUI.Label(new Rect(rect.x, rect.y - 29f, rect.width, 25f),
            $"{squeegeeAngle:0}°  ·  {status}",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
        GUI.Label(new Rect(rect.x, rect.yMax + 1f, rect.width, 20f), "25°                         45°                         65°",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11 });
    }

    private void DrawPullGauge(Rect rect)
    {
        GUI.Box(rect, GUIContent.none);
        Color previous = GUI.color;
        GUI.color = new Color(0.92f, 0.26f, 0.12f, 0.9f);
        GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, (rect.width - 6f) * pullProgress, rect.height - 6f), Texture2D.whiteTexture);
        GUI.color = previous;
        GUI.Label(rect, $"PULL  ↓  {pullProgress * 100f:0}%",
            new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
    }
}
