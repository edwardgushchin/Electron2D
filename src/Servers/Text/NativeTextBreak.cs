using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Electron2D;

// Process-owned, bounded native iterator cache. Mutable cursors never escape this lock or a pinned text scope.
// ponytail: boundary analysis serializes here; shard iterators only if measured concurrent shaping needs it.
internal static unsafe partial class NativeTextBreak
{
    private const string Library = "Electron2DTextBreak";
    private const int LineCapacity = 64;
    private static readonly object Gate = new();
    private static readonly Dictionary<string, (nint Handle, ulong Access)> Lines = new(StringComparer.Ordinal);
    private static nint _data, _word;
    private static bool _initialized;
    private static ulong _access;

    internal static void Fill(string text, string language, ReadOnlySpan<int> scalarUTF16Offsets,
        Span<bool> lineBreaks, Span<bool> wordBoundaries, Span<bool> wordEnds)
    {
        ArgumentNullException.ThrowIfNull(text); ArgumentNullException.ThrowIfNull(language);
        var count = scalarUTF16Offsets.Length;
        if (count == 0 || scalarUTF16Offsets[0] != 0 || scalarUTF16Offsets[^1] != text.Length ||
            lineBreaks.Length < count || wordBoundaries.Length < count || wordEnds.Length < count)
            throw new ArgumentException("Boundary buffers must describe the complete scalar range.");
        lineBreaks[..count].Clear(); wordBoundaries[..count].Clear(); wordEnds[..count].Clear();
        wordBoundaries[0] = true; lineBreaks[count - 1] = true;
        if (text.Length == 0) return;
        var locale = language.Length == 0 ? TranslationServer.GetToolLocale() : language;
        lock (Gate)
        {
            Initialize();
            var line = GetLine(locale);
            if (_word == 0) _word = Open(1, string.Empty);
            fixed (char* pointer = text)
            {
                try
                {
                    Check(Set(line, pointer, text.Length)); First(line);
                    for (var position = Next(line); position >= 0; position = Next(line))
                        lineBreaks[ScalarIndex(scalarUTF16Offsets, position)] = true;
                    Check(Set(_word, pointer, text.Length)); First(_word);
                    for (var position = Next(_word); position >= 0; position = Next(_word))
                    {
                        var scalar = ScalarIndex(scalarUTF16Offsets, position);
                        wordBoundaries[scalar] = true;
                        wordEnds[scalar] = Status(_word) >= 100;
                    }
                }
                finally
                {
                    // Cached native cursors must not retain a managed string after its fixed scope ends.
                    var lineError = Set(line, null, 0); var wordError = Set(_word, null, 0);
                    Check(lineError); Check(wordError);
                }
            }
        }
    }

    internal static bool IsLocaleRTL(string language)
    {
        ArgumentNullException.ThrowIfNull(language);
        var locale = (language.Length == 0 ? TranslationServer.GetToolLocale() : language).AsSpan();
        // Accept the managed culture tag separator as well as the native locale separator.
        var separator = locale.IndexOfAny('_', '-'); if (separator >= 0) locale = locale[..separator];
        return locale.SequenceEqual("ar") || locale.SequenceEqual("dv") || locale.SequenceEqual("he") ||
            locale.SequenceEqual("fa") || locale.SequenceEqual("ff") || locale.SequenceEqual("ku") || locale.SequenceEqual("ur");
    }

