namespace Electron2D;

internal static class CanvasRasterScale
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
    internal static void ChangeReference(int previous, int next)
    {
        if (previous == next) return;
        ScaleLevel? retired = null;
        lock (ScaleGate)
        {
            var levels = new Dictionary<int, ScaleLevel>(_scales);
            if (next != 64)
            {
                if (!levels.TryGetValue(next, out var level)) levels.Add(next, level = new());
                checked { level.Count++; }
            }
            if (previous != 64 && levels.TryGetValue(previous, out var old) && --old.Count == 0) { levels.Remove(previous); retired = old; }
            Volatile.Write(ref _scales, levels);
        }
        if (retired is not null)
        {
            foreach (var weak in retired.Textures)
                if (weak.TryGetTarget(out var texture)) texture.RetireScale(previous, retired);
            foreach (var weak in retired.Fonts)
                if (weak.TryGetTarget(out var font)) font.RetireRasterScale(previous, retired);
        }
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
