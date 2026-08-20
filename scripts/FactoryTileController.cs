using System;
using System.Collections.Generic;
using Godot;

#nullable enable
public partial class FactoryTileController : TileController
{
    public string FactoryUid { get; private set; } = null!;
    public EmpireController OwnerEmpire { get; private set; } = null!;
    public int ProductGenerationRate { get; private set; } = 3;
    public int ProductsSuppliedCount { get; private set; }
    public Dictionary<StoreTileController, int> SupplyTargetsAndCounts { get; private set; } = new();

    public void InitializeFactory(Vector2I tilePosition, EmpireController ownerEmpire, string factorfactory)
    {
        TilePosition = tilePosition;
        FactoryUid = factorfactory;
        OwnerEmpire = ownerEmpire;
    }

    public void ServeProducts(StoreTileController targetStore, int amount)
    {
        if (ProductsSuppliedCount + amount > ProductGenerationRate)
        {
            throw new InvalidOperationException("Tried to serve more products than what is available");
        }

        ProductsSuppliedCount += amount;

        if (SupplyTargetsAndCounts.ContainsKey(targetStore))
        {
            SupplyTargetsAndCounts[targetStore] += amount;
        }
        else
        {
            SupplyTargetsAndCounts.Add(targetStore, amount);
        }

        targetStore.AddSupplies(amount);
    }

    public void WithdrawProducts(StoreTileController targetStore, int amount)
    {
        if (ProductsSuppliedCount - amount < 0)
        {
            throw new InvalidOperationException("Tried to withdraw more products than are served");
        }

        ProductsSuppliedCount -= amount;

        if (SupplyTargetsAndCounts.ContainsKey(targetStore))
        {
            if (amount > SupplyTargetsAndCounts[targetStore])
            {
                throw new InvalidOperationException($"Tried to withdraw more products from store at {targetStore.TilePosition} than what is served there from this factory.");
            }

            SupplyTargetsAndCounts[targetStore] -= amount;
            targetStore.RemoveSupplies(amount);
        }
        else
        {
            throw new InvalidOperationException("Tried to withdraw products from store that is not being served by this factory. This should never happen.");
        }
    }

    public void WithdrawAllProducts()
    {
        foreach (var store in SupplyTargetsAndCounts.Keys)
        {
            var allServedProductsToStoreCount = SupplyTargetsAndCounts[store];
            WithdrawProducts(store, allServedProductsToStoreCount);
        }
    }
}
