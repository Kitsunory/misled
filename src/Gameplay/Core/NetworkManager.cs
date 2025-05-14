namespace Misled.Gameplay.Core;

using System;
using Godot;
using Godot.Collections;

public partial class NetworkManager : Node {
    public static NetworkManager? Instance { get; private set; }

    public event Action<long, Dictionary<string, string>>? PlayerConnected;
    public event Action<long>? PlayerDisconnected;

    [Export]
    public Dictionary<string, PackedScene> CharacterScenes = [];

    public string Address { get; set; } = "127.0.0.1";
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
        PlayerConnected?.Invoke(1, _playerInfo);
        return Error.Ok;
    }

    public Error Join() {
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(Address, Port);
        if (error != Error.Ok) {
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        return Error.Ok;
    }

    private void OnConnectedToServer() {
        var myId = Multiplayer.GetUniqueId();
        _players[myId] = _playerInfo;
        PlayerConnected?.Invoke(myId, _playerInfo);

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
        PlayerConnected?.Invoke(senderId, newPlayerInfo);
    }

    private void OnPlayerDisconnected(long id) {
        _players.Remove(id);
        PlayerDisconnected?.Invoke(id);
        RemovePlayerNode(id);
    }

    private void RemovePlayerNode(long id) {
        var playerNode = GetTree().Root.GetNode($"World/{id}");
        playerNode?.QueueFree();
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

        player.Name = $"{id}";
        (player as CharacterBody3D)?.SetMultiplayerAuthority((int)id);

        var rng = new RandomNumberGenerator();
        rng.Randomize();

        var randomX = rng.RandfRange(0f, -70f);
        var randomZ = rng.RandfRange(0f, 70f);
        var currentY = 2.0f;

        player.GlobalTransform = new Transform3D(
            player.GlobalTransform.Basis,
            new Vector3(randomX, currentY, randomZ)
        );

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

    public bool IsServer() => Multiplayer.IsServer();
}
