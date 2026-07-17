using System;
using System.Collections.Generic;
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
    protected int MaxHealth { get; private set; } = 10;
    protected int Health { get; private set; }
    protected bool CanMoveAfterActing { get; private set; }
    protected bool CanActAfterMoving { get; private set; }

    protected EmpireController OwnerEmpire;
    protected TurnSystem TurnSystem = null!;

    protected int MovementRangeLeft { get; private set; }
    protected bool HasActedThisTurn { get; private set; }
    protected bool HasMovedThisTurn { get; private set; }

    public BaseUnit(EmpireController unitOwner)
    {
        OwnerEmpire = unitOwner;
        Texture = GetSprite();
        Centered = false;
    }

    public override void _Ready()
    {
        ResetTurnState();
        Health = MaxHealth;
        TurnSystem = GodotUtilities.FindNodeOfType<TurnSystem>(GetTree().Root);

        if (Multiplayer.IsServer())
        {
            TurnSystem.TurnStarted += OnTurnStart;
        }
    }

    public override void _ExitTree()
    {
        TurnSystem.TurnStarted -= OnTurnStart;
    }

    private void OnTurnStart(int turn)
    {
        if (!Multiplayer.IsServer()) return;

        var ownerEmpire = GetOwnerEmpire();

        ResetTurnState();
        RpcId(ownerEmpire.GetOwnerPeerId(), MethodName.ResetTurnState);
    }

    public Dictionary<Vector2I, int> GetReachableTilesWithCosts()
    {
        // 1. Take the whole tile grid or some large area of tiles that the unit
        // could never get out of with its available movement range. Note that you can't
        // limit the initial area just based on movement range if there can be tiles
        // that increase range like roads, unless you make assumptions like roads can at most
        // double movement speed, in which case you could limit the initial area to double
        // of the unit's movement range.
        // 
        // 2. Run Dijkstra's on the tile area but don't let it continue once a path becomes too
        // expensive. This prevents running it on an excessively large node graph.

        // Dijkstra's
        PriorityQueue<Vector2I, int> closestNodes = new();
        HashSet<Vector2I> processedNodes = new();
        Dictionary<Vector2I, int> distances = new();

        // Add unit position as a 0 cost node to start from
        closestNodes.Enqueue(TilePosition, 0);
        distances.Add(TilePosition, 0);

        while (closestNodes.Count > 0)
        {
            var currentNode = closestNodes.Dequeue();

            if (processedNodes.Contains(currentNode)) continue;

            processedNodes.Add(currentNode);

            foreach (var neighbor in TileGrid.GetTileNeighbors(currentNode))
            {
                var neighborHasDistance = distances.ContainsKey(neighbor);
                var currentDistanceToNeighbor = neighborHasDistance ? distances[neighbor] : int.MaxValue;
                // For now, every tile has constant weight. Things like forest and hill tiles could
                // have more weight later.
                var weight = 1;
                var newDistanceToNeighbor = distances[currentNode] + weight;

                // Short circuit to prevent excessive processing
                if (newDistanceToNeighbor > MovementRangeLeft) continue;

                if (newDistanceToNeighbor < currentDistanceToNeighbor)
                {
                    distances[neighbor] = newDistanceToNeighbor;
                    closestNodes.Enqueue(neighbor, newDistanceToNeighbor);
                }
            }
        }

        return distances;
    }

    public Vector2[] GetPathToTargetTile(Vector2I targetTilePosition)
    {
        return TileGrid.GetShortestPath(TilePosition, targetTilePosition);
    }

    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestMoveToTile(Vector2I tilePosition)
    {
        if (!Multiplayer.IsServer())
        {
            RpcId(1, MethodName.RequestMoveToTile, tilePosition);
            return;
        }

        var callerId = Multiplayer.GetRemoteSenderId();

        // If server called this RPC or if server called this directly without RPC
        if (callerId == 1 || callerId == 0)
        {
            TryMoveToTile(tilePosition);
        }
        // If a client called this
        else
        {
            // Ensure its their turn and stuff
            TryMoveToTile(tilePosition);
        }
    }

    public void ForceMoveToTile(Vector2I tilePosition)
    {
        if (!Multiplayer.IsServer())
        {
            throw new InvalidOperationException("Tried to force move a unit directly from a client");
        }

        Rpc(MethodName.SetUnitTilePosition, tilePosition, MovementRangeLeft);
    }

    public bool TryMoveToTile(Vector2I tilePosition)
    {
        if (!Multiplayer.IsServer())
        {
            throw new InvalidOperationException("Tried to move a unit directly from a client");
        }

        if (HasActedThisTurn && !CanMoveAfterActing) return false;

        // TODO: Tile based path finding. How to get from current
        // position to target position, how many tiles would the unit
        // have to move through, would there be any obstacles in the way,
        // and does the unit have enough movement for it?

        // TODO: Make it so you can swap friendly units?
        if (EntitySelector.TryGetUnit(tilePosition, out var unit) && unit is not null) return false;

        var tileCosts = GetReachableTilesWithCosts();

        if (!tileCosts.ContainsKey(tilePosition)) return false;

        MovementRangeLeft -= tileCosts[tilePosition];
        HasMovedThisTurn = true;

        Rpc(MethodName.SetUnitTilePosition, tilePosition, MovementRangeLeft);

        if (EntitySelector.TryGetTile(tilePosition, out var tile) && tile is CityController city)
        {
            if (city.OwnerEmpire != OwnerEmpire)
            {
                city.OwnerEmpire.RequestReleaseCity(city.CityUid);
                OwnerEmpire.RequestAnnexCity(city.CityUid);
            }
        }

        return true;
    }

    [Rpc(CallLocal = true)]
    private void SetUnitTilePosition(Vector2I tilePosition, int newAvailableMovementRange)
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
        TilePosition = tilePosition;
        MovementRangeLeft = newAvailableMovementRange;
        Position = TileGrid.TileToWorldPosition(tilePosition);
        EntitySelector.SetUnit(tilePosition, this);
    }

    new public void SetPosition(Vector2 position)
    {
        if (!Multiplayer.IsServer())
        {
            throw new InvalidOperationException("Tried to move a unit directly from a client");
        }

        throw new InvalidOperationException($"Don't use SetPosition() for units. Use {MethodName.SetUnitTilePosition} instead.");
    }

    [Rpc(CallLocal = true)]
    public void ResetTurnState()
    {
        MovementRangeLeft = MovementRange;
        HasActedThisTurn = false;
        HasMovedThisTurn = false;
    }

    public abstract Texture2D GetSprite();

    public EmpireController GetOwnerEmpire()
    {
        return OwnerEmpire;
    }

    public void SyncTakeDamage(int amount)
    {
        if (!Multiplayer.IsServer())
        {
            throw new InvalidOperationException("Tried to make unit take damage directly from client");
        }

        Rpc(MethodName.TakeDamage, amount);
    }

    /// <summary>
    /// Damages the unit. If the unit's health falls to or below 0, the unit dies
    /// and is immediately removed and QueueFree() is called. Returns true if the
    /// unit's health is above 0 after taking damage, false otherwise.
    /// </summary>
    [Rpc(CallLocal = true)]
    public bool TakeDamage(int amount)
    {
        if (Multiplayer.GetRemoteSenderId() == 0)
        {
            throw new InvalidOperationException("Tried to damage unit without RPC");
        }

        if (Multiplayer.GetRemoteSenderId() > 1)
        {
            throw new InvalidOperationException("Client tried to damage unit via direct RPC");
        }

        if (Health <= 0) return false;

        Health -= amount;
        DebugUtility.Print($"Unit took {amount} damage and now has {Health} health.");

        if (Health <= 0)
        {
            Health = 0;
            Death();
            return false;
        }

        return true;
    }

    public void Death()
    {
        EntitySelector.SetUnit(TilePosition, null);
        QueueFree();
    }
}
