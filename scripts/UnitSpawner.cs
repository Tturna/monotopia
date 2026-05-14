using System;
using Godot;

public partial class UnitSpawner : Node2D
{
    [Export]
    public PackedScene UnitScene;

    public static UnitSpawner Instance;
    public enum Units
    {
        Warrior
    }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public BaseUnit SpawnUnit(Units unitType, EmpireController ownerEmpire)
    {
        return unitType switch
        {
            Units.Warrior => SpawnWarrior(ownerEmpire),
            _ => throw new ArgumentOutOfRangeException(nameof(unitType), "No spawner defined for given unit type")
        };
    }

    public WarriorUnit SpawnWarrior(EmpireController ownerEmpire)
    {
        var unit = new WarriorUnit(ownerEmpire);
        GetTree().Root.AddChild(unit);

        return unit;
    }
}
