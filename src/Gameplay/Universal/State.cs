namespace Misled.Gameplay.Universal;

using System;
using Godot;
using Misled.Gameplay.Model;

public partial class State : Node {
    public static State? Instance { get; private set; }

    public NormalConfig? NormalConfig { get; set; }

    public Elemental? Elemental { get; set; }

    public int Health = 10000;
    public int Resistance = 1000;
    public int Stamina = 100;

    public bool IsAttacking { get; set; }
    public bool IsIrrevocable { get; set; }
    public bool IsInterruptable { get; set; }

    public Action? OnAttackStarted { get; set; }
    public Action? OnAttackEnded { get; set; }
    public Action<int>? OnDamageReceived { get; set; }

    public override void _Ready() => Instance = this;

    public void StartAttack() {
        IsAttacking = true;
        OnAttackStarted?.Invoke();
    }

    public void ResetAttack() {
        IsAttacking = false;
        OnAttackEnded?.Invoke();
    }

    // ──────────────── HEALTH ────────────────

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestHealthChange(int amount) {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        Health += amount;
        OnDamageReceived?.Invoke(Multiplayer.GetRemoteSenderId());
        Rpc(nameof(SyncHealth), Health);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncHealth(int value) =>
        Health = value;

    // ──────────────── RESISTANCE ────────────────

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestResistanceChange(int amount) {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        Resistance += amount;
        Rpc(nameof(SyncResistance), Resistance);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncResistance(int value) =>
        Resistance = value;

    // ──────────────── STAMINA ────────────────

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RequestStaminaChange(int amount) {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        Stamina += amount;
        Rpc(nameof(SyncStamina), Stamina);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncStamina(int value) =>
        Stamina = value;
}
