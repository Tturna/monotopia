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

    protected bool IsSelected { get; private set; }

    private PackedScene tileSelectionScene => (PackedScene)GD.Load("res://scenes/TileSelection.tscn");
    private Sprite2D? tileSelectionNode;

    public BaseUnit()
    {
        Texture = GetSprite();
        Centered = false;
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton mouseButtonEvent) return;

        if (!mouseButtonEvent.IsPressed()) return;

        var mouseWorldPosition = GetViewport().GetCamera2D().GetGlobalMousePosition();

        if (!Collision.IsPointInArea(mouseWorldPosition, Position))
        {
            IsSelected = false;
            UpdateTileSelection();
            return;
        }

        IsSelected = !IsSelected;
        UpdateTileSelection();
    }

    private void UpdateTileSelection()
    {
        if (!IsSelected)
        {
            if (tileSelectionNode is not null)
            {
                tileSelectionNode.QueueFree();
                tileSelectionNode = null;
            }

            return;
        }

        if (tileSelectionNode is null)
        {
            tileSelectionNode = (Sprite2D)tileSelectionScene.Instantiate();
            AddChild(tileSelectionNode);
        }
    }

    public void SetUnitTilePosition(Vector2Int tilePosition)
    {
        this.TilePosition = tilePosition;
        Position = TileGrid.TileToWorldPosition(tilePosition);
    }

    public void SetUnitWorldPosition(Vector2 worldPosition)
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
