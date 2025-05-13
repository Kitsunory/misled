namespace Misled.Gameplay.Core;

using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Misled.Gameplay.Universal;

public abstract partial class Base : CharacterBody3D {
    public abstract string CharacterId { get; }

    protected State? _state;
    protected Movement? _movement;
    protected Normal? _normal;
    protected Animator? _animator;

    [Export] public Camera3D? Camera;
    [Export] public AnimationTree? AnimationTree;
    [Export] public AnimationPlayer? AnimationPlayer;
    [Export] public AnimationPlayer? UIPlayer;
    [Export] public GpuParticles3D? Particles;
    [Export] public AudioStreamPlayer3D? AudioPlayer;
    [Export] public Animator? Animator;
    [Export] public State? State;
    [Export] public Sprite3D? Bloodstain;
    [Export] public AudioStreamPlayer3D? SFX;

    [Export] public float MoveSpeed = 7.0f;
    [Export] public float JumpForce = 6.0f;
    [Export] public float Acceleration = 10f;
    [Export] public float Deceleration = 8f;
    [Export] public float BlendSmoothSpeed = 20f;
    [Export] public int MaxJumps = 2;

    public override void _Ready() {
        if (!AreDependenciesValid()) {
            GD.PrintErr("Missing required exported nodes. Check the editor.");
            return;
        }

        _animator = Animator;
        _state = State;

        InitSystems();
        _movement?.Ready();

        _state!.OnBlinded += HandleBlind;
        _state!.OnSpyed += HandleSpy;
        _state!.OnBloodstained += HandleBloodstain;
        _state!.OnBloodstainReset += ResetBloodstain;
        _state!.OnBreak += HandleBreak;
        _state!.OnDeath += HandleDeath;
        _state!.OnParry += HandleImmobilize;

        Multiplayer.MultiplayerPeer.SetTransferMode(MultiplayerPeer.TransferModeEnum.UnreliableOrdered);
    }

    protected void InitSystems() {
        if (_state == null || _animator == null) {
            GD.PrintErr("State or Animator is null. InitSystems cannot proceed.");
            return;
        }

        _movement = new Movement(_state, this, _animator, Particles!, Camera!, AudioPlayer!, MoveSpeed, JumpForce, Acceleration, Deceleration, MaxJumps);
        _normal = new Normal(_state, _animator, _state.NormalConfig!);
    }

    [Export]
    public bool EnableInterpolation = true;

    private Vector3 _targetPosition;
    private Vector3 _targetVelocity;
    private Vector3 _targetRotation;
    private bool _hasReceivedMovement;
    private double _lastPacketTime;
    private double _predictedArrivalTime;
    private Vector3 _predictedFuturePos;

    private const float MAX_PREDICTION_TIME = 1f;
    private float _timeSinceLastUpdate;
    private double _lastMovementTime;

