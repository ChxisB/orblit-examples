namespace Orblit.Simulation;

/// <summary>How fast a body is moving, in world units per second.</summary>
/// <remarks>Owner-writable: a client drives its own body, and the authority decides where that got it.</remarks>
[OrblitComponent(Replicated = true, OwnerWritable = true)]
internal sealed class Velocity
{
    public float X;
    public float Y;
    public float Z;
}

/// <summary>A replicated entity's stable identity on the wire.</summary>
[OrblitComponent]
internal sealed class NetworkId
{
    public long Value;
}
