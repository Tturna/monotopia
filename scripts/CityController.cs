using System;
using System.Collections.Generic;
using Godot;

#nullable enable
public partial class CityController : TileController
{
    [Export]
    public bool Freeze;

    public string CityName { get; private set; } = null!;
    public int CoinsGenerated { get; private set; } = 2;
    public Vector2Int CityTilePosition { get; private set; } = null!;

    private List<Vector2Int> controlledTilePositions = new();
    private Vector2Int? borderExpansionDirectionFocus = null;
    private Polygon2D borderPolygon = new();

    private void OnTurnStarted()
    {
        if (Freeze) return;

        var nextTileAvailable = CityBorderBuilder.TryGetNextBorderExpansionTile(
            controlledTilePositions.ToArray(),
            CityTilePosition,
            owner: this,
            out var nextTilePosition,
            borderExpansionDirectionFocus);

        if (nextTileAvailable)
        {
            TakeControlOfTile(nextTilePosition);
            UpdateBorderPolygon();
        }
    }

    private void TakeControlOfTile(Vector2Int tilePosition)
    {
        if (tilePosition == CityTilePosition) return;
        if (!TileGrid.IsTileInBounds(tilePosition)) return;
        if (TileGrid.IsTileOwned(tilePosition)) return;

        controlledTilePositions.Add(tilePosition);
        TileGrid.TrySetTileOwnerCity(tilePosition, this);
    }

    private void UpdateBorderPolygon()
    {
        var polygonVertices = CityBorderBuilder.Polygon2DFromTilePositions(controlledTilePositions.ToArray());
        borderPolygon.Polygon = polygonVertices;
    }

    public void InitializeCity(Vector2Int tilePosition)
    {
        CityName = $"City {tilePosition}";

        TurnSystem.Instance.TurnStarted += OnTurnStarted;

        CityTilePosition = tilePosition;
        controlledTilePositions.Add(CityTilePosition);
        TileGrid.TrySetTileOwnerCity(CityTilePosition, this);

        // Grow one tile outwards if possible, taking up a max of 3x3 tiles.
        for (var y = -1; y < 2; y++)
        {
            for (var x = -1; x < 2; x++)
            {
                var controlledTilePosition = CityTilePosition + new Vector2Int(x, y);
                TakeControlOfTile(controlledTilePosition);
            }
        }

        TakeControlOfTile(CityTilePosition + new Vector2Int(0, -2));
        TakeControlOfTile(CityTilePosition + new Vector2Int(0, 2));
        TakeControlOfTile(CityTilePosition + new Vector2Int(2, 0));
        TakeControlOfTile(CityTilePosition + new Vector2Int(-2, 0));

        UpdateBorderPolygon();
        AddChild(borderPolygon);
        // Set global pos to 0 to prevent offsetting border when the border
        // child node inherits the city position from the parent node.
        borderPolygon.GlobalPosition = Vector2.Zero;

        var r = (float)Random.Shared.NextDouble();
        var g = (float)Random.Shared.NextDouble();
        var b = (float)Random.Shared.NextDouble();
        var a = 0.65f;
        borderPolygon.Color = new Color(r, g, b, a);
    }
}
