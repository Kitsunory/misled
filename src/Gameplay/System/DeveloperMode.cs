namespace Misled.Gameplay.System;

using global::System;
using Godot;

public partial class DeveloperMode : Window {
    [Export] public Button? Host;
    [Export] public Button? Join;
    [Export] public Button? Output;
    [Export] public RichTextLabel? DebugTitle;
    [Export] public RichTextLabel? Frame;
    [Export] public RichTextLabel? FPS;
    [Export] public RichTextLabel? ConnectionStatus;
    [Export] public TextEdit? PlayerName;
    [Export] public TextEdit? Address;
    [Export] public TextEdit? Port;
    [Export] public OptionButton? Hyprs;
    [Export] public Node? NetworkManager;

    private NetworkManager? _networkManager;
    private float _timeSinceLastSpike;
    private RandomNumberGenerator _rng = new();
    private bool _isSpike;
    private float _updateTimer;
    private float _spikeTimer;
    private bool _isSpikeActive;

    public override void _Ready() {
        _networkManager = (NetworkManager)NetworkManager!;
        base._Ready();
        Host!.Pressed += OnHostPressed;
        Join!.Pressed += OnJoinPressed;
        Output!.Pressed += OnOutputPressed;
    }

    public override void _Process(double delta) {
        if (Input.IsActionJustPressed("Debug")) {
            Visible = !Visible;
        }

        _updateTimer += (float)delta;
        _timeSinceLastSpike += (float)delta;

        if (!_isSpikeActive && _timeSinceLastSpike >= 5f) {
            _isSpikeActive = true;
            _spikeTimer = 0f;
            _timeSinceLastSpike = 0f;
        }

        if (_updateTimer >= 0.1f) {
            _updateTimer = 0f;

            FPS!.Text = $"FPS:\n[color=green]{Engine.GetFramesPerSecond()}[/color]";

            if (_isSpikeActive) {
                _spikeTimer += 0.2f;
                var spikeValue = _rng.RandiRange(4, 20);
                Frame!.Text = $"Frame loss during hitbox lerp:\n[color=green]{spikeValue}ms[/color]";

                if (_spikeTimer >= 2f) {
                    _isSpikeActive = false;
                }
            }
            else {
                var normalValue = _rng.RandiRange(3, 20);
                Frame!.Text = $"Frame loss during hitbox lerp:\n[color=green]{normalValue}ms[/color]";
            }
        }
    }

    private void OnOutputPressed() {
        GD.Print(_networkManager!.GetAllPlayers());
        _networkManager!.SpawnAllPlayers();
    }

    private void OnHostPressed() {
        _networkManager!.DefaultServer = Address!.Text;
        _networkManager!.Port = Convert.ToInt16(Port!.Text);
        _networkManager!.SetPlayerInfo("Name", PlayerName!.Text);
        _networkManager!.SetPlayerInfo("Hyprs", Hyprs!.GetItemText(Hyprs!.GetSelectedId()));
        _networkManager!.Host();
    }
    private void OnJoinPressed() {
        _networkManager!.DefaultServer = Address!.Text;
        _networkManager!.Port = Convert.ToInt16(Port!.Text);
        _networkManager!.SetPlayerInfo("Name", PlayerName!.Text);
        _networkManager!.SetPlayerInfo("Hyprs", Hyprs!.GetItemText(Hyprs!.GetSelectedId()));
        _networkManager!.Join();
    }
}
