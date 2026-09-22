using System.Runtime.ExceptionServices;
using System.Threading;

namespace Electron2D;

/// <summary>Provides the common identity, notification, property, localization, and lifetime contract for engine objects.</summary>
/// <remarks>
/// The contract uses typed properties and events and deliberately omits dynamic calls, untyped metadata, generic signal
/// registration, and script storage. Instances have deterministic <see cref="IDisposable"/> lifetime.
/// </remarks>
public abstract class ElectronObject : IDisposable
{
    /// <summary>Identifies the post-initialization notification.</summary>
    /// <remarks>
    /// Electron2D does not dispatch this notification from the base constructor because invoking virtual members during
    /// construction is unsafe; a host may dispatch it explicitly after construction.
    /// </remarks>
    public const int NotificationPostInitialize = 0;

    /// <summary>Identifies the notification delivered immediately before owned resources are released.</summary>
    public const int NotificationPreDelete = 1;

    private const int Alive = 0;
    private const int Disposing = 1;
    private const int DisposedState = 2;

    private static long _lastInstanceId;
    private static readonly IReadOnlyList<PropertyDescriptor> ObjectProperties = Array.AsReadOnly<PropertyDescriptor>(
    [
        new PropertyDescriptor<ElectronObject, ulong>(nameof(InstanceID), owner => owner.InstanceID),
        new PropertyDescriptor<ElectronObject, string>(nameof(ClassName), owner => owner.ClassName),
        new PropertyDescriptor<ElectronObject, bool>(nameof(IsDisposed), owner => owner.IsDisposed),
        new PropertyDescriptor<ElectronObject, bool>(
            nameof(CanTranslateMessages),
            owner => owner.CanTranslateMessages,
            (owner, value) => owner.CanTranslateMessages = value,
            _ => true,
            stored: true),
        new PropertyDescriptor<ElectronObject, string>(
            nameof(TranslationDomain),
            owner => owner.TranslationDomain,
            (owner, value) => owner.TranslationDomain = value,
            _ => string.Empty,
            stored: true)
    ]);

    private int _disposeState;
    private int _disposingThreadId;
    private int _canTranslateMessages = 1;
    private string _translationDomain = string.Empty;

    /// <summary>Gets this object's process-local instance identifier.</summary>
    /// <value>A nonzero identifier that is never changed or reused during the current process.</value>
    public ulong InstanceID { get; } = unchecked((ulong)Interlocked.Increment(ref _lastInstanceId));

    /// <summary>Gets the unqualified runtime class name.</summary>
    /// <value>The <c>Name</c> of the <see cref="Type"/> returned by <see cref="object.GetType"/>.</value>
    public string ClassName => GetType().Name;

    /// <summary>Gets whether deterministic disposal has started.</summary>
    /// <value><see langword="true"/> from the moment a caller wins the disposal transition; otherwise <see langword="false"/>.</value>
    public bool IsDisposed => Volatile.Read(ref _disposeState) != Alive;

    /// <summary>Gets or sets whether this object resolves messages through <see cref="TranslationServer"/>.</summary>
    /// <value><see langword="true"/> by default; <see langword="false"/> to return source messages unchanged.</value>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    public bool CanTranslateMessages
    {
        get
        {
            ThrowIfDisposed();
            return Volatile.Read(ref _canTranslateMessages) != 0;
        }
        set
        {
            ValidateMutation();
            Volatile.Write(ref _canTranslateMessages, value ? 1 : 0);
        }
    }

    /// <summary>Gets or sets the translation domain used by this object.</summary>
    /// <value>The case-sensitive domain passed to <see cref="TranslationServer"/>. The default is an empty string.</value>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    public string TranslationDomain
    {
        get
        {
            ThrowIfDisposed();
            return _translationDomain;
        }
        set
        {
            ValidateMutation();
            ArgumentNullException.ThrowIfNull(value);
            _translationDomain = value;
        }
    }

    /// <summary>Occurs once after the object's owned resources have been released successfully.</summary>
    /// <remarks>
    /// The event is not raised when the pre-delete notification or derived cleanup throws. The object is already in
    /// its final disposed state when handlers run, and handler exceptions propagate to the disposing caller.
    /// </remarks>
    public event Action<ElectronObject>? Disposed;

    /// <summary>Occurs when <see cref="NotifyPropertyListChanged"/> reports that the tooling property list changed.</summary>
    /// <remarks>Delivery is synchronous on the notifying thread.</remarks>
    public event Action<ElectronObject>? PropertyListChanged;

    /// <summary>Occurs when a derived scripting component reports that this object's script attachment changed.</summary>
    /// <remarks>
    /// Electron2D does not yet provide a script attachment. The event reserves the typed notification contract for a
    /// future scripting component and is currently raised only through <see cref="NotifyScriptChanged"/>.
    /// </remarks>
    public event Action<ElectronObject>? ScriptChanged;

    /// <summary>Synchronously delivers a numeric engine notification to this object.</summary>
    /// <param name="what">The notification identifier.</param>
    /// <remarks>
    /// Delivery uses normal C# virtual dispatch. An override of <see cref="OnNotification"/> is responsible for calling
    /// its base implementation when inherited behavior is required.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception"><see cref="OnNotification"/> throws.</exception>
    public void Notify(int what)
    {
        ThrowIfDisposed();
        DispatchNotification(what);
    }

