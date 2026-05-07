using System;
using System.Collections.Generic;
using Godot;

public static class CityBorderBuilder
{
    private struct Edge
    {
        public Vector2 A;
        public Vector2 B;

        public Edge(Vector2 a, Vector2 b)
        {
            // Normalize here
            if (Compare(a, b) <= 0)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        static int Compare(Vector2 p1, Vector2 p2)
        {
            if (p1.X < p2.X) return -1;
            if (p1.X > p2.X) return 1;

            if (p1.Y < p2.Y) return -1;
            if (p1.Y > p2.Y) return 1;

            return 0;
        }

        public override bool Equals(object obj)
        {
            return obj is Edge e && A == e.A && B == e.B;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(A, B);
        }

        public override string ToString()
        {
            return $"{{ A: {A}, B: {B}}}";
        }
    }

    private static Edge[] TilePositionsToTileBorderEdges(Vector2Int[] tilePositions)
    {
        // 1. Store each 4 edge of each tile
        // 2. Remove duplicate edges
        // 3. Only border edges remain (only outside border edges if given tile positions
        // don't enclose a tile hole, which they shouldn't if they're the controlled tile
        // positions of a single city)

        Dictionary<Edge, int> tileEdges = new();

        foreach (var tilePosition in tilePositions)
        {
            var topEdge = new Edge
            (
                tilePosition,
                tilePosition + new Vector2(1f, 0f)
            );
            var rightEdge = new Edge
            (
                tilePosition + new Vector2(1f, 0f),
                tilePosition + new Vector2(1f, 1f)
            );
            var botEdge = new Edge
            (
                tilePosition + new Vector2(1f, 1f),
                tilePosition + new Vector2(0f, 1f)
            );
            var leftEdge = new Edge
            (
                tilePosition + new Vector2(0f, 1f),
                tilePosition
            );

            if (tileEdges.ContainsKey(topEdge)) tileEdges[topEdge]++;
            else tileEdges.Add(topEdge, 1);

            if (tileEdges.ContainsKey(rightEdge)) tileEdges[rightEdge]++;
            else tileEdges.Add(rightEdge, 1);

            if (tileEdges.ContainsKey(botEdge)) tileEdges[botEdge]++;
            else tileEdges.Add(botEdge, 1);

            if (tileEdges.ContainsKey(leftEdge)) tileEdges[leftEdge]++;
            else tileEdges.Add(leftEdge, 1);
        }

        List<Edge> borderEdges = new();

        foreach (var edgeEntry in tileEdges)
        {
            var edge = edgeEntry.Key;
            var edgeCount = edgeEntry.Value;

            if (edgeCount > 1) continue;

            borderEdges.Add(edge);
        }

        return borderEdges.ToArray();
    }

    private static Vector2Int[] BorderEdgesToPolygonTileVertices(Edge[] unorderedBorderEdges)
    {
        Dictionary<Vector2, List<Edge>> edgesTouchingVertices = new();

        foreach (var borderEdge in unorderedBorderEdges)
        {
            if (edgesTouchingVertices.ContainsKey(borderEdge.A))
            {
                edgesTouchingVertices[borderEdge.A].Add(borderEdge);
            }
            else
            {
                edgesTouchingVertices.Add(borderEdge.A, [borderEdge]);
            }

            if (edgesTouchingVertices.ContainsKey(borderEdge.B))
            {
                edgesTouchingVertices[borderEdge.B].Add(borderEdge);
            }
            else
            {
                edgesTouchingVertices.Add(borderEdge.B, [borderEdge]);
            }
        }

        List<Vector2Int> orderedBorderTileVertices = new();

        var currentEdge = unorderedBorderEdges[0];
        var currentVertex = currentEdge.A;
        var startingVertex = currentVertex;
        var goingToB = true;
        var failsafe = 100;

        while (true)
        {
            failsafe--;

            if (failsafe <= 5)
            {
                GD.Print($"""
                    =====
                    Failsafe close to firing! Printing everything:
                    Current vertex: {currentVertex},
                    Current edge: {currentEdge},
                    Starting vertex: {startingVertex},
                    Going towards B: {goingToB}
                """);
            }

            if (failsafe <= 0)
            {
                throw new Exception(message: "Failsafe!");
            }

            orderedBorderTileVertices.Add(Vector2Int.FromVector2(currentVertex));

            var nextVertex = goingToB ? currentEdge.B : currentEdge.A;

            if (nextVertex == currentVertex)
            {
                goingToB = !goingToB;
                nextVertex = goingToB ? currentEdge.B : currentEdge.A;
            }

            if (nextVertex == startingVertex) break;

            var nextEdges = edgesTouchingVertices[nextVertex];
            nextEdges.Remove(currentEdge);

            if (nextEdges.Count == 0) break;

            currentEdge = nextEdges[0];
            currentVertex = nextVertex;
        }

        return orderedBorderTileVertices.ToArray();
    }

