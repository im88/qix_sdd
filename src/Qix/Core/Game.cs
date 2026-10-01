namespace Qix.Core;

public enum MarkerMode
{
    OnBorder,
    Drawing,
}

public enum GamePhase
{
    Playing,
    Complete,
}

/// <summary>The player-controlled cursor.</summary>
public sealed class Marker(Point position)
{
    public Point Position { get; set; } = position;

    public MarkerMode Mode { get; set; } = MarkerMode.OnBorder;

    /// <summary>Cells drawn since leaving the border; empty while <see cref="MarkerMode.OnBorder"/>.</summary>
    public List<Point> Trail { get; } = [];
}

/// <summary>Ties together the playfield, the marker, and the session phase. Driven one move at a time.</summary>
public sealed class Game
{
    public const int TargetPercent = 75;

    public Game(Playfield? playfield = null)
    {
        Playfield = playfield ?? new Playfield();
        Marker = new Marker(StartPosition(Playfield));
    }

    public Playfield Playfield { get; private set; }

    public Marker Marker { get; private set; }

    public bool DrawOn { get; private set; }

    public GamePhase Phase { get; private set; } = GamePhase.Playing;

    public void ToggleDraw()
    {
        if (Phase == GamePhase.Playing)
        {
            DrawOn = !DrawOn;
        }
    }

    /// <summary>Applies one move (data-model transition table).</summary>
    public void Move(Direction direction)
    {
        var target = Marker.Position + direction;
        if (Phase == GamePhase.Complete || !Playfield.Contains(target))
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
                Marker.Position = target;
                Playfield.SetTrail(target);
                Marker.Trail.Add(target);
                Marker.Mode = MarkerMode.Drawing;
                break;

            // Everything else is blocked: Empty with draw off, any move while drawing with
            // draw off, and Trail or Claimed targets.
        }
    }

    /// <summary>Starts the same empty playfield again.</summary>
    public void Restart()
    {
        Playfield = new Playfield(Playfield.Width, Playfield.Height);
        Marker = new Marker(StartPosition(Playfield));
        DrawOn = false;
        Phase = GamePhase.Playing;
    }

    private void CloseLine()
    {
        Playfield.CloseTrail();
        Marker.Trail.Clear();
        Marker.Mode = MarkerMode.OnBorder;
        DrawOn = false;
        if (Playfield.ClaimedPercent >= TargetPercent)
        {
            Phase = GamePhase.Complete;
        }
    }

    private static Point StartPosition(Playfield playfield) =>
        new(playfield.Width / 2, playfield.Height - 1);
}
