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
    private Control ownedFactoryViewControl = null!;
    [Export]
    private Control ownedStoreViewControl = null!;
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
    private Button confirmSupplyTargetSelectionButton = null!;
    [Export]
    private Button cancelSupplyTargetSelectionButton = null!;
    [Export]
    private PackedScene plusMinusIconsScene = null!;
    [Export]
    private Texture2D supplyIndicatorEmpty = null!;
    [Export]
    private Texture2D supplyIndicatorFull = null!;

    private Sprite2D tileSelectionNode = null!;
    private Dictionary<Vector2I, Sprite2D> reachableTileIndicators = new();
	private Line2D unitPathLine = null!;

    private PanelContainer? selectedBuildableItemPanel;
    private BuildController.BuildableItemType? selectedBuildable;
    private HqController? selectedHq;
    private FactoryTileController? selectedFactory;
    private StoreTileController? selectedStore;
    private List<StoreTileController> storesSelectedForSupplyTargets = new();
    private List<StoreTileController> notTargetedStoresWithSuppliesWhenChoosingTargets = new();
    private List<Control> supplyTargetPlusMinusIcons = new();

    public bool IsChoosingSupplyTargets { get; private set; }

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

        var chooseSupplyTargetsButtonHq = (Button)ownedHqViewControl.FindChild("ChooseSupplyTargets");
        chooseSupplyTargetsButtonHq.Pressed += ChooseSupplyTargets;

        var chooseSupplyTargetsButtonFactory = (Button)ownedFactoryViewControl.FindChild("ChooseSupplyTargets");
        chooseSupplyTargetsButtonFactory.Pressed += ChooseSupplyTargets;

        confirmSupplyTargetSelectionButton.Pressed += () =>
        {
            DebugUtility.Print("Confirmed supply target selections. Selected stores:");
            IsChoosingSupplyTargets = false;
            confirmSupplyTargetSelectionButton.Hide();
            cancelSupplyTargetSelectionButton.Hide();

            if (selectedHq is not null && selectedHq.SupplyTarget is not null)
            {
                selectedHq.WithdrawProducts();
            }
            else if (selectedFactory is not null)
            {
                selectedFactory.WithdrawAllProducts();
            }

            foreach (var store in storesSelectedForSupplyTargets)
            {
                DebugUtility.Print($"- {store.TilePosition}, supplies: {store.SuppliesCount}/{store.MaxSupplyCapacity}");

                if (selectedHq is not null)
                {
                    selectedHq.ServeProducts(store, 1);
                }
                else if (selectedFactory is not null)
                {
                    selectedFactory.ServeProducts(store, 1);
                }
                else
                {
                    throw new InvalidOperationException("Neither HQ or factory selected. This should never happen.");
                }
            }

            HideSupplyTargetPlusMinusIcons();

            if (selectedHq is not null)
            {
                selectedHq.OwnerEmpire.RecalculateCustomersAndIncome();
            }
            else if (selectedFactory is not null)
            {
                selectedFactory.OwnerEmpire.RecalculateCustomersAndIncome();
            }
        };

        cancelSupplyTargetSelectionButton.Pressed += () =>
        {
            DebugUtility.Print("Cancelled supply target selections");
            IsChoosingSupplyTargets = false;
            confirmSupplyTargetSelectionButton.Hide();
            cancelSupplyTargetSelectionButton.Hide();
            HideSupplyTargetPlusMinusIcons();
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

    private void ChooseSupplyTargets()
    {
        IsChoosingSupplyTargets = true;
        confirmSupplyTargetSelectionButton.Show();
        cancelSupplyTargetSelectionButton.Show();

        HideSupplyTargetPlusMinusIcons();

        var playerEmpire = EmpireController.GetPeerEmpire(Multiplayer.GetUniqueId());
        var stores = playerEmpire.GetAllStores();

        if (selectedHq is not null && selectedHq.SupplyTarget is not null)
        {
            storesSelectedForSupplyTargets = [selectedHq.SupplyTarget];
        }
        else if (selectedFactory is not null)
        {
            foreach (var store in selectedFactory.SupplyTargetsAndCounts.Keys)
            {
                var storeSelectedCount = selectedFactory.SupplyTargetsAndCounts[store];

                for (var i = 0; i < storeSelectedCount; i++)
                {
                    storesSelectedForSupplyTargets.Add(store);
                }
            }
        }

        foreach (var store in stores)
        {
            var plusMinusIconsControl = (Control)plusMinusIconsScene.Instantiate();
            store.AddChild(plusMinusIconsControl);
            supplyTargetPlusMinusIcons.Add(plusMinusIconsControl);

            var minusButton = (Button)plusMinusIconsControl.FindChild("MinusButton");
            var plusButton = (Button)plusMinusIconsControl.FindChild("PlusButton");

            // Can use lambdas because hiding the buttons works by freeing the nodes,
            // which removes need for unsubscribing from the Pressed event
            plusButton.Pressed += () => AddSupplyTargetSelection(store);
            minusButton.Pressed += () => RemoveSupplyTargetSelection(store);

            var selectedCount = storesSelectedForSupplyTargets.FindAll(m => m == store).Count;

            for (var i = 0; i < store.SuppliesCount; i++)
            {
                if (i < selectedCount) continue;
                notTargetedStoresWithSuppliesWhenChoosingTargets.Add(store);
            }
        }

        ShowStoreSupplyIndicators(stores);
    }

    private void HideSupplyTargetPlusMinusIcons()
    {
        foreach (var oldIcons in supplyTargetPlusMinusIcons)
        {
            var oldMinusButton = (Button)oldIcons.FindChild("MinusButton");
            var oldPlusButton = (Button)oldIcons.FindChild("PlusButton");
            // no need to unsubscribe from Pressed event of plus and minus icons because
            // the nodes are freed
            oldIcons.QueueFree();
        }

        var playerEmpire = EmpireController.GetPeerEmpire(Multiplayer.GetUniqueId());
        var stores = playerEmpire.GetAllStores();

        HideStoreSupplyIndicators(stores);

        supplyTargetPlusMinusIcons.Clear();
        storesSelectedForSupplyTargets.Clear();
        notTargetedStoresWithSuppliesWhenChoosingTargets.Clear();
    }

    private void HideStoreSupplyIndicators(IEnumerable<StoreTileController> stores)
    {
        foreach (var store in stores)
        {
            var supplyIndicatorsParent = store.FindChild("SupplyIndicatorsParent");

            foreach (var child in supplyIndicatorsParent.GetChildren())
            {
                child.QueueFree();
                supplyIndicatorsParent.RemoveChild(child);
            }
        }
    }

    private void ShowStoreSupplyIndicators(IEnumerable<StoreTileController> stores)
    {
        foreach (var store in stores)
        {
            var supplyIndicatorsParent = store.FindChild("SupplyIndicatorsParent");
            var storeSelectedCount = storesSelectedForSupplyTargets.FindAll(m => m == store).Count;
            var notSelectedButSuppliedCount = notTargetedStoresWithSuppliesWhenChoosingTargets.FindAll(m => m == store).Count;

            for (var i = 0; i < store.MaxSupplyCapacity; i++)
            {
                var indicator = new TextureRect();
                Texture2D texture;
                
                if (i < storeSelectedCount + notSelectedButSuppliedCount)
                {
                    texture = supplyIndicatorFull;
                }
                else
                {
                    texture = supplyIndicatorEmpty;
                }

                indicator.Texture = texture;
                supplyIndicatorsParent.AddChild(indicator);
            }
        }
    }

    private void UpdateStoreSupplyIndicators(IEnumerable<StoreTileController>? targetStores = null)
    {
        var playerEmpire = EmpireController.GetPeerEmpire(Multiplayer.GetUniqueId());
        var stores = targetStores is null ? playerEmpire.GetAllStores() : targetStores;
        HideStoreSupplyIndicators(stores);
        ShowStoreSupplyIndicators(stores);
    }

    public void OnEntitySelectionChanged(EmpireController empire)
    {
        HideOwnedHqView();
        HideOwnedFactoryView();
        HideOwnedStoreView();
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
        else if (empire.TryGetSelectedFactory(out var factoryController))
        {
            ShowSelectedTileIndicator(factoryController!.TilePosition);

            if (empire.IsOwnFactorySelected)
            {
                ShowOwnedFactoryView(factoryController);
            }
        }
        else if (empire.TryGetSelectedStore(out var storeController))
        {
            ShowSelectedTileIndicator(storeController!.TilePosition);

            if (empire.IsOwnStoreSelected)
            {
                ShowOwnedStoreView(storeController);
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

        var productsServedLabel = (Label)ownedHqViewControl.FindChild("SuppliesServedLabel");
        productsServedLabel.Text = $"Supplies served: {hqController.ProductsSuppliedCount}";

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

    public void ShowOwnedFactoryView(FactoryTileController factory)
    {
        selectedFactory = factory;
        ownedFactoryViewControl.Show();
        var suppliesGeneratedLabel = (Label)ownedFactoryViewControl.FindChild("SuppliesGeneratedLabel");
        var suppliesServedLabel = (Label)ownedFactoryViewControl.FindChild("SuppliesServedLabel");
        suppliesGeneratedLabel.Text = $"Supplies generated: {factory.ProductGenerationRate}";
        suppliesServedLabel.Text = $"Supplies served: {factory.ProductsSuppliedCount}";
    }

    public void HideOwnedFactoryView()
    {
        selectedFactory = null;
        ownedFactoryViewControl.Hide();
    }

    public void ShowOwnedStoreView(StoreTileController store)
    {
        selectedStore = store;
        ownedStoreViewControl.Show();
        var suppliesLabel = (Label)ownedStoreViewControl.FindChild("SuppliesLabel");
        suppliesLabel.Text = $"Supplies: {store.SuppliesCount}/{store.MaxSupplyCapacity}";
    }

    public void HideOwnedStoreView()
    {
        selectedStore = null;
        ownedStoreViewControl.Hide();
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

    public void AddSupplyTargetSelection(StoreTileController target)
    {
        // If no hq or factory is selected, this function shouldn't even be callable
        var hqSelected = selectedHq is not null;
        var factorySelected = selectedFactory is not null;

        if (!hqSelected && !factorySelected)
        {
            throw new InvalidOperationException("No HQ or factory selected but supply targets are trying to be set");
        }

        var totalSuppliesAvailable = 0;

        if (hqSelected)
        {
            totalSuppliesAvailable = selectedHq!.BaseProductsGenerated;
        }
        else if (factorySelected)
        {
            totalSuppliesAvailable = selectedFactory!.ProductGenerationRate;
        }
        else
        {
            throw new InvalidOperationException("Neither HQ or factory selected. This should never happen.");
        }

        if (storesSelectedForSupplyTargets.Count >= totalSuppliesAvailable)
        {
            DebugUtility.Print("No more supplies");
            return;
        }

        var timesAlreadySelected = storesSelectedForSupplyTargets.FindAll(m => m == target).Count;
        var timesSuppliedButNotSelected = notTargetedStoresWithSuppliesWhenChoosingTargets.FindAll(m => m == target).Count;

        if (timesAlreadySelected + timesSuppliedButNotSelected + 1 > target.MaxSupplyCapacity)
        {
            DebugUtility.Print("Target can't fit more supplies");
            return;
        }

        storesSelectedForSupplyTargets.Add(target);
        UpdateStoreSupplyIndicators([target]);
    }

    public void RemoveSupplyTargetSelection(StoreTileController target)
    {
        if (!storesSelectedForSupplyTargets.Contains(target)) return;

        storesSelectedForSupplyTargets.Remove(target);
        UpdateStoreSupplyIndicators([target]);
    }
}
