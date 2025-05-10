namespace Misled.Gameplay.Universal;

using Godot;
using Misled.Gameplay.Model;

/// <summary>
/// Handles the normal attack behavior for a character, including combo management and animation playback.
/// </summary>
public class Normal {
    private bool _isAttacking;
    private readonly State _state;
    private readonly NormalConfig _config;
    private readonly Animator _animator;

    private float _attackTimer;
    private int _currentAttackIndex;
    private bool _nextAttackBuffered;

    /// <summary>
    /// Initializes a new instance of the <see cref="Normal"/> class.
    /// </summary>
    /// <param name="state">The character's state.</param>
    /// <param name="animator">The animator.</param>
    /// <param name="config">The normal attack configuration.</param>
    public Normal(
        State state,
        Animator animator,
        NormalConfig config
    ) {
        _state = state;
        _animator = animator;
        _config = config;

        _state.OnAttackEnded += ResetAttack;
    }

    /// <summary>
    /// Handles the attack input and manages the attack combo.
    /// </summary>
    private void HandleAttackInput() {
        if (Input.IsActionJustPressed("Normal")) {
            if (!_isAttacking) {
                StartAttackCombo();
            }
            else if (_currentAttackIndex < _config.MaxComboCount) {
                BufferNextAttack();
            }
        }
    }

    /// <summary>
    /// Buffers the next attack in the combo.
    /// </summary>
    private void BufferNextAttack() =>
        _nextAttackBuffered = true;

    /// <summary>
    /// Starts a new attack combo.
    /// </summary>
    private void StartAttackCombo() {
        if (_state.IsAttacking) {
            return;
        }

        _currentAttackIndex = 1;
        StartAttack(_currentAttackIndex);
        _isAttacking = true;
        _attackTimer = 0f;
    }

    /// <summary>
    /// Continues the attack combo.
    /// </summary>
    private void ContinueAttackCombo() {
        _currentAttackIndex++;
        StartAttack(_currentAttackIndex);
        _nextAttackBuffered = false;
        _attackTimer = 0f;
    }

    /// <summary>
    /// Starts an attack with the specified index.
    /// </summary>
    /// <param name="attackIndex">The index of the attack to start.</param>
    private void StartAttack(int attackIndex) {
        _state.StartAttack();

        if (!_config.AnimationMap.TryGetValue(attackIndex, out var animationName) || string.IsNullOrEmpty(animationName)) {
            GD.Print($"No animation found for attack index: {attackIndex}");
            return;
        }

        var animationLength = _animator.AnimationTree!.GetAnimation(animationName).Length;
        _config.AttackResetTime = animationLength;

        _animator.PlayNormal(animationName);
    }

    /// <summary>
    /// Resets the attack state.
    /// </summary>
    public void ResetAttack() {
        _isAttacking = false;
        _currentAttackIndex = 0;
        _attackTimer = 0f;
        _nextAttackBuffered = false;

        _animator.ResetNormal();
    }

    /// <summary>
    /// Updates the normal attack logic.
    /// </summary>
    /// <param name="delta">The time elapsed since the previous frame.</param>
    public void Update(float delta) {
        HandleAttackInput();

        if (!_isAttacking) {
            return;
        }

        _attackTimer += delta;

        if (_nextAttackBuffered && CanContinueCombo()) {
            ContinueAttackCombo();
        }

        if (_attackTimer > _config.AttackResetTime) {
            _state.ResetAttack();
            ResetAttack();
        }
    }

    /// <summary>
    /// Determines whether the attack combo can be continued.
    /// </summary>
    /// <returns><c>true</c> if the attack combo can be continued; otherwise, <c>false</c>.</returns>
    private bool CanContinueCombo() =>
        _attackTimer > _config.InputBufferTime && _currentAttackIndex < _config.MaxComboCount;
}
