using System.Collections.Generic;
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
    public Color BorderColor { get; private set; }

    private List<Vector2I> controlledTilePositions = new();
    private Vector2I? borderExpansionDirectionFocus = null;
    private Polygon2D borderPolygon = new();
    private TurnSystem turnSystem = null!;

    public override void _Ready()
    {
        turnSystem = GodotUtilities.FindNodeOfType<TurnSystem>(GetTree().Root);
    }

    private void OnTurnStarted(int turn)
    {
        if (Freeze) return;

        var nextTileAvailable = CityBorderBuilder.TryGetNextBorderExpansionTile(
            controlledTilePositions.ToArray(),
            CityTilePosition,
            owner: this,
            out var nextTilePosition,
            borderExpansionDirectionFocus);

        if (nextTileAvailable)
        {
            TakeControlOfTile(nextTilePosition);
            UpdateBorderPolygon();
        }
    }

    private void TakeControlOfTile(Vector2I tilePosition)
    {
        if (tilePosition == CityTilePosition) return;
        if (!TileGrid.IsTileInBounds(tilePosition)) return;
        if (TileGrid.IsTileOwned(tilePosition)) return;

        controlledTilePositions.Add(tilePosition);
        TileGrid.TrySetTileOwnerCity(tilePosition, this);
    }

    private void UpdateBorderPolygon()
    {
        var polygonVertices = CityBorderBuilder.Polygon2DFromTilePositions(controlledTilePositions.ToArray());
        borderPolygon.Polygon = polygonVertices;
    }

    public void InitializeCity(Vector2I tilePosition, EmpireController ownerEmpire, string newCityUid)
    {
        CityName = $"City {tilePosition}";
        CityUid = newCityUid;
        OwnerEmpire = ownerEmpire;

        turnSystem.TurnStarted += OnTurnStarted;

        CityTilePosition = tilePosition;
        controlledTilePositions.Add(CityTilePosition);
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

        UpdateBorderPolygon();
        AddChild(borderPolygon);
        // Set global pos to 0 to prevent offsetting border when the border
        // child node inherits the city position from the parent node.
        borderPolygon.GlobalPosition = Vector2.Zero;

        var a = 0.65f;
        var empirePrimaryColor = ownerEmpire.EmpirePrimaryColor;
        empirePrimaryColor.A = a;
        BorderColor = empirePrimaryColor;
        borderPolygon.Color = BorderColor;
    }

    public void SetOwnerEmpire(EmpireController newOwner, Color newBorderColor)
    {
        BorderColor = newBorderColor;
        borderPolygon.Color = BorderColor;
        OwnerEmpire = newOwner;

        // TODO: update border colors and stuff
    }
}
