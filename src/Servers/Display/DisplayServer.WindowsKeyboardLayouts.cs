using Microsoft.Win32;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private void EnsureKeyboardLayouts()
    {
        EnsureOwner();
        if (!OperatingSystem.IsWindows() || _videoDriver != "windows")
            throw new NotSupportedException("Keyboard layout enumeration and activation require the Windows display backend.");
    }

    /// <summary>Gets the number of input layouts currently loaded for the Windows desktop.</summary>
    /// <exception cref="InvalidOperationException">The display is inactive, the caller is off the owner thread, or Windows could not enumerate layouts.</exception>
    /// <exception cref="NotSupportedException">The active backend does not enumerate input layouts.</exception>
    public static int KeyboardGetLayoutCount()
    {
        RequireService().EnsureKeyboardLayouts();
        return WindowsKeyboardLayouts.Count();
    }

    /// <summary>Gets the active input layout's index in the current native list, or -1 if it is absent.</summary>
    /// <remarks>Requery the list after activation or an operating-system layout change. Indices do not identify layouts across changes.</remarks>
    /// <exception cref="InvalidOperationException">The display is inactive, the caller is off the owner thread, or Windows could not enumerate layouts.</exception>
    /// <exception cref="NotSupportedException">The active backend does not enumerate input layouts.</exception>
    public static int KeyboardGetCurrentLayout()
    {
        RequireService().EnsureKeyboardLayouts();
        return WindowsKeyboardLayouts.Current();
    }

    /// <summary>Gets the input language of an indexed Windows layout, or an empty string when unknown.</summary>
    /// <param name="index">An index in the current native layout list.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the current layout list.</exception>
    /// <exception cref="InvalidOperationException">The display is inactive, the caller is off the owner thread, or Windows could not enumerate layouts.</exception>
    /// <exception cref="NotSupportedException">The active backend does not enumerate input layouts.</exception>
    public static string KeyboardGetLayoutLanguage(int index)
    {
        RequireService().EnsureKeyboardLayouts();
        return WindowsKeyboardLayouts.Language(index);
    }

    /// <summary>Gets the localized name of an indexed Windows layout, or an empty string when unknown.</summary>
    /// <param name="index">An index in the current native layout list.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the current layout list.</exception>
    /// <exception cref="InvalidOperationException">The display is inactive, the caller is off the owner thread, or Windows could not enumerate layouts.</exception>
    /// <exception cref="NotSupportedException">The active backend does not enumerate input layouts.</exception>
    public static string KeyboardGetLayoutName(int index)
    {
        RequireService().EnsureKeyboardLayouts();
        return OperatingSystem.IsWindows() ? WindowsKeyboardLayouts.Name(index) : throw new PlatformNotSupportedException();
    }

    /// <summary>Activates an already loaded Windows input layout for the application.</summary>
    /// <param name="index">An index in the current native layout list.</param>
    /// <remarks>The new layout is available to physical-key and label queries when the call returns. A held key retains its original logical identity and label until release.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the current layout list.</exception>
    /// <exception cref="InvalidOperationException">The display is inactive, the caller is off the owner thread, or native enumeration or activation failed.</exception>
    /// <exception cref="NotSupportedException">The active backend does not activate input layouts.</exception>
    public static void KeyboardSetCurrentLayout(int index)
    {
        var display = RequireService();
        display.EnsureKeyboardLayouts();
        WindowsKeyboardLayouts.Activate(index, display.WindowGetNativeHandleCore(HandleType.WindowHandle));
    }

    private static unsafe partial class WindowsKeyboardLayouts
    {
        private const string LayoutRegistryPath = @"SYSTEM\CurrentControlSet\Control\Keyboard Layouts";
        private const uint InputLanguageChanged = 0x0051, SetForProcess = 0x0100;
        private const uint LocalizedDisplayName = 0x0002;

        internal static int Count()
        {
            var count = GetKeyboardLayoutList(0, null);
            return count > 0 ? count : throw Failure("enumerate");
        }

        private static nint[] Snapshot()
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var layouts = new nint[Count()];
                int written;
                fixed (nint* buffer = layouts)
                    written = GetKeyboardLayoutList(layouts.Length, buffer);
                if (written == 0)
                    throw Failure("copy");
                if (written < layouts.Length)
                {
                    Array.Resize(ref layouts, written);
                    return layouts;
                }
                if (Count() == written)
                    return layouts;
            }
            throw new InvalidOperationException("The Windows input layout list changed repeatedly during enumeration.");
        }

        private static nint At(int index)
        {
            var layouts = Snapshot();
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, layouts.Length);
            return layouts[index];
        }

        internal static int Current()
        {
            var layouts = Snapshot();
            return Array.IndexOf(layouts, GetKeyboardLayout(0));
        }

        internal static string Language(int index)
        {
            const int LocaleNameLength = 85;
            var buffer = stackalloc char[LocaleNameLength];
            var length = LCIDToLocaleName((ushort)At(index), buffer, LocaleNameLength, 0);
            if (length <= 1)
                return "";
            var tag = new ReadOnlySpan<char>(buffer, length - 1);
            var separator = tag.IndexOf('-');
            return new string(separator < 0 ? tag : tag[..separator]);
        }

        [SupportedOSPlatform("windows")]
        internal static string Name(int index)
        {
            var layout = At(index);
            using var root = Registry.LocalMachine.OpenSubKey(LayoutRegistryPath);
            if (root != null)
            {
                var device = (ushort)((nuint)layout >> 16);
                var key = LayoutKeyName(root, device == 0 ? (ushort)layout : device);
                using var entry = key == null ? null : root.OpenSubKey(key);
                if (entry?.GetValue("Layout Display Name") is string { Length: > 0 } indirect)
                {
                    const int DisplayNameLength = 512;
                    var buffer = stackalloc char[DisplayNameLength];
                    if (SHLoadIndirectString(indirect, buffer, DisplayNameLength, 0) == 0 && buffer[0] != '\0')
                        return new string(buffer);
                }
                if (entry?.GetValue("Layout Text") is string { Length: > 0 } name)
                    return name;
            }
            var length = GetLocaleInfoW((ushort)layout, LocalizedDisplayName, null, 0);
            if (length <= 1)
                return "";
            var fallback = new char[length];
            fixed (char* buffer = fallback)
            {
                var copied = GetLocaleInfoW((ushort)layout, LocalizedDisplayName, buffer, length);
                return copied > 1 ? new string(buffer, 0, copied - 1) : "";
            }
        }

        [SupportedOSPlatform("windows")]
        private static string? LayoutKeyName(RegistryKey root, ushort device)
        {
            if ((device & 0xF000) != 0xF000)
                return device.ToString("x8", CultureInfo.InvariantCulture);
            foreach (var key in root.GetSubKeyNames())
            {
                using var entry = root.OpenSubKey(key);
                if (entry?.GetValue("Layout Id") is string id &&
                    ushort.TryParse(id, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) &&
                    value == (device & 0x0FFF))
                    return key;
            }
            return null;
        }

        internal static void Activate(int index, nint window)
        {
            var layout = At(index);
            if (window == 0)
                throw new InvalidOperationException("The Windows input layout requires a native main window.");
            if (GetKeyboardLayout(0) != layout && ActivateKeyboardLayout(layout, SetForProcess) == 0)
                throw Failure("activate");
            if (GetKeyboardLayout(0) != layout)
                throw new InvalidOperationException("Windows did not activate the requested input layout.");
            // Windows only notifies the focused or active window. SDL also needs the keymap for hidden and unfocused hosts.
            SendMessageW(window, InputLanguageChanged, 0, layout);
        }

        private static InvalidOperationException Failure(string operation) =>
            new($"Could not {operation} the Windows input layout (Win32 error {Marshal.GetLastPInvokeError()}).");

        [LibraryImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int GetKeyboardLayoutList(int count, nint* layouts);
        [LibraryImport("user32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial nint GetKeyboardLayout(uint threadId);
        [LibraryImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial nint ActivateKeyboardLayout(nint layout, uint flags);
        [LibraryImport("user32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial nint SendMessageW(nint window, uint message, nuint wParam, nint lParam);
        [LibraryImport("kernel32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int LCIDToLocaleName(uint locale, char* name, int count, uint flags);
        [LibraryImport("kernel32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int GetLocaleInfoW(uint locale, uint type, char* name, int count);
        [LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static partial int SHLoadIndirectString(string source, char* result, int count, nint reserved);
    }
}
