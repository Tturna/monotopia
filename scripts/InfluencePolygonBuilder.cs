using System;
using System.Collections.Generic;
using Godot;

public partial class InfluencePolygonBuilder : Node
{
    private static Dictionary<long, HashSet<Vector2I>> peerInfluenceTiles = new();
    private static Dictionary<Vector2I, Polygon2D> tilePolygons = new();

    [Rpc(CallLocal = true)]
    private void SetPeerInfluenceTiles(long peer, Vector2[] tilePositions, Color peerColor)
    {
        if (!peerInfluenceTiles.ContainsKey(peer))
        {
            peerInfluenceTiles.Add(peer, new());
        }

        HashSet<Vector2I> newTiles = new();

        foreach (var tilePosition in tilePositions)
        {
            newTiles.Add(new Vector2I((int)tilePosition.X, (int)tilePosition.Y));
        }

        var tilesNoLongerControlled = peerInfluenceTiles[peer];
        tilesNoLongerControlled.ExceptWith(newTiles);

        foreach (var tile in tilesNoLongerControlled)
        {
            if (!tilePolygons.ContainsKey(tile))
            {
                throw new InvalidOperationException($"No polygon for tile ({tile}) someone controlled. How is this possible?");
            }

            // TODO: Pool polygons

            // Remove tile unless someone else controls it at this point. This can happen if
            // influence tiles are updated and the control of a tile changes from one player
            // to another in one frame. If the new tile controller is updated first, this
            // would take away their polygon unless we check for that here.

            var otherControllerExists = false;

            foreach (var (peerId, influenceTilesSet) in peerInfluenceTiles)
            {
                if (peerId == peer) continue;

                if (influenceTilesSet.Contains(tile))
                {
                    otherControllerExists = true;
                    break;
                }
            }

            if (!otherControllerExists)
            {
                tilePolygons[tile].QueueFree();
                tilePolygons.Remove(tile);
            }
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

    public void SyncPeerInfluenceTiles(long peer, Vector2[] tilePositions, Color peerColor)
    {
        if (!Multiplayer.IsServer())
        {
            throw new InvalidOperationException("Tried to sync peer influence tiles directly from a client.");
        }

        Rpc(MethodName.SetPeerInfluenceTiles, peer, tilePositions, peerColor);
    }
}
