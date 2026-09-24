namespace Electron2D;

/// <summary>Selects the algorithm between adjacent points.</summary>
public enum InterpolationMode
{
    /// <summary>Linear interpolation between adjacent colors.</summary>
    Linear = 0,
    /// <summary>Holds the preceding point's color until the next point.</summary>
    Constant = 1,
    /// <summary>Cubic interpolation using neighboring colors; may overshoot.</summary>
    Cubic = 2,
}

/// <summary>An ordered color transition with linear, constant or cubic interpolation.</summary>
/// <remarks>Points are sorted lazily by indexed color/offset operations, Reverse and Sample. Bulk array queries
/// and RemovePoint use the current storage order. Arrays are copied. State operations are serialized; synchronous
/// events run after mutation outside the lock. Coordinate multi-call edits, copying and disposal across threads.</remarks>
public sealed class Gradient : Resource
{
    /// <summary>Selects the space in which colors are interpolated; output remains nonlinear sRGB.</summary>
    public enum ColorSpace
    {
        /// <summary>Interpolate stored nonlinear sRGB values directly.</summary>
        SRGB = 0,
        /// <summary>Interpolate linear RGB before returning to sRGB.</summary>
        LinearSRGB = 1,
        /// <summary>Interpolate perceptual lightness and opponent components.</summary>
        OKLAB = 2,
    }

    // ponytail: one lock serializes samples and edits; immutable sorted snapshots can replace it if contention matters.
    private readonly object _gate = new();
    private (float Offset, Color Color)[] _points = [(0, Electron2D.Colors.Black), (1, Electron2D.Colors.White)];
    private bool _sorted = true;
    private InterpolationMode _mode;
    private ColorSpace _colorSpace;

    /// <summary>Creates the default black-to-white transition at offsets zero and one.</summary>
    public Gradient() { }

    /// <summary>Gets a copy of, or replaces, every offset in current storage order.</summary>
    /// <value>Initially [0, 1]. Any finite offsets are allowed, including values outside the unit interval.</value>
    /// <remarks>Resizes the point array, preserving existing colors and initializing new colors to opaque black.
    /// An empty array clears all points. Every assignment emits Changed; sorting remains lazy.</remarks>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentException">An offset is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float[] Offsets
    {
        get { lock (_gate) { ThrowIfDisposed(); return _points.Select(p => p.Offset).ToArray(); } }
        set
        {
            ArgumentNullException.ThrowIfNull(value); var copy = (float[])value.Clone();
            if (copy.Any(v => !float.IsFinite(v))) throw new ArgumentException("Gradient offsets must be finite.", nameof(value));
            lock (_gate) { ThrowIfDisposed(); Resize(copy.Length); for (var i = 0; i < copy.Length; i++) _points[i].Offset = copy[i]; _sorted = false; }
            EmitChanged();
        }
    }
    /// <summary>Gets a copy of, or replaces, every color in current storage order.</summary>
    /// <value>Initially opaque black and white. Finite HDR and signed channels are allowed.</value>
    /// <remarks>Resizes the point array, preserving existing offsets and assigning zero to new offsets.
    /// An empty array clears the gradient. Every assignment emits Changed.</remarks>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentException">A color is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Color[] Colors
    {
        get { lock (_gate) { ThrowIfDisposed(); return _points.Select(p => p.Color).ToArray(); } }
        set
        {
            ArgumentNullException.ThrowIfNull(value); var copy = (Color[])value.Clone();
            if (copy.Any(v => !v.IsFinite())) throw new ArgumentException("Gradient colors must be finite.", nameof(value));
            lock (_gate) { ThrowIfDisposed(); if (copy.Length > _points.Length) _sorted = false; Resize(copy.Length); for (var i = 0; i < copy.Length; i++) _points[i].Color = copy[i]; }
            EmitChanged();
        }
    }
    /// <summary>Gets or sets the interpolation algorithm.</summary>
    /// <value>Linear initially.</value>
    /// <remarks>Actual changes emit Changed followed by PropertyListChanged. Equal assignments are silent.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public InterpolationMode InterpolationMode
    {
        get { lock (_gate) { ThrowIfDisposed(); return _mode; } }
        set
        {
            lock (_gate) { ThrowIfDisposed(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_mode == value) return; _mode = value; }
            EmitChanged(); if (!IsDisposed) NotifyPropertyListChanged();
        }
    }
    /// <summary>Gets or sets the interpolation color space.</summary>
    /// <value>SRGB initially; Constant interpolation ignores the setting.</value>
    /// <remarks>Actual changes emit Changed; equal assignments are silent. Alpha is interpolated independently.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public ColorSpace InterpolationColorSpace
    {
        get { lock (_gate) { ThrowIfDisposed(); return _colorSpace; } }
        set
        {
            lock (_gate) { ThrowIfDisposed(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_colorSpace == value) return; _colorSpace = value; }
            EmitChanged();
        }
    }

