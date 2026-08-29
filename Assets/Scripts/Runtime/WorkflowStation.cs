using UnityEngine;

public sealed class WorkflowStation : Interactable
{
    public enum StationType
    {
        OrderClipboard,
        ShirtStorage,
        ScreenSetup,
        SubmissionDesk
    }

    [SerializeField] private StationType stationType;
    [SerializeField] private GameObject pickupVisual;

    public void Configure(StationType type, GameObject visual = null)
    {
        stationType = type;
        pickupVisual = visual;
    }

    private void Update()
    {
        if (stationType == StationType.ScreenSetup && pickupVisual != null && Day1Game.Instance != null)
            pickupVisual.SetActive(Day1Game.Instance.Stage == Day1Game.DayStage.PrepareScreen);
    }

    public override string GetPrompt(Day1Game game)
    {
        return stationType switch
        {
            StationType.OrderClipboard => game.Stage == Day1Game.DayStage.ReadOrder
                ? "[E] Read today's order"
                : "Order: one navy shirt, cream chest print",
            StationType.ShirtStorage => game.Stage == Day1Game.DayStage.CollectShirt
                ? "[E] Take one blank navy shirt"
                : "Shirt storage",
            StationType.ScreenSetup => game.Stage == Day1Game.DayStage.PrepareScreen
                ? "[E] Add cream ink + order stencil, then take screen"
                : "Screen preparation bench",
            StationType.SubmissionDesk => game.Stage == Day1Game.DayStage.SubmitOrder
                ? "[E] Submit the finished shirt"
                : "Finished orders go here",
            _ => string.Empty
        };
    }

    public override void Interact(PlayerInteractor player, Day1Game game)
    {
        switch (stationType)
        {
            case StationType.OrderClipboard:
                game.ReadOrder();
                break;
            case StationType.ShirtStorage:
                game.CollectShirt();
                break;
            case StationType.ScreenSetup:
                game.PrepareScreen();
                if (pickupVisual != null)
                    pickupVisual.SetActive(false);
                break;
            case StationType.SubmissionDesk:
                game.SubmitOrder();
                break;
        }
    }
}
