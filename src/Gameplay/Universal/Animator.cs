namespace Misled.Characters.Universal;
using Godot;

public class Animator {
    private readonly AnimationTree _animationTree;
    private readonly float _blendSpeed;

    private float _moveBlend;
    private float _groundBlend = -1f;
    private float _ppDeltaTime;

    public Animator(AnimationTree animationTree, float blendSmoothSpeed) {
        _animationTree = animationTree;
        _animationTree.Active = true;
        _blendSpeed = blendSmoothSpeed;
    }

    public void UpdatePhysicsProcessDeltaTime(float ppDeltaTime) => _ppDeltaTime = ppDeltaTime;

    public void UpdateMovementBlend(Vector3 velocity, bool isGrounded) {
        var horizontalSpeed = new Vector3(velocity.X, 0, velocity.Z).Length();
        var targetMoveBlend = Mathf.Clamp(horizontalSpeed / 5f, 0f, 1f);
        _moveBlend = Mathf.Lerp(_moveBlend, targetMoveBlend, _blendSpeed * _ppDeltaTime);
        _animationTree.Set("parameters/Move/blend_position", _moveBlend);

        var targetGroundBlend = isGrounded ? -1.0f : (velocity.Y > 0 ? 0.0f : 1.0f);
        _groundBlend = Mathf.Lerp(_groundBlend, targetGroundBlend, _blendSpeed * _ppDeltaTime);
        _animationTree.Set("parameters/Ground/blend_amount", _groundBlend);
    }
}
