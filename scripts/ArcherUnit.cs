using Godot;

public partial class ArcherUnit : BaseUnit, IBuildable
{
    public static BuildController.BuildableItemType ItemType => BuildController.BuildableItemType.Archer;
    public static string ItemName => "Archer";
    public static int Cost => 3;
    public static bool IsUnit => true;
    public static Texture2D Sprite => (Texture2D)GD.Load("res://sprites/archer.png");

    public ArcherUnit(EmpireController unitOwner) : base(unitOwner)
    {
        MovementRange = 2;
        AttackRange = 2;
    }

    public override Texture2D GetSprite() => Sprite;
}
