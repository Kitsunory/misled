namespace Misled.Gameplay.System;
using Godot;

public partial class NetworkManager : Node {
    [Export] public PackedScene? TangerScene;

    public void Host() {
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(31415);
        Multiplayer.MultiplayerPeer = peer;
        GD.Print("Hosting...");
        Multiplayer.PeerConnected += OnPeerConnected;
        SpawnPlayer(Multiplayer.GetUniqueId());
    }

    public void Join(string ip) {
        var peer = new ENetMultiplayerPeer();
        peer.CreateClient(ip, 31415);
        Multiplayer.MultiplayerPeer = peer;
        GD.Print("Joining server...");

        Multiplayer.ConnectedToServer += OnConnectedToServer;
    }

    private void OnConnectedToServer() {
        GD.Print("Connected to server!");
        RpcId(1, nameof(RequestSpawn), Multiplayer.GetUniqueId());
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestSpawn(long id) {
        GD.Print($"Spawn requested by peer {id}");
        SpawnPlayer(id);

        foreach (var peerId in Multiplayer.GetPeers()) {
            if (peerId != id) {
                RpcId(id, nameof(SpawnPlayer), peerId);
            }
        }
    }

    private void OnPeerConnected(long id) {
        GD.Print($"Player {id} connected");
        Rpc(nameof(SpawnPlayer), id);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SpawnPlayer(long id) {
        if (TangerScene == null) { return; }
        var player = TangerScene.Instantiate() as Node3D;
        player!.Name = $"Player_{id}";
        GetTree().Root.GetNode("World").AddChild(player);
        (player as CharacterBody3D)!.SetMultiplayerAuthority((int)id);
    }
}
