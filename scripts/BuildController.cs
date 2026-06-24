using System;

public static class BuildController
{
    public enum BuildableItemType
    {
        Warrior,
        Archer
    }

    public static BuildableItemInfo GetBuildableItemInfo(BuildableItemType itemType) => itemType switch
    {
        BuildableItemType.Warrior => InfoFrom<WarriorUnit>(),
        BuildableItemType.Archer  => InfoFrom<ArcherUnit>(),
        _ => throw new ArgumentOutOfRangeException(nameof(itemType))
    };

    private static BuildableItemInfo InfoFrom<T>() where T : IBuildable => new()
    {
        ItemName = T.ItemName,
        Cost     = T.Cost,
        IsUnit   = T.IsUnit,
        Icon     = T.Sprite
    };
}
