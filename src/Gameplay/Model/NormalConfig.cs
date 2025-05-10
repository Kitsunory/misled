namespace Misled.Gameplay.Model;

using Godot.Collections;

public class NormalConfig {
    public float InputBufferTime { get; set; } = 0.6f;
    public float AttackResetTime { get; set; } = 1.6f;
    public int MaxComboCount { get; set; } = 3;

    public Dictionary<int, string> AnimationMap { get; set; } = new()
    {
        { 1, "NA1" },
        { 2, "NA2" },
        { 3, "NA3" }
    };
}
