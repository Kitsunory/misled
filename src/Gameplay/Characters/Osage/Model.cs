namespace Misled.Characters.Osage;

using Misled.Characters.Core;
using Misled.Characters.Universal;
using Misled.Gameplay.Models;

public partial class Model : CharacterBase {
    public override string CharacterId => "Osage";

    public override void _Ready() {
        _state = new State() {
            NormalConfig = new NormalConfig() {
                InputBufferTime = 0.4f,
                MaxComboCount = 4,

                AnimationMap = new()
                {
                    { 1, "NA1" },
                    { 2, "NA2" },
                    { 3, "NA3" },
                    { 4, "NA4" },
                }
            }
        };

        base._Ready();

        InitSystems();
    }
}
