namespace Misled.Gameplay.Universal;
using Godot;

/// <summary>
/// Handles character movement logic, including jumping, rotation, dashing, and applying movement forces.
/// </summary>
public class Movement {
    private readonly State _state;
    private readonly CharacterBody3D _body;
    private readonly Animator _animator;
    private readonly GpuParticles3D _particles;
    private readonly Camera3D _camera;
    private readonly AudioStreamPlayer3D _audioPlayer;
    private readonly float _moveSpeed;
    private readonly float _jumpForce;
    private readonly float _acceleration;
    private readonly float _deceleration;
    private readonly int _maxJumps;
    private int _jumpCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="Movement"/> class.
    /// </summary>
    /// <param name="state">The character's state.</param>
    /// <param name="body">The character's body.</param>
    /// <param name="animator">The animator.</param>
    /// <param name="particles">The particle system.</param>
    /// <param name="camera">The camera.</param>
    /// <param name="audioPlayer">The audio player.</param>
    /// <param name="moveSpeed">The movement speed.</param>
    /// <param name="jumpForce">The jump force.</param>
    /// <param name="acceleration">The acceleration.</param>
    /// <param name="deceleration">The deceleration.</param>
    /// <param name="maxJumps">The maximum number of jumps.</param>
    public Movement(
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
        _state = state;
        _body = body;
        _animator = animator;
        _particles = particles;
        _camera = camera;
        _audioPlayer = audioPlayer;
        _moveSpeed = moveSpeed;
        _jumpForce = jumpForce;
        _acceleration = acceleration;
        _deceleration = deceleration;
        _maxJumps = maxJumps;
    }

    /// <summary>
    /// Called when the node enters the scene tree.
    /// </summary>
    public void Ready() {
        _state.OnAttackStarted += HandleAttackStarted;
        _state.OnDamageReceived += HandleDamage;
    }

    /// <summary>
    /// Updates the movement logic.
    /// </summary>
    /// <param name="delta">The time elapsed since the previous frame.</param>
    public void Update(float delta) {
        if (_state == null) {
            GD.PrintErr("State system not assigned.");
            return;
        }

        var velocity = _body.Velocity;

        ApplyGravity(ref velocity, delta);

        var direction = GetMovementDirection();

        if (!_state.IsAttacking) {
            HandleJump(ref velocity);
            RotateCharacterToward(direction, delta);
        }

        if (Input.IsActionJustPressed("Dash")) {
            HandleDash(ref velocity, direction);
        }

        ApplyMovement(ref velocity, direction, delta);

        _body.Velocity = velocity;
        _body.MoveAndSlide();

        UpdateJumpCount();
        UpdateAnimator(velocity);
        HandleWalkingAudio(velocity);
    }

    /// <summary>
    /// Applies gravity to the character.
    /// </summary>
    /// <param name="velocity">The character's velocity.</param>
    /// <param name="delta">The time elapsed since the previous frame.</param>
    private void ApplyGravity(ref Vector3 velocity, float delta) {
        if (!_body.IsOnFloor()) {
            velocity.Y = _state.IsAttacking ? 0f : velocity.Y + (_body.GetGravity().Y * delta);
        }
    }

    /// <summary>
    /// Gets the movement direction based on player input.
    /// </summary>
    /// <returns>The movement direction.</returns>
    private Vector3 GetMovementDirection() {
        var inputVec = Input.GetVector("Left", "Right", "Up", "Down");

        var forward = _camera.GlobalTransform.Basis.Z;
        var right = _camera.GlobalTransform.Basis.X;

        forward.Y = 0;
        right.Y = 0;

        return ((right * inputVec.X) + (forward * inputVec.Y)).Normalized();
    }

    /// <summary>
    /// Handles the jump action.
    /// </summary>
    /// <param name="velocity">The character's velocity.</param>
    private void HandleJump(ref Vector3 velocity) {
        if (Input.IsActionJustPressed("Jump") && _jumpCount < _maxJumps) {
            velocity.Y = _jumpForce;
            _jumpCount++;
        }
    }

    /// <summary>
    /// Rotates the character towards the movement direction.
    /// </summary>
    /// <param name="direction">The movement direction.</param>
    /// <param name="delta">The time elapsed since the previous frame.</param>
    private void RotateCharacterToward(Vector3 direction, float delta) {
        if (direction == Vector3.Zero) {
            return;
        }

        var current = _body.GlobalTransform;
        var targetBasis = Basis.LookingAt(direction, Vector3.Up);

        var newRotation = new Quaternion(current.Basis).Slerp(new Quaternion(targetBasis), 8f * delta);
        current.Basis = new Basis(newRotation);
        _body.GlobalTransform = current;
    }

