namespace Electron2D;

internal sealed class GradientTextureData(Texture owner, bool ramp)
{
    // ponytail: one resource lock covers lazy baking; use immutable versioned settings if large fills cause measured contention.
    private readonly object _gate = new();
    private int _width = ramp ? 256 : 64, _height = ramp ? 1 : 64;
    private bool _hdr, _dirty = true;
    private Gradient? _gradient;
    private GradientTexture.FillEnum _fill;
    private GradientTexture.RepeatEnum _repeat;
    private Vector2 _from, _to = new(1, 0);
    private TexturePixels? _pixels;

    internal int Width
    {
        get { lock (_gate) { CheckAlive(); return _width; } }
        set { lock (_gate) { CheckAlive(); Dimension(value); _width = value; _dirty = true; } owner.EmitChanged(); }
    }
    internal int Height
    {
        get { lock (_gate) { CheckAlive(); return _height; } }
        set { lock (_gate) { CheckAlive(); Dimension(value); _height = value; _dirty = true; } owner.EmitChanged(); }
    }
    internal bool UseHDR
    {
        get { lock (_gate) { CheckAlive(); return _hdr; } }
        set { lock (_gate) { CheckAlive(); if (_hdr == value) return; _hdr = value; _dirty = true; } owner.EmitChanged(); }
    }
    internal Gradient? Gradient
    {
        get { lock (_gate) { CheckAlive(); return _gradient; } }
        set
        {
            lock (_gate)
            {
                CheckAlive(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
                if (value == _gradient) return;
                SetSource(value); _dirty = true;
            }
            owner.EmitChanged();
        }
    }
    internal GradientTexture.FillEnum Fill
    {
        get { lock (_gate) { CheckAlive(); return _fill; } }
        set { lock (_gate) { CheckAlive(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _fill = value; _dirty = true; } owner.EmitChanged(); }
    }
    internal GradientTexture.RepeatEnum Repeat
    {
        get { lock (_gate) { CheckAlive(); return _repeat; } }
        set { lock (_gate) { CheckAlive(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _repeat = value; _dirty = true; } owner.EmitChanged(); }
    }
    internal Vector2 FillFrom
    {
        get { lock (_gate) { CheckAlive(); return _from; } }
        set { lock (_gate) { CheckAlive(); if (!value.IsFinite()) throw new ArgumentException("Fill coordinates must be finite.", nameof(value)); _from = value; _dirty = true; } owner.EmitChanged(); }
    }
    internal Vector2 FillTo
    {
        get { lock (_gate) { CheckAlive(); return _to; } }
        set { lock (_gate) { CheckAlive(); if (!value.IsFinite()) throw new ArgumentException("Fill coordinates must be finite.", nameof(value)); _to = value; _dirty = true; } owner.EmitChanged(); }
    }
    internal Vector2 GetSize() { lock (_gate) { CheckAlive(); return new(_width, _height); } }

    internal TexturePixels? CapturePixels()
    {
        lock (_gate)
        {
            CheckAlive();
            if (!_dirty) return _pixels;
            if (_gradient is not null) _pixels = _gradient.BakeTexture(_width, _height, _hdr, Offset, solidFill: !ramp);
            _dirty = false;
            return _pixels;
        }
    }

    private float Offset(int x, int y)
    {
        if (ramp) return _width == 1 ? 0 : x / (float)(_width - 1);
        if (_from == _to) return 0;
        double px = _width == 1 ? 0 : x / (double)(_width - 1), py = _height == 1 ? 0 : y / (double)(_height - 1);
        double dx = (double)_to.X - _from.X, dy = (double)_to.Y - _from.Y;
        px -= _from.X; py -= _from.Y;
        var offset = _fill switch
        {
            GradientTexture.FillEnum.Linear => dx * dx + dy * dy < 1e-20 ? 0 : (px * dx + py * dy) / (dx * dx + dy * dy),
            GradientTexture.FillEnum.Radial => Math.Sqrt(px * px + py * py) / Math.Sqrt(dx * dx + dy * dy),
            GradientTexture.FillEnum.Square => Math.Max(Math.Abs(px), Math.Abs(py)) / Math.Max(Math.Abs(dx), Math.Abs(dy)),
            _ => ((Math.Atan2(dx * py - dy * px, dx * px + dy * py) % Math.Tau + Math.Tau) % Math.Tau) / Math.Tau,
        };
        offset = _repeat switch
        {
            GradientTexture.RepeatEnum.None => Math.Clamp(offset, 0, 1),
            GradientTexture.RepeatEnum.Repeat => (offset % 1 + 1) % 1,
            _ => Math.Abs(offset) % 2,
        };
        if (_repeat == GradientTexture.RepeatEnum.Mirror && offset > 1) offset = 2 - offset;
        return (float)offset;
    }

    private void SetSource(Gradient? source)
    {
        if (_gradient == source) return;
        if (_gradient is not null) _gradient.Changed -= Invalidate;
        _gradient = source;
        if (_gradient is not null) _gradient.Changed += Invalidate;
    }
    private void Invalidate(Resource source) { lock (_gate) { if (!owner.IsDisposed && source == _gradient) _dirty = true; } }

    internal void CopyTo(GradientTextureData target, bool deep, Func<Resource?, Resource?> duplicate)
    {
        int width, height; bool hdr; Gradient? gradient; GradientTexture.FillEnum fill; GradientTexture.RepeatEnum repeat; Vector2 from, to; TexturePixels? pixels;
        lock (_gate) { CheckAlive(); width = _width; height = _height; hdr = _hdr; gradient = _gradient; fill = _fill; repeat = _repeat; from = _from; to = _to; pixels = _pixels; }
        if (deep) gradient = (Gradient?)duplicate(gradient);
        lock (target._gate)
        {
            target.CheckAlive(); target.SetSource(gradient); target._width = width; target._height = height; target._hdr = hdr;
            target._fill = fill; target._repeat = repeat; target._from = from; target._to = to; target._pixels = pixels; target._dirty = true;
        }
    }
    internal void Dispose() { lock (_gate) { SetSource(null); _pixels = null; } }
    private void CheckAlive() { if (owner.IsDisposed) throw new ObjectDisposedException(owner.GetType().Name); }
    private static void Dimension(int value) { if (value is < 1 or > 16384) throw new ArgumentOutOfRangeException(nameof(value)); }
}
