using System.Collections.Generic;
using Godot;

public partial class LobbyController : Node
{
    [Export]
    private Control playerListControl;

    [Export]
    private Button backButton;
    [Export]
    private Button startGameButton;
    [Export]
    private Label clientInfoLabel;
    [Export]
    private Label buildInfoLabel;

    private Dictionary<long, Control> playerListEntries = new();
    private NotificationController notificationController = null!;

    public override void _Ready()
    {
        notificationController = GodotUtilities.FindNodeOfType<NotificationController>(GetTree().Root);
        backButton.Pressed += OnBackButtonPressed;

        if (Multiplayer.IsServer())
        {
            startGameButton.Pressed += OnStartGameButtonPressed;
        }
        else
        {
            startGameButton.Hide();
            clientInfoLabel.Show();
        }

        buildInfoLabel.Text = DebugUtility.GetBriefBuildInfoString();

        OnPlayerConnected(Multiplayer.GetUniqueId());
        var otherPeers = Multiplayer.GetPeers();

        // Run manually so that connecting clients show the correct initial player listing.
        // This is required because when a client initially joins, they request to join the game
        // instead of directly loading the lobby (server decides if client can join). During
        // the time the client waits for a response, the Multiplayer.PeerConnected events
        // fire. Therefore, a connecting client misses these events because they haven't loaded
        // the lobby yet.
        foreach (var peerId in otherPeers)
        {
            OnPlayerConnected(peerId);
        }

        SubscribeToMultiplayerEvents();
    }

    private void SubscribeToMultiplayerEvents()
    {
        MultiplayerController.Instance.PlayerConnected += OnPlayerConnected;
        MultiplayerController.Instance.PlayerDisconnected += OnPlayerDisconnected;
    }

    private void UnsubscribeFromMultiplayerEvents()
    {
        MultiplayerController.Instance.PlayerConnected -= OnPlayerConnected;
        MultiplayerController.Instance.PlayerDisconnected -= OnPlayerDisconnected;
    }

    private void OnBackButtonPressed()
    {
        if (Multiplayer.IsServer())
        {
            MultiplayerController.Instance.ShutdownServer();
        }
        else
        {
            MultiplayerController.Instance.DisconnectClient("Manually disconnected");
        }

        UnsubscribeFromMultiplayerEvents();
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

    private void OnPlayerConnected(long peerId)
    {
        var playerLabel = new Label();
        playerLabel.Text = peerId.ToString();
        playerListControl.AddChild(playerLabel);
        playerListEntries.Add(peerId, playerLabel);
    }

    private void OnPlayerDisconnected(long peerId)
    {
        playerListEntries[peerId].QueueFree();
        playerListEntries.Remove(peerId);
    }

    private void OnStartGameButtonPressed()
    {
        if (Multiplayer.IsServer())
        {
            Rpc(MethodName.LoadGameScene);
        }
    }

    [Rpc(CallLocal = true)]
    private void LoadGameScene()
    {
        notificationController.ShowNotificationToast("Game started", "Server started the game", 5);
        UnsubscribeFromMultiplayerEvents();
        MultiplayerController.Instance.MarkGameStarted();
        GetTree().ChangeSceneToFile("res://scenes/Game.tscn");
    }
}
