namespace Misled.Gameplay.Universal;

using System;
using System.Collections.Generic;
using Godot;
using Misled.Gameplay.Core;
using Misled.Gameplay.Model;

public partial class State : Node {
    public static State? Instance { get; private set; }

    public Dictionary<long, float> PlayersScore = [];

    public NormalConfig? NormalConfig { get; set; }

    public string? CurrentName { get; set; }
    public Elemental? Elemental { get; set; }
    public Dictionary<string, float> SkillCooldowns { get; set; } = [];

    public void ChangeSkillCooldown(string skillName, float amount) {
        if (SkillCooldowns.TryGetValue(skillName, out var value)) {
            SkillCooldowns[skillName] = Mathf.Max(value + amount, 0f);
        }
        else {
            SkillCooldowns[skillName] = amount;
        }
    }


    public float? CheckCooldownOrNull(string skillName) {
        if (SkillCooldowns.TryGetValue(skillName, out var remaining) && remaining > 0f) {
            return remaining;
        }
        return null;
    }

    private float _health = 10000;
    public float Health {
        get => _health;
        set {
            if (IsMultiplayerAuthority()) {
                _health = value;
                Rpc(nameof(SyncHealth), _health);
            }
        }
    }

    private float _resistance = 1000;
    public float Resistance {
        get => _resistance;
        set {
            if (IsMultiplayerAuthority()) {
                _resistance = value;
                Rpc(nameof(SyncResistance), _resistance);
            }
        }
    }
    private float _stamina = 100;
    public float Stamina {
        get => _stamina;
        set {
            if (IsMultiplayerAuthority()) {
                _stamina = value;
                Rpc(nameof(SyncStamina), _stamina);
            }
        }
    }

    public bool IsAttacking { get; set; }
    public bool IsChainable { get; set; }
    public bool IsIrrevocable { get; set; } = true;
    public bool IsInterruptable { get; set; } = true;

    private bool _isImmobilized;
    public bool IsImmobilized {
        get => _isImmobilized;
        set {
            if (_isImmobilized == value) {
                return;
            }

            _isImmobilized = value;

            if (IsMultiplayerAuthority()) {
                Rpc(nameof(SyncImmobilized), _isImmobilized);
            }
        }
    }

    private bool _isParrying;
    public bool IsParrying {
        get => _isParrying;
        set {
            _isParrying = value;
            Rpc(nameof(SyncParry), _isParrying);
        }
    }

    public bool IsSpy { get; set; }
    public bool IsBloodstained { get; set; }
    public int Monocoins { get; set; }

    public Action? OnNormalAttack { get; set; }
    public Action? OnNormalAttackEnded { get; set; }
    public Action? OnReposture { get; set; }
    public Action<bool>? OnAttackStarted { get; set; }
    public Action? OnAttackEnded { get; set; }
    public Action<int>? OnDamageReceived { get; set; }
    public Action<int>? OnBreakCallback { get; set; }
    public Action<int>? OnBreak { get; set; }
    public Action<int>? OnParry { get; set; }
    public Action? OnDeath { get; set; }

    public Action? OnBlinded { get; set; }
    public Action? OnSpyed { get; set; }
    public Action? OnBloodstained { get; set; }
    public Action? OnBloodstainReset { get; set; }

    public override void _Ready() {
        Instance = this;

        foreach (var playerId in NetworkManager.Instance!.GetAllPlayers().Keys) {
            PlayersScore[playerId] = 0;
        }
    }

    public void StartAttack(bool reach = true) {
        IsAttacking = true;
        OnAttackStarted?.Invoke(reach);
    }

    public void ResetAttack() {
        IsAttacking = false;
        OnAttackEnded?.Invoke();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestBreakCallback(int requester) {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        OnBreakCallback?.Invoke(requester);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestBlind() {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        OnBlinded?.Invoke();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestSpy() {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        IsSpy = true;
        OnSpyed?.Invoke();
        Rpc(nameof(SyncSpy), IsSpy);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ResetSpy() {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        IsSpy = false;
        Rpc(nameof(SyncSpy), IsSpy);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncSpy(bool value) =>
            IsSpy = value;

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void RequestImmobilized(float time) {
        if (!IsMultiplayerAuthority()) {
            return;
        }

        IsImmobilized = true;
        Rpc(nameof(SyncImmobilized), IsImmobilized);

        await ToSignal(GetTree().CreateTimer(time), "timeout");

        IsImmobilized = false;
        Rpc(nameof(SyncImmobilized), IsImmobilized);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncImmobilized(bool value) =>
        _isImmobilized = value;

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestBloodstain() {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        IsBloodstained = true;
        OnBloodstained?.Invoke();
        Rpc(nameof(SyncBloodstain), IsBloodstained);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ResetBloodstain() {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        IsBloodstained = false;
        OnBloodstainReset?.Invoke();
        Rpc(nameof(SyncBloodstain), IsBloodstained);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncBloodstain(bool value) =>
        IsBloodstained = value;


    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncParry(bool value) =>
        IsParrying = value;

    // ──────────────── HEALTH ────────────────

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestHealthChange(float amount) {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        if (IsParrying) {
            OnParry?.Invoke(Multiplayer.GetRemoteSenderId());
            return;
        }
        if (Health + amount < Health) {
            OnDamageReceived?.Invoke(Multiplayer.GetRemoteSenderId());
            PlayersScore[Multiplayer.GetRemoteSenderId()] -= amount;
        }
        Health += amount;
        if (Health <= 0) {
            OnDeath?.Invoke();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncHealth(float value) =>
        _health = value;

    // ──────────────── RESISTANCE ────────────────

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestResistanceChange(float amount) {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        var init = Resistance;
        Resistance += amount;
        if (Resistance <= 0 && init > 0) {
            OnBreak?.Invoke(Multiplayer.GetRemoteSenderId());
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncResistance(float value) =>
        _resistance = value;

    // ──────────────── STAMINA ────────────────

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestStaminaChange(float amount) {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        Stamina += amount;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncStamina(float value) =>
        _stamina = value;
}
