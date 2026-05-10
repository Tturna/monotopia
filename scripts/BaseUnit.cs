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
    protected bool IsSelected { get; private set; }

    private PackedScene tileSelectionScene => (PackedScene)GD.Load("res://scenes/TileSelection.tscn");
    private Sprite2D? tileSelectionNode;

    public BaseUnit(EmpireController unitOwner)
    {
        OwnerEmpire = unitOwner;
        Texture = GetSprite();
        Centered = false;
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton mouseButtonEvent) return;

        if (IsSelected && mouseButtonEvent.ButtonIndex == MouseButton.Right)
        {
            IsSelected = false;
            OwnerEmpire.SelectedUnit = null;
            UpdateTileSelection();

            return;
        }

        if (mouseButtonEvent.ButtonIndex != MouseButton.Left) return;

        if (!mouseButtonEvent.IsPressed()) return;

        var mouseWorldPosition = GetViewport().GetCamera2D().GetGlobalMousePosition();
        var mouseTilePosition = TileGrid.WorldToTilePosition(mouseWorldPosition);

        if (!TileGrid.IsTileInBounds(mouseTilePosition)) return;

        if (mouseTilePosition != TilePosition)
        {
            if (IsSelected && TryMoveToTile(mouseTilePosition))
            {
                IsSelected = false;
                OwnerEmpire.SelectedUnit = null;
                UpdateTileSelection();
            }

            return;
        }

        if (OwnerEmpire.SelectedUnit is not null && OwnerEmpire.SelectedUnit != this) return;

        IsSelected = !IsSelected;
        OwnerEmpire.SelectedUnit = IsSelected ? this : null;
        UpdateTileSelection();
    }

    private bool TryMoveToTile(Vector2Int tilePosition)
    {
        // TODO: Tile based path finding. How to get from current
        // position to target position, how many tiles would the unit
        // have to move through, would there be any obstacles in the way,
        // and does the unit have enough movement for it?

        var unitOwner = TileGrid.GetTileUnitOwner(tilePosition);

        // TODO: Make it so you can swap friendly units?
        if (unitOwner is not null) return false;

        SetUnitTilePosition(tilePosition);

        return true;
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
        TileGrid.SetTileUnitOwner(TilePosition, null);
        this.TilePosition = tilePosition;
        Position = TileGrid.TileToWorldPosition(tilePosition);
        TileGrid.SetTileUnitOwner(tilePosition, OwnerEmpire);
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
