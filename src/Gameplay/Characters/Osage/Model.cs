namespace Misled.Gameplay.Osage;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Misled.Gameplay.Core;
using Misled.Gameplay.Model;
using Misled.Gameplay.Universal;

public partial class Model : Base {
    [Export] public Area3D? Dynamic;
    [Export] public Area3D? Whole;
    [Export] public Area3D? Area;

    private int _damageCounter;
    private int _lastScore;

    private readonly List<long> _playersInArea = [];
    private int CurrentBloodstainedInArea => GetBloodstainedPlayersInArea().Count;

    private bool _isAttacking;
    private float _attackTimer;
    private float _attackResetTime;

    private Node3D? _exclusiveTarget;
    private bool _awaitingExecution;

    public override string CharacterId => "Osage";

    public override void _Ready() {
        if (!IsMultiplayerAuthority()) {
            return;
        }

        base._Ready();
        InitializeState();
        InitializeAreas();

        _state!.OnNormalAttack += HandleNormalAttack;

        _state!.OnNormalAttackEnded += () => Dynamic!.Monitoring = false;
        _state!.OnBreakCallback += HandleBreakCallback;

        _lastScore = (int)_state.PlayersScore.GetValueOrDefault(Multiplayer.GetUniqueId(), 0);
        _state!.ChangeSkillCooldown("Exclusive", 20000.0f);

        InitSystems();
    }

    private async void HandleNormalAttack() {
        Dynamic!.Monitoring = true;
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");

        var attackNumber = GD.RandRange(1, 3);
        RequestPlaySFX($"Osage/NA{attackNumber}.wav");
    }

    private void InitializeState() =>
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

    private void InitializeAreas() {
        Dynamic!.BodyEntered += OnDynamicBodyEntered;
        Whole!.BodyEntered += OnWholeBodyEntered;
        Area!.BodyEntered += OnAreaEntered;
        Area!.BodyExited += OnAreaExited;
    }

    private void OnDynamicBodyEntered(Node body) {
        if (IsInvalidHitscan(body)) {
            return;
        }

        var peerId = GetPeerId(body);
        DealDamageAndBreak(body, -300, -50);
        RequestImmobilized(peerId, 0.5f);
    }

    private void OnWholeBodyEntered(Node body) {
        if (IsInvalidHitscan(body)) {
            return;
        }

        var peerId = GetPeerId(body);
        var state = GetPlayerState(peerId);
        if (state!.IsParrying) {
            return;
        }
        if (!state!.IsBloodstained) {
            GetPlayerState(peerId)?.RpcId(peerId, nameof(State.RequestBloodstain));
            _state!.ChangeSkillCooldown("Signature", -11.0f);
            PlaySFX("Osage/Bloodstain.mp3");
        }
        PlaySFX("Osage/Signature_Hit.mp3");
        DealDamageAndBreak(body, -500, -75);
        RequestImmobilized(peerId, 0.9f);
    }

    private void DealDamageAndBreak(Node body, int damage, int breakDamage) {
        var peerId = GetPeerId(body);
        RequestDealDamage(peerId, damage);
        RequestDealBreak(peerId, breakDamage);
    }

    private static long GetPeerId(Node body) => long.Parse(body.Name);

    private void OnAreaEntered(Node body) {
        if (long.TryParse(body.Name, out var peerId) && !_playersInArea.Contains(peerId)) {
            _playersInArea.Add(peerId);
        }
    }

    private void OnAreaExited(Node body) {
        if (long.TryParse(body.Name, out var peerId)) {
            _playersInArea.Remove(peerId);
        }
    }


    private async void HandleBreakCallback(int requester) {
        while (_state!.IsAttacking) {
            await Task.Delay(50);
        }

        _state!.IsAttacking = true;
        _state!.IsInterruptable = false;
        _animator!.PlayAbilities("Break");
        RequestDealDamage(requester, -2000);

        await Task.Delay(1160);
        _state!.IsAttacking = false;
        _state!.IsInterruptable = true;

        _animator!.ResetAbilities();
    }

