using System.IO.Enumeration;
using System.Threading;

namespace Electron2D;

/// <summary>Provides Electron2D's unified hierarchical, input-aware game object and 2D transform type.</summary>
/// <remarks>
/// The type combines ordered child ownership, tree lifecycle, paths, groups, processing, typed input callbacks, queued
/// deletion, visibility, Z ordering, and 2D spatial state. During Engine.Run, retained rectangle, line and texture
/// commands render with the node's transforms, visibility, modulation and material.
/// </remarks>
public partial class Node : ElectronObject
{
    /// <summary>Identifies the notification sent when a node enters an active <see cref="SceneTree"/>.</summary>
    public const int NotificationEnterTree = 10;

    /// <summary>Identifies the notification sent after descendants exit and before this node leaves its tree.</summary>
    public const int NotificationExitTree = 11;

    /// <summary>Identifies the child-first notification sent when a node becomes ready.</summary>
    public const int NotificationReady = 13;

    /// <summary>Identifies the notification sent when the owning tree becomes paused.</summary>
    public const int NotificationPaused = 14;

    /// <summary>Identifies the notification sent when the owning tree resumes from pause.</summary>
    public const int NotificationUnpaused = 15;

    /// <summary>Identifies a physics-process callback notification.</summary>
    public const int NotificationPhysicsProcess = 16;

    /// <summary>Identifies a process callback notification.</summary>
    public const int NotificationProcess = 17;

    /// <summary>Identifies the notification sent after a parent reference is assigned.</summary>
    public const int NotificationParented = 18;

    /// <summary>Identifies the notification sent after a parent reference is cleared.</summary>
    public const int NotificationUnparented = 19;

    /// <summary>Identifies the notification sent to the root after a packed scene is completely instantiated.</summary>
    public const int NotificationSceneInstantiated = 20;

    /// <summary>Identifies the notification propagated when this node's path changes.</summary>
    public const int NotificationPathRenamed = 23;

    /// <summary>Identifies the notification sent after the direct child order changes.</summary>
    public const int NotificationChildOrderChanged = 24;

    /// <summary>Identifies an engine-internal process callback notification.</summary>
    /// <remarks>Built-in node logic uses this lane independently of <see cref="ProcessEnabled"/>.</remarks>
    public const int NotificationInternalProcess = 25;

    /// <summary>Identifies an engine-internal physics-process callback notification.</summary>
    /// <remarks>Built-in node logic uses this lane independently of <see cref="PhysicsProcessEnabled"/>.</remarks>
    public const int NotificationInternalPhysicsProcess = 26;

    /// <summary>Identifies the notification sent after this node and its descendants finish entering a tree.</summary>
    public const int NotificationPostEnterTree = 27;

    /// <summary>Identifies the notification sent when the effective process mode becomes disabled.</summary>
    public const int NotificationDisabled = 28;

    /// <summary>Identifies the notification sent when the effective process mode stops being disabled.</summary>
    public const int NotificationEnabled = 29;

    /// <summary>Identifies the notification propagated after local or inherited visibility changes.</summary>
    public const int NotificationVisibilityChanged = 31;

    /// <summary>Identifies a local-transform change notification when local notification delivery is enabled.</summary>
    public const int NotificationLocalTransformChanged = 35;

    /// <summary>Identifies a global-transform change notification when global notification delivery is enabled.</summary>
    public const int NotificationTransformChanged = 2000;

    /// <summary>Identifies an operating-system low-memory warning propagated by the active scene tree.</summary>
    public const int NotificationOsMemoryWarning = MainLoop.NotificationOsMemoryWarning;

    /// <summary>Identifies a notification that translated messages may have changed.</summary>
    public const int NotificationTranslationChanged = MainLoop.NotificationTranslationChanged;

    /// <summary>Identifies an operating-system request to show application information.</summary>
    public const int NotificationWmAbout = MainLoop.NotificationWmAbout;

    /// <summary>Identifies a notification delivered immediately before an unrecoverable crash.</summary>
    public const int NotificationCrash = MainLoop.NotificationCrash;

    /// <summary>Identifies an input-method composition update supplied by the operating system.</summary>
    public const int NotificationOsImeUpdate = MainLoop.NotificationOsImeUpdate;

    /// <summary>Identifies that the application resumed after suspension.</summary>
    public const int NotificationApplicationResumed = MainLoop.NotificationApplicationResumed;

    /// <summary>Identifies that the application is about to be suspended.</summary>
    public const int NotificationApplicationPaused = MainLoop.NotificationApplicationPaused;

    /// <summary>Identifies that the application received keyboard focus.</summary>
    public const int NotificationApplicationFocusIn = MainLoop.NotificationApplicationFocusIn;

    /// <summary>Identifies that the application lost keyboard focus.</summary>
    public const int NotificationApplicationFocusOut = MainLoop.NotificationApplicationFocusOut;

    /// <summary>Identifies that the active text service changed.</summary>
    public const int NotificationTextServerChanged = MainLoop.NotificationTextServerChanged;

    /// <summary>Identifies that the application entered picture-in-picture mode.</summary>
    public const int NotificationApplicationPipModeEntered = MainLoop.NotificationApplicationPipModeEntered;

    /// <summary>Identifies that the application exited picture-in-picture mode.</summary>
    public const int NotificationApplicationPipModeExited = MainLoop.NotificationApplicationPipModeExited;

    /// <summary>Specifies the smallest supported local or effective Z index.</summary>
    public const int MinimumZIndex = -4096;

    /// <summary>Specifies the largest supported local or effective Z index.</summary>
    public const int MaximumZIndex = 4096;

    private const float TransformEpsilon = 0.000001f;

    private static readonly AsyncLocal<int> SceneFactoryDepth = new();

