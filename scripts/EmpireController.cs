using System.Collections.Generic;
using Godot;

public partial class EmpireController : Node2D
{
    private List<CityController> cities = new();

    public override void _Ready()
    {
        if (!TileGrid.TryGetVillageTileSpawnPoint(out var spawnPointX, out var spawnPointY))
        {
            GD.PrintErr("Couldn't get spawn point for empire!");
            return;
        }

        TileGrid.AddCity(spawnPointX, spawnPointY);
    }
}
