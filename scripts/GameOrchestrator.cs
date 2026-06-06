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
			var r = (float)Random.Shared.NextDouble();
			var g = (float)Random.Shared.NextDouble();
			var b = (float)Random.Shared.NextDouble();
			var empirePrimaryColor = new Color(r, g, b);

			if (peerId == 1)
			{
				empire.IsPlayerEmpire = true;
			}

			var empireUid = Guid.NewGuid().ToString();
			var capitalUid = Guid.NewGuid().ToString();

			empire.EmpireUid = empireUid;
			empire.EmpirePrimaryColor = empirePrimaryColor;
			empiresParent.AddChild(empire, forceReadableName: true);
			empire.AddNewCityToEmpire(capitalCityTilePosition, capitalUid);
			playerEmpires.Add(peerId, empire);

			Rpc(
				MethodName.SyncCreateEmpire,
				peerId,
				empireUid,
				capitalCityTilePosition,
				empirePrimaryColor,
				capitalUid);
		}
	}
	
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
	private void SyncCreateEmpire(
		long empireOwnerPeerId,
		string empireUid,
		Vector2I capitalCityTilePosition,
		Color empirePrimaryColor,
		string capitalCityUid)
	{
		var peerId = Multiplayer.GetUniqueId();
		DebugUtility.Print($"Peer {peerId} says: Sync creating empire for peer {empireOwnerPeerId}");

		var empire = (EmpireController)empireScene.Instantiate();

		if (empireOwnerPeerId == peerId)
		{
			empire.IsPlayerEmpire = true;
		}

		empire.EmpireUid = empireUid;
		empire.EmpirePrimaryColor = empirePrimaryColor;
		empiresParent.AddChild(empire, forceReadableName: true);
		empire.AddNewCityToEmpire(capitalCityTilePosition, capitalCityUid);
	}

	public EmpireController? GetEmpireForPeerId(long peerId)
	{
		if (!playerEmpires.TryGetValue(peerId, out var empire))
		{
			return null;
		}

		return empire;
	}
}
