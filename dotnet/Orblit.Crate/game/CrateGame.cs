using Orblit.Game;
using Orblit.Input;
using Orblit.Scene;
using Orblit.VectorMath;

namespace Crate;

/// <summary>
/// A crate on a ground plane. The arrow keys move it, it turns on its own, and a click puts a marker on
/// the tile under the pointer. The camera keeps the distance it was placed at.
/// </summary>
/// <remarks>
/// Everything the game knows is in the scene, so an edit made in the editor while it plays is read on the
/// next frame. The only thing it keeps is the camera's offset from the crate, found once at the start.
/// </remarks>
public sealed class CrateGame : IGame
{
    private const string CrateId = "crate";
    private const string MarkerId = "marker";
    private const string CameraId = "camera";

    private const double Speed = 4;
    private const double DegreesPerSecond = 90;

    // The ground is 12 metres square and a tile is a metre, so the last tile centre is half a metre in.
    private const double GroundHalf = 6;
    private const double LastTile = GroundHalf - 0.5;

    private (double X, double Y, double Z) _cameraOffset;

    public void Start(GameContext context)
    {
        var scene = context.Scene.Document;
        var crate = TransformOf(Find(scene, CrateId)).Position;
        _ = Find(scene, MarkerId);
        var camera = TransformOf(Find(scene, CameraId)).Position;
        _cameraOffset = (camera.X - crate.X, camera.Y - crate.Y, camera.Z - crate.Z);
    }

    public void Update(GameContext context, double seconds)
    {
        var before = context.Scene.Document;
        var crate = Turned(Moved(Find(before, CrateId), context.Input.Controls, seconds), seconds);
        var after = before
            .WithEntity(CrateId, crate)
            .WithEntity(CameraId, Following(Find(before, CameraId), crate))
            .WithEntity(MarkerId, Marked(Find(before, MarkerId), context.Input.Pointer));
        context.Scene.Apply(SceneDiff.Between(before, after));
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

    private static SceneEntity Marked(SceneEntity marker, GamePointer? pointer)
    {
        if (pointer is not { Pressed: true } || TileUnder(pointer) is not { } tile)
        {
            return marker;
        }

        return Placed(marker, new Vector3(tile.X, TransformOf(marker).Position.Y, tile.Z));
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
        var x = pointer.Origin.X + (along * pointer.Direction.X);
        var z = pointer.Origin.Z + (along * pointer.Direction.Z);
        return along < 0 || Math.Abs(x) >= GroundHalf || Math.Abs(z) >= GroundHalf
            ? null
            : (Math.Floor(x) + 0.5, Math.Floor(z) + 0.5);
    }
}
