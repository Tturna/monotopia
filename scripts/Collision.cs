using Godot;

#nullable enable
public static class Collision
{
    public static bool AABB(
        Vector2 aPos,
        Vector2 bPos,
        Vector2? aSize = null,
        Vector2? bSize = null)
    {
        var ax = aPos.X;
        var ay = aPos.Y;
        var bx = bPos.X;
        var by = bPos.Y;
        var aw = aSize?.X ?? TileGrid.TilePixelSize.X;
        var ah = aSize?.Y ?? TileGrid.TilePixelSize.Y;
        var bw = bSize?.X ?? TileGrid.TilePixelSize.X;
        var bh = bSize?.Y ?? TileGrid.TilePixelSize.Y;

        return
        (
            ax + aw >= bx && ax <= bx + bw &&
            ay + ah >= by && ay <= by + bh
        );
    }

    public static bool IsPointInArea(Vector2 point, Vector2 areaPos, Vector2? areaSize = null)
    {
        return AABB(areaPos, point, areaSize, Vector2.Zero);
    }
}
