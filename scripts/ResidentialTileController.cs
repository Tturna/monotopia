using System;
using Godot;

public partial class ResidentialTileController : TileController
{
    public int Residents { get; private set; } = -1;

    public void Initialize(int residentCount = -1)
    {
        if (Residents >= 0)
        {
            GD.PushWarning($"Attempted to initialize residential tile at {TilePosition} multiple times");
            return;
        }

        if (residentCount >= 0)
        {
            Residents = residentCount;
        }
        else
        {
            Residents = Random.Shared.Next(1, 4);
        }
    }
}
