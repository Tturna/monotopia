using Godot;

#nullable enable
public partial class TileController : Node2D
{
    public Vector2I TilePosition;
    public CityController? OwnerCity; // Who owns this tile if anyone
}
