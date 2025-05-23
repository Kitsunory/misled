namespace Misled;

using Godot;

#if RUN_TESTS
using System.Reflection;
using Chickensoft.GoDotTest;
#endif

// This entry-point file is responsible for determining if we should run tests.
//
// If you want to edit your game's main entry-point, please see Game.tscn and
// Game.cs instead.

public partial class Main : Node2D {
#if RUN_TESTS
    public TestEnvironment Environment = default!;
#endif

    public override void _Ready() {
#if RUN_TESTS
        // If this is a debug build, use GoDotTest to examine the
        // command line arguments and determine if we should run tests.
        Environment = TestEnvironment.From(OS.GetCmdlineArgs());
        if (Environment.ShouldRunTests) {
            CallDeferred("RunTests");
            return;
        }
#endif

        // If we don't need to run tests, we can just switch to the game scene.
        CallDeferred("RunScene");
    }

#if RUN_TESTS
    private void RunTests()
      => _ = GoTest.RunTests(Assembly.GetExecutingAssembly(), this, Environment);
#endif

    private void RunScene()
      => GetTree().ChangeSceneToFile("res://src/Level/Serbian/Serbian.tscn");
}
