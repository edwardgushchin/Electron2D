using System.Buffers.Binary;

namespace Electron2D;

internal sealed class CurveTextureData(Texture owner, bool single)
{
    // ponytail: rebaking holds one resource lock; publish versioned snapshots if edit contention becomes measurable.
    private readonly object _gate = new();
    private int _width = 256;
    private CurveTexture.TextureModeEnum _mode;
    private Curve?[] _curves = new Curve?[single ? 1 : 3];
    private TexturePixels? _pixels;

    internal int Width
    {
        get { lock (_gate) { CheckAlive(); return _width; } }
        set
        {
            lock (_gate)
            {
                CheckAlive();
                if (value is < 32 or > 4096) throw new ArgumentOutOfRangeException(nameof(value));
                if (value == _width) return;
                Replace(value, _mode, _curves, true);
            }
            owner.EmitChanged();
        }
    }

    internal CurveTexture.TextureModeEnum Mode
    {
        get { lock (_gate) { CheckAlive(); return _mode; } }
        set
        {
            lock (_gate)
            {
                CheckAlive();
                if (value is not (CurveTexture.TextureModeEnum.RGB or CurveTexture.TextureModeEnum.Red)) throw new ArgumentOutOfRangeException(nameof(value));
                if (value == _mode) return;
                Replace(_width, value, _curves, true);
            }
            owner.EmitChanged();
        }
    }

    internal Curve? GetCurve(int channel) { lock (_gate) { CheckAlive(); return _curves[channel]; } }
    internal void SetCurve(int channel, Curve? curve)
    {
        lock (_gate)
        {
            CheckAlive();
            if (curve?.IsDisposed == true) throw new ObjectDisposedException(nameof(curve));
            if (ReferenceEquals(_curves[channel], curve)) return;
            var curves = (Curve?[])_curves.Clone(); curves[channel] = curve;
            Replace(_width, _mode, curves, true);
        }
        owner.EmitChanged();
    }

    private void Replace(int width, CurveTexture.TextureModeEnum mode, Curve?[] curves, bool initialized)
    {
        // Subscribe before sampling so a racing edit cannot be lost between the sample and subscription.
        foreach (var curve in curves.Distinct())
            if (curve is not null && !_curves.Contains(curve)) curve.Changed += CurveChanged;
        TexturePixels? pixels;
        try { pixels = initialized ? Bake(width, mode, curves) : null; }
        catch
        {
            foreach (var curve in curves.Distinct())
                if (curve is not null && !_curves.Contains(curve)) curve.Changed -= CurveChanged;
            throw;
        }
        foreach (var curve in _curves.Distinct())
            if (curve is not null && !curves.Contains(curve)) curve.Changed -= CurveChanged;
        _width = width; _mode = mode; _curves = curves; _pixels = pixels;
    }

    private TexturePixels Bake(int width, CurveTexture.TextureModeEnum mode, Curve?[] curves)
    {
        var channels = mode == CurveTexture.TextureModeEnum.Red ? 1 : 3;
        var data = new byte[width * channels * sizeof(float)];
        var samples = new float[]?[curves.Length];
        for (var channel = 0; channel < curves.Length; channel++)
        {
            if (curves[channel] is not { } curve) continue;
            var previous = Array.IndexOf(curves, curve, 0, channel);
            samples[channel] = previous >= 0 ? samples[previous] : curve.SampleTexture(width);
        }
        for (var i = 0; i < width; i++)
            for (var channel = 0; channel < channels; channel++)
                BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan((i * channels + channel) * sizeof(float)), samples[single ? 0 : channel]?[i] ?? 0);
        using var image = Image.CreateFromData(width, 1, false, channels == 1 ? Image.Format.Rf : Image.Format.Rgbf, data);
        var pixels = TexturePixels.FromImage(image);
        if (_pixels is not null && _pixels.Source.Width == width && _pixels.Source.Format == pixels.Source.Format)
            pixels.Allocation = _pixels.Allocation;
        return pixels;
    }

    private void CurveChanged(Resource source)
    {
        lock (_gate)
        {
            if (owner.IsDisposed || !_curves.Contains(source)) return;
            _pixels = Bake(_width, _mode, _curves);
        }
        if (!owner.IsDisposed) owner.EmitChanged();
    }

    internal TexturePixels? CapturePixels() { lock (_gate) { CheckAlive(); return _pixels; } }

    internal void CopyTo(CurveTextureData target, bool deep, Func<Resource?, Resource?> duplicate)
    {
        int width; CurveTexture.TextureModeEnum mode; Curve?[] curves; bool initialized;
        lock (_gate) { CheckAlive(); width = _width; mode = _mode; curves = (Curve?[])_curves.Clone(); initialized = _pixels is not null; }
        if (deep) for (var i = 0; i < curves.Length; i++) curves[i] = (Curve?)duplicate(curves[i]);
        lock (target._gate) { target.CheckAlive(); target.Replace(width, mode, curves, initialized); }
    }

    internal void Dispose()
    {
        lock (_gate)
        {
            foreach (var curve in _curves.Distinct()) if (curve is not null) curve.Changed -= CurveChanged;
            Array.Clear(_curves); _pixels = null;
        }
    }

    private void CheckAlive() { if (owner.IsDisposed) throw new ObjectDisposedException(owner.GetType().Name); }
}
