namespace Misled.Universal.Panel;

using Godot;
using Misled.Gameplay.Core;
using Misled.Gameplay.Universal;

public partial class Panel : Sprite3D {
    [Export] public ProgressBar? HP;
    [Export] public ProgressBar? HPMark;
    [Export] public ProgressBar? Resistance;
    [Export] public RichTextLabel? PlayerName;

    private State? _state;

    private float _hpDelayTimer;
    private float _previousHealth = 10000f; // Or some high default
    private const float DELAY_BEFORE_DROP = 1.0f;
    private const float EASE_SPEED = 5f; // You can tweak this

    public override void _Ready() {
        if (IsMultiplayerAuthority()) {
            Visible = false;
        }

        _state = GetNode<State>("../State");
        var myId = Multiplayer.GetUniqueId();
        var allPlayers = NetworkManager.Instance?.GetAllPlayers();

        if (allPlayers != null && allPlayers.TryGetValue(myId, out var playerInfo)) {
            if (playerInfo.TryGetValue("Name", out var myName)) {
                PlayerName!.Text = myName;
            }
        }

        _previousHealth = _state?.Health ?? 0f;
        HPMark!.Value = _previousHealth;
    }

    public override void _Process(double delta) {
        if (_state == null) {
            return;
        }

        var currentHealth = _state.Health;
        HP!.Value = currentHealth;

        // Detect damage
        if (currentHealth < _previousHealth) {
            _hpDelayTimer = DELAY_BEFORE_DROP;
        }

        // Delay before easing
        if (_hpDelayTimer > 0f) {
            _hpDelayTimer -= (float)delta;
        }
        else {
            // Ease out HPMark toward actual HP
            HPMark!.Value = Mathf.Lerp((float)HPMark!.Value, currentHealth, (float)delta * EASE_SPEED);
        }

        _previousHealth = currentHealth;

        Resistance!.Value = _state.Resistance;
    }
}
