using Godot;

public partial class WarriorUnit : BaseUnit
{
    public WarriorUnit(EmpireController unitOwner) : base(unitOwner)
    {
        MovementRange = 2;
    }

    public override Texture2D GetSprite()
    {
        return (Texture2D)GD.Load("res://sprites/warrior.png");
    }
}
