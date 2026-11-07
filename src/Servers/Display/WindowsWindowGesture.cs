using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Electron2D;

internal static partial class WindowsWindowGesture
{
    private const int Size = 0xF000, Move = 0xF010, Caption = 2;
    private const uint SystemCommand = 0x0112;
    private const int SwapButton = 23, LeftButton = 1, RightButton = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        internal int X, Y;
    }

    internal static bool TryPrepare(nint window, DisplayServer.WindowResizeEdge? edge, Vector2 clientPosition, out nuint command, out nint position)
    {
        command = 0;
        position = 0;
        if (window == 0)
            throw new NotSupportedException("The window has no Win32 identity.");
        var capture = GetCapture();
        if (capture != 0 && capture != window)
            return false;

        // Read the owner queue's logical press after SDL has delivered its pending messages.
        var swapped = GetSystemMetrics(SwapButton) != 0;
        if (GetKeyState(swapped ? RightButton : LeftButton) >= 0)
            return false;

        if (GetCursorPos(out var pointer) == 0)
        {
            // Non-input desktops can still route window messages but cannot read the global cursor.
            pointer = new Point { X = (int)clientPosition.X, Y = (int)clientPosition.Y };
            if (ClientToScreen(window, ref pointer) == 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Cannot locate the pointer for a window gesture.");
        }
        command = edge switch
        {
            DisplayServer.WindowResizeEdge.Left => Size | 1,
            DisplayServer.WindowResizeEdge.Right => Size | 2,
            DisplayServer.WindowResizeEdge.Top => Size | 3,
            DisplayServer.WindowResizeEdge.TopLeft => Size | 4,
            DisplayServer.WindowResizeEdge.TopRight => Size | 5,
            DisplayServer.WindowResizeEdge.Bottom => Size | 6,
            DisplayServer.WindowResizeEdge.BottomLeft => Size | 7,
            DisplayServer.WindowResizeEdge.BottomRight => Size | 8,
            _ => Move | Caption,
        };
        position = (nint)unchecked((int)((uint)(ushort)pointer.X | ((uint)(ushort)pointer.Y << 16)));
        return true;
    }

    internal static void Run(nint window, nuint command, nint position)
    {
        if (GetCapture() == window && ReleaseCapture() == 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Cannot release pointer capture for a window gesture.");
        SendMessage(window, SystemCommand, command, position);
    }

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int index);
    [LibraryImport("user32.dll")]
    private static partial short GetKeyState(int key);
    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int GetCursorPos(out Point position);
    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int ClientToScreen(nint window, ref Point position);
    [LibraryImport("user32.dll")]
    private static partial nint GetCapture();
    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int ReleaseCapture();
    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial nint SendMessage(nint window, uint message, nuint wParam, nint lParam);
}