    /// <summary>Appends a color point and marks its storage for lazy sorting.</summary>
    /// <param name="offset">Finite position; not clamped.</param>
    /// <param name="color">Finite color; not clamped.</param>
    /// <exception cref="ArgumentException">An argument is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void AddPoint(float offset, Color color)
    {
        lock (_gate) { ThrowIfDisposed(); Validate(offset, color); var index = _points.Length; Resize(index + 1); _points[index] = (offset, color); _sorted = false; }
        EmitChanged();
    }
    /// <summary>Removes a point by its current storage index without sorting first.</summary>
    /// <param name="point">An existing index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="InvalidOperationException">Only one point remains; use an empty bulk array to clear it.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void RemovePoint(int point)
    {
        lock (_gate)
        {
            Index(point); if (_points.Length == 1) throw new InvalidOperationException("The last point cannot be removed individually.");
            Array.Copy(_points, point + 1, _points, point, _points.Length - point - 1); Array.Resize(ref _points, _points.Length - 1);
        }
        EmitChanged();
    }
    /// <summary>Returns the point count without changing storage order.</summary>
    /// <returns>The count, possibly zero.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int GetPointCount() { lock (_gate) { ThrowIfDisposed(); return _points.Length; } }
    /// <summary>Returns a color after sorting by offset.</summary>
    /// <param name="point">An existing index in sorted order.</param>
    /// <returns>The stored nonlinear color.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Color GetColor(int point) { lock (_gate) { Index(point); Sort(); return _points[point].Color; } }
    /// <summary>Returns an offset after sorting.</summary>
    /// <param name="point">An existing index in sorted order.</param>
    /// <returns>The finite, unclamped offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public float GetOffset(int point) { lock (_gate) { Index(point); Sort(); return _points[point].Offset; } }
    /// <summary>Sorts the points and replaces one color, then emits Changed even for an equal value.</summary>
    /// <param name="point">An existing sorted index.</param>
    /// <param name="color">A finite color.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ArgumentException">The color is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetColor(int point, Color color) { lock (_gate) { Index(point); Validate(0, color); Sort(); _points[point].Color = color; } EmitChanged(); }
    /// <summary>Sorts the points and changes one offset, marking the next indexed/sample operation for sorting.</summary>
    /// <param name="point">An existing sorted index.</param>
    /// <param name="offset">A finite unclamped offset.</param>
    /// <remarks>Emits Changed even for an equal value.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ArgumentException">The offset is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void SetOffset(int point, float offset) { lock (_gate) { Index(point); Validate(offset, default); Sort(); _points[point].Offset = offset; _sorted = false; } EmitChanged(); }
    /// <summary>Mirrors offsets around 0.5, sorts and emits Changed, including for an empty gradient.</summary>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void Reverse()
    {
        lock (_gate) { ThrowIfDisposed(); for (var i = 0; i < _points.Length; i++) _points[i].Offset = 1 - _points[i].Offset; _sorted = false; Sort(); }
        EmitChanged();
    }
    /// <summary>Returns a sampled color, sorting lazily and holding the outer endpoint colors.</summary>
    /// <param name="offset">Finite position. Out-of-range positions use the first or last point.</param>
    /// <returns>Opaque black when empty, otherwise the interpolated color in nonlinear sRGB.</returns>
    /// <remarks>Offsets are compared with actual point positions; values outside 0..1 are not pre-clamped.
    /// Exact points return their original color. Equal-offset tie ordering is unspecified. Cubic interpolation
    /// may overshoot; derived IEEE overflow is retained. Warm sampling allocates no memory.</remarks>
    /// <exception cref="ArgumentException">The offset is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Color Sample(float offset) { lock (_gate) { ThrowIfDisposed(); Validate(offset, default); Sort(); return SampleCore(offset); } }

