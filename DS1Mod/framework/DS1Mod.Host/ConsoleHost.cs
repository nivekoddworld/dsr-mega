using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace DS1Mod.Host;

/// <summary>
/// Allocates a console for the injected, otherwise-GUI game process and routes
/// <see cref="Console"/> output (from the host and from every mod) into it.
///
/// DarkSoulsRemastered.exe is a windowed application with no console, so any
/// Console.Write call normally goes to an invalid std handle and is lost. We
/// AllocConsole(), re-open CONOUT$, and repoint both the OS std handles and the
/// managed Console writers at it so all logging becomes visible.
/// </summary>
internal static class ConsoleHost
{
    private const int  STD_OUTPUT_HANDLE = -11;
    private const int  STD_ERROR_HANDLE  = -12;
    private const uint GENERIC_WRITE      = 0x40000000;
    private const uint GENERIC_READ       = 0x80000000;
    private const uint FILE_SHARE_READ     = 0x1;
    private const uint FILE_SHARE_WRITE    = 0x2;
    private const uint OPEN_EXISTING       = 3;
    private const uint SC_CLOSE            = 0xF060;
    private const uint MF_BYCOMMAND        = 0x0;

    private static bool _initialized;

    /// <param name="logFilePath">
    /// If provided, every line written to the console (by the host or any mod)
    /// is also appended to this file. The console window disappears when the
    /// game closes, so this is the only durable record of what mods logged —
    /// useful for tooling that needs to verify a run after the fact.
    /// </param>
    public static void Initialize(string title, string? logFilePath = null)
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            // Give the GUI process a console if it doesn't already have one.
            if (GetConsoleWindow() == nint.Zero)
                AllocConsole();

            // The std handles inherited by an injected process are usually
            // invalid, so Console.Out silently no-ops until we re-bind it to a
            // freshly opened CONOUT$.
            FileStream? conOut = OpenConsoleStream();
            TextWriter? consoleWriter = conOut is not null
                ? new StreamWriter(conOut) { AutoFlush = true }
                : null;

            TextWriter? fileWriter = null;
            if (logFilePath is not null)
            {
                try
                {
                    var fs = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    fileWriter = new StreamWriter(fs) { AutoFlush = true };
                }
                catch { /* file logging is best-effort */ }
            }

            TextWriter? combined = (consoleWriter, fileWriter) switch
            {
                (not null, not null) => new TeeTextWriter(consoleWriter, fileWriter),
                (not null, null)     => consoleWriter,
                (null, not null)     => fileWriter,
                _                    => null,
            };

            if (combined is not null)
            {
                Console.SetOut(combined);
                Console.SetError(combined);
            }

            try { Console.Title = title; } catch { /* title is cosmetic */ }

            // Closing the console window sends CTRL_CLOSE_EVENT, which would
            // terminate the host process — i.e. kill the game. Remove the close
            // button so a stray click can't take the session down; quit from the
            // game itself instead.
            nint hwnd = GetConsoleWindow();
            if (hwnd != nint.Zero)
            {
                nint menu = GetSystemMenu(hwnd, false);
                if (menu != nint.Zero)
                    DeleteMenu(menu, SC_CLOSE, MF_BYCOMMAND);
            }

            Console.WriteLine($"=== {title} ===");
        }
        catch
        {
            // Logging must never be able to take down the game. If anything here
            // fails we simply run without a visible console.
        }
    }

    private static FileStream? OpenConsoleStream()
    {
        nint handle = CreateFileW("CONOUT$",
            GENERIC_WRITE | GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            nint.Zero, OPEN_EXISTING, 0, nint.Zero);

        if (handle == nint.Zero || handle == new nint(-1))
            return null;

        SetStdHandle(STD_OUTPUT_HANDLE, handle);
        SetStdHandle(STD_ERROR_HANDLE,  handle);

        var safe = new SafeFileHandle(handle, ownsHandle: true);
        return new FileStream(safe, FileAccess.Write);
    }

    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AllocConsole();
    [DllImport("kernel32.dll")]                      private static extern nint GetConsoleWindow();
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetStdHandle(int nStdHandle, nint hHandle);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint CreateFileW(string name, uint access, uint share, nint sec, uint disp, uint flags, nint template);

    [DllImport("user32.dll")] private static extern nint GetSystemMenu(nint hWnd, bool revert);
    [DllImport("user32.dll")] private static extern bool DeleteMenu(nint hMenu, uint position, uint flags);
}

/// <summary>Forwards every write to two underlying writers — used to mirror console output to a log file.</summary>
internal sealed class TeeTextWriter(TextWriter a, TextWriter b) : TextWriter
{
    public override System.Text.Encoding Encoding => a.Encoding;

    public override void Write(char value)       { a.Write(value);  b.Write(value); }
    public override void Write(string? value)    { a.Write(value);  b.Write(value); }
    public override void WriteLine(string? value){ a.WriteLine(value); b.WriteLine(value); }
    public override void Flush()                 { a.Flush();       b.Flush(); }
}
