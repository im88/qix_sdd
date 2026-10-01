namespace Qix.Core;

public enum MarkerMode
{
    OnBorder,
    Drawing,
}

public enum GamePhase
{
    /// <summary>Normal play: the marker and the Qix move.</summary>
    Playing,

    /// <summary>The line was hit; the game waits <see cref="Game.LifeLostPause"/> before play resumes.</summary>
    LifeLost,

    /// <summary>The last life was lost.</summary>
    GameOver,

    /// <summary>The target percentage was reached.</summary>
    Complete,
}

/// <summary>The player-controlled cursor.</summary>
public sealed class Marker(Point position)
{
    public Point Position { get; set; } = position;

    public MarkerMode Mode { get; set; } = MarkerMode.OnBorder;

    /// <summary>The border cell the marker left from when the current line began; the respawn point after a lost life.</summary>
    public Point LineStart { get; set; }

    /// <summary>Cells drawn since leaving the border; empty while <see cref="MarkerMode.OnBorder"/>.</summary>
    public List<Point> Trail { get; } = [];
}

/// <summary>
/// Ties together the playfield, the marker, the Qix, lives, and the session phase. Driven by
/// player moves and by <see cref="Advance"/>, which lets time pass for the Qix and the pause.
/// </summary>
public sealed class Game
{
    public const int TargetPercent = 75;

    public const int StartingLives = 3;

    /// <summary>How long the hit line stays on screen before play resumes.</summary>
    public static readonly TimeSpan LifeLostPause = TimeSpan.FromSeconds(1);

    /// <summary>Time per Qix step: about 8 cells per second.</summary>
    public static readonly TimeSpan QixStepInterval = TimeSpan.FromMilliseconds(125);

    /// <summary>Longest time one <see cref="Advance"/> call counts, so a stalled loop can't make the Qix jump (research R10).</summary>
    private static readonly TimeSpan MaxAdvance = TimeSpan.FromMilliseconds(250);

    private readonly Random _random;
    private TimeSpan _qixClock;
    private TimeSpan _pauseLeft;

    public Game(Playfield? playfield = null, Random? random = null)
    {
        _random = random ?? new Random();
        Playfield = playfield ?? new Playfield();
        Marker = new Marker(StartPosition(Playfield));
        Qix = NewQix(Playfield);
    }

    public Playfield Playfield { get; private set; }

    public Marker Marker { get; private set; }

    public Qix Qix { get; private set; }

    public bool DrawOn { get; private set; }

    public int Lives { get; private set; } = StartingLives;

    public GamePhase Phase { get; private set; } = GamePhase.Playing;

    public void ToggleDraw()
    {
        if (Phase == GamePhase.Playing)
        {
            DrawOn = !DrawOn;
        }
    }

    /// <summary>Applies one move (data-model transition table); ignored outside <see cref="GamePhase.Playing"/>.</summary>
    public void Move(Direction direction)
    {
        var target = Marker.Position + direction;
        if (Phase != GamePhase.Playing || !Playfield.Contains(target))
        {
            return;
        }

        var drawing = Marker.Mode == MarkerMode.Drawing;
        switch (Playfield[target])
        {
            case Cell.Border when !drawing:
                Marker.Position = target;
                break;
            case Cell.Border when DrawOn:
                Marker.Position = target;
                CloseLine();
                break;
            case Cell.Empty when DrawOn:
                if (!drawing)
                {
                    Marker.LineStart = Marker.Position;
                }

                Marker.Position = target;
                Playfield.SetTrail(target);
                Marker.Trail.Add(target);
                Marker.Mode = MarkerMode.Drawing;
                if (Marker.Position == Qix.Position)
                {
                    LoseLife();
                }

                break;

            // Everything else is blocked: Empty with draw off, any move while drawing with
            // draw off, and Trail or Claimed targets.
        }
    }

    /// <summary>
    /// Lets time pass (research R10): steps the Qix once per <see cref="QixStepInterval"/> and
    /// counts down the life-lost pause. Returns true if anything visible changed.
    /// </summary>
    public bool Advance(TimeSpan elapsed)
    {
        if (elapsed > MaxAdvance)
        {
            elapsed = MaxAdvance;
        }

        switch (Phase)
        {
            case GamePhase.LifeLost:
                _pauseLeft -= elapsed;
                if (_pauseLeft > TimeSpan.Zero)
                {
                    return false;
                }

                EndLifeLostPause();
                return true;

            case GamePhase.Playing:
                _qixClock += elapsed;
                var changed = false;
                while (_qixClock >= QixStepInterval)
                {
                    _qixClock -= QixStepInterval;
                    switch (Qix.Step(Playfield))
                    {
                        case QixStepOutcome.Moved:
                            changed = true;
                            break;
                        case QixStepOutcome.Contact:
                            LoseLife();
                            return true;
                    }
                }

                return changed;

            default:
                return false;
        }
    }

    /// <summary>Starts the same empty playfield again with full lives and the Qix at the center.</summary>
    public void Restart()
    {
        Playfield = new Playfield(Playfield.Width, Playfield.Height);
        Marker = new Marker(StartPosition(Playfield));
        Qix = NewQix(Playfield);
        _qixClock = TimeSpan.Zero;
        _pauseLeft = TimeSpan.Zero;
        Lives = StartingLives;
        DrawOn = false;
        Phase = GamePhase.Playing;
    }

    private void LoseLife()
    {
        Lives--;
        if (Lives == 0)
        {
            Phase = GamePhase.GameOver;
        }
        else
        {
            Phase = GamePhase.LifeLost;
            _pauseLeft = LifeLostPause;
        }
    }

    private void EndLifeLostPause()
    {
        Playfield.ClearTrail();
        Marker.Trail.Clear();
        Marker.Position = Marker.LineStart;
        Marker.Mode = MarkerMode.OnBorder;
        DrawOn = false;
        _qixClock = TimeSpan.Zero;
        Phase = GamePhase.Playing;
    }

    private void CloseLine()
    {
        Playfield.CloseTrail(Qix.Position);
        Marker.Trail.Clear();
        Marker.Mode = MarkerMode.OnBorder;
        DrawOn = false;
        if (Playfield.ClaimedPercent >= TargetPercent)
        {
            Phase = GamePhase.Complete;
        }
    }

    private Qix NewQix(Playfield playfield) =>
        new(new Point(playfield.Width / 2, playfield.Height / 2), Velocity.Diagonals[_random.Next(Velocity.Diagonals.Length)]);

    private static Point StartPosition(Playfield playfield) =>
        new(playfield.Width / 2, playfield.Height - 1);
}
