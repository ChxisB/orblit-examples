using System.Text.Json;
using Orblit.Game;
using Orblit.Input;
using Orblit.Scene;
using Orblit.VectorMath;

namespace Crate.Tests;

/// <summary>
/// The crate game runs under a host with no window, so each test says what the game does with one
/// frame of input.
/// </summary>
public sealed class CrateGameTests
{
    private static readonly string Folder = AppContext.BaseDirectory;

    private static SceneDocument Authored() =>
        SceneDocument.Decode(File.ReadAllText(Path.Combine(Folder, "scenes", "main.oscene"))).Document;

    private static GameHost Started(SceneDocument? scene = null)
    {
        var host = new GameHost(new CrateGame(), scene ?? Authored(), Folder, new SilentLog());
        host.Start();
        return host;
    }

    private static void Frame(GameHost host, double seconds, params string[] keys) =>
        host.Update(new GameInput(new FrameControls(keys)), seconds);

    private static void Frame(GameHost host, double seconds, GamePointer pointer) =>
        host.Update(new GameInput(new FrameControls(), pointer), seconds);

    private static TransformComponent Transform(GameHost host, string id) => TransformOf(host.Scene[id]!);

    private static TransformComponent TransformOf(SceneEntity entity) =>
        (TransformComponent)entity[SceneComponents.Transform]!;

    private static GamePointer Click(Vector3 origin, Vector3 direction) => new(origin, direction, Pressed: true);

    [Theory]
    [InlineData("crate")]
    [InlineData("marker")]
    [InlineData("camera")]
    public void TheSceneHoldsWhatTheGamePlaysWith(string id)
    {
        var host = Started();

        Assert.Null(host.Fault);
        Assert.NotNull(host.Scene[id]);
    }

    [Fact]
    public void StartingChangesNothingInTheScene()
    {
        var scene = Authored();
        var host = Started(scene);

        Assert.Equal(scene.Encode(), host.Scene.Encode());
    }

    // A mesh with no asset is the renderer's cube, which is 2 across, so a scale is half a size. The
    // game keeps the crate on a ground 12 metres square, and the scene has to draw that ground.
    [Fact]
    public void TheDrawnGroundIsTheTwelveMetresTheGameKeepsTheCrateOn()
    {
        var ground = TransformOf(Authored()["ground"]!).Scale;

        Assert.Equal(12, ground.X * 2);
        Assert.Equal(12, ground.Z * 2);
    }

    [Fact]
    public void TheCrateStandsOnTheTopOfTheGround()
    {
        var scene = Authored();
        var ground = TransformOf(scene["ground"]!);
        var crate = TransformOf(scene["crate"]!);

        Assert.Equal(ground.Position.Y + ground.Scale.Y, crate.Position.Y - crate.Scale.Y, 9);
    }

    [Fact]
    public void RightMovesTheCrateAlongXBySpeedTimesSeconds()
    {
        var host = Started();
        var before = Transform(host, "crate").Position;

        Frame(host, 0.25, "ArrowRight");

        var after = Transform(host, "crate").Position;
        Assert.Equal(before.X + 1, after.X, 9);
        Assert.Equal(before.Z, after.Z, 9);
    }

    [Fact]
    public void UpMovesTheCrateAwayFromTheCamera()
    {
        var host = Started();
        var before = Transform(host, "crate").Position;

        Frame(host, 0.25, "ArrowUp");

        Assert.Equal(before.Z - 1, Transform(host, "crate").Position.Z, 9);
    }

    [Fact]
    public void KeysThatPullOppositeWaysCancel()
    {
        var host = Started();
        var before = Transform(host, "crate").Position;

        Frame(host, 0.25, "ArrowLeft", "ArrowRight");

        Assert.Equal(before.X, Transform(host, "crate").Position.X, 9);
    }

    [Fact]
    public void ADiagonalIsNoFasterThanAStraightLine()
    {
        var host = Started();
        var before = Transform(host, "crate").Position;

        Frame(host, 0.5, "ArrowRight", "ArrowDown");

        var after = Transform(host, "crate").Position;
        var moved = Math.Sqrt(Math.Pow(after.X - before.X, 2) + Math.Pow(after.Z - before.Z, 2));
        Assert.Equal(2, moved, 9);
    }

    [Theory]
    [InlineData("ArrowRight", 5.5, 0)]
    [InlineData("ArrowLeft", -5.5, 0)]
    [InlineData("ArrowDown", 0, 5.5)]
    [InlineData("ArrowUp", 0, -5.5)]
    public void TheCrateStopsAtTheEdgeOfTheGround(string key, double x, double z)
    {
        var host = Started();

        for (var frame = 0; frame < 10; frame++)
        {
            Frame(host, 1, key);
        }

        var at = Transform(host, "crate").Position;
        Assert.Equal(x, at.X, 9);
        Assert.Equal(z, at.Z, 9);
    }

