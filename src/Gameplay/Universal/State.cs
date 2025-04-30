namespace Misled.Characters.Universal;
using Godot;

public partial class State : Node {
    public NormalConfig? NormalConfig { get; set; }
    public bool IsAttacking { get; set; }
    public bool IsResetAttack { get; set; }
}