    private List<long> GetBloodstainedPlayersInArea() =>
        _playersInArea.Where(peerId => GetPlayerState(peerId)?.IsBloodstained == true).ToList();

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);
        if (!IsMultiplayerAuthority()) {
            return;
        }
        UpdateAttackTimer(delta);
        HandleAttackInputs();
        TrackScore();
    }

    private void UpdateAttackTimer(double delta) {
        if (!_isAttacking) {
            return;
        }

        _attackTimer += (float)delta;

        if (_attackTimer > _attackResetTime) {
            ResetAttackState();
        }
    }

    private void TrackScore() {
        var peerId = Multiplayer.GetUniqueId();
        var currentScore = (int)_state!.PlayersScore.GetValueOrDefault(peerId, 0);

        if (currentScore > _lastScore) {
            var deltaScore = currentScore - _lastScore;
            _damageCounter += deltaScore;

            _state.Monocoins = _damageCounter / 1000;
            _lastScore = currentScore;
        }
    }

    private void HandleAttackInputs() {
        if (_state!.IsAttacking || _state!.IsImmobilized) {
            return;
        }

        if (_awaitingExecution && Input.IsActionJustPressed("Normal")) {
            _awaitingExecution = false;
            ExecuteExclusive();
            return;
        }

        if (Input.IsActionJustPressed("Signature")) {
            if (_state!.CheckCooldownOrNull("Signature") != null)
                return;
            TryStartAttack("Signature", reach: false, isSignature: true);
        }

        if (Input.IsActionJustPressed("Alternate")) {
            if (_state!.CheckCooldownOrNull("Alternate") != null)
                return;
            HandleAlternateAttack();
        }

        if (Input.IsActionJustPressed("Exclusive")) {
            if (_state!.IsSpy) {
                return;
            }
            if (_state!.CheckCooldownOrNull("Exclusive1") != null)
                return;

            TryStartAttack("Exclusive1", isExclusive: true);
        }

    }

    private void HandleAlternateAttack() {
        var bloodstained = GetBloodstainedPlayersInArea();
        var count = bloodstained.Count;

        if (count > 0 && _state!.Monocoins > 0) {
            _state!.ChangeSkillCooldown("Alternate", 4.0f);
            PlaySFX("Osage/Coin.wav");
            TryStartAttack("Alternate", skipAttackState: true);
            _ = DelayedAlternateDamage(bloodstained, count);
        }
        else {
            GD.Print("No bloodstained players nearby. Alternate canceled.");
        }
    }

    private void TryStartAttack(string animationName, bool skipAttackState = false, bool reach = true, bool isSignature = false, bool isExclusive = false) {
        if (_state!.IsAttacking && !_state.IsChainable && !skipAttackState) {
            return;
        }

        if (isExclusive) {
            _ = StartExclusiveUltimate();
            return;
        }

        if (!skipAttackState) {
            _state!.StartAttack(reach);
        }
        StartAttack(animationName, skipAttackState, reach, isSignature);
    }

    private async Task StartExclusiveUltimate() {
        _exclusiveTarget = GetNearestBloodstainedEnemy();
        if (_exclusiveTarget == null) {
            GD.Print("No valid exclusive target found.");
            return;
        }
        _state!.ChangeSkillCooldown("Exclusive", 20000.0f);
        PlaySFX("Osage/Exclusive_Spin.wav");

        LookAt(_exclusiveTarget.GlobalPosition, Vector3.Up);
        RequestImmobilized(GetPeerId(_exclusiveTarget), 6.6f);

        StartAttack("Exclusive1");
        _awaitingExecution = true;

        await ToSignal(GetTree().CreateTimer(2.15f), "timeout");
        _state!.IsInterruptable = false;

        var timer = GetTree().CreateTimer(4.45f);
        var normalPressed = false;

        while (timer.TimeLeft > 0) {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (Input.IsActionJustPressed("Normal")) {
                normalPressed = true;
                break;
            }
        }

        if (normalPressed) {
            ExecuteExclusive();
        }
        else {
            _state!.IsInterruptable = true;
            _awaitingExecution = false;
        }
    }

    private Node3D? GetNearestBloodstainedEnemy() {
        Node3D? closest = null;
        var closestDistance = float.MaxValue;

        foreach (var body in Area!.GetOverlappingBodies()) {
            if (IsInvalidHitscan(body)) {
                continue;
            }

            var peerId = GetPeerId(body);
            var state = GetPlayerState(peerId);
            if (state == null || !state.IsBloodstained) {
                continue;
            }

            var dist = GlobalPosition.DistanceTo(body.GlobalPosition);
            if (dist < closestDistance) {
                closest = body;
                closestDistance = dist;
            }
        }

        return closest;
    }

    private async void ExecuteExclusive() {
        if (_exclusiveTarget == null) {
            return;
        }
        PlaySFX("Osage/Coin.wav");
        UIPlayer!.Play("Marker");

        _awaitingExecution = false;

        var peerId = GetPeerId(_exclusiveTarget);

        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        StartAttack("Exclusive2");
        PlaySFX("Osage/Exclusive_Shot.mp3");

        var targetState = GetPlayerState(peerId);
        if (targetState == null) {
            GD.Print("Exclusive failed: target has no state.");
            return;
        }

        if (targetState.IsParrying) {
            GD.Print("Exclusive attack was parried!");
        }
        else {
            RequestDealDamage(peerId, -4000);
            GD.Print("Exclusive attack succeeded!");
        }

        _state!.IsInterruptable = true;
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
            _state.ChangeSkillCooldown("Signature", 12.0f);
            RequestPlaySFX($"Osage/Signature.wav");
            _ = HandleSignatureDash(5f, 1f);
        }
    }

    private async Task DelayedAlternateDamage(List<long> bloodstained, int count) {
        await Task.Delay(700);

        const int coinDamage = 300;
        var damage = coinDamage * _state!.Monocoins / count;
        var maxDamage = 1500 + (500 * count);
        var cappedDamage = Mathf.Min(damage, maxDamage);

        foreach (var peerId in bloodstained) {
            RequestDealDamage(peerId, -cappedDamage);
            GetPlayerState(peerId)?.RpcId(peerId, nameof(State.ResetBloodstain));
        }

        PlaySFX("Osage/Alternate.wav");
        _damageCounter = 0;
        _state.Monocoins = 0;
        _lastScore = (int)_state!.PlayersScore.GetValueOrDefault(Multiplayer.GetUniqueId(), 0);
    }


    private async Task HandleSignatureDash(float distance, float duration) {
        Whole!.Monitoring = true;

        var start = GlobalPosition;
        var forward = -GlobalTransform.Basis.Z;
        var target = start + (forward * distance);

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
    }

    private static float EaseOutExpo(float t) =>
        t == 1f ? 1f : 1 - Mathf.Pow(2, -10 * t);

    private void ResetAttackState() {
        _state!.ResetAttack();
        ResetAttack();
    }

    private void ResetAttack() {
        _exclusiveTarget = null;
        _awaitingExecution = false;
        _isAttacking = false;
        _attackTimer = 0f;

        _animator!.ResetAbilities();
        Whole!.Monitoring = false;
        Dynamic!.Monitoring = false;
        _state!.IsInterruptable = true;
    }
}
