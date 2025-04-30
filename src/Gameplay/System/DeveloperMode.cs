namespace Misled.Gameplay.System;
using Godot;

public partial class DeveloperMode : Window {
    [Export] public Button? Host;
    [Export] public Button? Join;
    [Export] public RichTextLabel? WindowTitle;
    [Export] public RichTextLabel? DebugTitle;
    [Export] public Node? NetworkManager;

    private NetworkManager? _networkManager;

    public override void _Ready() {
        var time = Time.GetUnixTimeFromSystem().ToString().Substring(12, 3);
        base._Ready();
        _networkManager = (NetworkManager)NetworkManager!;
        WindowTitle!.Text = $"#{time}";
        DebugTitle!.Text = $"Misled Developer Mode #{time}";
        Host!.Pressed += OnHostPressed;
        Join!.Pressed += OnJoinPressed;
    }

    private void OnHostPressed() => _networkManager!.Host();

    private void OnJoinPressed() => _networkManager!.Join("127.0.0.1");

    public override void _Process(double delta) {
    }
}
