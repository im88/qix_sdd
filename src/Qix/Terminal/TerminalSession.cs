using System.Text;
using static Qix.Terminal.NativeMethods;

namespace Qix.Terminal;

/// <summary>
/// Puts the console into game mode (VT output, alternate screen, hidden cursor, Ctrl+C as a key)
/// and restores it on <see cref="Dispose"/> (research R2, R3; FR-016).
/// </summary>
internal sealed class TerminalSession : IDisposable
{
    private readonly IntPtr _outputHandle;
    private readonly bool _hasOriginalMode;
    private readonly uint _originalOutputMode;
    private bool _disposed;

    public TerminalSession()
    {
        // Failure is fine: some hosts already interpret VT sequences without the flag.
        _outputHandle = GetStdHandle(STD_OUTPUT_HANDLE);
        _hasOriginalMode = GetConsoleMode(_outputHandle, out _originalOutputMode);
        if (_hasOriginalMode)
        {
            SetConsoleMode(_outputHandle, _originalOutputMode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }

        Console.OutputEncoding = Encoding.UTF8;
        Console.TreatControlCAsInput = true;
        Console.Out.Write("\e[?1049h\e[?25l");
        Console.Out.Flush();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Console.Out.Write("\e[0m\e[?25h\e[?1049l");
        Console.Out.Flush();
        Console.TreatControlCAsInput = false;
        if (_hasOriginalMode)
        {
            SetConsoleMode(_outputHandle, _originalOutputMode);
        }
    }
}
