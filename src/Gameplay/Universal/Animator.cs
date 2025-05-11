namespace Misled.Gameplay.Universal;
using Godot;

/// <summary>
/// Manages animations using an AnimationTree.
/// </summary>
public partial class Animator : Node {

    [Export]
    public AnimationTree? AnimationTree { get; set; }

    [Export]
    public float BlendSpeed { get; set; } = 20f;

    private float _moveBlend;
    private float _osageBlend;
    private float _groundBlend = 1.0f;
    private float _physicsProcessDeltaTime;

    /// <summary>
    /// Updates the delta time used for physics processing.
    /// </summary>
    /// <param name="deltaTime">The delta time.</param>
    public void UpdatePhysicsProcessDeltaTime(float deltaTime) => _physicsProcessDeltaTime = deltaTime;

    /// <summary>
    /// Updates the blend values for movement animations.
    /// </summary>
    /// <param name="velocity">The character's velocity.</param>
    /// <param name="isGrounded">Whether the character is grounded.</param>
    public void UpdateMovementBlend(Vector3 velocity, bool isGrounded) {
        if (AnimationTree == null) {
            return;
        }

        var horizontalSpeed = new Vector3(velocity.X, 0, velocity.Z).Length();
        var targetMoveBlend = Mathf.Clamp(horizontalSpeed / 5f, 0f, 1f);

        _moveBlend = Mathf.Lerp(_moveBlend, targetMoveBlend, BlendSpeed * _physicsProcessDeltaTime);
        AnimationTree.Set("parameters/Move/blend_position", _moveBlend);

        var osageMoveBlend = Mathf.Clamp(horizontalSpeed / 5f, 0.1f, 0.9f);
        _osageBlend = Mathf.Lerp(_moveBlend, targetMoveBlend, BlendSpeed * _physicsProcessDeltaTime);
        AnimationTree.Set("parameters/Abilities/Alternate/State/blend_amount", _osageBlend);

        var targetGroundBlend = isGrounded ? 1.0f : (velocity.Y > 0 ? 0.0f : -1.0f);
        _groundBlend = Mathf.Lerp(_groundBlend, targetGroundBlend, BlendSpeed * _physicsProcessDeltaTime);
        AnimationTree.Set("parameters/Ground/blend_amount", _groundBlend);
    }

    private AnimationNodeStateMachinePlayback? GetAnimationStatePlayback(string stateMachineName) =>
        AnimationTree != null ? (AnimationNodeStateMachinePlayback?)AnimationTree.Get($"parameters/{stateMachineName}/playback") : null;

    private void PlayAnimation(string stateMachineName, string animationName) =>
        Rpc(MethodName.RpcPlayAnimation, stateMachineName, animationName);

    private void ResetAnimation(string stateMachineName) =>
        Rpc(MethodName.RpcResetAnimation, stateMachineName);

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcPlayAnimation(string stateMachineName, string animationName) {
        if (AnimationTree == null) {
            return;
        }

        AnimationTree.Set("parameters/Mode/blend_amount", stateMachineName == "Normal" ? 1f : -1f);
        var statePlayback = GetAnimationStatePlayback(stateMachineName);
        statePlayback?.Travel(animationName);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcResetAnimation(string stateMachineName) {
        if (AnimationTree == null) {
            return;
        }

        AnimationTree.Set("parameters/Mode/blend_amount", 0f);
        var statePlayback = GetAnimationStatePlayback(stateMachineName);
        statePlayback?.Travel("Start");
    }

    public void PlayAbilities(string animationName) {
        PlayAnimation("Abilities", animationName);
    }
    public void ResetAbilities() => ResetAnimation("Abilities");

    public void PlayNormal(string animationName) => PlayAnimation("Normal", animationName);
    public void ResetNormal() => ResetAnimation("Normal");
}
