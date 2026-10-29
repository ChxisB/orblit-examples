using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Orblit.Examples;

namespace Orblit.Gallery;

/// <summary>
/// The runner's screen and its hands. It shows the score, the cards that stand in front of the game,
/// and a "+N" over the runner as coins are taken. Keys play it. A tap means go, and a swipe changes
/// lane, jumps or slides.
/// </summary>
internal sealed class RunnerOverlay : Panel, IStageLayer
{
    // A drag this long is clearly a swipe, and is taken at once rather than when the finger lifts. A
    // lane change that waits for the end of the gesture is a tenth of a second late.
    private const double Swipe = 28;

    // A press that moves less than this before it lets go is a tap.
    private const double Tap = 4;

    private static readonly IBrush Gold = GalleryColours.Of(0xFFFFD34D);
    private static readonly IBrush Grey = GalleryColours.Of(0xFFB4BCCB);
    private static readonly IBrush Pale = GalleryColours.Of(0xFFD5DAE3);
    private static readonly IBrush Faint = GalleryColours.Of(0xFF8E97A8);

    // Keys by where they sit, as the walk takes them, so the letters are in the same place on any layout.
    private static readonly Dictionary<PhysicalKey, Move> Moves = new()
    {
        [PhysicalKey.ArrowLeft] = Move.Left,
        [PhysicalKey.A] = Move.Left,
        [PhysicalKey.ArrowRight] = Move.Right,
        [PhysicalKey.D] = Move.Right,
        [PhysicalKey.ArrowUp] = Move.Up,
        [PhysicalKey.W] = Move.Up,
        [PhysicalKey.Space] = Move.Up,
        [PhysicalKey.ArrowDown] = Move.Down,
        [PhysicalKey.S] = Move.Down,
        [PhysicalKey.Escape] = Move.Stop,
        [PhysicalKey.P] = Move.Stop,
        [PhysicalKey.Enter] = Move.Go,
        [PhysicalKey.NumPadEnter] = Move.Go,
    };

    private readonly RunnerExample _game;
    private readonly HashSet<PhysicalKey> _held = [];
    private readonly Border _flash = new() { Background = GalleryColours.Of(0xFFE0302A), IsHitTestVisible = false };
    private readonly TextBlock _score = Number(22, FontWeight.ExtraBold, Brushes.White);
    private readonly TextBlock _coins = Number(18, FontWeight.Bold, Gold);
    private readonly TextBlock _speed = Number(14, FontWeight.Medium, Grey);
    private readonly TextBlock _auto = new()
    {
        Text = "AUTO",
        FontSize = 11,
        FontWeight = FontWeight.ExtraBold,
        Foreground = GalleryColours.Of(0xFF7ED9B7),
        VerticalAlignment = VerticalAlignment.Center,
    };

    private readonly TextBlock _popup = new()
    {
        Width = 120,
        FontSize = 26,
        FontWeight = FontWeight.Black,
        Foreground = Gold,
        TextAlignment = TextAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        Effect = new DropShadowEffect { Color = Color.FromUInt32(0xCC3A2500), BlurRadius = 5, OffsetX = 0, OffsetY = 0 },
    };

    private readonly TextBlock _careful = new()
    {
        Text = "Careful!",
        Margin = new Thickness(0, 70, 0, 0),
        FontSize = 20,
        FontWeight = FontWeight.ExtraBold,
        Foreground = GalleryColours.Of(0xFFFFB13B),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Top,
        Effect = new DropShadowEffect { Color = Color.FromUInt32(0xAA000000), BlurRadius = 6, OffsetX = 0, OffsetY = 0 },
    };

    private readonly TextBlock _best = Said(string.Empty, Faint, 12);
    private readonly TextBlock _newBest = new() { Text = "A new best", Foreground = Gold, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center };
    private readonly TextBlock _overScore = Number(17, FontWeight.ExtraBold, Brushes.White);
    private readonly TextBlock _overCoins = Number(17, FontWeight.ExtraBold, Gold);
    private readonly TextBlock _overDistance = Number(17, FontWeight.ExtraBold, Brushes.White);
    private readonly TextBlock _overBest = Number(17, FontWeight.ExtraBold, Brushes.White);
    private readonly Border _hud;
    private readonly Border _ready;
    private readonly Border _paused;
    private readonly Border _over;
    private Point? _pressed;
    private bool _swiped;

    private enum Move
    {
        Left,
        Right,
        Up,
        Down,
        Stop,
        Go,
    }

