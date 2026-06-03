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

    private Dictionary<long, Control> playerListEntries = new();

    public override void _Ready()
    {
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

        OnPlayerConnected(Multiplayer.GetUniqueId());
        MultiplayerController.Instance.PlayerConnected += OnPlayerConnected;
        MultiplayerController.Instance.PlayerDisconnected += OnPlayerDisconnected;
    }

    private void OnBackButtonPressed()
    {
        if (Multiplayer.IsServer())
        {
            MultiplayerController.Instance.ShutdownServer();
        }
        else
        {
            MultiplayerController.Instance.DisconnectClient();
        }

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
        GD.Print("Start game!!!");
    }
}
