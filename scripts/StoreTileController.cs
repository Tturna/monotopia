using System;
using Godot;

public partial class StoreTileController : TileController
{
    public string StoreUid { get; private set; }
    public EmpireController OwnerEmpire { get; private set; } = null!;
    public int AvailableServiceCapacity { get; private set; }
    public int MaxSupplyCapacity { get; private set; }
    public int SuppliesCount { get; private set; }

    public static readonly int BaseCustomerServiceCapacity = 3;
    public static readonly int BaseMaxSupplyCapacity = 2;

    public void InitializeStore(Vector2I tilePosition, EmpireController ownerEmpire, string storeUid)
    {
        OwnerEmpire = ownerEmpire;
        TilePosition = tilePosition;
        StoreUid = storeUid;
        AvailableServiceCapacity = BaseCustomerServiceCapacity;
        MaxSupplyCapacity = BaseMaxSupplyCapacity;
    }

    public int ServeCustomers(int amount)
    {
        if (AvailableServiceCapacity >= amount)
        {
            AvailableServiceCapacity -= amount;
            return amount;
        }

        var served = AvailableServiceCapacity;
        AvailableServiceCapacity = 0;

        return served;
    }

    public void AddSupplies(int amount)
    {
        if (SuppliesCount + amount > MaxSupplyCapacity)
        {
            throw new InvalidOperationException("Tried to add more supplies than store can take");
        }

        SuppliesCount += amount;
    }

    public void RemoveSupplies(int amount)
    {
        if (SuppliesCount - amount < 0)
        {
            throw new InvalidOperationException("Tried to remove more supplies than are in the store");
        }

        SuppliesCount -= amount;
    }
}
