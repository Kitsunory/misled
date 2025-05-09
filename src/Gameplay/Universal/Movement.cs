namespace Misled.Characters.Universal;
using Godot;

public class Movement(
    State state,
    CharacterBody3D body,
    Animator animator,
    GpuParticles3D particles,
    Camera3D camera,
    AudioStreamPlayer3D audioPlayer,
    float moveSpeed,
    float jumpForce,
    float acceleration,
    float deceleration,
    int maxJumps
) {
    private int _jumpCount;

    public void Update(float delta) {
        if (state == null) {
            GD.PrintErr("State system not assigned.");
            return;
        }

        var velocity = body.Velocity;

        // Apply gravity
        if (!body.IsOnFloor()) {
            velocity += body.GetGravity() * delta;
        }

        // Movement input
        var inputVec = Input.GetVector("Left", "Right", "Up", "Down");

        var forward = camera.GlobalTransform.Basis.Z;
        var right = camera.GlobalTransform.Basis.X;

        forward.Y = 0;
        right.Y = 0;

        var direction = ((right * inputVec.X) + (forward * inputVec.Y)).Normalized();

        // Rotation and jump logic (only if not attacking)
        if (!state.IsAttacking) {
            HandleJump(ref velocity);
            RotateCharacterToward(direction, delta);
        }

        // Dash input
        if (Input.IsActionJustPressed("Dash")) {
            HandleDash(ref velocity, direction);
        }

        // Horizontal movement (only if not attacking or dashing)
        ApplyMovement(ref velocity, direction, delta);

        body.Velocity = velocity;
        body.MoveAndSlide();

        if (body.IsOnFloor()) {
            _jumpCount = 0;
        }

        animator.UpdateMovementBlend(velocity, body.IsOnFloor());

        // Play/Stop walking audio based on actual movement
        HandleWalkingAudio(velocity);
    }

    private void HandleJump(ref Vector3 velocity) {
        if (Input.IsActionJustPressed("Jump") && _jumpCount < maxJumps) {
            velocity.Y = jumpForce;
            _jumpCount++;
        }
    }

    private void RotateCharacterToward(Vector3 direction, float delta) {
        if (direction == Vector3.Zero) {
            return;
        }

        var current = body.GlobalTransform;
        var targetBasis = Basis.LookingAt(direction, Vector3.Up);

        var newRotation = new Quaternion(current.Basis).Slerp(new Quaternion(targetBasis), 8f * delta);
        current.Basis = new Basis(newRotation);
        body.GlobalTransform = current;
    }

    private void HandleDash(ref Vector3 velocity, Vector3 direction) {
        particles.Restart();
        state.IsResetAttack = true;

        var dashDirection = direction == Vector3.Zero
            ? -camera.GlobalTransform.Basis.Z with { Y = 0 }
            : direction;

        dashDirection = dashDirection.Normalized();
        var dashStrength = moveSpeed * 5f;

        velocity.X = dashDirection.X * dashStrength;
        velocity.Z = dashDirection.Z * dashStrength;

        var current = body.GlobalTransform;
        var targetBasis = Basis.LookingAt(dashDirection, Vector3.Up);
        current.Basis = targetBasis;
        body.GlobalTransform = current;
    }

    private void ApplyMovement(ref Vector3 velocity, Vector3 direction, float delta) {
        var horizontal = velocity with { Y = 0 };

        var shouldMove = direction != Vector3.Zero && !state.IsAttacking;

        var targetVelocity = shouldMove
            ? direction * moveSpeed
            : Vector3.Zero;

        var blend = shouldMove ? acceleration : deceleration;
        horizontal = horizontal.Lerp(targetVelocity, blend * delta);

        velocity.X = horizontal.X;
        velocity.Z = horizontal.Z;
    }

    private void HandleWalkingAudio(Vector3 velocity) {
        var horizontalVelocity = new Vector2(velocity.X, velocity.Z);
        var isMoving = horizontalVelocity.LengthSquared() > 1f;

        if (body.IsOnFloor() && isMoving && !audioPlayer.IsPlaying()) {
            audioPlayer.Play();
        }
        else if ((!body.IsOnFloor() || !isMoving) && audioPlayer.IsPlaying()) {
            audioPlayer.Stop();
        }
    }
}
