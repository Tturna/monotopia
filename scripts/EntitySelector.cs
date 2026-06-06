using System.Collections.Generic;
using Godot;

#nullable enable
public static class EntitySelector
{
    private static Dictionary<Vector2I, BaseUnit?> unitMap = new();
    private static Dictionary<Vector2I, TileController> tileMap = new();
    private static Dictionary<string, CityController> cityIdMap = new();

    public static void AddTile(Vector2I tilePosition, TileController tileController)
    {
        tileMap.Add(tilePosition, tileController);
    }

    public static void SetTile(Vector2I tilePosition, TileController tileController)
    {
        if (!tileMap.ContainsKey(tilePosition))
        {
            tileMap.Add(tilePosition, tileController);

            return;
        }

        tileMap[tilePosition].QueueFree();
        tileMap[tilePosition] = tileController;
    }

    public static bool TrySetTileOwner(Vector2I tilePosition, CityController? ownerCity)
    {
        if (!TryGetTile(tilePosition, out var tileController)) return false;
        if (tileController is null) return false;

        tileController.OwnerCity = ownerCity;

        return true;
    }

    public static bool TryGetTile(Vector2I tilePosition, out TileController? tileController)
    {
        tileController = null;

        if (!tileMap.ContainsKey(tilePosition)) return false;

        tileController = tileMap[tilePosition];

        return true;
    }

    public static void SetUnit(Vector2I tilePosition, BaseUnit? unitOnTile)
    {
        if (!unitMap.ContainsKey(tilePosition))
        {
            unitMap.Add(tilePosition, unitOnTile);

            return;
        }

        unitMap[tilePosition] = unitOnTile;
    }

    public static bool TryGetUnit(Vector2I tilePosition, out BaseUnit? unit)
    {
        unit = null;

        if (!unitMap.ContainsKey(tilePosition)) return false;

        unit = unitMap[tilePosition];

        return true;
    }

    public static void SetCity(string cityUid, CityController city)
    {
        if (!cityIdMap.ContainsKey(cityUid))
        {
            cityIdMap.Add(cityUid, city);
            return;
        }

        cityIdMap[cityUid] = city;
    }

    public static bool TryGetCity(string cityUid, out CityController? city)
    {
        city = null;

        if (!cityIdMap.ContainsKey(cityUid)) return false;

        city = cityIdMap[cityUid];

        return true;
    }
}
