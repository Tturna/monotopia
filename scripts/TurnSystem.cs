using System;
using Godot;

public partial class TurnSystem : Node2D
{
    public delegate void TurnStartedHandler();
    public event TurnStartedHandler TurnStarted;

    public static TurnSystem Instance;

    private Label turnCountLabel => (Label)GetNode("%Turn Count Label");
    private Label turnTimerLabel => (Label)GetNode("%Turn Timer Label");
    private Button endTurnButton => (Button)GetNode("%End Turn Button");

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

        endTurnButton.Pressed += () => StartNextTurn();
    }

    public override void _Process(double delta)
    {
        if (turnTimer <= 0) return;

        var timeSpan = TimeSpan.FromSeconds(turnTimer);
        turnTimerLabel.Text = timeSpan.ToString(@"mm\:ss");

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
        turnCountLabel.Text = $"Turn {turnCount}";
        GD.Print($"Starting turn {turnCount}");
        TurnStarted?.Invoke();
    }
}
