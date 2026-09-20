using System.Runtime.InteropServices;
using System.Text;

namespace GW2RTTFTimer;

internal static class Win32Interop
{
    public const int VkF8 = 0x77;
    public const int VkF11 = 0x7A;

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;

        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public int X;
        public int Y;
    }

    public readonly record struct ClientScreenRect(int Left, int Top, int Width, int Height);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowW(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public static ClientScreenRect? FindGw2ClientScreenRect()
    {
        IntPtr direct = FindWindowW(null, "Guild Wars 2");
        if (TryGetClientScreenRect(direct, out ClientScreenRect rect))
            return rect;

        ClientScreenRect? best = null;
        int bestArea = 0;
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || IsIconic(hwnd))
                return true;

            string title = GetWindowTitle(hwnd);
            if (!IsGw2TitleMatch(title))
                return true;

            if (TryGetClientScreenRect(hwnd, out ClientScreenRect candidate))
            {
                int area = candidate.Width * candidate.Height;
                if (area > bestArea)
                {
                    best = candidate;
                    bestArea = area;
                }
            }

            return true;
        }, IntPtr.Zero);

        return best;
    }

    private static bool TryGetClientScreenRect(IntPtr hwnd, out ClientScreenRect rect)
    {
        rect = default;
        if (hwnd == IntPtr.Zero || !GetClientRect(hwnd, out Rect client) || client.Width <= 0 || client.Height <= 0)
            return false;

        Point topLeft = new() { X = client.Left, Y = client.Top };
        if (!ClientToScreen(hwnd, ref topLeft))
            return false;

        rect = new ClientScreenRect(topLeft.X, topLeft.Y, client.Width, client.Height);
        return true;
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        StringBuilder sb = new(256);
        int length = GetWindowTextW(hwnd, sb, sb.Capacity);
        return length > 0 ? sb.ToString(0, length) : "";
    }

    private static bool IsGw2TitleMatch(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;

        if (title.Contains("RTTF", StringComparison.OrdinalIgnoreCase))
            return false;

        if (title.Equals("Guild Wars 2", StringComparison.Ordinal))
            return true;

        return title.Contains("Guild Wars 2", StringComparison.Ordinal)
            || title.StartsWith("gw2-64", StringComparison.OrdinalIgnoreCase);
    }
}
