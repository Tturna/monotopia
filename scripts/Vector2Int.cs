using Godot;

public record class Vector2Int
{
    public int X { get; }
    public int Y { get; }
    
    public static Vector2Int Zero => new Vector2Int();

    public Vector2Int(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Vector2Int() : this(0, 0) { }

    public static Vector2Int operator +(Vector2Int operand) => operand;
    public static Vector2Int operator -(Vector2Int operand) => new Vector2Int(-operand.X, -operand.Y);

    public static Vector2Int operator +(Vector2Int left, Vector2Int right)
    {
        return new Vector2Int(left.X + right.X, left.Y + right.Y);
    }

    public static Vector2Int operator -(Vector2Int left, Vector2Int right) => left + (-right);

    public static Vector2Int operator *(Vector2Int operand, int multiplier)
    {
        return new Vector2Int(operand.X * multiplier, operand.Y * multiplier);
    }

    public static implicit operator Vector2(Vector2Int operand)
    {
        return new Vector2(operand.X, operand.Y);
    }

    public static Vector2Int FromVector2(Vector2 vector2)
    {
        return new Vector2Int(Mathf.FloorToInt(vector2.X), Mathf.FloorToInt(vector2.Y));
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}
