using System;
using System.Collections.Generic;
using Godot;

#nullable enable
public partial class TileGrid : Node2D
{
    [Export]
    public required PackedScene TileScene;
    [Export]
    public required PackedScene CityScene;
    [Export]
    public required Texture2D villageTileTexture;
    [Export]
    public required Texture2D townTileTexture;
    public static int TilesWidth = 10;
    public static int TilesHeight = 10;
    public static Vector2I TilePixelSize { get; private set; } = Vector2I.Zero;

    public static TileGrid Instance = null!;

    private static int tileGap = 2;
    private static Vector2I[] playerTileSpawnPoints => [
        new(0, 0),
        new(4, 3),
        new(6, 5),
        new(7, 7),
        new(TilesWidth - 1, TilesHeight - 1)
    ];
    private static int spawnPointsLeft;
    private static AStar2D astar = null!;

    public override void _EnterTree()
    {
        Instance = this;
        spawnPointsLeft = playerTileSpawnPoints.Length;
    }

	public override void _Ready()
	{
        InitializeGeneralTileSize();
        astar = new();

        for (var y = 0; y < TilesHeight; y++)
        {
            for (var x = 0; x < TilesWidth; x++)
            {
                var tilePos = new Vector2I(x, y);
                var tileController = AddMapElement(tilePos, TileScene);
                EntitySelector.AddTile(tilePos, tileController);
                astar.AddPoint(astar.GetPointCount(), tilePos);
            }
        }

        for (var y = 0; y < TilesHeight; y++)
        {
            for (var x = 0; x < TilesWidth; x++)
            {
                var tilePos = new Vector2I(x, y);
                var neighbors = GetTileNeighbors(tilePos);
                var tileId = astar.GetClosestPoint(tilePos);

                foreach (var neighbor in neighbors)
                {
                    var neighborId = astar.GetClosestPoint(neighbor);
                    astar.ConnectPoints(tileId, neighborId);
                }
            }
        }
	}

    private void InitializeGeneralTileSize()
    {
        var tileSceneState = TileScene.GetState();
        var nodeIndex = 0; // First node in the scene
        var propertyIndex = 0; // First propery in the node (should be texture for Sprite2D)
        var tileTextureVariant = tileSceneState.GetNodePropertyValue(nodeIndex, propertyIndex);
        var tileTexture = (Texture2D)tileTextureVariant;
        var textureSize = tileTexture.GetSize();
        TilePixelSize = new Vector2I((int)textureSize.X, (int)textureSize.Y);
    }

    private TileController InstantiateMapElement(PackedScene scene)
    {
        var mapElementInstance = scene.Instantiate();

        if (mapElementInstance is null)
        {
            throw new ArgumentException();
        }

        AddChild(mapElementInstance);

        return (TileController)mapElementInstance;
    }

    private TileController AddMapElement(Vector2I tilePosition, PackedScene scene)
    {
        var mapElementInstance = InstantiateMapElement(scene);
        mapElementInstance.Position = TileToWorldPosition(tilePosition);
        mapElementInstance.TilePosition = tilePosition;

        return mapElementInstance;
    }

    public static bool TryGetPlayerTileSpawnPoint(out Vector2I tileSpawnPoint)
    {
        tileSpawnPoint = Vector2I.Zero;

        if (spawnPointsLeft == 0)
        {
            GD.PrintErr("No player spawn points left!");
            return false;
        }

        tileSpawnPoint = playerTileSpawnPoints[playerTileSpawnPoints.Length - spawnPointsLeft];
        spawnPointsLeft--;

        return true;
    }

    public static CityController AddCity(Vector2I tilePosition)
    {
        var cityNode = Instance.AddMapElement(tilePosition, Instance.CityScene);
        var cityController = (CityController)cityNode;
        EntitySelector.SetTile(tilePosition, cityController);

        return cityController;
    }

    public static Vector2 TileToWorldPosition(Vector2I tilePosition)
    {
        var tileWidth = TilePixelSize.X;
        var tileHeight = TilePixelSize.Y;
        var halfWorldWidth = (TilesWidth * tileWidth + (TilesWidth - 1) * tileGap) / 2;
        var halfWorldHeight = (TilesHeight * tileHeight + (TilesHeight - 1) * tileGap) / 2;
        var xPosition = tilePosition.X * tileWidth + tilePosition.X * tileGap - halfWorldWidth;
        var yPosition = tilePosition.Y * tileHeight + tilePosition.Y * tileGap - halfWorldHeight;

        return new Vector2(xPosition, yPosition);
    }

    public static Vector2I WorldToTilePosition(Vector2 worldPosition)
    {
        var tileWidth = TilePixelSize.X;
        var tileHeight = TilePixelSize.Y;
        var halfWorldWidth = (TilesWidth * tileWidth + (TilesWidth - 1) * tileGap) / 2;
        var halfWorldHeight = (TilesHeight * tileHeight + (TilesHeight - 1) * tileGap) / 2;

        // var xPosition = tilePosX * tileWidth + tilePosX * tileGap - halfWorldWidth;
        // When you factor this: tilePosX * tileWidth + tilePosX * tileGap - halfWorldWidth
        // You get: tilePosX * (tileWidth + tileGap) - halfWorldWidth
        // Therefore the reverse coordinate translation would be:
        // tilePosX = (xPos + halfWorldWidth) / (tileWidth + tileGap)

        var approxTilePosX = (worldPosition.X + halfWorldWidth) / (tileWidth + tileGap);
        var approxTilePosY = (worldPosition.Y + halfWorldHeight) / (tileHeight + tileGap);
        var tilePosX = Mathf.FloorToInt(approxTilePosX);
        var tilePosY = Mathf.FloorToInt(approxTilePosY);

        return new Vector2I(tilePosX, tilePosY);
    }

    public static bool IsTileInBounds(Vector2I tilePosition)
    {
        return EntitySelector.TryGetTile(tilePosition, out var _);
    }

    public static bool IsTileOwned(Vector2I tilePosition)
    {
        if (!EntitySelector.TryGetTile(tilePosition, out var tileController)) return false;
        if (tileController is null) return false;

        return tileController.OwnerCity is not null;
    }

    public static bool TrySetTileOwnerCity(Vector2I tilePosition, CityController? owner)
    {
        return EntitySelector.TrySetTileOwner(tilePosition, owner);
    }

    public static CityController? GetTileOwner(Vector2I tilePosition)
    {
        if (!EntitySelector.TryGetTile(tilePosition, out var tileController)
            || tileController is null)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(tilePosition),
                message: "Given tile position is out of bounds."
            );
        }

        return tileController.OwnerCity;
    }

    public static Vector2I[] GetTileNeighbors(Vector2I tilePosition)
    {
        var neighbors = new List<Vector2I>();
        var directions = new Vector2I[]
        {
            new(0, -1), // up
            new(0, 1),  // down
            new(-1, 0), // left
            new(1, 0),  // right
            new(-1, -1),// top left
            new(1, -1), // top right
            new(-1, 1), // bot left
            new(1, 1)   // bot right
        };

        foreach (var dir in directions)
        {
            var neighborPos = tilePosition + dir;

            if (IsTileInBounds(neighborPos))
            {
                neighbors.Add(neighborPos);
            }
        }

        return neighbors.ToArray();
    }

    public static Vector2[] GetShortestPath(Vector2I fromTile, Vector2I toTile)
    {
        var fromId = astar.GetClosestPoint(fromTile);
        var toId = astar.GetClosestPoint(toTile);
        return astar.GetPointPath(fromId, toId);
    }
}
