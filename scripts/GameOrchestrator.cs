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

	private UIController uiController = null!;
	
	public override void _Ready()
	{
		var rootNode = GetTree().Root;
		uiController = GodotUtilities.FindNodeOfType<UIController>(rootNode);
		var turnSystem = GodotUtilities.FindNodeOfType<TurnSystem>(rootNode);
		ConnectTurnSystemToUI(turnSystem);

		if (!Multiplayer.IsServer()) return;

		turnSystem.TurnStarted += OnTurnStarted;

		var allPeerIds = new List<int>(Multiplayer.GetPeers());
		allPeerIds.Add(1);

		Dictionary<EmpireController, Vector2I> playerSpawnTilePositionsDict = new();

		foreach (var peerId in allPeerIds)
		{
			if (!TileGrid.TryGetPlayerTileSpawnPoint(out var playerSpawnTilePosition))
			{
				throw new InvalidOperationException("No player spawn points left");
			}
			
			var empire = (EmpireController)empireScene.Instantiate();
			var r = (float)Random.Shared.NextDouble();
			var g = (float)Random.Shared.NextDouble();
			var b = (float)Random.Shared.NextDouble();
			var empirePrimaryColor = new Color(r, g, b);

			var isPlayerEmpire = peerId == 1;
			var empireUid = Guid.NewGuid().ToString();
			var capitalUid = Guid.NewGuid().ToString();

			ConnectEmpireToUI(empire);
			ConnectEmpireToOrchestrator(empire);
			empire.InitializeEmpire(peerId, empireUid, empirePrimaryColor, isPlayerEmpire);
			empiresParent.AddChild(empire, forceReadableName: true);
			EntitySelector.SetEmpire(empireUid, empire);
			playerSpawnTilePositionsDict.Add(empire, playerSpawnTilePosition);

			if (isPlayerEmpire)
			{
				var inputController = GodotUtilities.FindNodeOfType<PlayerInputController>(GetTree().Root);
				inputController.SetTargetEmpire(empire);
			}

			Rpc(
				MethodName.SyncCreateEmpire,
				peerId,
				empireUid,
				playerSpawnTilePosition,
				empirePrimaryColor,
				capitalUid);
		}

		foreach (var (empire, playerSpawnTilePosition) in playerSpawnTilePositionsDict)
		{
			UnitSpawner.Instance.SpawnAndSyncUnit(
				unitType: BuildController.BuildableItemType.Founder,
				tilePosition: playerSpawnTilePosition,
				ownerEmpire: empire);
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

		ConnectEmpireToUI(empire);
		empire.InitializeEmpire(empireOwnerPeerId, empireUid, empirePrimaryColor, isPlayerEmpire);
		empiresParent.AddChild(empire, forceReadableName: true);
		EntitySelector.SetEmpire(empireUid, empire);

		if (isPlayerEmpire)
		{
			var inputController = GodotUtilities.FindNodeOfType<PlayerInputController>(GetTree().Root);
			inputController.SetTargetEmpire(empire);
		}
	}

	private void OnTurnStarted(int turn)
	{
		SyncAllEmpireCoins(updateBalance: true);
	}

	private void ConnectEmpireToOrchestrator(EmpireController empire)
	{
		if (!Multiplayer.IsServer())
		{
			throw new InvalidOperationException("Don't connect the game orchestrator to empires on clients. Server should do all the syncing.");
		}

		empire.CityAnnexed += () => SyncAllEmpireCoins();
	}

	private void ConnectEmpireToUI(EmpireController empire)
	{
		empire.SelectionChanged += uiController.OnEntitySelectionChanged;
		empire.CoinsUpdated += uiController.SetCoinBalanceText;
		empire.GameEnded += uiController.ShowGameEndedOverlay;
		empire.UnitMovementPathUpdated += uiController.SetUnitMovementPathPoints;
	}

	private void ConnectTurnSystemToUI(TurnSystem turnSystem)
	{
		turnSystem.TurnStarted += uiController.OnTurnStarted;
		turnSystem.TurnTimerUpdated += uiController.SetTurnTimerText;
		uiController.EndTurnButtonPressed += turnSystem.OnEndTurnButtonPressed;
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