    public RunnerOverlay(RunnerExample game)
    {
        _game = game;
        // Without a background a tap between the cards would fall through to the stage and turn it.
        Background = Brushes.Transparent;
        Focusable = true;
        TextElement.SetForeground(this, Brushes.White);
        TextElement.SetFontSize(this, 14);
        TextElement.SetFontWeight(this, FontWeight.Medium);
        _hud = Hud(_score, _coins, _speed, _auto);
        _best.Margin = new Thickness(0, 6, 0, 0);
        _ready = Card(
            VerticalAlignment.Bottom,
            Heading("Tap or press Space to run", 20, FontWeight.ExtraBold),
            Gap(10),
            Keys("← →  or  A D", "change lane"),
            Keys("↑  W  or  Space", "jump"),
            Keys("↓  or  S", "slide"),
            Keys("Esc", "pause"),
            Gap(6),
            Said("On a touch screen, swipe.", Faint, 12),
            _best);
        _ready.Margin = new Thickness(16, 0, 16, 20);
        _paused = Card(
            VerticalAlignment.Center,
            Heading("Paused", 24, FontWeight.ExtraBold),
            Gap(6),
            Said("Tap, or press Esc or Space, to carry on", Grey, 14));
        _over = Card(
            VerticalAlignment.Center,
            Heading("Crashed!", 26, FontWeight.Black),
            _newBest,
            Gap(10),
            Figure("Score", _overScore),
            Figure("Coins", _overCoins),
            Figure("Distance", _overDistance),
            Figure("Best", _overBest),
            Gap(12),
            Said("Tap or press Space to run again", Pale, 14));
        Children.AddRange([_flash, _hud, _popup, _careful, _ready, _paused, _over]);
        Refresh(0);
    }

    public void Refresh(double seconds)
    {
        var phase = _game.Phase;
        var since = _game.SinceCrash;
        // Red for a moment at the crash, fading.
        _flash.IsVisible = phase == RunnerPhase.Over && since < 0.45;
        _flash.Opacity = 0.35 * (1 - (since / 0.45));
        _hud.IsVisible = phase != RunnerPhase.Ready;
        _score.Text = Whole(_game.Score);
        _coins.Text = Whole(_game.Coins);
        _speed.Text = Whole(Math.Round(_game.Speed, MidpointRounding.AwayFromZero)) + " m/s";
        _auto.IsVisible = _game.Autopilot;
        _careful.IsVisible = _game.Careful;
        _ready.IsVisible = phase == RunnerPhase.Ready;
        _paused.IsVisible = phase == RunnerPhase.Paused;
        // Not in the moment of the crash, while the runner is seen going down.
        _over.IsVisible = phase == RunnerPhase.Over && since > 0.7;
        ShowScores();
        ShowPopup();
    }

    private void ShowScores()
    {
        _best.IsVisible = _game.Best > 0;
        _best.Text = "Best " + Whole(_game.Best);
        _newBest.IsVisible = _game.NewBest;
        _overScore.Text = Whole(_game.Score);
        _overCoins.Text = Whole(_game.Coins);
        _overDistance.Text = Whole(Math.Floor(_game.Travelled)) + " m";
        _overBest.Text = Whole(_game.Best);
    }

