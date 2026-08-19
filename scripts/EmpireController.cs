using System;
using System.Collections.Generic;
using Godot;

#nullable enable
public partial class EmpireController : Node2D
{
	public string EmpireUid { get; private set; } = null!;
	public bool IsPlayerEmpire { get; private set; }
	public Color EmpirePrimaryColor { get; private set; }
    public int ProductGenerationRate { get; private set; }
    public int CustomerCount { get; private set; }
	public int Coins { get; private set; }
	public int TotalCoinIncome { get; private set; }
	public bool IsFrozen { get; private set; }
	public bool IsOwnHqSelected { get; private set; }
	public bool IsOwnFactorySelected { get; private set; }
	public bool IsOwnUnitSelected { get; private set; }
	public TileController? SelectedTile { get; private set; }
	public bool HasSelection { get; private set; }
    public bool IsHqCreated { get; private set; }

    private HqController? hq;
	private List<FactoryTileController> factories = new();
	private List<StoreTileController> stores = new();
	
	private long? ownerPeerId = null;
	private BaseUnit? selectedUnit;
	private Dictionary<Vector2I, int>? reachableTileCostMap;
    private InfluenceSystem influenceSystem = null!;

	public delegate void SelectionChangedHandler(EmpireController empire);
    public delegate void ProductGenerationRateUpdatedHandler(int rate);
    public delegate void CustomersUpdatedHandler(int amount);
	public delegate void CoinsUpdatedHandler(int balance, int income);
	public delegate void UnitMovementPathUpdatedHandler(Vector2[] pathTiles);
	public event SelectionChangedHandler? SelectionChanged;
    public event ProductGenerationRateUpdatedHandler? ProductGenerationRateUpdated;
    public event CustomersUpdatedHandler? CustomersUpdated;
	public event CoinsUpdatedHandler? CoinsUpdated;
	public event UnitMovementPathUpdatedHandler? UnitMovementPathUpdated;

    public override void _Ready()
    {
        influenceSystem = GodotUtilities.FindNodeOfType<InfluenceSystem>(GetTree().Root);
    }

	public bool IsActivePlayerEmpire()
	{
		return (IsPlayerEmpire && !IsFrozen);
	}

	public void HandleUnitSelection(BaseUnit unit)
	{
		Deselect();

		if (unit.GetOwnerEmpire().IsPlayerEmpire)
		{
			HandleOwnUnitSelection(unit);
		}
		else
		{
			HandleForeignUnitSelection(unit);
		}
	}

	private void HandleOwnUnitSelection(BaseUnit unit)
	{
		IsOwnUnitSelected = true;
		selectedUnit = unit;
		HasSelection = true;

		if (unit.GetOwnerEmpire().IsPlayerEmpire)
		{
			reachableTileCostMap = unit.GetReachableTilesWithCosts();

			if (reachableTileCostMap.ContainsKey(unit.TilePosition))
			{
				reachableTileCostMap.Remove(unit.TilePosition);
			}
		}

		SelectionChanged?.Invoke(this);
	}

	private void HandleForeignUnitSelection(BaseUnit unit)
	{
		selectedUnit = unit;
		HasSelection = true;
		SelectionChanged?.Invoke(this);
	}

	public void HandleTileSelection(TileController tile)
	{
		if (selectedUnit is not null && selectedUnit.GetOwnerEmpire().IsPlayerEmpire)
		{
			selectedUnit.RequestMoveToTile(tile.TilePosition);
			Deselect();
			return;
		}

		Deselect();

		if (tile == SelectedTile) return;

		IsOwnHqSelected = tile is HqController hqController && hqController.OwnerEmpire == this;
        IsOwnFactorySelected = tile is FactoryTileController factoryController && factoryController.OwnerEmpire == this;
		HasSelection = true;
		SelectedTile = tile;
		SelectionChanged?.Invoke(this);
	}

