namespace Misled.Characters.Tanger;
using Misled.Characters.Core;
using Misled.Characters.Universal;

public partial class Model : CharacterBase {
    public override string CharacterId => "Tanger";

    public override void _Ready() {
        _state = new State() {
            NormalConfig = new NormalConfig()
        };

        base._Ready();

        InitSystems();
    }
}
