using Godot;

public partial class FounderUnit : BaseUnit, IBuildable
{
    public static BuildController.BuildableItemType ItemType => BuildController.BuildableItemType.Founder;
    public static string ItemName => "Founder";
    public static int Cost => 2;
    public static bool IsUnit => true;
    public static Texture2D Sprite => (Texture2D)GD.Load("res://sprites/warrior.png");

    public FounderUnit(EmpireController unitOwner) : base(unitOwner)
    {
        MovementRange = 2;
    }

    public override Texture2D GetSprite() => Sprite;
    public override string GetUnitName() => ItemName;
}
