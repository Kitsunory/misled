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

    public Elemental? Elemental { get; set; }

    public float Health = 10000;
    public float Resistance = 1000;
    public float Stamina = 100;

    public bool IsAttacking { get; set; }
    public bool IsChainable { get; set; }
    public bool IsIrrevocable { get; set; } = true;
    public bool IsInterruptable { get; set; } = true;

    public bool IsSpy { get; set; }
    public bool IsBloodstained { get; set; }

    public Action? OnNormalAttack { get; set; }
    public Action? OnReposture { get; set; }
    public Action<bool>? OnAttackStarted { get; set; }
    public Action? OnAttackEnded { get; set; }
    public Action<int>? OnDamageReceived { get; set; }

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
    public void RequestBloodstain() {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        IsBloodstained = true;
        OnBloodstained?.Invoke();
        Rpc(nameof(SyncBloodstain), IsSpy);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ResetBloodstain() {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        IsBloodstained = false;
        OnBloodstainReset?.Invoke();
        Rpc(nameof(SyncBloodstain), IsSpy);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncBloodstain(bool value) =>
        IsBloodstained = value;

    // ──────────────── HEALTH ────────────────

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestHealthChange(float amount) {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        if (Health + amount < Health) {
            OnDamageReceived?.Invoke(Multiplayer.GetRemoteSenderId());
            PlayersScore[Multiplayer.GetRemoteSenderId()] -= amount;
        }
        Health += amount;
        Rpc(nameof(SyncHealth), Health);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncHealth(float value) =>
        Health = value;

    // ──────────────── RESISTANCE ────────────────

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestResistanceChange(float amount) {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        Resistance += amount;
        Rpc(nameof(SyncResistance), Resistance);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncResistance(float value) =>
        Resistance = value;

    // ──────────────── STAMINA ────────────────

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestStaminaChange(float amount) {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        Stamina += amount;
        Rpc(nameof(SyncStamina), Stamina);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncStamina(float value) =>
        Stamina = value;
}
