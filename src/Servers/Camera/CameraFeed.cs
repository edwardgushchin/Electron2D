namespace Electron2D;

/// <summary>Describes one camera source format without exposing native backend identifiers.</summary>
/// <param name="Width">Positive frame width in pixels.</param><param name="Height">Positive frame height in pixels.</param>
/// <param name="PixelFormat">Source format name, distinct from the converted output image format.</param>
/// <param name="FPSNumerator">Nonnegative rational frame-rate numerator; zero means unknown.</param>
/// <param name="FPSDenominator">Positive rational frame-rate denominator.</param>
public readonly record struct CameraFeedFormat(int Width, int Height, string PixelFormat, int FPSNumerator, int FPSDenominator);

/// <summary>Selects the defined camera conversion output.</summary>
public enum CameraFeedOutput
{
    /// <summary>Convert to full-color RGBA pixels.</summary>
    RGB,
    /// <summary>Publish Y and interleaved CbCr planes from a YUYV source.</summary>
    Separate,
    /// <summary>Publish the YUYV luminance as an opaque grayscale image.</summary>
    Grayscale,
    /// <summary>Copy packed YUYV bytes into a two-channel image.</summary>
    Copy
}

/// <summary>Owns a camera source and its copied image planes; consumers borrow it through CameraServer.</summary>
/// <remarks>Settings and hooks require the creating thread. Image submission may run on producer threads.
/// Process delivers coalesced format then frame events on the owner. Remove a caller-owned registered feed before disposing it.
/// External native image import has a separate ownership prerequisite.</remarks>
public class CameraFeed : ElectronObject
{
    /// <summary>Identifies the published pixel-plane arrangement.</summary>
    public enum FeedDataType
    {
        /// <summary>No image has been published.</summary>
        NoImage = 0,
        /// <summary>RGB or RGBA pixels.</summary>
        RGB = 1,
        /// <summary>Packed YCbCr pixels requiring consumer conversion.</summary>
        YCBCR = 2,
        /// <summary>Separate Y and interleaved CbCr planes requiring consumer conversion.</summary>
        YCBCRSeparate = 3
    }
    /// <summary>Describes the camera's physical position.</summary>
    public enum FeedPosition
    {
        /// <summary>Position is unknown.</summary>
        Unspecified = 0,
        /// <summary>Front-facing camera.</summary>
        Front = 1,
        /// <summary>Back-facing camera.</summary>
        Back = 2
    }
    private static int _nextID;
    private readonly int _id = Interlocked.Increment(ref _nextID);
    internal readonly int OwnerThread = Environment.CurrentManagedThreadId;
    private readonly object _gate = new();
    private TexturePixels? _first, _second;
    private FeedDataType _dataType = FeedDataType.RGB;
    private string _name = "???";
    private FeedPosition _position;
    private Transform _transform = new(1, 0, 0, -1, 0, 1);
    private bool _active, _calling, _emitting, _framePending, _formatPending;
    private EventPublisher<Action> _frameChanged, _formatChanged;

