namespace Misled.Gameplay.Model;

using Godot.Collections;

/// <summary>
/// Configuration for normal attack behavior, including combo timing and animation mappings.
/// </summary>
public class NormalConfig {
    /// <summary>
    /// Gets or sets the input buffer time for chaining attacks in a combo (in seconds).
    /// </summary>
    public float InputBufferTime { get; set; } = 0.6f;

    /// <summary>
    /// Gets or sets the time after which the attack combo resets (in seconds).
    /// </summary>
    public float AttackResetTime { get; set; } = 1.6f;

    /// <summary>
    /// Gets or sets the maximum number of attacks allowed in a combo.
    /// </summary>
    public int MaxComboCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the mapping between attack indices and animation names.
    /// </summary>
    public Dictionary<int, string> AnimationMap { get; set; } = new()
    {
        { 1, "NA1" },
        { 2, "NA2" },
        { 3, "NA3" }
    };
}
