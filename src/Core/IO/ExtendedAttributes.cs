using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Electron2D;

internal static class ExtendedAttributes
{
    internal static byte[] Get(string path, string name)
    {
        Validate(path, name);
        if (OperatingSystem.IsLinux())
            return LinuxExtendedAttributes.Get(path, name);
        if (OperatingSystem.IsMacOS())
            return MacExtendedAttributes.Get(path, name);
        if (OperatingSystem.IsWindows())
            return WindowsExtendedAttributes.Get(path, name);
        throw new PlatformNotSupportedException("Extended attributes are supported only on Linux, macOS, and Windows.");
    }

    internal static string[] List(string path)
    {
        ValidatePath(path);
        if (OperatingSystem.IsLinux())
            return LinuxExtendedAttributes.List(path);
        if (OperatingSystem.IsMacOS())
            return MacExtendedAttributes.List(path);
        if (OperatingSystem.IsWindows())
            return WindowsExtendedAttributes.List(path);
        throw new PlatformNotSupportedException("Extended attributes are supported only on Linux, macOS, and Windows.");
    }

    internal static void Remove(string path, string name)
    {
        Validate(path, name);
        if (OperatingSystem.IsLinux())
            LinuxExtendedAttributes.Remove(path, name);
        else if (OperatingSystem.IsMacOS())
            MacExtendedAttributes.Remove(path, name);
        else if (OperatingSystem.IsWindows())
            WindowsExtendedAttributes.Remove(path, name);
        else
            throw new PlatformNotSupportedException("Extended attributes are supported only on Linux, macOS, and Windows.");
    }

    internal static void Set(string path, string name, ReadOnlySpan<byte> value)
    {
        Validate(path, name);
        if (OperatingSystem.IsLinux())
            LinuxExtendedAttributes.Set(path, name, value);
        else if (OperatingSystem.IsMacOS())
            MacExtendedAttributes.Set(path, name, value);
        else if (OperatingSystem.IsWindows())
            WindowsExtendedAttributes.Set(path, name, value);
        else
            throw new PlatformNotSupportedException("Extended attributes are supported only on Linux, macOS, and Windows.");
    }

    internal static string[] DecodeNullSeparatedNames(ReadOnlySpan<byte> bytes)
    {
        var names = new List<string>();
        var start = 0;
        for (var index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] != 0)
                continue;
            names.Add(new UTF8Encoding(false, true).GetString(bytes[start..index]));
            start = index + 1;
        }
        if (start != bytes.Length)
            throw new InvalidDataException("The operating system returned a malformed extended-attribute list.");
        return names.ToArray();
    }

    internal static void ThrowNativeError(string operation)
    {
        var error = Marshal.GetLastPInvokeError();
        throw new IOException($"The operating system could not {operation}.", new Win32Exception(error));
    }

    private static void Validate(string path, string name)
    {
        ValidatePath(path);
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0 || name.Contains('\0') || name.Contains('/') || name.Contains('\\') || name.Contains(':'))
            throw new ArgumentException("An extended-attribute name contains an unsupported character.", nameof(name));
    }

    private static void ValidatePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("The file does not exist.", path);
    }
}

internal static class LinuxExtendedAttributes
{
    private const int RangeError = 34;
    private const string NamespacePrefix = "user.";

    internal static byte[] Get(string path, string name)
    {
        var nativeName = NamespacePrefix + name;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var length = GetXAttr(path, nativeName, null, 0);
            if (length < 0)
                ExtendedAttributes.ThrowNativeError("read the extended attribute");
            var value = new byte[checked((int)length)];
            if (length == 0)
                return value;
            var actual = GetXAttr(path, nativeName, value, (nuint)value.Length);
            if (actual >= 0)
                return actual == value.Length ? value : value.AsSpan(0, checked((int)actual)).ToArray();
            if (Marshal.GetLastPInvokeError() != RangeError)
                ExtendedAttributes.ThrowNativeError("read the extended attribute");
        }
        throw new IOException("The extended attribute changed repeatedly while it was being read.");
    }

    internal static string[] List(string path)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var length = ListXAttr(path, null, 0);
            if (length < 0)
                ExtendedAttributes.ThrowNativeError("list extended attributes");
            if (length == 0)
                return [];
            var buffer = new byte[checked((int)length)];
            var actual = ListXAttr(path, buffer, (nuint)buffer.Length);
            if (actual >= 0)
            {
                return ExtendedAttributes.DecodeNullSeparatedNames(buffer.AsSpan(0, checked((int)actual)))
                    .Where(name => name.StartsWith(NamespacePrefix, StringComparison.Ordinal))
                    .Select(name => name[NamespacePrefix.Length..])
                    .ToArray();
            }
            if (Marshal.GetLastPInvokeError() != RangeError)
                ExtendedAttributes.ThrowNativeError("list extended attributes");
        }
        throw new IOException("The extended-attribute list changed repeatedly while it was being read.");
    }

    internal static void Remove(string path, string name)
    {
        if (RemoveXAttr(path, NamespacePrefix + name) != 0)
            ExtendedAttributes.ThrowNativeError("remove the extended attribute");
    }

    internal static void Set(string path, string name, ReadOnlySpan<byte> value)
    {
        var bytes = value.ToArray();
        if (SetXAttr(path, NamespacePrefix + name, bytes, (nuint)bytes.Length, 0) != 0)
            ExtendedAttributes.ThrowNativeError("write the extended attribute");
    }

    [DllImport("libc", EntryPoint = "getxattr", SetLastError = true)]
    private static extern nint GetXAttr(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        byte[]? value,
        nuint size);

    [DllImport("libc", EntryPoint = "listxattr", SetLastError = true)]
    private static extern nint ListXAttr(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        byte[]? list,
        nuint size);

    [DllImport("libc", EntryPoint = "removexattr", SetLastError = true)]
    private static extern int RemoveXAttr(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport("libc", EntryPoint = "setxattr", SetLastError = true)]
    private static extern int SetXAttr(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        byte[] value,
        nuint size,
        int flags);
}

