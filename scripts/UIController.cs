using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

#nullable enable
public partial class UIController : Node2D
{
    [Export]
    private Control ownedHqViewControl = null!;
    [Export]
    private Control selectedUnitViewControl = null!;
    [Export]
	private Label productsLabel = null!;
    [Export]
	private Label customersLabel = null!;
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
    [Export]
    private Node notificationsParent = null!;
    [Export]
    private PackedScene notificationToastScene = null!;

    private Sprite2D tileSelectionNode = null!;
    private Dictionary<Vector2I, Sprite2D> reachableTileIndicators = new();
	private Line2D unitPathLine = null!;
    private Queue<NotificationToast> activeNotificationToasts = new();
    private NotificationToast? visibleNotificationToast;

    private PanelContainer? selectedBuildableItemPanel;
    private BuildController.BuildableItemType? selectedBuildable;
    private HqController? selectedHq;

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

        var buildButton = (Button)ownedHqViewControl.FindChild("Build Button");
        buildButton.Pressed += () =>
        {
            if (selectedBuildable is null) return;
            if (selectedHq is null) return;

            var buildable = (BuildController.BuildableItemType)selectedBuildable;
            BuildController.Instance.RequestBuildItem((int)buildable, selectedHq.HqUid);
        };

        endTurnButton.Pressed += () => EndTurnButtonPressed?.Invoke(this);
    }

    public override void _Process(double delta)
    {
        if (activeNotificationToasts.Count == 0 && visibleNotificationToast is null) return;

        if (visibleNotificationToast is null)
        {
            var toast = activeNotificationToasts.Peek();
            visibleNotificationToast = toast;
            visibleNotificationToast.ToastControl.Show();
        }

        visibleNotificationToast.DurationLeft -= (float)delta;

        if (visibleNotificationToast.DurationLeft <= 0)
        {
            visibleNotificationToast.ToastControl.Hide();
            visibleNotificationToast.ToastControl.QueueFree();
            visibleNotificationToast = null;
            activeNotificationToasts.Dequeue();
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton mouseButtonEvent) return;
        if (!mouseButtonEvent.IsPressed()) return;

        if (mouseButtonEvent.ButtonIndex == MouseButton.Left)
        {
            ShowNotificationToast("Test toast", "Mouse clicked!", 1);
        }

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

            var buildButton = (Button)ownedHqViewControl.FindChild("Build Button");
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

            var buildButton = (Button)ownedHqViewControl.FindChild("Build Button");
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
        var buildButton = (Button)ownedHqViewControl.FindChild("Build Button");
        buildButton.Disabled = true;
    }

    public void OnEntitySelectionChanged(EmpireController empire)
    {
        HideOwnedHqView();
        HideSelectedUnitView();
        HideReachableTileIndicators();
        HideSelectedTileIndicator();
        HideUnitMovementPathLine();

        if (!empire.HasSelection) return;

        if (empire.TryGetSelectedHq(out var hqController))
        {
            ShowSelectedTileIndicator(hqController!.TilePosition);

            if (empire.IsOwnHqSelected)
            {
                ShowOwnedHqView(hqController!);
            }
        }
        else if (empire.TryGetSelectedUnit(out var unit))
        {
            ShowSelectedTileIndicator(unit!.TilePosition);
            ShowSelectedUnitView(unit);

            if (empire.IsOwnUnitSelected && empire.TryGetReachableTileCostMap(out var costMap))
            {
                ShowUnitMovementPathLine();
                ShowReachableTileIndicators(costMap.Keys);
            }
        }
    }

    public void ShowOwnedHqView(HqController hqController)
    {
        if (hqController is null)
        {
            throw new ArgumentException("Can't show HQ info for null HQ", nameof(hqController));
        }

        HideOwnedHqView();

        selectedHq = hqController;
        ownedHqViewControl.Show();

        var hqNameLabel = (Label)ownedHqViewControl.FindChild("HqNameLabel");
        hqNameLabel.Text = "HQ name here";

        var coinsGeneratedLabel = (Label)ownedHqViewControl.FindChild("CoinsGeneratedLabel");
        var prefix = hqController.CoinsGenerated switch
        {
            > 0 => "+",
            < 0 => "-",
            _ => ""
        };
        var coinsText = Math.Abs(hqController.CoinsGenerated).ToString();

        coinsGeneratedLabel.Text = $"Coins generated: {prefix}{coinsText}";

        var buildListScrollVBox = (VBoxContainer)ownedHqViewControl.FindChild("Build List Scroll VBox");

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

    public void HideOwnedHqView()
    {
        selectedHq = null;
        selectedBuildable = null;
        selectedBuildableItemPanel = null;
        var buildButton = (Button)ownedHqViewControl.FindChild("Build Button");
        buildButton.Disabled = true;
        ownedHqViewControl.Hide();
    }

    public void ShowSelectedUnitView(BaseUnit unit)
    {
        selectedUnitViewControl.Show();
        var nameLabel = (Label)selectedUnitViewControl.FindChild("UnitNameLabel");
        nameLabel.Text = unit.GetUnitName();

        if (unit.GetOwnerEmpire().IsPlayerEmpire)
        {
            var unitActions = unit.GetCombinedUnitActions();
            var actionButtonContainer = selectedUnitViewControl.FindChild("Action Button Container");

            foreach (var unitAction in unitActions)
            {
                var button = new Button();
                button.Text = unitAction.ActionName;
                actionButtonContainer.AddChild(button);

                if (unitAction.IsSingleUse)
                {
                    button.Pressed += () => {
                        unitAction.ActionCallback();
                        actionButtonContainer.RemoveChild(button);
                        button.QueueFree();
                    };
                }
                else
                {
                    button.Pressed += unitAction.ActionCallback;
                }
            }
        }
    }

    public void HideSelectedUnitView()
    {
        selectedUnitViewControl.Hide();

        var actionButtonContainer = selectedUnitViewControl.FindChild("Action Button Container");

        for (var i = 0; i < actionButtonContainer.GetChildCount(); i++)
        {
            actionButtonContainer.GetChild(i).QueueFree();
        }
    }

    public void SetProductGenerationRate(int productGenerationRate)
    {
        productsLabel.Text = productGenerationRate.ToString();
    }

    public void SetCustomerCountText(int customerCount)
    {
        customersLabel.Text = customerCount.ToString();
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

    public void ShowNotificationToast(string title, string body, float duration)
    {
        var toastControl = notificationToastScene.Instantiate<Control>();
        var toastTitle = (Label)toastControl.FindChild("Toast Title");
        var toastBody = (Label)toastControl.FindChild("Toast Body");
        toastTitle.Text = title;
        toastBody.Text = body;
        toastControl.Hide();
        notificationsParent.AddChild(toastControl);
        activeNotificationToasts.Enqueue(new (toastControl, duration));
    }
}
