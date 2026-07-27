using Godot;

public partial class StoreTileController : TileController
{
    public string StoreUid { get; private set; }

    public void InitializeStore(Vector2I tilePosition, string storeUid)
    {
        TilePosition = tilePosition;
        StoreUid = storeUid;
    }
}
