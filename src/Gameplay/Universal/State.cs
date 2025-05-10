namespace Misled.Gameplay.Universal;

using System;
using Godot;
using Misled.Gameplay.Model;

/// <summary>
/// Manages the character's state, including health, resistance, stamina, and attack state.
/// Synchronizes state across the network.
/// </summary>
public partial class State : Node {
    /// <summary>
    /// Gets the singleton instance of the <see cref="State"/> class.
    /// </summary>
    public static State? Instance { get; private set; }

    /// <summary>
    /// Gets or sets the normal attack configuration.
    /// </summary>
    public NormalConfig? NormalConfig { get; set; }

    /// <summary>
    /// Gets or sets the elemental configuration.
    /// </summary>
    public Elemental? Elemental { get; set; }

    private int _health = 10000;
    private int _resistance = 1000;
    private int _stamina = 100;

    /// <summary>
    /// Gets or sets the character's health.  Changes are synchronized across the network.
    /// </summary>
    [Export]
    public int Health {
        get => _health;
        set => SetState(ref _health, value);
    }

    /// <summary>
    /// Gets or sets the character's resistance. Changes are synchronized across the network.
    /// </summary>
    [Export]
    public int Resistance {
        get => _resistance;
        set => SetState(ref _resistance, value);
    }

    /// <summary>
    /// Gets or sets the character's stamina. Changes are synchronized across the network.
    /// </summary>
    [Export]
    public int Stamina {
        get => _stamina;
        set => SetState(ref _stamina, value);
    }

    /// <summary>
    /// Called when the node enters the scene tree.
    /// </summary>
    public override void _Ready() => Instance = this;

    /// <summary>
    /// Sets the state property and synchronizes it across the network if the node has multiplayer authority.
    /// </summary>
    /// <param name="field">A reference to the backing field for the state property.</param>
    /// <param name="newValue">The new value for the state property.</param>
    private void SetState(ref int field, int newValue) {
        if (field == newValue) {
            return;
        }

        if (IsMultiplayerAuthority()) {
            field = newValue;
            Rpc(nameof(SyncState), Health, Resistance, Stamina);
        }
        else {
            field = newValue;
        }
    }

    /// <summary>
    /// Synchronizes the character's state (health, resistance, stamina) across the network.
    /// </summary>
    /// <param name="health">The character's health.</param>
    /// <param name="resistance">The character's resistance.</param>
    /// <param name="stamina">The character's stamina.</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncState(int health, int resistance, int stamina) {
        _health = health;
        _resistance = resistance;
        _stamina = stamina;
    }

    /// <summary>
    /// Sends a request to change the character's health by the specified amount.
    /// </summary>
    /// <param name="amount">The amount to change the character's health by.</param>
    public void SendHealthChange(int amount) => Rpc(nameof(SyncHealthChange), amount);

    /// <summary>
    /// Synchronizes a change in the character's health across the network.
    /// </summary>
    /// <param name="amount">The amount to change the character's health by.</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncHealthChange(int amount) => Health += amount;

    /// <summary>
    /// Sends a request to change the character's resistance by the specified amount.
    /// </summary>
    /// <param name="amount">The amount to change the character's resistance by.</param>
    public void SendResistanceChange(int amount) => Rpc(nameof(SyncResistanceChange), amount);

    /// <summary>
    /// Synchronizes a change in the character's resistance across the network.
    /// </summary>
    /// <param name="amount">The amount to change the character's resistance by.</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncResistanceChange(int amount) => Resistance += amount;

    /// <summary>
    /// Gets or sets a value indicating whether the character is currently attacking.
    /// </summary>
    public bool IsAttacking { get; set; }

    /// <summary>
    /// An event that is invoked when the character starts an attack.
    /// </summary>
    public Action? OnAttackStarted { get; set; }

    /// <summary>
    /// An event that is invoked when the character ends an attack.
    /// </summary>
    public Action? OnAttackEnded { get; set; }

    /// <summary>
    /// Starts an attack.
    /// </summary>
    public void StartAttack() {
        IsAttacking = true;
        OnAttackStarted?.Invoke();
    }

    /// <summary>
    /// Resets the attack state.
    /// </summary>
    public void ResetAttack() {
        IsAttacking = false;
        OnAttackEnded?.Invoke();
    }
}
