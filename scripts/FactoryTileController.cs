using Godot;

#nullable enable
public partial class FactoryTileController : TileController
{
    public string FactoryUid { get; private set; } = null!;
    public int ProductCreationRate { get; private set; } = 5;

    public void InitializeFactory(Vector2I tilePosition, string factorfactory)
    {
        TilePosition = tilePosition;
        FactoryUid = factorfactory;
    }
}