	public void UpdateSelectedOwnUnitPathLine(Vector2I mouseTilePosition)
	{
		if (selectedUnit is null) return;

		var pathTiles = selectedUnit.GetPathToTargetTile(mouseTilePosition);
		var pathIndicatorOffset = (Vector2)TileGrid.TilePixelSize / 2;

		for (var i = 0; i < pathTiles.Length; i++)
		{
			var tilePos = (Vector2I)pathTiles[i];
			var tileWorldPos = TileGrid.TileToWorldPosition(tilePos);
			pathTiles[i] = tileWorldPos + pathIndicatorOffset;
		}

		UnitMovementPathUpdated?.Invoke(pathTiles);
	}

	public void Deselect()
	{
		if (!HasSelection) return;

		IsOwnHqSelected = false;
        IsOwnFactorySelected = false;
		IsOwnUnitSelected = false;
		selectedUnit = null;
		SelectedTile = null;
		reachableTileCostMap = null;
		HasSelection = false;
		SelectionChanged?.Invoke(this);
	}

	[Rpc(CallLocal = true)]
	private void SyncSetCoinState(int newCoinBalance, int newCoinIncome)
	{
		Coins = newCoinBalance;
		TotalCoinIncome = newCoinIncome;

		if (IsPlayerEmpire)
		{
			CoinsUpdated?.Invoke(Coins, TotalCoinIncome);
		}
	}

	public void InitializeEmpire(
		long ownerPeerId,
		string empireUid,
		Color empireColor,
		bool isPlayerEmpire = false)
	{
		this.ownerPeerId = ownerPeerId;
		EmpireUid = empireUid;
		EmpirePrimaryColor = empireColor;
		IsPlayerEmpire = isPlayerEmpire;
	}

	public long GetOwnerPeerId()
	{
		if (ownerPeerId is null)
		{
			throw new InvalidOperationException("Owner peer ID is null");
		}

		return (long)ownerPeerId;
	}

	public void AddHqToEmpire(Vector2I tilePosition, string newHqUid)
	{
        if (IsHqCreated)
        {
            throw new InvalidOperationException("HQ already created");
        }

		var hqController = TileGrid.AddHq(tilePosition);
        hq = hqController;
		hqController.InitializeHq(tilePosition, ownerEmpire: this, newHqUid);
		EntitySelector.SetHq(newHqUid, hqController);
        IsHqCreated = true;
        ProductGenerationRate += hqController.BaseProductsGenerated;
        TotalCoinIncome += hqController.CoinsGenerated;

        if (IsPlayerEmpire)
        {
            ProductGenerationRateUpdated?.Invoke(ProductGenerationRate);
            CoinsUpdated?.Invoke(Coins, TotalCoinIncome);
        }
	}

    public void AddNewStoreToEmpire(Vector2I tilePosition, string newStoreUid)
    {
        var storeController = TileGrid.AddStore(tilePosition);
        storeController.InitializeStore(tilePosition, newStoreUid);
        stores.Add(storeController);
    }

    public void AddNewFactoryToEmpire(Vector2I tilePosition, string newFactoryUid)
    {
        var factoryController = TileGrid.AddFactory(tilePosition);
        factoryController.InitializeFactory(tilePosition, this, newFactoryUid);
        ProductGenerationRate += factoryController.ProductGenerationRate;
        ProductGenerationRateUpdated?.Invoke(ProductGenerationRate);
    }

	public void RequestUpdateCoins(int change)
	{
		RequestSetCoinState(Coins + change, TotalCoinIncome);
	}

	[Rpc(mode: MultiplayerApi.RpcMode.AnyPeer)]
	public void RequestSetCoinState(int newCoinBalance, int newCoinIncome)
	{
		if (!Multiplayer.IsServer())
		{
			RpcId(1, MethodName.RequestSetCoinState, newCoinBalance, newCoinIncome);
			return;
		}

		// Coin data should only be updated for each player's own empire
		Coins = newCoinBalance;
		TotalCoinIncome = newCoinIncome;
		RpcId(GetOwnerPeerId(), MethodName.SyncSetCoinState, newCoinBalance, newCoinIncome);
	}

