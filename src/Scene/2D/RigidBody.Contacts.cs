using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

public partial class RigidBody
{
    private static readonly PropertyDescriptor[] ContactProperties =
    [
        new PropertyDescriptor<RigidBody, bool>(nameof(ContactMonitor), body => body.ContactMonitor,
            (body, value) => body.ContactMonitor = value, _ => false, stored: true),
        new PropertyDescriptor<RigidBody, int>(nameof(MaxContactsReported), body => body.MaxContactsReported,
            (body, value) => body.MaxContactsReported = value, _ => 0, stored: true)
    ];

    private HashSet<PhysicsBody> _contacts = new(ReferenceEqualityComparer.Instance);
    private HashSet<PhysicsBody> _nextContacts = new(ReferenceEqualityComparer.Instance);
    private readonly PhysicsShapePairTracker _shapePairs = new();
    private readonly List<PhysicsShapePairChange> _pairChanges = [];
    private bool _contactMonitor;
    private int _maxContactsReported;
    private int _contactCount;
    private bool _dispatchingContact;
    private bool _sleepChangePending;
    internal bool NeedsContactSnapshot => _contactMonitor || _maxContactsReported > 0 || Runtime.View is { IsDisposed: false };

    /// <summary>Gets or sets whether this body reports object-level contact entry and exit.</summary>
    /// <value>False by default; reporting also requires <see cref="MaxContactsReported"/> greater than zero.</value>
    /// <exception cref="InvalidOperationException">The monitor is disabled from inside its contact callback.</exception>
    /// <remarks>Attached access requires the scene owner thread. Disabling clears the body snapshot without exit events.</remarks>
    public bool ContactMonitor
    {
        get { ThrowIfDisposed(); return _contactMonitor; }
        set
        {
            EnsureMutable();
            if (_contactMonitor == value) return;
            if (!value && _dispatchingContact)
                throw new InvalidOperationException("Disable contact monitoring after the contact callback returns.");
            _contactMonitor = value;
            if (!value) { _contacts.Clear(); _nextContacts.Clear(); _shapePairs.Clear(); }
        }
    }

    /// <summary>Gets or sets the maximum number of contact points reported from a fixed step.</summary>
    /// <value>Zero by default, which disables contact-point reporting and contact entry/exit.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned count is outside zero through 4095.</exception>
    /// <remarks>Attached access requires the scene owner thread. The assignment prepares bounded point storage. A full slot set keeps the deepest contacts; equal-depth candidates retain existing slots.</remarks>
    public int MaxContactsReported
    {
        get { ThrowIfDisposed(); return _maxContactsReported; }
        set
        {
            EnsureMutable();
            if ((uint)value > PhysicsBodyRuntime.MaxContactLimit) throw new ArgumentOutOfRangeException(nameof(value));
            _contacts.EnsureCapacity(value); _nextContacts.EnsureCapacity(value);
            _shapePairs.Prepare(value);
            _pairChanges.EnsureCapacity(checked(value * 4));
            PhysicsServer.Service.BodyRuntime(PhysicsRID).View?.PrepareContacts(value);
            _maxContactsReported = value;
            _contactCount = 0;
            Space?.PrepareMonitoringCapacity();
        }
    }

    /// <summary>Occurs when a physics body begins contributing a reported contact.</summary>
    public event Action<Node>? BodyEntered;

    /// <summary>Occurs when a physics body stops contributing reported contacts.</summary>
    public event Action<Node>? BodyExited;

    /// <summary>Occurs when a retained logical body-shape pair starts contributing monitored contact.</summary>
    /// <remarks>Arguments are collider RID, scene Node, collider global logical shape index and this body's global index.
    /// Requires ContactMonitor and a positive reported-contact cap. Compound fixtures are deduplicated by logical pair.</remarks>
    public event Action<RID, Node, int, int>? BodyShapeEntered;
    /// <summary>Occurs when a retained logical body-shape pair stops contributing monitored contact.</summary>
    /// <remarks>The sampled indices and departed RID are preserved; object exit precedes its last shape exit.</remarks>
    public event Action<RID, Node, int, int>? BodyShapeExited;

