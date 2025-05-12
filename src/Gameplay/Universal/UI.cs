namespace Misled.Gameplay.Universal;

using System.Collections.Generic; // Import the Dictionary namespace
using Godot;

public partial class UI : Control {
    [Export] public ProgressBar? HP;
    [Export] public ProgressBar? Resistance;
    [Export] public ProgressBar? Stamina;
    [Export] public RichTextLabel? Score;
    private State? _state;

    public override void _Ready() {
        if (!IsMultiplayerAuthority()) {
            Visible = false;
        }

        _state = GetNode<State>("../State");
    }

    public override void _PhysicsProcess(double delta) {
        if (_state == null) {
            return;
        }
        HP!.Value = _state.Health;
        Resistance!.Value = _state.Resistance;
        Stamina!.Value = _state.Stamina;

        // Updated Score display
        if (_state.PlayersScore != null) { // Check for null
            Score!.Text = DictionaryToString(_state.PlayersScore);
        }
        else {
            Score!.Text = "Dictionary is null or not accessible."; //Handle if the Dictionary is not there
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