    [Fact]
    public void TheCrateKeepsItsHeightAndScale()
    {
        var host = Started();
        var before = Transform(host, "crate");

        Frame(host, 0.25, "ArrowRight");

        var after = Transform(host, "crate");
        Assert.Equal(before.Position.Y, after.Position.Y);
        Assert.Equal(before.Scale.X, after.Scale.X);
        Assert.Equal(before.Scale.Y, after.Scale.Y);
        Assert.Equal(before.Scale.Z, after.Scale.Z);
    }

    [Fact]
    public void TheCameraKeepsItsDistanceFromTheCrate()
    {
        var host = Started();
        var offset = Offset(host);

        Frame(host, 0.5, "ArrowRight");
        Frame(host, 0.5, "ArrowDown");

        var now = Offset(host);
        Assert.Equal(offset.X, now.X, 9);
        Assert.Equal(offset.Y, now.Y, 9);
        Assert.Equal(offset.Z, now.Z, 9);
    }

    [Fact]
    public void TheCameraKeepsItsDistanceWhereverTheCrateStarts()
    {
        var authored = Authored();
        var moved = authored["crate"]!.WithComponent(SceneComponents.Transform, new TransformComponent(new Vector3(2, 0.5, -3)));
        var host = Started(authored.WithEntity("crate", moved));
        var offset = Offset(host);

        Frame(host, 0.5, "ArrowLeft");

        var now = Offset(host);
        Assert.Equal(offset.X, now.X, 9);
        Assert.Equal(offset.Y, now.Y, 9);
        Assert.Equal(offset.Z, now.Z, 9);
    }

    private static Vector3 Offset(GameHost host)
    {
        var crate = Transform(host, "crate").Position;
        var camera = Transform(host, "camera").Position;
        return new Vector3(camera.X - crate.X, camera.Y - crate.Y, camera.Z - crate.Z);
    }

    [Fact]
    public void TheCameraStaysPointedWhereItWasAuthored()
    {
        var host = Started();
        var before = Transform(host, "camera").Rotation;

        Frame(host, 0.5, "ArrowRight");

        var after = Transform(host, "camera").Rotation;
        Assert.Equal(before.X, after.X);
        Assert.Equal(before.Y, after.Y);
        Assert.Equal(before.Z, after.Z);
    }

    [Fact]
    public void TenIdleFramesLeaveTheCrateWhereItWasAndTurnItAQuarter()
    {
        var host = Started();
        var before = Transform(host, "crate");

        for (var frame = 0; frame < 10; frame++)
        {
            Frame(host, 0.1);
        }

        var after = Transform(host, "crate");
        Assert.Equal(before.Position.X, after.Position.X);
        Assert.Equal(before.Position.Z, after.Position.Z);
        Assert.Equal(before.Rotation.Y + 90, after.Rotation.Y, 9);
    }

    [Fact]
    public void TheTurnIsKeptUnderAFullCircle()
    {
        var host = Started();

        Frame(host, 5);

        Assert.InRange(Transform(host, "crate").Rotation.Y, 0, 360);
        Assert.Equal(90, Transform(host, "crate").Rotation.Y, 9);
    }

    [Fact]
    public void AClickPutsTheMarkerOnTheCentreOfTheTileUnderThePointer()
    {
        var host = Started();
        var height = Transform(host, "marker").Position.Y;

        Frame(host, 0.016, Click(new Vector3(2.3, 5, 3.7), new Vector3(0, -1, 0)));

        var at = Transform(host, "marker").Position;
        Assert.Equal(2.5, at.X, 9);
        Assert.Equal(3.5, at.Z, 9);
        Assert.Equal(height, at.Y);
    }

    [Fact]
    public void ASlantedRayLandsWhereItMeetsTheGroundAndNegativeTilesRoundDown()
    {
        var host = Started();

        // Four metres along the ray, so it reaches the ground at x 2.3 and z -0.8.
        Frame(host, 0.016, Click(new Vector3(0.3, 4, 0.2), new Vector3(0.5, -1, -0.25)));

        var at = Transform(host, "marker").Position;
        Assert.Equal(2.5, at.X, 9);
        Assert.Equal(-0.5, at.Z, 9);
    }

