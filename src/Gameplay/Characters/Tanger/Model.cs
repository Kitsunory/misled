namespace Misled.Gameplay.Tanger;
using Godot;
using Misled.Gameplay.Core;
using Misled.Gameplay.Model;

public partial class Model : Base {
    public override string CharacterId => "Tanger";

    public override void _Ready() {
        base._Ready();
        _state!.NormalConfig = new NormalConfig();

        InitSystems();
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);

        if (Input.IsActionJustPressed("Signature")) {
            _animator?.PlayAbilities("Signature");
        }

        if (Input.IsActionJustPressed("Alternate")) {
            _animator?.PlayAbilities("Alternate");
        }

        if (Input.IsActionJustPressed("Exclusive")) {
            _animator?.PlayAbilities("Exclusive");
        }
    }
}
