using System;
using System.Collections.Generic;
using Godot;

#nullable enable
public partial class TileGrid : Node2D
{
    [Export]
    public required PackedScene TileScene;
    [Export]
    public required PackedScene ResidentialTileScene;
    [Export]
    public required PackedScene HqScene;
    [Export]
    public required PackedScene StoreScene;
    [Export]
    public required PackedScene FactoryScene;
    [Export]
    public required Texture2D villageTileTexture;
    [Export]
    public required Texture2D townTileTexture;
    public static int TilesWidth = 40;
    public static int TilesHeight = 40;
    public static Vector2I TilePixelSize { get; private set; } = Vector2I.Zero;

    public static TileGrid Instance = null!;

    private static int tileGap = 2;
    private static Vector2I[] playerTileSpawnPoints = new Vector2I[]
    {
        new Vector2I(3, 3),
        new Vector2I(TilesWidth - 3, 3),
        new Vector2I(3, TilesHeight - 3),
        new Vector2I(TilesWidth - 3, TilesHeight - 3),
    };
    private static int spawnPointsLeft;
    private static AStar2D astar = null!;
    private static Vector2I[] tempResidentialTilePositions = new Vector2I[]
    {
        new Vector2I(8, 2),
        new Vector2I(29, 2),
        new Vector2I(32, 5),
        new Vector2I(1, 6),
        new Vector2I(4, 7),
        new Vector2I(36, 8),
        new Vector2I(20, 9),
        new Vector2I(12, 11),
        new Vector2I(18, 12),
        new Vector2I(24, 12),
        new Vector2I(15, 13),
        new Vector2I(21, 14),
        new Vector2I(9, 16),
        new Vector2I(25, 16),
        new Vector2I(14, 17),
        new Vector2I(18, 17),
        new Vector2I(22, 18),
        new Vector2I(16, 20),
        new Vector2I(21, 20),
        new Vector2I(10, 22),
        new Vector2I(25, 23),
        new Vector2I(19, 25),
        new Vector2I(9, 30),
        new Vector2I(30, 30),
        new Vector2I(4, 33),
        new Vector2I(31, 33),
        new Vector2I(12, 34),
        new Vector2I(24, 35),
    };

    public override void _EnterTree()
    {
        Instance = this;
        spawnPointsLeft = playerTileSpawnPoints.Length;
    }

    public override void _Ready()
    {
        InitializeGeneralTileSize();
        astar = new();

        GenerateBaseGrid(astar);
        ConnectAstarNeighbors(astar);

        if (Multiplayer.IsServer())
        {
            GenerateResidentialTiles();
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

    private void GenerateBaseGrid(AStar2D astar)
    {
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
    }

    private void ConnectAstarNeighbors(AStar2D astar)
    {
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

    [Rpc()]
    private void SyncResidentialTile(Vector2I tilePosition, int residentCount)
    {
        var residentialTileNode = Instance.AddMapElement(tilePosition, Instance.ResidentialTileScene);
        var residentialTile = (ResidentialTileController)residentialTileNode;
        residentialTile.Initialize(residentCount);
        EntitySelector.SetTile(tilePosition, residentialTile);

        var residentsLabel = new Label();
        residentsLabel.Text = residentialTile.Residents.ToString();
        residentsLabel.Size = TilePixelSize;
        residentsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        residentialTile.AddChild(residentsLabel);
    }

    private void GenerateResidentialTiles()
    {
        foreach (var tilePosition in tempResidentialTilePositions)
        {
            var residentialTileNode = Instance.AddMapElement(tilePosition, Instance.ResidentialTileScene);
            var residentialTile = (ResidentialTileController)residentialTileNode;
            residentialTile.Initialize();
            EntitySelector.SetTile(tilePosition, residentialTile);

            var residentsLabel = new Label();
            residentsLabel.Text = residentialTile.Residents.ToString();
            residentsLabel.Size = TilePixelSize;
            residentsLabel.HorizontalAlignment = HorizontalAlignment.Center;
            residentialTile.AddChild(residentsLabel);

            Rpc(MethodName.SyncResidentialTile, tilePosition, residentialTile.Residents);
        }
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

    private static T AddSpecialTile<T>(Vector2I tilePosition, PackedScene scene) where T : TileController
    {
        var node = Instance.AddMapElement(tilePosition, scene);
        var controller = (T)node;
        EntitySelector.SetTile(tilePosition, controller);
        return controller;
    }

    public static HqController AddHq(Vector2I tilePosition)
    {
        return AddSpecialTile<HqController>(tilePosition, Instance.HqScene);
    }

    public static StoreTileController AddStore(Vector2I tilePosition)
    {
        return AddSpecialTile<StoreTileController>(tilePosition, Instance.StoreScene);
    }

    public static FactoryTileController AddFactory(Vector2I tilePosition)
    {
        return AddSpecialTile<FactoryTileController>(tilePosition, Instance.FactoryScene);
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
