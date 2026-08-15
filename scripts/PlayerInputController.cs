using System;
using Godot;

public partial class PlayerInputController : Node2D
{
    private EmpireController playerEmpire;
	private Vector2I? hoveredTile;
    private UIController uiController = null!;

    public override void _Ready()
    {
        uiController = GodotUtilities.FindNodeOfType<UIController>(GetTree().Root);
    }

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

        // Debug
        if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Y)
            {
                DebugUtility.Print("Requesting debug store spawn");
                RequestSpawnStore();
            }
            else if (keyEvent.Keycode == Key.U)
            {
                RequestSpawnFactory();
            }
        }
	}

#region DEBUG
    private void RequestSpawnStore()
    {
        if (!Multiplayer.IsServer())
        {
            RpcId(1, MethodName.RequestSpawnStore);
            return;
        }

        var mouseTilePosition = GetMouseTilePosition();
        var playerEmpire = EmpireController.GetPeerEmpire(Multiplayer.GetUniqueId());
        var storeUid = Guid.NewGuid().ToString();
        playerEmpire.AddNewStoreToEmpire(mouseTilePosition, storeUid);
        Rpc(MethodName.SyncSpawnStore, storeUid, playerEmpire.EmpireUid, mouseTilePosition);

        var influenceSystem = GodotUtilities.FindNodeOfType<InfluenceSystem>(GetTree().Root);
        influenceSystem.RequestAddAreaOfInfluence(mouseTilePosition, influenceAmount: 4, radius: 3);
    }

    private void RequestSpawnFactory()
    {
        if (!Multiplayer.IsServer())
        {
            RpcId(1, MethodName.RequestSpawnFactory);
            return;
        }

        var mouseTilePosition = GetMouseTilePosition();
        var playerEmpire = EmpireController.GetPeerEmpire(Multiplayer.GetUniqueId());
        var factoryUid = Guid.NewGuid().ToString();
        playerEmpire.AddNewFactoryToEmpire(mouseTilePosition, factoryUid);
        Rpc(MethodName.SyncSpawnFactory, factoryUid, playerEmpire.EmpireUid, mouseTilePosition);
    }

    [Rpc()]
    private void SyncSpawnStore(string storeUid, string empireUid, Vector2I tilePosition)
    {
        if (!EntitySelector.TryGetEmpire(empireUid, out var empire)) return;
        empire.AddNewStoreToEmpire(tilePosition, storeUid);
    }

    [Rpc()]
    private void SyncSpawnFactory(string factoryUid, string empireUid, Vector2I tilePosition)
    {
        if (!EntitySelector.TryGetEmpire(empireUid, out var empire)) return;
        empire.AddNewFactoryToEmpire(tilePosition, factoryUid);
    }
#endregion DEBUG

    private void HandleMouseOneClick()
    {
        if (!playerEmpire.IsActivePlayerEmpire()) return;

        var mouseTilePosition = GetMouseTilePosition();

		if (!TileGrid.IsTileInBounds(mouseTilePosition)) return;

        if (uiController.IsChoosingSupplyTargets)
        {
            if (EntitySelector.TryGetTile(mouseTilePosition, out var genericTile) &&
                genericTile is not null &&
                genericTile is StoreTileController storeTile)
            {
                DebugUtility.Print("Clicked on store when targeting supplies");
                uiController.AddSupplyTargetSelection(storeTile);
            }

            return;
        }

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
        if (uiController.IsChoosingSupplyTargets)
        {
            var mouseTilePosition = GetMouseTilePosition();

            if (!TileGrid.IsTileInBounds(mouseTilePosition)) return;

            if (EntitySelector.TryGetTile(mouseTilePosition, out var genericTile) &&
                genericTile is not null &&
                genericTile is StoreTileController storeTile)
            {
                uiController.RemoveSupplyTargetSelection(storeTile);
            }

            return;
        }

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
