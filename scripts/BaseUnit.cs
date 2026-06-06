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
    public Vector2I TilePosition { get; private set; } = Vector2I.Zero;

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

    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestMoveToTile(Vector2I tilePosition)
    {
        if (!Multiplayer.IsServer())
        {
            DebugUtility.Print("Client requested to move unit. Calling RPC...");
            RpcId(1, MethodName.RequestMoveToTile, tilePosition);
            return;
        }

        var callerId = Multiplayer.GetRemoteSenderId();

        // If server called this RPC or if server called this directly without RPC
        if (callerId == 1 || callerId == 0)
        {
            DebugUtility.Print("Server is moving a unit for itself");
            TryMoveToTile(tilePosition);
        }
        // If a client called this
        else
        {
            DebugUtility.Print("Server is moving a unit on behalf of a client");
            // Ensure its their turn and stuff
            TryMoveToTile(tilePosition);
        }
    }

    public bool TryMoveToTile(Vector2I tilePosition)
    {
        if (!Multiplayer.IsServer())
        {
            throw new InvalidOperationException("Tried to move a unit directly from a client");
        }

        // TODO: Tile based path finding. How to get from current
        // position to target position, how many tiles would the unit
        // have to move through, would there be any obstacles in the way,
        // and does the unit have enough movement for it?

        // TODO: Make it so you can swap friendly units?
        if (EntitySelector.TryGetUnit(tilePosition, out var unit) && unit is not null)
        {
            DebugUtility.Print($"no unit there bro ({tilePosition})");
            return false;
        }

        Rpc(MethodName.SetUnitTilePosition, tilePosition);

        if (EntitySelector.TryGetTile(tilePosition, out var tile) && tile is CityController city)
        {
            if (city.OwnerEmpire != OwnerEmpire)
            {
                DebugUtility.Print($"Calling city release RPC function on empire {city.OwnerEmpire.EmpireUid}");
                city.OwnerEmpire.RequestReleaseCity(city.CityUid);
                DebugUtility.Print($"Calling city annex RPC function on empire {OwnerEmpire.EmpireUid}");
                OwnerEmpire.RequestAnnexCity(city.CityUid);
            }
        }

        return true;
    }

    [Rpc(CallLocal = true)]
    public void SetUnitTilePosition(Vector2I tilePosition)
    {
        if (Multiplayer.GetRemoteSenderId() == 0)
        {
            if (!Multiplayer.IsServer())
            {
                throw new InvalidOperationException("Client tried to set unit position without RPC");
            }

            GD.PushWarning("Setting unit tile position without RPC. Position is probably desynced.");
        }

        EntitySelector.SetUnit(TilePosition, null);
        this.TilePosition = tilePosition;
        Position = TileGrid.TileToWorldPosition(tilePosition);
        EntitySelector.SetUnit(tilePosition, this);
    }

    new public void SetPosition(Vector2 position)
    {
        if (!Multiplayer.IsServer())
        {
            throw new InvalidOperationException("Tried to move a unit directly from a client");
        }

        throw new InvalidOperationException("Don't use SetPosition() for units. Use SetUnitPosition instead.");
    }

    public abstract Texture2D GetSprite();
}
