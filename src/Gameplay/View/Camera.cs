namespace Misled.Gameplay.View;

using Godot;

/// <summary>
/// A third-person camera that follows a target character.
/// </summary>
public partial class Camera : Camera3D {
    [Export] public Vector3 PivotOffset = new(0, 2, 0);
    [Export] public float Distance = 6.0f;
    [Export] public float MinDistance = 2.0f;
    [Export] public float MaxDistance = 10.0f;
    [Export] public float ZoomSpeed = 1.0f;
    [Export] public float VerticalRotationSpeed = 0.1f;
    [Export] public float HorizontalRotationSpeed = 0.1f;
    [Export] public float FollowSpeed = 20f;
    [Export] public float MinPitch = -70f;
    [Export] public float MaxPitch = 40f;

    private CharacterBody3D? _target;
    private float _yaw;
    private float _pitch = 10.0f;

    /// <summary>
    /// Called when the node enters the scene tree.
    /// </summary>
    public override void _Ready() {
        _target = GetParent<CharacterBody3D>();
        SetAsTopLevel(true);
        Current = IsMultiplayerAuthority();
    }

    /// <summary>
    /// Handles input events that are not handled by other controls.
    /// </summary>
    /// <param name="event">The input event.</param>
    public override void _UnhandledInput(InputEvent @event) {
        if (!IsMultiplayerAuthority()) {
            return;
        }

        switch (@event) {
            case InputEventMouseMotion motion:
                HandleMouseMotion(motion);
                break;
            case InputEventMouseButton { Pressed: true } button:
                HandleMouseButton(button);
                break;
            case InputEventKey { Pressed: true, Keycode: Key.Escape }:
                ToggleMouseMode();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Called during the processing step of the main loop.
    /// </summary>
    /// <param name="delta">The time elapsed since the previous frame.</param>
    public override void _Process(double delta) {
        if (_target == null) {
            return;
        }

        if (!Current && IsMultiplayerAuthority()) {
            Current = true;
        }

        if (!IsMultiplayerAuthority()) {
            return;
        }

        UpdateCameraPosition((float)delta);
    }

    /// <summary>
    /// Updates the camera position.
    /// </summary>
    /// <param name="delta">The time elapsed since the previous frame.</param>
    private void UpdateCameraPosition(float delta) {
        var pivot = _target!.GlobalTransform.Origin + PivotOffset;
        var cameraRotation = GetCameraRotation();
        var desiredPosition = pivot + (cameraRotation.Z * -Distance);

        var finalPosition = AdjustForObstacles(pivot, desiredPosition);

        var smoothedPosition = GlobalTransform.Origin.Lerp(finalPosition, delta * FollowSpeed);
        GlobalTransform = new Transform3D(cameraRotation, smoothedPosition);
        LookAt(pivot, Vector3.Up);
    }

    /// <summary>
    /// Handles mouse motion events for camera rotation.
    /// </summary>
    /// <param name="motion">The mouse motion event.</param>
    private void HandleMouseMotion(InputEventMouseMotion motion) {
        _yaw -= motion.Relative.X * HorizontalRotationSpeed;
        _pitch = Mathf.Clamp(_pitch + (motion.Relative.Y * VerticalRotationSpeed), MinPitch, MaxPitch);
    }

    /// <summary>
    /// Handles mouse button events for camera zoom.
    /// </summary>
    /// <param name="button">The mouse button event.</param>
    private void HandleMouseButton(InputEventMouseButton button) {
        if (button.ButtonIndex == MouseButton.WheelUp) {
            Distance = Mathf.Clamp(Distance - ZoomSpeed, MinDistance, MaxDistance);
        }
        else if (button.ButtonIndex == MouseButton.WheelDown) {
            Distance = Mathf.Clamp(Distance + ZoomSpeed, MinDistance, MaxDistance);
        }
    }

    /// <summary>
    /// Toggles the mouse capture mode.
    /// </summary>
    private static void ToggleMouseMode() =>
        Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
            ? Input.MouseModeEnum.Visible
            : Input.MouseModeEnum.Captured;

    /// <summary>
    /// Gets the camera rotation as a Basis.
    /// </summary>
    /// <returns>The camera rotation.</returns>
    private Basis GetCameraRotation() =>
        new Basis(Vector3.Up, Mathf.DegToRad(_yaw)) *
        new Basis(Vector3.Right, Mathf.DegToRad(_pitch));

    /// <summary>
    /// Adjusts the camera position to avoid obstacles.
    /// </summary>
    /// <param name="from">The starting position.</param>
    /// <param name="to">The desired position.</param>
    /// <returns>The adjusted position.</returns>
    private Vector3 AdjustForObstacles(Vector3 from, Vector3 to) {
        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollisionMask = 1;
        query.Exclude = [GetCameraRid(), _target!.GetRid()];
        var result = spaceState.IntersectRay(query);

        if (result.Count > 0) {
            var collisionPoint = (Vector3)result["position"];
            var direction = (collisionPoint - from).Normalized();
            return collisionPoint - (direction * 0.2f);
        }

        return to;
    }

    /// <summary>
    /// Determines whether this node has multiplayer authority.
    /// </summary>
    /// <returns><c>true</c> if this node has multiplayer authority; otherwise, <c>false</c>.</returns>
    private new bool IsMultiplayerAuthority() => _target?.IsMultiplayerAuthority() == true;
}