    public override void _PhysicsProcess(double delta) {
        var dt = (float)delta;

        if (IsMultiplayerAuthority()) {
            _movement?.Update(dt);
            _normal?.Update(dt);
            _animator?.UpdatePhysicsProcessDeltaTime(dt);

            Rpc(nameof(SendMovement), GlobalTransform.Origin, Velocity, Rotation, Camera!.GlobalTransform.Origin, Camera!.Basis);
            HandleCooldown(dt);
            HandleParry();

            if (_state!.Health < 10000) {
                _state.RequestHealthChange(100f * dt);
            }

            if (Input.IsPhysicalKeyPressed(Key.F10)) {
                EnableInterpolation = !EnableInterpolation;
                GD.Print($"Interpolation: {(EnableInterpolation ? "Enabled" : "Disabled")}");
            }
        }
        else {
            _timeSinceLastUpdate += dt;

            if (!EnableInterpolation) {
                // No interpolation - just teleport to last known position
                GlobalTransform = new Transform3D(
                    Basis.Identity.Rotated(Vector3.Up, _targetRotation.Y),
                    _targetPosition
                );
                return;
            }

            var now = Time.GetTicksMsec();
            var timeSinceLastPacket = (now - _lastPacketTime) / 1000.0;
            var dtf = (float)delta;

            // Interpolate toward predicted position
            var currentPos = GlobalTransform.Origin;
            var newPos = currentPos.Lerp(_predictedFuturePos, 5f * dtf);

            var currentRotY = Rotation.Y;
            var newRotY = Mathf.LerpAngle(currentRotY, _targetRotation.Y, 5f * dtf);
            var basis = Basis.Identity.Rotated(Vector3.Up, newRotY);

            GlobalTransform = new Transform3D(basis, newPos);
        }

    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void SendMovement(Vector3 position, Vector3 velocity, Vector3 rotation, Vector3 cameraPosition, Basis cameraRotation) {
        if (!IsMultiplayerAuthority()) {
            var now = Time.GetTicksMsec();
            var timeSinceLast = (now - _lastPacketTime) / 1000.0; // in seconds

            // Predict when the next packet should arrive (very naive assumption)
            var expectedNextPacket = now + (timeSinceLast * 1000.0);
            _predictedArrivalTime = expectedNextPacket;

            var estimatedFuturePos = position + (velocity * (float)timeSinceLast);
            _predictedFuturePos = estimatedFuturePos;

            _targetPosition = position;
            _targetVelocity = velocity;
            _targetRotation = rotation;
            _lastPacketTime = now;
            _timeSinceLastUpdate = 0f;

            var camera = GetNode<Camera3D>("Camera3D");
            camera.GlobalTransform = new Transform3D(cameraRotation, cameraPosition);
        }
    }

    private void HandleCooldown(float dt) {
        var keys = new List<string>(_state!.SkillCooldowns.Keys);
        foreach (var key in keys) {
            _state.SkillCooldowns[key] -= dt;
            if (_state.SkillCooldowns[key] <= 0f) {
                _state.SkillCooldowns.Remove(key);
            }
        }
    }

    private void HandleParry() {
        if (Input.IsActionJustPressed("Parry")) {
            if (_state!.IsParrying) {
                return;
            }

            if (_state.Stamina < 30) {
                return;
            }

            _state.RequestStaminaChange(-30);
            _state.IsParrying = true;
            UIPlayer!.Play("Parry");

            _ = ResetParryAfterDelay(0.2f);
        }
    }

    private void HandleDeath() {
        _state!.Health = 10000;
        _state.Resistance = 1000;
        _state.Stamina = 100;

        TeleportToRandomPosition();
    }

    private async void TeleportToRandomPosition() {
        Visible = false;
        await ToSignal(GetTree().CreateTimer(2f), "timeout");

        var random = new RandomNumberGenerator();
        random.Randomize();

        var randomX = random.RandfRange(0, -70f);
        var randomZ = random.RandfRange(0, 70f);

        var currentY = GlobalTransform.Origin.Y;

        Vector3 newPosition = new Vector3(randomX, currentY, randomZ);
        GlobalTransform = new Transform3D(GlobalTransform.Basis, newPosition);

        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");

        Visible = true;
    }


    private void HandleBreak(int requester) {
        var state = GetPlayerState(requester);
        state!.RpcId(requester, nameof(_state.RequestBreakCallback), Multiplayer.GetUniqueId());
    }

    private void HandleImmobilize(int requester) {
        var state = GetPlayerState(requester);
        _state!.RpcId(requester, nameof(_state.RequestImmobilized), 1f);
    }

    private async Task ResetParryAfterDelay(float delay) {
        await ToSignal(GetTree().CreateTimer(delay), "timeout");
        _state!.IsParrying = false;
    }

    private void HandleBloodstain() =>
        Bloodstain!.Modulate = new Color(1f, 1f, 1f, 0.7f);

    private void ResetBloodstain() =>
        Bloodstain!.Modulate = new Color(1f, 1f, 1f, 0);

    private void HandleBlind() {
        UIPlayer!.Play("RESET");
        UIPlayer!.Play("Blind");
    }

    private void HandleSpy() =>
        UIPlayer!.Play("EyeSpy");

    protected void RequestDealDamage(long id, float amount) {
        var state = GetNode<State>("State");
        state.PlayersScore[Multiplayer.GetUniqueId()] -= amount;
        state.ChangeSkillCooldown("Exclusive", amount);
        GetPlayerState(id)?.RpcId(id, nameof(State.RequestHealthChange), amount);
    }

    protected void RequestDealBreak(long id, float amount) =>
        GetPlayerState(id)?.RpcId(id, nameof(State.RequestResistanceChange), amount);

    protected void RequestImmobilized(long id, float time) =>
        GetPlayerState(id)?.RpcId(id, nameof(State.RequestImmobilized), time);

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void PlaySFX(string value) {
        var path = $"res://src/Assets/Audio/{value}";
        var stream = GD.Load<AudioStream>(path);

        if (stream == null) {
            GD.PrintErr($"[SFX] Failed to load: {path}");
            return;
        }

        if (SFX != null) {
            SFX.Stream = stream;
            SFX.Play();
        }
    }

    public void RequestPlaySFX(string value) {
        Rpc(nameof(PlaySFX), value);
        PlaySFX(value);
    }

    protected State? GetPlayerState(long peerId) {
        var world = GetTree().Root.GetNode("World");

        var enemyBody = world.GetNodeOrNull<CharacterBody3D>(peerId.ToString());
        if (enemyBody == null) {
            GD.PrintErr("Enemy body not found: 1");
            return null;
        }

        var enemyState = enemyBody.GetNodeOrNull<State>("State");
        if (enemyState == null) {
            GD.PrintErr("Enemy state not found: 2");
            return null;
        }

        return enemyState;
    }

    public CharacterBody3D? GetPlayerNode(long peerId) {
        var world = GetTree().Root.GetNode("World");

        var enemyBody = world.GetNodeOrNull<CharacterBody3D>(peerId.ToString());
        if (enemyBody == null) {
            GD.PrintErr("Enemy body not found: 3");
            return null;
        }

        return enemyBody;
    }

    public Camera3D? GetPlayerCamera(long peerId) {
        var world = GetTree().Root.GetNode("World");

        var enemyBody = world.GetNodeOrNull<CharacterBody3D>(peerId.ToString());
        if (enemyBody == null) {
            GD.PrintErr("Enemy body not found: 4");
            return null;
        }

        var enemyCamera = enemyBody.GetNodeOrNull<Camera3D>("Camera3D");
        if (enemyCamera == null) {
            GD.PrintErr("Enemy camera not found");
            return null;
        }

        return enemyCamera;
    }

    protected bool IsInvalidHitscan(Node body) =>
            !body.IsInGroup("Players") || long.Parse(body.Name) == Multiplayer.GetUniqueId();

    private bool AreDependenciesValid() =>
        Camera != null && AnimationTree != null && AnimationPlayer != null && Particles != null && Animator != null && State != null;

    private new bool IsMultiplayerAuthority() => base.IsMultiplayerAuthority();
}
