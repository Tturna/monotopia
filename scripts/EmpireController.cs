using System.Collections.Generic;
using Godot;

#nullable enable
public partial class EmpireController : Node2D
{
	[Export]
	public bool Frozen;

	public bool HasCursorSelection;

	private Label coinsLabel => (Label)GetNode("%Coins Label");

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
			var rootNode = GetTree().Root;
			var warrior = UnitSpawner.Instance.SpawnWarrior(ownerEmpire: this);
			warrior.SetUnitTilePosition(mouseTilePosition);
		}
	}

	private void HandleMouseButtonEvent(InputEventMouseButton mouseButtonEvent)
	{
		if (hasSelection && mouseButtonEvent.ButtonIndex == MouseButton.Right)
		{
			hasSelection = false;
			selectedUnit = null;
			selectedTile = null;
			UpdateTileSelection(null);

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

			if (tileController is CityController cityController)
			{
                // Open city view
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
		var balanceText = coins.ToString();
		var deltaText = totalCoinsDelta.ToString();
		coinsLabel.Text = $"{balanceText} (+{deltaText})";
	}

	private void UpdateTotalCoinDelta()
	{
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
		cityController.InitializeCity(tilePosition);
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
}
