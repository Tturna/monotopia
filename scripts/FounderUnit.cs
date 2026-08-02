using System;
using System.Collections.Generic;
using Godot;

public partial class FounderUnit : BaseUnit
{
    public static Texture2D Sprite => (Texture2D)GD.Load("res://sprites/warrior.png");

    private InfluenceSystem influenceSystem;

    public FounderUnit(EmpireController unitOwner) : base(unitOwner)
    {
        MovementRange = 2;
    }

    public override Texture2D GetSprite() => Sprite;
    public override string GetUnitName() => "Founder";

    public override void _Ready()
    {
        influenceSystem = GodotUtilities.FindNodeOfType<InfluenceSystem>(GetTree().Root);
        base._Ready();
    }

    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RequestSpawnHQ()
    {
        if (!Multiplayer.IsServer())
        {
            RpcId(1, MethodName.RequestSpawnHQ);
            return;
        }

        var requesterPeerId = Multiplayer.GetRemoteSenderId();

        // The server doesn't send an RPC so remote sender ID doesn't match its peer ID.
        if (requesterPeerId == 0)
        {
            requesterPeerId = 1;
        }

        if (requesterPeerId != OwnerEmpire.GetOwnerPeerId())
        {
            throw new InvalidOperationException("Requested HQ spawn from an unowned unit.");
        }

        var hqUid = Guid.NewGuid().ToString();
        OwnerEmpire.AddHqToEmpire(TilePosition, hqUid);
        Rpc(MethodName.SyncSpawnHQ, hqUid);

        influenceSystem.RequestAddAreaOfInfluence(TilePosition, influenceAmount: 4, radius: 2);
    }

    [Rpc()]
    private void SyncSpawnHQ(string hqUid)
    {
        OwnerEmpire.AddHqToEmpire(TilePosition, hqUid);
    }

    public override UnitAction[] GetUnitActions()
    {
        List<UnitAction> actionList = new();

        if (!OwnerEmpire.IsHqCreated)
        {
            actionList.Add(new (ActionName: "Found HQ", ActionCallback: RequestSpawnHQ, IsSingleUse: true));
        }

        return actionList.ToArray();
    }
}