    /// <summary>
    /// Handles the dash action.
    /// </summary>
    /// <param name="velocity">The character's velocity.</param>
    /// <param name="direction">The movement direction.</param>
    private void HandleDash(ref Vector3 velocity, Vector3 direction) {
        _particles.Restart();
        _state.ResetAttack();

        var dashDirection = direction == Vector3.Zero
            ? -_camera.GlobalTransform.Basis.Z with { Y = 0 }
            : direction;

        dashDirection = dashDirection.Normalized();
        var dashStrength = _moveSpeed * 5f;

        velocity.X = dashDirection.X * dashStrength;
        velocity.Z = dashDirection.Z * dashStrength;

        var current = _body.GlobalTransform;
        var targetBasis = Basis.LookingAt(dashDirection, Vector3.Up);
        current.Basis = targetBasis;
        _body.GlobalTransform = current;
    }

    /// <summary>
    /// Applies movement forces to the character.
    /// </summary>
    /// <param name="velocity">The character's velocity.</param>
    /// <param name="direction">The movement direction.</param>
    /// <param name="delta">The time elapsed since the previous frame.</param>
    private void ApplyMovement(ref Vector3 velocity, Vector3 direction, float delta) {
        var horizontal = velocity with { Y = 0 };

        if (_state.IsAttacking) {
            horizontal = Vector3.Zero;
        }
        else {
            var shouldMove = direction != Vector3.Zero;
            var targetVelocity = shouldMove ? direction * _moveSpeed : Vector3.Zero;
            var blend = shouldMove ? _acceleration : _deceleration;
            horizontal = horizontal.Lerp(targetVelocity, blend * delta);
        }

        velocity.X = horizontal.X;
        velocity.Z = horizontal.Z;
    }

    /// <summary>
    /// Handles the walking audio.
    /// </summary>
    /// <param name="velocity">The character's velocity.</param>
    private void HandleWalkingAudio(Vector3 velocity) {
        var horizontalVelocity = new Vector2(velocity.X, velocity.Z);
        var isMoving = horizontalVelocity.LengthSquared() > 1f;

        if (_body.IsOnFloor() && isMoving && !_audioPlayer.IsPlaying()) {
            _audioPlayer.Play();
        }
        else if ((!_body.IsOnFloor() || !isMoving) && _audioPlayer.IsPlaying()) {
            _audioPlayer.Stop();
        }
    }

    /// <summary>
    /// Updates the jump count when the character lands on the floor.
    /// </summary>
    private void UpdateJumpCount() {
        if (_body.IsOnFloor()) {
            _jumpCount = 0;
        }
    }

    /// <summary>
    /// Updates the animator with the current velocity and grounded state.
    /// </summary>
    /// <param name="velocity">The character's velocity.</param>
    private void UpdateAnimator(Vector3 velocity) => _animator.UpdateMovementBlend(velocity, _body.IsOnFloor());

    private async void HandleAttackStarted() {
        var cameraDirection = -_camera.GlobalTransform.Basis.Z;
        cameraDirection.Y = 0;
        cameraDirection = cameraDirection.Normalized();

        var targetBasis = Basis.LookingAt(cameraDirection, Vector3.Up);
        _body.GlobalTransform = _body.GlobalTransform with { Basis = targetBasis };

        var attackMoveDistance = _moveSpeed * 0.1f;
        var targetPosition = _body.GlobalPosition + (cameraDirection * attackMoveDistance);

        var start = _body.GlobalPosition;
        var duration = 0.5f;
        var elapsed = 0f;

        while (elapsed < duration) {
            if (!_state.IsAttacking) { return; }
            var t = elapsed / duration;
            var easedT = 1f - Mathf.Pow(1f - t, 3f);
            _body.GlobalPosition = start.Lerp(targetPosition, easedT);
            await _body.ToSignal(_body.GetTree(), "process_frame");
            elapsed += (float)_body.GetProcessDeltaTime();
        }

        _body.GlobalPosition = targetPosition;
    }

    private async void HandleDamage(int requester) {
        var world = _body.GetTree().Root.GetNode("World");

        var enemyBody = world.GetNodeOrNull<CharacterBody3D>(requester.ToString());
        if (enemyBody == null) {
            GD.PrintErr("Enemy body not found");
            return;
        }

        // Get the opposite direction of the body's forward vector
        var bodyDirection = -enemyBody.GlobalTransform.Basis.Z;
        bodyDirection.Y = 0;
        bodyDirection = bodyDirection.Normalized();

        // No need to rotate the character

        // Calculate the target position to move back
        var damageMoveDistance = _moveSpeed * 0.1f;
        var targetPosition = _body.GlobalPosition + (bodyDirection * damageMoveDistance);

        var start = _body.GlobalPosition;
        var duration = 0.2f; // Shorten the duration for a quick pushback
        var elapsed = 0f;

        while (elapsed < duration) {
            var t = elapsed / duration;
            var easedT = 1f - Mathf.Pow(1f - t, 3f);
            _body.GlobalPosition = start.Lerp(targetPosition, easedT);
            await _body.ToSignal(_body.GetTree(), "process_frame");
            elapsed += (float)_body.GetProcessDeltaTime();
        }

        _body.GlobalPosition = targetPosition;
    }
}
