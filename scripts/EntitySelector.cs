using System.Collections.Generic;
using Godot;

#nullable enable
public static class EntitySelector
{
	private static Dictionary<Vector2I, BaseUnit?> unitMap = new();
	private static Dictionary<Vector2I, TileController> tileMap = new();
	private static Dictionary<string, HqController> hqIdMap = new();
	private static Dictionary<string, EmpireController> empireIdMap = new();

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
			// If setting a non existent entry to null, don't bother
            if (unitOnTile is null) return;

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

    public static void SetHq(string hqUid, HqController hqController)
    {
        if (!hqIdMap.ContainsKey(hqUid))
        {
            hqIdMap.Add(hqUid, hqController);
            return;
        }

        hqIdMap[hqUid] = hqController;
    }

    public static bool TryGetHq(string hqUid, out HqController? hqController)
    {
        hqController = null;

        if (!hqIdMap.ContainsKey(hqUid)) return false;

        hqController = hqIdMap[hqUid];

        return true;
    }

    public static void SetEmpire(string empireUid, EmpireController empire)
    {
        if (!empireIdMap.ContainsKey(empireUid))
        {
            empireIdMap.Add(empireUid, empire);
            return;
        }

        empireIdMap[empireUid] = empire;
    }

    public static bool TryGetEmpire(string empireUid, out EmpireController? empire)
    {
        empire = null;

        if (!empireIdMap.ContainsKey(empireUid)) return false;

        empire = empireIdMap[empireUid];

        return true;
    }

    public static Dictionary<string, EmpireController> GetEmpiresDict() => empireIdMap;
}
