using System.Collections.Generic;
using Godot;

public partial class EmpireController : Node2D
{
    private Label coinsLabel => (Label)GetNode("%Coins Label");

    private List<CityController> cities = new();
    private int coins;
    private int totalCoinsDelta;

    public override void _Ready()
    {
        TurnSystem.Instance.TurnStarted += OnTurnStarted;

        if (!TileGrid.TryGetVillageTileSpawnPoint(out var spawnPointX, out var spawnPointY))
        {
            GD.PrintErr("Couldn't get spawn point for empire!");
            return;
        }

        AddCityToEmpire(spawnPointX, spawnPointY);
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey keyEvent) return;

        if (!keyEvent.IsPressed()) return;

        if (keyEvent.Keycode != Key.C) return;

        var mouseWorldPosition = GetViewport().GetCamera2D().GetGlobalMousePosition();
        var mouseTilePosition = TileGrid.WorldToTilePosition(mouseWorldPosition);

        AddCityToEmpire(mouseTilePosition.tilePosX, mouseTilePosition.tilePosY);
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

    private void AddCityToEmpire(int spawnPointX, int spawnPointY)
    {
        var cityController = TileGrid.AddCity(spawnPointX, spawnPointY);
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