    /// <summary>Occurs when the solver, rather than a direct property assignment, changes sleep state.</summary>
    public event Action<RigidBody>? SleepingStateChanged;

    /// <summary>Returns the number of contact points retained from the last fixed step.</summary>
    /// <returns>Zero when <see cref="MaxContactsReported"/> is zero or the body is detached.</returns>
    /// <remarks>The count is independent of <see cref="ContactMonitor"/>. Attached access requires the scene owner thread.</remarks>
    public int GetContactCount()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return _contactCount;
    }

    /// <summary>Returns the current object-level contact snapshot.</summary>
    /// <returns>A caller-owned array of spatial bodies; empty while monitoring is disabled.</returns>
    /// <remarks>Multiple shape pairs with the same body appear once. Attached access requires the scene owner thread.</remarks>
    public Entity[] GetCollidingBodies()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        if (!_contactMonitor || _contacts.Count == 0) return [];
        var result = new Entity[_contacts.Count];
        var index = 0;
        foreach (var body in _contacts) result[index++] = body;
        return result;
    }

    internal bool TakeSleepChange()
    {
        var pending = _sleepChangePending;
        _sleepChangePending = false;
        return pending;
    }

    internal void CollectContacts(PhysicsDirectBodyState state)
    {
        _nextContacts.Clear(); _shapePairs.Begin();
        state.BeginContactSnapshot();
        if (HasBackend) state.CaptureContacts();
        if (_contactMonitor)
            foreach (ref readonly var contact in state.CapturedContacts)
                if (contact.SceneCollider is PhysicsBody other && !ReferenceEquals(other, this))
                {
                    _nextContacts.Add(other);
                    _shapePairs.Observe(new(contact.Collider, other, false, contact.ColliderShape, contact.LocalShape));
                }

        _pairChanges.Clear(); _shapePairs.Commit(_pairChanges);
        (_contacts, _nextContacts) = (_nextContacts, _contacts);
        _contactCount = state.CapturedContacts.Length;
    }

    internal void QueueContactChanges(List<PhysicsSpace.ContactEvent> events)
    {
        if (!_contactMonitor) return;
        foreach (var change in _pairChanges) events.Add(new(this, change));
        _pairChanges.Clear();
    }

    internal void ForgetContact(PhysicsBody other, List<PhysicsSpace.ContactEvent> events)
    {
        _nextContacts.Remove(other); _contacts.Remove(other);
        _pairChanges.Clear(); _shapePairs.Forget(other.PhysicsRID, _pairChanges);
        if (_contactMonitor) foreach (var change in _pairChanges) events.Add(new(this, change));
        _pairChanges.Clear();
    }

    internal bool ContainsContact(PhysicsShapePairChange change) => change.ObjectEvent
        ? change.Pair.Other is PhysicsBody body && _contacts.Contains(body) : _shapePairs.Contains(change.Pair);

    internal void ClearContactState()
    {
        _contacts.Clear(); _nextContacts.Clear(); _shapePairs.Clear(); _pairChanges.Clear();
        _contactCount = 0; _sleepChangePending = false;
    }

    internal void CaptureBackendSleep()
    {
        if (Space?.HasBackendFailure == true) return;
        if (b2Body_GetType(BackendID) == B2BodyType.b2_staticBody) return;
        _sleeping = !b2Body_IsAwake(BackendID);
        _sleepChangePending = false;
    }

    internal void RaiseContact(PhysicsShapePairChange change)
    {
        _dispatchingContact = true;
        try
        {
            if (change.Pair.Other is not PhysicsBody other) return;
            if (change.ObjectEvent) { if (change.Entered) BodyEntered?.Invoke(other); else BodyExited?.Invoke(other); }
            else if (change.Entered) BodyShapeEntered?.Invoke(change.Pair.RID, other, change.Pair.OtherShape, change.Pair.LocalShape);
            else BodyShapeExited?.Invoke(change.Pair.RID, other, change.Pair.OtherShape, change.Pair.LocalShape);
        }
        finally { _dispatchingContact = false; }
    }

    internal void RaiseSleepingStateChanged() => SleepingStateChanged?.Invoke(this);
}
