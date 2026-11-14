using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private byte[]? _defaultNativeIcon;
    private (nint Small, nint Large) _nativeIcon;

    private void SetNativeIconCore(string filename)
    {
        EnsureOwner();
        ArgumentException.ThrowIfNullOrEmpty(filename);
        if (!HasFeatureCore(Feature.NativeIcon))
            throw new NotSupportedException("This display has no native icon file support.");
        var data = FileAccess.GetFileAsBytes(filename);
        ValidateICO(data);
        var prepared = new List<(DisplayServer Window, nint Handle, (nint Small, nint Large) Icons)>();
        try
        {
            Prepare(this);
            foreach (var child in _childWindows.Values) Prepare(child);
            if (prepared.Count == 0) DestroyIcons(PrepareWindowsIcon(data));
        }
        catch
        {
            foreach (var item in prepared) DestroyIcons(item.Icons);
            throw;
        }
        foreach (var item in prepared) item.Window.SetWindowsIcon(item.Handle, item.Icons);
        _defaultNativeIcon = data;
        _defaultWindowIcon?.Dispose();
        _defaultWindowIcon = null;

        void Prepare(DisplayServer window)
        {
            if (window._windowIconOverridden) return;
            var handle = window.WindowGetNativeHandleCore(HandleType.WindowHandle);
            prepared.EnsureCapacity(prepared.Count + 1);
            prepared.Add((window, handle, window.PrepareWindowsIcon(data)));
        }
    }

    private static void ValidateICO(ReadOnlySpan<byte> data)
    {
        if (data.Length < 6 || BinaryPrimitives.ReadUInt32LittleEndian(data) != 0x00010000)
            throw new InvalidDataException("The file does not contain an ICO directory.");
        var count = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
        var directoryEnd = 6 + count * 16;
        if (count == 0 || directoryEnd > data.Length)
            throw new InvalidDataException("The ICO directory is empty or truncated.");
        for (var position = 6; position < directoryEnd; position += 16)
        {
            var entry = data.Slice(position, 16);
            var length = BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]);
            var offset = BinaryPrimitives.ReadUInt32LittleEndian(entry[12..]);
            if (length == 0 || offset < directoryEnd || (ulong)offset + length > (ulong)data.Length)
                throw new InvalidDataException("An ICO image extends outside the file.");
        }
    }

    private (nint Small, nint Large) PrepareWindowsIcon(ReadOnlySpan<byte> data)
    {
        var dpi = GetDpiForWindow(WindowGetNativeHandleCore(HandleType.WindowHandle));
        if (dpi == 0) throw new InvalidOperationException("Windows could not read the native window DPI.");
        var small = CreateWindowsIcon(data, GetSystemMetricsForDpi(49, dpi), GetSystemMetricsForDpi(50, dpi));
        try { return (small, CreateWindowsIcon(data, GetSystemMetricsForDpi(11, dpi), GetSystemMetricsForDpi(12, dpi))); }
        catch { StatusWin.DestroyIcon(small); throw; }
    }

    private static unsafe nint CreateWindowsIcon(ReadOnlySpan<byte> data, int width, int height)
    {
        var count = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
        var bestDistance = int.MaxValue;
        var bestDepth = -1;
        ReadOnlySpan<byte> image = default;
        for (var i = 0; i < count; i++)
        {
            var entry = data.Slice(6 + i * 16, 16);
            var w = entry[0] == 0 ? 256 : entry[0];
            var h = entry[1] == 0 ? 256 : entry[1];
            var distance = Math.Abs(w - width) + Math.Abs(h - height);
            var depth = BinaryPrimitives.ReadUInt16LittleEndian(entry[6..]);
            if (distance > bestDistance || distance == bestDistance && depth <= bestDepth) continue;
            bestDistance = distance;
            bestDepth = depth;
            image = data.Slice((int)BinaryPrimitives.ReadUInt32LittleEndian(entry[12..]),
                (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]));
        }
        // ICO image offsets need not be DWORD-aligned. The native decoder requires aligned input.
        fixed (byte* bytes = image.ToArray())
        {
            var icon = CreateIconFromResourceEx(bytes, (uint)image.Length, 1, 0x00030000, width, height, 0);
            if (icon == 0) throw new InvalidDataException("Windows could not decode an ICO image.");
            return icon;
        }
    }

    private void SetWindowsIcon(nint handle, (nint Small, nint Large) icons)
    {
        PublishWindowsIcon(handle, icons);
        ReleaseNativeWindowIcon();
        _nativeIcon = icons;
    }

    private static void PublishWindowsIcon(nint handle, (nint Small, nint Large) icons)
    {
        const uint setIcon = 0x0080;
        SendMessageW(handle, setIcon, 0, icons.Small);
        SendMessageW(handle, setIcon, 1, icons.Large);
    }

    private void ReleaseNativeWindowIcon()
    {
        DestroyIcons(_nativeIcon);
        _nativeIcon = default;
    }

    private static void DestroyIcons((nint Small, nint Large) icons)
    {
        if (icons.Small != 0) StatusWin.DestroyIcon(icons.Small);
        if (icons.Large != 0) StatusWin.DestroyIcon(icons.Large);
    }

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint GetDpiForWindow(nint window);
    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int GetSystemMetricsForDpi(int index, uint dpi);
    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static unsafe partial nint CreateIconFromResourceEx(byte* bits, uint size, int isIcon, uint version, int width, int height, uint flags);
    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint SendMessageW(nint window, uint message, nuint wParam, nint lParam);
}
