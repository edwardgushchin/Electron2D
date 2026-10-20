using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal static unsafe partial class NativeTextBreak
{
    private static nint _spoof;
    internal static void LoadSupportData() { lock (Gate) Initialize(); }
    internal static byte[] GetSupportData()
    {
        using var stream = typeof(NativeTextBreak).Assembly.GetManifestResourceStream("Electron2D.TextBreak.dat")
            ?? throw new InvalidOperationException("The native text boundary data is missing.");
        if (stream.Length is <= 0 or > 64 * 1024 * 1024) throw new InvalidDataException("The native text boundary data has an invalid length.");
        var result = new byte[(int)stream.Length]; stream.ReadExactly(result); return result;
    }
    internal static bool SpoofCheck(string text)
    {
        ValidateString(text);
        lock (Gate)
        {
            InitializeSpoof();
            fixed (char* source = text) { var result = SpoofCheckNative(_spoof, source, text.Length, out var error); Check(error); return result != 0; }
        }
    }
    internal static int IsConfusable(string text, ReadOnlySpan<string> dictionary)
    {
        ValidateString(text);
        if (dictionary.Length > 1048576) throw new ArgumentException("The dictionary exceeds 1,048,576 entries.", nameof(dictionary));
        var words = dictionary.ToArray(); long units = text.Length;
        foreach (var word in words)
        {
            ValidateString(word); units += word.Length;
            if (units > MaximumStringUnits) throw new ArgumentException("The text and dictionary exceed 64 MiB of UTF-16.", nameof(dictionary));
        }
        lock (Gate)
        {
            InitializeSpoof(); var skeleton = Skeleton(text);
            for (var i = 0; i < words.Length; i++) if (skeleton.AsSpan().SequenceEqual(Skeleton(words[i]))) return i;
            return -1;
        }
    }
    private static void InitializeSpoof()
    {
        Initialize(); if (_spoof != 0) return;
        var checker = SpoofOpen(out var error);
        if (error > 0 || checker == 0)
        {
            if (checker != 0) SpoofClose(checker);
            Check(error); throw new InvalidOperationException("Could not create the native Unicode security checker.");
        }
        _spoof = checker;
    }
    private static char[] Skeleton(string text)
    {
        fixed (char* source = text)
        {
            var count = SpoofSkeleton(_spoof, source, text.Length, null, 0, out var error);
            if (error != 15) Check(error);
            if (count < 0 || count > MaximumStringUnits) throw new ArgumentException("The confusable skeleton exceeds 64 MiB of UTF-16.", nameof(text));
            if (count == 0) return [];
            var result = new char[count];
            fixed (char* output = result)
            {
                var written = SpoofSkeleton(_spoof, source, text.Length, output, count, out error); Check(error);
                if (written != count) throw new InvalidOperationException("The native confusable skeleton changed length.");
            }
            return result;
        }
    }
    [LibraryImport(Library, EntryPoint = "e2d_spoof_open"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint SpoofOpen(out int error);
    [LibraryImport(Library, EntryPoint = "e2d_spoof_close"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void SpoofClose(nint checker);
    [LibraryImport(Library, EntryPoint = "e2d_spoof_skeleton"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int SpoofSkeleton(nint checker, char* text, int length, char* output, int capacity, out int error);
    [LibraryImport(Library, EntryPoint = "e2d_spoof_check"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int SpoofCheckNative(nint checker, char* text, int length, out int error);
}
