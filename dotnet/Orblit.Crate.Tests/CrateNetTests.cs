using Orblit.Game;
using Orblit.Input;
using Orblit.Net;
using Orblit.Net.Game;
using Orblit.Scene;
using Orblit.VectorMath;

namespace Crate.Tests;

/// <summary>
/// Two copies of the crate game, the server the host plays on and a guest's client, joined by a loopback
/// link the way the editor joins them when Play starts two players.
/// </summary>
public sealed class CrateNetTests
{
    private const string GuestId = "player2";

    private static readonly string Folder = AppContext.BaseDirectory;
    private static readonly GameInput Idle = new(new FrameControls());

    private static SceneDocument Authored() =>
        SceneDocument.Decode(File.ReadAllText(Path.Combine(Folder, "scenes", "main.oscene"))).Document;

    private static GameInput Holding(string key) => new(new FrameControls([key]));

    private static GameInput Clicking(double x, double z) =>
        new(new FrameControls(), new GamePointer(new Vector3(x, 5, z), new Vector3(0, -1, 0), Pressed: true));

    private static TransformComponent TransformOf(GameHost host, string id) =>
        (TransformComponent)host.Scene[id]![SceneComponents.Transform]!;

    private static (double X, double Z) Marker(GameHost host)
    {
        var at = TransformOf(host, "marker").Position;
        return (at.X, at.Z);
    }

    private static NetComponent NetOf(SceneEntity? entity) => Assert.IsType<NetComponent>(entity?[SceneComponents.Net]);

    private static object? ColourOf(GameHost host, string id) => host.Scene[id]![SceneComponents.Mesh]!.ToJson()["colour"];

    [Fact]
    public void TheSceneSharesTheCratesAndTheMarkerAndNothingElse()
    {
        var scene = Authored();

        Assert.Equal(NetAuthority.Owner, NetOf(scene["crate"]).Authority);
        Assert.Equal(["transform"], NetOf(scene["crate"]).Sync);
        Assert.Equal(NetAuthority.Server, NetOf(scene["marker"]).Authority);
        Assert.Equal(["transform"], NetOf(scene["marker"]).Sync);
        Assert.All(new[] { "sun", "ground", "camera" }, id => Assert.Null(scene[id]![SceneComponents.Net]));
    }

    [Fact]
    public async Task TheServerAddsASecondCrateOwnedLikeTheFirstAndTheGuestSeesIt()
    {
        await using var room = await Room.Open();

        await room.Frame();

        foreach (var host in new[] { room.Host, room.Guest })
        {
            Assert.Equal(NetAuthority.Owner, NetOf(host.Scene["crate2"]).Authority);
        }
    }

    [Fact]
    public async Task TheSecondCrateComesAfterTheFirstSoTheFirstGuestIsGivenIt()
    {
        await using var room = await Room.Open();

        var ids = room.Host.Scene.Entities.Select(entity => entity.Id).ToList();

        Assert.True(ids.IndexOf("crate2") > ids.IndexOf("crate"));
    }

    [Fact]
    public async Task TheSecondCrateStartsApartFromTheFirstAndIsAnotherColour()
    {
        await using var room = await Room.Open();

        var first = TransformOf(room.Host, "crate").Position;
        var second = TransformOf(room.Host, "crate2").Position;
        Assert.NotEqual(first.X, second.X);
        Assert.Equal(first.Y, second.Y);
        Assert.Equal(first.Z, second.Z);
        Assert.NotEqual(ColourOf(room.Host, "crate"), ColourOf(room.Host, "crate2"));
    }

    [Fact]
    public async Task TheGuestMovesTheCrateItOwnsAndTheServerSeesWhereItWent()
    {
        await using var room = await Room.Open();
        await room.Give();
        var start = TransformOf(room.Host, "crate2").Position.X;
        var crate = TransformOf(room.Host, "crate").Position.X;

        await room.Frame(guest: Holding("ArrowRight"));
        await room.Frame();

        Assert.Equal(start + 1, TransformOf(room.Host, "crate2").Position.X, 9);
        Assert.Equal(start + 1, TransformOf(room.Guest, "crate2").Position.X, 9);
        Assert.Equal(crate, TransformOf(room.Host, "crate").Position.X, 9);
        Assert.Equal(crate, TransformOf(room.Guest, "crate").Position.X, 9);
    }

    [Fact]
    public async Task TheHostMovesItsOwnCrateAndTheGuestSeesWhereItWent()
    {
        await using var room = await Room.Open();
        await room.Give();
        var start = TransformOf(room.Host, "crate").Position.Z;
        var second = TransformOf(room.Host, "crate2").Position.Z;

        await room.Frame(host: Holding("ArrowDown"));
        await room.Frame();

        Assert.Equal(start + 1, TransformOf(room.Host, "crate").Position.Z, 9);
        Assert.Equal(start + 1, TransformOf(room.Guest, "crate").Position.Z, 9);
        Assert.Equal(second, TransformOf(room.Host, "crate2").Position.Z, 9);
    }

    [Fact]
    public async Task AGuestThatHasNoCrateYetMovesNone()
    {
        await using var room = await Room.Open();
        var crate = TransformOf(room.Host, "crate").Position.X;
        var second = TransformOf(room.Host, "crate2").Position.X;

        await room.Frame(guest: Holding("ArrowRight"));
        await room.Frame();

        Assert.Equal(crate, TransformOf(room.Host, "crate").Position.X, 9);
        Assert.Equal(second, TransformOf(room.Host, "crate2").Position.X, 9);
        Assert.Equal(second, TransformOf(room.Guest, "crate2").Position.X, 9);
    }

