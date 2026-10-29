namespace Electron2D;

internal static partial class CanvasRasterScale
{
    internal sealed class ScaleLevel
    {
        internal int Count;
        internal readonly List<WeakReference<DPITexture>> Textures = [];
        internal readonly List<WeakReference<FontData>> Fonts = [];
    }
    internal static ScaleLevel? ActiveLevel(int key) => key == 64 ? null : Volatile.Read(ref _scales).GetValueOrDefault(key);
    internal static bool TrackFont(int key, ScaleLevel level, FontData font)
    {
        lock (ScaleGate)
        {
            if (!_scales.TryGetValue(key, out var current) || !ReferenceEquals(current, level)) return false;
            for (var i = level.Fonts.Count - 1; i >= 0; i--)
                if (!level.Fonts[i].TryGetTarget(out _)) level.Fonts.RemoveAt(i);
            level.Fonts.Add(new(font));
            return true;
        }
    }
    private static readonly object ScaleGate = new();
    private static Dictionary<int, ScaleLevel> _scales = [];
    internal static int Quantize(float scale) => (int)(Math.Clamp((double)scale, 0.1, 100) * 64);
    internal static void ChangeReference(ref int current, int next)
    {
        var previous = current;
        if (previous == next) return;
        ScaleLevel? retired = null; ProviderNotification[] providers; var removed = false;
        lock (ScaleGate)
        {
            EnsureProviderNotificationsIdle();
            var levels = new Dictionary<int, ScaleLevel>(_scales);
            if (next != 64 && levels.TryGetValue(next, out var full) && full.Count == int.MaxValue) throw new OverflowException("Raster reference count exceeds its limit.");
            providers = BeginProviderNotifications();
            if (next != 64)
            {
                if (!levels.TryGetValue(next, out var level)) levels.Add(next, level = new());
                checked { level.Count++; }
            }
            if (previous != 64 && levels.TryGetValue(previous, out var old))
            { removed = true; if (--old.Count == 0) { levels.Remove(previous); retired = old; } }
            Volatile.Write(ref _scales, levels);
            current = next;
        }
        List<Exception>? errors = null;
        try
        {
            foreach (var notification in providers)
            {
                var provider = notification.Provider;
                if (next != 64) NotifyProvider(provider, next, true, false, ref errors);
                if (removed) NotifyProvider(provider, previous, false, false, ref errors);
            }
        }
        finally { EndProviderNotifications(providers); }
        if (retired is not null)
        {
            foreach (var weak in retired.Textures)
                if (weak.TryGetTarget(out var texture)) try { texture.RetireScale(previous, retired); } catch (Exception error) { Node.CollectException(ref errors, error); }
            foreach (var weak in retired.Fonts)
                if (weak.TryGetTarget(out var font)) try { font.RetireRasterScale(previous, retired); } catch (Exception error) { Node.CollectException(ref errors, error); }
        }
        Node.ThrowCollected("Raster scale reference committed with retirement failures.", errors);
    }
    internal static ScaleLevel? CurrentLevel(int key) => key == 64 ? null : Volatile.Read(ref _scales).GetValueOrDefault(key) ?? throw new InvalidOperationException("The viewport raster scale is no longer active.");
    internal static void TrackScale(int key, ScaleLevel level, DPITexture texture)
    {
        lock (ScaleGate)
        {
            if (!_scales.TryGetValue(key, out var current) || !ReferenceEquals(current, level)) throw new InvalidOperationException("The viewport raster scale changed during generation.");
            for (var i = level.Textures.Count - 1; i >= 0; i--)
            {
                if (!level.Textures[i].TryGetTarget(out var existing)) level.Textures.RemoveAt(i);
                else if (ReferenceEquals(existing, texture)) return;
            }
            level.Textures.Add(new(texture));
        }
    }
}
