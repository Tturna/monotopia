using System;
using System.Diagnostics;
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
    [Export]
    public required Control WinOverlayControl;
    [Export]
    public required Control LoseOverlayControl;

    public static UIController Instance = null!;

    private PanelContainer? selectedBuildableItemPanel;
    private BuildController.BuildableItemType? selectedBuildable;
    private CityController? selectedCity;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        var buildButton = (Button)OwnedCityViewControl.FindChild("Build Button");
        buildButton.Pressed += () =>
        {
            if (selectedBuildable is null) return;
            if (selectedCity is null) return;

            var buildable = (BuildController.BuildableItemType)selectedBuildable;
            var playerEmpire = EmpireController.GetPlayerEmpire(GetTree().Root);
            playerEmpire.RequestBuildItem((int)buildable, selectedCity.CityUid);
        };
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton mouseButtonEvent) return;
        if (!mouseButtonEvent.IsPressed()) return;
        if (mouseButtonEvent.ButtonIndex != MouseButton.Right) return;
        if (selectedBuildable is null) return;

        DeselectBuildableItem();
    }

    private void SetSelectedBuildable(PanelContainer buildableItemPanel, BuildController.BuildableItemType itemType)
    {
        if (selectedBuildable is null)
        {
            selectedBuildable = itemType;
            selectedBuildableItemPanel = buildableItemPanel;
            var highlightColorRect = (ColorRect)selectedBuildableItemPanel.FindChild("Highlight Color");
            highlightColorRect.Show();

            var buildButton = (Button)OwnedCityViewControl.FindChild("Build Button");
            buildButton.Disabled = false;
        }
        else if (selectedBuildable != itemType)
        {
            Debug.Assert(selectedBuildableItemPanel is not null);

            var highlightColorRect = (ColorRect)selectedBuildableItemPanel.FindChild("Highlight Color");
            highlightColorRect.Hide();

            selectedBuildable = itemType;
            selectedBuildableItemPanel = buildableItemPanel;

            highlightColorRect = (ColorRect)selectedBuildableItemPanel.FindChild("Highlight Color");
            highlightColorRect.Show();

            var buildButton = (Button)OwnedCityViewControl.FindChild("Build Button");
            buildButton.Disabled = false;
        }
        else // clicked on already selected buildable
        {
            Debug.Assert(selectedBuildableItemPanel is not null);

            DeselectBuildableItem();
        }
    }

    private void DeselectBuildableItem()
    {
        if (selectedBuildableItemPanel is null) return;

        var highlightColorRect = (ColorRect)selectedBuildableItemPanel.FindChild("Highlight Color");
        highlightColorRect.Hide();
        selectedBuildable = null;
        selectedBuildableItemPanel = null;
        var buildButton = (Button)OwnedCityViewControl.FindChild("Build Button");
        buildButton.Disabled = true;
    }

    public void ShowOwnedCityView(
        CityController city,
        BuildController.BuildableItemType[] buildableTypes,
        Action<int, string> buildCallback)
    {
        if (city is null)
        {
            throw new ArgumentException("Can't show city info for null city", nameof(city));
        }

        // Reset all state so that switching directly between city views doesn't keep
        // old view state.
        HideOwnedCityView();

        selectedCity = city;
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

        foreach (var buildableType in buildableTypes)
        {
            var buildableItemInfo = BuildController.GetBuildableItemInfo(buildableType);
            var name = buildableItemInfo.ItemName;
            var cost = buildableItemInfo.Cost;

            var buildListItemPanelInstance = (PanelContainer)BuildListItemPanel.Instantiate();
            buildListScrollVBox.AddChild(buildListItemPanelInstance);

            var itemNameLabel = (Label)buildListItemPanelInstance.FindChild("Item Name");
            var itemCostLabel = (Label)buildListItemPanelInstance.FindChild("Item Cost");
            itemNameLabel.Text = name;
            itemCostLabel.Text = cost.ToString();

            var selectButton = (Button)buildListItemPanelInstance.FindChild("Select Button");
            selectButton.Pressed += () => SetSelectedBuildable(buildListItemPanelInstance, buildableType);
        }
    }

    public void HideOwnedCityView()
    {
        selectedCity = null;
        selectedBuildable = null;
        selectedBuildableItemPanel = null;
        var buildButton = (Button)OwnedCityViewControl.FindChild("Build Button");
        buildButton.Disabled = true;
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

    public void ShowWinOverlay()
    {
        WinOverlayControl.Show();
    }

    public void ShowLoseOverlay()
    {
        LoseOverlayControl.Show();
    }

    public void SetTurnEnded(bool state)
    {
        if (state)
        {
            EndTurnButton.Text = "Turn Ended";
            EndTurnButton.Disabled = true;
        }
        else
        {
            EndTurnButton.Text = "End Turn";
            EndTurnButton.Disabled = false;
        }
    }
}
