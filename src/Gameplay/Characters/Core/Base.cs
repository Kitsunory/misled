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
    [Export] public AudioStreamPlayer3D? AudioPlayer;
    [Export] public Node? Animator;

    [Export] public float MoveSpeed = 7.0f;
    [Export] public float JumpForce = 6.0f;
    [Export] public float Acceleration = 10f;
    [Export] public float Deceleration = 8f;
    [Export] public float BlendSmoothSpeed = 20f;
    [Export] public int MaxJumps = 2;

    public override void _Ready() {
        if (Camera == null || AnimationTree == null || AnimationPlayer == null || Particles == null) {
            GD.Print("Missing required exported nodes.");
            return;
        }

        _animator = Animator! as Animator;

        if (_state != null) {
            InitSystems();
        }

        Multiplayer.MultiplayerPeer.SetTransferMode(MultiplayerPeer.TransferModeEnum.UnreliableOrdered);
    }

    protected void InitSystems() {
        _movement = new Movement(_state!, this, _animator!, Particles!, Camera!, AudioPlayer!, MoveSpeed, JumpForce, Acceleration, Deceleration, MaxJumps);
        _normal = new Normal(_state!, _animator!, _state!.NormalConfig!);
    }

    public override void _PhysicsProcess(double delta) {
        if (!IsMultiplayerAuthority()) {
            return;
        }

        var dt = (float)delta;
        _movement?.Update(dt);
        _normal?.Update(dt);
        _animator?.UpdatePhysicsProcessDeltaTime(dt);

        Rpc(nameof(SendMovement), GlobalTransform.Origin, Velocity, Rotation);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public void SendMovement(Vector3 position, Vector3 velocity, Vector3 rotation) {
        if (!IsMultiplayerAuthority()) {
            GlobalTransform = new Transform3D(Basis.Identity.Rotated(Vector3.Up, rotation.Y), position);
        }
    }
}
