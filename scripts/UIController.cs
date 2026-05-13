using System;
using Godot;

#nullable enable
public partial class UIController : Node2D
{
    [Export]
    public required Control OwnedCityViewControl;
    [Export]
	public required Label CoinsLabel;
    [Export]
    public required Label TurnCountLabel;
    [Export]
    public required Label TurnTimerLabel;
    [Export]
    public required Button EndTurnButton;
    [Export]
    public required PackedScene BuildListItemPanel;

    public static UIController Instance = null!;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public void ShowOwnedCityView(CityController city)
    {
        if (city is null)
        {
            throw new ArgumentException("Can't show city info for null city", nameof(city));
        }

        OwnedCityViewControl.Show();

        var cityNameLabel = (Label)OwnedCityViewControl.FindChild("CityNameLabel");
        cityNameLabel.Text = city!.CityName;

        var coinsGeneratedLabel = (Label)OwnedCityViewControl.FindChild("CoinsGeneratedLabel");
        var prefix = city.CoinsGenerated switch
        {
            > 0 => "+",
            < 0 => "-",
            _ => ""
        };
        var coinsText = Math.Abs(city.CoinsGenerated).ToString();

        coinsGeneratedLabel.Text = $"Coins generated: {prefix}{coinsText}";

        var buildListScrollVBox = (VBoxContainer)OwnedCityViewControl.FindChild("Build List Scroll VBox");

        while (buildListScrollVBox.GetChildCount() > 0)
        {
            var child = buildListScrollVBox.GetChild(0);
            child.QueueFree();
            buildListScrollVBox.RemoveChild(child);
        }

        var buildables = city.TempGetBuildables();

        foreach (var buildable in buildables)
        {
            var name = buildable.Item1;
            var cost = buildable.Item2;

            var buildListItemPanelInstance = BuildListItemPanel.Instantiate();
            buildListScrollVBox.AddChild(buildListItemPanelInstance);

            var itemNameLabel = (Label)buildListItemPanelInstance.FindChild("Item Name");
            var itemCostLabel = (Label)buildListItemPanelInstance.FindChild("Item Cost");
            itemNameLabel.Text = name;
            itemCostLabel.Text = cost.ToString();
        }
    }

    public void HideOwnedCityView()
    {
        OwnedCityViewControl.Hide();
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
