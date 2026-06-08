using System;
using System.Collections.Generic;
using Godot;

#nullable enable
public partial class EmpireController : Node2D
{
	public string EmpireUid = null!;
	public bool IsPlayerEmpire;
	public bool HasCursorSelection;
	public Color EmpirePrimaryColor;
	public int Coins { get; private set; }
	public int TotalCoinsDelta { get; private set; }

	private List<CityController> cities = new();
	
	private PackedScene tileSelectionScene => (PackedScene)GD.Load("res://scenes/TileSelection.tscn");
	private Sprite2D? tileSelectionNode;
	private BaseUnit? selectedUnit;
	private TileController? selectedTile;
	private bool hasSelection;
	private bool isFrozen;

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!IsPlayerEmpire) return;
		if (isFrozen) return;

		if (inputEvent is InputEventMouseButton mouseButtonEvent)
		{
			HandleMouseButtonEvent(mouseButtonEvent);
			return;
		}

		if (inputEvent is not InputEventKey keyEvent) return;

		if (!keyEvent.IsPressed()) return;

		var mouseWorldPosition = GetViewport().GetCamera2D().GetGlobalMousePosition();
		var mouseTilePosition = TileGrid.WorldToTilePosition(mouseWorldPosition);
	}

	private void HandleMouseButtonEvent(InputEventMouseButton mouseButtonEvent)
	{
		if (hasSelection && mouseButtonEvent.ButtonIndex == MouseButton.Right)
		{
			Deselect();

			return;
		}

		if (mouseButtonEvent.ButtonIndex != MouseButton.Left) return;

		if (!mouseButtonEvent.IsPressed()) return;

		var mouseWorldPosition = GetViewport().GetCamera2D().GetGlobalMousePosition();
		var mouseTilePosition = TileGrid.WorldToTilePosition(mouseWorldPosition);

		if (!TileGrid.IsTileInBounds(mouseTilePosition)) return;

		if (EntitySelector.TryGetUnit(mouseTilePosition, out var unit) && unit is not null)
		{
			if (unit == selectedUnit)
			{
				Deselect();
			}
			else
			{
				selectedTile = null;
				selectedUnit = unit;
				hasSelection = true;
				UpdateTileSelection(mouseTilePosition);
				UIController.Instance.HideOwnedCityView();

				return;
			}
		}

		if (EntitySelector.TryGetTile(mouseTilePosition, out var tileController) && tileController is not null)
		{
			if (selectedUnit is not null && selectedUnit.GetOwnerEmpire().IsPlayerEmpire)
			{
				selectedUnit.RequestMoveToTile(mouseTilePosition);
				Deselect();

				return;
			}

			if (tileController == selectedTile)
			{
				Deselect();

				return;
			}

			if (tileController is CityController cityController && cities.Contains(cityController))
			{
				UIController.Instance.ShowOwnedCityView(cityController, GetBuildableItems(), RequestBuildItem);
			}
			else
			{
				UIController.Instance.HideOwnedCityView();
			}

			hasSelection = true;
			selectedUnit = null;
			selectedTile = tileController;
			UpdateTileSelection(mouseTilePosition);
		}
	}

	private void Deselect()
	{
		selectedUnit = null;
		selectedTile = null;
		hasSelection = false;
		UpdateTileSelection(null);
		UIController.Instance.HideOwnedCityView();
	}

	private void UpdateTileSelection(Vector2I? tilePosition)
	{
		if (!hasSelection || tilePosition is null)
		{
			if (tileSelectionNode is not null)
			{
				tileSelectionNode.QueueFree();
				tileSelectionNode = null;
			}

			return;
		}

		if (tileSelectionNode is null)
		{
			tileSelectionNode = (Sprite2D)tileSelectionScene.Instantiate();
			AddChild(tileSelectionNode);
		}

		tileSelectionNode.Position = TileGrid.TileToWorldPosition((Vector2I)tilePosition);
	}

	private void UpdateCoinsLabel()
	{
		if (!IsPlayerEmpire) return;
		UIController.Instance.SetCoinBalanceText(Coins, TotalCoinsDelta);
	}

	[Rpc(CallLocal = true)]
	private void SyncUnitSpawn(int unitTypeEnum, string spawnCityUid)
	{
		var selectedCity = cities.Find(city => city.CityUid == spawnCityUid)!;
		var spawnedUnit = UnitSpawner.Instance.SpawnUnit(
			(BuildController.BuildableItemType)unitTypeEnum,
			ownerEmpire: this);

		if (Multiplayer.IsServer())
		{
			spawnedUnit.RequestMoveToTile(selectedCity.CityTilePosition);
		}
	}

	[Rpc(CallLocal = true)]
	private void SyncReleaseCity(string targetCityUid)
	{
		var targetCity = cities.Find(city => city.CityUid == targetCityUid)!;
		cities.Remove(targetCity);
		DebugUtility.Print($"Sync release city {targetCityUid} from empire {EmpireUid}. Empire {EmpireUid} now has {cities.Count} cities");

		if (IsPlayerEmpire || Multiplayer.IsServer())
		{
			TotalCoinsDelta -= targetCity.CoinsGenerated;
		}

		if (IsPlayerEmpire && cities.Count == 0)
		{
			UIController.Instance.ShowLoseOverlay();
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
		targetCity.SetOwnerEmpire(this, cities[0].BorderColor);
		DebugUtility.Print($"Sync annex city {targetCityUid} for empire {EmpireUid}. Empire {EmpireUid} now has {cities.Count} cities");

		if (IsPlayerEmpire || Multiplayer.IsServer())
		{
			TotalCoinsDelta += targetCity.CoinsGenerated;
		}

		if (GetAliveEmpireCount(GetTree().Root) == 1)
		{
			FreezeAllEmpires(GetTree().Root);

			if (IsPlayerEmpire)
			{
				UIController.Instance.ShowWinOverlay();
			}
			else
			{
				UIController.Instance.ShowLoseOverlay();
			}
		}

		if (Multiplayer.IsServer())
		{
			GameOrchestrator.Instance.SyncAllEmpireCoins();
		}
	}

	public void SetCoinState(int newBalance, int newIncome)
	{
		Coins = newBalance;
		TotalCoinsDelta = newIncome;

		if (IsPlayerEmpire)
		{
			UpdateCoinsLabel();
		}
	}

	public BuildController.BuildableItemType[] GetBuildableItems()
	{
		return
		[
			BuildController.BuildableItemType.Warrior
		];
	}

	[Rpc(mode: MultiplayerApi.RpcMode.AnyPeer)]
	public void RequestBuildItem(int itemTypeEnum, string cityUid)
	{
		if (!Multiplayer.IsServer())
		{
			RpcId(1, nameof(RequestBuildItem), itemTypeEnum, cityUid);
			return;
		}

		// TODO: check if item is unlocked and actually available for building

		var selectedCity = cities.Find(city => city.CityUid == cityUid)!;
		var itemType = (BuildController.BuildableItemType)itemTypeEnum;
		var itemInfo = BuildController.GetBuildableItemInfo(itemType);

		if (itemInfo.Cost > Coins)
		{
			throw new InvalidOperationException($"Item {itemInfo.ItemName} is too expensive to build in empire {Name}");
		}

		Coins -= itemInfo.Cost;
		GameOrchestrator.Instance.SyncAllEmpireCoins();

		if (itemInfo.IsUnit)
		{
			if (EntitySelector.TryGetUnit(selectedCity.CityTilePosition, out var unit) && unit is not null)
			{
				throw new InvalidOperationException($"Can't build unit in an occupied city {selectedCity.Name}");
			}

			Rpc(MethodName.SyncUnitSpawn, (int)itemType, cityUid);

			return; 
		}

		throw new NotImplementedException($"Empire should build a structure ({itemInfo.ItemName}) but it can only build units for now.");
	}

	public void AddNewCityToEmpire(Vector2I tilePosition, string newCityUid)
	{
		var cityController = TileGrid.AddCity(tilePosition);
		cityController.InitializeCity(tilePosition, ownerEmpire: this, newCityUid);
		cities.Add(cityController);
		EntitySelector.SetCity(newCityUid, cityController);

		if (IsPlayerEmpire || Multiplayer.IsServer())
		{
			TotalCoinsDelta += cityController.CoinsGenerated;
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

	public bool HasCitiesRemaining()
	{
		return cities.Count > 0;
	}

	public static EmpireController GetPlayerEmpire(Node rootNode)
	{
		var empires = GodotUtilities.FindNodesOfType<EmpireController>(rootNode);

		foreach (EmpireController empire in empires)
		{
			if (empire.IsPlayerEmpire) return empire;
		}

		throw new ArgumentException("No player empire found from given root node", nameof(rootNode));
	}

	public static int GetAliveEmpireCount(Node rootNode)
	{
		var empires = GodotUtilities.FindNodesOfType<EmpireController>(rootNode);
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
		var empires = GodotUtilities.FindNodesOfType<EmpireController>(rootNode);

		foreach (EmpireController empire in empires)
		{
			empire.isFrozen = true;
		}
	}
}
