using System;
using System.Collections.Generic;
using Godot;

#nullable enable
public partial class InfluenceSystem : Node
{
    // Each tile (Vector2I) has a dictionary of players' peer IDs (long) and
    // their influence (int) over the tile
    private Dictionary<Vector2I, Dictionary<long, int>> tileInfluences = new();
    // Each tile has a peer ID that controls the most influence over it
    private Dictionary<Vector2I, long> topTileInfluencers = new();
    // Each peer has a hash set of tiles they have the most influence over.
    private Dictionary<long, HashSet<Vector2I>> peerTopInfluenceTiles = new();

    private InfluencePolygonBuilder polygonBuilder = null!;

    // temp
    private PlayerInputController inputController = null!;

    public override void _Ready()
    {
        inputController = GodotUtilities.FindNodeOfType<PlayerInputController>(GetTree().Root);
        polygonBuilder = GodotUtilities.FindNodeOfType<InfluencePolygonBuilder>(GetTree().Root);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton mouseButtonEvent) return;
        if (mouseButtonEvent.ButtonIndex != MouseButton.Left) return;
        if (!mouseButtonEvent.IsPressed()) return;

        var mouseTilePosition = inputController.GetMouseTilePosition();

        if (!tileInfluences.ContainsKey(mouseTilePosition)) return;
    }

    [Rpc()]
    private void SyncTileInfluenceForPeer(long targetPeerId, Vector2I tilePosition, int targetPeerInfluence, long topInfluencerId)
    {
        if (!tileInfluences.ContainsKey(tilePosition))
        {
            tileInfluences.Add(tilePosition, new());
        }

        var playerInfluencesDict = tileInfluences[tilePosition];

        if (!playerInfluencesDict.ContainsKey(targetPeerId))
        {
            playerInfluencesDict.Add(targetPeerId, targetPeerInfluence);
        }
        else
        {
            playerInfluencesDict[targetPeerId] = targetPeerInfluence;
        }

        if (!topTileInfluencers.ContainsKey(tilePosition))
        {
            topTileInfluencers.Add(tilePosition, topInfluencerId);
        }
        else
        {
            topTileInfluencers[tilePosition] = topInfluencerId;
        }

        if (!peerTopInfluenceTiles.ContainsKey(topInfluencerId))
        {
            peerTopInfluenceTiles.Add(topInfluencerId, new());
        }

        peerTopInfluenceTiles[topInfluencerId].Add(tilePosition);
    }

    [Rpc(CallLocal = true)]
    private void SyncRecalculateCustomers(string empireUid)
    {
        if (!EntitySelector.TryGetEmpire(empireUid, out var empire) || empire is null)
        {
            throw new InvalidOperationException($"No empire with uid {empireUid}");
        }

        empire.RecalculateCustomersAndIncome();
    }

    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RequestAddAreaOfInfluence(Vector2I centerTilePosition, int influenceAmount, int radius = 1)
    {
        if (!Multiplayer.IsServer())
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

        HashSet<long> peersWhosePolygonsNeedUpdating = new();

        for (var yOffset = -radius; yOffset <= radius; yOffset++)
        {
            for (var xOffset = -radius; xOffset <= radius; xOffset++)
            {
                var tilePos = centerTilePosition + new Vector2I(xOffset, yOffset);

                if (!TileGrid.IsTileInBounds(tilePos)) continue;

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

                List<long> peersSharingHighestInfluence = new();
                var highestInfluence = 0;

                foreach (var (peerId, influence) in playerInfluencesDict)
                {
                    if (influence > highestInfluence)
                    {
                        highestInfluence = influence;
                        peersSharingHighestInfluence.Clear();
                        peersSharingHighestInfluence.Add(peerId);
                    }
                    else if (influence == highestInfluence)
                    {
                        peersSharingHighestInfluence.Add(peerId);
                    }
                }

                long topInfluencer = 0;

                // Only randomize real owner when the it's relevant for the requester.
                // Otherwise the ambiguity between players can resolve differently
                // when another party changes their own influence over this tile even if
                // the influence wouldn't be significant.
                if (peersSharingHighestInfluence.Contains(requesterPeedId))
                {
                    var randomPeerIndex = Random.Shared.Next(0, peersSharingHighestInfluence.Count);
                    topInfluencer = peersSharingHighestInfluence[randomPeerIndex];
                }

                long oldTopInfluencer = 0;

                if (!topTileInfluencers.ContainsKey(tilePos))
                {
                    topTileInfluencers.Add(tilePos, 0);
                }
                else
                {
                    oldTopInfluencer = topTileInfluencers[tilePos];

                    if (topInfluencer == 0)
                    {
                        topInfluencer = oldTopInfluencer;
                    }
                }

                topTileInfluencers[tilePos] = topInfluencer;

                var oldTopIsNewTop = oldTopInfluencer == topInfluencer;

                if (oldTopInfluencer > 0 && !oldTopIsNewTop)
                {
                    peerTopInfluenceTiles[oldTopInfluencer].Remove(tilePos);
                    peersWhosePolygonsNeedUpdating.Add(oldTopInfluencer);
                }

                if (!peerTopInfluenceTiles.ContainsKey(topInfluencer))
                {
                    peerTopInfluenceTiles.Add(topInfluencer, new());
                }

                if (!peerTopInfluenceTiles[topInfluencer].Contains(tilePos))
                {
                    peerTopInfluenceTiles[topInfluencer].Add(tilePos);
                    peersWhosePolygonsNeedUpdating.Add(topInfluencer);
                }

                // TODO: Consider whether it would be better to RPC all updated tiles at once instead
                Rpc(MethodName.SyncTileInfluenceForPeer,
                    requesterPeedId,                        // targetPeerId
                    tilePos,                                // tilePosition
                    playerInfluencesDict[requesterPeedId],  // targetPeerInfluence
                    topInfluencer                           // topInfluencerId
                );
            }
        }

        foreach (var peerId in peersWhosePolygonsNeedUpdating)
        {
            var empire = EmpireController.GetPeerEmpire(peerId);
            var empireColor = empire.EmpirePrimaryColor;
            empireColor.A = 0.65f;
            var influenceTiles = new Vector2[peerTopInfluenceTiles[peerId].Count];
            var i = 0;

            foreach (var topInfluenceTile in peerTopInfluenceTiles[peerId])
            {
                influenceTiles[i++] = (Vector2)topInfluenceTile;
            }

            polygonBuilder.SyncPeerInfluenceTiles(peerId, influenceTiles, empireColor);

            empire.RecalculateCustomersAndIncome();
            RpcId(empire.GetOwnerPeerId(), MethodName.SyncRecalculateCustomers, empire.EmpireUid);
        }
    }

    public HashSet<Vector2I> GetPeerTopInfluenceTiles(long peerId)
    {
        if (!peerTopInfluenceTiles.ContainsKey(peerId))
        {
            GD.PushWarning($"{Multiplayer.GetUniqueId()} says: Peer {peerId} doesn't have top influence tiles");
            return new();
        }

        return peerTopInfluenceTiles[peerId];
    }
}
