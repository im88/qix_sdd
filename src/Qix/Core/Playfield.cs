namespace Qix.Core;

/// <summary>The fixed grid of cells. The outer ring is the frame.</summary>
public sealed class Playfield
{
    public const int DefaultWidth = 78;
    public const int DefaultHeight = 22;

    private readonly Cell[,] _cells;

    public Playfield(int width = DefaultWidth, int height = DefaultHeight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 3);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 3);

        Width = width;
        Height = height;
        _cells = new Cell[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var onFrame = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                _cells[x, y] = onFrame ? Cell.Border : Cell.Empty;
            }
        }
    }

    public int Width { get; }

    public int Height { get; }

    public int InteriorCount => (Width - 2) * (Height - 2);

    /// <summary>
    /// Share of interior cells that are not Empty, rounded down (research R6). Updated only by
    /// <see cref="CloseTrail"/>, so a line being drawn doesn't change it.
    /// </summary>
    public int ClaimedPercent { get; private set; }

    /// <summary>Callers check <see cref="Contains"/> first.</summary>
    public Cell this[Point p] => _cells[p.X, p.Y];

    public bool Contains(Point p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

    /// <summary>Marks an <see cref="Cell.Empty"/> cell as part of the line being drawn.</summary>
    public void SetTrail(Point p)
    {
        if (_cells[p.X, p.Y] != Cell.Empty)
        {
            throw new InvalidOperationException($"Cell ({p.X}, {p.Y}) is {_cells[p.X, p.Y]}, not Empty.");
        }

        _cells[p.X, p.Y] = Cell.Trail;
    }

    /// <summary>
    /// Completes the line: it becomes border, every Empty region except the one containing
    /// <paramref name="keep"/> (the Qix's cell) is claimed, and border cells that no longer touch
    /// open area are demoted (research R5, R15).
    /// </summary>
    /// <exception cref="InvalidOperationException"><paramref name="keep"/> is not an Empty cell.</exception>
    public void CloseTrail(Point keep)
    {
        Replace(Cell.Trail, Cell.Border);
        ClaimAllExceptRegionOf(keep);
        DemoteEnclosedBorder();
        ClaimedPercent = 100 * CountNonEmptyInterior() / InteriorCount;
    }

    /// <summary>Erases the line being drawn after a lost life; <see cref="ClaimedPercent"/> is unaffected.</summary>
    public void ClearTrail() => Replace(Cell.Trail, Cell.Empty);

    private int CountNonEmptyInterior()
    {
        var count = 0;
        for (var x = 1; x < Width - 1; x++)
        {
            for (var y = 1; y < Height - 1; y++)
            {
                if (_cells[x, y] != Cell.Empty)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void Replace(Cell from, Cell to)
    {
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                if (_cells[x, y] == from)
                {
                    _cells[x, y] = to;
                }
            }
        }
    }

    /// <summary>Claims every Empty cell outside the region containing <paramref name="keep"/> (research R15).</summary>
    private void ClaimAllExceptRegionOf(Point keep)
    {
        if (!Contains(keep) || _cells[keep.X, keep.Y] != Cell.Empty)
        {
            throw new InvalidOperationException($"Keep point ({keep.X}, {keep.Y}) is not an Empty cell.");
        }

        var kept = FloodFill(keep);
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                if (_cells[x, y] == Cell.Empty && !kept[x, y])
                {
                    _cells[x, y] = Cell.Claimed;
                }
            }
        }
    }

    /// <summary>Marks the 4-connected Empty region containing <paramref name="start"/>.</summary>
    private bool[,] FloodFill(Point start)
    {
        var inRegion = new bool[Width, Height];
        var queue = new Queue<Point>();
        inRegion[start.X, start.Y] = true;
        queue.Enqueue(start);
        while (queue.TryDequeue(out var p))
        {
            foreach (var d in Enum.GetValues<Direction>())
            {
                var n = p + d;
                if (Contains(n) && _cells[n.X, n.Y] == Cell.Empty && !inRegion[n.X, n.Y])
                {
                    inRegion[n.X, n.Y] = true;
                    queue.Enqueue(n);
                }
            }
        }

        return inRegion;
    }

    private void DemoteEnclosedBorder()
    {
        // Collect first, then apply, so the result doesn't depend on scan order.
        var demote = new List<Point>();
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                if (_cells[x, y] == Cell.Border && !TouchesEmpty(x, y))
                {
                    demote.Add(new Point(x, y));
                }
            }
        }

        foreach (var p in demote)
        {
            _cells[p.X, p.Y] = Cell.Claimed;
        }
    }

    /// <summary>True if any of the 8 neighbours is Empty.</summary>
    private bool TouchesEmpty(int x, int y)
    {
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                var n = new Point(x + dx, y + dy);
                if (Contains(n) && _cells[n.X, n.Y] == Cell.Empty)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
