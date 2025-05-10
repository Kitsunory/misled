namespace Misled.Gameplay.Universal;

using Godot;
using Misled.Gameplay.Model;

public class Normal {
    private readonly State _state;
    private readonly NormalConfig _config;
    private readonly Animator _animator;

    private float _attackTimer;
    private int _currentAttackIndex;
    private bool _nextAttackBuffered;

    public Normal(
        State state,
        Animator animator,
        NormalConfig config
    ) {
        _state = state;
        _animator = animator;
        _config = config;

        _state.OnResetAttack += ResetAttack;
    }


    private void HandleAttackInput() {
        if (Input.IsActionJustPressed("Normal")) {
            if (!_state.IsAttacking) {
                StartNewAttackCombo();
            }
            else {
                // Already attacking, buffer next attack
                _nextAttackBuffered = true;
            }
        }
    }

    public void Update(float delta) {
        HandleAttackInput();

        if (!_state.IsAttacking) {
            return;
        }

        _attackTimer += delta;

        // Only allow chaining after delay time
        if (_nextAttackBuffered && _attackTimer > _config.InputBufferTime && _currentAttackIndex < _config.MaxComboCount) {
            ContinueAttackCombo();
        }

        if (_attackTimer > _config.AttackResetTime) {
            ResetAttack();
        }
    }

    private void StartNewAttackCombo() {
        _currentAttackIndex = 1;
        StartAttack(_currentAttackIndex);
        _state.IsAttacking = true;
        _attackTimer = 0f;
    }

    private void ContinueAttackCombo() {
        _currentAttackIndex++;
        StartAttack(_currentAttackIndex);
        _nextAttackBuffered = false;
        _attackTimer = 0f;
    }

    private void StartAttack(int attackIndex) {
        _state.StartAttack();

        if (!_config.AnimationMap.TryGetValue(attackIndex, out var animationName) || string.IsNullOrEmpty(animationName)) {
            return;
        }

        var animationLength = _animator.AnimationTree!.GetAnimation(animationName).Length;
        _config.AttackResetTime = animationLength;

        _animator.PlayNormal(animationName);
    }

    public void ResetAttack() {
        _state.IsAttacking = false;
        _currentAttackIndex = 0;
        _attackTimer = 0f;

        _animator.ResetNormal();
    }
}
