using System;
using Godot;

public partial class BuildController : Node2D
{
    public enum BuildableItemType
    {
        Founder
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
        BuildableItemType.Founder => InfoFrom<FounderUnit>(),
        _ => throw new ArgumentOutOfRangeException(nameof(itemType))
    };

    // TODO: Figure out a mechanism to determine buildable items based on empire and
    // city state (server side).
	public static BuildController.BuildableItemType[] GetBuildableItems()
	{
		return
		[
			BuildController.BuildableItemType.Founder
		];
	}

	[Rpc(mode: MultiplayerApi.RpcMode.AnyPeer)]
	public void RequestBuildItem(int itemTypeEnum, string cityUid)
	{
		if (!Multiplayer.IsServer())
		{
			RpcId(1, nameof(RequestBuildItem), itemTypeEnum, cityUid);
			return;
		}

		// TODO: check if item is unlocked and actually available for building

        if (!EntitySelector.TryGetCity(cityUid, out var selectedCity))
        {
            throw new InvalidOperationException($"Can't find city with UID: {cityUid}");
        }

        var ownerEmpire = selectedCity.OwnerEmpire;
		var itemType = (BuildController.BuildableItemType)itemTypeEnum;
		var itemInfo = BuildController.GetBuildableItemInfo(itemType);

		if (itemInfo.Cost > ownerEmpire.Coins)
		{
			throw new InvalidOperationException($"Item {itemInfo.ItemName} is too expensive to build in empire {Name}");
		}

        ownerEmpire.RequestUpdateCoins(-itemInfo.Cost);

		if (itemInfo.IsUnit)
		{
			if (EntitySelector.TryGetUnit(selectedCity.CityTilePosition, out var unit) && unit is not null)
			{
				throw new InvalidOperationException($"Can't build unit in an occupied city {selectedCity.Name}");
			}

            DebugUtility.Print($"Spawning unit in city tile position: {selectedCity.CityTilePosition}");
            UnitSpawner.Instance.SpawnAndSyncUnit(itemType, selectedCity.CityTilePosition, ownerEmpire);
			return; 
		}

		throw new NotImplementedException($"Empire should build a structure ({itemInfo.ItemName}) but it can only build units for now.");
	}
}
