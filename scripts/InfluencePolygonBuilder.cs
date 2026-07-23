using System;
using System.Collections.Generic;
using Godot;

public partial class InfluencePolygonBuilder : Node
{
    private static Dictionary<long, HashSet<Vector2I>> peerInfluenceTiles = new();
    private static Dictionary<Vector2I, Polygon2D> tilePolygons = new();

    public void SetPeerInfluenceTiles(long peer, IEnumerable<Vector2I> tilePositions, Color peerColor)
    {
        if (!peerInfluenceTiles.ContainsKey(peer))
        {
            peerInfluenceTiles.Add(peer, new());
        }

        HashSet<Vector2I> newTiles = new(tilePositions);
        var tilesNoLongerControlled = peerInfluenceTiles[peer];
        tilesNoLongerControlled.ExceptWith(newTiles);

        foreach (var tile in tilesNoLongerControlled)
        {
            if (!tilePolygons.ContainsKey(tile))
            {
                throw new InvalidOperationException($"No polygon for tile ({tile}) someone controlled. How is this possible?");
            }

            // TODO: Pool polygons
            tilePolygons[tile].QueueFree();
            tilePolygons.Remove(tile);
        }

        peerInfluenceTiles[peer] = newTiles;

        foreach (var tile in newTiles)
        {
            if (tilePolygons.TryGetValue(tile, out var polygon))
            {
                polygon.Color = peerColor;
            }
            else
            {
                var newPolygon = new Polygon2D();
                AddChild(newPolygon);
                newPolygon.GlobalPosition = Vector2.Zero;
                newPolygon.Color = peerColor;

                Vector2[] worldVertices = [
                    TileGrid.TileToWorldPosition(tile),
                    TileGrid.TileToWorldPosition(tile + Vector2I.Right),
                    TileGrid.TileToWorldPosition(tile + Vector2I.One),
                    TileGrid.TileToWorldPosition(tile + Vector2I.Down)
                ];

                newPolygon.Polygon = worldVertices;
                tilePolygons.Add(tile, newPolygon);
            }
        }
    }
}
