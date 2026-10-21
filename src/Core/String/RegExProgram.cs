using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Electron2D;

internal sealed partial class RegExProgram : SafeHandle
{
    internal const int TextLimit = 16_777_216;
    internal int GroupCount { get; }
    internal (string Name, int Index)[] Names { get; }
    public override bool IsInvalid => handle == 0;
    private unsafe RegExProgram(nint code) : base(0, true)
    {
        SetHandle(code);
        try
        {
            Check(Info(this, out var groups, out var count, out var width, out var address), string.Empty, string.Empty, "pattern");
            GroupCount = checked((int)groups); Names = new (string, int)[checked((int)count)];
            var table = (uint*)address;
            for (var i = 0; i < Names.Length; i++)
            {
                var entry = new ReadOnlySpan<uint>(table + checked(i * (int)width), checked((int)width));
                var length = entry[1..].IndexOf(0u); if (length < 0) throw new InvalidDataException("Invalid native capture name.");
                Names[i] = (Decode(entry.Slice(1, length)), checked((int)entry[0]));
            }
        }
        catch { Dispose(); throw; }
    }
    internal static unsafe RegExProgram Compile(string pattern)
    {
        var input = new Text(pattern);
        fixed (uint* values = input.Values)
        {
            var code = Create(values, (nuint)input.Count, out var error, out var offset);
            if (code == 0 && error is 121 or -48) throw new OutOfMemoryException(Message(error));
            if (code == 0) throw new ArgumentException($"Pattern at code point {offset}: {Message(error)}", nameof(pattern));
            return new RegExProgram(code);
        }
    }
    internal unsafe bool Search(Text text, int offset, int end, nuint[] positions, string pattern, string subject)
    {
        fixed (uint* input = text.Values)
        fixed (nuint* output = positions)
        {
            var status = Match(this, input, (nuint)end, (nuint)offset, output, (nuint)positions.Length);
            if (status == -1) return false;
            Check(status, pattern, subject, nameof(subject)); return true;
        }
    }
    internal unsafe string Substitute(Text text, int offset, int end, string subject, string replacement, bool all, string pattern)
    {
        var replace = new Text(replacement); var output = new uint[Math.Min(256, text.Count + 1)]; var started = Stopwatch.GetTimestamp();
        var maximum = TextLimit - (text.Count - end);
        fixed (uint* input = text.Values)
        fixed (uint* value = replace.Values)
        {
            while (true)
            {
                var remaining = checked((int)Math.Ceiling(5000 - Stopwatch.GetElapsedTime(started).TotalMilliseconds));
                if (remaining <= 0) throw new RegexMatchTimeoutException(subject, pattern, TimeSpan.FromSeconds(5));
                var length = (nuint)output.Length; int status;
                fixed (uint* destination = output)
                    status = SubstituteNative(this, input, (nuint)end, (nuint)offset, value, (nuint)replace.Count, all ? 1 : 0, remaining, destination, ref length);
                if (status == -48 && length > (nuint)output.Length)
                {
                    if (length > (nuint)maximum + 1u) throw new InvalidOperationException("Regular-expression output exceeds the code-point budget.");
                    output = new uint[checked((int)length)]; continue;
                }
                Check(status, pattern, subject, nameof(replacement));
                if (length > (nuint)maximum) throw new InvalidOperationException("Regular-expression output exceeds the code-point budget.");
                return Decode(output.AsSpan(0, checked((int)length))) + subject[text.Boundaries[end]..];
            }
        }
    }
    private static void Check(int status, string pattern, string subject, string argument)
    {
        if (status >= 0) return;
        if (status == -37) throw new RegexMatchTimeoutException(subject, pattern, TimeSpan.FromSeconds(5));
        if (status == -48) throw new OutOfMemoryException(Message(status));
        if (status is -47 or -53 or -63) throw new InvalidOperationException("Regular-expression resource limit: " + Message(status));
        throw new ArgumentException(Message(status), argument);
    }
    private static unsafe string Message(int error)
    {
        Span<uint> values = stackalloc uint[256];
        fixed (uint* output = values)
        {
            var length = Error(error, output, (nuint)values.Length);
            return length >= 0 ? Decode(values[..length]) : "Native regular-expression error " + error;
        }
    }
    internal static string Decode(ReadOnlySpan<uint> values)
    {
        var length = values.Length;
        foreach (var value in values) { if (value > 0x10FFFF) throw new InvalidDataException("Invalid native text scalar."); if (value > 0xFFFF) length = checked(length + 1); }
        var result = new char[length]; var cursor = 0;
        foreach (var value in values)
        {
            if (value <= 0xFFFF) result[cursor++] = (char)value;
            else { var scalar = value - 0x10000; result[cursor++] = (char)(0xD800 + (scalar >> 10)); result[cursor++] = (char)(0xDC00 + (scalar & 1023)); }
        }
        return new string(result);
    }
    internal sealed class Text
    {
        internal readonly uint[] Values;
        internal readonly int[] Boundaries;
        internal int Count => Boundaries.Length - 1;
        internal Text(string value)
        {
            if (value.Length > TextLimit * 2) throw new ArgumentException("Regular-expression input exceeds the code-point budget.");
            var count = 0;
            for (var i = 0; i < value.Length; i++, count++) if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) i++;
            if (count > TextLimit) throw new ArgumentException("Regular-expression input exceeds the code-point budget.");
            Values = new uint[Math.Max(1, count)]; Boundaries = new int[count + 1]; var cursor = 0;
            for (var i = 0; i < value.Length; i++)
            {
                Boundaries[cursor] = i; uint scalar = value[i];
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) scalar = (uint)char.ConvertToUtf32(value[i], value[++i]);
                Values[cursor++] = scalar;
            }
            Boundaries[count] = value.Length;
        }
    }
    protected override bool ReleaseHandle() { Destroy(handle); return true; }
    [LibraryImport(NativeLibraries.TextBreakLibrary, EntryPoint = "e2d_regex_create")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial nint Create(uint* pattern, nuint length, out int error, out nuint offset);
    [LibraryImport(NativeLibraries.TextBreakLibrary, EntryPoint = "e2d_regex_destroy")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial void Destroy(nint code);
    [LibraryImport(NativeLibraries.TextBreakLibrary, EntryPoint = "e2d_regex_info")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Info(RegExProgram code, out uint groups, out uint names, out uint width, out nint table);
    [LibraryImport(NativeLibraries.TextBreakLibrary, EntryPoint = "e2d_regex_match")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int Match(RegExProgram code, uint* subject, nuint length, nuint offset, nuint* positions, nuint capacity);
    [LibraryImport(NativeLibraries.TextBreakLibrary, EntryPoint = "e2d_regex_substitute")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int SubstituteNative(RegExProgram code, uint* subject, nuint length, nuint offset, uint* replacement, nuint replacementLength, int all, int milliseconds, uint* output, ref nuint outputLength);
    [LibraryImport(NativeLibraries.TextBreakLibrary, EntryPoint = "e2d_regex_error")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial int Error(int error, uint* output, nuint capacity);
}
