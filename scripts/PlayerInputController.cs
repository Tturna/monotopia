using Godot;

public partial class PlayerInputController : Node2D
{
    private EmpireController playerEmpire;
	private Vector2I? hoveredTile;

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.IsPressed())
		{
            if (mouseButtonEvent.ButtonIndex == MouseButton.Left)
            {
                HandleMouseOneClick();
            }
            else if (mouseButtonEvent.ButtonIndex == MouseButton.Right)
            {
                HandleMouseTwoClick();
            }

			return;
		}

		if (inputEvent is InputEventMouseMotion mouseMotionEvent)
		{
            HandleMouseMove();
			return;
		}
	}

    private void HandleMouseOneClick()
    {
        if (!playerEmpire.IsActivePlayerEmpire()) return;

        var mouseTilePosition = GetMouseTilePosition();

		if (!TileGrid.IsTileInBounds(mouseTilePosition)) return;

		if (EntitySelector.TryGetUnit(mouseTilePosition, out var unit) && unit is not null)
		{
            playerEmpire.HandleUnitSelection(unit);
            return;
		}

		if (EntitySelector.TryGetTile(mouseTilePosition, out var tileController) && tileController is not null)
		{
            playerEmpire.HandleTileSelection(tileController);
		}
    }

    private void HandleMouseTwoClick()
    {
        playerEmpire.Deselect();
    }

	private void HandleMouseMove()
	{
        var mouseTilePosition = GetMouseTilePosition();

		if (TileGrid.IsTileInBounds(mouseTilePosition))
		{
			if (playerEmpire.IsOwnUnitSelected && mouseTilePosition != hoveredTile)
			{
                playerEmpire.UpdateSelectedOwnUnitPathLine(mouseTilePosition);
			}

			hoveredTile = mouseTilePosition;
		}
		else
		{
			hoveredTile = null;
		}
	}

    public void SetTargetEmpire(EmpireController empire)
    {
        playerEmpire = empire;
    }

    public Vector2 GetMouseWorldPosition()
    {
		return GetViewport().GetCamera2D().GetGlobalMousePosition();
    }

    public Vector2I GetMouseTilePosition()
    {
		return TileGrid.WorldToTilePosition(GetMouseWorldPosition());
    }
}
