using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

public sealed partial class RigidBody
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
    private B2ContactData[] _contactData = [];
    private bool _contactMonitor;
    private int _maxContactsReported;
    private int _contactCount;
    private bool _dispatchingContact;
    private bool _sleepChangePending;

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
            if (!value) { _contacts.Clear(); _nextContacts.Clear(); }
        }
    }

    /// <summary>Gets or sets the maximum number of contact points reported from a fixed step.</summary>
    /// <value>Zero by default, which disables contact-point reporting and contact entry/exit.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned count is negative.</exception>
    /// <remarks>Attached access requires the scene owner thread. Reported points are capped after each fixed step.</remarks>
    public int MaxContactsReported
    {
        get { ThrowIfDisposed(); return _maxContactsReported; }
        set
        {
            EnsureMutable();
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _maxContactsReported = value;
        }
    }

    /// <summary>Occurs when a physics body begins contributing a reported contact.</summary>
    public event Action<Node>? BodyEntered;

    /// <summary>Occurs when a physics body stops contributing reported contacts.</summary>
    public event Action<Node>? BodyExited;

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

    internal void CollectContacts(PhysicsSpace space, List<PhysicsSpace.ContactEvent> events)
    {
        _nextContacts.Clear();
        var pointCount = 0;
        if (_maxContactsReported > 0 && HasBackend)
        {
            var capacity = b2Body_GetContactCapacity(BackendID);
            if (_contactData.Length < capacity) Array.Resize(ref _contactData, capacity);
            var pairCount = b2Body_GetContactData(BackendID, _contactData, capacity);
            for (var index = 0; index < pairCount && pointCount < _maxContactsReported; index++)
            {
                ref readonly var contact = ref _contactData[index];
                var retained = Math.Min(contact.manifold.pointCount, _maxContactsReported - pointCount);
                if (retained == 0) continue;
                pointCount += retained;
                if (!_contactMonitor) continue;
                var first = b2Shape_GetBody(contact.shapeIdA);
                var second = b2Shape_GetBody(contact.shapeIdB);
                var other = space.FindBody(first == BackendID ? second : first);
                if (other is not null && !ReferenceEquals(other, this)) _nextContacts.Add(other);
            }
        }

        foreach (var other in _contacts)
            if (!_nextContacts.Contains(other)) events.Add(new(this, other, false));
        foreach (var other in _nextContacts)
            if (!_contacts.Contains(other)) events.Add(new(this, other, true));
        (_contacts, _nextContacts) = (_nextContacts, _contacts);
        _contactCount = pointCount;
    }

    internal void ForgetContact(PhysicsBody other, List<PhysicsSpace.ContactEvent> events)
    {
        _nextContacts.Remove(other);
        if (_contacts.Remove(other) && _contactMonitor) events.Add(new(this, other, false));
    }

    internal bool ContainsContact(PhysicsBody other) => _contacts.Contains(other);

    internal void ClearContactState()
    {
        _contacts.Clear();
        _nextContacts.Clear();
        _contactCount = 0;
        _sleepChangePending = false;
    }

    internal void CaptureBackendSleep()
    {
        _sleeping = !b2Body_IsAwake(BackendID);
        _sleepChangePending = false;
    }

    internal void RaiseContact(PhysicsBody other, bool entered)
    {
        _dispatchingContact = true;
        try
        {
            if (entered) BodyEntered?.Invoke(other);
            else BodyExited?.Invoke(other);
        }
        finally { _dispatchingContact = false; }
    }

    internal void RaiseSleepingStateChanged() => SleepingStateChanged?.Invoke(this);
}
