namespace Misled.Gameplay.View;

using System.Collections.Generic;
using System.Linq;
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

    // 🌟 Touch tracking
    private Dictionary<int, Vector2> _activeTouches = new();
    private float _initialPinchDistance = 0f;
    private float _initialDistance = 0f;

    public override void _Ready() {
        _target = GetParent<CharacterBody3D>();
        SetAsTopLevel(true);
        Current = IsMultiplayerAuthority();
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (!IsMultiplayerAuthority()) {
            return;
        }

        switch (@event) {
            case InputEventMouseMotion motion:
                if (Input.MouseMode == Input.MouseModeEnum.Captured) {
                    HandleMouseMotion(motion);
                }
                break;

            case InputEventMouseButton { Pressed: true } button:
                HandleMouseButton(button);
                break;

            case InputEventKey { Pressed: true, Keycode: Key.Escape }:
                ToggleMouseMode();
                break;

            // 🌟 Touch input
            case InputEventScreenTouch touch:
                if (touch.Pressed) {
                    _activeTouches[touch.Index] = touch.Position;
                }
                else {
                    _activeTouches.Remove(touch.Index);
                    _initialPinchDistance = 0f; // reset pinch state
                }
                break;

            case InputEventScreenDrag drag:
                _activeTouches[drag.Index] = drag.Position;

                if (_activeTouches.Count == 1) {
                    Vector2 delta = drag.Relative;
                    _yaw -= delta.X * HorizontalRotationSpeed * 0.5f;
                    _pitch = Mathf.Clamp(_pitch + delta.Y * VerticalRotationSpeed * 0.5f, MinPitch, MaxPitch);
                }
                break;
        }
    }

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

        HandleTouchPinchZoom();
        UpdateCameraPosition((float)delta);
    }

    private void HandleTouchPinchZoom() {
        if (_activeTouches.Count == 2) {
            var touches = _activeTouches.Values.ToArray();
            float currentDistance = touches[0].DistanceTo(touches[1]);

            if (_initialPinchDistance == 0f) {
                _initialPinchDistance = currentDistance;
                _initialDistance = Distance;
            }
            else {
                float deltaDistance = currentDistance - _initialPinchDistance;
                Distance = Mathf.Clamp(_initialDistance - deltaDistance * 0.01f, MinDistance, MaxDistance);
            }
        }
        else {
            _initialPinchDistance = 0f;
        }
    }

    private void UpdateCameraPosition(float delta) {
        var pivot = _target!.GlobalTransform.Origin + PivotOffset;
        var cameraRotation = GetCameraRotation();
        var desiredPosition = pivot + (cameraRotation.Z * -Distance);

        var finalPosition = AdjustForObstacles(pivot, desiredPosition);
        var smoothedPosition = GlobalTransform.Origin.Lerp(finalPosition, delta * FollowSpeed);
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
