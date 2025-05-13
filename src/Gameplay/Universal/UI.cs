namespace Misled.Gameplay.Universal;

using System.Collections.Generic; // Import the Dictionary namespace
using Godot;

public partial class UI : Control {
    [Export] public ProgressBar? HP;
    [Export] public RichTextLabel? HPText;
    [Export] public ProgressBar? HPMark;
    [Export] public RichTextLabel? Monocoins;
    [Export] public ProgressBar? Resistance;
    [Export] public ProgressBar? Stamina;
    [Export] public RichTextLabel? Score;
    [Export] public TextureRect? Signature;
    [Export] public TextureRect? Alternate;
    [Export] public TextureRect? Exclusive;
    [Export] public RichTextLabel? SICD;
    [Export] public RichTextLabel? ALCD;
    [Export] public RichTextLabel? EXCD;
    [Export] public Control? Control;

    private State? _state;

    private float _hpDelayTimer;
    private float _previousHealth = 10000f;
    private const float DELAY_BEFORE_DROP = 1.0f;
    private const float EASE_SPEED = 5f;

    public override void _Ready() {
        if (!IsMultiplayerAuthority()) {
            Visible = false;
        }

        _state = GetNode<State>("../State");
        _previousHealth = _state?.Health ?? 0f;
        HPMark!.Value = _previousHealth;
    }


    public override void _Process(double delta) {
        if (_state == null) {
            return;
        }

        var currentHealth = _state.Health;
        HP!.Value = currentHealth;
        HPText!.Text = $"{Mathf.RoundToInt(_state.Health)}";

        if (currentHealth < _previousHealth) {
            _hpDelayTimer = DELAY_BEFORE_DROP;
        }

        if (_hpDelayTimer > 0f) {
            _hpDelayTimer -= (float)delta;
        }
        else {
            HPMark!.Value = Mathf.Lerp((float)HPMark!.Value, currentHealth, (float)delta * EASE_SPEED);
        }

        _previousHealth = currentHealth;

        Resistance!.Value = _state.Resistance;
        Stamina!.Value = _state.Stamina;

        // Updated Score display
        if (_state.PlayersScore != null) { // Check for null
            Score!.Text = DictionaryToString(_state.PlayersScore);
        }
        else {
            Score!.Text = "Dictionary is null or not accessible."; //Handle if the Dictionary is not there
        }

        UpdateCD();
        UpdateUI();
    }

    private void UpdateUI() {
        if (_state!.IsSpy) {
            Exclusive!.Modulate = new Color(1f, 1f, 1f, 0.2f);
        }
        else {
            Exclusive!.Modulate = new Color(1f, 1f, 1f, 1f);
        }
        if (_state!.IsImmobilized) {
            Control!.Modulate = new Color(1f, 1f, 1f, 0.2f);
        }
        else {
            Control!.Modulate = new Color(1f, 1f, 1f, 1f);
        }
        if (Monocoins != null) {
            Monocoins.Text = $"{_state.Monocoins}";
        }
    }

    private void UpdateCD() {
        // Signature
        float? sigCd = _state!.CheckCooldownOrNull("Signature");
        if (sigCd != null) {
            SICD!.Text = $"{sigCd.Value:F1}s";
            Signature!.SelfModulate = new Color(0.5f, 0.5f, 0.5f); // Darken
        }
        else {
            SICD!.Text = "";
            Signature!.SelfModulate = new Color(1f, 1f, 1f); // Normal color
        }

        // Alternate
        float? altCd = _state.CheckCooldownOrNull("Alternate");
        if (altCd != null) {
            ALCD!.Text = $"{altCd.Value:F1}s";
            Alternate!.SelfModulate = new Color(0.5f, 0.5f, 0.5f);
        }
        else {
            ALCD!.Text = "";
            Alternate!.SelfModulate = new Color(1f, 1f, 1f);
        }

        // Exclusive (simulating 20000 max cooldown)
        float? exCd = _state.CheckCooldownOrNull("Exclusive");
        if (exCd != null) {
            float percent = Mathf.Clamp(1f - (exCd.Value / 20000f), 0f, 1f);
            EXCD!.Text = $"{percent * 100f:F0}%";
            Exclusive!.SelfModulate = new Color(0.5f, 0.5f, 0.5f);
        }
        else {
            EXCD!.Text = "";
            Exclusive!.SelfModulate = new Color(1f, 1f, 1f);
        }
    }


    //Helper function to convert the Dictionary to String
    private static string DictionaryToString(Dictionary<long, float> dict) {
        var result = "";
        foreach (var kvp in dict) {
            result += $"Key: {kvp.Key}, Value: {kvp.Value}\n"; // Format each entry
        }
        return result;
    }

}