    private void ShowPopup()
    {
        var popup = _game.Popup();
        _popup.IsVisible = popup is not null;
        if (popup is null)
        {
            return;
        }

        _popup.Text = "+" + Whole(popup.Points);
        _popup.Opacity = popup.Opacity;
        var x = (popup.At.X + 1) / 2 * Bounds.Width;
        var y = (1 - popup.At.Y) / 2 * Bounds.Height;
        _popup.Margin = new Thickness(x - 60, y - 20, 0, 0);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Focus();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        // A tall window needs a wider view to keep all three lanes in it.
        if (e.NewSize is { Width: > 0, Height: > 0 } size)
        {
            _game.Aspect = size.Width / size.Height;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!Moves.TryGetValue(e.PhysicalKey, out var move))
        {
            return;
        }

        e.Handled = true;
        // A held key repeats, and a held arrow is not three lane changes.
        if (_held.Add(e.PhysicalKey))
        {
            Play(move);
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        _held.Remove(e.PhysicalKey);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        _held.Clear();
        // Somebody who clicked somewhere else is not playing.
        if (_game.Phase == RunnerPhase.Running && !_game.Autopilot)
        {
            _game.Pause();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        _pressed = e.GetPosition(this);
        _swiped = false;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pressed is not { } from || _swiped)
        {
            return;
        }

        // Once per swipe, as soon as it is clearly one.
        Vector moved = e.GetPosition(this) - from;
        if (moved.Length >= Swipe)
        {
            _swiped = true;
            Play(Math.Abs(moved.X) > Math.Abs(moved.Y) ? (moved.X > 0 ? Move.Right : Move.Left) : moved.Y < 0 ? Move.Up : Move.Down);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pressed is { } from && !_swiped && ((Vector)(e.GetPosition(this) - from)).Length < Tap)
        {
            _game.Confirm();
        }

        _pressed = null;
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pressed = null;
    }

    /// <summary>Stop pauses and carries on. Anything else means go until the run is under way, and then plays.</summary>
    private void Play(Move move)
    {
        if (move == Move.Stop)
        {
            if (_game.Phase == RunnerPhase.Running)
            {
                _game.Pause();
            }
            else
            {
                _game.Resume();
            }

            return;
        }

        if (_game.Phase != RunnerPhase.Running)
        {
            _game.Confirm();
            return;
        }

        switch (move)
        {
            case Move.Left:
                _game.Steer(-1);
                break;
            case Move.Right:
                _game.Steer(1);
                break;
            case Move.Up:
                _game.Jump();
                break;
            case Move.Down:
                _game.Slide();
                break;
            default:
                break;
        }
    }

    private static string Whole(double value) => value.ToString("F0", CultureInfo.InvariantCulture);

    private static Border Hud(TextBlock score, TextBlock coins, TextBlock speed, TextBlock auto)
    {
        coins.Margin = new Thickness(6, 0, 0, 0);
        speed.Margin = new Thickness(16, 0, 0, 0);
        auto.Margin = new Thickness(12, 0, 0, 0);
        return new Border
        {
            Margin = new Thickness(0, 14, 0, 0),
            Padding = new Thickness(18, 7),
            CornerRadius = new CornerRadius(18),
            Background = GalleryColours.Of(0xD91A1D26),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { score, Coin(), coins, speed, auto } },
        };
    }

    /// <summary>A coin, drawn: a gold disc lit from the top left, with a paler rim.</summary>
    private static Ellipse Coin() => new()
    {
        Width = 18,
        Height = 18,
        Margin = new Thickness(16, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center,
        Fill = new RadialGradientBrush
        {
            Center = new RelativePoint(0.35, 0.325, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.35, 0.325, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromUInt32(0xFFFFF0A0), 0),
                new GradientStop(Color.FromUInt32(0xFFFFC21A), 0.55),
                new GradientStop(Color.FromUInt32(0xFFC98A00), 1),
            },
        },
        Stroke = GalleryColours.Of(0xFFFFE27A),
        StrokeThickness = 1.2,
    };

    private static Border Card(VerticalAlignment where, params Control[] children)
    {
        var column = new StackPanel();
        foreach (var child in children)
        {
            child.HorizontalAlignment = HorizontalAlignment.Center;
            column.Children.Add(child);
        }

        return new Border
        {
            Padding = new Thickness(22, 16),
            CornerRadius = new CornerRadius(16),
            Background = GalleryColours.Of(0xE61A1D26),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = where,
            Child = column,
        };
    }

    private static TextBlock Heading(string text, double size, FontWeight weight) =>
        new() { Text = text, FontSize = size, FontWeight = weight };

    private static TextBlock Said(string text, IBrush colour, double size) =>
        new() { Text = text, Foreground = colour, FontSize = size };

    private static Control Gap(double height) => new() { Height = height };

    // Figures of one width, so a number does not shuffle sideways as it changes.
    private static TextBlock Number(double size, FontWeight weight, IBrush colour) =>
        new() { FontSize = size, FontWeight = weight, Foreground = colour, FontFeatures = [FontFeature.Parse("tnum")] };

    /// <summary>A key and what it does, the keys right against the middle so the two columns read as pairs.</summary>
    private static StackPanel Keys(string keys, string does) => new()
    {
        Orientation = Orientation.Horizontal,
        Margin = new Thickness(0, 1.5),
        Spacing = 10,
        Children =
        {
            new TextBlock { Text = keys, Width = 118, TextAlignment = TextAlignment.Right, Foreground = Gold, FontWeight = FontWeight.Bold, FontSize = 13 },
            new TextBlock { Text = does, Width = 110, Foreground = Pale, FontSize = 13 },
        },
    };

    private static StackPanel Figure(string label, TextBlock value)
    {
        value.Width = 76;
        value.TextAlignment = TextAlignment.Right;
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 2),
            Children = { new TextBlock { Text = label, Width = 76, Foreground = Grey }, value },
        };
    }
}
