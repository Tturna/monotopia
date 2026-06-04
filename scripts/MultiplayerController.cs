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

    public override void _EnterTree()
    {
        if (Instance is not null)
        {
            QueueFree();
            return;
        }

        Instance = this;
    }

    public bool InitializeServer(string? listenAddress = null, int listenPort = DefaultServerListenPort)
    {
        var serverPeer = new ENetMultiplayerPeer();
        serverPeer.SetBindIP(listenAddress);
        var errorStatus = serverPeer.CreateServer(listenPort);

        if (errorStatus == Error.Ok)
        {
            Multiplayer.MultiplayerPeer = serverPeer;
            DebugUtility.Print($"Server started. Listening on {listenAddress}:{listenPort}");
            SubscribeToMultiplayerEvents();
            isMultiplayerPeerActive = true;

            return true;
        }
        else
        {
            DebugUtility.Print($"Failed to start server. Status: {errorStatus.ToString()}");

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
            DebugUtility.Print($"Client created. Target server: {address}:{port}. ID: {Multiplayer.GetUniqueId()}");
            isMultiplayerPeerActive = true;

            return true;
        }
        else
        {
            DebugUtility.Print($"Status when creating client: {errorStatus.ToString()}");

            return false;
        }
    }

    public void ShutdownServer()
    {
        if (!IsConnectedToMultiplayer()) return;

        DebugUtility.Print("Shutting down server");
        Rpc(MethodName.NotifyServerShutdown);
        DisconnectMultiplayer();
    }

    public void DisconnectClient()
    {
        if (!IsConnectedToMultiplayer()) return;

        DebugUtility.Print($"Disconnecting client {Multiplayer.GetUniqueId()}");
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
        DebugUtility.Print($"Peer {peerId} notified that it will disconnect");
    }

    // Called on clients
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void NotifyServerShutdown()
    {
        DebugUtility.Print("Server notified that it is shutting down");
        DisconnectClient();
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
        DebugUtility.Print($"Peer {Multiplayer.GetUniqueId()} says: connected to server.");
    }

    // Only called on clients
    // I think this can be called really late too, but assuming the server shut down
    // notification works and the client disconnects its multiplayer system, this
    // would not be called at all. This would be called when the server shuts down
    // unexpectedly for example.
    private void OnServerDisconnected()
    {
        DebugUtility.Print("Disconnected from server.");
        DisconnectMultiplayer();
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }

    // Only called on clients
    private void OnConnectionFailed()
    {
        DebugUtility.Print($"Peer {Multiplayer.GetUniqueId()} says: failed to connect server.");
    }
}
