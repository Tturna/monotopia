using System;
using Godot;

public partial class UnitSpawner : Node2D
{
    [Export]
    public PackedScene UnitScene;

    public static UnitSpawner Instance;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public BaseUnit SpawnUnit(BuildController.BuildableItemType unitType, EmpireController ownerEmpire)
    {
        return unitType switch
        {
            BuildController.BuildableItemType.Warrior => SpawnWarrior(ownerEmpire),
            _ => throw new ArgumentOutOfRangeException(nameof(unitType), "No spawner defined for given unit type")
        };
    }

    public WarriorUnit SpawnWarrior(EmpireController ownerEmpire)
    {
        var unit = new WarriorUnit(ownerEmpire);
        GetTree().Root.AddChild(unit, forceReadableName: true);

        return unit;
    }
}
