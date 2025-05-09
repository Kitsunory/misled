namespace Misled.Gameplay.Universal;

using Godot;

public partial class UI : Control {
    [Export] public ProgressBar? HP;
    [Export] public ProgressBar? Resistance;
    [Export] public ProgressBar? Stamina;
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
    }

}
