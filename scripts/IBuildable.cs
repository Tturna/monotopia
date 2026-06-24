using Godot;

public interface IBuildable
{
    public static abstract BuildController.BuildableItemType ItemType { get; }
    public static abstract string ItemName { get; }
    public static abstract int Cost { get; }
    public static abstract bool IsUnit { get; }
    public static abstract Texture2D Sprite { get; }
}