    [Fact]
    public void APointerThatIsNotPressedLeavesTheMarkerAlone()
    {
        var host = Started();
        var before = Transform(host, "marker").Position;

        Frame(host, 0.016, new GamePointer(new Vector3(2.3, 5, 3.7), new Vector3(0, -1, 0)));

        Assert.Equal(before.X, Transform(host, "marker").Position.X);
        Assert.Equal(before.Z, Transform(host, "marker").Position.Z);
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 0)]
    public void ARayThatNeverReachesTheGroundLeavesTheMarkerAlone(double x, double y, double z)
    {
        var host = Started();
        var before = Transform(host, "marker").Position;

        Frame(host, 0.016, Click(new Vector3(2, 5, 3), new Vector3(x, y, z)));

        Assert.Equal(before.X, Transform(host, "marker").Position.X);
        Assert.Equal(before.Z, Transform(host, "marker").Position.Z);
    }

    [Fact]
    public void ARayAlongTheGroundLeavesTheMarkerAlone()
    {
        var host = Started();
        var before = Transform(host, "marker").Position;

        Frame(host, 0.016, Click(new Vector3(2, 0, 3), new Vector3(1, 0, 0)));

        Assert.Equal(before.X, Transform(host, "marker").Position.X);
        Assert.Equal(before.Z, Transform(host, "marker").Position.Z);
    }

    [Fact]
    public void ARayThatMeetsTheGroundBehindThePointerLeavesTheMarkerAlone()
    {
        var host = Started();
        var before = Transform(host, "marker").Position;

        Frame(host, 0.016, Click(new Vector3(1, -2, 1), new Vector3(0, -1, 0)));

        Assert.Equal(before.X, Transform(host, "marker").Position.X);
        Assert.Equal(before.Z, Transform(host, "marker").Position.Z);
    }

    [Fact]
    public void AClickKeepsTheScaleOfTheMarker()
    {
        var host = Started();
        var before = Transform(host, "marker").Scale;

        Frame(host, 0.016, Click(new Vector3(2.3, 5, 3.7), new Vector3(0, -1, 0)));

        var after = Transform(host, "marker").Scale;
        Assert.Equal(before.X, after.X);
        Assert.Equal(before.Y, after.Y);
        Assert.Equal(before.Z, after.Z);
    }

    [Theory]
    [InlineData(7, 0)]
    [InlineData(6, 0)]
    [InlineData(-6, 0)]
    [InlineData(0, 6)]
    [InlineData(0, -6.5)]
    public void AClickBeyondTheGroundLeavesTheMarkerAlone(double x, double z)
    {
        var host = Started();
        var before = Transform(host, "marker").Position;

        Frame(host, 0.016, Click(new Vector3(x, 5, z), new Vector3(0, -1, 0)));

        Assert.Equal(before.X, Transform(host, "marker").Position.X);
        Assert.Equal(before.Z, Transform(host, "marker").Position.Z);
    }

    [Fact]
    public void EachFrameIsOneChangeToTheScene()
    {
        var host = Started();
        var changes = 0;
        host.SceneChanged += _ => changes++;

        Frame(host, 0.1, "ArrowRight");
        Frame(host, 0.1);
        Frame(host, 0.1, Click(new Vector3(1, 5, 1), new Vector3(0, -1, 0)));

        Assert.Equal(3, changes);
    }

    [Theory]
    [InlineData("crate")]
    [InlineData("marker")]
    [InlineData("camera")]
    public void ASceneWithoutAnEntityStopsTheGameAndNamesIt(string id)
    {
        var host = Started(Authored().WithEntity(id, null));

        var fault = Assert.IsType<InvalidOperationException>(host.Fault);
        Assert.Contains($"\"{id}\"", fault.Message);
        Assert.False(host.Running);
    }

    [Fact]
    public void AnEditMadeWhilePlayingIsReadOnTheNextFrame()
    {
        var host = Started();
        var crate = host.Scene["crate"]!;
        var moved = crate.WithComponent(SceneComponents.Transform, new TransformComponent(new Vector3(-3, 0.5, 2)));
        host.ApplyEdit(SceneDiff.Between(host.Scene, host.Scene.WithEntity("crate", moved)));

        Frame(host, 0.25, "ArrowRight");

        Assert.Equal(-2, Transform(host, "crate").Position.X, 9);
        Assert.Equal(2, Transform(host, "crate").Position.Z, 9);
    }

    [Fact]
    public void TheProjectFileNamesTheGameAndItsProject()
    {
        using var file = JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder, "orblit.project.json")));
        var game = file.RootElement.GetProperty("game");

        var project = game.GetProperty("project").GetString()!;
        Assert.Equal(typeof(CrateGame).FullName, game.GetProperty("type").GetString());
        Assert.True(File.Exists(Path.Combine(Folder, project)), $"The project file names {project}, which is not there.");
        // The editor loads the dll by the project's own name.
        Assert.Equal(Path.GetFileNameWithoutExtension(project), typeof(CrateGame).Assembly.GetName().Name);
    }

    private sealed class SilentLog : IGameLog
    {
        public void Say(string message)
        {
        }

        public void Error(string message, Exception error)
        {
        }
    }
}
