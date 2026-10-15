namespace Electron2D;

public sealed partial class DPITexture
{
    private sealed class ScaleLevel
    {
        internal int Count;
        internal readonly List<WeakReference<DPITexture>> Textures = [];
    }
    private sealed record RasterVariant(ImageTexture Texture, ScaleLevel? Level);
    private static readonly object ScaleGate = new();
    private static Dictionary<int, ScaleLevel> _scales = [];
    internal static int QuantizeScale(float scale) => (int)(Math.Clamp((double)scale, 0.1, 100) * 64);
    internal static void ChangeScaleReference(int previous, int next)
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
            foreach (var weak in retired.Textures)
                if (weak.TryGetTarget(out var texture)) texture.RetireScale(previous, retired);
    }
    private static ScaleLevel? CurrentLevel(int key) => key == 64 ? null : Volatile.Read(ref _scales).GetValueOrDefault(key) ?? throw new InvalidOperationException("The viewport raster scale is no longer active.");
    private static void TrackScale(int key, ScaleLevel level, DPITexture texture)
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
