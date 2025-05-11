namespace Misled.Gameplay.Universal;

using System;
using Godot;
using Misled.Gameplay.Model;

public partial class State : Node {
    public static State? Instance { get; private set; }

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

    public Action? OnNormalAttack { get; set; }
    public Action? OnReposture { get; set; }
    public Action? OnAttackStarted { get; set; }
    public Action? OnAttackEnded { get; set; }
    public Action<int>? OnDamageReceived { get; set; }
    public Action? OnBlinded { get; set; }
    public Action? OnSpyed { get; set; }

    public override void _Ready() => Instance = this;

    public void StartAttack() {
        IsAttacking = true;
        OnAttackStarted?.Invoke();
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
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void ResetSpy() {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        IsSpy = false;
    }

    // ──────────────── HEALTH ────────────────

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestHealthChange(float amount) {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        if (Health + amount < Health) {
            OnDamageReceived?.Invoke(Multiplayer.GetRemoteSenderId());
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