internal static class MacExtendedAttributes
{
    private const int RangeError = 34;

    internal static byte[] Get(string path, string name)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var length = GetXAttr(path, name, null, 0, 0, 0);
            if (length < 0)
                ExtendedAttributes.ThrowNativeError("read the extended attribute");
            var value = new byte[checked((int)length)];
            if (length == 0)
                return value;
            var actual = GetXAttr(path, name, value, (nuint)value.Length, 0, 0);
            if (actual >= 0)
                return actual == value.Length ? value : value.AsSpan(0, checked((int)actual)).ToArray();
            if (Marshal.GetLastPInvokeError() != RangeError)
                ExtendedAttributes.ThrowNativeError("read the extended attribute");
        }
        throw new IOException("The extended attribute changed repeatedly while it was being read.");
    }

    internal static string[] List(string path)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var length = ListXAttr(path, null, 0, 0);
            if (length < 0)
                ExtendedAttributes.ThrowNativeError("list extended attributes");
            if (length == 0)
                return [];
            var buffer = new byte[checked((int)length)];
            var actual = ListXAttr(path, buffer, (nuint)buffer.Length, 0);
            if (actual >= 0)
                return ExtendedAttributes.DecodeNullSeparatedNames(buffer.AsSpan(0, checked((int)actual)));
            if (Marshal.GetLastPInvokeError() != RangeError)
                ExtendedAttributes.ThrowNativeError("list extended attributes");
        }
        throw new IOException("The extended-attribute list changed repeatedly while it was being read.");
    }

    internal static void Remove(string path, string name)
    {
        if (RemoveXAttr(path, name, 0) != 0)
            ExtendedAttributes.ThrowNativeError("remove the extended attribute");
    }

    internal static void Set(string path, string name, ReadOnlySpan<byte> value)
    {
        var bytes = value.ToArray();
        if (SetXAttr(path, name, bytes, (nuint)bytes.Length, 0, 0) != 0)
            ExtendedAttributes.ThrowNativeError("write the extended attribute");
    }

    [DllImport("libSystem.B.dylib", EntryPoint = "getxattr", SetLastError = true)]
    private static extern nint GetXAttr(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        byte[]? value,
        nuint size,
        uint position,
        int options);

    [DllImport("libSystem.B.dylib", EntryPoint = "listxattr", SetLastError = true)]
    private static extern nint ListXAttr(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        byte[]? list,
        nuint size,
        int options);

    [DllImport("libSystem.B.dylib", EntryPoint = "removexattr", SetLastError = true)]
    private static extern int RemoveXAttr(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        int options);

    [DllImport("libSystem.B.dylib", EntryPoint = "setxattr", SetLastError = true)]
    private static extern int SetXAttr(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        byte[] value,
        nuint size,
        uint position,
        int options);
}

internal static class WindowsExtendedAttributes
{
    private const int FindStreamInfoStandard = 0;
    private const int HandleEnd = 38;
    private static readonly nint InvalidHandle = new(-1);

    internal static byte[] Get(string path, string name) => File.ReadAllBytes(GetStreamPath(path, name));

    internal static string[] List(string path)
    {
        var handle = FindFirstStream(path, FindStreamInfoStandard, out var data, 0);
        if (handle == InvalidHandle)
        {
            if (Marshal.GetLastPInvokeError() == HandleEnd)
                return [];
            ExtendedAttributes.ThrowNativeError("list extended attributes");
        }

        try
        {
            var names = new List<string>();
            do
            {
                if (data.StreamName.StartsWith(':') &&
                    data.StreamName.EndsWith(":$DATA", StringComparison.OrdinalIgnoreCase) &&
                    !data.StreamName.Equals("::$DATA", StringComparison.OrdinalIgnoreCase))
                {
                    names.Add(data.StreamName[1..^6]);
                }
            }
            while (FindNextStream(handle, out data));

            var error = Marshal.GetLastPInvokeError();
            if (error != HandleEnd)
                throw new IOException("The operating system could not list extended attributes.", new Win32Exception(error));
            return names.ToArray();
        }
        finally
        {
            FindClose(handle);
        }
    }

    internal static void Remove(string path, string name)
    {
        var streamPath = GetStreamPath(path, name);
        if (!DeleteFile(streamPath))
            ExtendedAttributes.ThrowNativeError("remove the extended attribute");
    }

    internal static void Set(string path, string name, ReadOnlySpan<byte> value) =>
        File.WriteAllBytes(GetStreamPath(path, name), value.ToArray());

    private static string GetStreamPath(string path, string name) => $"{path}:{name}";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FindStreamData
    {
        internal long StreamSize;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)]
        internal string StreamName;
    }

    [DllImport("kernel32.dll", EntryPoint = "FindFirstStreamW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint FindFirstStream(
        string fileName,
        int infoLevel,
        out FindStreamData findStreamData,
        uint flags);

    [DllImport("kernel32.dll", EntryPoint = "FindNextStreamW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FindNextStream(nint findStream, out FindStreamData findStreamData);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FindClose(nint findFile);

    [DllImport("kernel32.dll", EntryPoint = "DeleteFileW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteFile(string fileName);
}