    public static Vector2[] Polygon2DFromTilePositions(Vector2Int[] tilePositions)
    {
        var borderEdges = TilePositionsToTileBorderEdges(tilePositions);
        var polygonTileVertices = BorderEdgesToPolygonTileVertices(borderEdges);
        var polygonVertices = new Vector2[polygonTileVertices.Length];

        for (var i = 0; i < polygonTileVertices.Length; i++)
        {
            polygonVertices[i] = TileGrid.TileToWorldPosition(polygonTileVertices[i]);
        }

        return polygonVertices;
    }

#nullable enable
    public static bool TryGetNextBorderExpansionTile(
        Vector2Int[] controlledTiles,
        Vector2Int cityTilePosition,
        CityController owner,
        out Vector2Int tilePosition,
        Vector2Int? expansionDirection = null)
    {
        tilePosition = Vector2Int.Zero;
        HashSet<Vector2Int> availableNeighborTileSet = new();

        // Check tiles in expanding "rings" around the city center to find the first
        // layer with uncontrolled tiles. Later the results can be filtered based on
        // player's target expansion direction. This way the city grows somewhat evenly
        // and shouldn't form holes in its own borders naturally.

        var maxRingOffset = 5; // The city can't grow more than 5 tiles away.
        Vector2Int[] cardinalDirections = [
            new(1, 0), new(-1, 0), new(0, -1), new(0, 1)
        ];

        for (var ringOffsetFromCenter = 1; ringOffsetFromCenter <= maxRingOffset; ringOffsetFromCenter++)
        {
            for (var y = -ringOffsetFromCenter; y <= ringOffsetFromCenter; y++)
            {
                for (var x = -ringOffsetFromCenter; x <= ringOffsetFromCenter; x++)
                {
                    // Skip checking tiles enclosed by the current ring
                    if (Math.Abs(x) != ringOffsetFromCenter && Math.Abs(y) != ringOffsetFromCenter)
                    {
                        continue;
                    }

                    var offset = new Vector2Int(x, y);
                    var testTilePosition = cityTilePosition + offset;

                    if (!TileGrid.IsTileInBounds(testTilePosition)) continue;
                    if (TileGrid.IsTileOwned(testTilePosition)) continue;

                    // Ensure the tile we're about to accept as available is actually
                    // next to a controlled tile.
                    var hasNeighbor = false;

                    foreach (var dir in cardinalDirections)
                    {
                        if (TileGrid.TryGetTileOwner(testTilePosition + dir, out var tileOwner))
                        {
                            if (tileOwner is not null && tileOwner == owner)
                            {
                                hasNeighbor = true;
                                break;
                            }
                        }
                    }

                    if (!hasNeighbor) continue;

                    availableNeighborTileSet.Add(testTilePosition);
                }
            }

            if (availableNeighborTileSet.Count > 0) break;
        }

        if (availableNeighborTileSet.Count == 0) return false;

        var availableNeighborTiles = new Vector2Int[availableNeighborTileSet.Count];
        availableNeighborTileSet.CopyTo(availableNeighborTiles);

        if (expansionDirection is null)
        {
            var rng = Random.Shared.Next(availableNeighborTileSet.Count);
            tilePosition = availableNeighborTiles[rng];

            return true;
        }

        List<Vector2Int> availableTilePositionsInRightDirection = new();

        foreach (var availableTilePos in availableNeighborTiles)
        {
            var diffFromCity = availableTilePos - cityTilePosition;
            var dirFromCity = ((Vector2)diffFromCity).Normalized();
            var dot = dirFromCity.Dot(Vector2.Up);

            if (dot >= 0.5f && expansionDirection == Vector2Int.Up)
            {
                availableTilePositionsInRightDirection.Add(availableTilePos);
            }
            else if (dot <= -0.5 && expansionDirection == Vector2Int.Down)
            {
                availableTilePositionsInRightDirection.Add(availableTilePos);
            }
            else if (dot < 0.5f && dot > -0.5f && expansionDirection.Y == 0)
            {
                if (dirFromCity.X > 0 && expansionDirection.X > 0 ||
                    dirFromCity.X < 0 && expansionDirection.X < 0)
                {
                    availableTilePositionsInRightDirection.Add(availableTilePos);
                }
            }
        }

        if (availableTilePositionsInRightDirection.Count > 0)
        {
            var rng = Random.Shared.Next(availableTilePositionsInRightDirection.Count);
            tilePosition = availableTilePositionsInRightDirection[rng];
        }
        else
        {
            var rng = Random.Shared.Next(availableNeighborTileSet.Count);
            tilePosition = availableNeighborTiles[rng];
        }

        return true;
    }
}
