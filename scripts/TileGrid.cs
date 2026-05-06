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
    public static Vector2Int TilePixelSize { get; private set; } = Vector2Int.Zero;

    public static TileGrid Instance = null!;

    private static int tileGap = 2;
    private static Vector2Int[] villageTileSpawnPoints => [
        new(0, 0),
        new(4, 3),
        new(6, 5),
        new(7, 7),
        new(TilesWidth - 1, TilesHeight - 1)
    ];
    private static int spawnPointsLeft;
    private static Dictionary<Vector2Int, CityController?> tileOwners = new();

    public override void _EnterTree()
    {
        Instance = this;
        spawnPointsLeft = villageTileSpawnPoints.Length;
    }

	public override void _Ready()
	{
        InitializeGeneralTileSize();

        for (var y = 0; y < TilesHeight; y++)
        {
            for (var x = 0; x < TilesHeight; x++)
            {
                var tilePos = new Vector2Int(x, y);
                AddMapElement(tilePos, TileScene);
                tileOwners.Add(tilePos, null);
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
        TilePixelSize = Vector2Int.FromVector2(tileTexture.GetSize());
    }

    private Node2D InstantiateMapElement(PackedScene scene)
    {
        var mapElementInstance = scene.Instantiate();

        if (mapElementInstance is null)
        {
            throw new ArgumentException();
        }

        AddChild(mapElementInstance);

        return (Node2D)mapElementInstance;
    }

    private Node2D AddMapElement(Vector2Int tilePosition, PackedScene scene)
    {
        var mapElementInstance = InstantiateMapElement(scene);
        mapElementInstance.Position = TileToWorldPosition(tilePosition);

        return mapElementInstance;
    }

    public static bool TryGetVillageTileSpawnPoint(out Vector2Int tileSpawnPoint)
    {
        tileSpawnPoint = Vector2Int.Zero;

        if (spawnPointsLeft == 0)
        {
            GD.PrintErr("No village spawn points left!");
            return false;
        }

        tileSpawnPoint = villageTileSpawnPoints[villageTileSpawnPoints.Length - spawnPointsLeft];
        spawnPointsLeft--;

        return true;
    }

    public static CityController AddCity(Vector2Int tilePosition)
    {
        var cityNode = Instance.AddMapElement(tilePosition, Instance.CityScene);
        return (CityController)cityNode;
    }

    public static Vector2 TileToWorldPosition(Vector2Int tilePosition)
    {
        var tileWidth = TilePixelSize.X;
        var tileHeight = TilePixelSize.Y;
        var halfWorldWidth = (TilesWidth * tileWidth + (TilesWidth - 1) * tileGap) / 2;
        var halfWorldHeight = (TilesHeight * tileHeight + (TilesHeight - 1) * tileGap) / 2;
        var xPosition = tilePosition.X * tileWidth + tilePosition.X * tileGap - halfWorldWidth;
        var yPosition = tilePosition.Y * tileHeight + tilePosition.Y * tileGap - halfWorldHeight;

        return new Vector2(xPosition, yPosition);
    }

    public static Vector2Int WorldToTilePosition(Vector2 worldPosition)
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

        return new Vector2Int(tilePosX, tilePosY);
    }

    public static bool IsTileOwned(Vector2Int tilePosition)
    {
        return tileOwners.ContainsKey(tilePosition) && tileOwners[tilePosition] is not null;
    }

    public static void SetTileOwner(Vector2Int tilePosition, CityController owner)
    {
        if (tileOwners.ContainsKey(tilePosition))
        {
            tileOwners[tilePosition] = owner;
        }
        else
        {
            tileOwners.Add(tilePosition, owner);
        }
    }
}
