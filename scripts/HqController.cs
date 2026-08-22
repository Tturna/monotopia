using System;
using Godot;

#nullable enable
public partial class HqController : TileController
{
    public string HqUid { get; private set; } = null!;
    public Vector2I HqTilePosition { get; private set; }
    public EmpireController OwnerEmpire { get; private set; } = null!;

    public readonly int CoinsGenerated = 2;
    public readonly int BaseProductsGenerated = 1;
    public int ProductsSuppliedCount { get; private set; }
    public StoreTileController? SupplyTarget { get; private set; }

    public void InitializeHq(Vector2I tilePosition, EmpireController ownerEmpire, string newHqUid)
    {
        HqUid = newHqUid;
        OwnerEmpire = ownerEmpire;
        HqTilePosition = tilePosition;
    }

    public void SetOwnerEmpire(EmpireController newOwner)
    {
        OwnerEmpire = newOwner;
    }

    public void ServeProducts(StoreTileController targetStore, int amount)
    {
        if (ProductsSuppliedCount + amount > BaseProductsGenerated)
        {
            throw new InvalidOperationException("Tried to serve more products than what is available");
        }

        ProductsSuppliedCount += amount;
        SupplyTarget = targetStore;
        targetStore.AddSupplies(amount);
    }

    public void WithdrawProducts()
    {
        if (SupplyTarget is null)
        {
            throw new InvalidOperationException("Tried to withdraw products when no supply target is set");
        }

        SupplyTarget.RemoveSupplies(1);
        ProductsSuppliedCount -= 1;
        SupplyTarget = null;
    }
}
