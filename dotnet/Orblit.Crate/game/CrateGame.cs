using Orblit.Game;
using Orblit.Input;
using Orblit.Net.Game;
using Orblit.Scene;
using Orblit.VectorMath;

namespace Crate;

/// <summary>
/// A crate on a ground plane. The arrow keys move it, it turns on its own, and a click puts a marker on
/// the tile under the pointer. The camera keeps the distance it was placed at. With two players, the
/// server adds a second crate and the guest plays that one.
/// </summary>
/// <remarks>
/// Everything the game knows is in the scene, so an edit made in the editor while it plays is read on the
/// next frame. It keeps the camera's offset from the crate, found once at the start, and the tile a click
/// asked for until the frame that places it.
/// <para>
/// Each copy of the game plays the crate it owns and the camera of its own view. The marker is the
/// server's. A click asks for it by name, and the server decides where it goes.
/// </para>
/// </remarks>
public sealed class CrateGame : IGame
{
    private const string CrateId = "crate";
    private const string SecondCrateId = "crate2";
    private const string MarkerId = "marker";
    private const string CameraId = "camera";

    private const double Speed = 4;
    private const double DegreesPerSecond = 90;

    // The second crate stands beside the first, on the same row so the one camera offset fits both.
    private const double SecondCrateX = 2;
    private const string SecondCrateColour = "#4F8FD9";

    // The ground is 12 metres square and a tile is a metre, so the last tile centre is half a metre in.
    private const double GroundHalf = 6;
    private const double LastTile = GroundHalf - 0.5;

    // In the order the server gives them out, so the first is the host's.
    private static readonly string[] Crates = [CrateId, SecondCrateId];

    private (double X, double Y, double Z) _cameraOffset;
    private (double X, double Z)? _requested;

    public void Start(GameContext context)
    {
        context.Net.Bind(this);
        var scene = context.Scene.Document;
        var crate = Find(scene, CrateId);
        _ = Find(scene, MarkerId);
        var camera = TransformOf(Find(scene, CameraId)).Position;
        var at = TransformOf(crate).Position;
        _cameraOffset = (camera.X - at.X, camera.Y - at.Y, camera.Z - at.Z);

        if (context.Net.IsServer)
        {
            context.Scene.Apply(SceneDiff.Between(scene, scene.WithEntity(SecondCrateId, Second(crate))));
        }
    }

    public void Update(GameContext context, double seconds)
    {
        AskForMarker(context);
        var before = context.Scene.Document;
        var tile = _requested;
        _requested = null;
        var after = Marked(Played(before, context, seconds), tile);
        context.Scene.Apply(SceneDiff.Between(before, after));
    }

    /// <summary>
    /// Puts the marker on the tile that holds <paramref name="at"/>. It runs on the server. The place comes
    /// from a client, so one off the ground is ignored and any other is snapped to the middle of its tile.
    /// </summary>
    [ServerRpc]
    public void PlaceMarker(Vector3 at)
    {
        if (TileAt(at.X, at.Z) is { } tile)
        {
            _requested = tile;
        }
    }

    public void Stop(GameContext context)
    {
    }

    private static SceneEntity Find(SceneDocument scene, string id) =>
        scene[id] ?? throw new InvalidOperationException($"The scene has no entity named \"{id}\".");

    private static TransformComponent TransformOf(SceneEntity entity) =>
        entity[SceneComponents.Transform] as TransformComponent ?? new TransformComponent();

    // A copy of the entity at a new place or turn. Whatever is not given, and the scale always, stays as it was.
    private static SceneEntity Placed(SceneEntity entity, Vector3? position = null, Vector3? rotation = null)
    {
        var was = TransformOf(entity);
        return entity.WithComponent(
            SceneComponents.Transform,
            new TransformComponent(position ?? was.Position.Clone(), rotation ?? was.Rotation.Clone(), was.Scale.Clone()));
    }

    private static SceneEntity Moved(SceneEntity crate, FrameControls controls, double seconds)
    {
        var (pushX, pushZ) = Push(controls);
        var step = Speed * seconds;
        var at = TransformOf(crate).Position;
        return Placed(crate, new Vector3(Kept(at.X + pushX * step), at.Y, Kept(at.Z + pushZ * step)));
    }

