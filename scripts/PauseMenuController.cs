using Godot;

public partial class PauseMenuController : Node
{
    [Export]
    private Control pauseMenuControl;
    [Export]
    private Button continueButton;
    [Export]
    private Button settingsButton;
    [Export]
    private Button disconnectButton;

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey keyEvent) return;
        if (!keyEvent.Pressed) return;
        if (keyEvent.Keycode != Key.Escape) return;

        if (pauseMenuControl.Visible)
        {
            TogglePauseMenu(false);
        }
        else
        {
            TogglePauseMenu(true);
        }
    }

    public override void _Ready()
    {
        continueButton.Pressed += OnContinuePressed;
        settingsButton.Pressed += OnSettingsPressed;
        disconnectButton.Pressed += OnDisconnectPressed;

        if (Multiplayer.IsServer())
        {
            disconnectButton.Text = "Shut down server";
        }
        else
        {
            disconnectButton.Text = "Disconnect";
        }
    }

    private void TogglePauseMenu(bool visible)
    {
        if (visible)
        {
            pauseMenuControl.Show();
        }
        else
        {
            pauseMenuControl.Hide();
        }
    }

    private void OnContinuePressed()
    {
        TogglePauseMenu(false);
    }

    private void OnSettingsPressed()
    {
        GD.Print("Clicked on pause menu settings button");
    }

    private void OnDisconnectPressed()
    {
        TogglePauseMenu(false);

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
}
