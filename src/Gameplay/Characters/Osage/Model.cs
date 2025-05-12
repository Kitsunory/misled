namespace Misled.Gameplay.Osage;

using System.Threading.Tasks;
using Godot;
using Misled.Gameplay.Core;
using Misled.Gameplay.Model;

public partial class Model : Base {
    [Export] public Area3D? Dynamic;
    [Export] public Area3D? Whole;
    [Export] public Area3D? Area;

    private bool _isAttacking;
    private float _attackTimer;
    private float _attackResetTime;

    public override string CharacterId => "Osage";

    public override void _Ready() {
        if (!IsMultiplayerAuthority()) {
            return;
        }

        base._Ready();
        _state!.NormalConfig = new NormalConfig {
            InputBufferTime = 0.4f,
            MaxComboCount = 4,
            AnimationMap = new() {
                { 1, "NA1" },
                { 2, "NA2" },
                { 3, "NA3" },
                { 4, "NA4" },
            }
        };

        Dynamic!.BodyEntered += OnDynamicBodyEntered;
        Whole!.BodyEntered += OnWholeBodyEntered;

        _state.OnNormalAttack += HandleNormalAttack;

        InitSystems();
    }

    private void OnDynamicBodyEntered(Node body) {
        if (IsInvalidHitscan(body)) {
            return;
        }

        var peerId = long.Parse(body.Name);
        RequestDealDamage(peerId, -300);
        RequestDealBreak(peerId, -50);
    }

    private void OnWholeBodyEntered(Node body) {
        if (IsInvalidHitscan(body)) {
            return;
        }

        var peerId = long.Parse(body.Name);
        GetPlayerState(peerId)?.RpcId(peerId, nameof(State.RequestBloodstain));
        RequestDealDamage(peerId, -500);
        RequestDealBreak(peerId, -75);
    }

    public override void _PhysicsProcess(double delta) {
        if (!IsMultiplayerAuthority()) {
            return;
        }

        base._PhysicsProcess(delta);
        UpdateAttackTimer(delta);
        HandleAttackInputs();
    }

    private void HandleNormalAttack() =>
        Dynamic!.Monitoring = true;

    private void UpdateAttackTimer(double delta) {
        if (!_isAttacking) {
            return;
        }

        _attackTimer += (float)delta;

        if (_attackTimer > _attackResetTime) {
            ResetAttackState();
        }
    }

    private void HandleAttackInputs() {

        if (_state!.IsAttacking && _state.IsChainable) {
            Dynamic!.Monitoring = false;
        }

        if (Input.IsActionJustPressed("Signature")) {
            TryStartAttack("Signature", reach: false, isSignature: true);
        }

        if (Input.IsActionJustPressed("Alternate")) {
            TryStartAttack("Alternate", skipAttackState: true);
        }

        if (Input.IsActionJustPressed("Exclusive")) {
            TryStartAttack("Exclusive1");
        }
    }

    private void TryStartAttack(string animationName, bool skipAttackState = false, bool reach = true, bool isSignature = false) {
        if (_state!.IsAttacking && !skipAttackState) {
            return;
        }

        StartAttack(animationName, skipAttackState, reach, isSignature);
    }

    private void StartAttack(string animationName, bool skipAttackState = false, bool reach = true, bool isSignature = false) {
        _attackTimer = 0f;
        _isAttacking = true;
        _state!.IsChainable = false;

        Dynamic!.Monitoring = false;

        if (!skipAttackState) {
            _state!.StartAttack(reach);
        }

        var animationLength = _animator!.AnimationTree!.GetAnimation(animationName).Length;
        _attackResetTime = animationLength;

        _animator.PlayAbilities(animationName);

        if (isSignature) {
            _ = HandleSignatureDash(5f, 1f);
        }
        else {
            // Re-enable monitoring after normal skill
            _ = RestoreCollisionMonitoring(animationLength);
        }
    }

    private async Task HandleSignatureDash(float distance, float duration) {
        Whole!.Monitoring = true;

        var start = GlobalPosition;
        var forward = -GlobalTransform.Basis.Z;
        var target = start + forward * distance;

        var time = 0f;
        while (time < duration) {
            time += (float)GetProcessDeltaTime();
            var t = Mathf.Clamp(time / duration, 0f, 1f);
            var easedT = EaseOutExpo(t);
            GlobalPosition = start.Lerp(target, easedT);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        GlobalPosition = target;
        Whole!.Monitoring = false;
        Dynamic!.Monitoring = true;
    }

    private async Task RestoreCollisionMonitoring(float delay) {
        await Task.Delay((int)(delay * 1000));
        Dynamic!.Monitoring = true;
    }

    private static float EaseOutExpo(float t) =>
        (t == 1f) ? 1f : 1 - Mathf.Pow(2, -10 * t);

    private void ResetAttackState() {
        _state!.ResetAttack();
        ResetAttack();
    }

    public void ResetAttack() {
        _state!.IsChainable = true;
        _isAttacking = false;
        _attackTimer = 0f;

        _animator!.ResetAbilities();
        Whole!.Monitoring = false;
        Dynamic!.Monitoring = false;
    }
}
