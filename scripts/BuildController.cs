using System.Collections.Generic;

public static class BuildController
{
    public enum BuildableItemType
    {
        Warrior
    }

    private static Dictionary<BuildableItemType, BuildableItemInfo> buildableItemInfos = new() {
        {
            BuildableItemType.Warrior, new()
            {
                ItemName = "Warrior",
                Cost = 2,
                IsUnit = true
            }
        }
    };

    public static BuildableItemInfo GetBuildableItemInfo(BuildableItemType itemType)
    {
        return buildableItemInfos[itemType];
    }
}
