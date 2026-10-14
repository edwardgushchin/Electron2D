namespace Electron2D;

public partial class FontFile
{
    private byte[] _type1Metrics = [];
    private static readonly PropertyDescriptor MetricsProperty = new PropertyDescriptor<FontFile, byte[]>(
        "_font_metrics", font => font.SaveMetrics(), (font, value) => font.RestoreMetrics(value), _ => [], stored: true);
    private byte[] SaveMetrics() { lock (FontGate) { ThrowIfDisposed(); return (byte[])_type1Metrics.Clone(); } }
    private void RestoreMetrics(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value); var copy = (byte[])value.Clone(); List<FontData> retired;
        lock (FontGate)
        {
            ThrowIfDisposed(); ValidateSourceBudget(_bytes, copy);
            if (_type1Metrics.AsSpan().SequenceEqual(copy)) return;
            if (_bytes.Length == 0 && copy.Length != 0) throw new InvalidDataException("Auxiliary metrics require outline source bytes.");
            var previous = _type1Metrics; _type1Metrics = copy;
            try
            {
                var caches = new List<FontCache>();
                for (var i = 0; i < _caches.Count; i++) { var data = DataLocked(i); lock (data.Gate) caches.Add(_caches[i].Clone()); }
                retired = PublishCachesLocked(caches);
            }
            catch { _type1Metrics = previous; throw; }
        }
        NotifyRetired(retired);
    }
    private static void ValidateSourceBudget(byte[] bytes, byte[] metrics)
    {
        if ((long)bytes.Length + metrics.Length > MaximumFontBytes) throw new InvalidDataException("Outline and auxiliary font data exceed the source byte budget.");
    }
}
