namespace Electron2D;

public abstract partial class TextServer
{
    private static readonly object NativeFontsGate = new();
    private static readonly Dictionary<RID, WeakReference<ServerFont>> NativeFonts = [];

    private static bool TryGetNativeFontLocked(RID rid, out ServerFont font)
    {
        if (NativeFonts.TryGetValue(rid, out var weak) && weak.TryGetTarget(out font!) && !font.IsDisposed && !font.Retiring) return true;
        font = null!; return false;
    }
    private static bool HasNativeFont(RID rid)
    { lock (NativeFontsGate) return TryGetNativeFontLocked(rid, out _); }

    internal readonly struct NativeFontRead : IDisposable
    {
        private readonly ServerFont? _font;
        private readonly bool _retainRoot;
        internal NativeFontRead(RID rid)
        {
            lock (NativeFontsGate)
            {
                _font = null; _retainRoot = false;
                if (!NativeFonts.TryGetValue(rid, out var weak) || !weak.TryGetTarget(out var font)) return;
                if (font.IsDisposed || font.Retiring) throw new InvalidOperationException("The native font identity has retired.");
                _font = font; font.Readers++;
                _retainRoot = !font.Root.IsDisposed && !font.Root.Retiring;
                if (_retainRoot) font.Root.FamilyReaders++;
            }
        }
        internal bool IsCurrent => _font is null || !_font.IsDisposed;
        public void Dispose()
        { if (_font is not null) lock (NativeFontsGate) { _font.Readers--; if (_retainRoot) _font.Root.FamilyReaders--; } }
    }
    internal static void RetainNativeFontSources(List<Font> fonts, List<NativeFontRead> reads)
    { foreach (var font in fonts) if (font is ServerFont serverFont && serverFont.Identity != default) reads.Add(new(serverFont.Identity)); }

    private ServerFont BeginOwnedFontRetirement(RID rid)
    {
        ServerFont? font; lock (_fontsGate) _fonts.TryGetValue(rid, out font);
        if (font is null) throw new InvalidOperationException("Only identities owned by this implementation can be freed here.");
        lock (NativeFontsGate)
        {
            if (font.Retiring || font.IsDisposed) throw new InvalidOperationException("The native font is already retiring.");
            font.CheckCanRetire(); font.Retiring = true; return font;
        }
    }
}