    private static readonly IReadOnlyList<PropertyDescriptor> NodeProperties = Array.AsReadOnly<PropertyDescriptor>(
    [
        new PropertyDescriptor<Node, string>(
            nameof(Name),
            node => node.Name,
            (node, value) => node.Name = value,
            node => node.ClassName,
            (_, value) => IsValidNodeName(value),
            stored: true),
        new PropertyDescriptor<Node, Vector2>(
            nameof(Position),
            node => node.Position,
            (node, value) => node.Position = value,
            _ => Vector2.Zero,
            (_, value) => IsFinite(value),
            stored: true),
        new PropertyDescriptor<Node, float>(
            nameof(RotationDegrees),
            node => node.RotationDegrees,
            (node, value) => node.RotationDegrees = value,
            _ => 0f,
            (_, value) => Mathf.IsFinite(value),
            stored: true),
        new PropertyDescriptor<Node, Vector2>(
            nameof(Scale),
            node => node.Scale,
            (node, value) => node.Scale = value,
            _ => Vector2.One,
            (_, value) => IsFinite(value),
            stored: true),
        new PropertyDescriptor<Node, float>(
            nameof(Skew),
            node => node.Skew,
            (node, value) => node.Skew = value,
            _ => 0f,
            (_, value) => Mathf.IsFinite(value),
            stored: true),
        new PropertyDescriptor<Node, bool>(nameof(Visible), node => node.Visible, (node, value) => node.Visible = value, _ => true, stored: true),
        new PropertyDescriptor<Node, int>(
            nameof(ZIndex),
            node => node.ZIndex,
            (node, value) => node.ZIndex = value,
            _ => 0,
            (_, value) => value is >= MinimumZIndex and <= MaximumZIndex,
            stored: true),
        new PropertyDescriptor<Node, bool>(nameof(ZAsRelative), node => node.ZAsRelative, (node, value) => node.ZAsRelative = value, _ => true, stored: true),
        new PropertyDescriptor<Node, bool>(nameof(TopLevel), node => node.TopLevel, (node, value) => node.TopLevel = value, _ => false, stored: true),
        new PropertyDescriptor<Node, NodeProcessMode>(
            nameof(ProcessMode),
            node => node.ProcessMode,
            (node, value) => node.ProcessMode = value,
            _ => NodeProcessMode.Inherit,
            (_, value) => Enum.IsDefined(value),
            stored: true),
        new PropertyDescriptor<Node, bool>(nameof(ProcessEnabled), node => node.ProcessEnabled, (node, value) => node.ProcessEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<Node, bool>(nameof(PhysicsProcessEnabled), node => node.PhysicsProcessEnabled, (node, value) => node.PhysicsProcessEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<Node, bool>(nameof(InputEnabled), node => node.InputEnabled, (node, value) => node.InputEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<Node, bool>(nameof(UnhandledInputEnabled), node => node.UnhandledInputEnabled, (node, value) => node.UnhandledInputEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<Node, bool>(nameof(UnhandledKeyInputEnabled), node => node.UnhandledKeyInputEnabled, (node, value) => node.UnhandledKeyInputEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<Node, int>(nameof(ProcessPriority), node => node.ProcessPriority, (node, value) => node.ProcessPriority = value, _ => 0, stored: true),
        new PropertyDescriptor<Node, int>(nameof(PhysicsProcessPriority), node => node.PhysicsProcessPriority, (node, value) => node.PhysicsProcessPriority = value, _ => 0, stored: true)
    ]);

    private readonly List<Node> _children = [];
    private readonly IReadOnlyList<Node> _childrenView;
    private readonly Dictionary<string, bool> _groups = new(StringComparer.Ordinal);
    private Transform _transform = Transform.Identity;
    private string _name;
    private string _sceneFilePath = string.Empty;
    private Node? _owner;
    private List<Resource>? _ownedSceneResources;
    private int _queuedForDeletion;
    private int _sceneCaptureDepth;
    private int _sceneInstantiationDepth;
    private bool _readyCalled;
    private bool _isEnteringTree;
    private bool _isMakingReady;
    private bool _isExitingTree;
    private bool _visible = true;
    private bool _zAsRelative = true;
    private bool _topLevel;
    private bool _notifyLocalTransformChanges;
    private bool _notifyTransformChanges;
    private int _zIndex;
    private int _processPriority;
    private int _physicsProcessPriority;
    private NodeProcessMode _processMode;
    private bool _processEnabled;
    private bool _physicsProcessEnabled;
    private bool _inputEnabled;
    private bool _unhandledInputEnabled;
    private bool _unhandledKeyInputEnabled;
    private bool _internalProcessEnabled;
    private bool _internalPhysicsProcessEnabled;
    private double _unscaledProcessDeltaTime;
    private double _unscaledPhysicsProcessDeltaTime;

    /// <summary>Initializes a detached node with its runtime class name and an identity transform.</summary>
    public Node()
    {
        _name = ClassName;
        _childrenView = _children.AsReadOnly();
    }

    /// <summary>Gets or sets the node name used in sibling lookup and paths.</summary>
    /// <value>The nonblank name, initialized to <see cref="ElectronObject.ClassName"/>.</value>
    /// <remarks>
    /// Names use ordinal equality among siblings and cannot be <c>.</c>, <c>..</c>, or contain <c>/</c>. Renaming an
    /// active node propagates <see cref="NotificationPathRenamed"/> through its subtree and then raises
    /// <see cref="Renamed"/> on this node.
    /// </remarks>
    /// <exception cref="ArgumentException">The assigned name is invalid.</exception>
    /// <exception cref="InvalidOperationException">A sibling already has the assigned name, or an attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="AggregateException">One or more path, node-renamed, or tree-renamed callbacks fail after the name changes.</exception>
    public string Name
    {
        get
        {
            ThrowIfDisposed();
            return _name;
        }
        set
        {
            EnsureMutable();

            if (!IsValidNodeName(value))
                throw new ArgumentException("A node name cannot be empty, '.', '..', or contain '/'.", nameof(value));

            if (StringComparer.Ordinal.Equals(_name, value))
                return;

            Parent?.EnsureChildNameAvailable(value, this);
            _name = value;

            if (IsInsideTree)
            {
                List<Exception>? errors = null;

                try
                {
                    PropagatePathRenamed();
                }
                catch (Exception error)
                {
                    CollectException(ref errors, error);
                }

                try
                {
                    Renamed?.Invoke(this);
                }
                catch (Exception error)
                {
                    CollectException(ref errors, error);
                }

                try
                {
                    Tree?.NotifyNodeRenamed(this);
                }
                catch (Exception error)
                {
                    CollectException(ref errors, error);
                }

                ThrowCollected("One or more node-renamed callbacks failed.", errors);
            }
        }
    }

    /// <summary>Gets the direct parent.</summary>
    /// <value>The owning parent, or <see langword="null"/> while detached.</value>
    public Node? Parent { get; private set; }

    /// <summary>Gets the external resource path from which this scene root was instantiated.</summary>
    /// <value>The packed-scene path for an instantiated external scene root; otherwise an empty string.</value>
    /// <remarks>The value is assigned by packed-scene instantiation and is not inherited by descendants.</remarks>
    public string SceneFilePath
    {
        get
        {
            ThrowIfDisposed();
            return _sceneFilePath;
        }
    }

    /// <summary>Gets or sets the ancestor that owns this node for packed-scene storage.</summary>
    /// <value>An ancestor node, or <see langword="null"/> when this node is not stored by an ancestor scene root.</value>
    /// <remarks>
    /// A scene root does not own itself. Removing or reparenting a subtree automatically clears owner references that
    /// no longer point to an ancestor.
    /// </remarks>
    /// <exception cref="ArgumentException">The assigned node is this node or is not an ancestor.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the tree owner thread or scene capture is active.</exception>
    /// <exception cref="ObjectDisposedException">This node or the assigned owner is disposing or disposed.</exception>
    public Node? Owner
    {
        get
        {
            ThrowIfDisposed();
            return _owner;
        }
        set
        {
            EnsureMutable();

            if (ReferenceEquals(_owner, value))
                return;

            if (ReferenceEquals(this, value))
                throw new ArgumentException("A node cannot own itself.", nameof(value));

            if (value is not null)
            {
                ObjectDisposedException.ThrowIf(value.IsDisposed, value);
                if (!value.IsAncestorOf(this))
                    throw new ArgumentException("A node owner must be an ancestor.", nameof(value));
            }

            _owner = value;
        }
    }

    /// <summary>Gets a live read-only view of the ordered direct children.</summary>
    /// <value>A view backed by this node's child list; later hierarchy changes are visible through it.</value>
    public IReadOnlyList<Node> Children => _childrenView;

    /// <summary>Gets the number of direct children.</summary>
    /// <value>The current child count.</value>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public int ChildCount
    {
        get
        {
            ThrowIfDisposed();
            return _children.Count;
        }
    }

    /// <summary>Gets the active scene tree containing this node.</summary>
    /// <value>The owning tree, or <see langword="null"/> while detached.</value>
    public SceneTree? Tree { get; private set; }

    /// <summary>Gets whether this node currently belongs to a scene tree.</summary>
    /// <value><see langword="true"/> when <see cref="Tree"/> is non-null.</value>
    public bool IsInsideTree => Tree is not null;

    /// <summary>Gets whether SceneTree-managed ready delivery has occurred since construction or the last ready reset.</summary>
    /// <value>The stored ready state. It remains true after detachment until <see cref="RequestReady"/> is called.</value>
    /// <remarks>Manual <see cref="ElectronObject.Notify(int)"/> delivery of <see cref="NotificationReady"/> does not change this value.</remarks>
    public bool IsNodeReady => _readyCalled;

    /// <summary>Gets whether deletion has been requested through <see cref="QueueFree"/>.</summary>
    /// <value>An atomic snapshot of the deletion-request flag.</value>
    public bool IsQueuedForDeletion => Volatile.Read(ref _queuedForDeletion) != 0;

    /// <summary>Gets or sets the affine transform relative to the parent.</summary>
    /// <value>A finite <see cref="Electron2D.Transform"/>; the default is <see cref="Electron2D.Transform.Identity"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">An assigned transform component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated from a thread other than the tree owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the transform changes.</exception>
    public Transform Transform
    {
        get
        {
            ThrowIfDisposed();
            return _transform;
        }
        set
        {
            EnsureMutable();
            SetTransform(value);
        }
    }

    /// <summary>Gets or sets the affine transform in hierarchy-global coordinates.</summary>
    /// <value>The local transform composed with non-top-level ancestors.</value>
    /// <exception cref="ArgumentOutOfRangeException">An assigned transform component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or an attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the transform changes.</exception>
    public Transform GlobalTransform
    {
        get
        {
            ThrowIfDisposed();
            return Parent is null || TopLevel ? _transform : Parent.GlobalTransform * _transform;
        }
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            SetTransform(ToLocalTransform(value, Parent, TopLevel));
        }
    }

    /// <summary>Gets or sets local translation in pixels or other host-defined 2D units.</summary>
    /// <value>The translation component of <see cref="Transform"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">An assigned component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the position changes.</exception>
    public Vector2 Position
    {
        get
        {
            ThrowIfDisposed();
            return _transform.Origin;
        }
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));

            var transform = _transform;
            transform.Origin = value;
            SetTransform(transform);
        }
    }

    /// <summary>Gets or sets translation in hierarchy-global coordinates.</summary>
    /// <value>The translation component of <see cref="GlobalTransform"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">An assigned component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or an attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the position changes.</exception>
    public Vector2 GlobalPosition
    {
        get => GlobalTransform.Origin;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));

