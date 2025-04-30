namespace Misled.Characters.Universal;
using System.Collections.Generic;
using Godot;

public class NormalConfig {
    public float DelayTime { get; init; } = 0.6f;
    public float DefaultResetTime { get; init; } = 1.6f;
    public int MaxComboCount { get; init; } = 3;

    public Dictionary<int, string> AnimationMap { get; init; } = new()
    {
        { 1, "NA1" },
        { 2, "NA2" },
        { 3, "NA3" }
    };
}

public class Normal(
    State state,
    AnimationTree tree,
    AnimationPlayer player,
    NormalConfig config
) {
    private readonly AnimationTree _tree = tree;
    private readonly AnimationPlayer _player = player;
    private readonly NormalConfig _config = config;

    private float _resetTime;
    private float _timer;

    public int AttackIndex { get; private set; }

    private bool _bufferedNextAttack;

    private void HandleAttackInput() {
        if (!Input.IsActionJustPressed("Attack")) {
            return;
        }

        if (!state.IsAttacking) {
            AttackIndex = 1;
            StartAttack(AttackIndex);
            state.IsAttacking = true;
            _timer = 0f;
            return;
        }

        // Already attacking, buffer next attack
        _bufferedNextAttack = true;
    }

    public void Update(float delta) {
        HandleAttackInput();

        if (state.IsResetAttack) {
            ResetAttack();
            return;
        }

        if (!state.IsAttacking) {
            return;
        }

        _timer += delta;

        // Only allow chaining after delay time
        if (_bufferedNextAttack && _timer > _config.DelayTime && AttackIndex < _config.MaxComboCount) {
            AttackIndex++;
            StartAttack(AttackIndex);
            _bufferedNextAttack = false;
            _timer = 0f;
        }

        if (_timer > _resetTime) {
            ResetAttack();
        }
    }

    private void StartAttack(int index) {
        if (!_config.AnimationMap.TryGetValue(index, out var animationName) || string.IsNullOrEmpty(animationName)) {
            return;
        }

        _resetTime = _player.GetAnimation(animationName).Length;

        _tree.Set("parameters/Attack/blend_amount", 1f);
        _tree.Set("parameters/Normal/blend_amount", index - 2f);

        var path = $"parameters/{animationName}/request";
        _tree.Set(path, (int)AnimationNodeOneShot.OneShotRequest.Fire);
    }

    public void ResetAttack() {
        state.IsResetAttack = false;
        state.IsAttacking = false;
        AttackIndex = 0;
        _timer = 0f;

        _tree.Set("parameters/Attack/blend_amount", 0f);
        _tree.Set("parameters/Normal/blend_amount", -1f);
    }
}