    /// <summary>Creates a caller-owned inactive source with no pixels.</summary>
    public CameraFeed() { if (_id <= 0) throw new InvalidOperationException("Camera feed identity space is exhausted."); }
    internal CameraFeed(FeedDataType initial) : this() => _dataType = initial;
    internal void CheckOwner()
    {
        ThrowIfDisposed();
        if (Environment.CurrentManagedThreadId != OwnerThread) throw new InvalidOperationException("Camera feed operations require the creating thread.");
    }
    private void CheckMutation() { CheckOwner(); if (_calling) throw new InvalidOperationException("Camera source hooks cannot recursively reenter source operations."); }
    /// <summary>Returns the process-unique positive source identity.</summary><returns>The same identity throughout this feed's lifetime.</returns>
    public int GetID() { ThrowIfDisposed(); return _id; }
    /// <summary>Returns the source's display name.</summary><returns>Initially three question marks.</returns>
    public string GetName() { lock (_gate) { ThrowIfDisposed(); return _name; } }
    /// <summary>Replaces the source's display name.</summary><param name="name">A nonnull display label.</param>
    public void SetName(string name) { CheckMutation(); ArgumentNullException.ThrowIfNull(name); lock (_gate) _name = name; }
    /// <summary>Returns the physical camera position.</summary><returns>Unspecified initially.</returns>
    public FeedPosition GetPosition() { lock (_gate) { ThrowIfDisposed(); return _position; } }
    /// <summary>Replaces the physical camera position.</summary><param name="position">A defined position.</param>
    public void SetPosition(FeedPosition position) { CheckMutation(); if (!Enum.IsDefined(position)) throw new ArgumentOutOfRangeException(nameof(position)); lock (_gate) _position = position; }
    /// <summary>Returns the current pixel arrangement.</summary><returns>RGB initially, even before a first image.</returns>
    public FeedDataType GetDatatype() { lock (_gate) { ThrowIfDisposed(); return _dataType; } }
    /// <summary>Gets or changes source activation through the typed hooks.</summary>
    /// <value>False initially. A rejected activation remains false. Deactivation commits false even if cleanup throws.</value>
    public bool FeedIsActive
    {
        get { lock (_gate) { ThrowIfDisposed(); return _active; } }
        set
        {
            CheckMutation(); if (value == FeedIsActive) return;
            _calling = true;
            try
            {
                if (value) { if (OnActivateFeed()) lock (_gate) _active = true; }
                else { lock (_gate) _active = false; OnDeactivateFeed(); }
            }
            finally { _calling = false; }
        }
    }
    /// <summary>Gets or sets finite normalized image-coordinate metadata.</summary>
    /// <value>Initially flips the vertical coordinate and translates it by one. Ordinary Texture drawing does not apply it implicitly.</value>
    public Transform FeedTransform
    {
        get { lock (_gate) { ThrowIfDisposed(); return _transform; } }
        set { CheckMutation(); if (!value.IsFinite()) throw new ArgumentException("Camera transform must be finite.", nameof(value)); lock (_gate) _transform = value; }
    }
    /// <summary>Gets a copied list of supported formats from the source hook.</summary><value>Empty for a plain authored-image source.</value>
    public CameraFeedFormat[] Formats
    {
        get
        {
            CheckMutation(); _calling = true;
            try
            {
                var formats = OnGetFormats() ?? throw new InvalidOperationException("Camera format hook returned null.");
                if (formats.Length > 4096) throw new InvalidOperationException("Camera format count exceeds 4096.");
                foreach (var f in formats)
                    if (f.Width is <= 0 or > 16384 || f.Height is <= 0 or > 16384 || string.IsNullOrEmpty(f.PixelFormat) || f.FPSNumerator < 0 || f.FPSDenominator <= 0)
                        throw new InvalidOperationException("Camera hook returned invalid format metadata.");
                return (CameraFeedFormat[])formats.Clone();
            }
            finally { _calling = false; }
        }
    }
    /// <summary>Selects a supported source format while inactive.</summary>
    /// <param name="index">Index in Formats.</param><param name="output">Defined conversion output; RGB initially.</param>
    /// <returns>Whether the source accepts the format and conversion.</returns>
    /// <exception cref="InvalidOperationException">The source is active or a hook is reentered.</exception>
    public bool SetFormat(int index, CameraFeedOutput output = CameraFeedOutput.RGB)
    {
        CheckMutation(); if (FeedIsActive) throw new InvalidOperationException("Deactivate the camera before selecting a format.");
        if (!Enum.IsDefined(output)) throw new ArgumentOutOfRangeException(nameof(output));
        if ((uint)index >= (uint)Formats.Length) throw new ArgumentOutOfRangeException(nameof(index));
        _calling = true; try { return OnSetFormat(index, output); } finally { _calling = false; }
    }
    /// <summary>Starts this source; the plain authored-image source accepts activation.</summary><returns>True if activation succeeds.</returns>
    protected virtual bool OnActivateFeed() => true;
    /// <summary>Stops this source and releases its active native state.</summary>
    protected virtual void OnDeactivateFeed() { }
    /// <summary>Supplies supported source-format metadata.</summary><returns>Source-owned metadata, copied by Formats.</returns>
    protected virtual CameraFeedFormat[] OnGetFormats() => [];
    /// <summary>Selects one supported source format and conversion.</summary><param name="index">Validated format index.</param><param name="output">Validated conversion output.</param><returns>False when unsupported.</returns>
    protected virtual bool OnSetFormat(int index, CameraFeedOutput output) => false;
    /// <summary>Submits copied color pixels; an inactive source ignores a valid image.</summary><param name="image">Nonempty supported texture image, bounded to 64 MiB including upload storage.</param>
    public void SetRGBImage(Image image) => Submit(Plane(image), null, FeedDataType.RGB);
    /// <summary>Submits copied packed YCbCr pixels for consumer-side conversion.</summary><param name="image">Nonempty supported packed image; its channel interpretation remains with the producer and consumer.</param>
    public void SetYCBCRImage(Image image) => Submit(Plane(image), null, FeedDataType.YCBCR);
    /// <summary>Atomically submits copied luminance and interleaved CbCr planes.</summary><param name="yImage">Nonempty supported luminance image.</param><param name="cbcrImage">Nonempty supported chroma image; sampling dimensions may differ.</param>
    public void SetYCBCRImages(Image yImage, Image cbcrImage)
    {
        var y = Plane(yImage); var cbcr = Plane(cbcrImage);
        if ((long)y.Source.Data.Length + y.Upload.Data.Length + cbcr.Source.Data.Length + cbcr.Upload.Data.Length > 67108864) throw new ArgumentException("Camera planes exceed the 64 MiB storage budget.");
        Submit(y, cbcr, FeedDataType.YCBCRSeparate);
    }
    private TexturePixels Plane(Image image)
    {
        ThrowIfDisposed(); return TexturePixels.FromImage(image, 67108864);
    }
    internal void Submit(TexturePixels first, TexturePixels? second, FeedDataType type)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (!_active) return;
            _formatPending |= _dataType != type || !SameFormat(_first, first) || !SameFormat(_second, second);
            _first = first; _second = second; _dataType = type; _framePending = true;
        }
    }
    internal void SubmitPrepared(TexturePixels first, TexturePixels? second, FeedDataType type)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (!_active) return;
            var changed = _dataType != type || !SameFormat(_first, first) || !SameFormat(_second, second);
            var a = _first is { Mutable: true } && SameFormat(_first, first) ? _first : TexturePixels.PreparedPlane(first.Source.Width, first.Source.Height, first.Source.Format);
            var b = second is null ? null : _second is { Mutable: true } && SameFormat(_second, second) ? _second : TexturePixels.PreparedPlane(second.Source.Width, second.Source.Height, second.Source.Format);
            Copy(first, a); if (second is not null) Copy(second, b!);
            _first = a; _second = b; _dataType = type; _formatPending |= changed; _framePending = true;
        }
        static void Copy(TexturePixels source, TexturePixels target)
        {
            source.Source.Data.CopyTo(target.Source.Data, 0); source.Upload.Data.CopyTo(target.Upload.Data, 0); unchecked { target.Revision++; }
        }
    }
    private static bool SameFormat(TexturePixels? a, TexturePixels? b) => a is null ? b is null : b is not null && a.Source.Width == b.Source.Width && a.Source.Height == b.Source.Height && a.Source.Format == b.Source.Format && a.Source.HasMIPMaps == b.Source.HasMIPMaps;
    internal TexturePixels? Pixels(CameraServer.FeedImage image) { lock (_gate) { ThrowIfDisposed(); return (int)image == 0 ? _first : _second; } }
    internal Image? CopyImage(CameraServer.FeedImage image) { lock (_gate) { ThrowIfDisposed(); return ((int)image == 0 ? _first : _second)?.CopyImage(); } }
    internal Vector2i Dimensions { get { lock (_gate) { ThrowIfDisposed(); return _first is { } p ? new(p.Source.Width, p.Source.Height) : Vector2i.Zero; } } }
    /// <summary>Runs on the creating thread when new image geometry or plane layout has been published.</summary>
    public event Action? FormatChanged { add => _formatChanged.Add(value); remove => _formatChanged.Remove(value); }
    /// <summary>Runs on the creating thread when one or more new frames have been published.</summary>
    public event Action? FrameChanged { add => _frameChanged.Add(value); remove => _frameChanged.Remove(value); }
    internal void Deliver()
    {
        bool format, frame;
        lock (_gate) { if (IsDisposed) return; format = _formatPending; frame = _framePending; _formatPending = _framePending = false; }
        List<Exception>? errors = null; _emitting = true;
        try
        {
            if (format) EmitAll(_formatChanged.Handlers, ref errors);
            if (frame) EmitAll(_frameChanged.Handlers, ref errors);
        }
        finally { _emitting = false; }
        Node.ThrowCollected("Camera feed callbacks failed.", errors);
    }
    private void EmitAll(Action? callbacks, ref List<Exception>? errors)
    {
        callbacks = BeginEventEmission(callbacks);
        foreach (var callback in Delegate.EnumerateInvocationList(callbacks))
            try { callback(); } catch (Exception error) { Node.CollectException(ref errors, error); }
    }
    internal virtual void Poll() { }
    internal bool Emitting => _emitting;
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<CameraFeed, bool>(nameof(FeedIsActive), f => f.FeedIsActive, (f, v) => f.FeedIsActive = v, _ => false),
        new PropertyDescriptor<CameraFeed, Transform>(nameof(FeedTransform), f => f.FeedTransform, (f, v) => f.FeedTransform = v, _ => new(1, 0, 0, -1, 0, 1)),
        new PropertyDescriptor<CameraFeed, CameraFeedFormat[]>(nameof(Formats), f => f.Formats)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override void ValidateMutation() { CheckMutation(); base.ValidateMutation(); }
    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        CheckMutation();
        if (_emitting) throw new InvalidOperationException("Cannot dispose a camera source during its event emission.");
        if (CameraServer.Contains(this)) throw new InvalidOperationException("Remove the registered camera feed before disposal.");
        base.ValidateDisposal();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try { if (_active) { _active = false; OnDeactivateFeed(); } }
        finally { lock (_gate) { _first = _second = null; _framePending = _formatPending = false; } _frameChanged.Handlers = _formatChanged.Handlers = null; base.Dispose(disposing); }
    }
}
