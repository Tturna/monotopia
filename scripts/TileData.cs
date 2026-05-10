#nullable enable
public record struct TileData
{
    public CityController? TileOwner; // Who owns this tile if anyone
    public EmpireController? UnitOwner; // Who owns the unit on this tile if there is one
}
