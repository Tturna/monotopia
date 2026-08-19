using System;
using Godot;

#nullable enable
public partial class FactoryTileController : TileController
{
    public string FactoryUid { get; private set; } = null!;
    public EmpireController OwnerEmpire { get; private set; } = null!;
    public int ProductGenerationRate { get; private set; } = 3;
    public int ProductsSuppliedCount { get; private set; }

    public void InitializeFactory(Vector2I tilePosition, EmpireController ownerEmpire, string factorfactory)
    {
        TilePosition = tilePosition;
        FactoryUid = factorfactory;
        OwnerEmpire = ownerEmpire;
    }

    public void ServeProducts(int amount)
    {
        if (ProductsSuppliedCount + amount > ProductGenerationRate)
        {
            throw new InvalidOperationException("Tried to serve more products than what is available");
        }

        ProductsSuppliedCount += amount;
    }
}
