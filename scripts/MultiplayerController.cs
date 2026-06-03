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
            GD.Print($"Server started. Listening on {listenAddress}:{listenPort}");
            SubscribeToMultiplayerEvents();
            isMultiplayerPeerActive = true;

            return true;
        }
        else
        {
            GD.Print($"Failed to start server. Status: {errorStatus.ToString()}");

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
            GD.Print($"Client created. Target server: {address}:{port}. ID: {Multiplayer.GetUniqueId()}");
            isMultiplayerPeerActive = true;

            return true;
        }
        else
        {
            GD.Print($"Status when creating client: {errorStatus.ToString()}");

            return false;
        }
    }

    public void ShutdownServer()
    {
        // TODO: Notify all clients that server is shutting down
        Multiplayer.MultiplayerPeer = null;
        isMultiplayerPeerActive = false;
        GD.Print("Server shut down");
        UnsubscribeFromMultiplayerEvents();
    }

    public void DisconnectClient()
    {
        // TODO: Notify server that client disconnected
        GD.Print($"Disconnecting client {Multiplayer.GetUniqueId()}");
        Multiplayer.MultiplayerPeer = null;
        isMultiplayerPeerActive = false;
        UnsubscribeFromMultiplayerEvents();
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

    private void OnPeerConnected(long id)
    {
        GD.Print($"Peer {Multiplayer.GetUniqueId()} says: peer {id} connected.");
        PlayerConnected?.Invoke(id);
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print($"Peer {Multiplayer.GetUniqueId()} says: peer {id} disconnected.");
        PlayerDisconnected?.Invoke(id);
    }

    private void OnConnectedToServer()
    {
        GD.Print($"Peer {Multiplayer.GetUniqueId()} says: connected to server.");
    }

    private void OnServerDisconnected()
    {
        GD.Print("Disconnected from server. Multiplayer peer inactive.");
        isMultiplayerPeerActive = false;
        UnsubscribeFromMultiplayerEvents();
    }

    private void OnConnectionFailed()
    {
        GD.Print($"Peer {Multiplayer.GetUniqueId()} says: failed to connect server.");
    }
}
