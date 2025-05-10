namespace Misled.Gameplay.Universal;
using Godot;

public partial class Animator : Node {

    [Export]
    public AnimationTree? AnimationTree { get; set; }

    [Export]
    public float BlendSpeed { get; set; } = 20f;

    private float _moveBlend;
    private float _groundBlend = 1.0f;
    private float _physicsProcessDeltaTime;
    public void UpdatePhysicsProcessDeltaTime(float deltaTime) => _physicsProcessDeltaTime = deltaTime;
    public void UpdateMovementBlend(Vector3 velocity, bool isGrounded) {
        var horizontalSpeed = new Vector3(velocity.X, 0, velocity.Z).Length();
        var targetMoveBlend = Mathf.Clamp(horizontalSpeed / 5f, 0f, 1f);

        _moveBlend = Mathf.Lerp(_moveBlend, targetMoveBlend, BlendSpeed * _physicsProcessDeltaTime);
        AnimationTree?.Set("parameters/Move/blend_position", _moveBlend);

        var targetGroundBlend = isGrounded ? 1.0f : (velocity.Y > 0 ? 0.0f : -1.0f);
        _groundBlend = Mathf.Lerp(_groundBlend, targetGroundBlend, BlendSpeed * _physicsProcessDeltaTime);
        AnimationTree?.Set("parameters/Ground/blend_amount", _groundBlend);
    }

    public AnimationNodeStateMachinePlayback? GetAbilitiesStatePlayback() => (AnimationNodeStateMachinePlayback)AnimationTree!.Get("parameters/Abilities/playback");
    public void PlayAbilities(string animationName) => Rpc(MethodName.RpcPlayAbilities, animationName);
    public void ResetAbilities() => Rpc(MethodName.RpcResetAbilities);

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcPlayAbilities(string animationName) {
        AnimationTree!.Set("parameters/Mode/blend_amount", -1f);
        var abilitiesStatePlayback = GetAbilitiesStatePlayback();
        abilitiesStatePlayback?.Travel(animationName);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcResetAbilities() {
        AnimationTree!.Set("parameters/Mode/blend_amount", 0f);
        var abiliesStatePlayback = GetAbilitiesStatePlayback();
        abiliesStatePlayback?.Travel("Start");
    }

    public AnimationNodeStateMachinePlayback? GetNormalStatePlayback() => (AnimationNodeStateMachinePlayback)AnimationTree!.Get("parameters/Normal/playback");
    public void PlayNormal(string animationName) => Rpc(MethodName.RpcPlayNormal, animationName);
    public void ResetNormal() => Rpc(MethodName.RpcResetNormal);

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcPlayNormal(string animationName) {
        AnimationTree!.Set("parameters/Mode/blend_amount", 1f);
        var normalStatePlayback = GetNormalStatePlayback();
        normalStatePlayback?.Travel(animationName);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcResetNormal() {
        AnimationTree!.Set("parameters/Mode/blend_amount", 0f);
        var normalStatePlayback = GetNormalStatePlayback();
        normalStatePlayback?.Travel("Start");
    }
}
