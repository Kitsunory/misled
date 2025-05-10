namespace Misled.Gameplay.Universal;

using global::System;
using Godot;
using Misled.Gameplay.Model;

public partial class State : Node {
    public static State? Instance { get; private set; }
    public NormalConfig? NormalConfig { get; set; }
    public Elemental? Elemental { get; set; }

    private int _health = 10000;
    [Export]
    public int Health {
        get => _health;
        set {
            if (_health != value && IsMultiplayerAuthority()) {
                _health = value;
                SendState();
            }
            else {
                _health = value;
            }
        }
    }

    private int _resistance = 1000;
    [Export]
    public int Resistance {
        get => _resistance;
        set {
            if (_resistance != value && IsMultiplayerAuthority()) {
                _resistance = value;
                SendState();
            }
            else {
                _resistance = value;
            }
        }
    }

    private int _stamina = 100;
    [Export]
    public int Stamina {
        get => _stamina;
        set {
            if (_stamina != value && IsMultiplayerAuthority()) {
                _stamina = value;
                SendState();
            }
            else {
                _stamina = value;
            }
        }
    }

    public override void _Ready() =>
        Instance = this;

    public void SendState() =>
        Rpc(nameof(SyncState), Health, Resistance, Stamina);

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncState(int health, int resistance, int stamina) {
        Health = health;
        Resistance = resistance;
        Stamina = stamina;
    }

    public void SendHealthChange(int amount) =>
        Rpc(nameof(SyncHealthChange), amount);

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncHealthChange(int amount) =>
        Health += amount;

    public void SendResistanceChange(int amount) =>
        Rpc(nameof(SyncResistanceChange), amount);

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncResistanceChange(int amount) =>
        Resistance += amount;

    public bool IsAttacking { get; set; }
    public Action? OnAttackStarted { get; set; }
    public Action? OnResetAttack { get; set; }

    public void StartAttack() =>
        OnAttackStarted?.Invoke();

    public void ResetAttack() =>
        OnResetAttack?.Invoke();
}