    /// <summary>Builds the current validated list of typed properties exposed to tooling.</summary>
    /// <returns>A read-only snapshot whose property names are unique using ordinal comparison.</returns>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="ArgumentNullException"><see cref="GetPropertyDescriptors"/> yielded a null descriptor.</exception>
    /// <exception cref="ArgumentException">A validated descriptor is incompatible with this object's runtime type.</exception>
    /// <exception cref="InvalidOperationException">Two validated descriptors have the same name.</exception>
    /// <exception cref="Exception">A derived property-discovery or validation hook throws.</exception>
    public IReadOnlyList<PropertyDescriptor> GetPropertyList()
    {
        ThrowIfDisposed();

        var properties = new List<PropertyDescriptor>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in GetPropertyDescriptors())
        {
            ArgumentNullException.ThrowIfNull(property);

            var validated = ValidateProperty(property);
            if (validated is null)
                continue;

            validated.EnsureCompatible(this);

            if (!names.Add(validated.Name))
                throw new InvalidOperationException($"Property '{validated.Name}' is exposed more than once by {ClassName}.");

            properties.Add(validated);
        }

        return properties.AsReadOnly();
    }

    /// <summary>Reports whether a tooling property currently differs from its revert value.</summary>
    /// <param name="property">A descriptor compatible with this object.</param>
    /// <returns><see langword="true"/> when the property has a distinct revert value; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="property"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="property"/> is not compatible with this object.</exception>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A descriptor delegate or custom revert-policy override throws.</exception>
    public bool PropertyCanRevert(PropertyDescriptor property)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(property);
        property.EnsureCompatible(this);
        return CanRevertProperty(property);
    }

    /// <summary>Restores a tooling property to its current typed revert value.</summary>
    /// <param name="property">A writable descriptor compatible with this object.</param>
    /// <exception cref="ArgumentNullException"><paramref name="property"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="property"/> is not compatible with this object.</exception>
    /// <exception cref="InvalidOperationException">The descriptor has no writable revert value.</exception>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A descriptor delegate throws.</exception>
    public void RevertProperty(PropertyDescriptor property)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(property);
        property.EnsureCompatible(this);
        property.Revert(this);
    }

    /// <summary>Translates a singular message using this object's translation domain.</summary>
    /// <param name="message">The source message.</param>
    /// <param name="context">An optional disambiguation context. A null context is equivalent to an empty context.</param>
    /// <returns>The resolved translation, or <paramref name="message"/> when translation is disabled or no entry exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    public string Tr(string message, string? context = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(message);
        return CanTranslateMessages ? TranslationServer.Translate(TranslationDomain, message, context) : message;
    }

    /// <summary>Translates a plural message using this object's translation domain.</summary>
    /// <param name="singular">The source singular form.</param>
    /// <param name="plural">The source plural form.</param>
    /// <param name="count">The quantity passed to the registered plural selector.</param>
    /// <param name="context">An optional disambiguation context. A null context is equivalent to an empty context.</param>
    /// <returns>
    /// The resolved plural translation. Without a matching translation, the singular form is returned only for
    /// <paramref name="count"/> equal to <c>1</c>; otherwise the plural form is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="singular"/> or <paramref name="plural"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A matching plural selector returns <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A matching plural selector throws.</exception>
    public string TrN(string singular, string plural, long count, string? context = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(singular);
        ArgumentNullException.ThrowIfNull(plural);
        return CanTranslateMessages
            ? TranslationServer.TranslatePlural(TranslationDomain, singular, plural, count, context)
            : count == 1 ? singular : plural;
    }

    /// <summary>Deterministically releases resources owned by this object.</summary>
    /// <remarks>
    /// Disposal is idempotent. The winning caller synchronously sends <see cref="NotificationPreDelete"/>, invokes
    /// <see cref="Dispose(bool)"/>, publishes the final state, clears base event subscribers, and suppresses finalization.
    /// Callers that lose the atomic transition return without repeating cleanup, although caller-specific
    /// <see cref="ValidateDisposal"/> may already have run and may throw before that transition. The disposing thread
    /// may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.
    /// </remarks>
    /// <exception cref="AggregateException">Both notification delivery and derived cleanup fail.</exception>
    /// <exception cref="Exception">
    /// Disposal validation, a pre-delete callback, derived cleanup, or a <see cref="Disposed"/> handler fails.
    /// Validation failure leaves this caller from starting disposal; a <see cref="Disposed"/> handler failure occurs
    /// after the final disposed state has been published.
    /// </exception>
    public void Dispose()
    {
        if (Volatile.Read(ref _disposeState) != Alive)
            return;

        ValidateDisposal();

        if (Interlocked.CompareExchange(ref _disposeState, Disposing, Alive) != Alive)
            return;

        Volatile.Write(ref _disposingThreadId, Environment.CurrentManagedThreadId);

        Action<ElectronObject>? disposed = null;
        Exception? notificationError = null;
        Exception? disposalError = null;

        try
        {
            try
            {
                DispatchNotification(NotificationPreDelete);
            }
            catch (Exception error)
            {
                notificationError = error;
            }

            try
            {
                Dispose(disposing: true);
            }
            catch (Exception error)
            {
                disposalError = error;
            }
        }
        finally
        {
            Volatile.Write(ref _disposeState, DisposedState);
            disposed = Disposed;
            Disposed = null;
            PropertyListChanged = null;
            ScriptChanged = null;
            GC.SuppressFinalize(this);
        }

        if (notificationError is not null && disposalError is not null)
            throw new AggregateException(notificationError, disposalError);

        if (notificationError is not null)
            ExceptionDispatchInfo.Capture(notificationError).Throw();

        if (disposalError is not null)
            ExceptionDispatchInfo.Capture(disposalError).Throw();

        disposed?.Invoke(this);
    }

    /// <summary>Handles an engine notification delivered to this object.</summary>
    /// <param name="what">The notification identifier.</param>
    /// <remarks>Derived overrides should call the base implementation unless they intentionally suppress inherited handling.</remarks>
    protected virtual void OnNotification(int what)
    {
    }

    /// <summary>Validates that mutable base state may change at the current lifecycle point.</summary>
    /// <remarks>Derived types may reject mutation while they are participating in an atomic operation.</remarks>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    protected virtual void ValidateMutation() => ThrowIfDisposed();

    internal void DispatchNotification(int what) => OnNotification(what);

    /// <summary>Returns the typed properties exposed to tooling before validation.</summary>
    /// <returns>The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.</returns>
    /// <remarks>Overrides append or replace descriptors; they must not yield null entries.</remarks>
    protected virtual IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => ObjectProperties;

    /// <summary>Validates or customizes one property before it is exposed to tooling.</summary>
    /// <param name="property">The descriptor supplied by <see cref="GetPropertyDescriptors"/>.</param>
    /// <returns>The descriptor to expose, a replacement descriptor, or <see langword="null"/> to hide the property.</returns>
    protected virtual PropertyDescriptor? ValidateProperty(PropertyDescriptor property) => property;

    /// <summary>Determines whether a property has a distinct revert value.</summary>
    /// <param name="property">A descriptor already validated for this object.</param>
    /// <returns><see langword="true"/> when the property can currently be reverted; otherwise <see langword="false"/>.</returns>
    protected virtual bool CanRevertProperty(PropertyDescriptor property) => property.CanRevert(this);

    /// <summary>Synchronously raises <see cref="PropertyListChanged"/>.</summary>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An event handler throws.</exception>
    protected void NotifyPropertyListChanged()
    {
        ThrowIfDisposed();
        PropertyListChanged?.Invoke(this);
    }

    /// <summary>Synchronously raises <see cref="ScriptChanged"/>.</summary>
    /// <remarks>A future scripting component should call this when its script reference changes.</remarks>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An event handler throws.</exception>
    protected void NotifyScriptChanged()
    {
        ThrowIfDisposed();
        ScriptChanged?.Invoke(this);
    }

    /// <summary>Releases resources owned by a derived class.</summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>.</param>
    /// <remarks>Overrides release managed resources when <paramref name="disposing"/> is true and then call the base implementation.</remarks>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <summary>Validates caller-specific disposal preconditions before this caller attempts the disposal transition.</summary>
    /// <remarks>
    /// This method can run concurrently in multiple callers and can race with another caller starting disposal.
    /// Overrides must therefore be side-effect-free and tolerate repeated execution.
    /// </remarks>
    protected virtual void ValidateDisposal()
    {
    }

    /// <summary>Publishes the terminal disposed state for an instance whose derived construction cannot complete.</summary>
    /// <remarks>
    /// This helper skips disposal notifications, derived cleanup, and the <see cref="Disposed"/> event because ownership
    /// was never transferred to a successfully constructed instance. The derived constructor remains responsible for
    /// releasing only resources acquired by its failed construction attempt.
    /// </remarks>
    private protected void CompleteFailedConstruction()
    {
        if (Interlocked.CompareExchange(ref _disposeState, DisposedState, Alive) != Alive)
            return;

        Volatile.Write(ref _disposingThreadId, 0);
        Disposed = null;
        PropertyListChanged = null;
        ScriptChanged = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>Rejects access after disposal starts, except on the thread currently running disposal callbacks.</summary>
    /// <exception cref="ObjectDisposedException">The object is disposing on another thread or has finished disposing.</exception>
    protected void ThrowIfDisposed()
    {
        var state = Volatile.Read(ref _disposeState);
        if (state == Alive || state == Disposing && Volatile.Read(ref _disposingThreadId) == Environment.CurrentManagedThreadId)
            return;

        throw new ObjectDisposedException(GetType().FullName);
    }

    /// <summary>Returns a diagnostic string containing the runtime class name and instance identifier.</summary>
    /// <returns>A string in the form <c>&lt;ClassName&gt;#&lt;InstanceID&gt;</c>.</returns>
    public override string ToString() => $"{ClassName}#{InstanceID}";
}
