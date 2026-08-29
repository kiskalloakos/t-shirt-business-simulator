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

    public void Configure(StationType type) => stationType = type;

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
                ? "[E] Prepare the screen and cream ink"
                : "Screen and ink setup",
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
                break;
            case StationType.SubmissionDesk:
                game.SubmitOrder();
                break;
        }
    }
}
