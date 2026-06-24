using Godot;

public record struct BuildableItemInfo
{
    public string ItemName;
    public int Cost;
    public bool IsUnit;
    public Texture2D Icon;
}