    [Fact]
    public async Task EachCameraFollowsTheCrateItsOwnCopyPlays()
    {
        await using var room = await Room.Open();
        await room.Give();

        await room.Frame(host: Holding("ArrowLeft"), guest: Holding("ArrowRight"));

        Assert.Equal(TransformOf(room.Host, "crate").Position.X, TransformOf(room.Host, "camera").Position.X, 9);
        Assert.Equal(TransformOf(room.Guest, "crate2").Position.X, TransformOf(room.Guest, "camera").Position.X, 9);
    }

    [Fact]
    public async Task AGuestsClickMovesTheMarkerToTheTileUnderItOnBothCopies()
    {
        await using var room = await Room.Open();
        await room.Give();

        await room.Frame(guest: Clicking(2.3, -1.2));
        await room.Frame();

        Assert.Equal((2.5, -1.5), Marker(room.Host));
        Assert.Equal((2.5, -1.5), Marker(room.Guest));
    }

    [Fact]
    public async Task TheHostsClickMovesTheMarkerOnBothCopies()
    {
        await using var room = await Room.Open();

        await room.Frame(host: Clicking(-3.7, 4.1));

        Assert.Equal((-3.5, 4.5), Marker(room.Host));
        Assert.Equal((-3.5, 4.5), Marker(room.Guest));
    }

    [Fact]
    public async Task AGuestCannotPutTheMarkerOffTheGround()
    {
        await using var room = await Room.Open();
        var before = Marker(room.Host);

        room.Client.Call("PlaceMarker", new Vector3(50, 0, 0));
        await room.Settle();
        await room.Frame();

        Assert.Equal(before, Marker(room.Host));
        Assert.Equal(before, Marker(room.Guest));
    }

    // A place 6 or more from the middle is refused on either side, the same as a click there.
    [Theory]
    [InlineData(6, 0)]
    [InlineData(0, 6)]
    [InlineData(-6, 0)]
    [InlineData(0, -6)]
    public async Task AGuestCannotPutTheMarkerOnTheEdgeOfTheGround(double x, double z)
    {
        await using var room = await Room.Open();
        var before = Marker(room.Host);

        room.Client.Call("PlaceMarker", new Vector3(x, 0, z));
        await room.Settle();
        await room.Frame();

        Assert.Equal(before, Marker(room.Host));
    }

    [Fact]
    public async Task TheLastTileInsideTheEdgeTakesTheMarker()
    {
        await using var room = await Room.Open();

        room.Client.Call("PlaceMarker", new Vector3(5.99, 0, -5.99));
        await room.Settle();
        await room.Frame();

        Assert.Equal((5.5, -5.5), Marker(room.Host));
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0)]
    public async Task AGuestCannotPutTheMarkerAtAPlaceThatIsNotANumber(double x, double z)
    {
        await using var room = await Room.Open();
        var before = Marker(room.Host);

        room.Client.Call("PlaceMarker", new Vector3(x, 0, z));
        await room.Settle();
        await room.Frame();

        Assert.Equal(before, Marker(room.Host));
    }

    [Fact]
    public async Task TheServerSnapsAnyPlaceAGuestAsksForToTheCentreOfATile()
    {
        await using var room = await Room.Open();

        room.Client.Call("PlaceMarker", new Vector3(1.2, 0, 1.9));
        await room.Settle();
        await room.Frame();

        Assert.Equal((1.5, 1.5), Marker(room.Host));
    }

    private sealed class Room : IAsyncDisposable
    {
        private readonly LoopbackLink _link = new();
        private readonly NetworkedServer _server = new();

        private Room()
        {
            Host = new GameHost(new CrateGame(), Authored(), Folder, new ThrowingLog());
            Host.UseNet(_server);
            Host.Start();
            Client = new NetworkedClient(new NetworkedClientOptions { ClientId = GuestId, Transport = _link.Client });
            Guest = new GameHost(new CrateGame(), Authored(), Folder, new ThrowingLog());
            Guest.UseNet(Client);
            Guest.Start();
        }

        public GameHost Host { get; }

        public GameHost Guest { get; }

        public NetworkedClient Client { get; }

        /// <summary>The order the editor uses: the host starts, then the guest, then the guest is connected.</summary>
        public static async Task<Room> Open()
        {
            var room = new Room();
            room._server.AddClient(GuestId, room._link.Host);
            await room.Settle();
            return room;
        }

        /// <summary>Gives the guest the second crate, as the editor does once every guest has started.</summary>
        public async Task Give()
        {
            _server.Assign("crate2", GuestId);
            await Settle();
        }

        /// <summary>The server's frame, a pause for the wire, the guest's frame, and another pause.</summary>
        public async Task Frame(GameInput? host = null, GameInput? guest = null)
        {
            Host.Update(host ?? Idle, 0.25);
            await Settle();
            Guest.Update(guest ?? Idle, 0.25);
            await Settle();
            Assert.Null(Host.Fault);
            Assert.Null(Guest.Fault);
        }

        public Task Settle() => _link.IdleAsync();

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            _server.Dispose();
            await _link.CloseAsync();
        }
    }

    // A game that faults should fail the test that ran it, not stop moving and leave a position unchanged.
    private sealed class ThrowingLog : IGameLog
    {
        public void Say(string message)
        {
        }

        public void Error(string message, Exception error) => throw new InvalidOperationException(message, error);
    }
}
