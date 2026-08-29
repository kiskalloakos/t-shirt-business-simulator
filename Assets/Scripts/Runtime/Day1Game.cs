using UnityEngine;

public sealed class Day1Game : MonoBehaviour
{
    public enum DayStage
    {
        ReadOrder,
        CollectShirt,
        PrepareScreen,
        LoadPress,
        AlignAndPrint,
        SubmitOrder,
        Complete
    }

    public static Day1Game Instance { get; private set; }

    [SerializeField] private float startingCash = 1000f;
    [SerializeField] private float orderPayment = 75f;
    [SerializeField] private float wastedShirtCost = 8f;

    private GUIStyle headerStyle;
    private GUIStyle bodyStyle;
    private GUIStyle centerStyle;
    private float cash;
    private float elapsedTime;
    private string interactionPrompt = string.Empty;
    private string notification = "Read the clipboard to start Day 1.";
    private float notificationUntil;
    private float finalQuality;
    private int wastedShirts;

    public DayStage Stage { get; private set; } = DayStage.ReadOrder;
    public bool InputCaptured { get; private set; }
    public string Objective { get; private set; } = "Read today's order";

    private void Awake()
    {
        Instance = this;
        cash = startingCash;
    }

    private void Update()
    {
        if (Stage != DayStage.Complete)
            elapsedTime += Time.deltaTime;
    }

    public void SetInteractionPrompt(string prompt) => interactionPrompt = prompt;

    public void SetInputCaptured(bool captured)
    {
        InputCaptured = captured;
        if (captured)
            interactionPrompt = string.Empty;
    }

    public void ReadOrder()
    {
        if (Stage != DayStage.ReadOrder)
            return;
        Advance(DayStage.CollectShirt, "Collect a navy blank shirt", "Order accepted: one navy shirt with a cream chest print.");
    }

    public void CollectShirt()
    {
        if (Stage != DayStage.CollectShirt)
            return;
        Advance(DayStage.PrepareScreen, "Prepare the screen and cream ink", "Blank shirt collected.");
    }

    public void PrepareScreen()
    {
        if (Stage != DayStage.PrepareScreen)
            return;
        Advance(DayStage.LoadPress, "Load the shirt onto the printing press", "Screen prepared. Try not to taste the ink.");
    }

    public void BeginPrinting()
    {
        if (Stage != DayStage.LoadPress)
            return;
        Advance(DayStage.AlignAndPrint, "Centre the design, then print at 45°", "Use the mouse carefully—this shirt cost actual money.");
        SetInputCaptured(true);
    }

    public void ResolvePrint(float quality)
    {
        finalQuality = quality;
        SetInputCaptured(false);

        if (quality >= 70f)
        {
            Advance(DayStage.SubmitOrder, "Take the finished shirt to the submission desk", $"Print accepted at {quality:0}% quality.");
            return;
        }

        wastedShirts++;
        cash -= wastedShirtCost;
        Advance(DayStage.CollectShirt, "Collect a replacement shirt", $"Print rejected ({quality:0}%). Wasted shirt: -${wastedShirtCost:0}.");
    }

    public void SubmitOrder()
    {
        if (Stage != DayStage.SubmitOrder)
            return;

        cash += orderPayment;
        Stage = DayStage.Complete;
        Objective = "Day 1 complete";
        Notify($"Order paid +${orderPayment:0}. Not bad for a garage empire.", 8f);
    }

    private void Advance(DayStage stage, string objective, string message)
    {
        Stage = stage;
        Objective = objective;
        Notify(message);
    }

    private void Notify(string message, float duration = 4f)
    {
        notification = message;
        notificationUntil = Time.time + duration;
    }

    private void OnGUI()
    {
        EnsureStyles();
        float scale = Mathf.Clamp(Screen.height / 900f, 0.8f, 1.3f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        float height = Screen.height / scale;

        GUI.Box(new Rect(18, 18, 330, 112), GUIContent.none);
        GUI.Label(new Rect(34, 29, 300, 28), "DAY 1 · GARAGE", headerStyle);
        GUI.Label(new Rect(34, 59, 300, 24), $"Cash  ${cash:0}     Time  {FormatTime(elapsedTime)}", bodyStyle);
        GUI.Label(new Rect(34, 85, 300, 36), Objective, bodyStyle);

        if (!InputCaptured)
        {
            GUI.Label(new Rect(width * 0.5f - 10, height * 0.5f - 15, 20, 30), "+", centerStyle);
            if (!string.IsNullOrEmpty(interactionPrompt))
                GUI.Box(new Rect(width * 0.5f - 190, height - 105, 380, 42), interactionPrompt);
        }

        if (Time.time < notificationUntil || (Stage == DayStage.ReadOrder && Time.time < 7f))
            GUI.Box(new Rect(width * 0.5f - 260, 24, 520, 44), notification);

        if (Stage == DayStage.Complete)
        {
            GUI.Box(new Rect(width * 0.5f - 230, height * 0.5f - 140, 460, 280), GUIContent.none);
            GUI.Label(new Rect(width * 0.5f - 200, height * 0.5f - 115, 400, 42), "DAY 1 COMPLETE", headerStyle);
            GUI.Label(new Rect(width * 0.5f - 200, height * 0.5f - 55, 400, 150),
                $"Print quality: {finalQuality:0}%\nWasted shirts: {wastedShirts}\nTime: {FormatTime(elapsedTime)}\nClosing cash: ${cash:0}", bodyStyle);
        }
    }

    private void EnsureStyles()
    {
        if (headerStyle != null)
            return;

        headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
        centerStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }
}
