using Orblit.Core;
using Orblit.Net;

namespace Orblit.Simulation;

/// <summary>
/// A small world that exercises the parts of the engine that have to agree: storage, the
/// transform hierarchy, a system written over columns, and replication of the result.
/// </summary>
/// <remarks>
/// Headless on purpose. The renderer is a separate concern with its own proof, and a simulation
/// that only runs inside a window cannot be tested.
/// </remarks>
internal sealed class Simulation : IDisposable
{
    private readonly Query _movers;

    public Simulation()
    {
        Transforms = World.RegisterTransforms();
        var components = new OrblitSimulationComponents(World);
        Velocity = components.Velocity;
        NetworkId = components.NetworkId;

        // The world transform is what replicates: a client needs where things ended up, not the
        // parent links that got them there.
        Replication = new ReplicationSet([Transforms.World, Velocity]);
        _movers = World.Query(Transforms.Local, Velocity);
    }

    public World World { get; } = new();

    public TransformComponents Transforms { get; }

    public ComponentType Velocity { get; }

    public ComponentType NetworkId { get; }

    public ReplicationSet Replication { get; }

    /// <summary>A body that moves under its own velocity.</summary>
    public ulong SpawnBody(float x, float y, float speed)
    {
        var entity = World.CreateEntity();
        World.Add<float>(entity, Transforms.Local, Core.Transforms.Local(x, y));
        World.Add(entity, Transforms.World);
        World.Add<float>(entity, Velocity, [speed, 0, 0]);
        return entity;
    }

    /// <summary>Something carried by another entity, in its parent's space.</summary>
    public ulong Attach(ulong parent, float offsetY)
    {
        var entity = World.CreateEntity();
        World.Add<float>(entity, Transforms.Local, Core.Transforms.Local(y: offsetY));
        World.Add(entity, Transforms.World);
        World.Add<long>(entity, Transforms.Parent, [(long)parent]);
        return entity;
    }

    /// <summary>Turns an entity about Y, in place.</summary>
    public void SetSpin(ulong entity, double radians)
    {
        World.TryGetFloat32(entity, Transforms.Local, out var local);
        local[4] = (float)Math.Sin(radians / 2);
        local[6] = (float)Math.Cos(radians / 2);
    }

    /// <summary>One step of the simulation.</summary>
    /// <remarks>
    /// The movement pass is the shape every system should have: fetch the columns once, then
    /// loop in C#. Nothing here calls into the engine per entity.
    /// </remarks>
    public void Step(float delta)
    {
        foreach (var chunk in _movers.Chunks)
        {
            var locals = chunk.Float32(0);
            // The generated column names the offsets, so a system reads as fields rather than as
            // arithmetic, and costs the same, since it is the same memory.
            var velocities = new VelocityColumn(chunk.Float32(1));
            for (var row = 0; row < chunk.Length; row++)
            {
                var at = row * 10;
                locals[at] += velocities.X(row) * delta;
                locals[at + 1] += velocities.Y(row) * delta;
                locals[at + 2] += velocities.Z(row) * delta;
            }
        }

        // Children follow their parents only once the parents have moved, so this runs after the
        // movement pass rather than beside it.
        World.PropagateTransforms();
    }

    /// <summary>Where <paramref name="entity"/> ended up in <paramref name="world"/>, read out of its world matrix.</summary>
    public static (float X, float Y, float Z) WorldPositionOf(World world, TransformComponents transforms, ulong entity)
    {
        world.TryGetFloat32(entity, transforms.World, out var matrix);
        return (matrix[12], matrix[13], matrix[14]);
    }

    public void Dispose()
    {
        _movers.Dispose();
        World.Dispose();
    }
}
