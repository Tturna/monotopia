#nullable enable
public record struct BuildableItem
{
    public string ItemName;
    public int Cost;
    public UnitSpawner.Units? BuildableUnitType;
}
