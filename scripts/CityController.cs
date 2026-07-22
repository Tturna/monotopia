using Godot;

#nullable enable
public partial class CityController : TileController
{
    [Export]
    public bool Freeze;

    public string CityName { get; private set; } = null!;
    public string CityUid { get; private set; } = null!;
    public int CoinsGenerated { get; private set; } = 2;
    public Vector2I CityTilePosition { get; private set; }
    public EmpireController OwnerEmpire { get; private set; } = null!;

    private TurnSystem turnSystem = null!;

    public override void _Ready()
    {
        turnSystem = GodotUtilities.FindNodeOfType<TurnSystem>(GetTree().Root);
    }

    private void TakeControlOfTile(Vector2I tilePosition)
    {
        if (tilePosition == CityTilePosition) return;
        if (!TileGrid.IsTileInBounds(tilePosition)) return;
        if (TileGrid.IsTileOwned(tilePosition)) return;

        TileGrid.TrySetTileOwnerCity(tilePosition, this);
    }

    public void InitializeCity(Vector2I tilePosition, EmpireController ownerEmpire, string newCityUid)
    {
        CityName = $"City {tilePosition}";
        CityUid = newCityUid;
        OwnerEmpire = ownerEmpire;

        CityTilePosition = tilePosition;
        TileGrid.TrySetTileOwnerCity(CityTilePosition, this);

        // Grow one tile outwards if possible, taking up a max of 3x3 tiles.
        for (var y = -1; y < 2; y++)
        {
            for (var x = -1; x < 2; x++)
            {
                var controlledTilePosition = CityTilePosition + new Vector2I(x, y);
                TakeControlOfTile(controlledTilePosition);
            }
        }

        TakeControlOfTile(CityTilePosition + new Vector2I(0, -2));
        TakeControlOfTile(CityTilePosition + new Vector2I(0, 2));
        TakeControlOfTile(CityTilePosition + new Vector2I(2, 0));
        TakeControlOfTile(CityTilePosition + new Vector2I(-2, 0));

        var a = 0.65f;
        var empirePrimaryColor = ownerEmpire.EmpirePrimaryColor;
        empirePrimaryColor.A = a;
    }

    public void SetOwnerEmpire(EmpireController newOwner)
    {
        OwnerEmpire = newOwner;
    }
}
