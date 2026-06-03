using Godot;

public partial class MainMenuController : Node
{
	[Export]
	private Control mainButtonControl;
	[Export]
	private Button startServerButton;
	[Export]
	private Button connectClientButton;
	[Export]
	private Button settingsButton;
	[Export]
	private Button quitButton;

	[Export]
	private Control startServerControl;
	[Export]
	private LineEdit listenAddressLineEdit;
	[Export]
	private LineEdit listenPortLineEdit;
	[Export]
	private Button confirmStartServerButton;
	[Export]
	private Button backStartServerButton;

	[Export]
	private Control connectClientControl;
	[Export]
	private LineEdit connectAddressLineEdit;
	[Export]
	private LineEdit connectPortLineEdit;
	[Export]
	private Button confirmConnectButton;
	[Export]
	private Button backConnectClientButton;

	public override void _Ready()
	{
		startServerButton.Pressed += OnStartServerPressed;
		connectClientButton.Pressed += OnConnectClientPressed;
		confirmStartServerButton.Pressed += OnConfirmStartServerPressed;
		confirmConnectButton.Pressed += OnConfirmConnectClientPressed;
		backStartServerButton.Pressed += OnBackStartServerPressed;
		backConnectClientButton.Pressed += OnBackConnectClientPressed;
		settingsButton.Pressed += () => GD.Print("Settings button pressed");
		quitButton.Pressed += () => GetTree().Quit();

		if (MultiplayerController.TryGetPreferredListenIPv4Address(out var address))
		{
			listenAddressLineEdit.Text = address;
		}
		else
		{
			listenAddressLineEdit.Text = string.Empty;
		}

		listenPortLineEdit.PlaceholderText = MultiplayerController.DefaultServerListenPort.ToString();
		listenPortLineEdit.Text = MultiplayerController.DefaultServerListenPort.ToString();
		connectPortLineEdit.PlaceholderText = MultiplayerController.DefaultServerListenPort.ToString();
		connectPortLineEdit.Text = MultiplayerController.DefaultServerListenPort.ToString();

		listenPortLineEdit.TextChanged += _ => EnsureValidPortText(listenPortLineEdit);
		connectPortLineEdit.TextChanged += _ => EnsureValidPortText(connectPortLineEdit);
	}

	private void EnsureValidPortText(LineEdit portLineEdit)
	{
		var text = portLineEdit.Text;

		if (text.Length == 0) return;

		if (!int.TryParse(text, out var _) || text.Contains("-") || text.Contains("+"))
		{
			portLineEdit.DeleteCharAtCaret();
		}
	}

	private void OnStartServerPressed()
	{
		mainButtonControl.Hide();
		startServerControl.Show();
	}

	private void OnConnectClientPressed()
	{
		mainButtonControl.Hide();
		connectClientControl.Show();
	}

	private void OnConfirmStartServerPressed()
	{
		// TODO: Validate address

		if (!int.TryParse(listenPortLineEdit.Text, out var port))
		{
			GD.PushWarning($"Given port is not a string. Got: {listenPortLineEdit.Text}");
			return;
		}

		var address = listenAddressLineEdit.Text;

		GD.Print($"Confirmed server start with address {address} and port {port}");
		var startSucceeded = MultiplayerController.Instance.InitializeServer(address, port);

		if (startSucceeded)
		{
			GetTree().ChangeSceneToFile("res://scenes/Lobby.tscn");
		}
	}

	private void OnConfirmConnectClientPressed()
	{
		// TODO: Validate address

		if (!int.TryParse(connectPortLineEdit.Text, out var port))
		{
			GD.PushWarning($"Given port is not a string. Got: {connectPortLineEdit.Text}");
			return;
		}

		var address = connectAddressLineEdit.Text;
		GD.Print($"Confirmed connect to address {address} and port {port}");
		var connectSucceeded = MultiplayerController.Instance.InitializeClient(address, port);

		if (connectSucceeded)
		{
			GetTree().ChangeSceneToFile("res://scenes/Lobby.tscn");
		}
	}

	private void OnBackStartServerPressed()
	{
		startServerControl.Hide();
		mainButtonControl.Show();
	}

	private void OnBackConnectClientPressed()
	{
		connectClientControl.Hide();
		mainButtonControl.Show();
	}
}
