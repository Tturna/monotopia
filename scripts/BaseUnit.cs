using System;
using Godot;

#nullable enable
public abstract partial class BaseUnit : Sprite2D
{
    new public Vector2 Position
    {
        get => base.Position;
        private set => base.Position = value;
    }
    public Vector2Int TilePosition { get; private set; } = Vector2Int.Zero;

    protected int MovementRange { get; init; } = 1;
    protected int AttackRange { get; init; } = 1;
    protected int Damage { get; init; } = 1;
    protected int Defense { get; init; } = 1;

    protected EmpireController OwnerEmpire;

    public BaseUnit(EmpireController unitOwner)
    {
        OwnerEmpire = unitOwner;
        Texture = GetSprite();
        Centered = false;
    }

    public bool TryMoveToTile(Vector2Int tilePosition)
    {
        // TODO: Tile based path finding. How to get from current
        // position to target position, how many tiles would the unit
        // have to move through, would there be any obstacles in the way,
        // and does the unit have enough movement for it?

        // TODO: Make it so you can swap friendly units?
        if (EntitySelector.TryGetUnit(tilePosition, out var unit) && unit is not null) return false;

        SetUnitTilePosition(tilePosition);

        return true;
    }

    public void SetUnitTilePosition(Vector2Int tilePosition)
    {
        EntitySelector.SetUnit(TilePosition, null);
        this.TilePosition = tilePosition;
        Position = TileGrid.TileToWorldPosition(tilePosition);
        EntitySelector.SetUnit(tilePosition, this);
    }

    public void SetUnitWorldPosition(Vector2 worldPosition)
    {
        var tilePosition = TileGrid.WorldToTilePosition(worldPosition);
        SetUnitTilePosition(tilePosition);
    }

    new public void SetPosition(Vector2 position)
    {
        throw new InvalidOperationException("Don't use SetPosition() for units. Use SetUnitPosition instead.");
    }

    public abstract Texture2D GetSprite();
}
