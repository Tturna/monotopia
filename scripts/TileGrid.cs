using System;
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

    public static TileGrid Instance = null!;

    private static Vector2 tilePixelSize = Vector2.Zero;
    private static int tileGap = 2;
    private static Vector2[] villageTileSpawnPoints => [
        new(0, 0),
        new(TilesWidth - 1, TilesHeight - 1)
    ];
    private static int spawnPointsLeft;

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
                AddMapElement(x, y, TileScene);
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
        tilePixelSize = tileTexture.GetSize();
    }

    private void SetMapElementPositionFromTileCoords(int tilePosX, int tilePosY, Node2D mapElement)
    {
        var tileWidth = tilePixelSize.X;
        var tileHeight = tilePixelSize.Y;
        var halfWorldWidth = (TilesWidth * tileWidth + (TilesWidth - 1) * tileGap) / 2;
        var halfWorldHeight = (TilesHeight * tileHeight + (TilesHeight - 1) * tileGap) / 2;
        var xPosition = tilePosX * tileWidth + tilePosX * tileGap - halfWorldWidth;
        var yPosition = tilePosY * tileHeight + tilePosY * tileGap - halfWorldHeight;
        mapElement.Position = new Vector2(xPosition, yPosition);
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

    private Node2D AddMapElement(int tilePosX, int tilePosY, PackedScene scene)
    {
        var mapElementInstance = InstantiateMapElement(scene);
        SetMapElementPositionFromTileCoords(tilePosX, tilePosY, mapElementInstance);
        return mapElementInstance;
    }

    public static bool TryGetVillageTileSpawnPoint(out int spawnPointX, out int spawnPointY)
    {
        spawnPointX = 0;
        spawnPointY = 0;

        if (spawnPointsLeft == 0)
        {
            GD.PrintErr("No village spawn points left!");
            return false;
        }

        var spawnPoint = villageTileSpawnPoints[villageTileSpawnPoints.Length - spawnPointsLeft];
        spawnPointX = (int)spawnPoint.X;
        spawnPointY = (int)spawnPoint.Y;
        spawnPointsLeft--;
        return true;
    }

    public static CityController AddCity(int tilePosX, int tilePosY)
    {
        var cityNode = Instance.AddMapElement(tilePosX, tilePosY, Instance.CityScene);
        return (CityController)cityNode;
    }

    public static (int tilePosX, int tilePosY) WorldToTilePosition(Vector2 worldPosition)
    {
        var tileWidth = tilePixelSize.X;
        var tileHeight = tilePixelSize.Y;
        var halfWorldWidth = (TilesWidth * tileWidth + (TilesWidth - 1) * tileGap) / 2;
        var halfWorldHeight = (TilesHeight * tileHeight + (TilesHeight - 1) * tileGap) / 2;

        // var xPosition = tilePosX * tileWidth + tilePosX * tileGap - halfWorldWidth;
        // When you factor this: tilePosX * tileWidth + tilePosX * tileGap - halfWorldWidth
        // You get: tilePosX * (tileWidth + tileGap) - halfWorldWidth
        // Therefore the reverse coordinate translation would be:
        // tilePosX = (xPos + halfWorldWidth) / (tileWidth + tileGap)

        var tilePosX = (worldPosition.X + halfWorldWidth) / (tileWidth + tileGap);
        var tilePosY = (worldPosition.Y + halfWorldHeight) / (tileHeight + tileGap);

        return (Mathf.FloorToInt(tilePosX), Mathf.FloorToInt(tilePosY));
    }
}
