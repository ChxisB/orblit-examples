using System.Globalization;
using Orblit.Examples;
using Orblit.Filament;
using Orblit.Light;
using Orblit.VectorMath;

namespace Orblit.Gallery;

/// <summary>
/// A heads-up display over a scene: a chip that names the place, and a readout of power and shields.
/// The example holds what it says, and <see cref="NativeInterfaceOverlay"/> draws it.
/// </summary>
/// <remarks>
/// An Orblit interface is Avalonia controls, so a game draws its display with the same controls as
/// the rest of its window. There is nothing between the two to learn.
/// </remarks>
public sealed class NativeInterfaceExample : Example
{
    /// <summary>How wide the power bar is when full, in pixels.</summary>
    internal const double Track = 224;

    // The bar's colour, and the lit pips' a step lighter, as ARGB.
    private static readonly (string Name, uint Bar, uint Pip)[] Accents =
    [
        ("ember", 0xFFC25E22, 0xFFD97B37),
        ("steel", 0xFF3B7F97, 0xFF56A3BE),
        ("moss", 0xFF2F7A4E, 0xFF4FA871),
        ("rose", 0xFFC2415A, 0xFFD95E70),
    ];

    public override string Name => "An interface in C#";

    public override string Blurb => "A heads-up display made of Avalonia controls, changed in place as the settings change.";

    public override ExampleSection Section => ExampleSection.Interface;

    public override ViewPoint ViewPoint => new() { Distance = 12, Pitch = 0.22 };

    internal double Health { get; set; } = 0.72;

    internal int Shields { get; set; } = 3;

    internal string Accent { get; set; } = "steel";

    /// <summary>The power as the readout prints it, to the whole percent.</summary>
    internal string PowerLabel => $"{Whole(Health * 100)}%";

    /// <summary>How much of the track the bar fills, to the whole pixel.</summary>
    internal double BarWidth => Math.Round(Health * Track, MidpointRounding.AwayFromZero);

    internal (uint Bar, uint Pip) AccentColours
    {
        get
        {
            var accent = Accents.Single(one => one.Name == Accent);
            return (accent.Bar, accent.Pip);
        }
    }

    public override RenderScene Scene(RenderCamera camera, double seconds) => new()
    {
        Objects = [.. Enumerable.Range(0, 4).Select(i => Orbiting(i, seconds)), Ground],
        Lights =
        [
            new RenderLight
            {
                Key = 860,
                Kind = RenderLightKind.Directional,
                Intensity = 74000,
                Direction = new Vector3(-0.4, -1, -0.45).Normalized(),
                Colour = Tint.Hex(0xFFF3E0).Linear,
                SunAngularRadius = 1.5,
            },
        ],
        Sky = new Sky { Colour = Tint.Hex(0x161C25).Linear, Ambient = 11000 },
        Camera = camera,
    };

    public override IReadOnlyList<ExampleSetting> Settings() =>
    [
        new Setting("Power", Health, 0, 1, value => Health = value),
        new Setting("Shields", Shields, 0, 5, value => Shields = (int)Math.Round(value, MidpointRounding.AwayFromZero)) { Decimals = 0 },
        new Choice("Accent", [.. Accents.Select(one => one.Name)], Accent, option => Accent = option),
    ];

    public override string Code => """
        // Plain Avalonia controls, built once. The bar sits in a track, a
        // second Border with ClipToBounds, so a full bar keeps its round ends.
        private readonly TextBlock _power = new() { FontSize = 11, Foreground = Soft };
        private readonly Border _bar = new() { Height = 8, CornerRadius = new CornerRadius(4), HorizontalAlignment = HorizontalAlignment.Left };
        private readonly Border[] _pips = [.. Enumerable.Range(0, 5).Select(i => new Border { Width = 24, Height = 4 })];

        // Then changed in place each frame, from the settings.
        public void Refresh(double seconds)
        {
            // Brushes are made when the accent changes, not every frame.
            var colours = _example.AccentColours;
            if (colours != _colours)
            {
                _colours = colours;
                _bar.Background = GalleryColours.Of(colours.Bar);
                _lit = GalleryColours.Of(colours.Pip);
            }

            _bar.Width = _example.BarWidth;
            _power.Text = _example.PowerLabel;
            for (var i = 0; i < _pips.Length; i++)
            {
                _pips[i].Background = i < _example.Shields ? _lit : Groove;
            }
        }
        """;

    private static RenderObject Ground
    {
        get
        {
            var transform = Matrix4.Identity();
            transform.SetTranslation(new Vector3(0, -1.7, 0));
            transform.ScaleByDouble(10, 0.06, 10, 1);
            return new RenderObject { Key = 840, Transform = transform, Colour = Tint.Hex(0x39404A).Linear, CastShadows = false };
        }
    }

    private static RenderObject Orbiting(int i, double seconds)
    {
        var transform = Matrix4.Identity();
        transform.SetTranslation(new Vector3(
            Math.Cos((seconds * 0.3) + (i * 1.6)) * 2.6,
            -0.4 + (Math.Sin((seconds * 0.5) + i) * 0.35),
            Math.Sin((seconds * 0.3) + (i * 1.6)) * 2.6));
        transform.RotateY((seconds * 0.4) + i);
        transform.ScaleByDouble(0.6, 0.6, 0.6, 1);
        return new RenderObject { Key = 850 + i, Transform = transform, Colour = Tint.Hex(i % 2 == 0 ? 0x5FA8D3 : 0x7FB069).Linear };
    }

    private static string Whole(double value) => Math.Round(value, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
}
