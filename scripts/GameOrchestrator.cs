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

	public override void _EnterTree()
	{
		Instance = this;
	}
	
	public override void _Ready()
	{
		if (!Multiplayer.IsServer()) return;

		TurnSystem.Instance.TurnStarted += OnTurnStarted;

		var allPeerIds = new List<int>(Multiplayer.GetPeers());
		allPeerIds.Add(1);

		foreach (var peerId in allPeerIds)
		{
			if (!TileGrid.TryGetVillageTileSpawnPoint(out var capitalCityTilePosition))
			{
				throw new InvalidOperationException("No capital city spawn points left");
			}
			
			var empire = (EmpireController)empireScene.Instantiate();
			var r = (float)Random.Shared.NextDouble();
			var g = (float)Random.Shared.NextDouble();
			var b = (float)Random.Shared.NextDouble();
			var empirePrimaryColor = new Color(r, g, b);

			var isPlayerEmpire = peerId == 1;
			var empireUid = Guid.NewGuid().ToString();
			var capitalUid = Guid.NewGuid().ToString();

			empire.InitializeEmpire(peerId, empireUid, empirePrimaryColor, isPlayerEmpire);
			empiresParent.AddChild(empire, forceReadableName: true);
			empire.AddNewCityToEmpire(capitalCityTilePosition, capitalUid);
			EntitySelector.SetEmpire(empireUid, empire);

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
		var isPlayerEmpire = empireOwnerPeerId == peerId;
		var empire = (EmpireController)empireScene.Instantiate();
		EntitySelector.SetEmpire(empireUid, empire);

		empire.InitializeEmpire(empireOwnerPeerId, empireUid, empirePrimaryColor, isPlayerEmpire);
		empiresParent.AddChild(empire, forceReadableName: true);
		empire.AddNewCityToEmpire(capitalCityTilePosition, capitalCityUid);
	}

	private void OnTurnStarted()
	{
		SyncAllEmpireCoins(updateBalance: true);
	}

	public void SyncAllEmpireCoins(bool updateBalance = false)
	{
		foreach (var (_, empire) in EntitySelector.GetEmpiresDict())
		{
			var newBalance = empire.Coins;

			if (updateBalance)
			{
				newBalance += empire.TotalCoinIncome;
			}

			var newIncome = empire.TotalCoinIncome;
			empire.RequestSetCoinState(newBalance, newIncome);
		}
	}
}
