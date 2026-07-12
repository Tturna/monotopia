using System;
using System.Collections.Generic;
using Godot;

public partial class TurnSystem : Node2D
{
    public delegate void TurnStartedHandler();
    public event TurnStartedHandler TurnStarted;

    public static TurnSystem Instance;

    private int turnTimeSeconds = 15;
    private float turnTimer;
    private int turnCount;
    private bool startingNewTurn;
    private List<long> playersThatEndedTurn = new();

    public override void _EnterTree()
    {
        if (TurnSystem.Instance is not null)
        {
            QueueFree();
            return;
        }

        Instance = this;
    }

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            Rpc(MethodName.StartNextTurn, turnCount + 1, turnTimeSeconds);
        }

        UIController.Instance.RegisterEndTurnButtonCallback(() =>
        {
            UIController.Instance.SetTurnEnded(true);

            if (Multiplayer.IsServer())
            {
                SetPlayerEndedTurn(1);
            }
            else
            {
                RpcId(1, MethodName.SetPlayerEndedTurn, Multiplayer.GetUniqueId());
            }
        });
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey keyEvent) return;
        if (keyEvent.Keycode != Key.M) return;
        if (!keyEvent.Pressed) return;

        turnTimer = 1;
        turnCount = Random.Shared.Next(100);
    }

    public override void _Process(double delta)
    {
        if (turnTimer <= 0) return;

        UIController.Instance.SetTurnTimerText(turnTimer);

        turnTimer -= (float)delta;

        if (Multiplayer.IsServer() && turnTimer <= 0 && !startingNewTurn)
        {
            startingNewTurn = true;
            Rpc(MethodName.StartNextTurn, turnCount + 1, turnTimeSeconds);
        }
    }

    [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void StartNextTurn(int nextTurnNumber, int nextTurnTimeSeconds)
    {
        turnTimer = nextTurnTimeSeconds;
        turnCount = nextTurnNumber;
        UIController.Instance.SetTurnCountText(turnCount);
        DebugUtility.Print($"Starting turn {turnCount}");
        TurnStarted?.Invoke();
        UIController.Instance.SetTurnEnded(false);

        if (Multiplayer.IsServer())
        {
            startingNewTurn = false;
            playersThatEndedTurn.Clear();
        }
    }

    /// <summary>
    /// This should only be called on the server. Use RpcId with peer ID 1
    /// instead of Rpc.
    /// </summary>
    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void SetPlayerEndedTurn(long peerId)
    {
        if (!Multiplayer.IsServer())
        {
            throw new InvalidOperationException("SetPlayerEndedTurn called on a client. Use RpcId instead of Rpc.");
        }

        // +1 because server is not counted in GetPeers(). Assume server is a player.
        var totalPlayerCount = Multiplayer.GetPeers().Length + 1; 
        playersThatEndedTurn.Add(peerId);

        if (playersThatEndedTurn.Count == totalPlayerCount)
        {
            Rpc(MethodName.StartNextTurn, turnCount + 1, turnTimeSeconds);
        }
    }
}
