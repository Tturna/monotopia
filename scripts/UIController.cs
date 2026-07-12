using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

#nullable enable
public partial class UIController : Node2D
{
    [Export]
    private Control ownedCityViewControl = null!;
    [Export]
	private Label coinsLabel = null!;
    [Export]
    private Label turnCountLabel = null!;
    [Export]
    private Label turnTimerLabel = null!;
    [Export]
    private Button endTurnButton = null!;
    [Export]
    private PackedScene buildListItemPanel = null!;
    [Export]
    private Control winOverlayControl = null!;
    [Export]
    private Control loseOverlayControl = null!;
    [Export]
	private PackedScene tileSelectionScene = null!;
    [Export]
	private PackedScene reachableTileIndicatorScene = null!;

    private Sprite2D tileSelectionNode = null!;
    private Dictionary<Vector2I, Sprite2D> reachableTileIndicators = new();
	private Line2D unitPathLine = null!;

    private PanelContainer? selectedBuildableItemPanel;
    private BuildController.BuildableItemType? selectedBuildable;
    private CityController? selectedCity;

    public delegate void EndTurnButtonPressedHandler(UIController uiController);
    public event EndTurnButtonPressedHandler? EndTurnButtonPressed;

    public override void _Ready()
    {
		tileSelectionNode = (Sprite2D)tileSelectionScene.Instantiate();
		AddChild(tileSelectionNode);
		tileSelectionNode.Hide();

		unitPathLine = new Line2D();
		unitPathLine.Width = 1;
		AddChild(unitPathLine);
		unitPathLine.Hide();

        var buildButton = (Button)ownedCityViewControl.FindChild("Build Button");
        buildButton.Pressed += () =>
        {
            if (selectedBuildable is null) return;
            if (selectedCity is null) return;

            var buildable = (BuildController.BuildableItemType)selectedBuildable;
            BuildController.Instance.RequestBuildItem((int)buildable, selectedCity.CityUid);
        };

        endTurnButton.Pressed += () => EndTurnButtonPressed?.Invoke(this);
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

            var buildButton = (Button)ownedCityViewControl.FindChild("Build Button");
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

            var buildButton = (Button)ownedCityViewControl.FindChild("Build Button");
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
        var buildButton = (Button)ownedCityViewControl.FindChild("Build Button");
        buildButton.Disabled = true;
    }

    public void OnEntitySelectionChanged(EmpireController empire)
    {
        HideOwnedCityView();
        HideReachableTileIndicators();
        HideSelectedTileIndicator();
        HideUnitMovementPathLine();

        if (!empire.HasSelection) return;

        if (empire.TryGetSelectedCity(out var city))
        {
            ShowSelectedTileIndicator(city!.TilePosition);

            if (empire.IsOwnCitySelected)
            {
                ShowOwnedCityView(city!);
            }
        }
        else if (empire.TryGetSelectedUnit(out var unit))
        {
            ShowSelectedTileIndicator(unit!.TilePosition);

            if (empire.IsOwnUnitSelected && empire.TryGetReachableTileCostMap(out var costMap))
            {
                ShowUnitMovementPathLine();
                ShowReachableTileIndicators(costMap.Keys);
            }
        }
    }

