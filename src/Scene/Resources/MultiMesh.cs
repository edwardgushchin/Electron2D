namespace Electron2D;

/// <summary>Owns copied two-dimensional instance transforms, colors and shader data for a borrowed mesh.</summary>
/// <remarks>Configuration flags change only while InstanceCount is zero. Packed records contain eight transform
/// floats, followed by optional four-float color and custom-data channels. Resource operations serialize;
/// changes commit before Changed callbacks. Presentation snapshots are transient and do not enter copies.</remarks>
public sealed class MultiMesh : Resource
{
    /// <summary>Selects the interpolation of two-dimensional instance bases.</summary>
    public enum PhysicsInterpolationQuality
    {
        /// <summary>Interpolate each basis and translation component linearly.</summary>
        Fast = 0,
        /// <summary>Interpolate rotation, scale and skew for nonsingular compatible bases; otherwise interpolate components.</summary>
        High = 1
    }
    internal readonly object Gate = new();
    private Mesh? _mesh;
    private int _count, _visible = -1;
    private bool _colors, _custom, _inTick, _interpolationEnabled;
    private float[] _buffer = [], _previous = [];
    private PhysicsInterpolationQuality _quality;
    private RID _rid;
    private Rect2 _customAABB;
    internal int Stride => 8 + (_colors ? 4 : 0) + (_custom ? 4 : 0);
    internal int DrawCount => _visible < 0 ? _count : Math.Min(_visible, _count);
    /// <summary>Creates an empty collection with no optional channels and all instances visible.</summary>
    public MultiMesh() { }
    /// <summary>Gets or sets the borrowed mesh drawn by every visible instance.</summary>
    /// <value>Null initially; changing the reference does not reset instance data.</value>
    /// <exception cref="ObjectDisposedException">This resource or the assigned mesh is disposed.</exception>
    public Mesh? Mesh
    {
        get { lock (Gate) { ThrowIfDisposed(); return _mesh; } }
        set { lock (Gate) { ThrowIfDisposed(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(_mesh, value)) return; _mesh = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets a manual local visibility rectangle for the whole instance resource.</summary>
    /// <value>The zero rectangle selects automatic bounds; other finite nonnegative rectangles override computation.</value>
    /// <remarks>Bounds are expressed in this resource's local coordinates and participate in native canvas culling.</remarks>
    /// <exception cref="ArgumentException">Bounds are nonfinite, negative-sized or have an unrepresentable end.</exception>
    public Rect2 CustomAABB
    {
        get { lock (Gate) { ThrowIfDisposed(); return _customAABB; } }
        set { lock (Gate) { ThrowIfDisposed(); if (!value.IsFinite() || !value.End.IsFinite() || value.Size.X < 0 || value.Size.Y < 0) throw new ArgumentException("Instance bounds must be finite nonnegative rectangles.", nameof(value)); if (_customAABB == value) return; _customAABB = value; } EmitChanged(); }
    }
    /// <summary>Gets the local visibility rectangle of the visible instance prefix.</summary>
    /// <returns>The manual rectangle or merged transformed mesh bounds, empty without visible geometry.</returns>
    public Rect2 GetAABB() { lock (Gate) { ThrowIfDisposed(); return GetPresentationBounds(1); } }
    internal Rect2 GetPresentationBounds(float fraction)
    {
        if (_customAABB != default) return _customAABB;
        var mesh = _mesh; var count = DrawCount; if (mesh is null || count == 0) return default;
        var revision = ChangeRevision; var surfaces = mesh.GetSurfaceCount(); var local = surfaces == 0 ? default : mesh.GetAABB();
        ThrowIfDisposed(); if (ChangeRevision != revision) throw new InvalidOperationException("Mesh callbacks changed instance storage during bounds preparation.");
        if (surfaces == 0) return default;
        var result = default(Rect2);
        for (var i = 0; i < count; i++) { Presentation(i, fraction, out var pose, out _, out _); var bounds = pose * local; result = i == 0 ? bounds : result.Merge(bounds); }
        if (!result.IsFinite() || !result.End.IsFinite()) throw new ArithmeticException("Instance bounds exceed finite rectangles.");
        return result;
    }
    /// <summary>Gets or sets the allocated instance count.</summary>
    /// <value>Zero initially. A different count clears both packed snapshots; equal writes preserve data.</value>
    /// <remarks>Allocation is explicit cold work. Shrinking clamps an authored visible count to the new capacity.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative or packed storage exceeds managed array limits.</exception>
    public int InstanceCount
    {
        get { lock (Gate) { ThrowIfDisposed(); return _count; } }
        set
        {
            bool changed; lock (Gate) changed = AllocateCore(value, _colors, _custom);
            if (changed) EmitChanged();
        }
    }
    /// <summary>Gets or sets the visible prefix without reallocating packed storage.</summary>
    /// <value>Minus one draws every instance; otherwise zero through InstanceCount.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is less than minus one or greater than capacity.</exception>
    public int VisibleInstanceCount
    {
        get { lock (Gate) { ThrowIfDisposed(); return _visible; } }
        set { lock (Gate) { ThrowIfDisposed(); if (value < -1 || value > _count) throw new ArgumentOutOfRangeException(nameof(value)); if (_visible == value) return; _visible = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets whether packed records include instance color multipliers.</summary>
    /// <value>False initially; configure before allocating instances.</value>
    /// <exception cref="InvalidOperationException">Instances are allocated.</exception>
    public bool UseColors
    {
        get { lock (Gate) { ThrowIfDisposed(); return _colors; } }
        set { lock (Gate) { ThrowIfDisposed(); RequireEmpty(); if (_colors == value) return; _colors = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets whether records include four raw floating shader components.</summary>
    /// <value>False initially. Shader data is independent of color multiplication.</value>
    /// <exception cref="InvalidOperationException">Instances are allocated.</exception>
    public bool UseCustomData
    {
        get { lock (Gate) { ThrowIfDisposed(); return _custom; } }
        set { lock (Gate) { ThrowIfDisposed(); RequireEmpty(); if (_custom == value) return; _custom = value; } EmitChanged(); }
    }
    /// <summary>Gets or sets the two-dimensional basis interpolation policy.</summary>
    /// <value>Fast initially; High retains angular motion where finite compatible bases allow decomposition.</value>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined.</exception>
    public PhysicsInterpolationQuality PhysicsInterpolationQualityMode
    {
        get { lock (Gate) { ThrowIfDisposed(); return _quality; } }
        set { lock (Gate) { ThrowIfDisposed(); if (value is < PhysicsInterpolationQuality.Fast or > PhysicsInterpolationQuality.High) throw new ArgumentOutOfRangeException(nameof(value)); if (_quality == value) return; _quality = value; } EmitChanged(); }
    }
    /// <summary>Gets a copied packed buffer or assigns a whole copied finite buffer.</summary>
    /// <value>Records store X.X, Y.X, zero padding, Origin.X, X.Y, Y.Y, zero padding, Origin.Y, then optional RGBA channels.</value>
    /// <remarks>Caller-supplied padding is retained. Outside a physics tick, ordinary writes reset their previous values.
    /// Use SetBufferInterpolated to supply an explicit presentation pair.</remarks>
    /// <exception cref="ArgumentException">The size differs from capacity times stride or any float is nonfinite.</exception>
    public float[] Buffer
    {
        get { lock (Gate) { ThrowIfDisposed(); return (float[])_buffer.Clone(); } }
        set { ArgumentNullException.ThrowIfNull(value); SetBuffer(value); }
    }
    internal void SetBuffer(ReadOnlySpan<float> values)
    {
        lock (Gate) { ThrowIfDisposed(); ValidateBuffer(values); values.CopyTo(_buffer); CommitRange(0, _buffer.Length); }
        EmitChanged();
    }
    /// <summary>Copies coherent current and previous packed records for interpolated rendering.</summary>
    /// <param name="bufferCurrent">Whole finite current buffer.</param>
    /// <param name="bufferPrevious">Whole finite previous buffer of the same schema.</param>
    /// <exception cref="ArgumentException">A buffer is malformed; both snapshots remain unchanged.</exception>
    public void SetBufferInterpolated(ReadOnlySpan<float> bufferCurrent, ReadOnlySpan<float> bufferPrevious)
    {
        lock (Gate) { ThrowIfDisposed(); ValidateBuffer(bufferCurrent); ValidateBuffer(bufferPrevious); bufferCurrent.CopyTo(_buffer); bufferPrevious.CopyTo(_previous); _interpolationEnabled = true; }
        EmitChanged();
    }
    /// <summary>Returns the current stored local transform for one instance.</summary>
    /// <param name="instance">Existing zero-based instance index.</param>
    /// <returns>The un-interpolated two-dimensional transform.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside allocated storage.</exception>
    public Transform GetInstanceTransform2D(int instance) { lock (Gate) return ReadTransform(_buffer, Offset(instance)); }
    /// <summary>Commits a finite local transform for one instance.</summary>
    /// <param name="instance">Existing zero-based index.</param>
    /// <param name="transform">Finite basis and translation; singular and reflected bases are accepted.</param>
    /// <exception cref="ArgumentException">The transform is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside storage.</exception>
    public void SetInstanceTransform2D(int instance, Transform transform)
    {
        lock (Gate) { var offset = Offset(instance); if (!transform.IsFinite()) throw new ArgumentException("Instance transform must be finite.", nameof(transform)); WriteTransform(_buffer, offset, transform); CommitRange(offset, 8); }
        EmitChanged();
    }
    /// <summary>Gets one stored instance color multiplier.</summary>
    /// <param name="instance">Existing zero-based index.</param>
    /// <returns>Four raw finite color components.</returns>
    /// <exception cref="InvalidOperationException">The color channel is disabled.</exception>
    public Color GetInstanceColor(int instance) { lock (Gate) return ReadColor(_buffer, Channel(instance, custom: false)); }
    /// <summary>Sets one finite instance color multiplier.</summary>
    /// <param name="instance">Existing zero-based index.</param>
    /// <param name="color">Raw multiplier; components are not clamped in CPU storage.</param>
    /// <exception cref="InvalidOperationException">The color channel is disabled.</exception>
    /// <exception cref="ArgumentException">A component is nonfinite.</exception>
    public void SetInstanceColor(int instance, Color color) => SetColor(instance, color, custom: false);
    /// <summary>Gets one stored four-component shader value.</summary>
    /// <param name="instance">Existing zero-based index.</param>
    /// <returns>Raw shader data represented by Color without color conversion.</returns>
    /// <exception cref="InvalidOperationException">The custom channel is disabled.</exception>
    public Color GetInstanceCustomData(int instance) { lock (Gate) return ReadColor(_buffer, Channel(instance, custom: true)); }
    /// <summary>Sets one finite four-component shader value.</summary>
    /// <param name="instance">Existing zero-based index.</param>
    /// <param name="customData">Raw components, independent of the color multiplier.</param>
    /// <exception cref="InvalidOperationException">The custom channel is disabled.</exception>
    /// <exception cref="ArgumentException">A component is nonfinite.</exception>
    public void SetInstanceCustomData(int instance, Color customData) => SetColor(instance, customData, custom: true);
    /// <summary>Gets or sets copied legacy packed transform columns.</summary>
    /// <value>Three Vector2 columns per instance: X, Y and Origin. An empty assignment is a no-op.</value>
    /// <exception cref="ArgumentException">A nonempty array has a different count or nonfinite column.</exception>
    public Vector2[] Transform2DArray
    {
        get { lock (Gate) { ThrowIfDisposed(); var result = new Vector2[_count * 3]; for (var i = 0; i < _count; i++) { var t = ReadTransform(_buffer, i * Stride); result[i * 3] = t.X; result[i * 3 + 1] = t.Y; result[i * 3 + 2] = t.Origin; } return result; } }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            lock (Gate)
            {
                ThrowIfDisposed(); if (value.Length == 0) return; if (value.Length != _count * 3) throw new ArgumentException("Transform column count differs from instance storage.", nameof(value));
                foreach (var column in value) if (!column.IsFinite()) throw new ArgumentException("Transform columns must be finite.", nameof(value));
                for (var i = 0; i < _count; i++) { WriteTransform(_buffer, i * Stride, new(value[i * 3], value[i * 3 + 1], value[i * 3 + 2])); CommitRange(i * Stride, 8); }
            }
            EmitChanged();
        }
    }
    /// <summary>Gets or sets copied legacy instance color values.</summary>
    /// <value>An empty array when the channel is disabled. Empty assignments are no-ops.</value>
    public Color[] ColorArray { get => GetColors(custom: false); set => SetColors(value, custom: false); }
    /// <summary>Gets or sets copied legacy four-component shader values.</summary>
    /// <value>An empty array when the channel is disabled. Empty assignments are no-ops.</value>
    public Color[] CustomDataArray { get => GetColors(custom: true); set => SetColors(value, custom: true); }
    /// <summary>Prevents interpolation for one instance by copying its current record to the previous snapshot.</summary>
    /// <param name="instance">Existing zero-based index.</param>
    public void ResetInstancePhysicsInterpolation(int instance) { lock (Gate) { var offset = Offset(instance); _buffer.AsSpan(offset, Stride).CopyTo(_previous.AsSpan(offset)); } }
    /// <summary>Resets all presentation records to current values without editing logical data.</summary>
    public void ResetInstancesPhysicsInterpolation() { lock (Gate) { ThrowIfDisposed(); _buffer.CopyTo(_previous, 0); } }
    /// <summary>Gets the stable borrowed resource identity.</summary>
    /// <returns>A weak logical RID valid until resource disposal, independently of a renderer.</returns>
    public override RID GetRID() { lock (Gate) { ThrowIfDisposed(); return _rid.IsValid() ? _rid : _rid = RenderingMultiMeshRegistry.Register(this); } }
    internal void Allocate(int count, bool colors, bool custom)
    {
        bool changed; lock (Gate) changed = AllocateCore(count, colors, custom);
        if (changed) EmitChanged();
    }
    private bool AllocateCore(int count, bool colors, bool custom)
    {
        ThrowIfDisposed(); ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (_count == count && _colors == colors && _custom == custom) return false;
        var stride = 8 + (colors ? 4 : 0) + (custom ? 4 : 0); var length = (long)count * stride;
        if (length > Array.MaxLength) throw new ArgumentOutOfRangeException(nameof(count));
        var buffer = new float[(int)length]; var previous = new float[(int)length];
        _buffer = buffer; _previous = previous; _count = count; _colors = colors; _custom = custom; _visible = Math.Min(_visible, count);
        return true;
    }
    internal RID RegisterOwned(RenderingServer owner) { lock (Gate) { ThrowIfDisposed(); return _rid = RenderingMultiMeshRegistry.Register(this, owner); } }
    private void RequireEmpty() { if (_count != 0) throw new InvalidOperationException("Instance channel flags require zero allocated instances."); }
    private int Offset(int instance) { ThrowIfDisposed(); if ((uint)instance >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(instance)); return instance * Stride; }
    private int Channel(int instance, bool custom) { var offset = Offset(instance); if (custom ? !_custom : !_colors) throw new InvalidOperationException("The requested instance channel is disabled."); return offset + 8 + (custom && _colors ? 4 : 0); }
    private void ValidateBuffer(ReadOnlySpan<float> values) { if (values.Length != _buffer.Length) throw new ArgumentException("Packed instance buffer size differs from allocated storage.", nameof(values)); foreach (var value in values) if (!float.IsFinite(value)) throw new ArgumentException("Instance buffers must be finite.", nameof(values)); }
    private void CommitRange(int offset, int count) { if (!_inTick) _buffer.AsSpan(offset, count).CopyTo(_previous.AsSpan(offset)); }
    private void SetColor(int instance, Color value, bool custom)
    {
        lock (Gate) { var offset = Channel(instance, custom); if (!value.IsFinite()) throw new ArgumentException("Instance components must be finite.", nameof(value)); WriteColor(_buffer, offset, value); CommitRange(offset, 4); }
        EmitChanged();
    }
    private Color[] GetColors(bool custom) { lock (Gate) { ThrowIfDisposed(); if (custom ? !_custom : !_colors) return []; var result = new Color[_count]; for (var i = 0; i < _count; i++) result[i] = ReadColor(_buffer, i * Stride + 8 + (custom && _colors ? 4 : 0)); return result; } }
    private void SetColors(Color[] values, bool custom)
    {
        ArgumentNullException.ThrowIfNull(values);
        lock (Gate)
        {
            ThrowIfDisposed(); if (values.Length == 0) return; if (values.Length != _count) throw new ArgumentException("Color count differs from instance storage.", nameof(values));
            Channel(0, custom); foreach (var value in values) if (!value.IsFinite()) throw new ArgumentException("Instance components must be finite.", nameof(values));
            for (var i = 0; i < _count; i++) { var offset = i * Stride + 8 + (custom && _colors ? 4 : 0); WriteColor(_buffer, offset, values[i]); CommitRange(offset, 4); }
        }
        EmitChanged();
    }
    internal static Transform ReadTransform(float[] values, int offset) => new(new(values[offset], values[offset + 4]), new(values[offset + 1], values[offset + 5]), new(values[offset + 3], values[offset + 7]));
    private static void WriteTransform(float[] values, int offset, Transform pose) { values[offset] = pose.X.X; values[offset + 1] = pose.Y.X; values[offset + 2] = 0; values[offset + 3] = pose.Origin.X; values[offset + 4] = pose.X.Y; values[offset + 5] = pose.Y.Y; values[offset + 6] = 0; values[offset + 7] = pose.Origin.Y; }
    private static Color ReadColor(float[] values, int offset) => new(values[offset], values[offset + 1], values[offset + 2], values[offset + 3]);
    private static void WriteColor(float[] values, int offset, Color value) { values[offset] = value.R; values[offset + 1] = value.G; values[offset + 2] = value.B; values[offset + 3] = value.A; }
    internal void SetInterpolationEnabled(bool enabled)
    {
        lock (Gate) { ThrowIfDisposed(); if (_interpolationEnabled == enabled) return; _interpolationEnabled = enabled; _buffer.CopyTo(_previous, 0); }
    }
    internal void PhysicsTick(bool start) { lock (Gate) { if (IsDisposed) return; if (start && _interpolationEnabled) _buffer.CopyTo(_previous, 0); _inTick = start && _interpolationEnabled; } }
    internal void Presentation(int instance, float fraction, out Transform pose, out Color color, out Color custom)
    {
        var offset = instance * Stride; fraction = _interpolationEnabled ? Math.Clamp(fraction, 0, 1) : 1;
        var current = ReadTransform(_buffer, offset); var previous = ReadTransform(_previous, offset);
        if (fraction == 1 || current == previous) pose = current;
        else if (fraction == 0) pose = previous;
        else if (_quality == PhysicsInterpolationQuality.High && CanDecompose(previous) && CanDecompose(current) && MathF.Sign(previous.Determinant()) == MathF.Sign(current.Determinant()))
        {
            pose = previous.InterpolateWith(current, fraction); pose.Origin = Blend(previous.Origin, current.Origin, fraction);
        }
        else pose = new(Blend(previous.X, current.X, fraction), Blend(previous.Y, current.Y, fraction), Blend(previous.Origin, current.Origin, fraction));
        color = _colors ? Blend(ReadColor(_previous, offset + 8), ReadColor(_buffer, offset + 8), fraction) : Colors.White;
        var channel = offset + 8 + (_colors ? 4 : 0);
        custom = _custom ? Blend(ReadColor(_previous, channel), ReadColor(_buffer, channel), fraction) : new(0, 0, 0, 0);
        if (!pose.IsFinite() || !color.IsFinite() || !custom.IsFinite()) throw new ArithmeticException("Instance interpolation exceeded finite presentation values.");
    }
    private static bool CanDecompose(Transform value) => float.IsFinite(value.Determinant()) && value.Determinant() != 0 && value.Scale.IsFinite() && float.IsFinite(value.Skew);
    private static float Blend(float previous, float current, float fraction) => (float)((1 - (double)fraction) * previous + (double)fraction * current);
    private static Vector2 Blend(Vector2 previous, Vector2 current, float fraction) => new(Blend(previous.X, current.X, fraction), Blend(previous.Y, current.Y, fraction));
    private static Color Blend(Color previous, Color current, float fraction) => new(Blend(previous.R, current.R, fraction), Blend(previous.G, current.G, fraction), Blend(previous.B, current.B, fraction), Blend(previous.A, current.A, fraction));
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new MultiMesh();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var other = (MultiMesh)target; Mesh? mesh; float[] buffer; int count, visible; bool colors, custom; PhysicsInterpolationQuality quality; Rect2 bounds;
        lock (Gate) { ThrowIfDisposed(); mesh = _mesh; buffer = (float[])_buffer.Clone(); count = _count; visible = _visible; colors = _colors; custom = _custom; quality = _quality; bounds = _customAABB; }
        mesh = (Mesh?)duplicateSubresource(mesh);
        lock (other.Gate) { other.ThrowIfDisposed(); other._mesh = mesh; other._buffer = buffer; other._previous = (float[])buffer.Clone(); other._count = count; other._visible = visible; other._colors = colors; other._custom = custom; other._quality = quality; other._customAABB = bounds; }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        lock (Gate) { if (_rid.IsValid()) RenderingMultiMeshRegistry.Remove(_rid); _rid = default; _mesh = null; _buffer = _previous = []; _inTick = false; }
        base.Dispose(disposing);
    }
}
