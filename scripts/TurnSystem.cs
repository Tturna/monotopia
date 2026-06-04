using Godot;

public partial class TurnSystem : Node2D
{
    public delegate void TurnStartedHandler();
    public event TurnStartedHandler TurnStarted;

    public static TurnSystem Instance;

    private int turnTimeSeconds = 5;
    private float turnTimer;
    private int turnCount;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        StartNextTurn();

        UIController.Instance.EndTurnButton.Pressed += () => StartNextTurn();
    }

    public override void _Process(double delta)
    {
        if (turnTimer <= 0) return;

        UIController.Instance.SetTurnTimerText(turnTimer);

        turnTimer -= (float)delta;

        if (turnTimer <= 0)
        {
            StartNextTurn();
        }
    }

    private void StartNextTurn()
    {
        turnTimer = turnTimeSeconds;
        turnCount++;
        UIController.Instance.SetTurnCountText(turnCount);
        DebugUtility.Print($"Starting turn {turnCount}");
        TurnStarted?.Invoke();
    }
}
