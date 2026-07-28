using System;
using Godot;

public partial class ExpansionTeamUnit : BaseUnit, IBuildable
{
    public ExpansionTeamUnit(EmpireController unitOwner) : base(unitOwner) { }

    public static BuildController.BuildableItemType ItemType => BuildController.BuildableItemType.ExpansionTeam;
    public static string ItemName => "Expansion Team";
    public static int Cost => 2;
    public static bool IsUnit => true;
    public static Texture2D Sprite => (Texture2D)GD.Load("res://sprites/expansionteam.png");

    public override Texture2D GetSprite() => Sprite;
    public override string GetUnitName() => ItemName;

    private InfluenceSystem influenceSystem;

    public override void _Ready()
    {
        influenceSystem = GodotUtilities.FindNodeOfType<InfluenceSystem>(GetTree().Root);

        base._Ready();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RequestSpawnStore()
    {
        if (!Multiplayer.IsServer())
        {
            RpcId(1, MethodName.RequestSpawnStore);
            return;
        }

        OwnerEmpire.AddNewStoreToEmpire(TilePosition, Guid.NewGuid().ToString());
        influenceSystem.RequestAddAreaOfInfluence(TilePosition, influenceAmount: 4, radius: 3);
        RequestDeath();
    }

    public override UnitAction[] GetUnitActions()
    {
        return [
            new (ActionName: "Build store", ActionCallback: RequestSpawnStore)
        ];
    }
}
