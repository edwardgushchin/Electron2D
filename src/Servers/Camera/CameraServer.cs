using SDL3;

namespace Electron2D;

/// <summary>Discovers native cameras and retains borrowed custom image feeds.</summary>
/// <remarks>Static operations use one retained service. Registry, monitoring and Process require their first owner's thread.
/// Monitoring enumerates devices without activating them. Native feeds belong to the service; custom feeds remain caller-owned.</remarks>
public sealed class CameraServer : ElectronObject
{
    /// <summary>Selects a camera image plane; the first-plane names intentionally share zero.</summary>
    public enum FeedImage
    {
        /// <summary>The ordinary RGB/RGBA image.</summary>
        RGBAImage = 0,
        /// <summary>The packed YCbCr image.</summary>
        YCBCRImage = 0,
        /// <summary>The separate luminance image.</summary>
        YImage = 0,
        /// <summary>The separate interleaved chroma image.</summary>
        CBCRImage = 1
    }
    private static readonly CameraServer Shared = new();
    private readonly List<CameraFeed> _feeds = [], _batch = [];
    private readonly List<WeakReference<CameraTexture>> _views = [];
    private readonly object _feedGate = new(), _viewGate = new();
    private readonly List<CameraTexture> _viewBatch = [];
    private int _nextViewSweep = 64;
    private bool _changingRegistry;
    private int _owner;
    private bool _monitoring, _processing, _discovering;
    private long _nextDiscovery;
    private EventPublisher<Action<int>> _added, _removed;
    private EventPublisher<Action> _updated;
    private CameraServer() { }
    internal static CameraServer Service => Shared;
    private void EnsureOwner()
    {
        var thread = Environment.CurrentManagedThreadId;
        Interlocked.CompareExchange(ref _owner, thread, 0);
        if (_owner != thread) throw new InvalidOperationException("CameraServer requires its owner thread.");
    }
    /// <summary>Gets or changes native-device discovery; disabling it retires all service-owned feeds.</summary>
    /// <value>False initially. Camera activation is separate and remains opt-in.</value>
    /// <exception cref="IOException">The native camera subsystem cannot initialize.</exception>
    public static bool MonitoringFeeds
    {
        get => Shared._monitoring;
        set => Shared.SetMonitoring(value);
    }
    private void SetMonitoring(bool value)
    {
        EnsureOwner(); if (_processing || _discovering || _changingRegistry) throw new InvalidOperationException("Camera monitoring cannot change during a pump or discovery callback.");
        if (_monitoring == value) return;
        if (value)
        {
            if (!SDL.InitSubSystem(SDL.InitFlags.Camera)) throw new IOException("Cannot initialize cameras: " + SDL.GetError());
            _monitoring = true; _nextDiscovery = 0;
        }
        else
        {
            _monitoring = false; List<Exception>? errors = null;
            try
            {
                for (var i = _feeds.Count - 1; i >= 0; i--) if (_feeds[i] is NativeCameraFeed feed)
                        try { RemoveCore(feed); } catch (Exception error) { Node.CollectException(ref errors, error); }
            }
            finally { SDL.QuitSubSystem(SDL.InitFlags.Camera); }
            Node.ThrowCollected("Camera monitoring cleanup failed.", errors);
        }
    }
    /// <summary>Adds one caller-owned feed, retaining its identity until removal.</summary><param name="feed">Live feed created on the service owner thread.</param>
    /// <remarks>Repeated registration of the same feed has no effect. Added events observe the committed registry.</remarks>
    public static void AddFeed(CameraFeed feed)
    {
        ArgumentNullException.ThrowIfNull(feed); Shared.EnsureOwner(); feed.CheckOwner();
        if (feed is NativeCameraFeed) throw new InvalidOperationException("Native feeds are registered by monitoring.");
        Shared.AddCore(feed);
    }
    private void AddCore(CameraFeed feed)
    {
        if (feed.Emitting) throw new InvalidOperationException("Cannot register a feed during its event emission.");
        if (_changingRegistry) throw new InvalidOperationException("Camera registry callbacks cannot reenter registry mutation.");
        lock (_feedGate) { if (_feeds.Contains(feed)) return; _feeds.Add(feed); }
        _changingRegistry = true;
        try
        {
            List<Exception>? errors = null; NotifyViews(ref errors);
            try { BeginEventEmission(_added.Handlers)?.Invoke(feed.GetID()); } catch (Exception error) { Node.CollectException(ref errors, error); }
            Node.ThrowCollected("Camera registration callbacks failed.", errors);
        }
        finally { _changingRegistry = false; }
    }
    /// <summary>Removes a registered feed; native feeds also close and retire their device.</summary><param name="feed">Currently registered identity.</param>
    /// <remarks>A custom feed's activity and ownership remain with its caller. Removed events observe the committed registry.</remarks>
    public static void RemoveFeed(CameraFeed feed) { ArgumentNullException.ThrowIfNull(feed); Shared.EnsureOwner(); feed.CheckOwner(); Shared.RemoveCore(feed); }
    private void RemoveCore(CameraFeed feed)
    {
        if (feed.Emitting) throw new InvalidOperationException("Cannot remove a feed during its event emission.");
        if (_changingRegistry) throw new InvalidOperationException("Camera registry callbacks cannot reenter registry mutation.");
        lock (_feedGate) { if (!_feeds.Remove(feed)) throw new ArgumentException("The camera feed is not registered.", nameof(feed)); }
        _changingRegistry = true;
        try
        {
            var id = feed.GetID(); List<Exception>? errors = null; NotifyViews(ref errors);
            if (feed is NativeCameraFeed)
            {
                try { feed.FeedIsActive = false; } catch (Exception error) { Node.CollectException(ref errors, error); }
                try { feed.Dispose(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            }
            try { BeginEventEmission(_removed.Handlers)?.Invoke(id); } catch (Exception error) { Node.CollectException(ref errors, error); }
            Node.ThrowCollected("Camera removal callbacks failed.", errors);
        }
        finally { _changingRegistry = false; }
    }
    /// <summary>Returns registered feeds in insertion order as a copied array.</summary><returns>Borrowed source identities.</returns>
    public static CameraFeed[] Feeds() { lock (Shared._feedGate) return Shared._feeds.ToArray(); }
    /// <summary>Returns one registered feed by index.</summary><param name="index">Zero-based insertion-order index.</param><returns>A borrowed live source.</returns>
    public static CameraFeed GetFeed(int index) { lock (Shared._feedGate) return Shared._feeds[index]; }
    /// <summary>Returns the registered source count.</summary><returns>Native and custom feeds together.</returns>
    public static int GetFeedCount() { lock (Shared._feedGate) return Shared._feeds.Count; }
    internal static CameraFeed? Find(int id) { lock (Shared._feedGate) { foreach (var feed in Shared._feeds) if (feed.GetID() == id) return feed; return null; } }
    internal static bool Contains(CameraFeed feed) { lock (Shared._feedGate) return Shared._feeds.Contains(feed); }
    internal static void Watch(CameraTexture view)
    {
        lock (Shared._viewGate)
        {
            if (Shared._views.Count >= Shared._nextViewSweep)
            {
                Shared._views.RemoveAll(static weak => !weak.TryGetTarget(out var target) || target.IsDisposed);
                Shared._nextViewSweep = Shared._views.Count + Math.Max(64, Shared._views.Count);
            }
            Shared._views.Add(new(view));
        }
    }
    internal static void Unwatch(CameraTexture view)
    {
        lock (Shared._viewGate)
            for (var i = Shared._views.Count - 1; i >= 0; i--) if (!Shared._views[i].TryGetTarget(out var target) || ReferenceEquals(target, view)) Shared._views.RemoveAt(i);
    }
    private void NotifyViews(ref List<Exception>? errors)
    {
        lock (_viewGate)
            for (var i = _views.Count - 1; i >= 0; i--)
            {
                if (!_views[i].TryGetTarget(out var target) || target.IsDisposed) _views.RemoveAt(i);
                else _viewBatch.Add(target);
            }
        try
        {
            foreach (var target in _viewBatch)
                if (!target.IsDisposed) try { target.RegistryChanged(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        }
        finally { _viewBatch.Clear(); }
    }
    /// <summary>Discovers changes, acquires available frames and delivers coalesced source events on the owner.</summary>
    /// <remarks>The native display pump calls this automatically. Standalone hosts call it explicitly.
    /// Callback failures are collected after other feeds are attempted. Recursive pumps are rejected.</remarks>
    public static void Process() => Shared.ProcessCore();
    private void ProcessCore()
    {
        EnsureOwner(); if (_processing || _discovering || _changingRegistry) throw new InvalidOperationException("Camera processing cannot reenter.");
        _processing = true; List<Exception>? errors = null;
        try
        {
            if (_monitoring && Environment.TickCount64 >= _nextDiscovery)
            {
                _nextDiscovery = Environment.TickCount64 + 1000;
                try { Discover(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            }
            lock (_feedGate) _batch.AddRange(_feeds);
            foreach (var feed in _batch)
            {
                if (feed.IsDisposed || !_feeds.Contains(feed)) continue;
                try { feed.Poll(); } catch (Exception error) { Node.CollectException(ref errors, error); }
                try { feed.Deliver(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            }
        }
        finally { _batch.Clear(); _processing = false; }
        Node.ThrowCollected("Camera processing failed.", errors);
    }
    private void Discover()
    {
        _discovering = true; List<Exception>? errors = null;
        try
        {
            var devices = SDL.GetCameras(out _) ?? throw new IOException("Cannot enumerate cameras: " + SDL.GetError());
            for (var i = _feeds.Count - 1; i >= 0; i--) if (_feeds[i] is NativeCameraFeed feed && Array.IndexOf(devices, feed.DeviceID) < 0)
                    try { RemoveCore(feed); } catch (Exception error) { Node.CollectException(ref errors, error); }
            foreach (var id in devices)
            {
                var found = false; foreach (var feed in _feeds) if (feed is NativeCameraFeed native && native.DeviceID == id) { found = true; break; }
                if (!found)
                {
                    var feed = new NativeCameraFeed(id);
                    try { AddCore(feed); }
                    catch (Exception error) { if (!_feeds.Contains(feed)) feed.Dispose(); Node.CollectException(ref errors, error); }
                }
            }
            try { BeginEventEmission(_updated.Handlers)?.Invoke(); } catch (Exception error) { Node.CollectException(ref errors, error); }
        }
        finally { _discovering = false; }
        Node.ThrowCollected("Camera discovery callbacks failed.", errors);
    }
    /// <summary>Reports a newly registered process-local feed ID.</summary>
    public static event Action<int>? CameraFeedAdded { add => Shared._added.Add(value); remove => Shared._added.Remove(value); }
    /// <summary>Reports a removed process-local feed ID.</summary>
    public static event Action<int>? CameraFeedRemoved { add => Shared._removed.Add(value); remove => Shared._removed.Remove(value); }
    /// <summary>Reports completion of a native device scan, including an empty result.</summary>
    public static event Action? CameraFeedsUpdated { add => Shared._updated.Add(value); remove => Shared._updated.Remove(value); }
    internal static void PumpIfOwned() { if (Shared._owner == Environment.CurrentManagedThreadId) Shared.ProcessCore(); }
    internal static void CloseForEngine() { if (Shared._owner == Environment.CurrentManagedThreadId) Shared.SetMonitoring(false); }
    private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<CameraServer, bool>(nameof(MonitoringFeeds), _ => MonitoringFeeds, (_, v) => MonitoringFeeds = v, _ => false)];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override void ValidateMutation() { EnsureOwner(); base.ValidateMutation(); }
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The camera service has permanent process lifetime.");
}