    internal static bool IsNonprinting(uint scalar)
    {
        if (!Rune.IsValid(scalar)) throw new ArgumentOutOfRangeException(nameof(scalar));
        lock (Gate) { Initialize(); return IsNonprintingNative(scalar) != 0; }
    }
    private static int ScalarIndex(ReadOnlySpan<int> offsets, int utf16)
    {
        var index = offsets.BinarySearch(utf16);
        if (index < 0) throw new InvalidOperationException("A native text boundary does not end at a Unicode scalar.");
        return index;
    }
    private static nint GetLine(string locale)
    {
        if (Lines.TryGetValue(locale, out var entry))
        {
            Lines[locale] = (entry.Handle, ++_access); return entry.Handle;
        }
        var iterator = Open(2, locale);
        try
        {
            if (Lines.Count == LineCapacity)
            {
                string? oldest = null; var age = ulong.MaxValue;
                foreach (var pair in Lines) if (pair.Value.Access < age) { oldest = pair.Key; age = pair.Value.Access; }
                Close(Lines[oldest!].Handle); Lines.Remove(oldest!);
            }
            Lines.Add(locale, (iterator, ++_access)); return iterator;
        }
        catch { Close(iterator); throw; }
    }
    private static nint Open(int kind, string locale)
    {
        var bytes = Encoding.UTF8.GetBytes(locale + '\0');
        fixed (byte* pointer = bytes)
        {
            var handle = OpenNative(kind, pointer, null, 0, out var error);
            if (error > 0 || handle == 0)
            {
                if (handle != 0) Close(handle);
                throw new InvalidOperationException("Could not create a native text boundary iterator (status " + error + ").");
            }
            return handle;
        }
    }
    private static void Initialize()
    {
        if (_initialized) return;
        if ((!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsWindows() && !OperatingSystem.IsAndroid()) ||
            RuntimeInformation.ProcessArchitecture is not (Architecture.X64 or Architecture.Arm64) && !(OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X86) && !(OperatingSystem.IsAndroid() && RuntimeInformation.ProcessArchitecture is Architecture.X86 or Architecture.Arm))
            throw new PlatformNotSupportedException("Native word and line boundaries require a packaged desktop or Android backend.");
        if (_data == 0)
        {
            using var stream = typeof(NativeTextBreak).Assembly.GetManifestResourceStream("Electron2D.TextBreak.dat")
                ?? throw new InvalidOperationException("The native text boundary data is missing.");
            var length = checked((int)stream.Length);
            if (length is <= 0 or > 64 * 1024 * 1024) throw new InvalidDataException("The native text boundary data has an invalid length.");
            var memory = NativeMemory.AlignedAlloc((nuint)((length + 15) & ~15), 16);
            if (memory == null) throw new OutOfMemoryException();
            try { stream.ReadExactly(new Span<byte>(memory, length)); Check(InitializeData(memory)); }
            catch { NativeMemory.AlignedFree(memory); throw; }
            // ICU retains this process-owned immutable block; it is not a renderer or font-resource allocation.
            _data = (nint)memory;
        }
        byte* version = stackalloc byte[8]; Versions(version, version + 4);
        if (version[0] != 78 || version[1] != 3 || version[4] != 17 || version[5] != 0)
            throw new PlatformNotSupportedException("Text boundaries require the packaged ICU 78.3 and Unicode 17 data.");
        _initialized = true;
    }
    private static void Check(int error)
    {
        if (error > 0) throw new InvalidOperationException("Native text boundary processing failed (status " + error + ").");
    }

    [LibraryImport(Library, EntryPoint = "e2d_text_init"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int InitializeData(void* data);
    [LibraryImport(Library, EntryPoint = "e2d_text_versions"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void Versions(byte* icu, byte* unicode);
    [LibraryImport(Library, EntryPoint = "e2d_text_is_nonprinting"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int IsNonprintingNative(uint scalar);
    [LibraryImport(Library, EntryPoint = "e2d_break_open"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint OpenNative(int kind, byte* locale, char* text, int length, out int error);
    [LibraryImport(Library, EntryPoint = "e2d_break_set"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int Set(nint iterator, char* text, int length);
    [LibraryImport(Library, EntryPoint = "e2d_break_first"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int First(nint iterator);
    [LibraryImport(Library, EntryPoint = "e2d_break_next"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int Next(nint iterator);
    [LibraryImport(Library, EntryPoint = "e2d_break_status"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int Status(nint iterator);
    [LibraryImport(Library, EntryPoint = "e2d_break_close"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void Close(nint iterator);
}
