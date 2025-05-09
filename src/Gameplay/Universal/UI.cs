namespace Misled.Gameplay.Universal;

using Godot;

public partial class UI : Control {
    public override void _Ready() {
        if (!IsMultiplayerAuthority()) {
            Visible = false;
        }
    }
}
