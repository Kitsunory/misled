namespace Misled.Gameplay.Core;

using System;
using Godot;
using Godot.Collections;

public partial class LobbyManager : Control {
    [Export] public TextEdit? PlayerName;
    [Export] public TextEdit? Code;
    [Export] public TextEdit? IP;
    [Export] public OptionButton? Hyprs;
    [Export] public Button? Host;
    [Export] public Button? Join;
    [Export] public Node3D? Subpoint1;
    [Export] public Node3D? Subpoint2;
    [Export] public Node3D? Subpoint3;
    [Export] public Node3D? Subpoint4;
    [Export] public Node? NetworkManager;
    private NetworkManager? _networkManager;

    private Node3D[] _spawnPoints = [];
    private readonly Dictionary<long, Node3D> _figurines = [];

    public override void _Ready() {
        _networkManager = (NetworkManager)NetworkManager!;
        _spawnPoints = [
            Subpoint1 ?? throw new ArgumentNullException(nameof(Subpoint1), "Subpoint1 is not assigned."),
            Subpoint2 ?? throw new ArgumentNullException(nameof(Subpoint2), "Subpoint2 is not assigned."),
            Subpoint3 ?? throw new ArgumentNullException(nameof(Subpoint3), "Subpoint3 is not assigned."),
            Subpoint4 ?? throw new ArgumentNullException(nameof(Subpoint4), "Subpoint4 is not assigned.")
        ];

        Host!.Pressed += OnHostPressed;
        Join!.Pressed += OnJoinPressed;

        // Subscribe to C# events
        _networkManager.PlayerConnected += OnPlayerConnected;
        _networkManager.PlayerDisconnected += OnPlayerDisconnected;
    }

    private async void OnHostPressed() {
        var name = PlayerName!.Text.Trim();
        var hyprs = Hyprs!.GetItemText(Hyprs.GetSelectedId());
        var validPort = int.TryParse(Code!.Text, out var port);
        var ip = IP!.Text.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(hyprs) || !validPort || string.IsNullOrEmpty(ip)) {
            GD.Print("Invalid input. Please fill all fields correctly (´･ω･`)");
            return;
        }

        _networkManager!.Address = ip;
        _networkManager.Port = port;
        _networkManager.SetPlayerInfo("Name", name);
        _networkManager.SetPlayerInfo("Hyprs", hyprs);

        GD.Print($"Trying to connect to {ip}:{port} as '{name}' using {hyprs} (≧◡≦)");

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var error = _networkManager.Host();
        GD.Print(error);

        // Lock inputs after success
        PlayerName.Editable = false;
        Hyprs.Disabled = true;
        Code.Editable = false;

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        Host!.Text = "Start";


        Join!.Visible = false;
        Join!.Disabled = true;
        Join!.Pressed -= OnJoinPressed;
        Host!.Pressed -= OnHostPressed;
        Host!.Pressed += Multiplayer.IsServer() ? OnStartMatch : OnLeaveRoom;
    }

    private async void OnJoinPressed() {
        var name = PlayerName!.Text.Trim();
        var hyprs = Hyprs!.GetItemText(Hyprs.GetSelectedId());
        var validPort = int.TryParse(Code!.Text, out var port);
        var ip = IP!.Text.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(hyprs) || !validPort || string.IsNullOrEmpty(ip)) {
            GD.Print("Invalid input. Please fill all fields correctly (´･ω･`)");
            return;
        }

        _networkManager!.Address = ip;
        _networkManager.Port = port;
        _networkManager.SetPlayerInfo("Name", name);
        _networkManager.SetPlayerInfo("Hyprs", hyprs);

        GD.Print($"Trying to connect to {ip}:{port} as '{name}' using {hyprs} (≧◡≦)");

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var error = _networkManager.Join();
        GD.Print(error);

        // Lock inputs after success
        PlayerName.Editable = false;
        Hyprs.Disabled = true;
        Code.Editable = false;

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        Host!.Visible = false;
        Host!.Disabled = true;
        Host!.Pressed -= OnHostPressed;
        Join!.Visible = false;
        Join!.Disabled = true;
        Join!.Pressed -= OnJoinPressed;
    }

    private void OnPlayerConnected(long id, Dictionary<string, string> info) {
        if (!_figurines.ContainsKey(id)) {
            var index = _figurines.Count;
            if (index >= _spawnPoints.Length) {
                return;
            }

            var spawnPoint = _spawnPoints[index];
            var hyprs = info["Hyprs"];
            var scenePath = $"res://src/Menu/{hyprs}.tscn";

            if (GD.Load(scenePath) is not PackedScene figurineScene) {
                return;
            }

            var instance = figurineScene.Instantiate<Node3D>();
            instance.Name = $"Figurine_{id}";
            spawnPoint.AddChild(instance);
            _figurines[id] = instance;
        }
    }

    private void OnPlayerDisconnected(long id) {
        if (_figurines.TryGetValue(id, out var node)) {
            node.QueueFree();
            _figurines.Remove(id);
        }
    }

    private void OnStartMatch() {
        _networkManager!.SpawnAllPlayers();
        Rpc(nameof(DisableMenu));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void DisableMenu() {
        Visible = false;
        foreach (var subpoint in _spawnPoints) {
            subpoint.Visible = false;
        }
    }

    private void OnLeaveRoom() {
        GD.Print("Client left the room (ﾉ>ω<)ﾉ :｡･:*:･ﾟ’★");
        GetTree().ReloadCurrentScene();
    }
}
