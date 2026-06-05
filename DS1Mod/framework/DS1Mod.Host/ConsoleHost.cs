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

    public static void Initialize(string title)
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
            if (conOut is not null)
            {
                var writer = new StreamWriter(conOut) { AutoFlush = true };
                Console.SetOut(writer);
                Console.SetError(writer);
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
