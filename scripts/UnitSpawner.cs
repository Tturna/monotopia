using System;
using Godot;

public partial class UnitSpawner : Node2D
{
    [Export]
    public Node2D UnitsContainer;

    public static UnitSpawner Instance;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public void SpawnAndSyncUnit(BuildController.BuildableItemType unitType, Vector2I tilePosition, EmpireController ownerEmpire)
    {
        if (!Multiplayer.IsServer())
        {
            throw new InvalidOperationException("Tried to spawn unit from client");
        }

        Rpc(MethodName.SyncUnitSpawn, (int)unitType, tilePosition, ownerEmpire.EmpireUid);
    }

	[Rpc(CallLocal = true)]
	private void SyncUnitSpawn(int unitTypeEnum, Vector2I tilePosition, string ownerEmpireUid)
	{
        if (Multiplayer.GetRemoteSenderId() == 0)
        {
            throw new InvalidOperationException("Tried to sync unit spawn without RPC");
        }

        var unitType = (BuildController.BuildableItemType)unitTypeEnum;

        if (!EntitySelector.TryGetEmpire(ownerEmpireUid, out var ownerEmpire))
        {
            throw new InvalidOperationException($"Can't find empire with UID: {ownerEmpireUid}");
        }

        BaseUnit unit = unitType switch
        {
            BuildController.BuildableItemType.Warrior => new WarriorUnit(ownerEmpire),
            BuildController.BuildableItemType.Archer => new ArcherUnit(ownerEmpire),
            _ => throw new ArgumentOutOfRangeException(nameof(unitType))
        };

        UnitsContainer.AddChild(unit, forceReadableName: true);

        if (Multiplayer.IsServer())
        {
            unit.ForceMoveToTile(tilePosition);
        }
	}
}
