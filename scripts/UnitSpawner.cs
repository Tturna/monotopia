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

    public T SpawnUnit<T>() where T: BaseUnit, new()
    {
        var unit = new T();
        GetTree().Root.AddChild(unit);

        return unit;
    }
}
