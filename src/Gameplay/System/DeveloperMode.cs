namespace Misled.Gameplay.System;

using Godot;

public partial class DeveloperMode : Window {
    [Export] public Button? Host;
    [Export] public Button? Join;
    [Export] public Button? Output;
    [Export] public RichTextLabel? DebugTitle;
    [Export] public TextEdit? PlayerName;
    [Export] public Node? NetworkManager;

    private NetworkManager? _networkManager;

    public override void _Ready() {
        var time = Time.GetUnixTimeFromSystem().ToString().Substring(12, 3);
        base._Ready();
        _networkManager = (NetworkManager)NetworkManager!;
        DebugTitle!.Text = $"Misled Debug #{time}";
        Host!.Pressed += OnHostPressed;
        Join!.Pressed += OnJoinPressed;
        Output!.Pressed += OnOutputPressed;
    }

    private void OnOutputPressed() {
        GD.Print(_networkManager!.GetAllPlayers());
        _networkManager!.SpawnAllPlayers();
    }

    private void OnHostPressed() {
        _networkManager!.SetPlayerInfo("Name", PlayerName!.Text);
        _networkManager!.Host();
    }
    private void OnJoinPressed() {
        _networkManager!.SetPlayerInfo("Name", PlayerName!.Text);
        _networkManager!.Join("127.0.0.1");
    }
}
