using System;
using Godot;

public partial class UIController : Node2D
{
    [Export]
    public Control OwnedCityViewControl;
    [Export]
	public Label CoinsLabel;
    [Export]
    public Label TurnCountLabel;
    [Export]
    public Label TurnTimerLabel;
    [Export]
    public Button EndTurnButton;

    public static UIController Instance;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public void ToggleOwnedCityView(bool visible)
    {
        if (visible)
        {
            OwnedCityViewControl.Show();
        }
        else
        {
            OwnedCityViewControl.Hide();
        }
    }

    public void SetCoinBalanceText(int coins, int delta)
    {
		var balanceText = coins.ToString();
		var deltaText = delta.ToString();
		CoinsLabel.Text = $"{balanceText} (+{deltaText})";
    }

    public void SetTurnTimerText(float secondsLeft)
    {
        var timeSpan = TimeSpan.FromSeconds(secondsLeft);
        TurnTimerLabel.Text = timeSpan.ToString(@"mm\:ss");
    }

    public void SetTurnCountText(int turn)
    {
        TurnCountLabel.Text = $"Turn {turn}";
    }
}
