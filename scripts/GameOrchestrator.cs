using System;
using System.Collections.Generic;
using Godot;

#nullable enable
public partial class GameOrchestrator : Node2D
{
	[Export]
	private PackedScene empireScene = null!;
	[Export]
	private Node2D empiresParent = null!;
	
	public static GameOrchestrator Instance = null!;

	private Dictionary<long, EmpireController> playerEmpires = new();

	public override void _EnterTree()
	{
		if (GameOrchestrator.Instance is not null)
		{
			QueueFree();

			return;
		}

		Instance = this;
	}
	
	public override void _Ready()
	{
		if (!Multiplayer.IsServer()) return;

		var allPeerIds = new List<int>(Multiplayer.GetPeers());
		allPeerIds.Add(1);

		foreach (var peerId in allPeerIds)
		{
			bool isLocalPlayer = (peerId == Multiplayer.GetUniqueId());
			DebugUtility.Print($"Adding empire for peer {peerId}");
			
			if (!TileGrid.TryGetVillageTileSpawnPoint(out var capitalCityTilePosition))
			{
				throw new InvalidOperationException("No capital city spawn points left");
			}
			
			var empire = (EmpireController)empireScene.Instantiate();
			empiresParent.AddChild(empire);
			empire.AddNewCityToEmpire(capitalCityTilePosition);
			playerEmpires.Add(peerId, empire);
			Rpc(MethodName.SyncCreateEmpire, peerId, capitalCityTilePosition);
		}
	}
	
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	private void SyncCreateEmpire(long empireOwnerPeerId, Vector2I capitalCityTilePosition)
	{
		var peerId = Multiplayer.GetUniqueId();

		DebugUtility.Print($"{peerId} says: Creating empire for {empireOwnerPeerId}");
		var empire = (EmpireController)empireScene.Instantiate();
		empiresParent.AddChild(empire);
		empire.AddNewCityToEmpire(capitalCityTilePosition);

		if (empireOwnerPeerId == peerId)
		{
			empire.IsPlayerEmpire = true;
		}
	}
}
