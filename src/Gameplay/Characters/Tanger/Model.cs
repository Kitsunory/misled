namespace Misled.Gameplay.Tanger;

using System.Collections.Generic;
using System.Threading.Tasks;
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
    private readonly float[] _signatureHitTimes = [0.4f, 0.7f, 1.0f];
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
        Whole!.BodyEntered += OnWholeBodyEntered;
        _state.OnNormalAttack += HandleNormalAttack;
        _state.OnNormalAttackEnded += HandleNormalAttackEnded;
        _state.OnBreakCallback += HandleBreakCallback;
        _state.ChangeSkillCooldown("Exclusive", 20000f);

        InitSystems();
    }

    private void OnDynamicBodyEntered(Node body) {
        if (IsInvalidHitscan(body)) { return; }

        var peerId = long.Parse(body.Name);
        RequestDealDamage(peerId, -400);
        RequestDealBreak(peerId, -40);
        RequestImmobilized(peerId, 0.5f);
    }

    private void OnWholeBodyEntered(Node body) {
        if (IsInvalidHitscan(body)) { return; }

        var peerId = long.Parse(body.Name);
        RequestDealDamage(peerId, -500);
        RequestDealBreak(peerId, -50);
        RequestImmobilized(peerId, 0.5f);
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);
        if (!IsMultiplayerAuthority()) {
            return;
        }

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
            RequestPlaySFX($"Tanger/Exclusive_Snap.mp3");
            RequestPlaySFX($"Tanger/Exclusive_Cine.mp3");
            TriggerSpyEffect();
            _state!.IsInterruptable = true;
            _hasTriggeredSpy = true;
        }

        if (_attackTimer > _attackResetTime) {
            ResetAttackState();
        }
    }

    private async void HandleBreakCallback(int requester) {
        while (_state!.IsAttacking) {
            await Task.Delay(50);
        }

        _state.IsAttacking = true;
        _state.IsInterruptable = false;
        _animator!.PlayAbilities("Break");

        MoveAndFaceTarget(requester);

        await Task.Delay(1000);

        MoveAndFaceTarget(requester);

        await Task.Delay(100);
        RequestDealDamage(requester, -1000);

        await Task.Delay(1100);
        RequestDealDamage(requester, -1000);

        await Task.Delay(1050);
        _state.IsAttacking = false;
        _state.IsInterruptable = true;
        _animator!.ResetAbilities();
    }

    private void MoveAndFaceTarget(int requester) {
        var targetNode = GetPlayerNode(requester);
        if (targetNode == null)
            return;

        var selfPos = GlobalTransform.Origin;
        var targetPos = targetNode.GlobalTransform.Origin;

        // Keep on same Y plane
        targetPos.Y = selfPos.Y;

        // Face the enemy
        LookAt(targetPos, Vector3.Up);

        // Move closer, but stop a short distance away
        var stopDistance = 0.5f;
        var toTarget = targetPos - selfPos;
        var currentDistance = toTarget.Length();

        if (currentDistance > stopDistance) {
            var moveDir = toTarget.Normalized();
            var moveAmount = Mathf.Min(currentDistance - stopDistance, 2.0f); // up to 2 units per call
            var newPos = selfPos + (moveDir * moveAmount);

            GlobalTransform = new Transform3D(GlobalTransform.Basis, newPos);
        }
    }

    private async void HandleNormalAttack() {
        Dynamic!.Monitoring = true;
        await ToSignal(GetTree().CreateTimer(0.4f), "timeout");

        var attackNumber = GD.RandRange(1, 3);
        RequestPlaySFX($"Tanger/NA{attackNumber}.wav");
    }

    private void HandleNormalAttackEnded() => Dynamic!.Monitoring = false;

    private void HandleAttackInputs() {
        if (_state!.IsAttacking && _state.IsChainable) {
            Dynamic!.Monitoring = false;
        }

        if (_state!.IsImmobilized) {
            return;
        }

        if (Input.IsActionJustPressed("Signature")) {
            if (_state.CheckCooldownOrNull("Signature") != null) {
                return;
            }
            TryStartAttack("Signature");
        }

        if (Input.IsActionJustPressed("Alternate")) {
            if (_state.CheckCooldownOrNull("Alternate") != null) {
                return;
            }
            TryStartAttack("Alternate");
        }

        if (Input.IsActionJustPressed("Exclusive")) {
            if (_state!.IsSpy) {
                return;
            }
            if (_state.CheckCooldownOrNull("Exclusive") != null) {
                return;
            }
            TryStartAttack("Exclusive");
            _state.ChangeSkillCooldown("Exclusive", 20000f);
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
            RequestPlaySFX($"Tanger/Signature.wav");
            _signatureHitIndex = 0;
            _state.ChangeSkillCooldown("Signature", 8.0f);
        }
        else if (animationName == "Alternate") {
            Whole!.Monitoring = true;
            Area!.Monitoring = true;
            _state.ChangeSkillCooldown("Alternate", 6.0f);
            CheckAreaForViewers();
        }
        else if (animationName == "Exclusive") {
            _hasTriggeredSpy = false;
            _state!.IsInterruptable = false;
            RequestPlaySFX($"Tanger/Exclusive.wav");
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
        await ToSignal(GetTree().CreateTimer(0.45f), "timeout");
        RequestPlaySFX($"Tanger/Alternate.wav");
        await ToSignal(GetTree().CreateTimer(0.10f), "timeout");

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

        await ToSignal(GetTree().CreateTimer(1.10f), "timeout");
        ResetAttackState();
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
            RequestDealDamage(peerId, -400);
            RequestDealBreak(peerId, -50);
            RequestImmobilized(peerId, 1);
        }
    }
}
