namespace Misled.Characters.View;
using Godot;

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

    public override void _Ready() {
        _target = GetParent() as CharacterBody3D;
        SetAsTopLevel(true);
        if (_target?.IsMultiplayerAuthority() == true) {
            Current = true;
        }
        else {
            Current = false;
        }
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (!IsMultiplayerAuthority()) { return; }

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

    public override void _Process(double delta) {
        if (!Current && IsMultiplayerAuthority()) {
            Current = true;
        }

        if (_target == null || !IsMultiplayerAuthority()) { return; }

        var dt = (float)delta;

        var pivot = _target.GlobalTransform.Origin + PivotOffset;
        var cameraRotation = GetCameraRotation();
        var desiredPosition = pivot + (cameraRotation.Z * -Distance);

        var finalPosition = AdjustForObstacles(pivot, desiredPosition);

        var smoothedPosition = GlobalTransform.Origin.Lerp(finalPosition, dt * FollowSpeed);
        GlobalTransform = new Transform3D(cameraRotation, smoothedPosition);
        LookAt(pivot, Vector3.Up);
    }

    private void HandleMouseMotion(InputEventMouseMotion motion) {
        _yaw -= motion.Relative.X * HorizontalRotationSpeed;
        _pitch = Mathf.Clamp(_pitch + (motion.Relative.Y * VerticalRotationSpeed), MinPitch, MaxPitch);
    }

    private void HandleMouseButton(InputEventMouseButton button) {
        if (button.ButtonIndex == MouseButton.WheelUp) {
            Distance = Mathf.Clamp(Distance - ZoomSpeed, MinDistance, MaxDistance);
        }
        else if (button.ButtonIndex == MouseButton.WheelDown) {
            Distance = Mathf.Clamp(Distance + ZoomSpeed, MinDistance, MaxDistance);
        }
    }

    private static void ToggleMouseMode() =>
        Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
            ? Input.MouseModeEnum.Visible
            : Input.MouseModeEnum.Captured;


    private Basis GetCameraRotation() =>
        new Basis(Vector3.Up, Mathf.DegToRad(_yaw)) *
        new Basis(Vector3.Right, Mathf.DegToRad(_pitch));


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

    private new bool IsMultiplayerAuthority() => _target?.IsMultiplayerAuthority() == true;
}
