using System;
using System.Collections.Generic;
using Godot;

#nullable enable
public partial class InfluenceSystem : Node
{
    // Each tile (Vector2I) has a dictionary of players' peer IDs (long) and
    // their influence (int) over the tile
    private Dictionary<Vector2I, Dictionary<long, int>> tileInfluences = new();
    private Dictionary<long, Polygon2D> playerInfluencePolygons = new();

    // temp
    private PlayerInputController inputController = null!;

    public override void _Ready()
    {
        inputController = GodotUtilities.FindNodeOfType<PlayerInputController>(GetTree().Root);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton mouseButtonEvent) return;
        if (mouseButtonEvent.ButtonIndex != MouseButton.Left) return;
        if (!mouseButtonEvent.IsPressed()) return;

        var mouseTilePosition = inputController.GetMouseTilePosition();

        if (!tileInfluences.ContainsKey(mouseTilePosition))
        {
            DebugUtility.Print($"No one has influence on tile {mouseTilePosition}");
            return;
        }

        DebugUtility.Print($"Tile {mouseTilePosition} influences:");

        foreach (var (peerId, influence) in tileInfluences[mouseTilePosition])
        {
            DebugUtility.Print($"Peer {peerId}: {influence}");
        }

        DebugUtility.Print("---------------");
    }

    [Rpc()]
    private void SyncTileInfluenceForPeer(Vector2I tilePosition, long peerId, int influence)
    {
        if (!tileInfluences.ContainsKey(tilePosition))
        {
            tileInfluences.Add(tilePosition, new());
        }

        var playerInfluencesDict = tileInfluences[tilePosition];

        if (!playerInfluencesDict.ContainsKey(peerId))
        {
            playerInfluencesDict.Add(peerId, influence);
        }
        else
        {
            playerInfluencesDict[peerId] = influence;
        }
    }

    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RequestAddAreaOfInfluence(Vector2I centerTilePosition, int influenceAmount, int radius = 1)
    {
        if (Multiplayer.GetUniqueId() != 1)
        {
            RpcId(1, MethodName.RequestAddAreaOfInfluence, centerTilePosition, influenceAmount, radius);
            return;
        }

        var requesterPeedId = Multiplayer.GetRemoteSenderId();

        // Server doesn't RPC so remote ID is 0
        if (requesterPeedId == 0)
        {
            requesterPeedId = 1;
        }

        for (var yOffset = -radius; yOffset <= radius; yOffset++)
        {
            for (var xOffset = -radius; xOffset <= radius; xOffset++)
            {
                var tilePos = centerTilePosition + new Vector2I(xOffset, yOffset);

                if (!tileInfluences.ContainsKey(tilePos))
                {
                    tileInfluences.Add(tilePos, new());
                }

                var playerInfluencesDict = tileInfluences[tilePos];

                if (!playerInfluencesDict.ContainsKey(requesterPeedId))
                {
                    playerInfluencesDict.Add(requesterPeedId, 0);
                }

                var distanceFromCenter = Math.Max(Math.Abs(xOffset), Math.Abs(yOffset));
                var influenceFalloffMultiplier = 1f / (1f + distanceFromCenter);
                var influenceGain = influenceAmount * influenceFalloffMultiplier;
                playerInfluencesDict[requesterPeedId] += (int)influenceGain;

                // TODO: Consider whether it would be better to RPC all updated tiles at once instead
                Rpc(MethodName.SyncTileInfluenceForPeer, tilePos, requesterPeedId, playerInfluencesDict[requesterPeedId]);
            }
        }
    }
}
