namespace Misled.Characters.Core;

using Godot;
using Misled.Characters.Universal;

public abstract partial class CharacterBase : CharacterBody3D {
    public abstract string CharacterId { get; }

    protected State? _state;
    protected Movement? _movement;
    protected Normal? _normal;
    protected Animator? _animator;

    [Export] public Camera3D? Camera;
    [Export] public AnimationTree? AnimationTree;
    [Export] public AnimationPlayer? AnimationPlayer;
    [Export] public GpuParticles3D? Particles;

    [Export] public float MoveSpeed = 7f;
    [Export] public float JumpForce = 6f;
    [Export] public float Acceleration = 10f;
    [Export] public float Deceleration = 8f;
    [Export] public float BlendSmoothSpeed = 5f;
    [Export] public int MaxJumps = 2;

    public override void _Ready() {
        if (Camera == null || AnimationTree == null || AnimationPlayer == null || Particles == null) {
            GD.Print("Missing required exported nodes.");
            return;
        }

        _animator = new Animator(AnimationTree, BlendSmoothSpeed);

        // Don't create Movement or NormalConfig yet if _state isn't ready
        if (_state != null) {
            InitSystems();
        }
    }

    protected void InitSystems() {
        _movement = new Movement(_state!, this, _animator!, Particles!, Camera!, MoveSpeed, JumpForce, Acceleration, Deceleration, MaxJumps);
        _normal = new Normal(_state!, AnimationTree!, AnimationPlayer!, _state!.NormalConfig!);
    }

    public override void _PhysicsProcess(double delta) {
        var dt = (float)delta;
        _movement?.Update(dt);
        _normal?.Update(dt);
        _animator?.UpdatePhysicsProcessDeltaTime(dt);
    }
}