    private Color SampleCore(float offset)
    {
        if (_points.Length == 0) return Electron2D.Colors.Black;
        int low = 0, high = _points.Length - 1;
        while (low <= high)
        {
            var mid = low + (high - low) / 2;
            if (_points[mid].Offset > offset) high = mid - 1;
            else if (_points[mid].Offset < offset) low = mid + 1;
            else return _points[mid].Color;
        }
        if (high < 0) return _points[0].Color;
        if (low >= _points.Length) return _points[^1].Color;
        var a = _points[high]; var b = _points[low];
        if (_mode == InterpolationMode.Constant) return a.Color;
        var weight = (float)(((double)offset - a.Offset) / ((double)b.Offset - a.Offset));
        var first = Transform(a.Color); var second = Transform(b.Color);
        var color = first.Lerp(second, weight);
        if (_mode == InterpolationMode.Cubic)
        {
            var before = Transform(_points[Math.Max(high - 1, 0)].Color); var after = Transform(_points[Math.Min(low + 1, _points.Length - 1)].Color);
            color = new(Mathf.CubicInterpolate(first.R, second.R, before.R, after.R, weight), Mathf.CubicInterpolate(first.G, second.G, before.G, after.G, weight),
                Mathf.CubicInterpolate(first.B, second.B, before.B, after.B, weight), Mathf.CubicInterpolate(first.A, second.A, before.A, after.A, weight));
        }
        return _colorSpace switch { ColorSpace.LinearSRGB => color.LinearToSRGB(), ColorSpace.OKLAB => OkColor.FromOKLAB(color), _ => color };
    }
    private Color Transform(Color color) => _colorSpace switch { ColorSpace.LinearSRGB => color.SRGBToLinear(), ColorSpace.OKLAB => OkColor.ToOKLAB(color), _ => color };
    private void Resize(int size)
    {
        var previous = _points.Length; Array.Resize(ref _points, size);
        for (var i = previous; i < size; i++) _points[i].Color = Electron2D.Colors.Black;
    }
    private void Sort() { if (_sorted) return; Array.Sort(_points, static (a, b) => a.Offset.CompareTo(b.Offset)); _sorted = true; }
    private void Index(int point) { ThrowIfDisposed(); if ((uint)point >= (uint)_points.Length) throw new ArgumentOutOfRangeException(nameof(point)); }
    private static void Validate(float offset, Color color) { if (!float.IsFinite(offset) || !color.IsFinite()) throw new ArgumentException("Gradient values must be finite."); }

    internal TexturePixels BakeTexture(int width, int height, bool hdr, Func<int, int, float> offset, bool solidFill)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); Sort();
            if (solidFill && _points.Length <= 1)
            {
                using var solid = Image.CreateEmpty(width, height, false, hdr ? Image.Format.Rgbaf : Image.Format.Rgba8);
                solid.Fill(_points.Length == 0 ? Electron2D.Colors.Black : _points[0].Color);
                return TexturePixels.FromImage(solid);
            }
            var stride = hdr ? 16 : 4;
            var data = new byte[checked(width * height * stride)];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var color = SampleCore(offset(x, y)); var pixel = data.AsSpan((y * width + x) * stride);
                    if (hdr)
                    {
                        System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(pixel, color.R);
                        System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(pixel[4..], color.G);
                        System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(pixel[8..], color.B);
                        System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(pixel[12..], color.A);
                    }
                    else System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(pixel, color.ToABGR32());
                }
            using var image = Image.CreateFromData(width, height, false, hdr ? Image.Format.Rgbaf : Image.Format.Rgba8, data);
            return TexturePixels.FromImage(image);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<Gradient, float[]>(nameof(Offsets), g => g.Offsets, (g, v) => g.Offsets = v, _ => [0, 1]),
        new PropertyDescriptor<Gradient, Color[]>(nameof(Colors), g => g.Colors, (g, v) => g.Colors = v, _ => [Electron2D.Colors.Black, Electron2D.Colors.White]),
        new PropertyDescriptor<Gradient, InterpolationMode>(nameof(InterpolationMode), g => g.InterpolationMode, (g, v) => g.InterpolationMode = v, _ => InterpolationMode.Linear),
        new PropertyDescriptor<Gradient, ColorSpace>(nameof(InterpolationColorSpace), g => g.InterpolationColorSpace, (g, v) => g.InterpolationColorSpace = v, _ => ColorSpace.SRGB),
    ]);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new Gradient();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        (float Offset, Color Color)[] points; bool sorted; InterpolationMode mode; ColorSpace space;
        lock (_gate) { ThrowIfDisposed(); points = ((float Offset, Color Color)[])_points.Clone(); sorted = _sorted; mode = _mode; space = _colorSpace; }
        var copy = (Gradient)target;
        lock (copy._gate) { copy.ThrowIfDisposed(); copy._points = points; copy._sorted = sorted; copy._mode = mode; copy._colorSpace = space; }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_gate) _points = []; base.Dispose(disposing); }
}
