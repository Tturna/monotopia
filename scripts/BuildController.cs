using System;
using Godot;

public partial class BuildController : Node2D
{
    public enum BuildableItemType
    {
        Founder,
        ExpansionTeam
    }

    public static BuildController Instance;

    public override void _EnterTree()
    {
        Instance = this;
    }

    private static BuildableItemInfo InfoFrom<T>() where T : IBuildable => new()
    {
        ItemName = T.ItemName,
        Cost     = T.Cost,
        IsUnit   = T.IsUnit,
        Icon     = T.Sprite
    };

    public static BuildableItemInfo GetBuildableItemInfo(BuildableItemType itemType) => itemType switch
    {
        BuildableItemType.ExpansionTeam => InfoFrom<ExpansionTeamUnit>(),
        _ => throw new ArgumentOutOfRangeException(nameof(itemType))
    };

    // TODO: Figure out a mechanism to determine buildable items based on empire state (server side)
	public static BuildController.BuildableItemType[] GetBuildableItems()
	{
		return
		[
			BuildController.BuildableItemType.ExpansionTeam
		];
	}

	[Rpc(mode: MultiplayerApi.RpcMode.AnyPeer)]
	public void RequestBuildItem(int itemTypeEnum, string hqUid)
	{
		if (!Multiplayer.IsServer())
		{
			RpcId(1, nameof(RequestBuildItem), itemTypeEnum, hqUid);
			return;
		}

		// TODO: check if item is unlocked and actually available for building

        if (!EntitySelector.TryGetHq(hqUid, out var selectedHq))
        {
            throw new InvalidOperationException($"Can't find HQ with UID: {hqUid}");
        }

        var ownerEmpire = selectedHq.OwnerEmpire;
		var itemType = (BuildController.BuildableItemType)itemTypeEnum;
		var itemInfo = BuildController.GetBuildableItemInfo(itemType);

		if (itemInfo.Cost > ownerEmpire.Coins)
		{
			throw new InvalidOperationException($"Item {itemInfo.ItemName} is too expensive to build in empire {Name}");
		}

        ownerEmpire.RequestUpdateCoins(-itemInfo.Cost);

		if (itemInfo.IsUnit)
		{
			if (EntitySelector.TryGetUnit(selectedHq.HqTilePosition, out var unit) && unit is not null)
			{
				throw new InvalidOperationException($"Can't build unit in an occupied HQ {selectedHq.Name}");
			}

            DebugUtility.Print($"Spawning unit in HQ tile position: {selectedHq.HqTilePosition}");
            UnitSpawner.Instance.SpawnAndSyncUnit(itemType, selectedHq.HqTilePosition, ownerEmpire);
			return; 
		}

		throw new NotImplementedException($"Empire should build a structure ({itemInfo.ItemName}) but it can only build units for now.");
	}
}
