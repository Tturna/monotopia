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

    public WarriorUnit SpawnWarrior(EmpireController ownerEmpire)
    {
        var unit = new WarriorUnit(ownerEmpire);
        GetTree().Root.AddChild(unit);

        return unit;
    }
}