            var transform = GlobalTransform;
            transform.Origin = value;
            GlobalTransform = transform;
        }
    }

    /// <summary>Gets or sets local rotation in radians.</summary>
    /// <value>The canonical rotation decomposed from <see cref="Transform"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned angle is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the rotation changes.</exception>
    public float Rotation
    {
        get => Transform.Rotation;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            SetTransform(new Transform(value, _transform.Scale, _transform.Skew, _transform.Origin));
        }
    }

    /// <summary>Gets or sets local rotation in degrees.</summary>
    /// <value><see cref="Rotation"/> converted between radians and degrees.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned angle is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the rotation changes.</exception>
    public float RotationDegrees
    {
        get => Mathf.RadToDeg(Rotation);
        set
        {
            EnsureFinite(value, "degrees");
            Rotation = Mathf.DegToRad(value);
        }
    }

    /// <summary>Gets or sets hierarchy-global rotation in radians.</summary>
    /// <value>The canonical rotation decomposed from <see cref="GlobalTransform"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned angle is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or an attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the rotation changes.</exception>
    public float GlobalRotation
    {
        get => GlobalTransform.Rotation;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            var transform = GlobalTransform;
            GlobalTransform = new Transform(value, transform.Scale, transform.Skew, transform.Origin);
        }
    }

    /// <summary>Gets or sets hierarchy-global rotation in degrees.</summary>
    /// <value><see cref="GlobalRotation"/> converted between radians and degrees.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned angle is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or an attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the rotation changes.</exception>
    public float GlobalRotationDegrees
    {
        get => Mathf.RadToDeg(GlobalRotation);
        set
        {
            EnsureFinite(value, "degrees");
            GlobalRotation = Mathf.DegToRad(value);
        }
    }

    /// <summary>Gets or sets local scale.</summary>
    /// <value>The canonical scale decomposed from <see cref="Transform"/>.</value>
    /// <remarks>Equivalent reflected matrices can decompose to a different but equivalent rotation, scale, and skew tuple.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An assigned component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the scale changes.</exception>
    public Vector2 Scale
    {
        get => Transform.Scale;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            SetTransform(new Transform(_transform.Rotation, value, _transform.Skew, _transform.Origin));
        }
    }

    /// <summary>Gets or sets hierarchy-global scale.</summary>
    /// <value>The canonical scale decomposed from <see cref="GlobalTransform"/>.</value>
    /// <remarks>Equivalent reflected matrices can decompose to a different but equivalent rotation, scale, and skew tuple.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An assigned component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or an attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the scale changes.</exception>
    public Vector2 GlobalScale
    {
        get => GlobalTransform.Scale;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            var transform = GlobalTransform;
            GlobalTransform = new Transform(transform.Rotation, value, transform.Skew, transform.Origin);
        }
    }

    /// <summary>Gets or sets the local skew angle in radians.</summary>
    /// <value>The canonical angle between the transformed basis axes relative to an unskewed basis.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned angle is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the skew changes.</exception>
    public float Skew
    {
        get => Transform.Skew;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            SetTransform(new Transform(_transform.Rotation, _transform.Scale, value, _transform.Origin));
        }
    }

    /// <summary>Gets or sets the hierarchy-global skew angle in radians.</summary>
    /// <value>The canonical skew decomposed from <see cref="GlobalTransform"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned angle is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or an attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the skew changes.</exception>
    public float GlobalSkew
    {
        get => GlobalTransform.Skew;
        set
        {
            EnsureMutable();
            EnsureFinite(value, nameof(value));
            var transform = GlobalTransform;
            GlobalTransform = new Transform(transform.Rotation, transform.Scale, value, transform.Origin);
        }
    }

    /// <summary>Gets or sets whether this node ignores its parent's transform.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>The current global transform is preserved when the mode changes.</remarks>
    /// <exception cref="InvalidOperationException">The parent transform is singular when disabling top-level mode, or mutation occurs off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the mode changes.</exception>
    public bool TopLevel
    {
        get
        {
            ThrowIfDisposed();
            return _topLevel;
        }
        set
        {
            EnsureMutable();

            if (_topLevel == value)
                return;

            var global = GlobalTransform;
            var local = ToLocalTransform(global, Parent, value);
            _topLevel = value;
            SetTransform(local);
        }
    }

    /// <summary>Gets or sets this node's local logical visibility.</summary>
    /// <value><see langword="true"/> by default.</value>
    /// <remarks>An actual change synchronously propagates visibility notifications and events through all descendants.</remarks>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A visibility notification or event handler throws after visibility changes.</exception>
    public virtual bool Visible
    {
        get
        {
            ThrowIfDisposed();
            return _visible;
        }
        set
        {
            EnsureMutable();

            if (_visible == value)
                return;

            _visible = value;
            PropagateVisibilityChanged();
        }
    }

    /// <summary>Finds this node's nearest viewport, including itself.</summary>
    /// <returns>The nearest viewport ancestor, or null in a hierarchy without a viewport.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Viewport? GetViewport()
    {
        ThrowIfDisposed();
        for (Node? node = this; node is not null; node = node.Parent)
            if (node is Viewport viewport)
                return viewport;
        return null;
    }

    /// <summary>Finds this node's containing window, including itself.</summary>
    /// <returns>The nearest window ancestor, or null in a hierarchy without a window.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Window? GetWindow() => GetViewport() as Window;

    /// <summary>Gets whether this node is active and locally visible through its complete ancestor chain.</summary>
    /// <value><see langword="true"/> only inside a tree when this node and every ancestor are visible.</value>
    /// <exception cref="ObjectDisposedException">This node or a queried ancestor is disposing on another thread, or has finished disposing.</exception>
    public bool IsVisibleInTree => IsInsideTree && Visible && (Parent?.IsVisibleInTree ?? true);

    /// <summary>Gets or sets this node's local Z-order value.</summary>
    /// <value>An integer from <see cref="MinimumZIndex"/> through <see cref="MaximumZIndex"/>; the default is zero.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is outside the supported range.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public int ZIndex
    {
        get
        {
            ThrowIfDisposed();
            return _zIndex;
        }
        set
        {
            EnsureMutable();

            if (value is < MinimumZIndex or > MaximumZIndex)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Z index must be between {MinimumZIndex} and {MaximumZIndex}.");

            _zIndex = value;
        }
    }

    /// <summary>Gets or sets whether effective Z order accumulates ancestor Z values.</summary>
    /// <value><see langword="true"/> by default.</value>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool ZAsRelative
    {
        get
        {
            ThrowIfDisposed();
            return _zAsRelative;
        }
        set
        {
            EnsureMutable();
            _zAsRelative = value;
        }
    }

    /// <summary>Gets the Z order after optional ancestor accumulation.</summary>
    /// <value>The accumulated or absolute value, clamped to the supported Z range.</value>
    /// <exception cref="ObjectDisposedException">This node or a queried ancestor is disposing on another thread, or has finished disposing.</exception>
    internal int EffectiveZIndex => ZAsRelative && Parent is not null
        ? Mathf.Clamp(Parent.EffectiveZIndex + ZIndex, MinimumZIndex, MaximumZIndex)
        : ZIndex;

    /// <summary>Gets or sets whether local transform changes dispatch <see cref="NotificationLocalTransformChanged"/>.</summary>
    /// <value><see langword="false"/> by default. <see cref="LocalTransformChanged"/> is raised regardless.</value>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool NotifyLocalTransformChanges
    {
        get
        {
            ThrowIfDisposed();
            return _notifyLocalTransformChanges;
        }
        set
        {
            EnsureMutable();
            _notifyLocalTransformChanges = value;
        }
    }

    /// <summary>Gets or sets whether global transform changes dispatch <see cref="NotificationTransformChanged"/>.</summary>
    /// <value><see langword="false"/> by default. <see cref="TransformChanged"/> is raised regardless.</value>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool NotifyTransformChanges
    {
        get
        {
            ThrowIfDisposed();
            return _notifyTransformChanges;
        }
        set
        {
            EnsureMutable();
            _notifyTransformChanges = value;
        }
    }

    /// <summary>Gets or sets the pause policy used by both process callback lanes.</summary>
    /// <value><see cref="NodeProcessMode.Inherit"/> by default.</value>
    /// <remarks>Crossing the effective disabled boundary synchronously notifies this node and affected inheriting descendants.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned enum value is undefined.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">An enabled or disabled notification callback throws after the mode changes.</exception>
    public NodeProcessMode ProcessMode
    {
        get
        {
            ThrowIfDisposed();
            return _processMode;
        }
        set
        {
            EnsureMutable();

            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown process mode.");

            if (_processMode == value)
                return;

            var disabledStates = EnumerateDepthFirst().ToDictionary(node => node, node => node.ResolveProcessMode() == NodeProcessMode.Disabled);
            _processMode = value;

            foreach (var (node, wasDisabled) in disabledStates)
            {
                var isDisabled = node.ResolveProcessMode() == NodeProcessMode.Disabled;
                if (wasDisabled != isDisabled)
                    node.DispatchNotification(isDisabled ? NotificationDisabled : NotificationEnabled);
            }
        }
    }

    /// <summary>Gets or sets whether this node participates in host-driven process frames.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool ProcessEnabled
    {
        get
        {
            ThrowIfDisposed();
            return _processEnabled;
        }
        set
        {
            EnsureMutable();
            _processEnabled = value;
        }
    }

    /// <summary>Gets or sets whether this node participates in host-driven physics-process frames.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool PhysicsProcessEnabled
    {
        get
        {
            ThrowIfDisposed();
            return _physicsProcessEnabled;
        }
        set
        {
            EnsureMutable();
            _physicsProcessEnabled = value;
        }
    }

    /// <summary>Gets or sets whether this node receives the first input-propagation stage.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>Eligible nodes are visited in reverse depth-first order before unhandled-input stages.</remarks>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool InputEnabled
    {
        get
        {
            ThrowIfDisposed();
            return _inputEnabled;
        }
        set
        {
            EnsureMutable();
            _inputEnabled = value;
        }
    }

    /// <summary>Gets or sets whether this node receives input left unhandled by earlier stages.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>This final stage runs for every event that remains unhandled.</remarks>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool UnhandledInputEnabled
    {
        get
        {
            ThrowIfDisposed();
            return _unhandledInputEnabled;
        }
        set
        {
            EnsureMutable();
            _unhandledInputEnabled = value;
        }
    }

    /// <summary>Gets or sets whether this node receives unhandled keyboard events before general unhandled input.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>The stage is skipped for non-keyboard events and after <see cref="SceneTree.SetInputAsHandled"/>.</remarks>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public bool UnhandledKeyInputEnabled
    {
        get
        {
            ThrowIfDisposed();
            return _unhandledKeyInputEnabled;
        }
        set
        {
            EnsureMutable();
            _unhandledKeyInputEnabled = value;
        }
    }

    /// <summary>Gets or sets this node's ascending process-frame order key.</summary>
    /// <value>Any integer; the default is zero. Equal priorities retain captured tree order.</value>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public int ProcessPriority
    {
        get
        {
            ThrowIfDisposed();
            return _processPriority;
        }
        set
        {
            EnsureMutable();
            _processPriority = value;
        }
    }

    /// <summary>Gets or sets this node's ascending physics-process order key.</summary>
    /// <value>Any integer; the default is zero. Equal priorities retain captured tree order.</value>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public int PhysicsProcessPriority
    {
        get
        {
            ThrowIfDisposed();
            return _physicsProcessPriority;
        }
        set
        {
            EnsureMutable();
            _physicsProcessPriority = value;
        }
    }

    /// <summary>Gets the delta from the most recent SceneTree-managed process frame delivered to this node.</summary>
    /// <value>The last delivered process delta in seconds, or zero before the first managed delivery.</value>
    /// <remarks><see cref="Engine"/> applies <see cref="Engine.TimeScale"/> before an Engine-driven delivery.</remarks>
    /// <remarks>Manual <see cref="ElectronObject.Notify(int)"/> delivery does not update this value.</remarks>
    public double ProcessDeltaTime { get; private set; }

    /// <summary>Gets the delta from the most recent SceneTree-managed physics-process frame delivered to this node.</summary>
    /// <value>The last delivered physics-process delta in seconds, or zero before the first managed delivery.</value>
    /// <remarks><see cref="Engine"/> applies <see cref="Engine.TimeScale"/> before an Engine-driven delivery.</remarks>
    /// <remarks>Manual <see cref="ElectronObject.Notify(int)"/> delivery does not update this value.</remarks>
    public double PhysicsProcessDeltaTime { get; private set; }

    /// <summary>Occurs on the parent after a direct child is structurally attached and child order is reported.</summary>
    /// <remarks>
    /// The first argument is the publishing parent and the second is the child. Delivery is synchronous and precedes
    /// active-tree attachment of the child's subtree.
    /// </remarks>
    public event Action<Node, Node>? ChildAdded;

    /// <summary>Occurs on the former parent after a direct child is detached and child order is reported.</summary>
    /// <remarks>
    /// The first argument is the publishing former parent and the second is the removed child. Delivery is synchronous,
    /// and structural changes are not rolled back if a handler throws.
    /// </remarks>
    public event Action<Node, Node>? ChildRemoved;

    /// <summary>Occurs on the direct parent when a child enters the active tree.</summary>
    /// <remarks>
    /// The first argument is the publishing parent and the second is the entering child. Delivery follows that child's
    /// enter notification and event.
    /// </remarks>
    public event Action<Node, Node>? ChildEnteredTree;

    /// <summary>Occurs on the direct parent while a child is exiting the active tree.</summary>
    /// <remarks>
    /// The first argument is the publishing parent and the second is the exiting child. Descendants have already exited,
    /// and the child's <see cref="Tree"/> is still set.
    /// </remarks>
    public event Action<Node, Node>? ChildExitingTree;

    /// <summary>Occurs after the order or membership of direct children changes.</summary>
    /// <remarks>The argument is this parent node. Delivery is synchronous after <see cref="NotificationChildOrderChanged"/>.</remarks>
    public event Action<Node>? ChildOrderChanged;

    /// <summary>Occurs after an active node's own name changes and path notifications propagate.</summary>
    /// <remarks>The argument is this node. Detached-node renames do not raise the event.</remarks>
    public event Action<Node>? Renamed;

    /// <summary>Occurs when this node enters an active scene tree.</summary>
    /// <remarks>Delivery follows <see cref="NotificationEnterTree"/> and precedes descendant entry.</remarks>
    public event Action<Node>? TreeEntered;

    /// <summary>Occurs while this node is exiting its active scene tree.</summary>
    /// <remarks>Descendants have exited, <see cref="NotificationExitTree"/> has run, and <see cref="Tree"/> remains available.</remarks>
    public event Action<Node>? TreeExiting;

    /// <summary>Occurs after this node has left its scene tree.</summary>
    /// <remarks><see cref="Tree"/> is already <see langword="null"/> when handlers run.</remarks>
    public event Action<Node>? TreeExited;

    /// <summary>Occurs after child-first ready notification delivery.</summary>
    /// <remarks>SceneTree-managed delivery occurs once until <see cref="RequestReady"/> resets the ready state.</remarks>
    public event Action<Node>? Ready;

    /// <summary>Occurs after local or inherited logical visibility is propagated to this node.</summary>
    /// <remarks>Delivery follows <see cref="NotificationVisibilityChanged"/> and continues through descendants.</remarks>
    public event Action<Node>? VisibilityChanged;

    /// <summary>Occurs after this node's local transform actually changes.</summary>
    /// <remarks>The event is always enabled; numeric local-transform notification delivery is separately configurable.</remarks>
    public event Action<Node>? LocalTransformChanged;

    /// <summary>Occurs when this node's global transform is affected by a local or ancestor change.</summary>
    /// <remarks>Propagation stops at top-level descendants. The event is independent of numeric transform notifications.</remarks>
    public event Action<Node>? TransformChanged;

    /// <summary>Appends a detached node as the last direct child.</summary>
    /// <param name="child">The live node to adopt.</param>
    /// <remarks>If this node is active, the child's subtree enters immediately and receives ready where eligible.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="child"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="child"/> is this node.</exception>
    /// <exception cref="InvalidOperationException">
    /// The operation would create a cycle, the child already has a parent or tree, a sibling name conflicts, or mutation
    /// occurs off the owner thread.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// This node is disposing on another thread or has finished disposing, or disposal of <paramref name="child"/> has started.
    /// </exception>
    /// <exception cref="AggregateException">One or more structural, lifecycle, notification, or event callbacks fail after insertion begins.</exception>
    public void AddChild(Node child) => InsertChild(child, _children.Count);

    /// <summary>Inserts a detached node immediately after this node in its parent's child order.</summary>
    /// <param name="sibling">The live node to insert.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sibling"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sibling"/> is the destination parent.</exception>
    /// <exception cref="InvalidOperationException">
    /// This node has no parent, insertion would create a cycle, the sibling is already attached, a name conflicts, or
    /// mutation occurs off the owner thread.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// This node or its parent is disposing on another thread or has finished disposing, or disposal of
    /// <paramref name="sibling"/> has started.
    /// </exception>
    /// <exception cref="AggregateException">One or more structural, lifecycle, notification, or event callbacks fail after insertion begins.</exception>
    public void AddSibling(Node sibling)
    {
        ThrowIfDisposed();

        if (Parent is null)
            throw new InvalidOperationException("A root or detached node cannot add a sibling.");

        Parent.InsertChild(sibling, GetIndex() + 1);
    }

    /// <summary>Removes a direct child without disposing it.</summary>
    /// <param name="child">The node to detach.</param>
    /// <returns><see langword="true"/> when the node was a direct child and was detached; otherwise <see langword="false"/>.</returns>
    /// <remarks>An active subtree exits its tree child-first before the parent reference is cleared.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="child"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread, this parent is exiting, or the child is in tree lifecycle delivery.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="AggregateException">One or more lifecycle, notification, or event callbacks fail after removal begins.</exception>
    public bool RemoveChild(Node child)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(child);
        return RemoveChildCore(child);
    }

    /// <summary>Moves a direct child to another sibling index.</summary>
    /// <param name="child">The direct child to reorder.</param>
    /// <param name="index">The destination index; negative values count from the end, with <c>-1</c> selecting the last position.</param>
    /// <exception cref="ArgumentNullException"><paramref name="child"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="child"/> is not a direct child.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> does not resolve to an existing child position.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="AggregateException">One or more child-order or tree-change callbacks fail after the order changes.</exception>
    public void MoveChild(Node child, int index)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(child);

        var oldIndex = _children.IndexOf(child);
        if (oldIndex < 0)
            throw new ArgumentException("The node is not a direct child.", nameof(child));

        index = NormalizeChildIndex(index, _children.Count);
        if (oldIndex == index)
            return;

        _children.RemoveAt(oldIndex);
        _children.Insert(index, child);
        List<Exception>? errors = null;

        try
        {
            NotifyChildOrderChanged();
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            Tree?.NotifyTreeChanged();
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        ThrowCollected("One or more child-order callbacks failed.", errors);
    }

    /// <summary>Moves this node to the last position among its siblings.</summary>
    /// <remarks>A detached or hierarchy-root node is left unchanged.</remarks>
    /// <exception cref="InvalidOperationException">An attached parent is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or its parent is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="AggregateException">One or more child-order or tree-change callbacks fail after the order changes.</exception>
    public void MoveToFront()
    {
        ThrowIfDisposed();
        Parent?.MoveChild(this, -1);
    }

    /// <summary>Moves this non-root node under a new parent.</summary>
    /// <param name="newParent">The live destination parent.</param>
    /// <param name="keepGlobalTransform">Whether to preserve the complete current global transform. The default is <see langword="true"/>.</param>
    /// <remarks>The operation detaches first and then appends to <paramref name="newParent"/>; callback failures are not rolled back.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="newParent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The destination validation rejects this node as its own child.</exception>
    /// <exception cref="InvalidOperationException">
    /// This node has no parent, the move creates a cycle, a destination child name conflicts, either attached hierarchy
    /// is accessed off its owner thread, this node or its current parent is in protected tree lifecycle delivery, or the destination parent transform is singular while
    /// <paramref name="keepGlobalTransform"/> is true.
    /// </exception>
    /// <exception cref="ObjectDisposedException">This node or <paramref name="newParent"/> is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="AggregateException">One or more structural, lifecycle, notification, or event callbacks fail after reparenting begins.</exception>
    public void Reparent(Node newParent, bool keepGlobalTransform = true)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(newParent);

        if (_isEnteringTree || _isMakingReady || _isExitingTree)
            throw new InvalidOperationException("A node cannot be reparented while its tree lifecycle is in progress.");

        if (Parent is null)
            throw new InvalidOperationException("A root or detached node cannot be reparented.");

        if (ReferenceEquals(Parent, newParent))
            return;

        newParent.ValidateChildForInsertion(this, allowExistingParent: true);
        var localTransform = keepGlobalTransform
            ? ToLocalTransform(GlobalTransform, newParent, TopLevel)
            : _transform;
        var retainedOwners = EnumerateDepthFirst()
            .Where(node => node._owner is not null)
            .Select(node => (Node: node, Owner: node._owner!))
            .ToArray();

        var oldParent = Parent;
        oldParent.EnsureMutable();
        oldParent.RemoveChildCore(this);
        _transform = localTransform;
        try
        {
            newParent.InsertChild(this, newParent._children.Count);
        }
        finally
        {
            if (ReferenceEquals(Parent, newParent))
            {
                foreach (var (node, owner) in retainedOwners)
                {
                    if (owner.IsAncestorOf(node))
                        node._owner = owner;
                }
            }
        }
    }

    /// <summary>Gets a direct child by index.</summary>
    /// <param name="index">The child index; negative values count from the end.</param>
    /// <returns>The selected direct child.</returns>
    /// <exception cref="ArgumentOutOfRangeException">This node has no children or <paramref name="index"/> is outside the valid range.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public Node GetChild(int index)
    {
        ThrowIfDisposed();
        index = NormalizeChildIndex(index, _children.Count);
        return _children[index];
    }

    /// <summary>Gets this node's index in its parent's ordered child list.</summary>
    /// <returns>The zero-based sibling index, or <c>-1</c> when this node has no parent.</returns>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public int GetIndex()
    {
        ThrowIfDisposed();
        return Parent?._children.IndexOf(this) ?? -1;
    }

    /// <summary>Determines whether this node is a strict ancestor of another node.</summary>
    /// <param name="node">The node whose parent chain is inspected.</param>
    /// <returns><see langword="true"/> when this node appears in the parent chain; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public bool IsAncestorOf(Node node)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(node);

        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, this))
                return true;
        }

        return false;
    }

    /// <summary>Finds the first descendant whose name matches a wildcard pattern.</summary>
    /// <param name="pattern">A nonblank simple expression using <c>*</c> and <c>?</c>; matching is case-insensitive.</param>
    /// <param name="recursive">Whether descendants below direct children are searched.</param>
    /// <returns>The first matching node in depth-first pre-order, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This node or a recursively searched node is disposing on another thread, or has finished disposing.</exception>
    public Node? FindChild(string pattern, bool recursive = true) => FindChild<Node>(pattern, recursive);

    /// <summary>Finds the first descendant of a requested type whose name matches a wildcard pattern.</summary>
    /// <typeparam name="TNode">The required node subtype.</typeparam>
    /// <param name="pattern">A nonblank simple expression using <c>*</c> and <c>?</c>; matching is case-insensitive.</param>
    /// <param name="recursive">Whether descendants below direct children are searched.</param>
    /// <returns>The first typed match in depth-first pre-order, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This node or a recursively searched node is disposing on another thread, or has finished disposing.</exception>
    public TNode? FindChild<TNode>(string pattern = "*", bool recursive = true)
        where TNode : Node
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        foreach (var child in _children)
        {
            if (child is TNode typedChild && FileSystemName.MatchesSimpleExpression(pattern, child.Name))
                return typedChild;

            if (recursive && child.FindChild<TNode>(pattern, recursive: true) is { } descendant)
                return descendant;
        }

        return null;
    }

    /// <summary>Finds all descendants whose names match a wildcard pattern.</summary>
    /// <param name="pattern">A nonblank simple expression using <c>*</c> and <c>?</c>; matching is case-insensitive.</param>
    /// <param name="recursive">Whether descendants below direct children are searched.</param>
    /// <returns>A read-only snapshot in depth-first pre-order.</returns>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public IReadOnlyList<Node> FindChildren(string pattern, bool recursive = true) => FindChildren<Node>(pattern, recursive);

    /// <summary>Finds all descendants of a requested type whose names match a wildcard pattern.</summary>
    /// <typeparam name="TNode">The required node subtype.</typeparam>
    /// <param name="pattern">A nonblank simple expression using <c>*</c> and <c>?</c>; matching is case-insensitive.</param>
    /// <param name="recursive">Whether descendants below direct children are searched.</param>
    /// <returns>A read-only typed snapshot in depth-first pre-order.</returns>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public IReadOnlyList<TNode> FindChildren<TNode>(string pattern = "*", bool recursive = true)
        where TNode : Node
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var result = new List<TNode>();
        FindChildrenCore(pattern, recursive, result);
        return result.AsReadOnly();
    }

    /// <summary>Finds the nearest ancestor whose name matches a wildcard pattern.</summary>
    /// <param name="pattern">A nonblank simple expression using <c>*</c> and <c>?</c>; matching is case-insensitive.</param>
    /// <returns>The nearest matching ancestor, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public Node? FindParent(string pattern)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        for (var current = Parent; current is not null; current = current.Parent)
        {
            if (FileSystemName.MatchesSimpleExpression(pattern, current.Name))
                return current;
        }

        return null;
    }

    /// <summary>Builds this node's absolute path from the root of its current hierarchy.</summary>
    /// <returns>A slash-prefixed path that includes the hierarchy root name.</returns>
    /// <remarks>The path is available for both attached and detached hierarchies.</remarks>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public string GetPath()
    {
        ThrowIfDisposed();
        var names = GetAncestry().Select(node => node.Name);
        return "/" + string.Join('/', names);
    }

    /// <summary>Builds a relative path from this node to another node in the same hierarchy.</summary>
    /// <param name="node">The destination node.</param>
    /// <returns><c>.</c> for this node, otherwise a slash-separated sequence of <c>..</c> and child names.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The nodes do not share a hierarchy root.</exception>
    /// <exception cref="ObjectDisposedException">
    /// This node is disposing on another thread or has finished disposing, or disposal of <paramref name="node"/> has started.
    /// </exception>
    public string GetPathTo(Node node)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(node);
        ObjectDisposedException.ThrowIf(node.IsDisposed, node);

        if (ReferenceEquals(this, node))
            return ".";

        var from = GetAncestry();
        var to = node.GetAncestry();
        var common = 0;

        while (common < from.Count && common < to.Count && ReferenceEquals(from[common], to[common]))
            common++;

        if (common == 0)
            throw new InvalidOperationException("Nodes do not share a hierarchy root.");

        var parts = Enumerable.Repeat("..", from.Count - common).Concat(to.Skip(common).Select(item => item.Name));
        return string.Join('/', parts);
    }

    /// <summary>Resolves a required relative or absolute node path.</summary>
    /// <param name="path">A nonblank slash-separated path supporting <c>.</c>, <c>..</c>, and an optional absolute root-name segment.</param>
    /// <returns>The resolved node.</returns>
    /// <remarks>Absolute paths are resolved from the hierarchy root even when the hierarchy is detached.</remarks>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">No node exists at the requested path.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public Node GetNode(string path) => GetNodeOrNull(path) ?? throw new KeyNotFoundException($"Node path '{path}' was not found from '{GetPath()}'.");

    /// <summary>Resolves a required relative or absolute path to a requested node type.</summary>
    /// <typeparam name="TNode">The required node subtype.</typeparam>
    /// <param name="path">A nonblank slash-separated node path.</param>
    /// <returns>The resolved node cast to <typeparamref name="TNode"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidCastException">The resolved node is not a <typeparamref name="TNode"/>.</exception>
    /// <exception cref="KeyNotFoundException">No node exists at the requested path.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public TNode GetNode<TNode>(string path)
        where TNode : Node => GetNode(path) as TNode ?? throw new InvalidCastException($"Node at '{path}' is not a {typeof(TNode).Name}.");

    /// <summary>Attempts to resolve a relative or absolute node path.</summary>
    /// <param name="path">A nonblank slash-separated path supporting <c>.</c>, <c>..</c>, and an optional absolute root-name segment.</param>
    /// <returns>The resolved node, or <see langword="null"/> when traversal cannot continue.</returns>
    /// <remarks>Absolute paths are resolved from the hierarchy root even when the hierarchy is detached.</remarks>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public Node? GetNodeOrNull(string path)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var absolute = path.StartsWith('/');
        var current = absolute ? GetHierarchyRoot() : this;
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var start = absolute && parts.Length > 0 && StringComparer.Ordinal.Equals(parts[0], current.Name) ? 1 : 0;

        for (var index = start; index < parts.Length; index++)
        {
            var part = parts[index];

            if (part == ".")
                continue;

            if (part == "..")
            {
                current = current.Parent;
                if (current is null)
                    return null;

                continue;
            }

            current = current._children.FirstOrDefault(child => StringComparer.Ordinal.Equals(child.Name, part));
            if (current is null)
                return null;
        }

        return current;
    }

    /// <summary>Adds this node to a case-sensitive group.</summary>
    /// <param name="group">The nonblank group name.</param>
    /// <param name="persistent">Whether packed scenes containing this node should retain the membership.</param>
    /// <remarks>Adding an existing membership keeps it persistent once persistence has been requested.</remarks>
    /// <exception cref="ArgumentException"><paramref name="group"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public void AddToGroup(string group, bool persistent = false)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        _groups[group] = persistent || _groups.GetValueOrDefault(group);
    }

    /// <summary>Removes this node from a case-sensitive group.</summary>
    /// <param name="group">The nonblank group name.</param>
    /// <returns><see langword="true"/> when membership existed and was removed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="group"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public bool RemoveFromGroup(string group)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        return _groups.Remove(group);
    }

    /// <summary>Determines whether this node belongs to a case-sensitive group.</summary>
    /// <param name="group">The nonblank group name.</param>
    /// <returns><see langword="true"/> when this node is a member; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="group"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public bool IsInGroup(string group)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        return _groups.ContainsKey(group);
    }

    /// <summary>Returns this node's group memberships.</summary>
    /// <returns>A read-only snapshot sorted using ordinal string order.</returns>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public IReadOnlyList<string> GetGroups()
    {
        ThrowIfDisposed();
        return Array.AsReadOnly(_groups.Keys.Order(StringComparer.Ordinal).ToArray());
    }

    /// <summary>Determines whether the resolved process mode allows callbacks in the current tree pause state.</summary>
    /// <returns><see langword="false"/> while detached or disabled; otherwise the result of the resolved pause policy.</returns>
    /// <exception cref="InvalidOperationException">An invalid inherited process mode cannot be resolved.</exception>
    /// <exception cref="ObjectDisposedException">This node or its tree is disposing on another thread, or has finished disposing.</exception>
    public bool CanProcess()
    {
        ThrowIfDisposed();

        if (Tree is null)
            return false;

        return ResolveProcessMode() switch
        {
            NodeProcessMode.Pausable => !Tree.Paused,
            NodeProcessMode.WhenPaused => Tree.Paused,
            NodeProcessMode.Always => true,
            NodeProcessMode.Disabled => false,
            _ => throw new InvalidOperationException("An inherited process mode was not resolved.")
        };
    }

    /// <summary>Requests ready delivery the next time SceneTree attachment reaches the ready phase.</summary>
    /// <remarks>The method only resets stored ready state; it never delivers ready immediately.</remarks>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public void RequestReady()
    {
        EnsureMutable();
        _readyCalled = false;
    }

    /// <summary>Creates a tween in this node's scene tree and binds it to this node.</summary>
    /// <returns>A running empty tween that halts while this node is detached and is killed when this node is disposed.</returns>
    /// <remarks>The caller must append at least one tweener before the next matching frame, including a zero-delta frame.</remarks>
    /// <exception cref="InvalidOperationException">The node is detached or the call is made off the tree owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node or its tree is disposing or disposed.</exception>
    public Tween CreateTween()
    {
        ThrowIfDisposed();
        var tree = Tree ?? throw new InvalidOperationException("A detached node cannot create a scene-tree tween.");
        return tree.CreateTween().BindNode(this);
    }

    /// <summary>Atomically requests this node's deferred disposal at a future scene-tree safe point.</summary>
    /// <remarks>
    /// A request made while already detached is queued if the node later enters a tree. Removing the node before its
    /// current tree flushes does not cancel deletion: that tree disposes the detached node at its safe point. If the
    /// node has entered another tree first, the old entry leaves the request intact for the new tree. Repeated calls
    /// are idempotent. The method may be called from a non-owner thread.
    /// </remarks>
    /// <exception cref="InvalidOperationException">This node is the active scene-tree root.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public void QueueFree()
    {
        EnsureNotSceneCapture();

        if (Tree?.IsRoot(this) == true)
            throw new InvalidOperationException("The SceneTree root cannot be queued for deletion.");

        var spinner = new SpinWait();

        while (true)
        {
            var state = Volatile.Read(ref _queuedForDeletion);
            if (state == 2)
                return;

            if (state == 1)
            {
                spinner.SpinOnce();
                continue;
            }

            if (Interlocked.CompareExchange(ref _queuedForDeletion, 1, 0) != 0)
                continue;

            try
            {
                var tree = Tree;
                if (tree is null)
                    Interlocked.CompareExchange(ref _queuedForDeletion, 2, 1);
                else
                    tree.QueueForDeletion(this);

                return;
            }
            catch
            {
                Interlocked.CompareExchange(ref _queuedForDeletion, 0, 1);
                throw;
            }
        }
    }

    /// <summary>Atomically cancels a pending deletion request.</summary>
    /// <returns><see langword="true"/> when a request was pending; otherwise <see langword="false"/>.</returns>
    /// <remarks>A stale queue entry may remain, but the tree ignores it when flushing.</remarks>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public bool CancelFree()
    {
        EnsureNotSceneCapture();
        return Interlocked.Exchange(ref _queuedForDeletion, 0) != 0;
    }

    /// <summary>Sets <see cref="Visible"/> to <see langword="true"/>.</summary>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A visibility notification or event handler throws after visibility changes.</exception>
    public void Show() => Visible = true;

    /// <summary>Sets <see cref="Visible"/> to <see langword="false"/>.</summary>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A visibility notification or event handler throws after visibility changes.</exception>
    public void Hide() => Visible = false;

    /// <summary>Component-multiplies the local scale by a ratio.</summary>
    /// <param name="ratio">The finite X and Y scale ratios.</param>
    /// <exception cref="ArgumentOutOfRangeException">A ratio component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the scale changes.</exception>
    public void ApplyScale(Vector2 ratio)
    {
        EnsureFinite(ratio, nameof(ratio));
        Scale *= ratio;
    }

    /// <summary>Adds an angle to the local rotation.</summary>
    /// <param name="radians">The finite angle in radians.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radians"/> is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the rotation changes.</exception>
    public void Rotate(float radians)
    {
        EnsureFinite(radians, nameof(radians));
        Rotation += radians;
    }

    /// <summary>Moves this node by an offset rotated by its local rotation.</summary>
    /// <param name="offset">The finite local-space offset.</param>
    /// <remarks>Scale and skew do not affect the offset.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An offset component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the position changes.</exception>
    public void Translate(Vector2 offset)
    {
        EnsureFinite(offset, nameof(offset));
        Position += new Transform(Rotation, Vector2.Zero).BasisXform(offset);
    }

    /// <summary>Moves this node by a hierarchy-global offset.</summary>
    /// <param name="offset">The finite global-space offset.</param>
    /// <exception cref="ArgumentOutOfRangeException">An offset component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or mutation occurs off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the position changes.</exception>
    public void GlobalTranslate(Vector2 offset)
    {
        EnsureFinite(offset, nameof(offset));
        GlobalPosition += offset;
    }

    /// <summary>Moves this node along its local X basis axis.</summary>
    /// <param name="delta">The finite signed distance.</param>
    /// <param name="scaled">Whether scale magnitude is retained. By default the axis is normalized.</param>
    /// <remarks>A near-zero normalized axis causes no movement.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the position changes.</exception>
    public void MoveLocalX(float delta, bool scaled = false) => MoveLocal(delta, useXAxis: true, scaled);

    /// <summary>Moves this node along its local Y basis axis.</summary>
    /// <param name="delta">The finite signed distance.</param>
    /// <param name="scaled">Whether scale magnitude is retained. By default the axis is normalized.</param>
    /// <remarks>A near-zero normalized axis causes no movement.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the position changes.</exception>
    public void MoveLocalY(float delta, bool scaled = false) => MoveLocal(delta, useXAxis: false, scaled);

    /// <summary>Computes the signed angle from this node's global positive X direction to a global point.</summary>
    /// <param name="globalPoint">The finite point in hierarchy-global coordinates.</param>
    /// <returns>A normalized angle in radians, or zero when the point equals <see cref="GlobalPosition"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A point component is NaN or infinite.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    public float GetAngleTo(Vector2 globalPoint)
    {
        EnsureFinite(globalPoint, nameof(globalPoint));
        var direction = globalPoint - GlobalPosition;
        return direction == Vector2.Zero ? 0f : NormalizeAngle(Mathf.Atan2(direction.Y, direction.X) - GlobalRotation);
    }

    /// <summary>Rotates this node so its positive local X direction points at a global point.</summary>
    /// <param name="globalPoint">The finite target point in hierarchy-global coordinates.</param>
    /// <remarks>A target equal to <see cref="GlobalPosition"/> leaves rotation unchanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A point component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The parent transform is singular, or mutation occurs off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    /// <exception cref="Exception">A transform notification or event handler throws after the rotation changes.</exception>
    public void LookAt(Vector2 globalPoint)
    {
        EnsureFinite(globalPoint, nameof(globalPoint));

        if (globalPoint != GlobalPosition)
            GlobalRotation += GetAngleTo(globalPoint);
    }

    /// <summary>Transforms a point from this node's local coordinates to hierarchy-global coordinates.</summary>
    /// <param name="localPoint">The finite local point.</param>
    /// <returns>The point transformed by <see cref="GlobalTransform"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A point component is NaN or infinite.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    public Vector2 ToGlobal(Vector2 localPoint)
    {
        EnsureFinite(localPoint, nameof(localPoint));
        return GlobalTransform * localPoint;
    }

    /// <summary>Transforms a point from hierarchy-global coordinates to this node's local coordinates.</summary>
    /// <param name="globalPoint">The finite global point.</param>
    /// <returns>The point transformed by the inverse global transform.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A point component is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The global transform is singular.</exception>
    /// <exception cref="ObjectDisposedException">This node or an ancestor is disposing on another thread, or has finished disposing.</exception>
    public Vector2 ToLocal(Vector2 globalPoint)
    {
        EnsureFinite(globalPoint, nameof(globalPoint));
        return GlobalTransform.AffineInverse() * globalPoint;
    }

    /// <summary>Returns this node's transform relative to an ancestor.</summary>
    /// <param name="parent">This node itself or a strict ancestor.</param>
    /// <returns>Identity for this node; otherwise the global transform expressed relative to <paramref name="parent"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="parent"/> is not an ancestor of this node.</exception>
    /// <exception cref="InvalidOperationException">The ancestor's global transform is singular.</exception>
    /// <exception cref="ObjectDisposedException">
    /// This node, <paramref name="parent"/>, or a queried ancestor is disposing on another thread or has finished disposing.
    /// </exception>
    public Transform GetRelativeTransformToParent(Node parent)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parent);

        if (ReferenceEquals(parent, this))
            return Transform.Identity;

        if (!parent.IsAncestorOf(this))
            throw new ArgumentException("The supplied node is not an ancestor of this node.", nameof(parent));

        return parent.GlobalTransform.AffineInverse() * GlobalTransform;
    }

    /// <summary>Creates a reusable factory for packed-scene instances of this exact runtime node type.</summary>
    /// <returns>A non-null factory that creates a fresh node of the exact same runtime type.</returns>
    /// <remarks>
    /// The base implementation supports only an exact <see cref="Node"/>. Derived node types that can be packed must
    /// return a static, non-capturing factory that remains valid after the source node is disposed and creates a live,
    /// detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore
    /// the instance state.
    /// </remarks>
    /// <exception cref="NotSupportedException">A derived node has not explicitly supplied an instancing factory.</exception>
    protected virtual Func<Node> CreateSceneInstanceFactory()
    {
        if (GetType() != typeof(Node))
            throw new NotSupportedException($"{GetType().Name} must override {nameof(CreateSceneInstanceFactory)} to support packed scenes.");

        return CreateDefaultSceneNode;
    }

    /// <summary>Called synchronously when this node enters an active scene tree.</summary>
    /// <remarks>
    /// <see cref="Tree"/> is already assigned. The callback runs parent-first, before <see cref="TreeEntered"/>, before
    /// descendants enter, and on the tree owner thread during SceneTree-managed lifecycle.
    /// </remarks>
    protected virtual void OnEnterTree()
    {
    }

    /// <summary>Called synchronously when this node exits an active scene tree.</summary>
    /// <remarks>
    /// Descendants have already exited and <see cref="Tree"/> remains assigned. The callback precedes
    /// <see cref="TreeExiting"/> and runs on the tree owner thread during SceneTree-managed lifecycle.
    /// </remarks>
    protected virtual void OnExitTree()
    {
    }

    /// <summary>Called synchronously when this node receives SceneTree-managed ready delivery.</summary>
    /// <remarks>
    /// Children are ready first. The callback precedes <see cref="Ready"/>, is one-shot until <see cref="RequestReady"/>,
    /// and runs on the owner thread during SceneTree-managed delivery. Manual notification runs on its caller's thread.
    /// </remarks>
    protected virtual void OnReady()
    {
    }

    /// <summary>Called during an eligible host-driven process frame.</summary>
    /// <param name="delta">The finite non-negative frame delta in seconds.</param>
    /// <remarks>
    /// The callback is not auto-enabled by overriding it; <see cref="ProcessEnabled"/> must be true. It executes on
    /// the tree owner thread after <see cref="ProcessDeltaTime"/> is updated.
    /// </remarks>
    protected virtual void OnProcess(double delta)
    {
    }

    /// <summary>Called during an eligible host-driven physics-process frame.</summary>
    /// <param name="delta">The finite non-negative physics-step delta in seconds.</param>
    /// <remarks>
    /// The callback is not auto-enabled by overriding it; <see cref="PhysicsProcessEnabled"/> must be true. It executes
    /// on the tree owner thread after <see cref="PhysicsProcessDeltaTime"/> is updated and does not perform simulation.
    /// </remarks>
    protected virtual void OnPhysicsProcess(double delta)
    {
    }

    /// <summary>Receives an input event during the first scene-input propagation stage.</summary>
    /// <param name="event">The live caller-owned event being dispatched.</param>
    /// <remarks>
    /// The callback runs synchronously on the scene-tree owner thread when <see cref="InputEnabled"/> is true and
    /// <see cref="CanProcess"/> allows the node. Call <see cref="SceneTree.SetInputAsHandled"/> to stop later stages.
    /// </remarks>
    protected virtual void OnInput(InputEvent @event)
    {
    }

    /// <summary>Receives a keyboard event that remains unhandled after the first input stage.</summary>
    /// <param name="event">The live caller-owned keyboard event being dispatched.</param>
    /// <remarks>
    /// The callback runs synchronously on the scene-tree owner thread when <see cref="UnhandledKeyInputEnabled"/> is
    /// true and <see cref="CanProcess"/> allows the node.
    /// </remarks>
    protected virtual void OnUnhandledKeyInput(InputEventKey @event)
    {
    }

    /// <summary>Receives an event that remains unhandled after earlier scene-input stages.</summary>
    /// <param name="event">The live caller-owned event being dispatched.</param>
    /// <remarks>
    /// The callback runs synchronously on the scene-tree owner thread when <see cref="UnhandledInputEnabled"/> is true
    /// and <see cref="CanProcess"/> allows the node.
    /// </remarks>
    protected virtual void OnUnhandledInput(InputEvent @event)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Calls the base implementation, then maps enter, exit, ready, process, and physics-process notification IDs to
    /// the corresponding typed virtual callbacks. Manual <see cref="ElectronObject.Notify(int)"/> calls invoke those
    /// callbacks but do not mutate tree membership, ready state, or delta values.
    /// </remarks>
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);

        switch (what)
        {
            case NotificationEnterTree:
                OnEnterTree();
                break;
            case NotificationExitTree:
                OnExitTree();
                break;
            case NotificationReady:
                OnReady();
                break;
            case NotificationProcess:
                OnProcess(ProcessDeltaTime);
                break;
            case NotificationPhysicsProcess:
                OnPhysicsProcess(PhysicsProcessDeltaTime);
                break;
        }
    }

    /// <inheritdoc />
    protected override void ValidateMutation()
    {
        base.ValidateMutation();
        EnsureNotSceneCapture();
    }

    /// <inheritdoc />
    /// <remarks>Appends this class's typed hierarchy, spatial, visibility, and processing descriptors to the inherited descriptors.</remarks>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(NodeProperties).Concat(DrawingProperties);

    /// <inheritdoc />
    /// <remarks>Rejects disposal during tree lifecycle delivery or of an active tree root, and requires the owner thread for an attached node.</remarks>
    /// <exception cref="InvalidOperationException">This node is in lifecycle delivery, its parent is exiting, it is an active tree root, or disposal is attempted off the owner thread.</exception>
    protected override void ValidateDisposal()
    {
        if (Volatile.Read(ref _sceneInstantiationDepth) != 0)
            throw new InvalidOperationException("A node cannot be disposed while packed-scene instantiation is active.");

        if (Volatile.Read(ref _sceneCaptureDepth) != 0)
            throw new InvalidOperationException("A node cannot be disposed while a packed-scene capture is active.");

        if (_isEnteringTree || _isMakingReady || _isExitingTree)
            throw new InvalidOperationException("A node cannot be disposed from its in-progress tree lifecycle callbacks.");

        if (Parent?._isExitingTree == true)
            throw new InvalidOperationException("A node cannot be disposed while its parent is exiting a SceneTree.");

        if (Tree?.IsRoot(this) == true)
            throw new InvalidOperationException("An active SceneTree root can only be disposed by disposing its SceneTree.");

        Tree?.EnsureOwnerThread();
        base.ValidateDisposal();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Cancels queued deletion, detaches this node, recursively disposes every owned child, clears groups and event
    /// subscribers, and then calls the base implementation. Every teardown stage is attempted before failures are
    /// reported together.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        List<Exception>? errors = null;

        if (disposing)
        {
            Interlocked.Exchange(ref _queuedForDeletion, 0);

            try
            {
                if (Parent is not null)
                    Parent.RemoveChildCore(this);
                else
                    Tree?.DetachSubtree(this);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }

            foreach (var child in _children.ToArray())
            {
                try
                {
                    child.Dispose();
                }
                catch (Exception error)
                {
                    CollectException(ref errors, error);
                }
            }

            _children.Clear();
            _canvasCommands?.Clear();
            _material = null;
            _groups.Clear();
            _owner = null;

            if (_ownedSceneResources is not null)
            {
                foreach (var resource in _ownedSceneResources)
                {
                    try
                    {
                        resource.Dispose();
                    }
                    catch (Exception error)
                    {
                        CollectException(ref errors, error);
                    }
                }

                _ownedSceneResources = null;
            }
            ChildAdded = null;
            ChildRemoved = null;
            ChildEnteredTree = null;
            ChildExitingTree = null;
            ChildOrderChanged = null;
            Renamed = null;
            TreeEntered = null;
            TreeExiting = null;
            TreeExited = null;
            Ready = null;
            VisibilityChanged = null;
            LocalTransformChanged = null;
            TransformChanged = null;
        }

        try
        {
            base.Dispose(disposing);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        ThrowCollected("One or more node teardown operations failed.", errors);
    }

    internal bool TryConsumeQueuedDeletion() => Interlocked.CompareExchange(ref _queuedForDeletion, 0, 2) == 2;

    internal Func<Node> CaptureSceneInstanceFactory()
    {
        ThrowIfDisposed();
        var factory = CreateSceneInstanceFactory() ??
                      throw new InvalidOperationException("A scene instance factory cannot be null.");

        if (factory.Target is not null)
            throw new InvalidOperationException("A scene instance factory must not capture source or mutable state.");

        return factory;
    }

    internal Node[] BeginSceneCapture()
    {
        EnsureMutable();
        var nodes = EnumerateDepthFirst().ToArray();
        var marked = 0;

        try
        {
            for (; marked < nodes.Length; marked++)
            {
                nodes[marked].ThrowIfDisposed();
                if (Interlocked.CompareExchange(ref nodes[marked]._sceneCaptureDepth, 1, 0) != 0)
                    throw new InvalidOperationException("A packed-scene capture is already active for this hierarchy.");
            }

            return nodes;
        }
        catch
        {
            for (var index = marked - 1; index >= 0; index--)
                Volatile.Write(ref nodes[index]._sceneCaptureDepth, 0);
            throw;
        }
    }

    internal static void EndSceneCapture(IEnumerable<Node> nodes)
    {
        foreach (var node in nodes)
            Volatile.Write(ref node._sceneCaptureDepth, 0);
    }

    internal IReadOnlyList<string> GetPersistentGroups() => Array.AsReadOnly(
        _groups.Where(pair => pair.Value).Select(pair => pair.Key).Order(StringComparer.Ordinal).ToArray());

    internal void AdoptSceneResources(IReadOnlyList<Resource> resources)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(resources);

        if (_ownedSceneResources is not null)
            throw new InvalidOperationException("The node already owns scene-local resources.");

        var owned = new List<Resource>(resources.Count);
        foreach (var resource in resources)
        {
            ArgumentNullException.ThrowIfNull(resource);
            ObjectDisposedException.ThrowIf(resource.IsDisposed, resource);
            owned.Add(resource);
        }

        _ownedSceneResources = owned;
    }

    internal void SetSceneFilePath(string path)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(path);
        _sceneFilePath = path;
    }

    internal void BeginSceneInstantiation()
    {
        ThrowIfDisposed();
        if (Interlocked.CompareExchange(ref _sceneInstantiationDepth, 1, 0) != 0)
            throw new InvalidOperationException("Packed-scene instantiation is already active for this node.");
    }

    internal void EndSceneInstantiation() => Volatile.Write(ref _sceneInstantiationDepth, 0);

    internal void EnsureSceneActivationAvailable()
    {
        EnsureNotSceneCapture();
        if (Volatile.Read(ref _sceneInstantiationDepth) != 0)
            throw new InvalidOperationException("A node cannot become a scene-tree root before packed-scene instantiation completes.");
    }

    internal static Node InvokeSceneInstanceFactory(Func<Node> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        SceneFactoryDepth.Value++;
        try
        {
            return factory();
        }
        finally
        {
            SceneFactoryDepth.Value--;
        }
    }

    internal static void EnsureSceneFactoryComplete()
    {
        if (SceneFactoryDepth.Value != 0)
            throw new InvalidOperationException("A node factory cannot activate a SceneTree before returning its node.");
    }

    internal bool TryPublishQueuedDeletion()
    {
        while (true)
        {
            var state = Volatile.Read(ref _queuedForDeletion);
            if (state == 0)
                return false;

            if (state == 2 || Interlocked.CompareExchange(ref _queuedForDeletion, 2, 1) == 1)
                return true;
        }
    }

    internal IEnumerable<Node> EnumerateDepthFirst()
    {
        yield return this;

        foreach (var child in _children.ToArray())
        {
            foreach (var descendant in child.EnumerateDepthFirst())
                yield return descendant;
        }
    }

    internal bool HasProcessCallback(bool physics) => physics
        ? _physicsProcessEnabled || _internalPhysicsProcessEnabled
        : _processEnabled || _internalProcessEnabled;

    internal void SetInternalProcessing(bool processEnabled, bool physicsProcessEnabled)
    {
        EnsureMutable();
        _internalProcessEnabled = processEnabled;
        _internalPhysicsProcessEnabled = physicsProcessEnabled;
    }

    internal double GetUnscaledProcessDelta(bool physics) =>
        physics ? _unscaledPhysicsProcessDeltaTime : _unscaledProcessDeltaTime;

    internal void RunProcess(double delta, double unscaledDelta, bool physics)
    {
        if (physics)
        {
            PhysicsProcessDeltaTime = delta;
            _unscaledPhysicsProcessDeltaTime = unscaledDelta;
        }
        else
        {
            ProcessDeltaTime = delta;
            _unscaledProcessDeltaTime = unscaledDelta;
        }

        var internalEnabled = physics ? _internalPhysicsProcessEnabled : _internalProcessEnabled;
        var externalEnabled = physics ? _physicsProcessEnabled : _processEnabled;
        var expectedTree = Tree;
        List<Exception>? errors = null;

        if (internalEnabled)
        {
            try
            {
                DispatchNotification(physics ? NotificationInternalPhysicsProcess : NotificationInternalProcess);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        if (!IsDisposed && ReferenceEquals(Tree, expectedTree) && externalEnabled &&
            (physics ? _physicsProcessEnabled : _processEnabled))
        {
            try
            {
                DispatchNotification(physics ? NotificationPhysicsProcess : NotificationProcess);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        ThrowCollected("One or more node process callbacks failed.", errors);
    }

    internal void EnterTree(SceneTree tree)
    {
        EnsureSceneFactoryComplete();
        EnsureSceneActivationAvailable();

        if (_isEnteringTree || _isExitingTree)
            throw new InvalidOperationException($"Node '{Name}' cannot re-enter a SceneTree from an in-progress lifecycle callback.");

        if (Tree is not null)
            throw new InvalidOperationException($"Node '{Name}' is already inside a SceneTree.");

        _isEnteringTree = true;

        try
        {
            EnterTreeCore(tree);
        }
        finally
        {
            _isEnteringTree = false;
        }
    }

    private void EnterTreeCore(SceneTree tree)
    {

        Tree = tree;
        List<Exception>? errors = null;

        try
        {
            DispatchNotification(NotificationEnterTree);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            TreeEntered?.Invoke(this);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        var parent = Parent;

        try
        {
            parent?.ChildEnteredTree?.Invoke(parent, this);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            tree.NotifyNodeAdded(this);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        if (!ReferenceEquals(Tree, tree))
        {
            ThrowCollected("One or more enter-tree callbacks failed.", errors);
            return;
        }

        foreach (var child in _children.ToArray())
        {
            if (!ReferenceEquals(child.Parent, this) || ReferenceEquals(child.Tree, tree))
                continue;

            try
            {
                child.EnterTree(tree);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        try
        {
            DispatchNotification(NotificationPostEnterTree);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        if (IsQueuedForDeletion)
        {
            try
            {
                tree.QueueForDeletion(this);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        ThrowCollected("One or more enter-tree callbacks failed.", errors);
    }

    internal void MakeReady(List<Node>? readied = null)
    {
        if (_isMakingReady)
            throw new InvalidOperationException($"Node '{Name}' cannot re-enter ready delivery.");

        var expectedTree = Tree;
        if (expectedTree is null || IsDisposed)
            return;

        _isMakingReady = true;

        try
        {
            MakeReadyCore(expectedTree, readied);
        }
        finally
        {
            _isMakingReady = false;
        }
    }

    private void MakeReadyCore(SceneTree expectedTree, List<Node>? readied)
    {
        List<Exception>? errors = null;

        foreach (var child in _children.ToArray())
        {
            if (IsDisposed || !ReferenceEquals(Tree, expectedTree))
                break;

            if (!ReferenceEquals(child.Parent, this) || !ReferenceEquals(child.Tree, expectedTree))
                continue;

            try
            {
                child.MakeReady(readied);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        if (IsDisposed || !ReferenceEquals(Tree, expectedTree))
        {
            ThrowCollected("One or more ready callbacks failed.", errors);
            return;
        }

        if (!_readyCalled)
        {
            _readyCalled = true;
            readied?.Add(this);

            try
            {
                DispatchNotification(NotificationReady);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }

            try
            {
                Ready?.Invoke(this);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        ThrowCollected("One or more ready callbacks failed.", errors);
    }

    internal void ExitTree(SceneTree tree)
    {
        if (_isEnteringTree)
            throw new InvalidOperationException($"Node '{Name}' cannot exit a SceneTree while it is still entering.");

        if (!ReferenceEquals(Tree, tree) || _isExitingTree)
            return;

        _isExitingTree = true;
        List<Exception>? errors = null;

        try
        {
            foreach (var child in _children.ToArray())
            {
                try
                {
                    child.ExitTree(tree);
                }
                catch (Exception error)
                {
                    CollectException(ref errors, error);
                }
            }

            try
            {
                DispatchNotification(NotificationExitTree);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }

            try
            {
                TreeExiting?.Invoke(this);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }

            var parent = Parent;

            try
            {
                parent?.ChildExitingTree?.Invoke(parent, this);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }

            Tree = null;

            try
            {
                TreeExited?.Invoke(this);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }

            try
            {
                tree.NotifyNodeRemoved(this);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }
        finally
        {
            _isExitingTree = false;

            if (ReferenceEquals(Tree, tree))
                Tree = null;
        }

        ThrowCollected("One or more exit-tree callbacks failed.", errors);
    }

    internal void ResetReadyAfterFailedActivation() => _readyCalled = false;

    internal void DispatchInput(InputEvent @event) => OnInput(@event);

    internal void DispatchUnhandledKeyInput(InputEventKey @event) => OnUnhandledKeyInput(@event);

    internal void DispatchUnhandledInput(InputEvent @event) => OnUnhandledInput(@event);

    private static bool IsValidNodeName(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value is not "." and not ".." && !value.Contains('/');

    private static Node CreateDefaultSceneNode() => new();

    private static bool IsFinite(Vector2 value) => Mathf.IsFinite(value.X) && Mathf.IsFinite(value.Y);

    private static void EnsureFinite(float value, string parameterName)
    {
        if (!Mathf.IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite.");
    }

    private static void EnsureFinite(Vector2 value, string parameterName)
    {
        if (!IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName, value, "Both vector components must be finite.");
    }

    private static void EnsureFinite(Transform value, string parameterName)
    {
        if (!value.IsFinite())
            throw new ArgumentOutOfRangeException(parameterName, value, "Every transform component must be finite.");
    }

    private static int NormalizeChildIndex(int index, int count)
    {
        if (count == 0)
            throw new ArgumentOutOfRangeException(nameof(index), index, "A node has no children.");

        if (index < 0)
            index += count;

        if ((uint)index >= (uint)count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Child index is outside the valid range.");

        return index;
    }

    private static float NormalizeAngle(float angle) => MathF.IEEERemainder(angle, Mathf.Tau);

    private static Transform ToLocalTransform(Transform global, Node? parent, bool topLevel)
    {
        if (parent is null || topLevel)
            return global;

        return parent.GlobalTransform.AffineInverse() * global;
    }

    /// <summary>Validates that this node may be mutated at the current lifecycle point.</summary>
    /// <remarks>Derived node property setters should call this before changing state that can be stored in a packed scene.</remarks>
    /// <exception cref="InvalidOperationException">Scene capture is active or an attached node is accessed off the tree owner thread.</exception>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    protected void EnsureMutable()
    {
        ThrowIfDisposed();

        EnsureNotSceneCapture();

        Tree?.EnsureOwnerThread();
    }

    private void EnsureNotSceneCapture()
    {
        ThrowIfDisposed();

        if (IsDisposed)
            throw new ObjectDisposedException(GetType().FullName);

        if (Volatile.Read(ref _sceneCaptureDepth) != 0)
            throw new InvalidOperationException("A node cannot be mutated while a packed-scene capture is active.");
    }

    private void SetTransform(Transform transform)
    {
        EnsureFinite(transform, nameof(transform));

        if (_transform.Equals(transform))
            return;

        _transform = transform;

        if (_notifyLocalTransformChanges)
            DispatchNotification(NotificationLocalTransformChanged);

        LocalTransformChanged?.Invoke(this);
        PropagateGlobalTransformChanged();
    }

    private void PropagateGlobalTransformChanged()
    {
        if (_notifyTransformChanges)
            DispatchNotification(NotificationTransformChanged);

        TransformChanged?.Invoke(this);

        foreach (var child in _children.ToArray())
        {
            if (!child._topLevel)
                child.PropagateGlobalTransformChanged();
        }
    }

    private void PropagateVisibilityChanged()
    {
        DispatchNotification(NotificationVisibilityChanged);
        VisibilityChanged?.Invoke(this);

        foreach (var child in _children.ToArray())
            child.PropagateVisibilityChanged();
    }

    private void PropagatePathRenamed()
    {
        DispatchNotification(NotificationPathRenamed);

        foreach (var child in _children.ToArray())
            child.PropagatePathRenamed();
    }

    private void InsertChild(Node child, int index)
    {
        EnsureMutable();
        ValidateChildForInsertion(child, allowExistingParent: false);

        if ((uint)index > (uint)_children.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Insertion index is outside the valid range.");

        var tree = Tree;
        _children.Insert(index, child);
        child.Parent = this;
        List<Exception>? errors = null;

        try
        {
            child.DispatchNotification(NotificationParented);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            child.PropagateGlobalTransformChanged();
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            NotifyChildOrderChanged();
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            ChildAdded?.Invoke(this, child);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        if (tree is not null && ReferenceEquals(child.Parent, this) && child.Tree is null)
        {
            try
            {
                tree.AttachSubtree(child);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        try
        {
            tree?.NotifyTreeChanged();
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        ThrowCollected("One or more child-insertion callbacks failed.", errors);
    }

    private void ValidateChildForInsertion(Node child, bool allowExistingParent)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(child);
        child.EnsureNotSceneCapture();
        if (child is Viewport)
            throw new NotSupportedException("Child viewports require multiwindow or offscreen rendering support.");
        Tree?.EnsureOwnerThread();

        if (ReferenceEquals(child, this))
            throw new ArgumentException("A node cannot be its own child.", nameof(child));

        if (!allowExistingParent && child.Parent is not null)
            throw new InvalidOperationException($"Node '{child.Name}' already has a parent.");

        if (!allowExistingParent && child.Tree is not null)
            throw new InvalidOperationException($"Node '{child.Name}' already belongs to a SceneTree.");

        if (_isExitingTree)
            throw new InvalidOperationException("A child cannot be added while its parent is exiting a SceneTree.");

        for (Node? ancestor = this; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ReferenceEquals(ancestor, child))
                throw new InvalidOperationException("Adding this child would create a node cycle.");
        }

        EnsureChildNameAvailable(child.Name, child);
    }

    private void EnsureChildNameAvailable(string name, Node? except)
    {
        if (_children.Any(child => !ReferenceEquals(child, except) && StringComparer.Ordinal.Equals(child.Name, name)))
            throw new InvalidOperationException($"A child named '{name}' already exists under '{Name}'.");
    }

    private bool RemoveChildCore(Node child)
    {
        if (!ReferenceEquals(child.Parent, this))
            return false;

        if (_isExitingTree)
            throw new InvalidOperationException("A child cannot be removed while its parent is exiting a SceneTree.");

        if (child._isEnteringTree || child._isMakingReady || child._isExitingTree)
            throw new InvalidOperationException("A node cannot be structurally removed while its tree lifecycle callbacks are running.");

        var tree = Tree;
        List<Exception>? errors = null;

        try
        {
            tree?.DetachSubtree(child);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        _children.Remove(child);
        child.Parent = null;
        child.ClearInvalidOwnersRecursive();

        try
        {
            child.DispatchNotification(NotificationUnparented);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            child.PropagateGlobalTransformChanged();
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            NotifyChildOrderChanged();
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            ChildRemoved?.Invoke(this, child);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            tree?.NotifyTreeChanged();
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        ThrowCollected("One or more child-removal callbacks failed.", errors);
        return true;
    }

    private void NotifyChildOrderChanged()
    {
        DispatchNotification(NotificationChildOrderChanged);
        ChildOrderChanged?.Invoke(this);
    }

    private void ClearInvalidOwnersRecursive()
    {
        foreach (var node in EnumerateDepthFirst())
        {
            if (node._owner is not null && !node._owner.IsAncestorOf(node))
                node._owner = null;
        }
    }

    private void FindChildrenCore<TNode>(string pattern, bool recursive, List<TNode> result)
        where TNode : Node
    {
        foreach (var child in _children)
        {
            if (child is TNode typedChild && FileSystemName.MatchesSimpleExpression(pattern, child.Name))
                result.Add(typedChild);

            if (recursive)
                child.FindChildrenCore(pattern, recursive: true, result);
        }
    }

    private List<Node> GetAncestry()
    {
        var result = new List<Node>();

        for (Node? current = this; current is not null; current = current.Parent)
            result.Add(current);

        result.Reverse();
        return result;
    }

    private Node GetHierarchyRoot()
    {
        var root = this;
        while (root.Parent is not null)
            root = root.Parent;

        return root;
    }

    private NodeProcessMode ResolveProcessMode()
    {
        if (ProcessMode != NodeProcessMode.Inherit)
            return ProcessMode;

        return Parent?.ResolveProcessMode() ?? NodeProcessMode.Pausable;
    }

    private void MoveLocal(float delta, bool useXAxis, bool scaled)
    {
        EnsureFinite(delta, nameof(delta));
        EnsureMutable();

        var axis = useXAxis ? _transform.X : _transform.Y;

        if (!scaled)
        {
            if (axis.LengthSquared() <= TransformEpsilon * TransformEpsilon)
                return;

            axis = axis.Normalized();
        }

        Position += axis * delta;
    }

    private static void CollectException(ref List<Exception>? errors, Exception error)
    {
        errors ??= [];

        if (error is AggregateException aggregate)
            errors.AddRange(aggregate.Flatten().InnerExceptions);
        else
            errors.Add(error);
    }

    private static void ThrowCollected(string message, List<Exception>? errors)
    {
        if (errors is not null)
            throw new AggregateException(message, errors);
    }

}
