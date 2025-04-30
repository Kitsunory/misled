namespace Misled.Characters.View;
using Godot;

public partial class FollowCamera : Camera3D {
    [Export] public NodePath? TargetPath;
    [Export] public Vector3 PivotOffset = new(0, 2, 0);
    [Export] public float Distance = 6.0f;
    [Export] public float MinDistance = 2.0f;
    [Export] public float MaxDistance = 10.0f;
    [Export] public float ZoomSpeed = 1.0f;
    [Export] public float VerticalRotationSpeed = 0.01f;
    [Export] public float HorizontalRotationSpeed = 0.01f;
    [Export] public float FollowSpeed = 8.0f;
    [Export] public float MinPitch = -30f;
    [Export] public float MaxPitch = 60f;

    private CharacterBody3D? _target;

    private float _yaw;
    private float _pitch = 10.0f;

    public override void _Ready() {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        if (TargetPath != null) { _target = GetNode<CharacterBody3D>(TargetPath); }
    }

    public override void _UnhandledInput(InputEvent @event) {
        switch (@event) {
            case InputEventMouseMotion motion:
                _yaw -= motion.Relative.X * HorizontalRotationSpeed;
                _pitch = Mathf.Clamp(_pitch - (motion.Relative.Y * VerticalRotationSpeed), MinPitch, MaxPitch);
                break;
            case InputEventMouseButton { Pressed: true } button: {
                    Distance = button.ButtonIndex switch {
                        MouseButton.WheelUp => Mathf.Clamp(Distance - ZoomSpeed, MinDistance, MaxDistance),
                        MouseButton.WheelDown => Mathf.Clamp(Distance + ZoomSpeed, MinDistance, MaxDistance),
                        MouseButton.None => Distance,
                        MouseButton.Left => Distance,
                        MouseButton.Right => Distance,
                        MouseButton.Middle => Distance,
                        MouseButton.WheelLeft => Distance,
                        MouseButton.WheelRight => Distance,
                        MouseButton.Xbutton1 => Distance,
                        MouseButton.Xbutton2 => Distance,
                        _ => Distance
                    };
                    break;
                }
            case InputEventKey { Pressed: true, Keycode: Key.Escape }: {
                    Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
                    break;
                }

            default:
                break;
        }
    }

    public override void _Process(double delta) {
        if (_target == null) {
            return;
        }

        var dt = (float)delta;

        var rotation = new Basis(Vector3.Up, Mathf.DegToRad(_yaw)) * new Basis(Vector3.Right, Mathf.DegToRad(_pitch));

        var pivotPosition = _target.GlobalTransform.Origin + PivotOffset;
        var desiredCameraPosition = pivotPosition + (rotation.Z * -Distance);

        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(pivotPosition, desiredCameraPosition);
        query.CollisionMask = 1;
        query.Exclude = [GetCameraRid(), _target.GetRid()];

        var result = spaceState.IntersectRay(query);

        var finalPosition = desiredCameraPosition;
        if (result.Count > 0) {
            var collisionPoint = (Vector3)result["position"];
            var toCollision = (collisionPoint - pivotPosition).Normalized();
            finalPosition = collisionPoint - (toCollision * 0.2f); // Push back a little
        }

        var current = GlobalTransform.Origin;
        var newPosition = current.Lerp(finalPosition, dt * FollowSpeed);

        GlobalTransform = new Transform3D(rotation, newPosition);
        LookAt(pivotPosition, Vector3.Up);
    }
}
