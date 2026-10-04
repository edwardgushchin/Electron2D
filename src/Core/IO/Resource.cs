using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using CryptographicRandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator;

namespace Electron2D;

/// <summary>Provides reusable data, change notification, path identity, and typed duplication for engine assets.</summary>
/// <remarks>
/// Resource lifetime uses the managed runtime together with <see cref="ElectronObject.Dispose()"/>; no public reference
/// counter is exposed. Base state is safe for concurrent reads and serialized writes, but derived resource state and
/// callbacks have no implicit synchronization or thread affinity.
/// </remarks>
public class Resource : ElectronObject
{
    /// <summary>Returns a resource's typed backend identity, or empty when it has no registered backend role.</summary>
    /// <returns>Empty for base managed resources; Shape overrides return stable physics shape identities.</returns>
    /// <remarks>Overrides project an existing backend role. Rendering resource RID registration remains incomplete.</remarks>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public virtual RID GetRID() { ThrowIfDisposed(); return default; }

    private const string LocalPathPrefix = "local://";
    private const string EmbeddedPathSeparator = "::";
    private const int SceneUniqueIdLength = 5;
    private const string SceneUniqueIdAlphabet = "abcdefghijklmnopqrstuvwxy012345678";

    private static readonly object PathCacheGate = new();
    private static readonly Dictionary<string, WeakReference<Resource>> PathCache = new(StringComparer.Ordinal);

    internal static Resource? GetRegisteredPath(string path)
    {
        lock (PathCacheGate)
        {
            if (!PathCache.TryGetValue(path, out var weak)) return null;
            if (weak.TryGetTarget(out var resource) && !resource.IsDisposed) return resource;
            PathCache.Remove(path);
            return null;
        }
    }
    private static readonly IReadOnlyList<PropertyDescriptor> ResourceProperties = Array.AsReadOnly<PropertyDescriptor>(
    [
        new PropertyDescriptor<Resource, bool>(
            nameof(ResourceLocalToScene),
            resource => resource.ResourceLocalToScene,
            (resource, value) => resource.ResourceLocalToScene = value,
            _ => false),
        new PropertyDescriptor<Resource, string>(
            nameof(ResourceName),
            resource => resource.ResourceName,
            (resource, value) => resource.ResourceName = value,
            _ => string.Empty),
        new PropertyDescriptor<Resource, string>(
            nameof(ResourcePath),
            resource => resource.ResourcePath,
            (resource, value) => resource.ResourcePath = value,
            _ => string.Empty),
        new PropertyDescriptor<Resource, string>(
            nameof(ResourceSceneUniqueID),
            resource => resource.ResourceSceneUniqueID,
            (resource, value) => resource.ResourceSceneUniqueID = value,
            _ => string.Empty,
            (_, value) => IsValidSceneUniqueID(value))
    ]);

    private ResourceFileOwnership? _fileOwnership;
    internal void AdoptFileResources(Resource[] resources) { var old = _fileOwnership; _fileOwnership = resources.Length == 0 ? null : new ResourceFileOwnership(resources); old?.ReleaseOwner(); OnFileOwnershipChanged(); }
    internal bool FilePathRegistered { get { lock (PathCacheGate) return _pathIsRegistered; } }
    internal void AdoptFileOwnership(ResourceFileOwnership? ownership) { var old = _fileOwnership; _fileOwnership = ownership; old?.ReleaseOwner(); OnFileOwnershipChanged(); }
    internal virtual void OnFileOwnershipChanged() { }
    internal IDisposable? RetainFileResources() => _fileOwnership?.Retain();

    private readonly object _changeBatchGate = new();
    private readonly object _stateGate = new();
    private string _resourceName = string.Empty;
    private string _resourcePath = string.Empty;
    private string _sceneUniqueId = string.Empty;
    private Node? _localScene;
    private bool _localToScene;
    private bool _pathIsRegistered;
    private int _changeBlockDepth;
    private int _changeBlockOwnerThreadId;
    private bool _changePending;
    private long _changeRevision;
    internal long ChangeRevision => Volatile.Read(ref _changeRevision);

