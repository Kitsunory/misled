namespace Misled.Gameplay.Core;

using Godot;

public partial class LobbyManager : Control {
    [Export]
    public TextEdit? PlayerName;
    [Export]
    public TextEdit? Code; // This is actually just port number
    [Export]
    public OptionButton? Hyprs; // "Tanger" and "Osage"
    [Export]
    public Button? StateButton; // Initiate with Join. If host, can Start the match, if client, can leave the room.
    [Export]
    public Button? Subpoint1;
    [Export]
    public Button? Subpoint2;
    [Export]
    public Button? Subpoint3;
    [Export]
    public Button? Subpoint4;
}
