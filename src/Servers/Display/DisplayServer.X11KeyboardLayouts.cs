using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private static unsafe partial class X11KeyboardLayouts
    {
        private const uint CoreKeyboard = 0x100, AllControls = 0xf8001fff;
        private const uint SymbolsName = 1 << 2, GroupNames = 1 << 12;
        private const int MaxGroups = 4;
        private const int GroupNamesOffset = 5 + 16 + 32;

        // XkbDescRec and XkbStateRec use Xlib's C ABI. Only the required descriptor prefix is read.
        [StructLayout(LayoutKind.Sequential)]
        private struct Description
        {
            internal nint Display;
            internal ushort Flags;
            internal ushort Device;
            internal byte MinKeycode, MaxKeycode;
            internal byte* Controls;
            internal nint Server, Map, Indicators;
            internal nuint* Names;
        }

        [StructLayout(LayoutKind.Explicit, Size = 18)]
        private struct State
        {
            [FieldOffset(0)] internal byte Group;
            [FieldOffset(1)] internal byte LockedGroup;
        }

        internal static int Count(nint display)
        {
            var keyboard = Describe(display);
            try { return GroupCount(keyboard); }
            finally { XkbFreeKeyboard(keyboard, 0, 1); }
        }

        internal static int Current(nint display) => ReadState(display).Group;

        internal static string Language(nint display, int index)
        {
            var symbols = ReadName(display, index, language: true);
            if (symbols.Length == 0)
                return "";
            var parts = symbols.Split('+');
            var primary = "";
            foreach (var part in parts)
            {
                var token = part.AsSpan();
                var qualifier = token.LastIndexOf(':');
                if (qualifier >= 0)
                {
                    if (int.TryParse(token[(qualifier + 1)..], out var group) && group == index + 1)
                        return LayoutPrefix(token[..qualifier]);
                    continue;
                }
                if (primary.Length == 0 && !token.SequenceEqual("pc") &&
                    !token.StartsWith("inet(") && !token.StartsWith("group("))
                    primary = LayoutPrefix(token);
            }
            return index == 0 && primary.Length != 0 ? primary : "en";
        }

        private static string LayoutPrefix(ReadOnlySpan<char> token)
        {
            var variant = token.IndexOf('(');
            if (variant >= 0)
                token = token[..variant];
            return new string(token[..Math.Min(token.Length, 2)]);
        }

        internal static string Name(nint display, int index) => ReadName(display, index, language: false);

        private static string ReadName(nint display, int index, bool language)
        {
            var keyboard = Describe(display);
            try
            {
                ValidateIndex(index, GroupCount(keyboard));
                if (XkbGetNames(display, SymbolsName | GroupNames, keyboard) != 0 || keyboard->Names == null)
                    return "";
                // XkbNamesRec has five header atoms, 16 virtual modifiers and 32 indicators before the groups.
                var atom = keyboard->Names[language ? 2 : GroupNamesOffset + index];
                if (atom == 0)
                    return "";
                var name = XGetAtomName(display, atom);
                try { return Marshal.PtrToStringUTF8(name) ?? ""; }
                finally { if (name != 0) XFree(name); }
            }
            finally { XkbFreeKeyboard(keyboard, 0, 1); }
        }

        internal static void Activate(nint display, int index)
        {
            ValidateIndex(index, Count(display));
            if (XkbLockGroup(display, CoreKeyboard, (uint)index) == 0 || ReadState(display).LockedGroup != index)
                throw new InvalidOperationException("The X server did not lock the requested keyboard group.");
        }

        private static void ValidateIndex(int index, int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);
        }

        private static Description* Describe(nint display)
        {
            EnsureConnection(display);
            var keyboard = XkbAllocKeyboard();
            if (keyboard == null)
                throw new InvalidOperationException("Could not allocate the XKB keyboard description.");
            keyboard->Display = display;
            keyboard->Device = (ushort)CoreKeyboard;
            if (XkbGetControls(display, AllControls, keyboard) == 0)
                return keyboard;
            XkbFreeKeyboard(keyboard, 0, 1);
            throw new NotSupportedException("The X server did not expose XKB keyboard controls.");
        }

        private static int GroupCount(Description* keyboard)
        {
            // num_groups follows mk_dflt_btn in XkbControlsRec.
            var count = keyboard->Controls == null ? 0 : keyboard->Controls[1];
            return count is >= 1 and <= MaxGroups ? count :
                throw new InvalidOperationException("The X server returned an invalid keyboard group count.");
        }

        private static State ReadState(nint display)
        {
            EnsureConnection(display);
            return XkbGetState(display, CoreKeyboard, out var state) == 0 ? state :
                throw new NotSupportedException("The X server did not expose XKB keyboard state.");
        }

        private static void EnsureConnection(nint display)
        {
            if (display == 0)
                throw new InvalidOperationException("A native X11 display connection is required.");
        }

        [LibraryImport("libX11.so.6")] private static partial Description* XkbAllocKeyboard();
        [LibraryImport("libX11.so.6")] private static partial int XkbGetControls(nint display, uint which, Description* keyboard);
        [LibraryImport("libX11.so.6")] private static partial int XkbGetNames(nint display, uint which, Description* keyboard);
        [LibraryImport("libX11.so.6")] private static partial void XkbFreeKeyboard(Description* keyboard, uint which, int freeAll);
        [LibraryImport("libX11.so.6")] private static partial int XkbGetState(nint display, uint device, out State state);
        [LibraryImport("libX11.so.6")] private static partial int XkbLockGroup(nint display, uint device, uint group);
        [LibraryImport("libX11.so.6")] private static partial nint XGetAtomName(nint display, nuint atom);
        [LibraryImport("libX11.so.6")] private static partial int XFree(nint data);
    }
}