    /// <summary>Gets or sets whether a scene-instancing component should make this resource unique to each scene instance.</summary>
    /// <value><see langword="false"/> by default; <see langword="true"/> requests per-instance duplication.</value>
    /// <remarks>
    /// Changing this value does not retroactively affect existing instances. <see cref="PackedScene.Instantiate"/>
    /// duplicates a marked resource once per instance while preserving aliases in its duplicated resource graph.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    public bool ResourceLocalToScene
    {
        get
        {
            ThrowIfDisposed();
            lock (_stateGate)
            {
                ThrowIfDisposed();
                return _localToScene;
            }
        }
        set
        {
            ThrowIfDisposed();
            lock (_stateGate)
            {
                ThrowIfDisposed();
                _localToScene = value;
            }
        }
    }

    /// <summary>Gets or sets the optional display name of this resource.</summary>
    /// <value>An arbitrary non-null string; the default is empty.</value>
    /// <remarks>Every successful assignment synchronously raises <see cref="Changed"/>, even when the value is unchanged.</remarks>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A <see cref="Changed"/> handler throws after the value has been assigned.</exception>
    public string ResourceName
    {
        get
        {
            ThrowIfDisposed();
            lock (_stateGate)
            {
                ThrowIfDisposed();
                return _resourceName;
            }
        }
        set
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(value);

            lock (_stateGate)
            {
                ThrowIfDisposed();
                _resourceName = value;
            }

            EmitChanged();
        }
    }

    /// <summary>Gets or sets the unique cache path associated with this resource.</summary>
    /// <value>An opaque, case-sensitive path, or an empty string when the resource has no registered path.</value>
    /// <remarks>
    /// Nonempty paths are process-wide and unique among live resources. Assigning an occupied path throws without
    /// changing either resource. Use <see cref="TakeOverPath"/> to transfer ownership deliberately.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Another live resource owns the assigned nonempty path.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    public string ResourcePath
    {
        get
        {
            ThrowIfDisposed();
            lock (PathCacheGate)
            {
                ThrowIfDisposed();
                return _resourcePath;
            }
        }
        set => SetPath(value, takeOver: false);
    }

    /// <summary>Gets or sets the identifier used when this resource is embedded in a serialized scene.</summary>
    /// <value>An empty string, or an identifier containing only ASCII letters, digits, and underscores.</value>
    /// <remarks>Assignments do not raise <see cref="Changed"/>. Scene saving and collision resolution are not implemented yet.</remarks>
    /// <exception cref="ArgumentException">The assigned value contains a character outside ASCII letters, digits, and underscores.</exception>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    public string ResourceSceneUniqueID
    {
        get
        {
            ThrowIfDisposed();
            lock (_stateGate)
            {
                ThrowIfDisposed();
                return _sceneUniqueId;
            }
        }
        set
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(value);

            if (!IsValidSceneUniqueID(value))
                throw new ArgumentException("A scene-unique ID may contain only ASCII letters, digits, and underscores.", nameof(value));

            lock (_stateGate)
            {
                ThrowIfDisposed();
                _sceneUniqueId = value;
            }
        }
    }

    /// <summary>Gets whether this resource is embedded rather than represented by a standalone external path.</summary>
    /// <value>
    /// <see langword="true"/> when the path is empty, contains an embedded-resource separator, or starts with the
    /// local-resource prefix; otherwise <see langword="false"/>.
    /// </value>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    public bool IsBuiltIn
    {
        get
        {
            var path = ResourcePath;
            return path.Length == 0 || path.Contains(EmbeddedPathSeparator, StringComparison.Ordinal) ||
                path.StartsWith(LocalPathPrefix, StringComparison.Ordinal);
        }
    }

    /// <summary>Gets the root node whose scene instance owns this scene-local resource.</summary>
    /// <returns>The owning scene root after scene instantiation, or <see langword="null"/> for other resources.</returns>
    /// <remarks>The association is assigned before <see cref="OnSetupLocalToScene"/> runs. Replacing the owning
    /// root through <see cref="Node.ReplaceBy"/> transfers it to the replacement; otherwise it remains until disposal.</remarks>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    public Node? GetLocalScene()
    {
        ThrowIfDisposed();
        lock (_stateGate)
        {
            ThrowIfDisposed();
            return _localScene;
        }
    }

    /// <summary>Occurs when this resource reports a meaningful content change.</summary>
    /// <remarks>
    /// Delivery is synchronous on the calling thread. Custom resource setters should call <see cref="EmitChanged"/>
    /// after committing a meaningful change. A throwing handler stops later handlers and propagates to the caller.
    /// </remarks>
    public event Action<Resource>? Changed;

    /// <summary>Occurs immediately before <see cref="OnSetupLocalToScene"/> is invoked.</summary>
    /// <remarks>Packed-scene instantiation raises this after assigning the local scene; overrides are preferred.</remarks>
    [Obsolete("Override OnSetupLocalToScene instead.")]
    public event Action<Resource>? SetupLocalToSceneRequested;

    /// <summary>Copies stored data from another resource of the exact same runtime type while preserving this resource's path and scene ID.</summary>
    /// <param name="source">The live resource whose stored data is copied.</param>
    /// <remarks>
    /// Copying is shallow: nested resources and collection instances remain shared unless a derived override explicitly
    /// defines other behavior. <see cref="ResetState"/> runs first. Change notifications raised while copying are
    /// coalesced into one final <see cref="Changed"/> event. Batches targeting the same resource are serialized, but
    /// derived state still requires caller coordination. The operation is not transactional if a derived callback fails.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="source"/> has a different runtime type.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Either resource is disposing or disposed.</exception>
    /// <exception cref="Exception">A reset, copy, setter, or final change handler fails.</exception>
    public void CopyFromResource(Resource source)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(source);
        source.ThrowIfDisposed();

        if (ReferenceEquals(this, source))
            return;

        if (GetType() != source.GetType())
            throw new ArgumentException("Resources must have the same runtime type.", nameof(source));

        var retention = source._fileOwnership is { } owner && !owner.Contains(this) ? owner.RetainOwner() : null;
        try
        {
            ExecuteChangeBatch(
                () =>
                {
                    var copied = false;
                    try
                    {
                        source.ThrowIfDisposed();
                        var (name, localToScene) = source.GetCopyableBaseState();
                        EmitChanged(); ResetState(); ResourceName = name; ResourceLocalToScene = localToScene;
                        source.CopyCustomStateTo(this, deep: false, DeepDuplicateMode.None, static resource => resource, static resource => resource);
                        copied = true;
                    }
                    finally
                    {
                        if (copied) { var transfer = retention; retention = null; AdoptFileOwnership(transfer); }
                        else if (retention is not null) { var previous = _fileOwnership; _fileOwnership = previous is null ? retention : new ResourceFileOwnership([], [previous, retention]); retention = null; OnFileOwnershipChanged(); }
                    }
                }, emitAtEnd: true);
        }
        finally { retention?.ReleaseOwner(); }
    }

    /// <summary>Creates a shallow or internally deep duplicate of this resource.</summary>
    /// <param name="deep">
    /// <see langword="false"/> to share collection containers and nested resources; <see langword="true"/> to let
    /// derived resources clone containers and duplicate built-in nested resources.
    /// </param>
    /// <returns>A new live resource of the exact same runtime type with an empty path and scene ID.</returns>
    /// <exception cref="InvalidOperationException">A derived duplication factory returns an invalid instance.</exception>
    /// <exception cref="NotSupportedException">A derived resource does not explicitly implement the duplication hooks.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposing or disposed.</exception>
    /// <exception cref="Exception">Construction, copying, or cleanup of a failed duplicate throws.</exception>
    public Resource Duplicate(bool deep = false) =>
        DuplicateCore(deep, deep ? DeepDuplicateMode.Internal : DeepDuplicateMode.None);

    /// <summary>Creates a deep duplicate with explicit nested-resource policy.</summary>
    /// <param name="subresourceMode">Controls which nested resources are duplicated.</param>
    /// <returns>A new live resource of the exact same runtime type with an empty path and scene ID.</returns>
    /// <remarks>
    /// Repeated and cyclic resource references preserve graph identity. Derived resources remain responsible for
    /// cloning their typed collection containers and passing nested resources to the appropriate supplied duplication delegate.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="subresourceMode"/> is not defined.</exception>
    /// <exception cref="InvalidOperationException">A derived duplication factory returns an invalid instance.</exception>
    /// <exception cref="NotSupportedException">A derived resource does not explicitly implement the duplication hooks.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposing or disposed.</exception>
    /// <exception cref="Exception">Construction, copying, or cleanup of a failed duplicate throws.</exception>
    public Resource DuplicateDeep(DeepDuplicateMode subresourceMode = DeepDuplicateMode.Internal)
    {
        ValidateDuplicateMode(subresourceMode);
        return DuplicateCore(deep: true, subresourceMode);
    }

    /// <summary>Synchronously reports that this resource's meaningful content changed.</summary>
    /// <remarks>Calls made inside a copy batch are coalesced into one event when the outermost batch ends.</remarks>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A <see cref="Changed"/> handler throws.</exception>
    public void EmitChanged()
    {
        ThrowIfDisposed();
        Action<Resource>? handlers;

        lock (_stateGate)
        {
            ThrowIfDisposed();

            Interlocked.Increment(ref _changeRevision);
            if (_changeBlockDepth != 0 && _changeBlockOwnerThreadId == Environment.CurrentManagedThreadId)
            {
                _changePending = true;
                return;
            }

            handlers = Changed;
        }

        handlers?.Invoke(this);
    }

    /// <summary>Generates a compact scene-relative resource identifier.</summary>
    /// <returns>A five-character string composed of lowercase letters <c>a</c> through <c>y</c> and digits <c>0</c> through <c>8</c>.</returns>
    /// <remarks>The result is probabilistically unique; a future scene saver must still detect and resolve collisions.</remarks>
    public static string GenerateSceneUniqueID()
    {
        return string.Create(
            SceneUniqueIdLength,
            0,
            static (buffer, _) =>
            {
                for (var index = 0; index < buffer.Length; index++)
                    buffer[index] = SceneUniqueIdAlphabet[CryptographicRandomNumberGenerator.GetInt32(SceneUniqueIdAlphabet.Length)];
            });
    }

    /// <summary>Clears non-stored state through <see cref="OnResetState"/>.</summary>
    /// <remarks>The base implementation does not change stored properties and does not raise <see cref="Changed"/>.</remarks>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception"><see cref="OnResetState"/> throws.</exception>
    public void ResetState()
    {
        ThrowIfDisposed();
        OnResetState();
    }

    /// <summary>Sets the path value without registering it in the process-wide resource cache.</summary>
    /// <param name="path">The non-null opaque path, or an empty string.</param>
    /// <remarks>
    /// This loader-oriented operation may produce the same visible path on multiple resources. It first removes this
    /// resource's previously registered path, then invokes <see cref="OnPathCacheSet"/> after committing the value.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception"><see cref="OnPathCacheSet"/> throws after the path has been committed.</exception>
    public void SetPathCache(string path)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(path);

        lock (PathCacheGate)
        {
            ThrowIfDisposed();
            RemoveRegisteredPathUnderLock(this);
            _resourcePath = path;
            _pathIsRegistered = false;
        }

        Exception? cacheError = null;
        Exception? changedError = null;

        try
        {
            OnPathCacheSet(path);
        }
        catch (Exception error)
        {
            cacheError = error;
        }

        try
        {
            OnResourcePathChanged(path);
        }
        catch (Exception error)
        {
            changedError = error;
        }

        ThrowCombined(cacheError, changedError);
    }

    /// <summary>Invokes scene-local setup callbacks for a resource duplicated by a scene-instancing component.</summary>
    /// <remarks>Packed-scene instantiation invokes this automatically for each duplicated scene-local resource.</remarks>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="AggregateException">Both the compatibility event and virtual callback fail.</exception>
    /// <exception cref="Exception">The compatibility event or virtual callback fails.</exception>
    [Obsolete("This method is reserved for scene-instancing infrastructure. Override OnSetupLocalToScene instead.")]
    public void SetupLocalToScene()
    {
        SetupLocalToSceneCore();
    }

    internal static SceneDuplicationScope CreateSceneDuplicationScope() => new();

    private void SetupLocalToSceneCore()
    {
        ThrowIfDisposed();
        Exception? eventError = null;
        Exception? callbackError = null;

        try
        {
#pragma warning disable CS0618
            SetupLocalToSceneRequested?.Invoke(this);
#pragma warning restore CS0618
        }
        catch (Exception error)
        {
            eventError = error;
        }

        try
        {
            OnSetupLocalToScene();
        }
        catch (Exception error)
        {
            callbackError = error;
        }

        ThrowCombined(eventError, callbackError);
    }

    /// <summary>Transfers ownership of a process-wide resource path to this resource.</summary>
    /// <param name="path">The non-null opaque path. An empty path simply clears this resource's current path.</param>
    /// <remarks>A displaced live resource atomically receives an empty path.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    public void TakeOverPath(string path) => SetPath(path, takeOver: true);

    /// <summary>Creates a fresh default instance used as the target of duplication.</summary>
    /// <returns>A live resource of the exact same runtime type with empty path and scene ID.</returns>
    /// <remarks>
    /// The base implementation supports only an exact <see cref="Resource"/> instance. Every derived class must
    /// override this method, even when it adds no state, so duplication support is explicit.
    /// </remarks>
    /// <exception cref="NotSupportedException">The runtime type derives from <see cref="Resource"/> and has not overridden this method.</exception>
    protected virtual Resource CreateDuplicateInstance()
    {
        if (GetType() != typeof(Resource))
            throw new NotSupportedException($"{GetType().Name} must override {nameof(CreateDuplicateInstance)} to support duplication.");

        return new Resource();
    }

    /// <summary>Copies derived stored state into a duplicate or copy target.</summary>
    /// <param name="target">A live resource with the exact same runtime type.</param>
    /// <param name="deep">Whether typed collection containers should be cloned recursively.</param>
    /// <param name="subresourceMode">The nested-resource policy for this copy.</param>
    /// <param name="duplicateSubresource">
    /// A graph-preserving function that returns the correct shared or duplicated instance for a nested resource.
    /// Pass every nested resource through this function when <paramref name="deep"/> is <see langword="true"/>.
    /// </param>
    /// <param name="forceDuplicateSubresource">
    /// A graph-preserving function that duplicates a nested resource even when the current policy would share it.
    /// Use it for typed properties whose contract requires duplication; assign the original reference directly for
    /// properties whose contract forbids duplication.
    /// </param>
    /// <remarks>
    /// The base implementation supports only an exact <see cref="Resource"/> instance. Derived implementations must
    /// copy all stored custom state and call the base implementation only when they intentionally want its validation.
    /// Assigning the original nested-resource reference directly expresses a never-duplicate property.
    /// </remarks>
    /// <exception cref="NotSupportedException">A derived resource has not explicitly implemented custom-state copying.</exception>
    protected virtual void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        if (GetType() != typeof(Resource))
            throw new NotSupportedException($"{GetType().Name} must override {nameof(CopyCustomStateTo)} to support copying.");
    }

    /// <summary>Clears non-stored state when <see cref="ResetState"/> or <see cref="CopyFromResource"/> requests it.</summary>
    protected virtual void OnResetState()
    { }

    /// <summary>Handles a raw path-cache assignment after the new path has been committed.</summary>
    /// <param name="path">The newly committed path.</param>
    protected virtual void OnPathCacheSet(string path)
    { }

    /// <summary>Handles any committed change to this resource's visible path.</summary>
    /// <param name="path">The newly committed path, or an empty string after displacement.</param>
    /// <remarks>The path has already changed when this callback runs.</remarks>
    protected virtual void OnResourcePathChanged(string path)
    { }

    /// <summary>Customizes a newly duplicated scene-local resource.</summary>
    /// <remarks>The owning scene is available through <see cref="GetLocalScene"/> while this callback runs.</remarks>
    protected virtual void OnSetupLocalToScene()
    { }

    /// <inheritdoc />
    /// <remarks>Appends resource identity and scene-instancing configuration descriptors.</remarks>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(ResourceProperties);

    /// <inheritdoc />
    /// <remarks>Unregisters the cache path and clears resource event subscribers before base cleanup.</remarks>
    protected override void Dispose(bool disposing)
    {
        Exception? fileError = null; if (disposing) try { var ownership = _fileOwnership; _fileOwnership = null; ownership?.ReleaseOwner(); } catch (Exception error) { fileError = error; }
        if (disposing)
        {
            lock (_changeBatchGate)
            {
                lock (PathCacheGate)
                {
                    RemoveRegisteredPathUnderLock(this);
                    _resourcePath = string.Empty;
                    _pathIsRegistered = false;
                }

                lock (_stateGate)
                {
                    _localScene = null;
                    Changed = null;
#pragma warning disable CS0618
                    SetupLocalToSceneRequested = null;
#pragma warning restore CS0618
                    _changeBlockDepth = 0;
                    _changeBlockOwnerThreadId = 0;
                    _changePending = false;
                }
            }
        }

        base.Dispose(disposing);
        if (fileError is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(fileError).Throw();
    }

    /// <summary>Returns a diagnostic string containing the optional resource name, path, runtime class, and instance identifier.</summary>
    /// <returns>A stable diagnostic representation of this resource's current base state.</returns>
    public override string ToString()
    {
        string name;
        string path;

        lock (_stateGate)
            name = _resourceName;

        lock (PathCacheGate)
            path = _resourcePath;

        return $"{(name.Length == 0 ? string.Empty : name + " ")}({path}):{base.ToString()}";
    }

    private Resource DuplicateCore(bool deep, DeepDuplicateMode subresourceMode)
    {
        ThrowIfDisposed();
        ValidateDuplicateMode(subresourceMode);

        var session = new DuplicationSession(deep, subresourceMode);

        try
        {
            return session.DuplicateRoot(this);
        }
        catch (Exception duplicationError)
        {
            var cleanupErrors = session.DisposeCreated();
            if (cleanupErrors.Count == 0)
                ExceptionDispatchInfo.Capture(duplicationError).Throw();

            cleanupErrors.Insert(0, duplicationError);
            throw new AggregateException("Resource duplication and cleanup failed.", cleanupErrors);
        }

        throw new InvalidOperationException("Unreachable resource duplication state.");
    }

    private (string Name, bool LocalToScene) GetCopyableBaseState()
    {
        ThrowIfDisposed();
        lock (_stateGate)
        {
            ThrowIfDisposed();
            return (_resourceName, _localToScene);
        }
    }

    private void SetPath(string path, bool takeOver)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(path);

        Resource? displaced = null;

        lock (PathCacheGate)
        {
            ThrowIfDisposed();

            if (string.Equals(_resourcePath, path, StringComparison.Ordinal) &&
                (path.Length == 0 || _pathIsRegistered))
                return;

            Resource? existing = null;
            if (path.Length != 0 && PathCache.TryGetValue(path, out var weak))
            {
                if (!weak.TryGetTarget(out existing) || existing.IsDisposed)
                {
                    PathCache.Remove(path);
                    existing = null;
                }
            }

            if (existing is not null && !ReferenceEquals(existing, this) && !takeOver)
                throw new InvalidOperationException($"Another live resource owns path '{path}'.");

            RemoveRegisteredPathUnderLock(this);

            if (existing is not null && !ReferenceEquals(existing, this))
            {
                existing._resourcePath = string.Empty;
                existing._pathIsRegistered = false;
                PathCache.Remove(path);
                displaced = existing;
            }

            _resourcePath = path;
            _pathIsRegistered = path.Length != 0;

            if (_pathIsRegistered)
                PathCache[path] = new WeakReference<Resource>(this);
        }

        Exception? displacedError = null;
        Exception? changedError = null;

        try
        {
            displaced?.OnResourcePathChanged(string.Empty);
        }
        catch (Exception error)
        {
            displacedError = error;
        }

        try
        {
            OnResourcePathChanged(path);
        }
        catch (Exception error)
        {
            changedError = error;
        }

        ThrowCombined(displacedError, changedError);
    }

    private static void RemoveRegisteredPathUnderLock(Resource resource)
    {
        if (!resource._pathIsRegistered || resource._resourcePath.Length == 0)
            return;

        if (PathCache.TryGetValue(resource._resourcePath, out var weak) &&
            weak.TryGetTarget(out var owner) && ReferenceEquals(owner, resource))
        {
            PathCache.Remove(resource._resourcePath);
        }

        resource._pathIsRegistered = false;
    }

    private static bool IsValidSceneUniqueID(string? value)
    {
        if (value is null)
            return false;

        foreach (var character in value)
        {
            if (character is not (>= 'a' and <= 'z') and not (>= 'A' and <= 'Z') and
                not (>= '0' and <= '9') and not '_')
            {
                return false;
            }
        }

        return true;
    }

    private static void ValidateDuplicateMode(DeepDuplicateMode mode)
    {
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown deep-duplication mode.");
    }

    private void ExecuteChangeBatch(Action operation, bool emitAtEnd)
    {
        lock (_changeBatchGate)
        {
            BeginChangeBatch();
            Exception? operationError = null;
            Exception? changeError = null;

            try
            {
                operation();
            }
            catch (Exception error)
            {
                operationError = error;
            }

            try
            {
                EndChangeBatch(emitAtEnd);
            }
            catch (Exception error)
            {
                changeError = error;
            }

            ThrowCombined(operationError, changeError);
        }
    }

    private void BeginChangeBatch()
    {
        lock (_stateGate)
        {
            if (_changeBlockDepth == 0)
                _changeBlockOwnerThreadId = Environment.CurrentManagedThreadId;

            _changeBlockDepth++;
        }
    }

    private void EndChangeBatch(bool emitAtEnd)
    {
        Action<Resource>? handlers = null;

        lock (_stateGate)
        {
            if (_changeBlockDepth <= 0)
                throw new InvalidOperationException("Resource change batching is unbalanced.");

            if (_changeBlockOwnerThreadId != Environment.CurrentManagedThreadId)
                throw new InvalidOperationException("Resource change batching must end on its owning thread.");

            _changeBlockDepth--;
            if (_changeBlockDepth == 0)
            {
                if (emitAtEnd && _changePending)
                    handlers = Changed;

                _changePending = false;
                _changeBlockOwnerThreadId = 0;
            }
        }

        handlers?.Invoke(this);
    }

    internal static void ThrowCombined(Exception? first, Exception? second)
    {
        if (first is not null && second is not null)
            throw new AggregateException(first, second);

        if (first is not null)
            ExceptionDispatchInfo.Capture(first).Throw();

        if (second is not null)
            ExceptionDispatchInfo.Capture(second).Throw();
    }

    internal sealed class SceneDuplicationScope
    {
        private readonly DuplicationSession _session = new(
            deep: true,
            DeepDuplicateMode.Internal,
            static resource => resource.ResourceLocalToScene || resource.IsBuiltIn);

        internal Resource Resolve(Resource source)
        {
            ArgumentNullException.ThrowIfNull(source);
            source.ThrowIfDisposed();

            if (_session.TryGetDuplicate(source, out var duplicate))
                return duplicate;

            return source.ResourceLocalToScene ? _session.DuplicateRoot(source) : source;
        }

        internal void SetupLocalResources()
        {
            List<Exception>? errors = null;

            foreach (var resource in _session.SceneLocalDuplicates)
            {
                try
                {
                    resource.SetupLocalToSceneCore();
                }
                catch (Exception error)
                {
                    errors ??= [];
                    if (error is AggregateException aggregate)
                        errors.AddRange(aggregate.Flatten().InnerExceptions);
                    else
                        errors.Add(error);
                }
            }

            if (errors is not null)
                throw new AggregateException("One or more scene-local resource setup callbacks failed.", errors);
        }

        internal void AssignLocalScene(Node root)
        {
            ArgumentNullException.ThrowIfNull(root);
            ObjectDisposedException.ThrowIf(root.IsDisposed, root);

            foreach (var resource in _session.SceneLocalDuplicates)
                resource.AssignLocalScene(root);
        }

        internal IReadOnlyList<Resource> ReleaseCreated() => _session.ReleaseCreated();

        internal List<Exception> DisposeCreated() => _session.DisposeCreated();
    }

    private void AssignLocalScene(Node root)
    {
        ThrowIfDisposed();
        lock (_stateGate)
        {
            ThrowIfDisposed();
            _localScene = root;
        }
    }

    internal void ReassignLocalScene(Node oldRoot, Node newRoot)
    {
        ThrowIfDisposed();
        lock (_stateGate)
        {
            ThrowIfDisposed();
            if (ReferenceEquals(_localScene, oldRoot)) _localScene = newRoot;
        }
    }

    private sealed class DuplicationSession(
        bool deep,
        DeepDuplicateMode mode,
        Func<Resource, bool>? shouldDuplicate = null)
    {
        private readonly Dictionary<Resource, Resource> _duplicates = new(ReferenceEqualityComparer.Instance);
        private readonly List<Resource> _created = [];
        private readonly List<Resource> _sceneLocalDuplicates = [];

        public Resource DuplicateRoot(Resource source) => Duplicate(source, isRoot: true);

        public IReadOnlyList<Resource> SceneLocalDuplicates => _sceneLocalDuplicates;

        public bool TryGetDuplicate(Resource source, out Resource duplicate) =>
            _duplicates.TryGetValue(source, out duplicate!);

        public IReadOnlyList<Resource> ReleaseCreated()
        {
            var released = Array.AsReadOnly(_created.ToArray());
            _created.Clear();
            return released;
        }

        public List<Exception> DisposeCreated()
        {
            var errors = new List<Exception>();

            for (var index = _created.Count - 1; index >= 0; index--)
            {
                try
                {
                    _created[index].Dispose();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }

            return errors;
        }

        private Resource Duplicate(Resource source, bool isRoot = false, bool force = false)
        {
            source.ThrowIfDisposed();

            if (_duplicates.TryGetValue(source, out var existing))
                return existing;

            if (!isRoot && !force && !ShouldDuplicate(source))
                return source;

            var target = source.CreateDuplicateInstance();
            if (target is not null && !ReferenceEquals(source, target))
                _created.Add(target);

            target = ValidateTarget(source, target);
            _duplicates.Add(source, target);

            if (source.ResourceLocalToScene)
                _sceneLocalDuplicates.Add(target);

            var (name, localToScene) = source.GetCopyableBaseState();
            target.ExecuteChangeBatch(
                () =>
                {
                    target.ResourceName = name;
                    target.ResourceLocalToScene = localToScene;
                    source.CopyCustomStateTo(
                        target,
                        deep,
                        mode,
                        resource => resource is null ? null : Duplicate(resource),
                        resource => resource is null ? null : Duplicate(resource, force: true));
                },
                emitAtEnd: false);

            ValidateTarget(source, target);
            target.AdoptFileOwnership(source._fileOwnership?.RetainOwner());

            return target;
        }

        private bool ShouldDuplicate(Resource resource) =>
            deep && (shouldDuplicate?.Invoke(resource) ??
                (mode == DeepDuplicateMode.All || mode == DeepDuplicateMode.Internal && resource.IsBuiltIn));

        private static Resource ValidateTarget(Resource source, Resource? target)
        {
            if (target is null)
                throw new InvalidOperationException("A resource duplication factory returned null.");

            if (ReferenceEquals(source, target))
                throw new InvalidOperationException("A resource duplication factory returned its source instance.");

            if (target.GetType() != source.GetType())
                throw new InvalidOperationException("A resource duplication factory returned a different runtime type.");

            ObjectDisposedException.ThrowIf(target.IsDisposed, target);

            if (target.ResourcePath.Length != 0 || target.ResourceSceneUniqueID.Length != 0)
                throw new InvalidOperationException("A resource duplication factory must return a default instance with empty path and scene ID.");

            return target;
        }
    }
}
