namespace Misled.Gameplay.Tanger;

using Godot;
using Misled.Gameplay.Core;
using Misled.Gameplay.Model;
using Misled.Gameplay.Universal;

/// <summary>
/// Represents the Tanger character model in the game.
/// </summary>
public partial class Model : Base {
    [Export]
    public Area3D? Dynamic;

    [Export]
    public Area3D? Whole;

    [Export]
    public Area3D? Big;

    private bool _isAttacking;
    private float _attackTimer;
    private float _attackResetTime;

    /// <summary>
    /// Gets the character's unique identifier.
    /// </summary>
    public override string CharacterId => "Tanger";

    /// <summary>
    /// Called when the node enters the scene tree.
    /// </summary>
    public override void _Ready() {
        if (!IsMultiplayerAuthority()) {
            return;
        }

        base._Ready();
        _state!.NormalConfig = new NormalConfig();

        Dynamic!.BodyEntered += OnDynamicBodyEntered;

        InitSystems();
    }

    private void OnDynamicBodyEntered(Node body) {
        if (IsInvalidHitscan(body)) { return; }

        var peerId = long.Parse(body.Name);
        GetPlayerState(peerId)?.RpcId(peerId, nameof(State.RequestHealthChange), -400);
    }

    /// <summary>
    /// Called during the physics processing step of the main loop.
    /// </summary>
    /// <param name="delta">The time elapsed since the previous physics update.</param>
    public override void _PhysicsProcess(double delta) {
        if (!IsMultiplayerAuthority()) {
            return;
        }

        base._PhysicsProcess(delta);
        UpdateAttackTimer(delta);
        HandleAttackInputs();
    }

    /// <summary>
    /// Updates the attack timer and resets the attack if necessary.
    /// </summary>
    /// <param name="delta">The time elapsed since the previous physics update.</param>
    private void UpdateAttackTimer(double delta) {
        if (_isAttacking) {
            _attackTimer += (float)delta;

            if (_attackTimer > _attackResetTime) {
                ResetAttackState();
            }
        }
    }

    /// <summary>
    /// Handles player input for initiating attacks.
    /// </summary>
    private void HandleAttackInputs() {
        if (_state!.IsAttacking) {
            Dynamic!.Monitoring = true;
        } else {
            Dynamic!.Monitoring = false;
        }

        if (Input.IsActionJustPressed("Signature")) {
            TryStartAttack("Signature");
        }

        if (Input.IsActionJustPressed("Alternate")) {
            TryStartAttack("Alternate");
        }

        if (Input.IsActionJustPressed("Exclusive")) {
            TryStartAttack("Exclusive");
        }
    }

    /// <summary>
    /// Attempts to start an attack with the given animation name.
    /// </summary>
    /// <param name="animationName">The name of the attack animation.</param>
    private void TryStartAttack(string animationName) {
        if (_state!.IsAttacking) {
            return;
        }

        StartAttack(animationName);
    }

    /// <summary>
    /// Starts the attack animation and sets the attack state.
    /// </summary>
    /// <param name="animationName">The name of the attack animation.</param>
    private void StartAttack(string animationName) {
        _attackTimer = 0f;
        _isAttacking = true;
        _state!.StartAttack();

        var animationLength = _animator!.AnimationTree!.GetAnimation(animationName).Length;
        _attackResetTime = animationLength;

        _animator.PlayAbilities(animationName);
    }

    /// <summary>
    /// Resets the attack state.
    /// </summary>
    private void ResetAttackState() {
        _state!.ResetAttack();
        ResetAttack();
    }

    /// <summary>
    /// Resets the attack flags and animator.
    /// </summary>
    public void ResetAttack() {
        _isAttacking = false;
        _attackTimer = 0f;

        _animator!.ResetAbilities();
    }
}
