namespace Misled.Characters.Universal;
using Godot;
using Misled.Gameplay.Model;

public partial class State : Node {
    public NormalConfig? NormalConfig { get; set; }
    public int Health { get; set; }
    public int Resistance { get; set; }
    public int Stamina { get; set; }
    public Elemental? Elemental { get; set; }

    public bool IsAttacking { get; set; }
    public bool IsResetAttack { get; set; }
}