    public void ShowOwnedCityView(CityController city)
    {
        if (city is null)
        {
            throw new ArgumentException("Can't show city info for null city", nameof(city));
        }

        // Reset all state so that switching directly between city views doesn't keep
        // old view state.
        HideOwnedCityView();

        selectedCity = city;
        ownedCityViewControl.Show();

        var cityNameLabel = (Label)ownedCityViewControl.FindChild("CityNameLabel");
        cityNameLabel.Text = city!.CityName;

        var coinsGeneratedLabel = (Label)ownedCityViewControl.FindChild("CoinsGeneratedLabel");
        var prefix = city.CoinsGenerated switch
        {
            > 0 => "+",
            < 0 => "-",
            _ => ""
        };
        var coinsText = Math.Abs(city.CoinsGenerated).ToString();

        coinsGeneratedLabel.Text = $"Coins generated: {prefix}{coinsText}";

        var buildListScrollVBox = (VBoxContainer)ownedCityViewControl.FindChild("Build List Scroll VBox");

        while (buildListScrollVBox.GetChildCount() > 0)
        {
            var child = buildListScrollVBox.GetChild(0);
            child.QueueFree();
            buildListScrollVBox.RemoveChild(child);
        }

        var buildableTypes = BuildController.GetBuildableItems();

        foreach (var buildableType in buildableTypes)
        {
            var buildableItemInfo = BuildController.GetBuildableItemInfo(buildableType);
            var name = buildableItemInfo.ItemName;
            var cost = buildableItemInfo.Cost;

            var buildListItemPanelInstance = (PanelContainer)buildListItemPanel.Instantiate();
            buildListScrollVBox.AddChild(buildListItemPanelInstance);

            var itemNameLabel = (Label)buildListItemPanelInstance.FindChild("Item Name");
            var itemCostLabel = (Label)buildListItemPanelInstance.FindChild("Item Cost");
            var itemIcon = (TextureRect)buildListItemPanelInstance.FindChild("Item Icon");
            itemNameLabel.Text = name;
            itemCostLabel.Text = cost.ToString();
            itemIcon.Texture = buildableItemInfo.Icon;

            var selectButton = (Button)buildListItemPanelInstance.FindChild("Select Button");
            selectButton.Pressed += () => SetSelectedBuildable(buildListItemPanelInstance, buildableType);
        }
    }

    public void HideOwnedCityView()
    {
        selectedCity = null;
        selectedBuildable = null;
        selectedBuildableItemPanel = null;
        var buildButton = (Button)ownedCityViewControl.FindChild("Build Button");
        buildButton.Disabled = true;
        ownedCityViewControl.Hide();
    }

    public void SetCoinBalanceText(int coins, int delta)
    {
		var balanceText = coins.ToString();
		var deltaText = delta.ToString();
		coinsLabel.Text = $"{balanceText} (+{deltaText})";
    }

    public void OnTurnStarted(int turn)
    {
        SetTurnEnded(false);
        SetTurnCountText(turn);
    }

    public void SetTurnTimerText(float secondsLeft)
    {
        var timeSpan = TimeSpan.FromSeconds(secondsLeft);
        turnTimerLabel.Text = timeSpan.ToString(@"mm\:ss");
    }

    public void SetTurnCountText(int turn)
    {
        turnCountLabel.Text = $"Turn {turn}";
    }

    public void ShowGameEndedOverlay(bool didPlayerWin)
    {
        if (didPlayerWin)
        {
            winOverlayControl.Show();
        }
        else
        {
            loseOverlayControl.Show();
        }
    }

    public void SetTurnEnded(bool state)
    {
        if (state)
        {
            endTurnButton.Text = "Turn Ended";
            endTurnButton.Disabled = true;
        }
        else
        {
            endTurnButton.Text = "End Turn";
            endTurnButton.Disabled = false;
        }
    }

    public void ShowSelectedTileIndicator(Vector2I tilePosition)
    {
		tileSelectionNode.Show();
		tileSelectionNode.Position = TileGrid.TileToWorldPosition(tilePosition);
    }

    public void HideSelectedTileIndicator()
    {
        tileSelectionNode.Hide();
    }

	public void ShowReachableTileIndicators(IEnumerable<Vector2I> reachableTiles)
	{
		foreach (var tilePosition in reachableTiles)
		{
			if (reachableTileIndicators.ContainsKey(tilePosition))
			{
				reachableTileIndicators[tilePosition].Show();
			}
			else
			{
				var indicator = (Sprite2D)reachableTileIndicatorScene.Instantiate();
				AddChild(indicator);
				reachableTileIndicators.Add(tilePosition, indicator);
			}

			reachableTileIndicators[tilePosition].Position = TileGrid.TileToWorldPosition(tilePosition);
		}
	}

	public void HideReachableTileIndicators()
	{
		foreach (var (_, indicator) in reachableTileIndicators)
		{
			indicator.Hide();
		}
	}

    public void ShowUnitMovementPathLine() => unitPathLine.Show();
    public void HideUnitMovementPathLine() => unitPathLine.Hide();
    public void SetUnitMovementPathPoints(Vector2[] points)
    {
        unitPathLine.Points = points;
    }
}
