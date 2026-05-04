using Godot;

#nullable enable
public partial class TileGrid : Node2D
{
    [Export]
    public required PackedScene TileScene;
    [Export]
    public required Texture2D villageTileTexture;
    [Export]
    public required Texture2D townTileTexture;
    public static int TilesWidth = 10;
    public static int TilesHeight = 10;

    public static TileGrid Instance = null!;

    private int tileGap = 2;
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
        for (var y = 0; y < TilesHeight; y++)
        {
            for (var x = 0; x < TilesHeight; x++)
            {
                AddTile(x, y);
            }
        }
	}

    private void AddTile(int tilePosX, int tilePosY, Texture2D? texture = null)
    {
        var tileInstanceNode = (Sprite2D)TileScene.Instantiate();

        if (texture is not null)
        {
            tileInstanceNode.Texture = texture;
        }

        var textureSize = tileInstanceNode.Texture.GetSize();
        var tileWidth = textureSize.X;
        var tileHeight = textureSize.Y;
        var halfWorldWidth = TilesWidth * tileWidth / 2;
        var halfWorldHeight = TilesHeight * tileHeight / 2;
        var xPosition = tilePosX * tileWidth - halfWorldWidth;
        var yPosition = tilePosY * tileHeight - halfWorldHeight;
        tileInstanceNode.Position = new Vector2(xPosition + tileGap * tilePosX, yPosition + tileGap * tilePosY);

        AddChild(tileInstanceNode);
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

    public static void AddCity(int tilePosX, int tilePosY)
    {
        Instance.AddTile(tilePosX, tilePosY, Instance.villageTileTexture);
    }
}
