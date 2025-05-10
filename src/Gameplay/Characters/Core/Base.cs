namespace Misled.Gameplay.Core;

using Godot;
using Misled.Gameplay.Universal;

/// <summary>
/// Base class for all characters in the game.
/// </summary>
public abstract partial class Base : CharacterBody3D {
    /// <summary>
    /// Gets the unique identifier for the character.
    /// </summary>
    public abstract string CharacterId { get; }

    protected State? _state;
    protected Movement? _movement;
    protected Normal? _normal;
    protected Animator? _animator;

    [Export] public Camera3D? Camera;
    [Export] public AnimationTree? AnimationTree;
    [Export] public AnimationPlayer? AnimationPlayer;
    [Export] public GpuParticles3D? Particles;
    [Export] public AudioStreamPlayer3D? AudioPlayer;
    [Export] public Animator? Animator; // Enforce type here
    [Export] public State? State; // Enforce type here

    [Export] public float MoveSpeed = 7.0f;
    [Export] public float JumpForce = 6.0f;
    [Export] public float Acceleration = 10f;
    [Export] public float Deceleration = 8f;
    [Export] public float BlendSmoothSpeed = 20f;
    [Export] public int MaxJumps = 2;

    /// <summary>
    /// Called when the node enters the scene tree.
    /// </summary>
    public override void _Ready() {
        if (!AreDependenciesValid()) {
            GD.PrintErr("Missing required exported nodes. Check the editor.");
            return;
        }

        _animator = Animator;
        _state = State;

        InitSystems();
        _movement?.Ready();

        Multiplayer.MultiplayerPeer.SetTransferMode(MultiplayerPeer.TransferModeEnum.UnreliableOrdered);
    }

    /// <summary>
    /// Initializes the character's systems (movement, normal attacks).
    /// </summary>
    protected void InitSystems() {
        if (_state == null || _animator == null) {
            GD.PrintErr("State or Animator is null. InitSystems cannot proceed.");
            return;
        }

        _movement = new Movement(_state, this, _animator, Particles!, Camera!, AudioPlayer!, MoveSpeed, JumpForce, Acceleration, Deceleration, MaxJumps);
        _normal = new Normal(_state, _animator, _state.NormalConfig!);
    }

    /// <summary>
    /// Called during the physics processing step of the main loop.
    /// </summary>
    /// <param name="delta">The time elapsed since the previous physics update.</param>
    public override void _PhysicsProcess(double delta) {
        if (!IsMultiplayerAuthority()) {
            return;
        }
        var dt = (float)delta;
        _movement?.Update(dt);
        _normal?.Update(dt);
        _animator?.UpdatePhysicsProcessDeltaTime(dt);

        Rpc(nameof(SendMovement), GlobalTransform.Origin, Velocity, Rotation);
    }

    /// <summary>
    /// Synchronizes the character's movement across the network.
    /// </summary>
    /// <param name="position">The character's position.</param>
    /// <param name="velocity">The character's velocity.</param>
    /// <param name="rotation">The character's rotation.</param>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)] // Authority mode, don't call locally
    public void SendMovement(Vector3 position, Vector3 velocity, Vector3 rotation) {
        // Only apply transform on non-authority peer
        if (!IsMultiplayerAuthority()) {
            GlobalTransform = new Transform3D(Basis.Identity.Rotated(Vector3.Up, rotation.Y), position);
        }
    }

    protected State? GetPlayerState(long peerId) {
        var world = GetTree().Root.GetNode("World");

        var enemyBody = world.GetNodeOrNull<CharacterBody3D>(peerId.ToString());
        if (enemyBody == null) {
            GD.PrintErr("Enemy body not found");
            return null;
        }

        var enemyState = enemyBody.GetNodeOrNull<State>("State");
        if (enemyState == null) {
            GD.PrintErr("Enemy state not found");
            return null;
        }

        return enemyState;
    }

    public CharacterBody3D? GetPlayerNode(long peerId) {
        var world = GetTree().Root.GetNode("World");

        var enemyBody = world.GetNodeOrNull<CharacterBody3D>(peerId.ToString());
        if (enemyBody == null) {
            GD.PrintErr("Enemy body not found");
            return null;
        }

        return enemyBody;
    }

    protected bool IsInvalidHitscan(Node body) =>
            !body.IsInGroup("Players") || long.Parse(body.Name) == Multiplayer.GetUniqueId();

    /// <summary>
    /// Validates that all required exported nodes are assigned.
    /// </summary>
    /// <returns><c>true</c> if all dependencies are valid; otherwise, <c>false</c>.</returns>
    private bool AreDependenciesValid() =>
        Camera != null && AnimationTree != null && AnimationPlayer != null && Particles != null && Animator != null && State != null;

    /// <summary>
    /// Determines whether this node has multiplayer authority.
    /// </summary>
    /// <returns><c>true</c> if this node has multiplayer authority; otherwise, <c>false</c>.</returns>
    private new bool IsMultiplayerAuthority() => base.IsMultiplayerAuthority();
}
