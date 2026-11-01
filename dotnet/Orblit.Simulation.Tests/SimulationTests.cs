using System.Diagnostics;
using Orblit.Net;

namespace Orblit.Simulation.Tests;

public sealed class SimulationTests(ITestOutputHelper output)
{
    [Fact]
    public void AHierarchyMovesWithItsParent()
    {
        using var sim = new Simulation();
        var body = sim.SpawnBody(0, 0, speed: 2);
        var carried = sim.Attach(body, offsetY: 1);

        for (var i = 0; i < 10; i++)
        {
            sim.Step(0.1f);
        }

        // Two units per second for one second.
        Assert.Equal(2, sim.WorldPositionOf(body).X, 1e-4);
        // The attachment travels with what carries it.
        var child = sim.WorldPositionOf(carried);
        Assert.Equal(2, child.X, 1e-4);
        Assert.Equal(1, child.Y, 1e-4);
    }

    [Fact]
    public void AParentsRotationMovesItsAttachmentAroundIt()
    {
        using var sim = new Simulation();
        var body = sim.SpawnBody(0, 0, speed: 0);
        var carried = sim.Attach(body, offsetY: 2);
        sim.SetSpin(body, Math.PI);
        sim.Step(0);

        // A turn about Y leaves something directly above unmoved, so its matrix shows the turn.
        var position = sim.WorldPositionOf(carried);
        Assert.Equal(2, position.Y, 1e-4);
        Assert.Equal(0, position.X, 1e-4);
        Assert.True(sim.World.TryGetFloat32(carried, sim.Transforms.World, out var matrix));
        // Its X axis now points along -X.
        Assert.Equal(-1, matrix[0], 1e-4);
        Assert.Equal(0, matrix[1], 1e-4);
        Assert.Equal(0, matrix[2], 1e-4);
    }

    [Fact]
    public async Task TheWholeSliceComposesSimulatePropagateReplicate()
    {
        await using var pair = new Pair();
        // Both replicate by their world transforms and not their parent links. The client is told
        // where things are, not how they got there.
        var body = pair.Host.SpawnBody(0, 0, speed: 3);
        var carried = pair.Host.Attach(body, offsetY: 1);
        var bodyId = pair.NetHost.Spawn(body);
        var carriedId = pair.NetHost.Spawn(carried);

        for (var tick = 0; tick < 20; tick++)
        {
            pair.Host.Step(1 / 20f);
            pair.NetHost.Publish();
            await pair.Link.IdleAsync();
        }

        Assert.Equal(2, pair.NetClient.EntityCount);

        var hostBody = pair.Host.WorldPositionOf(body);
        var replicaBody = pair.Replica.WorldPositionOf(pair.NetClient.EntityFor(bodyId)!.Value);
        Assert.Equal(hostBody.X, replicaBody.X, 1e-4);
        Assert.Equal(3, hostBody.X, 1e-3);

        // The attachment arrives already in world space, with no parent link on the client.
        var replicaCarriedEntity = pair.NetClient.EntityFor(carriedId)!.Value;
        var hostCarried = pair.Host.WorldPositionOf(carried);
        var replicaCarried = pair.Replica.WorldPositionOf(replicaCarriedEntity);
        Assert.Equal(hostCarried.X, replicaCarried.X, 1e-4);
        Assert.Equal(1, replicaCarried.Y, 1e-4);

        // Hierarchy is the authority's business. A replica needs the result, not the structure.
        Assert.False(pair.Replica.World.Has(replicaCarriedEntity, pair.Replica.Transforms.Parent));
    }

    [Fact]
    public async Task AThousandBodiesStepAndReplicateWithoutPerEntityCalls()
    {
        await using var pair = new Pair();
        const int count = 1000;
        var last = 0ul;
        for (var i = 0; i < count; i++)
        {
            last = pair.Host.SpawnBody(i, 0, speed: 1);
            pair.NetHost.Spawn(last);
        }

        var stopwatch = Stopwatch.StartNew();
        for (var tick = 0; tick < 10; tick++)
        {
            pair.Host.Step(1 / 60f);
            pair.NetHost.Publish();
            await pair.Link.IdleAsync();
        }

        stopwatch.Stop();

        Assert.Equal(count, pair.NetClient.EntityCount);
        // The last row moved by its own velocity, so the pass walks each row's own transform.
        Assert.Equal(count - 1 + (10 / 60f), pair.Host.WorldPositionOf(last).X, 1e-3);
        output.WriteLine($"{count} bodies, 10 ticks of simulate, propagate and replicate: {stopwatch.ElapsedMilliseconds} ms");
    }

    /// <summary>An authority and a replica, each a whole simulation, joined by a loopback link.</summary>
    private sealed class Pair : IAsyncDisposable
    {
        public Pair()
        {
            NetHost = new NetHost(new NetHostOptions { World = Host.World, Set = Host.Replication, NetworkId = Host.NetworkId });
            NetClient = new NetClient(new NetClientOptions
            {
                World = Replica.World,
                Set = Replica.Replication,
                NetworkId = Replica.NetworkId,
                Transport = Link.Client,
            });
            NetHost.AddClient("player-1", Link.Host);
        }

        public Simulation Host { get; } = new();

        public Simulation Replica { get; } = new();

        public LoopbackLink Link { get; } = new();

        public NetHost NetHost { get; }

        public NetClient NetClient { get; }

        public async ValueTask DisposeAsync()
        {
            NetClient.Dispose();
            NetHost.Dispose();
            await Link.CloseAsync();
            Replica.Dispose();
            Host.Dispose();
        }
    }
}
