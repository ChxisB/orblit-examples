using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Orblit.Examples;

namespace Orblit.Gallery;

/// <summary>
/// What a frame of the benchmark costs the GPU, beside the load that costs it. Read four times a
/// second, because a number that changes every frame cannot be read at all.
/// </summary>
internal sealed class Readout : Border, IStageLayer
{
    private const double Every = 0.25;

    private static readonly IBrush Grey = GalleryColours.Of(0xFF8A93A0);

    private readonly BenchmarkExample _benchmark;
    private readonly Func<double> _gpuMilliseconds;
    private readonly TextBlock _cost = new()
    {
        FontSize = 20,
        FontWeight = FontWeight.SemiBold,
        // Figures of one width, so the number does not shuffle sideways as it changes.
        FontFeatures = [FontFeature.Parse("tnum")],
    };

    private readonly TextBlock _rate = new() { FontSize = 12, Foreground = Grey };
    private readonly TextBlock _load = new() { FontSize = 11, Foreground = GalleryColours.Of(0xFF6E7681), Margin = new Thickness(0, 6, 0, 0) };
    private double? _sampled;

    public Readout(BenchmarkExample benchmark, Func<double> gpuMilliseconds)
    {
        _benchmark = benchmark;
        _gpuMilliseconds = gpuMilliseconds;
        IsHitTestVisible = false;
        Margin = new Thickness(18);
        Padding = new Thickness(16, 12);
        CornerRadius = new CornerRadius(10);
        Background = GalleryColours.Of(0xE6111418);
        BorderBrush = GalleryColours.Of(0xFF2A313A);
        BorderThickness = new Thickness(1);
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        Child = new StackPanel { Children = { _cost, _rate, _load } };
        Show(0);
    }

    public void Refresh(double seconds)
    {
        _load.Text = _benchmark.Load;
        if (_sampled is { } last && seconds - last < Every)
        {
            return;
        }

        _sampled = seconds;
        Show(_gpuMilliseconds());
    }

    /// <summary>
    /// Green inside a frame at 120 a second, amber inside one at 60, red past that, and grey before the
    /// GPU has reported.
    /// </summary>
    private void Show(double cost)
    {
        _cost.Foreground = cost switch
        {
            <= 0 => Grey,
            < 8 => GalleryColours.Of(0xFF7FB069),
            < 16.6 => GalleryColours.Of(0xFFE0B252),
            _ => GalleryColours.Of(0xFFD9634F),
        };
        _cost.Text = cost > 0 ? cost.ToString("F2", CultureInfo.InvariantCulture) + " ms a frame" : "measuring…";
        _rate.IsVisible = cost > 0;
        _rate.Text = cost > 0
            ? Math.Round(1000 / cost, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture) + " a second if nothing waited for the display"
            : null;
    }
}
