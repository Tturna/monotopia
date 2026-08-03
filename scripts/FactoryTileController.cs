using Godot;

#nullable enable
public partial class FactoryTileController : TileController
{
    public string FactoryUid { get; private set; } = null!;
    public int ProductGenerationRate { get; private set; } = 3;

    public void InitializeFactory(Vector2I tilePosition, string factorfactory)
    {
        TilePosition = tilePosition;
        FactoryUid = factorfactory;
    }
}
