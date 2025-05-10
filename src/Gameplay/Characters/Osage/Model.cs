namespace Misled.Gameplay.Osage;

using Misled.Gameplay.Core;
using Misled.Gameplay.Model;

public partial class Model : Base {
    public override string CharacterId => "Osage";

    public override void _Ready() {
        base._Ready();
        _state!.NormalConfig = new NormalConfig() {
            InputBufferTime = 0.4f,
            MaxComboCount = 4,

            AnimationMap = new()
            {
                { 1, "NA1" },
                { 2, "NA2" },
                { 3, "NA3" },
                { 4, "NA4" },
            }
        };

        InitSystems();
    }
}
