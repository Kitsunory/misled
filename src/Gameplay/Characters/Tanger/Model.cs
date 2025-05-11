namespace Misled.Gameplay.Tanger;

using System.Collections.Generic;
using Godot;
using Misled.Gameplay.Core;
using Misled.Gameplay.Model;
using Misled.Gameplay.Universal;

public partial class Model : Base {
    [Export]
    public Area3D? Dynamic;

    [Export]
    public Area3D? Whole;

    [Export]
    public Area3D? Big;

    [Export]
    public Area3D? Area;

    private bool _isAttacking;
    private float _attackTimer;
    private float _attackResetTime;

    private const float EXCLUSIVE_HIT_TIME = 1.3f;
    private readonly float[] _signatureHitTimes = { 0.4f, 0.7f, 1.0f };
    private int _signatureHitIndex;
    private bool _hasTriggeredSpy;


    private string _currentAbility = string.Empty;

    public override string CharacterId => "Tanger";

    public override void _Ready() {
        if (!IsMultiplayerAuthority()) {
            return;
        }

        base._Ready();
        _state!.NormalConfig = new NormalConfig();

        Dynamic!.BodyEntered += OnDynamicBodyEntered;
        _state.OnNormalAttack += HandleNormalAttack;

        InitSystems();
    }

    private void OnDynamicBodyEntered(Node body) {
        if (IsInvalidHitscan(body)) { return; }

        var peerId = long.Parse(body.Name);
        GetPlayerState(peerId)?.RpcId(peerId, nameof(State.RequestHealthChange), -400);
        GetPlayerState(peerId)?.RpcId(peerId, nameof(State.RequestResistanceChange), -50);
    }

    public override void _PhysicsProcess(double delta) {
        if (!IsMultiplayerAuthority()) {
            return;
        }

        base._PhysicsProcess(delta);
        UpdateAttackTimer(delta);
        HandleAttackInputs();
    }

    private void UpdateAttackTimer(double delta) {
        if (!_isAttacking) {
            return;
        }

        _attackTimer += (float)delta;

        if (_currentAbility == "Signature" && _signatureHitIndex < _signatureHitTimes.Length) {
            if (_attackTimer >= _signatureHitTimes[_signatureHitIndex]) {
                CheckBigForHits();
                _signatureHitIndex++;
            }
        }
        if (_currentAbility == "Exclusive" && !_hasTriggeredSpy && _attackTimer >= EXCLUSIVE_HIT_TIME) {
            TriggerSpyEffect();
            _hasTriggeredSpy = true;
        }

        if (_attackTimer > _attackResetTime) {
            ResetAttackState();
        }
    }

    private void HandleNormalAttack() =>
        Dynamic!.Monitoring = true;

    private void HandleAttackInputs() {
        if (_state!.IsAttacking && _state.IsChainable) {
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

    private void TryStartAttack(string animationName) {
        if (_state!.IsAttacking && !_state!.IsChainable) {
            return;
        }

        if (_state.IsChainable) {
            _state.ResetAttack();
        }

        if (animationName == "Signature") {
            Dynamic!.Monitoring = false;
            Big!.Monitoring = true;
            _signatureHitIndex = 0;
        }
        else if (animationName == "Alternate") {
            Whole!.Monitoring = true;
            Area!.Monitoring = true;
            CheckAreaForViewers();
        }
        else if (animationName == "Exclusive") {
            _hasTriggeredSpy = false;
        }

        StartAttack(animationName);
    }

    private async void TriggerSpyEffect() {
        UIPlayer!.Play("Ultimate");

        var affectedPeers = new List<long>();

        foreach (var body in Area!.GetOverlappingBodies()) {
            if (IsInvalidHitscan(body)) {
                continue;
            }

            var peerId = long.Parse(body.Name);
            affectedPeers.Add(peerId);
            GetPlayerState(peerId)?.RpcId(peerId, nameof(State.RequestSpy));
        }

        await ToSignal(GetTree().CreateTimer(9.0f), "timeout");

        foreach (var peerId in affectedPeers) {
            GetPlayerState(peerId)?.RpcId(peerId, nameof(State.ResetSpy));
        }

        UIPlayer.Play("NoUltimate");
    }


    private async void CheckAreaForViewers() {
        await ToSignal(GetTree().CreateTimer(0.55f), "timeout");

        foreach (var body in Area!.GetOverlappingBodies()) {
            if (IsInvalidHitscan(body)) {
                continue;
            }

            var peerId = long.Parse(body.Name);
            var enemy = GetPlayerCamera(peerId);
            if (enemy == null) {
                continue;
            }

            // Direction from enemy to self (Tanger)
            var toSelf = GlobalPosition - enemy.GlobalPosition;
            toSelf = toSelf.Normalized();

            // Enemy's forward direction
            var enemyForward = -enemy.GlobalTransform.Basis.Z;

            // Dot product to check if enemy is facing this character
            var dot = enemyForward.Dot(toSelf);
            if (dot > 0.5f) {
                GetPlayerState(peerId)?.RpcId(peerId, nameof(State.RequestBlind));
            }
        }
    }

    private void StartAttack(string animationName) {
        _currentAbility = animationName; // track current ability

        _state!.IsChainable = false;

        _attackTimer = 0f;
        _isAttacking = true;
        _state!.StartAttack();

        var animationLength = _animator!.AnimationTree!.GetAnimation(animationName).Length;
        _attackResetTime = animationLength;

        _animator.PlayAbilities(animationName);
    }


    private void ResetAttackState() {
        _state!.ResetAttack();
        ResetAttack();
    }

    public void ResetAttack() {
        _currentAbility = string.Empty;
        _state!.IsChainable = true;
        _isAttacking = false;
        _attackTimer = 0f;

        Dynamic!.Monitoring = false;
        Whole!.Monitoring = false;
        Big!.Monitoring = false;

        _animator!.ResetAbilities();
    }

    private void CheckBigForHits() {
        foreach (var body in Big!.GetOverlappingBodies()) {
            if (IsInvalidHitscan(body)) {
                continue;
            }

            var peerId = long.Parse(body.Name);
            GetPlayerState(peerId)?.RpcId(peerId, nameof(State.RequestHealthChange), -400);
            GetPlayerState(peerId)?.RpcId(peerId, nameof(State.RequestResistanceChange), -25);
        }
    }
}
