using System.Net;
using System.Net.Sockets;
using Godot;

#nullable enable
public partial class MultiplayerController : Node2D
{
    public static MultiplayerController Instance = null!;

    public const int DefaultServerListenPort = 12312;

    public delegate void PlayerConnectedHandler(long peerId);
    public event PlayerConnectedHandler? PlayerConnected;

    public delegate void PlayerDisconnectedHandler(long peerId);
    public event PlayerDisconnectedHandler? PlayerDisconnected;

    private bool isMultiplayerPeerActive;
    private bool isSubscribedToClientEvents;

    private NotificationController notificationController = null!;

    public override void _EnterTree()
    {
        if (Instance is not null)
        {
            QueueFree();
            return;
        }

        Instance = this;
    }

    public override void _Ready()
    {
        notificationController = GodotUtilities.FindNodeOfType<NotificationController>(GetTree().Root);
    }

    public bool InitializeServer(string? listenAddress = null, int listenPort = DefaultServerListenPort)
    {
        var serverPeer = new ENetMultiplayerPeer();
        serverPeer.SetBindIP(listenAddress);
        var errorStatus = serverPeer.CreateServer(listenPort);

        if (errorStatus == Error.Ok)
        {
            Multiplayer.MultiplayerPeer = serverPeer;
            SubscribeToMultiplayerEvents();
            isMultiplayerPeerActive = true;
            var toastBody = $"Listening on {listenAddress}:{listenPort}";
            notificationController.ShowNotificationToast("Server started", toastBody, 5);

            return true;
        }
        else
        {
            var toastBody = $"Status: {errorStatus.ToString()}";
            notificationController.ShowNotificationToast("Failed to start server", toastBody, 5);

            return false;
        }
    }

    public bool InitializeClient(string address, int port = DefaultServerListenPort)
    {
        var clientPeer = new ENetMultiplayerPeer();
        var errorStatus = clientPeer.CreateClient(address, port);

        if (errorStatus == Error.Ok)
        {
            Multiplayer.MultiplayerPeer = clientPeer;
            SubscribeToMultiplayerEvents();
            isMultiplayerPeerActive = true;
            var toastBody = $"Target server: {address}:{port}. ID: {Multiplayer.GetUniqueId()}";
            notificationController.ShowNotificationToast("Client created", toastBody, 5);

            return true;
        }
        else
        {
            var toastBody = $"Status: {errorStatus.ToString()}";
            notificationController.ShowNotificationToast("Failed to create client", toastBody, 5);

            return false;
        }
    }

    public void ShutdownServer()
    {
        if (!IsConnectedToMultiplayer()) return;

        notificationController.ShowNotificationToast("Server shut down", "Manually stopped the server", 5);

        Rpc(MethodName.NotifyServerShutdown);
        DisconnectMultiplayer();
    }

    public void DisconnectClient(string reason)
    {
        if (!IsConnectedToMultiplayer()) return;

        notificationController.ShowNotificationToast("Disconnecting client", reason, 5);
        Rpc(MethodName.NotifyPlayerDisconnect, Multiplayer.GetUniqueId());
        DisconnectMultiplayer();
    }

    public static bool TryGetPreferredListenIPv4Address(out string address)
    {
        address = string.Empty;

        // See this Stack Overflow comment:
        // https://stackoverflow.com/a/27376368
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;

                if (endPoint is not null)
                {
                    address = endPoint.Address.ToString();
                }

                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private void DisconnectMultiplayer()
    {
        if (!IsConnectedToMultiplayer()) return;

        Multiplayer.MultiplayerPeer = null;
        isMultiplayerPeerActive = false;
        UnsubscribeFromMultiplayerEvents();
    }

    // Called on the server
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void NotifyPlayerDisconnect(long peerId)
    {
        notificationController.ShowNotificationToast("Client disconnected", $"Peer {peerId} disconnected", 5);
    }

    // Called on clients
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void NotifyServerShutdown()
    {
        DisconnectClient("Server shutting down");
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

    private void SubscribeToMultiplayerEvents()
    {
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        if (!Multiplayer.IsServer())
        {
            isSubscribedToClientEvents = true;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ServerDisconnected += OnServerDisconnected;
            Multiplayer.ConnectionFailed += OnConnectionFailed;
        }
    }

    private void UnsubscribeFromMultiplayerEvents()
    {
        Multiplayer.PeerConnected -= OnPeerConnected;
        Multiplayer.PeerDisconnected -= OnPeerDisconnected;

        if (isSubscribedToClientEvents)
        {
            isSubscribedToClientEvents = false;
            Multiplayer.ConnectedToServer -= OnConnectedToServer;
            Multiplayer.ServerDisconnected -= OnServerDisconnected;
            Multiplayer.ConnectionFailed -= OnConnectionFailed;
        }
    }

    private bool IsConnectedToMultiplayer()
    {
        return isMultiplayerPeerActive && Multiplayer.MultiplayerPeer != null;
    }

    private void OnPeerConnected(long id)
    {
        DebugUtility.Print($"Peer {Multiplayer.GetUniqueId()} says: peer {id} connected.");
        PlayerConnected?.Invoke(id);
    }

    // Can be called really late
    private void OnPeerDisconnected(long id)
    {
        DebugUtility.Print($"Peer {Multiplayer.GetUniqueId()} says: peer {id} disconnected.");
        PlayerDisconnected?.Invoke(id);
    }

    // Only called on clients
    private void OnConnectedToServer()
    {
        notificationController.ShowNotificationToast("Connected to server", "Connection successful", 5);
    }

    // Only called on clients
    // I think this can be called really late too, but assuming the server shut down
    // notification works and the client disconnects its multiplayer system, this
    // would not be called at all. This would be called when the server shuts down
    // unexpectedly for example.
    private void OnServerDisconnected()
    {
        notificationController.ShowNotificationToast("Disconnected from server", "Unexpectedly disconnected from server", 5);
        DisconnectMultiplayer();
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

    // Only called on clients
    private void OnConnectionFailed()
    {
        notificationController.ShowNotificationToast("Connection failed", "Failed to connect to server", 5);
    }
}
