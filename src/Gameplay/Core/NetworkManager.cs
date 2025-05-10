namespace Misled.Gameplay.Core;
using Godot;
using Godot.Collections;

/// <summary>
/// Manages network connections, player data, and spawning of player characters.
/// </summary>
public partial class NetworkManager : Node {
    public static NetworkManager? Instance { get; private set; }

    [Signal]
    public delegate void PlayerConnectedEventHandler(int peerId, Dictionary<string, string> playerInfo);
    [Signal]
    public delegate void PlayerDisconnectedEventHandler(int peerId);

    [Export]
    public Dictionary<string, PackedScene> CharacterScenes = [];

    public string DefaultServer { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 31415;

    private Dictionary<long, Dictionary<string, string>> _players = [];

    private Dictionary<string, string> _playerInfo = new()
    {
        { "Name", "Demo" },
        { "Hyprs", "Osage" },
    };

    /// <summary>
    /// Initializes the NetworkManager, setting up signals and instance.
    /// </summary>
    public override void _Ready() {
        Instance = this;

        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += OnPlayerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += () => Multiplayer.MultiplayerPeer = null;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    /// <summary>
    /// Starts a server.
    /// </summary>
    /// <returns>Error.Ok if successful, otherwise an error code.</returns>
    public Error Host() {
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(Port, 20);
        if (error != Error.Ok) {
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        _players[1] = _playerInfo;
        EmitSignal(SignalName.PlayerConnected, 1, _playerInfo);
        return Error.Ok;
    }

    /// <summary>
    /// Connects to a server.
    /// </summary>
    /// <returns>Error.Ok if successful, otherwise an error code.</returns>
    public Error Join() {
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(DefaultServer, Port);
        if (error != Error.Ok) {
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        return Error.Ok;
    }

    /// <summary>
    /// Called when connected to a server.
    /// </summary>
    private void OnConnectedToServer() {
        var myId = Multiplayer.GetUniqueId();
        _players[myId] = _playerInfo;
        EmitSignal(SignalName.PlayerConnected, myId, _playerInfo);
    }

    /// <summary>
    /// Called when the server disconnects.
    /// </summary>
    private void OnServerDisconnected() {
        Multiplayer.MultiplayerPeer = null;
        _players.Clear();
    }

    /// <summary>
    /// Called when a player connects.
    /// </summary>
    /// <param name="id">The ID of the player that connected.</param>
    private void OnPlayerConnected(long id) =>
        RpcId(id, MethodName.RegisterPlayer, _playerInfo);

    /// <summary>
    /// Registers a new player.
    /// </summary>
    /// <param name="newPlayerInfo">The player's information.</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RegisterPlayer(Dictionary<string, string> newPlayerInfo) {
        var senderId = Multiplayer.GetRemoteSenderId();
        _players[senderId] = newPlayerInfo;
        EmitSignal(SignalName.PlayerConnected, senderId, newPlayerInfo);
    }

    /// <summary>
    /// Called when a player disconnects.
    /// </summary>
    /// <param name="id">The ID of the player that disconnected.</param>
    private void OnPlayerDisconnected(long id) {
        _players.Remove(id);
        EmitSignal(SignalName.PlayerDisconnected, id);
        RemovePlayerNode(id);
    }

    /// <summary>
    /// Removes the player node from the world.
    /// </summary>
    /// <param name="id">The ID of the player to remove.</param>
    private void RemovePlayerNode(long id) {
        var playerNode = GetTree().Root.GetNode($"World/Player_{id}");
        playerNode?.QueueFree();
    }

    /// <summary>
    /// Spawns a player.
    /// </summary>
    /// <param name="id">The ID of the player to spawn.</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SpawnPlayer(long id) {
        if (!_players.TryGetValue(id, out var playerData)) {
            return;
        }

        if (!playerData.TryGetValue("Hyprs", out var characterKey)) {
            return;
        }

        if (!CharacterScenes.TryGetValue(characterKey, out var scene)) {
            return;
        }

        if (scene.Instantiate() is not Node3D player) {
            return;
        }

        player.Name = $"Player_{id}";
        (player as CharacterBody3D)?.SetMultiplayerAuthority((int)id);

        var world = GetTree().Root.GetNode("World");
        world.AddChild(player);
    }

    /// <summary>
    /// Spawns all players.
    /// </summary>
    public void SpawnAllPlayers() {
        foreach (var id in _players.Keys) {
            Rpc(nameof(SpawnPlayer), id);
        }
    }

    /// <summary>
    /// Gets all players.
    /// </summary>
    /// <returns>A dictionary of all players.</returns>
    public Dictionary<long, Dictionary<string, string>> GetAllPlayers() => _players;

    /// <summary>
    /// Sets player information.
    /// </summary>
    /// <param name="key">The key of the information to set.</param>
    /// <param name="value">The value of the information to set.</param>
    public void SetPlayerInfo(string key, string value) => _playerInfo[key] = value;

    /// <summary>
    /// Gets player information.
    /// </summary>
    /// <param name="key">The key of the information to get.</param>
    /// <returns>The value of the information, or null if it doesn't exist.</returns>
    public string? GetPlayerInfo(string key) => _playerInfo.TryGetValue(key, out var value) ? value : null;
}
