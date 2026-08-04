using Godot;

public partial class StoreTileController : TileController
{
    public string StoreUid { get; private set; }
    public int AvailableServiceCapacity { get; private set; }

    public static readonly int BaseCustomerServiceCapacity = 3;

    private PlayerInputController inputController;

    // Temp
    public override void _Ready()
    {
        inputController = GodotUtilities.FindNodeOfType<PlayerInputController>(GetTree().Root);
    }

    // Temp
    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton buttonEvent) return;
        if (buttonEvent.ButtonIndex != MouseButton.Left) return;

        var mouseTilePosition = inputController.GetMouseTilePosition();

        if (mouseTilePosition != TilePosition) return;

        DebugUtility.Print($"Store at {TilePosition} has {AvailableServiceCapacity} available service capacity");
    }

    public void InitializeStore(Vector2I tilePosition, string storeUid)
    {
        TilePosition = tilePosition;
        StoreUid = storeUid;
        AvailableServiceCapacity = BaseCustomerServiceCapacity;
    }

    public int ServeCustomers(int amount)
    {
        if (AvailableServiceCapacity >= amount)
        {
            AvailableServiceCapacity -= amount;
            return amount;
        }

        var served = AvailableServiceCapacity;
        AvailableServiceCapacity = 0;

        return served;
    }
}
