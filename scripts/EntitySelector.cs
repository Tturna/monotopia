using System.Collections.Generic;

#nullable enable
public static class EntitySelector
{
    private static Dictionary<Vector2Int, BaseUnit?> unitMap = new();
    private static Dictionary<Vector2Int, TileController> tileMap = new();

    public static void AddTile(Vector2Int tilePosition, TileController tileController)
    {
        tileMap.Add(tilePosition, tileController);
    }

    public static void SetTile(Vector2Int tilePosition, TileController tileController)
    {
        if (!tileMap.ContainsKey(tilePosition))
        {
            tileMap.Add(tilePosition, tileController);

            return;
        }

        tileMap[tilePosition].QueueFree();
        tileMap[tilePosition] = tileController;
    }

    public static bool TrySetTileOwner(Vector2Int tilePosition, CityController? ownerCity)
    {
        if (!TryGetTile(tilePosition, out var tileController)) return false;
        if (tileController is null) return false;

        tileController.OwnerCity = ownerCity;

        return true;
    }

    public static bool TryGetTile(Vector2Int tilePosition, out TileController? tileController)
    {
        tileController = null;

        if (!tileMap.ContainsKey(tilePosition)) return false;

        tileController = tileMap[tilePosition];

        return true;
    }

    public static void SetUnit(Vector2Int tilePosition, BaseUnit? unitOnTile)
    {
        if (!unitMap.ContainsKey(tilePosition))
        {
            unitMap.Add(tilePosition, unitOnTile);

            return;
        }

        unitMap[tilePosition] = unitOnTile;
    }

    public static bool TryGetUnit(Vector2Int tilePosition, out BaseUnit? unit)
    {
        unit = null;

        if (!unitMap.ContainsKey(tilePosition)) return false;

        unit = unitMap[tilePosition];

        return true;
    }
}
