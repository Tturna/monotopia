using System;
using Godot;

public abstract partial class BaseUnit : Sprite2D
{
    new public Vector2 Position
    {
        get => base.Position;
        private set => base.Position = value;
    }
    public Vector2Int TilePosition { get; private set; }

    protected int MovementRange { get; init; } = 1;
    protected int AttackRange { get; init; } = 1;
    protected int Damage { get; init; } = 1;
    protected int Defense { get; init; } = 1;

    public BaseUnit()
    {
        GD.Print("base constructor");
        Texture = GetSprite();
        Centered = false;
    }

    public void SetUnitPosition(Vector2Int tilePosition)
    {
        this.TilePosition = tilePosition;
        Position = TileGrid.TileToWorldPosition(tilePosition);
    }

    public void SetUnitPosition(Vector2 worldPosition)
    {
        Position = worldPosition;
        TilePosition = TileGrid.WorldToTilePosition(worldPosition);
    }

    new public void SetPosition(Vector2 position)
    {
        throw new InvalidOperationException("Don't use SetPosition() for units. Use SetUnitPosition instead.");
    }

    public abstract Texture2D GetSprite();
}
