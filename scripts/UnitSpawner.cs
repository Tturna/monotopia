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

    public BaseUnit SpawnUnit(BuildController.BuildableItemType unitType, EmpireController ownerEmpire)
    {
        BaseUnit unit = unitType switch
        {
            BuildController.BuildableItemType.Warrior => new WarriorUnit(ownerEmpire),
            BuildController.BuildableItemType.Archer => new ArcherUnit(ownerEmpire),
            _ => throw new ArgumentOutOfRangeException(nameof(unitType))
        };

        UnitsContainer.AddChild(unit, forceReadableName: true);
        return unit;
    }
}
