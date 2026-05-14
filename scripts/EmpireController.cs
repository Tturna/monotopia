using System;
using System.Collections.Generic;
using Godot;

#nullable enable
public partial class EmpireController : Node2D
{
	[Export]
	public bool IsPlayerEmpire;
	[Export]
	public bool Frozen;

	public bool HasCursorSelection;

	private List<CityController> cities = new();
	private int coins;
	private int totalCoinsDelta;
	
	private PackedScene tileSelectionScene => (PackedScene)GD.Load("res://scenes/TileSelection.tscn");
	private Sprite2D? tileSelectionNode;
	private BaseUnit? selectedUnit;
	private TileController? selectedTile;
	private bool hasSelection;

	public override void _Ready()
	{
		TurnSystem.Instance.TurnStarted += OnTurnStarted;

		if (!TileGrid.TryGetVillageTileSpawnPoint(out var spawnPoint))
		{
			GD.PrintErr("Couldn't get spawn point for empire!");
			return;
		}

		AddCityToEmpire(spawnPoint);
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!IsPlayerEmpire) return;
		if (Frozen) return;

		if (inputEvent is InputEventMouseButton mouseButtonEvent)
		{
			HandleMouseButtonEvent(mouseButtonEvent);
			return;
		}

		if (inputEvent is not InputEventKey keyEvent) return;

		if (!keyEvent.IsPressed()) return;

		var mouseWorldPosition = GetViewport().GetCamera2D().GetGlobalMousePosition();
		var mouseTilePosition = TileGrid.WorldToTilePosition(mouseWorldPosition);

		if (keyEvent.Keycode == Key.C)
		{
			AddCityToEmpire(mouseTilePosition);
		}
		else if (keyEvent.Keycode == Key.U)
		{
			var warrior = UnitSpawner.Instance.SpawnWarrior(ownerEmpire: this);
			warrior.SetUnitTilePosition(mouseTilePosition);
		}
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
			if (selectedUnit is not null)
			{
				selectedUnit.TryMoveToTile(mouseTilePosition);
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
				UIController.Instance.ShowOwnedCityView(cityController, GetBuildableItems(), BuildItem);
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

	private void UpdateTileSelection(Vector2Int? tilePosition)
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

		tileSelectionNode.Position = TileGrid.TileToWorldPosition(tilePosition);
	}

	private void UpdateCoinsLabel()
	{
		if (!IsPlayerEmpire) return;
		UIController.Instance.SetCoinBalanceText(coins, totalCoinsDelta);
	}

	private void UpdateTotalCoinDelta()
	{
		if (!IsPlayerEmpire) return;

		var total = 0;

		foreach (var city in cities)
		{
			total += city.CoinsGenerated;
		}

		totalCoinsDelta = total;
	}

	private void AddCityToEmpire(Vector2Int tilePosition)
	{
		var cityController = TileGrid.AddCity(tilePosition);
		cityController.InitializeCity(tilePosition, ownerEmpire: this);
		cities.Add(cityController);
		totalCoinsDelta += cityController.CoinsGenerated;
		UpdateTotalCoinDelta();
		UpdateCoinsLabel();
	}

	private void OnTurnStarted()
	{
		UpdateTotalCoinDelta();
		coins += totalCoinsDelta;
		UpdateCoinsLabel();
	}

	public BuildableItem[] GetBuildableItems()
	{
		return
		[
			new BuildableItem {
				ItemName = UnitSpawner.Units.Warrior.ToString(),
				Cost = 2,
				BuildableUnitType = UnitSpawner.Units.Warrior
			}
		];
	}

	public void BuildItem(BuildableItem item, CityController selectedCity)
	{
		// TODO: check if item is unlocked and actually available for building

		if (item.Cost > coins)
		{
			throw new InvalidOperationException($"Item {item.ItemName} is too expensive to build in empire {Name}");
		}

		if (item.BuildableUnitType is not null)
		{
			if (EntitySelector.TryGetUnit(selectedCity.CityTilePosition, out var unit) && unit is not null)
			{
				throw new InvalidOperationException($"Can't build unit in an occupied city {selectedCity.Name}");
			}

			coins -= item.Cost;
			UpdateCoinsLabel();

			var spawnedUnit = UnitSpawner.Instance.SpawnUnit(
				(UnitSpawner.Units)item.BuildableUnitType,
				ownerEmpire: this);
			spawnedUnit.SetUnitTilePosition(selectedCity.CityTilePosition);

			return; 
		}

		throw new NotImplementedException($"Empire should build a structure ({item.ItemName}) but it can only build units for now.");
	}

	public void AnnexCity(CityController targetCity)
	{
		cities.Add(targetCity);
		targetCity.SetOwnerEmpire(this, cities[0].BorderColor);

		if (GetAliveEmpireCount(GetTree().Root) == 1)
		{
			UIController.Instance.ShowWinOverlay();
			FreezeAllEmpires(GetTree().Root);
		}
	}

	public void ReleaseCity(CityController targetCity)
	{
		cities.Remove(targetCity);
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
			empire.Frozen = true;
		}
	}
}
