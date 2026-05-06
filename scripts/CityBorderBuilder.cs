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

        while (true)
        {
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

    public static Polygon2D Polygon2DFromTilePositions(Vector2Int[] tilePositions)
    {
        var borderEdges = TilePositionsToTileBorderEdges(tilePositions);
        var polygonTileVertices = BorderEdgesToPolygonTileVertices(borderEdges);
        var polygonVertices = new Vector2[polygonTileVertices.Length];

        for (var i = 0; i < polygonTileVertices.Length; i++)
        {
            polygonVertices[i] = TileGrid.TileToWorldPosition(polygonTileVertices[i]);
        }

        var polygon = new Polygon2D();
        polygon.Polygon = polygonVertices;

        return polygon;
    }
}
