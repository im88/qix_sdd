using System.Text;

namespace Qix.Terminal;

/// <summary>
/// Frame buffer that writes only the cells that changed since the last frame, as VT escape
/// sequences, in a single write (research R3).
/// </summary>
internal sealed class Renderer
{
    private readonly StringBuilder _output = new();
    private (char Glyph, ConsoleColor? Color)[,] _frame = new (char, ConsoleColor?)[0, 0];
    private (char Glyph, ConsoleColor? Color)[,] _previous = new (char, ConsoleColor?)[0, 0];
    private bool _fullRedraw;

    private int Width => _frame.GetLength(0);

    private int Height => _frame.GetLength(1);

    /// <summary>Starts a new frame; reallocates the buffers if the window size changed.</summary>
    public void Clear()
    {
        if (Console.WindowWidth != Width || Console.WindowHeight != Height)
        {
            _frame = new (char, ConsoleColor?)[Console.WindowWidth, Console.WindowHeight];
            _previous = new (char, ConsoleColor?)[Console.WindowWidth, Console.WindowHeight];
            _fullRedraw = true;
        }

        for (var col = 0; col < Width; col++)
        {
            for (var row = 0; row < Height; row++)
            {
                _frame[col, row] = (' ', null);
            }
        }
    }

    /// <summary>Writes outside the window, or on its last row, are ignored.</summary>
    public void Put(int col, int row, char glyph, ConsoleColor? color)
    {
        if (col >= 0 && row >= 0 && col < Width && row < Height - 1)
        {
            _frame[col, row] = (glyph, color);
        }
    }

    public void Text(int col, int row, string text, ConsoleColor? color = null)
    {
        for (var i = 0; i < text.Length; i++)
        {
            Put(col + i, row, text[i], color);
        }
    }

    /// <summary>Forces a full redraw on the next <see cref="Present"/>.</summary>
    public void Invalidate() => _fullRedraw = true;

    public void Present()
    {
        _output.Clear();
        if (_fullRedraw)
        {
            _output.Append("\e[0m\e[2J");
        }

        ConsoleColor? currentColor = null;
        var colorSet = false;
        for (var row = 0; row < Height - 1; row++)
        {
            for (var col = 0; col < Width; col++)
            {
                var cell = _frame[col, row];
                if (!_fullRedraw && cell == _previous[col, row])
                {
                    continue;
                }

                _output.Append("\e[").Append(row + 1).Append(';').Append(col + 1).Append('H');
                if (!colorSet || cell.Color != currentColor)
                {
                    _output.Append("\e[").Append(SgrCode(cell.Color)).Append('m');
                    currentColor = cell.Color;
                    colorSet = true;
                }

                _output.Append(cell.Glyph);
                _previous[col, row] = cell;
            }
        }

        _fullRedraw = false;
        if (_output.Length > 0)
        {
            Console.Out.Write(_output.ToString());
            Console.Out.Flush();
        }
    }

    // ConsoleColor's numbering doesn't follow ANSI order, so the mapping is explicit.
    private static int SgrCode(ConsoleColor? color) => color switch
    {
        null => 39,
        ConsoleColor.White => 97,
        ConsoleColor.Yellow => 93,
        ConsoleColor.Red => 91,
        ConsoleColor.DarkCyan => 36,
        ConsoleColor.Magenta => 95,
        _ => throw new ArgumentOutOfRangeException(nameof(color)),
    };
}
