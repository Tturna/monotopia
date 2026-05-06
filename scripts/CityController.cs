using System;
using System.Collections.Generic;
using Godot;

public partial class CityController : Node2D
{
    public int CoinsGenerated = 2;

    private Vector2Int cityTilePosition;
    private List<Vector2Int> controlledTilePositions = new();

    private void GrowCityBorder(Vector2Int tilePosition)
    {
        if (tilePosition == cityTilePosition) return;
        if (TileGrid.IsTileOwned(tilePosition)) return;

        controlledTilePositions.Add(tilePosition);
        TileGrid.SetTileOwner(tilePosition, this);
    }

    public void InitializeCity(Vector2Int tilePosition)
    {
        cityTilePosition = tilePosition;
        controlledTilePositions.Add(cityTilePosition);

        // Grow one tile outwards if possible, taking up a max of 3x3 tiles.
        for (var y = -1; y < 2; y++)
        {
            for (var x = -1; x < 2; x++)
            {
                var controlledTilePosition = cityTilePosition + new Vector2Int(x, y);
                GrowCityBorder(controlledTilePosition);
            }
        }

        GrowCityBorder(cityTilePosition + new Vector2Int(0, -2));
        GrowCityBorder(cityTilePosition + new Vector2Int(0, 2));
        GrowCityBorder(cityTilePosition + new Vector2Int(2, 0));
        GrowCityBorder(cityTilePosition + new Vector2Int(-2, 0));

        var borderPolygon = CityBorderBuilder.Polygon2DFromTilePositions(controlledTilePositions.ToArray());
        AddChild(borderPolygon);
        // Set global pos to 0 to prevent offsetting border when the border
        // child node inherits the city position from the parent node.
        borderPolygon.GlobalPosition = Vector2.Zero;

        var r = (float)Random.Shared.NextDouble();
        var g = (float)Random.Shared.NextDouble();
        var b = (float)Random.Shared.NextDouble();
        borderPolygon.Color = new Color(r, g, b, 1f);
    }
}
