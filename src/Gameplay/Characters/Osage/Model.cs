namespace Misled.Characters.Osage;
using Misled.Characters.Core;
using Misled.Characters.Universal;

public partial class Model : CharacterBase {
    public override string CharacterId => "Osage";

    public override void _Ready() {
        _state = new State() {
            NormalConfig = new NormalConfig()
        };

        base._Ready();

        InitSystems();
    }
}
