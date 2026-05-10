using Godot;

public partial class WarriorUnit : BaseUnit
{
    public WarriorUnit(EmpireController unitOwner) : base(unitOwner) { }

    public override Texture2D GetSprite()
    {
        return (Texture2D)GD.Load("res://sprites/warrior.png");
    }
}
