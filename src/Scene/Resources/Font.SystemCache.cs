namespace Electron2D;

public partial class Font
{
    private static readonly object LiveFontsGate = new();
    private static readonly List<WeakReference<Font>> LiveFonts = [];
    private static int _clearingSystemFonts;
    private static long _liveRegistrations, _nextLiveSweep = 64;
    private void RegisterLiveFont()
    {
        lock (LiveFontsGate)
        {
            if (++_liveRegistrations >= _nextLiveSweep)
            {
                LiveFonts.RemoveAll(static weak => !weak.TryGetTarget(out var font) || font.IsDisposalCommitted);
                _nextLiveSweep = _liveRegistrations + Math.Max(64, LiveFonts.Count);
            }
            LiveFonts.Add(new(this));
        }
    }
    internal static void ClearSystemFontCaches()
    {
        if (Interlocked.Exchange(ref _clearingSystemFonts, 1) != 0) throw new InvalidOperationException("System font cache clearing is already active.");
        try
        {
            Font[] fonts;
            lock (LiveFontsGate)
            {
                LiveFonts.RemoveAll(static weak => !weak.TryGetTarget(out var font) || font.IsDisposalCommitted);
                fonts = LiveFonts.Select(weak => weak.TryGetTarget(out var font) ? font : null).OfType<Font>().ToArray();
            }
            var sources = new HashSet<FontData>();
            foreach (var font in fonts) lock (font.FontGate)
                {
                    if (font.IsDisposalCommitted) continue;
                    foreach (var source in font._sources) sources.Add(source);
                    font.ClearLayoutCaches();
                }
            foreach (var source in sources) source.ClearSystemFallbackCache();
            List<Exception>? errors = null;
            foreach (var font in fonts)
                try { if (!font.IsDisposed) font.EmitFontChanged(); }
                catch (Exception error) { (errors ??= []).Add(error); }
            if (errors is not null) throw new AggregateException("System font cache invalidation callbacks failed.", errors);
        }
        finally { Volatile.Write(ref _clearingSystemFonts, 0); }
    }
}
