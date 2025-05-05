namespace Misled.Gameplay.System;
using Godot;
using Godot.Collections;

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

    public override void _Ready() {
        Instance = this;

        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += OnPlayerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += () => Multiplayer.MultiplayerPeer = null;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

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

    public Error Join() {
        GD.Print("Y");
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(DefaultServer, Port);
        if (error != Error.Ok) {
            GD.Print("No");
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        return Error.Ok;
    }

    private void OnConnectedToServer() {
        var myId = Multiplayer.GetUniqueId();
        _players[myId] = _playerInfo;
        EmitSignal(SignalName.PlayerConnected, myId, _playerInfo);
    }

    private void OnServerDisconnected() {
        Multiplayer.MultiplayerPeer = null;
        _players.Clear();
    }

    private void OnPlayerConnected(long id) =>
        RpcId(id, MethodName.RegisterPlayer, _playerInfo);

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RegisterPlayer(Dictionary<string, string> newPlayerInfo) {
        var senderId = Multiplayer.GetRemoteSenderId();
        _players[senderId] = newPlayerInfo;
        EmitSignal(SignalName.PlayerConnected, senderId, newPlayerInfo);
    }

    private void OnPlayerDisconnected(long id) {
        _players.Remove(id);
        EmitSignal(SignalName.PlayerDisconnected, id);
    }

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

    public void SpawnAllPlayers() {
        foreach (var id in _players.Keys) {
            Rpc(nameof(SpawnPlayer), id);
        }
    }

    public Dictionary<long, Dictionary<string, string>> GetAllPlayers() => _players;

    public void SetPlayerInfo(string key, string value) => _playerInfo[key] = value;

    public string? GetPlayerInfo(string key) => _playerInfo.TryGetValue(key, out var value) ? value : null;
}
