namespace Qix.Core;

public enum QixStepOutcome
{
    /// <summary>The Qix moved one cell.</summary>
    Moved,

    /// <summary>The Qix touched the line (research R13); it stays where it is.</summary>
    Contact,

    /// <summary>Every move was blocked (a single-cell region).</summary>
    Stayed,
}

/// <summary>The enemy: one cell bouncing diagonally through the open area (research R12).</summary>
public sealed class Qix(Point position, Velocity velocity)
{
    public Point Position { get; set; } = position;

    public Velocity Velocity { get; set; } = velocity;

    /// <summary>Takes one step: the first candidate move that isn't blocked wins (research R12, R13).</summary>
    public QixStepOutcome Step(Playfield playfield)
    {
        var (dx, dy) = Velocity;
        var flipX = IsWall(playfield, new Point(Position.X + dx, Position.Y));
        var flipY = IsWall(playfield, new Point(Position.X, Position.Y + dy));
        var reflected = flipX || flipY
            ? new Velocity(flipX ? -dx : dx, flipY ? -dy : dy)
            : new Velocity(-dx, -dy);

        var diagonals = new List<Velocity> { Velocity, reflected };
        diagonals.AddRange(Velocity.Diagonals.Where(d => !diagonals.Contains(d)));
        foreach (var candidate in diagonals)
        {
            var outcome = TryMove(playfield, candidate.Dx, candidate.Dy);
            if (outcome is not null)
            {
                return outcome.Value;
            }
        }

        (int Dx, int Dy)[] straight = [(dx, 0), (-dx, 0), (0, dy), (0, -dy)];
        foreach (var (sx, sy) in straight)
        {
            var outcome = TryMove(playfield, sx, sy);
            if (outcome is not null)
            {
                return outcome.Value;
            }
        }

        return QixStepOutcome.Stayed;
    }

    /// <summary>Returns null if the move is blocked; applies it if the outcome is <see cref="QixStepOutcome.Moved"/>.</summary>
    private QixStepOutcome? TryMove(Playfield playfield, int dx, int dy)
    {
        var diagonal = dx != 0 && dy != 0;
        if (diagonal)
        {
            // No corner cutting: squeezing between two non-Empty cells is blocked, or a contact
            // if one of them is the line.
            var horizontal = CellAt(playfield, new Point(Position.X + dx, Position.Y));
            var vertical = CellAt(playfield, new Point(Position.X, Position.Y + dy));
            if (horizontal != Cell.Empty && vertical != Cell.Empty)
            {
                return horizontal == Cell.Trail || vertical == Cell.Trail ? QixStepOutcome.Contact : null;
            }
        }

        var target = new Point(Position.X + dx, Position.Y + dy);
        switch (CellAt(playfield, target))
        {
            case Cell.Trail:
                return QixStepOutcome.Contact;
            case Cell.Empty:
                Position = target;
                Velocity = new Velocity(dx != 0 ? dx : Velocity.Dx, dy != 0 ? dy : Velocity.Dy);
                return QixStepOutcome.Moved;
            default:
                return null;
        }
    }

    /// <summary>Outside the playfield counts as <see cref="Cell.Border"/>.</summary>
    private static Cell CellAt(Playfield playfield, Point p) => playfield.Contains(p) ? playfield[p] : Cell.Border;

    private static bool IsWall(Playfield playfield, Point p) => CellAt(playfield, p) is Cell.Border or Cell.Claimed;
}
