namespace Misled.Gameplay.Universal;

using Godot;

public partial class Panel : Sprite3D {
    [Export] public ProgressBar? HP;
    [Export] public ProgressBar? Resistance;
    private State? _state;

    public override void _Ready() {
        if (IsMultiplayerAuthority()) {
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
    }
}
