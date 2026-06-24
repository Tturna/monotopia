using Godot;

public partial class WarriorUnit : BaseUnit, IBuildable
{
    public static BuildController.BuildableItemType ItemType => BuildController.BuildableItemType.Warrior;
    public static string ItemName => "Warrior";
    public static int Cost => 2;
    public static bool IsUnit => true;
    public static Texture2D Sprite => (Texture2D)GD.Load("res://sprites/warrior.png");

    public WarriorUnit(EmpireController unitOwner) : base(unitOwner)
    {
        MovementRange = 2;
    }

    public override Texture2D GetSprite() => Sprite;
}
