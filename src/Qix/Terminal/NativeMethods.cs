using System.Runtime.InteropServices;

namespace Qix.Terminal;

/// <summary>Win32 console mode API, used only to enable VT output (research R3).</summary>
internal static partial class NativeMethods
{
    public const int STD_OUTPUT_HANDLE = -11;

    public const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}