    // The way the arrow keys point, one metre long however many are held, so a diagonal is no faster.
    private static (double X, double Z) Push(FrameControls controls)
    {
        var x = Held(controls, "ArrowRight") - Held(controls, "ArrowLeft");
        var z = Held(controls, "ArrowDown") - Held(controls, "ArrowUp");
        if (x == 0 && z == 0)
        {
            return (0, 0);
        }

        var length = Math.Sqrt((x * x) + (z * z));
        return (x / length, z / length);
    }

    private static int Held(FrameControls controls, string key) => controls.Keys.Contains(key) ? 1 : 0;

    private static double Kept(double along) => Math.Clamp(along, -LastTile, LastTile);

    // Kept under a full circle so the angle does not grow for as long as the game runs.
    private static SceneEntity Turned(SceneEntity crate, double seconds)
    {
        var was = TransformOf(crate).Rotation;
        return Placed(crate, rotation: new Vector3(was.X, (was.Y + (DegreesPerSecond * seconds)) % 360, was.Z));
    }

    private SceneEntity Following(SceneEntity camera, SceneEntity crate)
    {
        var at = TransformOf(crate).Position;
        return Placed(camera, new Vector3(at.X + _cameraOffset.X, at.Y + _cameraOffset.Y, at.Z + _cameraOffset.Z));
    }

    // A copy of the first crate beside it in another colour, so the players can tell them apart. It
    // keeps the first one's net tag, which is what makes it the guest's to move.
    private static SceneEntity Second(SceneEntity crate)
    {
        var at = TransformOf(crate).Position;
        var drawn = crate[SceneComponents.Mesh] as MeshComponent ?? new MeshComponent();
        var painted = MeshComponent.FromJson(new Dictionary<string, object?>(drawn.ToJson()) { ["colour"] = SecondCrateColour });
        var copy = crate.CopyWith(id: SecondCrateId, name: "Second crate").WithComponent(SceneComponents.Mesh, painted);
        return Placed(copy, new Vector3(SecondCrateX, at.Y, at.Z));
    }

    // The crate this copy plays, or null while it owns none. Offline and on the host nobody is named, and
    // nobody owns the first crate, so they play it. A guest plays the one the server gave it, and
    // plays none until the server has said which.
    private static string? Mine(IGameNet net) => Crates.FirstOrDefault(id => net.OwnerOf(id) == net.LocalClient);

    // The crate moved and turned, and the camera behind it. A copy with no crate leaves the scene as it is.
    private SceneDocument Played(SceneDocument scene, GameContext context, double seconds)
    {
        if (Mine(context.Net) is not { } id)
        {
            return scene;
        }

        var crate = Turned(Moved(Find(scene, id), context.Input.Controls, seconds), seconds);
        return scene.WithEntity(id, crate).WithEntity(CameraId, Following(Find(scene, CameraId), crate));
    }

    private static SceneDocument Marked(SceneDocument scene, (double X, double Z)? tile)
    {
        if (tile is not { } at)
        {
            return scene;
        }

        var marker = Find(scene, MarkerId);
        return scene.WithEntity(MarkerId, Placed(marker, new Vector3(at.X, TransformOf(marker).Position.Y, at.Z)));
    }

    // Every copy asks the same way. The server's call runs at once. A client's goes to the server, which
    // moves the marker and sends it back. Offline the call runs at once too.
    private static void AskForMarker(GameContext context)
    {
        if (context.Input.Pointer is { Pressed: true } pointer && TileUnder(pointer) is { } tile)
        {
            context.Net.Call(nameof(PlaceMarker), new Vector3(tile.X, 0, tile.Z));
        }
    }

    // Where the pointer's ray meets the ground, as the centre of the tile there. Null for a ray that
    // never meets it, one that meets it behind the pointer, and one that lands off the ground.
    private static (double X, double Z)? TileUnder(GamePointer pointer)
    {
        if (pointer.Direction.Y >= 0)
        {
            return null;
        }

        var along = -pointer.Origin.Y / pointer.Direction.Y;
        return along < 0
            ? null
            : TileAt(pointer.Origin.X + (along * pointer.Direction.X), pointer.Origin.Z + (along * pointer.Direction.Z));
    }

    // Written as "inside" so a place that is not a number fails the test and is refused.
    private static (double X, double Z)? TileAt(double x, double z) =>
        Math.Abs(x) < GroundHalf && Math.Abs(z) < GroundHalf
            ? (Math.Floor(x) + 0.5, Math.Floor(z) + 0.5)
            : null;
}
