using System;
using System.Collections.Generic;
using Godot;

#nullable enable
public partial class EmpireController : Node2D
{
	public string EmpireUid { get; private set; } = null!;
	public bool IsPlayerEmpire { get; private set; }
	public Color EmpirePrimaryColor { get; private set; }
	public int Coins { get; private set; }
	public int TotalCoinIncome { get; private set; }
	public bool IsFrozen { get; private set; }
	public bool IsOwnCitySelected { get; private set; }
	public bool IsOwnUnitSelected { get; private set; }
	public TileController? SelectedTile { get; private set; }
	public bool HasSelection { get; private set; }

	private List<CityController> cities = new();
	
	private long? ownerPeerId = null;
	private BaseUnit? selectedUnit;
	private Dictionary<Vector2I, int>? reachableTileCostMap;

	public delegate void SelectionChangedHandler(EmpireController empire);
	public delegate void CityAnnexedHandler();
	public delegate void CoinsUpdatedHandler(int balance, int income);
	public delegate void GameEndedHandler(bool isWinner);
	public delegate void UnitMovementPathUpdatedHandler(Vector2[] pathTiles);
	public event SelectionChangedHandler? SelectionChanged;
	public event CityAnnexedHandler? CityAnnexed;
	public event CoinsUpdatedHandler? CoinsUpdated;
	public event GameEndedHandler? GameEnded;
	public event UnitMovementPathUpdatedHandler? UnitMovementPathUpdated;

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

		IsOwnCitySelected = tile is CityController cityController && cities.Contains(cityController);
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

		IsOwnCitySelected = false;
		IsOwnUnitSelected = false;
		selectedUnit = null;
		SelectedTile = null;
		reachableTileCostMap = null;
		HasSelection = false;
		SelectionChanged?.Invoke(this);
	}

	[Rpc(CallLocal = true)]
	private void SyncReleaseCity(string targetCityUid)
	{
		var targetCity = cities.Find(city => city.CityUid == targetCityUid)!;
		cities.Remove(targetCity);
		DebugUtility.Print($"Sync release city {targetCityUid} from empire {EmpireUid}. Empire {EmpireUid} now has {cities.Count} cities");

		if (IsPlayerEmpire || Multiplayer.IsServer())
		{
			TotalCoinIncome -= targetCity.CoinsGenerated;
		}

		if (IsPlayerEmpire && cities.Count == 0)
		{
			GameEnded?.Invoke(isWinner: false);
		}
	}

	[Rpc(CallLocal = true)]
	private void SyncAnnexCity(string targetCityUid)
	{
		if (!EntitySelector.TryGetCity(targetCityUid, out var targetCity) || targetCity is null)
		{
			throw new ArgumentException($"No city with given UID {targetCityUid} in empire {EmpireUid}. {EmpireUid} has {cities.Count} cities.");
		}

		cities.Add(targetCity);
		targetCity.SetOwnerEmpire(this);
		DebugUtility.Print($"Sync annex city {targetCityUid} for empire {EmpireUid}. Empire {EmpireUid} now has {cities.Count} cities");

		if (IsPlayerEmpire || Multiplayer.IsServer())
		{
			TotalCoinIncome += targetCity.CoinsGenerated;
		}

		if (GetAliveEmpireCount(GetTree().Root) == 1)
		{
			FreezeAllEmpires(GetTree().Root);

			if (IsPlayerEmpire)
			{
				GameEnded?.Invoke(isWinner: true);
			}
			else
			{
				GameEnded?.Invoke(isWinner: false);
			}
		}

		if (Multiplayer.IsServer())
		{
			CityAnnexed?.Invoke();
		}
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

	public void AddNewCityToEmpire(Vector2I tilePosition, string newCityUid)
	{
		var cityController = TileGrid.AddCity(tilePosition);
		cityController.InitializeCity(tilePosition, ownerEmpire: this, newCityUid);
		cities.Add(cityController);
		EntitySelector.SetCity(newCityUid, cityController);

		if (IsPlayerEmpire || Multiplayer.IsServer())
		{
			TotalCoinIncome += cityController.CoinsGenerated;
		}

		DebugUtility.Print($"Empire {EmpireUid} now has {cities.Count} cities");
	}

	[Rpc(mode: MultiplayerApi.RpcMode.AnyPeer)]
	public void RequestAnnexCity(string targetCityUid)
	{
		if (!Multiplayer.IsServer())
		{
			RpcId(1, MethodName.RequestAnnexCity, targetCityUid);
			return;
		}

		DebugUtility.Print($"Annexing city {targetCityUid} for empire {EmpireUid}");
		Rpc(MethodName.SyncAnnexCity, targetCityUid);
	}

	[Rpc(mode: MultiplayerApi.RpcMode.AnyPeer)]
	public void RequestReleaseCity(string targetCityUid)
	{
		if (!Multiplayer.IsServer())
		{
			RpcId(1, MethodName.RequestReleaseCity, targetCityUid);
			return;
		}

		DebugUtility.Print($"Releasing city {targetCityUid} from empire {EmpireUid}");
		Rpc(MethodName.SyncReleaseCity, targetCityUid);
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

	public bool TryGetSelectedCity(out CityController? city)
	{
		city = null;

		if (SelectedTile is not null && SelectedTile is CityController cityController)
		{
			city = cityController;
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

	public bool HasCitiesRemaining()
	{
		return cities.Count > 0;
	}

	public static int GetAliveEmpireCount(Node rootNode)
	{
		var empiresDict = EntitySelector.GetEmpiresDict();
		var empires = empiresDict.Values;
		var aliveCount = 0;

		foreach (EmpireController empire in empires)
		{
			if (empire.HasCitiesRemaining())
			{
				aliveCount++;
			}
		}

		return aliveCount;
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
}
