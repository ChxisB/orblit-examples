namespace Orblit.Simulation;

/// <summary>Runs the slice and prints where things ended up, so the engine runs without a window.</summary>
internal static class Program
{
    public static void Main()
    {
        using var sim = new Simulation();
        var body = sim.SpawnBody(0, 0, speed: 2);
        var carried = sim.Attach(body, offsetY: 1);
        // Nothing has a world transform until the first propagation, so tick zero would otherwise
        // report everything at the origin.
        sim.Step(0);

        Console.WriteLine("tick  body                    attachment");
        for (var tick = 0; tick <= 5; tick++)
        {
            if (tick > 0)
            {
                sim.Step(0.2f);
            }

            var b = Simulation.WorldPositionOf(sim.World, sim.Transforms, body);
            var c = Simulation.WorldPositionOf(sim.World, sim.Transforms, carried);
            Console.WriteLine($"{tick,-6}{Format(b),-24}{Format(c)}");
        }
    }

    private static string Format((float X, float Y, float Z) at) => $"({at.X:F2}, {at.Y:F2}, {at.Z:F2})";
}
