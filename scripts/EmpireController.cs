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

	private List<CityController> cities = new();
	
	private long? ownerPeerId = null;
	private UIController uiController = null!;
	private BaseUnit? selectedUnit;
	private TileController? selectedTile;
	private Vector2I? hoveredTile;
	private bool hasSelection;
	private bool isFrozen;

	public override void _Ready()
	{
		uiController = UIController.Instance;
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!IsPlayerEmpire) return;
		if (isFrozen) return;

		if (inputEvent is InputEventMouseButton mouseButtonEvent)
		{
			HandleMouseButtonEvent(mouseButtonEvent);
			return;
		}

		if (inputEvent is InputEventMouseMotion mouseMotionEvent)
		{
			HandleMouseMotionEvent(mouseMotionEvent);
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
			else if (selectedUnit is not null && !unit.GetOwnerEmpire().IsPlayerEmpire)
			{
				// Try attacking clicked unit with selected unit

				// If the target unit dies, it immediately disappears from the world, so letting logic
				// fall through to the tile check should move the attacking unit to the victim
				// unit's position. If the unit doesn't die, the tile check fails below.

				selectedUnit.RequestAttackUnit(unit.TilePosition);
			}
			else
			{
				// No unit selected or selecting another own unit
				HandleOwnUnitSelection(unit, mouseTilePosition);
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
				uiController.ShowOwnedCityView(cityController);
			}
			else
			{
				uiController.HideOwnedCityView();
			}

			hasSelection = true;
			selectedUnit = null;
			selectedTile = tileController;
			UpdateTileSelection(mouseTilePosition);
		}
	}

	private void HandleMouseMotionEvent(InputEventMouseMotion motionEvent)
	{
		var mouseWorldPosition = GetViewport().GetCamera2D().GetGlobalMousePosition();
		var mouseTilePosition = TileGrid.WorldToTilePosition(mouseWorldPosition);

		if (TileGrid.IsTileInBounds(mouseTilePosition))
		{
			if (selectedUnit is not null && mouseTilePosition != hoveredTile)
			{
				var pathTiles = selectedUnit.GetPathToTargetTile(mouseTilePosition);
				var pathIndicatorOffset = (Vector2)TileGrid.TilePixelSize / 2;

				for (var i = 0; i < pathTiles.Length; i++)
				{
					var tilePos = (Vector2I)pathTiles[i];
					var tileWorldPos = TileGrid.TileToWorldPosition(tilePos);
					pathTiles[i] = tileWorldPos + pathIndicatorOffset;
				}

				uiController.SetUnitMovementPathPoints(pathTiles);
			}

			hoveredTile = mouseTilePosition;
		}
		else
		{
			hoveredTile = null;
		}
	}

	private void HandleOwnUnitSelection(BaseUnit unit, Vector2I mouseTilePosition)
	{
		Deselect();

		selectedTile = null;
		selectedUnit = unit;
		hasSelection = true;
		UpdateTileSelection(mouseTilePosition);
		uiController.HideOwnedCityView();

		if (!unit.GetOwnerEmpire().IsPlayerEmpire) return;

		uiController.ShowUnitMovementPathLine();

		var tileCosts = unit.GetReachableTilesWithCosts();

		if (tileCosts.ContainsKey(unit.TilePosition))
		{
			tileCosts.Remove(unit.TilePosition);
		}

		uiController.ShowReachableTileIndicators(tileCosts.Keys);
	}

	private void Deselect()
	{
		selectedUnit = null;
		selectedTile = null;
		hasSelection = false;
		UpdateTileSelection(null);
		uiController.HideOwnedCityView();
		uiController.HideReachableTileIndicators();
		uiController.HideUnitMovementPathLine();
	}

	private void UpdateTileSelection(Vector2I? tilePosition)
	{
		if (!hasSelection || tilePosition is null)
		{
			uiController.HideSelectedTileIndicator();
			return;
		}

		uiController.ShowSelectedTileIndicator((Vector2I)tilePosition);
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
			uiController.ShowLoseOverlay();
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
			TotalCoinIncome += targetCity.CoinsGenerated;
		}

		if (GetAliveEmpireCount(GetTree().Root) == 1)
		{
			FreezeAllEmpires(GetTree().Root);

			if (IsPlayerEmpire)
			{
				uiController.ShowWinOverlay();
			}
			else
			{
				uiController.ShowLoseOverlay();
			}
		}

		if (Multiplayer.IsServer())
		{
			GameOrchestrator.Instance.SyncAllEmpireCoins();
		}
	}

	[Rpc(CallLocal = true)]
	private void SyncSetCoinState(int newCoinBalance, int newCoinIncome)
	{
		Coins = newCoinBalance;
		TotalCoinIncome = newCoinIncome;

		if (IsPlayerEmpire)
		{
			UpdateCoinsLabel();
		}
	}

	private void UpdateCoinsLabel()
	{
		if (!IsPlayerEmpire) return;
		uiController.SetCoinBalanceText(Coins, TotalCoinIncome);
	}

	public void InitializeEmpire(long ownerPeerId, string empireUid, Color empireColor, bool isPlayerEmpire = false)
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

	public bool HasCitiesRemaining()
	{
		return cities.Count > 0;
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
