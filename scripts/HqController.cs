using Godot;

#nullable enable
public partial class HqController : TileController
{
    [Export]
    public bool Freeze;

    public string HqUid { get; private set; } = null!;
    public int CoinsGenerated { get; private set; } = 2;
    public Vector2I HqTilePosition { get; private set; }
    public EmpireController OwnerEmpire { get; private set; } = null!;

    private TurnSystem turnSystem = null!;

    public override void _Ready()
    {
        turnSystem = GodotUtilities.FindNodeOfType<TurnSystem>(GetTree().Root);
    }

    public void InitializeHq(Vector2I tilePosition, EmpireController ownerEmpire, string newHqUid)
    {
        HqUid = newHqUid;
        OwnerEmpire = ownerEmpire;

        HqTilePosition = tilePosition;
    }

    public void SetOwnerEmpire(EmpireController newOwner)
    {
        OwnerEmpire = newOwner;
    }
}