	public bool TryGetSelectedHq(out HqController? hqController)
	{
		hqController = null;

		if (SelectedTile is not null && SelectedTile is HqController selectedHqController)
		{
			hqController = selectedHqController;
			return true;
		}

		return false;
	}

    public bool TryGetSelectedFactory(out FactoryTileController? factoryController)
    {
        factoryController = null;

        if (SelectedTile is not null && SelectedTile is FactoryTileController selectedFactoryController)
        {
            factoryController = selectedFactoryController;
            return true;
        }

        return false;
    }

	public bool TryGetSelectedUnit(out BaseUnit? unit)
	{
		unit = null;

		if (selectedUnit is not null)
		{
			unit = selectedUnit;
			return true;
		}

		return false;
	}

	public bool TryGetReachableTileCostMap(out Dictionary<Vector2I, int> costMap)
	{
		if (IsOwnUnitSelected && reachableTileCostMap is not null && reachableTileCostMap.Count > 0)
		{
			costMap = reachableTileCostMap;
			return true;
		}

		costMap = new();
		return false;
	}

    public void RecalculateCustomersAndIncome()
    {
        var topInfluenceTiles = influenceSystem.GetPeerTopInfluenceTiles(GetOwnerPeerId());
        var totalServedCustomers = 0;

        foreach (var tilePosition in topInfluenceTiles)
        {
            if (!EntitySelector.TryGetTile(tilePosition, out var tileController))
            {
                throw new InvalidOperationException($"No tile at given position {tilePosition}");
            }

            if (tileController is not ResidentialTileController residentialController) continue;

            var clientsToServe = residentialController.Residents;

            while (clientsToServe > 0)
            {
                StoreTileController? closestAvailableStore = null;
                var closestStoreDistance = float.MaxValue;

                foreach (var store in stores)
                {
                    if (closestAvailableStore is null && store.AvailableServiceCapacity > 0)
                    {
                        closestAvailableStore = store;
                        continue;
                    }

                    var storeDistance = tilePosition.DistanceTo(store.TilePosition);

                    if (storeDistance < closestStoreDistance && store.AvailableServiceCapacity > 0)
                    {
                        closestAvailableStore = store;
                        closestStoreDistance = storeDistance;
                    }
                }

                if (closestAvailableStore is null) break;

                var servedCustomers = closestAvailableStore.ServeCustomers(clientsToServe);

                if (servedCustomers <= 0)
                {
                    throw new InvalidOperationException($"Clients to serve still: {clientsToServe}, closest available store: {closestAvailableStore.TilePosition}, store available service capacity: {closestAvailableStore.AvailableServiceCapacity}, served customers: {servedCustomers}");
                }

                clientsToServe -= servedCustomers;
                totalServedCustomers += servedCustomers;
            }
        }

        CustomerCount = totalServedCustomers;
        var customerIncome = CustomerCount;
        var totalIncome = customerIncome;

        // factor in other than customer income
        if (TryGetHq(out var empireHq) && empireHq is not null)
        {
            totalIncome += empireHq.CoinsGenerated;
        }

        TotalCoinIncome = totalIncome;

        if (IsPlayerEmpire)
        {
            CustomersUpdated?.Invoke(CustomerCount);
            CoinsUpdated?.Invoke(Coins, TotalCoinIncome);
        }
    }

	public static void FreezeAllEmpires(Node rootNode)
	{
		var empiresDict = EntitySelector.GetEmpiresDict();
		var empires = empiresDict.Values;

		foreach (EmpireController empire in empires)
		{
			empire.IsFrozen = true;
		}
	}

    public static EmpireController GetPeerEmpire(long peerId)
    {
        var empiresDict = EntitySelector.GetEmpiresDict();

        foreach (var empire in empiresDict.Values)
        {
            if (empire.GetOwnerPeerId() == peerId)
            {
                return empire;
            }
        }

        throw new InvalidOperationException("No empire found for given peer ID");
    }

    public bool TryGetHq(out HqController? empireHq)
    {
        empireHq = null;
        if (hq is null) return false;
        empireHq = hq;
        return true;
    }

    public List<StoreTileController> GetAllStores()
    {
        return stores;
    }
}
