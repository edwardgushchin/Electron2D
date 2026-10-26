using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Electron2D;

/// <summary>Provides tree membership, ownership, lifecycle, processing and input for scene objects.</summary>
/// <remarks>Children may be any Node subtype. Spatial and drawing behavior belongs to CanvasItem and Entity.</remarks>
public partial class Node : ElectronObject
{
    private sealed class CallbackMask(byte bits) { internal readonly byte Bits = bits; }
    private static readonly ConditionalWeakTable<Type, CallbackMask> CallbackMasks = new();

    private static readonly PropertyDescriptor[] SceneNodeProperties =
    [
        new PropertyDescriptor<Node, MultiplayerAPI>(nameof(Multiplayer), node => node.Multiplayer),
        new PropertyDescriptor<Node, string>(
            nameof(Name),
            node => node.Name,
            (node, value) => node.Name = value,
            node => node.ClassName,
            (_, value) => !string.IsNullOrWhiteSpace(value),
            stored: true),
        new PropertyDescriptor<Node, bool>(nameof(UniqueNameInOwner), node => node.UniqueNameInOwner,
            (node, value) => node.UniqueNameInOwner = value, _ => false, stored: true),
        new PropertyDescriptor<Node, string>(nameof(EditorDescription), node => node.EditorDescription,
            (node, value) => node.EditorDescription = value, _ => string.Empty, stored: true),
        new PropertyDescriptor<Node, bool>(nameof(DisplayFolded), node => node.DisplayFolded,
            (node, value) => node.DisplayFolded = value, _ => false),
        new PropertyDescriptor<Node, bool>(nameof(IsPartOfEditedScene), node => node.IsPartOfEditedScene),
        new PropertyDescriptor<Node, string>(nameof(SceneFilePath), node => node.SceneFilePath,
            (node, value) => node.SceneFilePath = value, _ => string.Empty),
        new PropertyDescriptor<Node, ProcessMode>(
            nameof(ProcessMode),
            node => node.ProcessMode,
            (node, value) => node.ProcessMode = value,
            _ => ProcessMode.Inherit,
            (_, value) => Enum.IsDefined(value),
            stored: true),
        new PropertyDescriptor<Node, PhysicsInterpolationMode>(nameof(PhysicsInterpolationMode),
            node => node.PhysicsInterpolationMode, (node, value) => node.PhysicsInterpolationMode = value,
            node => node switch
            {
                Control => PhysicsInterpolationMode.Off,
                Viewport => PhysicsInterpolationMode.On,
                _ => PhysicsInterpolationMode.Inherit
            },
            (_, value) => Enum.IsDefined(value), stored: true),
        new PropertyDescriptor<Node, NodeAutoTranslateMode>(
            nameof(AutoTranslateMode),
            node => node.AutoTranslateMode,
            (node, value) => node.AutoTranslateMode = value,
            _ => NodeAutoTranslateMode.Inherit,
            (_, value) => Enum.IsDefined(value),
            stored: true),
        new PropertyDescriptor<Node, bool>(nameof(ProcessEnabled), node => node.ProcessEnabled, (node, value) => node.ProcessEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<Node, bool>(nameof(PhysicsProcessEnabled), node => node.PhysicsProcessEnabled, (node, value) => node.PhysicsProcessEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<Node, bool>(nameof(InternalProcessEnabled), node => node.InternalProcessEnabled, (node, value) => node.InternalProcessEnabled = value, _ => false),
        new PropertyDescriptor<Node, bool>(nameof(InternalPhysicsProcessEnabled), node => node.InternalPhysicsProcessEnabled, (node, value) => node.InternalPhysicsProcessEnabled = value, _ => false),
        new PropertyDescriptor<Node, bool>(nameof(InputEnabled), node => node.InputEnabled, (node, value) => node.InputEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<Node, bool>(nameof(ShortcutInputEnabled), node => node.ShortcutInputEnabled, (node, value) => node.ShortcutInputEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<Node, bool>(nameof(UnhandledInputEnabled), node => node.UnhandledInputEnabled, (node, value) => node.UnhandledInputEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<Node, bool>(nameof(UnhandledKeyInputEnabled), node => node.UnhandledKeyInputEnabled, (node, value) => node.UnhandledKeyInputEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<Node, int>(nameof(ProcessPriority), node => node.ProcessPriority, (node, value) => node.ProcessPriority = value, _ => 0, stored: true),
        new PropertyDescriptor<Node, int>(nameof(PhysicsProcessPriority), node => node.PhysicsProcessPriority, (node, value) => node.PhysicsProcessPriority = value, _ => 0, stored: true)
    ];

    /// <summary>Identifies the notification sent when a node enters an active <see cref="SceneTree"/>.</summary>
    public const int NotificationEnterTree = 10;

    /// <summary>Identifies the notification sent after descendants exit and before this node leaves its tree.</summary>
    public const int NotificationExitTree = 11;

    /// <summary>Identifies pointer entry into an embedded viewport.</summary>
    public const int NotificationVPMouseEnter = 1010;
    /// <summary>Identifies pointer exit from an embedded viewport.</summary>
    public const int NotificationVPMouseExit = 1011;

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
    /// <summary>Identifies the notification after a GUI drag payload becomes active.</summary>
    public const int NotificationDragBegin = 21;
    /// <summary>Identifies the notification after a GUI drag ends and its result commits.</summary>
    public const int NotificationDragEnd = 22;

    /// <summary>Identifies the notification propagated after an accepted name assignment, including an unchanged name.</summary>
    public const int NotificationPathRenamed = 23;

    /// <summary>Identifies the notification sent after the direct child order changes.</summary>
    public const int NotificationChildOrderChanged = 24;

    /// <summary>Identifies the internal idle-frame notification.</summary>
    /// <remarks>The InternalProcessEnabled lane precedes ordinary ProcessEnabled callbacks.</remarks>
    public const int NotificationInternalProcess = 25;

    /// <summary>Identifies the internal fixed-frame notification.</summary>
    /// <remarks>The InternalPhysicsProcessEnabled lane precedes ordinary PhysicsProcessEnabled callbacks.</remarks>
    public const int NotificationInternalPhysicsProcess = 26;

    /// <summary>Identifies the child-first notification sent on every attachment after descendant ready delivery and before this node's optional ready notification.</summary>
    public const int NotificationPostEnterTree = 27;

    /// <summary>Identifies the notification sent when the effective process mode becomes disabled.</summary>
    public const int NotificationDisabled = 28;

    /// <summary>Identifies the notification sent when the effective process mode stops being disabled.</summary>
    public const int NotificationEnabled = 29;

    /// <summary>Identifies the notification before an explicit live-scene authoring save captures this subtree.</summary>
    /// <remarks>Published by ResourceSaver.SaveScene; ordinary packing and resource saving do not publish it.</remarks>
    public const int NotificationEditorPreSave = 9001;

    /// <summary>Identifies cleanup notification after an attempted live-scene authoring save.</summary>
    /// <remarks>Published after pre-save delivery begins, including failed saves, so temporary authoring changes can be restored.</remarks>
    public const int NotificationEditorPostSave = 9002;

    /// <summary>Identifies the notification sent when the pointer enters the containing window.</summary>
    public const int NotificationWmMouseEnter = 1002;

    /// <summary>Identifies the notification sent when the pointer exits the containing window.</summary>
    public const int NotificationWmMouseExit = 1003;

    /// <summary>Identifies the notification sent when the containing window gains keyboard focus.</summary>
    public const int NotificationWmWindowFocusIn = 1004;

    /// <summary>Identifies the notification sent when the containing window loses keyboard focus.</summary>
    public const int NotificationWmWindowFocusOut = 1005;

    /// <summary>Identifies the notification sent when closure of the containing window is requested.</summary>
    public const int NotificationWmCloseRequest = 1006;

    /// <summary>Identifies the containing-window notification for an Android system back request.</summary>
    public const int NotificationWmGoBackRequest = 1007;

    /// <summary>Identifies the notification sent when this window updates its content size.</summary>
    public const int NotificationWmSizeChanged = 1008;

    /// <summary>Identifies the notification sent when the containing window reports a display content-scale change.</summary>
    public const int NotificationWmDpiChange = 1009;

    /// <summary>Identifies the notification sent when the containing window changes client position.</summary>
    /// <remarks>A confirmed native move reaches the containing subtree after position commit. A placement request alone does not emit it.</remarks>
    public const int NotificationWmPositionChanged = 1012;

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

    private readonly List<Node> _children = [];

    private readonly IReadOnlyList<Node> _childrenView;

    private readonly Dictionary<string, bool> _groups = new(StringComparer.Ordinal);

    private string _name;
    private string? _absolutePathCache;
    private volatile Viewport? _activeViewport;

    private string _sceneFilePath = string.Empty;

    private ObjectIdentity _ownerIdentity;
    private List<ObjectIdentity>? _ownedNodes;
    private Node? SceneOwner => _ownerIdentity.Target as Node;
    private bool _uniqueNameInOwner;

    private List<Resource>? _ownedSceneResources;
    private ResourceFileOwnership? _sharedSceneResourceOwnership;

    // Low bits hold publication/queued/claimed and closure; higher bits distinguish canceled generations.
    private long _queuedForDeletion;

    private int _sceneCaptureDepth;
    private int _notificationPropagationDepth;
    private List<List<Node>>? _pathRenameSnapshots;
    private int _pathRenameDepth;

    private int _sceneInstantiationDepth;

    private bool _readyCalled;
    internal bool HasStartedReady { get; private set; }

    private bool _isEnteringTree;

    private bool _isMakingReady;

    private bool _isExitingTree;
    private bool _hasPendingTreeExit;
    internal bool IsDepartingTree => _isExitingTree || _hasPendingTreeExit;

    private int _processPriority;

    private int _physicsProcessPriority;

    private readonly List<List<(Node Node, bool CanProcess, bool Disabled, ulong PauseRevision, ulong EnableRevision, ulong Attachment)>> _processModeSnapshots = [];
    private int _processModeDepth;
    private ulong _processEnableRevision;
    private ulong _processPauseRevision;
    private ProcessMode _processMode;

    private NodeAutoTranslateMode _autoTranslateMode;

    private bool _translationDomainInherited = true;

    private bool _processEnabled;

    private bool _physicsProcessEnabled;

    private bool _inputEnabled;

    private bool _shortcutInputEnabled;

    private bool _unhandledInputEnabled;

    private bool _unhandledKeyInputEnabled;

    private bool _internalProcessEnabled;

    private bool _internalPhysicsProcessEnabled;

    private double _unscaledProcessDeltaTime;

    private double _unscaledPhysicsProcessDeltaTime;

    /// <summary>Initializes a detached node with its runtime class name and no parent.</summary>
    public Node()
    {
        _name = ClassName;
        _childrenView = _children.AsReadOnly();
        _publicChildrenView = new OrdinaryChildrenView(this);
#if DEBUG
        DiagnosticNodes.Add(this, DiagnosticNodeMarker);
#endif
    }

    /// <summary>Gets or sets the node name used in sibling lookup and paths.</summary>
    /// <value>The nonblank name, initialized to <see cref="ElectronObject.ClassName"/>.</value>
    /// <remarks>
    /// Assignment replaces each <c>.</c>, <c>:</c>, <c>@</c>, <c>/</c>, <c>"</c> and <c>%</c> with <c>_</c> before
    /// readable sibling collision resolution. Every accepted assignment, including an unchanged name, propagates
    /// <see cref="NotificationPathRenamed"/> through its subtree. Only an active node then raises <see cref="Renamed"/>
    /// and tree events. Names and path caches commit before delivery; remaining notifications are attempted after errors.
    /// Renaming may revoke <see cref="UniqueNameInOwner"/> when another node with the same owner claims the resolved name.
    /// </remarks>
    /// <exception cref="ArgumentException">The assigned name is null, empty or whitespace-only.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="AggregateException">One or more path, node-renamed, or tree-renamed callbacks fail after the assignment commits.</exception>
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

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A node name cannot be null, empty or whitespace-only.", nameof(value));

            value = NormalizeNodeName(value);
            value = Parent?.ResolveChildName(this, value, readable: true) ?? value;
            _name = value;
            InvalidatePathCache();
            ClaimUniqueName();

            List<Exception>? errors = null;
            try
            {
                PropagatePathRenamed();
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }

            if (IsInsideTree)
            {
                try
                {
                    BeginEventEmission(_publisherRenamed.Handlers)?.Invoke(this);
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
            }
            ThrowCollected("One or more node-renamed callbacks failed.", errors);
        }
    }

    /// <summary>Gets the direct structural parent.</summary>
    /// <value>The parent, or <see langword="null"/> for a parentless node. A descendant may retain a parent while outside every tree.</value>
    /// <remarks>This stored observation remains available after disposal. Coordinate reads with topology mutation;
    /// it is not an atomic hierarchy snapshot or permission to mutate another scene.</remarks>
    public Node? Parent { get; private set; }

    /// <summary>Gets or sets this node's translation domain, inherited from its parent until explicitly assigned.</summary>
    /// <value>The effective domain, or the empty main domain for a parentless node without an override.</value>
    /// <remarks>An explicit assignment, including an empty string, stops inheritance. Changes notify affected active nodes.</remarks>
    /// <exception cref="ArgumentNullException">The assigned domain is null.</exception>
    /// <exception cref="InvalidOperationException">An attached node is read or mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public override string TranslationDomain
    {
        get
        {
            ThrowIfDisposed();
            Tree?.EnsureOwnerThread();
            return _translationDomainInherited ? Parent?.TranslationDomain ?? string.Empty : base.TranslationDomain;
        }
        set
        {
            EnsureMutable();
            ArgumentNullException.ThrowIfNull(value);
            if (!_translationDomainInherited && base.TranslationDomain == value)
                return;
            base.TranslationDomain = value;
            _translationDomainInherited = false;
            NotifyInheritedTranslationDomainChanged();
        }
    }

    /// <summary>Restores inheritance of the parent's translation domain, or the main domain without a parent.</summary>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void SetTranslationDomainInherited()
    {
        EnsureMutable();
        if (!_translationDomainInherited)
        {
            _translationDomainInherited = true;
            NotifyInheritedTranslationDomainChanged();
        }
    }

    internal bool IsTranslationDomainInherited => _translationDomainInherited;

    internal void InitializeRootAutoTranslateMode(bool enabled)
    {
        if (_autoTranslateMode == NodeAutoTranslateMode.Inherit)
            _autoTranslateMode = enabled ? NodeAutoTranslateMode.Always : NodeAutoTranslateMode.Disabled;
    }

    /// <summary>Gets or sets whether automatic translation is inherited, enabled, or disabled for this subtree.</summary>
    /// <value><see cref="NodeAutoTranslateMode.Inherit"/> by default; a scene root resolves it from <see cref="ProjectSettings.RootNodeAutoTranslate"/> when its tree starts.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined mode.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread, or an active root is set to Inherit.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public NodeAutoTranslateMode AutoTranslateMode
    {
        get { ThrowIfDisposed(); return _autoTranslateMode; }
        set
        {
            EnsureMutable();
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown automatic translation mode.");
            if (value == NodeAutoTranslateMode.Inherit && IsInsideTree && Parent is null)
                throw new InvalidOperationException("An active scene root cannot inherit automatic translation mode.");
            if (_autoTranslateMode == value)
                return;
            _autoTranslateMode = value;
            PropagateNotification(NotificationTranslationChanged);
        }
    }

    /// <summary>Gets whether this node's inherited automatic translation policy is enabled.</summary>
    /// <returns>True for the nearest ancestor's Always mode, or for a parentless inherited node.</returns>
    /// <exception cref="InvalidOperationException">An attached node is queried off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool CanAutoTranslate()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        for (Node? node = this; node is not null; node = node.Parent)
        {
            if (node._autoTranslateMode != NodeAutoTranslateMode.Inherit)
                return node._autoTranslateMode == NodeAutoTranslateMode.Always;
        }
        return true;
    }

    /// <summary>Translates a message only when automatic translation is enabled for this node.</summary>
    /// <param name="message">The source message.</param>
    /// <param name="context">An optional disambiguation context; null or empty uses the local default on a Control.</param>
    /// <returns>The resolved translation or the source message.</returns>
    /// <exception cref="ArgumentNullException">The message is null.</exception>
    /// <exception cref="InvalidOperationException">An attached node is queried off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public string Atr(string message, string? context = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(message);
        return CanAutoTranslate() ? Tr(message, ResolveAutomaticTranslationContext(context)) : message;
    }

    /// <summary>Translates singular or plural text only when automatic translation is enabled for this node.</summary>
    /// <param name="singular">The source singular form.</param>
    /// <param name="plural">The source plural form.</param>
    /// <param name="count">The message quantity.</param>
    /// <param name="context">An optional disambiguation context; null or empty uses the local default on a Control.</param>
    /// <returns>The resolved form or the source form for the quantity.</returns>
    /// <exception cref="ArgumentNullException">Either source form is null.</exception>
    /// <exception cref="InvalidOperationException">An attached node is queried off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public string AtrN(string singular, string plural, long count, string? context = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(singular);
        ArgumentNullException.ThrowIfNull(plural);
        return CanAutoTranslate() ? TrN(singular, plural, count, ResolveAutomaticTranslationContext(context)) : count == 1 ? singular : plural;
    }

    internal virtual string? ResolveAutomaticTranslationContext(string? context) => context;

    private void NotifyInheritedTranslationDomainChanged()
    {
        List<Exception>? errors = null;
        NotifyInheritedTranslationDomainChanged(ref errors);
        ThrowCollected("One or more translation-change callbacks failed.", errors);
    }

    private void NotifyInheritedTranslationDomainChanged(ref List<Exception>? errors)
    {
        if (!IsInsideTree)
            return;

        if (CanAutoTranslate())
        {
            try { DispatchNotification(NotificationTranslationChanged); }
            catch (Exception error) { CollectException(ref errors, error); }
        }

        foreach (var child in _children.ToArray())
            if (!child.IsDisposed && child._translationDomainInherited && ReferenceEquals(child.Parent, this))
                child.NotifyInheritedTranslationDomainChanged(ref errors);
    }

    /// <summary>Gets or sets this node's scene source path metadata.</summary>
    /// <value>Non-null text, initially empty. External packed-scene instantiation assigns its resource path to the root.</value>
    /// <remarks>The value is not inherited and assignment neither loads a file nor changes the linked scene template.
    /// Every valid assignment emits EditorStateChanged in the selected edited subtree, including an equal value.
    /// Observer errors propagate after commitment. The ordinary tooling descriptor is not stored; packed-scene
    /// capture retains nonroot origin metadata separately, while a reconstructed root takes its template's path.
    /// Reads require a live receiver and coordination with writes. Attached writes require the scene owner and reject
    /// capture and physical borrowing. Derived validation runs before commitment and its resulting lifetime is rechecked.</remarks>
    /// <exception cref="ArgumentNullException">The assigned value is null.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable or attached access is off the scene owner.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing or disposed.</exception>
    public string SceneFilePath
    {
        get
        {
            ThrowIfDisposed();
            return _sceneFilePath;
        }
        set => SetSceneFilePath(value);
    }

    /// <summary>Gets or sets the borrowed authoring owner used for packed-scene storage.</summary>
    /// <value>The live owner, or <see langword="null"/> when unassigned, disposed or collected. Assignment requires an ancestor.</value>
    /// <remarks>
    /// Owner does not select a physical world or own node lifetime. Detached topology changes retain the previous
    /// owner and its unique-name registration, even when it is no longer an ancestor. Active removal clears unreachable
    /// owners after structural callbacks, before TreeExited. Owner disposal releases metadata without disposing
    /// separately parented nodes. Reparent restores its reachable outside owner only on ordinary descendants sharing it.
    /// Reads require a live receiver and coordination with topology/owner mutation. Attached writes require the scene
    /// owner thread; capture rejects writes, including unchanged values. Invalid assignments preserve the prior owner.
    /// Assigning a new owner may revoke UniqueNameInOwner when that owner already has a live claim.
    /// </remarks>
    /// <exception cref="ArgumentException">The assigned node is this node or is not an ancestor.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the tree owner thread or scene capture is active.</exception>
    /// <exception cref="ObjectDisposedException">This node or the assigned owner is disposing or disposed.</exception>
    public Node? Owner
    {
        get
        {
            ThrowIfDisposed();
            return SceneOwner;
        }
        set
        {
            EnsureMutable();

            if (ReferenceEquals(SceneOwner, value))
                return;

            if (ReferenceEquals(this, value))
                throw new ArgumentException("A node cannot own itself.", nameof(value));

            if (value is not null)
            {
                ObjectDisposedException.ThrowIf(value.IsDisposed, value);
                if (!value.IsAncestorOf(this))
                    throw new ArgumentException("A node owner must be an ancestor.", nameof(value));
            }

            SetSceneOwner(value);
            ClaimUniqueName();
            if (value is not null) EmitEditorStateChanged();
        }
    }

    /// <summary>Gets or sets whether this node can be addressed as <c>%Name</c> within its owner's scene.</summary>
    /// <value>False by default. When another node already claims the same name for the same owner, enabling this property leaves it false.</value>
    /// <remarks>The flag may be set before an owner is assigned. Renaming or changing the owner can revoke it on a conflict. PackedScene stores the flag with the node.</remarks>
    /// <exception cref="InvalidOperationException">An attached mutation is off the scene owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool UniqueNameInOwner
    {
        get { ThrowIfDisposed(); return _uniqueNameInOwner; }
        set
        {
            EnsureMutable();
            if (_uniqueNameInOwner == value) return;
            _uniqueNameInOwner = value;
            ClaimUniqueName();
            Tree?.NotifyConfigurationWarningsChanged(this);
            EmitEditorStateChanged();
        }
    }

    /// <summary>Gets a live read-only view of the ordered ordinary direct children.</summary>
    /// <value>A view excluding internal children; later hierarchy changes are visible through it.</value>
    public IReadOnlyList<Node> Children => _publicChildrenView;

    /// <summary>Gets the number of direct children.</summary>
    /// <value>The current child count.</value>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    public int ChildCount
    {
        get
        {
            ThrowIfDisposed();
            return ExternalChildCount;
        }
    }

    /// <summary>Gets the active scene tree containing this node.</summary>
    /// <value>The associated tree, or <see langword="null"/> outside a tree.</value>
    /// <remarks>Entry assigns this association before callbacks. Departure retains it through NodeRemoved and
    /// ChildExitingTree, then clears the whole departing subtree before TreeExited. Stored observation remains
    /// available after disposal; coordinate it with topology mutation.</remarks>
    public SceneTree? Tree { get => Volatile.Read(ref _tree); private set => Volatile.Write(ref _tree, value); }

    private SceneTree? _tree;
    internal ulong TreeAttachmentVersion { get; private set; }

    /// <summary>Gets whether this node currently belongs to a scene tree.</summary>
    /// <value><see langword="true"/> when <see cref="Tree"/> is non-null.</value>
    /// <remarks>Structural parentage alone does not imply active membership. This stored observation remains
    /// available after disposal; coordinate reads with tree transitions.</remarks>
    public bool IsInsideTree => Tree is not null;

    /// <summary>Gets whether SceneTree-managed ready delivery has occurred since construction or the last ready reset.</summary>
    /// <value>The stored ready state. It remains true after detachment until <see cref="RequestReady"/> is called.</value>
    /// <remarks>Manual <see cref="ElectronObject.Notify(int)"/> delivery of <see cref="NotificationReady"/> does not change this value.
    /// Attached reads require the owner or current processing group. Detached and disposed nodes retain this stored observation.</remarks>
    /// <exception cref="InvalidOperationException">Attached access is outside the owner or current processing group.</exception>
    public bool IsNodeReady { get { Tree?.EnsureProcessingAccess(this); return _readyCalled; } }

    internal override bool HasQueuedNodeDeletion => (Volatile.Read(ref _queuedForDeletion) & 3) != 0;

    /// <summary>Returns this node's cached active viewport, including itself when it is a viewport.</summary>
    /// <returns>The nearest active viewport, or null while detached or in an active hierarchy without a viewport.</returns>
    /// <remarks>The association is published before enter callbacks and cleared before TreeExited.
    /// Reading this borrowed reference does not traverse the hierarchy or grant access to the returned viewport's operations.</remarks>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Viewport? GetViewport()
    {
        ThrowIfDisposed();
        return _activeViewport;
    }

    /// <summary>Finds this node's active containing window, including itself when it is a window.</summary>
    /// <returns>The containing window reached through active viewport parents, or null while detached or without a window.</returns>
    /// <remarks>Available during enter/exit callbacks, with detachment committed before TreeExited.
    /// The scene owner or this node's current processing group may query its window.</remarks>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="InvalidOperationException">The node is accessed outside its permitted scene processing scope.</exception>
    public Window? GetWindow()
    {
        ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this);
        for (var viewport = _activeViewport; viewport is not null; viewport = viewport.Parent?._activeViewport)
            if (viewport is Window window) return window;
        return null;
    }

    /// <summary>Finds the last active exclusive window starting at this node's containing window.</summary>
    /// <returns>The containing window or its last exclusive transient descendant; null while detached or without a containing window.</returns>
    /// <remarks>Follows active transient parent links rather than display order.
    /// A window without an active exclusive child is returned itself.</remarks>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    /// <exception cref="InvalidOperationException">This node is accessed outside its permitted scene processing scope.</exception>
    public Window? GetLastExclusiveWindow()
    {
        ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this);
        if (!IsInsideTree) return null;
        var window = GetWindow();
        while (window?.ExclusiveChild is { } child) window = child;
        return window;
    }

    /// <summary>Gets or sets the pause policy used by both process callback lanes.</summary>
    /// <value><see cref="ProcessMode.Inherit"/> initially; an entering root becomes Pausable.</value>
    /// <remarks>Detached changes are silent. An attached root cannot select Inherit. Attached changes notify this node
    /// and inheriting descendants parent-first: Paused/Unpaused when callback eligibility changes, then Disabled/Enabled
    /// when the effective disabled state changes. Explicit descendant modes stop propagation. Failures are collected
    /// after remaining notifications. Nested changes supersede stale outer delivery; moved or retired attachments skip.
    /// Prepare subtree capacity and reentrancy depth before a measured hot interval.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned enum value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Access is outside the scene owner/current processing group,
    /// mutation is off the owner thread, an attached root selects Inherit, or physical participation is borrowed.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="AggregateException">Transition callbacks throw after all remaining affected notifications are attempted.</exception>
    public ProcessMode ProcessMode
    {
        get
        {
            ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this);
            return _processMode;
        }
        set
        {
            EnsureMutable();
            if ((uint)value > (uint)ProcessMode.Disabled)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown process mode.");
            var tree = Tree;
            if (tree is not null && Parent is null && value == ProcessMode.Inherit)
                throw new InvalidOperationException("An attached root cannot inherit its process mode.");
            if (_processMode == value) return;
            if (tree is null) { _processMode = value; return; }
            var depth = _processModeDepth++;
            if (depth == _processModeSnapshots.Count) _processModeSnapshots.Add([]);
            var states = _processModeSnapshots[depth];
            try
            {
                CaptureProcessStates(this, states, tree.Paused);
                foreach (var state in states)
                    if (state.Node is CollisionObject collider) collider.EnsurePhysicsParticipationChange();
                _processMode = value;
                List<Exception>? errors = null;
                foreach (var state in states)
                {
                    var node = state.Node;
                    if (!Current()) continue;
                    var canProcess = node.CanProcessWhenPaused(tree.Paused);
                    if (node._processPauseRevision == state.PauseRevision && state.CanProcess != canProcess)
                    {
                        node._processPauseRevision++;
                        try { node.DispatchNotification(canProcess ? NotificationUnpaused : NotificationPaused); }
                        catch (Exception error) { CollectException(ref errors, error); }
                    }
                    if (!Current() || node._processEnableRevision != state.EnableRevision) continue;
                    var isDisabled = node.ResolveProcessMode() == ProcessMode.Disabled;
                    if (state.Disabled != isDisabled)
                    {
                        node._processEnableRevision++;
                        try { node.DispatchNotification(isDisabled ? NotificationDisabled : NotificationEnabled); }
                        catch (Exception error) { CollectException(ref errors, error); }
                    }
                    bool Current() => !node.IsDisposed && ReferenceEquals(node.Tree, tree) && node.TreeAttachmentVersion == state.Attachment;
                }
                try { EmitEditorStateChanged(); }
                catch (Exception error) { CollectException(ref errors, error); }
                ThrowCollected("Process mode notification callbacks failed.", errors);
            }
            finally { states.Clear(); _processModeDepth--; }
        }
    }

    /// <summary>Gets or sets whether this node participates in host-driven process frames.</summary>
    /// <value>False initially; ready notification enables an overridden callback before OnReady.</value>
    /// <remarks>A node absent from the captured idle schedule starts on the next idle frame. Disabling a captured
    /// node suppresses later delivery immediately. An existing internal participant may enable its public callback in that frame.
    /// Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot.
    /// Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture
    /// even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.
    /// Packed scenes store this configuration before ready applies automatic callback preparation.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.</exception>
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
            EnsureProcessingAccess();
            if (_processEnabled == value) return;
            _processEnabled = value;
            Tree?.InvalidateProcessOrder();
        }
    }

    /// <summary>Gets or sets whether this node participates in host-driven physics-process frames.</summary>
    /// <value>False initially; ready notification enables an overridden callback before OnReady.</value>
    /// <remarks>A node absent from the captured physics schedule starts on the next physics frame. Disabling a captured
    /// node suppresses later delivery immediately. An existing internal participant may enable its public callback in that frame.
    /// Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot.
    /// Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture
    /// even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.
    /// Packed scenes store this configuration before ready applies automatic callback preparation.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.</exception>
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
            EnsureProcessingAccess();
            if (_physicsProcessEnabled == value) return;
            _physicsProcessEnabled = value;
            Tree?.InvalidateProcessOrder();
        }
    }

    /// <summary>Gets or sets whether this node receives internal idle-frame notifications.</summary>
    /// <value>False for a plain Node. Built-in subclasses may enable their own internal work.</value>
    /// <remarks>This lane is independent of ProcessEnabled and runs before ordinary processing. The resolved
    /// ProcessMode and tree pause state still gate both lanes. Changes are runtime state, not stored scene data.
    /// Disabling a built-in node's internal work can suspend animation, timers or other implementation logic.
    /// Participation is captured together with ordinary idle processing. A newly enabled node waits for
    /// the next idle snapshot; a node already included through its ordinary callback can enable internal delivery in this frame.
    /// Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot.
    /// Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture
    /// even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool InternalProcessEnabled
    {
        get { ThrowIfDisposed(); return _internalProcessEnabled; }
        set { EnsureProcessingAccess(); if (_internalProcessEnabled == value) return; _internalProcessEnabled = value; Tree?.InvalidateProcessOrder(); }
    }

    /// <summary>Gets or sets whether this node receives internal fixed-frame notifications.</summary>
    /// <value>False for a plain Node. Built-in subclasses may enable their own internal work.</value>
    /// <remarks>This lane is independent of PhysicsProcessEnabled and runs before ordinary physics processing.
    /// The resolved ProcessMode and tree pause state still apply. This runtime switch is not stored in scenes.
    /// Disabling built-in internal physics can suspend casts, timers or other implementation logic.
    /// Participation is captured together with ordinary physics processing. A newly enabled node waits for
    /// the next physics snapshot; a node already included through its ordinary callback can enable internal delivery in this frame.
    /// Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot.
    /// Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture
    /// even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool InternalPhysicsProcessEnabled
    {
        get { ThrowIfDisposed(); return _internalPhysicsProcessEnabled; }
        set { EnsureProcessingAccess(); if (_internalPhysicsProcessEnabled == value) return; _internalPhysicsProcessEnabled = value; Tree?.InvalidateProcessOrder(); }
    }

    /// <summary>Gets or sets whether this node receives the first input-propagation stage.</summary>
    /// <value>False initially; ready notification enables an overridden callback before OnReady.</value>
    /// <remarks>Eligible nodes are visited in reverse depth-first order before unhandled-input stages.
    /// Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot.
    /// Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture
    /// even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.
    /// Packed scenes store this configuration before ready applies automatic callback preparation.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.</exception>
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
            EnsureProcessingAccess();
            _inputEnabled = value;
        }
    }

    /// <summary>Gets or sets whether this node receives keyboard, gamepad-button and shortcut events after GUI handling.</summary>
    /// <value>False initially; ready notification enables an overridden callback before OnReady.</value>
    /// <remarks>The stage precedes unhandled-key and general unhandled input and obeys the node's process policy.
    /// Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot.
    /// Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture
    /// even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.
    /// Packed scenes store this configuration before ready applies automatic callback preparation.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.</exception>
    /// <exception cref="ObjectDisposedException">The node has finished disposing.</exception>
    public bool ShortcutInputEnabled
    {
        get { ThrowIfDisposed(); return _shortcutInputEnabled; }
        set { EnsureProcessingAccess(); _shortcutInputEnabled = value; }
    }

    /// <summary>Gets or sets whether this node receives input left unhandled by earlier stages.</summary>
    /// <value>False initially; ready notification enables an overridden callback before OnReady.</value>
    /// <remarks>This final stage runs for every event that remains unhandled.
    /// Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot.
    /// Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture
    /// even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.
    /// Packed scenes store this configuration before ready applies automatic callback preparation.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.</exception>
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
            EnsureProcessingAccess();
            _unhandledInputEnabled = value;
        }
    }

    /// <summary>Gets or sets whether this node receives unhandled keyboard events before general unhandled input.</summary>
    /// <value>False initially; ready notification enables an overridden callback before OnReady.</value>
    /// <remarks>The stage is skipped for non-keyboard events and after <see cref="SceneTree.SetInputAsHandled"/>.
    /// Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot.
    /// Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture
    /// even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.
    /// Packed scenes store this configuration before ready applies automatic callback preparation.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.</exception>
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
            EnsureProcessingAccess();
            _unhandledKeyInputEnabled = value;
        }
    }

    /// <summary>Gets or sets this node's ascending process-frame order key.</summary>
    /// <value>Any integer; the default is zero. Equal priorities retain captured tree order within the group.</value>
    /// <remarks>Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot.
    /// Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture
    /// even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.</exception>
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
            EnsureProcessingAccess();
            if (_processPriority == value) return;
            _processPriority = value;
            Tree?.InvalidateProcessOrder();
        }
    }

    /// <summary>Gets or sets this node's ascending physics-process order key.</summary>
    /// <value>Any integer; the default is zero. Equal priorities retain captured tree order within the group.</value>
    /// <remarks>Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot.
    /// Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture
    /// even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.</exception>
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
            EnsureProcessingAccess();
            if (_physicsProcessPriority == value) return;
            _physicsProcessPriority = value;
            Tree?.InvalidateProcessOrder();
        }
    }

    /// <summary>Gets the current or most recent process-frame duration of the attached scene tree.</summary>
    /// <value>The tree's scaled process duration in seconds, or zero while detached or before its first process frame.</value>
    /// <remarks>All attached nodes observe the same lane duration, including disabled or paused nodes. It is committed before frame-start callbacks. Manual notification does not advance it.</remarks>
    /// <exception cref="InvalidOperationException">An attached node is read outside its scene owner or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public double ProcessDeltaTime
    {
        get { ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this); return Tree?.ProcessDelta ?? 0; }
    }

    /// <summary>Gets the current or most recent physics-frame duration of the attached scene tree.</summary>
    /// <value>The tree's scaled physics duration in seconds, or zero while detached or before its first physics frame.</value>
    /// <remarks>All attached nodes observe the same lane duration, including disabled or paused nodes. It is committed before frame-start callbacks. Manual notification does not advance it.</remarks>
    /// <exception cref="InvalidOperationException">An attached node is read outside its scene owner or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public double PhysicsProcessDeltaTime
    {
        get { ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this); return Tree?.PhysicsDelta ?? 0; }
    }

    /// <summary>Occurs on the parent after a direct child is structurally attached and child order is reported.</summary>
    /// <remarks>
    /// The first argument is the publishing parent and the second is the child. Delivery is synchronous and precedes
    /// active-tree attachment of the child's subtree.
    /// </remarks>
    public event Action<Node, Node>? ChildAdded { add => _publisherChildAdded.Add(value); remove => _publisherChildAdded.Remove(value); }
    private EventPublisher<Action<Node, Node>> _publisherChildAdded;

    /// <summary>Occurs on the former parent after a direct child is detached and child order is reported.</summary>
    /// <remarks>
    /// The first argument is the publishing former parent and the second is the removed child. Delivery is synchronous,
    /// and structural changes are not rolled back if a handler throws.
    /// </remarks>
    public event Action<Node, Node>? ChildRemoved { add => _publisherChildRemoved.Add(value); remove => _publisherChildRemoved.Remove(value); }
    private EventPublisher<Action<Node, Node>> _publisherChildRemoved;

    /// <summary>Occurs on the direct parent when a child enters the active tree.</summary>
    /// <remarks>
    /// The first argument is the publishing parent and the second is the entering child. Delivery follows that child's
    /// enter notification, callback, event and SceneTree node-added event.
    /// </remarks>
    public event Action<Node, Node>? ChildEnteredTree { add => _publisherChildEnteredTree.Add(value); remove => _publisherChildEnteredTree.Remove(value); }
    private EventPublisher<Action<Node, Node>> _publisherChildEnteredTree;

    /// <summary>Occurs on the direct parent while a child is exiting the active tree.</summary>
    /// <remarks>
    /// The first argument is the publishing parent and the second is the exiting child. Descendants have already exited,
    /// and the child's <see cref="Tree"/> is still set. Delivery follows its exit notification, physical release and SceneTree node-removed event.
    /// </remarks>
    public event Action<Node, Node>? ChildExitingTree { add => _publisherChildExitingTree.Add(value); remove => _publisherChildExitingTree.Remove(value); }
    private EventPublisher<Action<Node, Node>> _publisherChildExitingTree;

    /// <summary>Occurs after the order or membership of direct children changes.</summary>
    /// <remarks>The argument is this parent node. Delivery follows complete NotificationChildOrderChanged dispatch.
    /// Drawing-order publication, notification and signal are each attempted after earlier failures; errors aggregate
    /// after the committed mutation. Unchanged moves and absent removals are silent. Normal C# subscriber ordering applies.</remarks>
    public event Action<Node>? ChildOrderChanged { add => _publisherChildOrderChanged.Add(value); remove => _publisherChildOrderChanged.Remove(value); }
    private EventPublisher<Action<Node>> _publisherChildOrderChanged;

    /// <summary>Occurs after an active node accepts a name assignment and path notifications propagate.</summary>
    /// <remarks>The argument is this node. Unchanged names also raise the event; detached assignments only propagate path notifications.</remarks>
    public event Action<Node>? Renamed { add => _publisherRenamed.Add(value); remove => _publisherRenamed.Remove(value); }
    private EventPublisher<Action<Node>> _publisherRenamed;

    /// <summary>Occurs when this node enters an active scene tree.</summary>
    /// <remarks>Delivery follows <see cref="NotificationEnterTree"/> and <see cref="OnEnterTree"/>, and precedes descendant entry.</remarks>
    public event Action<Node>? TreeEntered { add => _publisherTreeEntered.Add(value); remove => _publisherTreeEntered.Remove(value); }
    private EventPublisher<Action<Node>> _publisherTreeEntered;

    /// <summary>Occurs while this node is exiting its active scene tree.</summary>
    /// <remarks>Descendants have exited and <see cref="OnExitTree"/> has run. Delivery precedes <see cref="NotificationExitTree"/> and engine membership release; <see cref="Tree"/> remains available.</remarks>
    public event Action<Node>? TreeExiting { add => _publisherTreeExiting.Add(value); remove => _publisherTreeExiting.Remove(value); }
    private EventPublisher<Action<Node>> _publisherTreeExiting;

    /// <summary>Occurs after this node has left its scene tree.</summary>
    /// <remarks>The complete departing subtree has no <see cref="Tree"/> association. Structural removal has cleared its root parent and invalid owners. Delivery visits siblings in reverse order, children before parents; lifecycle reentry remains rejected.</remarks>
    public event Action<Node>? TreeExited { add => _publisherTreeExited.Add(value); remove => _publisherTreeExited.Remove(value); }
    private EventPublisher<Action<Node>> _publisherTreeExited;

    /// <summary>Occurs after child-first ready notification delivery.</summary>
    /// <remarks>SceneTree-managed delivery occurs once until <see cref="RequestReady"/> resets the ready state.</remarks>
    public event Action<Node>? Ready { add => _publisherReady.Add(value); remove => _publisherReady.Remove(value); }
    private EventPublisher<Action<Node>> _publisherReady;

    /// <summary>Occurs after a replacement enters the former parent and before this node's children move to it.</summary>
    /// <remarks>The argument is the live replacement. This node has already left its parent. A callback failure is collected while replacement continues.</remarks>
    public event Action<Node>? ReplacingBy { add => _publisherReplacingBy.Add(value); remove => _publisherReplacingBy.Remove(value); }
    private EventPublisher<Action<Node>> _publisherReplacingBy;

    /// <summary>Appends a detached node as the last direct child.</summary>
    /// <param name="child">The live node to adopt.</param>
    /// <remarks>Name collisions receive a generated class name. If this node is active, the child's subtree enters
    /// immediately and receives ready where eligible. Use the bool overload for readable numeric suffixes.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="child"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="child"/> is this node.</exception>
    /// <exception cref="InvalidOperationException">
    /// The operation would create a cycle, the child already has a parent or tree, or mutation
    /// occurs off the owner thread, or child order is blocked during notification propagation.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// This node is disposing on another thread or has finished disposing, or disposal of <paramref name="child"/> has started.
    /// </exception>
    /// <exception cref="AggregateException">One or more structural, lifecycle, notification, or event callbacks fail after insertion begins.</exception>
    /// <param name="internalMode">Whether the child belongs to the internal front/back range; Disabled by default.</param>
    /// <exception cref="ArgumentOutOfRangeException">The internal mode is not defined.</exception>
    public void AddChild(Node child, InternalMode internalMode = InternalMode.Disabled) => AddChild(child, false, internalMode);

    /// <summary>Appends a detached child, choosing readable collision names when requested.</summary>
    /// <param name="child">The live node to adopt.</param>
    /// <param name="forceReadableName">Whether name collisions use a numeric suffix rather than a generated class name.</param>
    /// <param name="internalMode">The front, ordinary or back child partition.</param>
    /// <remarks>Existing unique names are preserved. Naming commits before parenting callbacks. The ordinary overload
    /// uses false. All insertion, lifetime, thread and failure rules of that overload apply.</remarks>
    public void AddChild(Node child, bool forceReadableName, InternalMode internalMode = InternalMode.Disabled)
    {
        if (!Enum.IsDefined(internalMode)) throw new ArgumentOutOfRangeException(nameof(internalMode));
        InsertChild(child, ChildInsertionIndex(internalMode), internalMode, forceReadableName);
    }

    /// <summary>Inserts a detached node immediately after this node in its parent's child order.</summary>
    /// <param name="sibling">The live node to insert.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sibling"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sibling"/> is the destination parent.</exception>
    /// <exception cref="InvalidOperationException">
    /// This node has no parent, insertion would create a cycle, the sibling is already attached, or
    /// mutation occurs off the owner thread, or child order is blocked during notification propagation.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// This node or its parent is disposing on another thread or has finished disposing, or disposal of
    /// <paramref name="sibling"/> has started.
    /// </exception>
    /// <exception cref="AggregateException">One or more structural, lifecycle, notification, or event callbacks fail after insertion begins.</exception>
    public void AddSibling(Node sibling) => AddSibling(sibling, false);

    /// <summary>Inserts a detached sibling after this node, resolving name collisions with the selected policy.</summary>
    /// <param name="sibling">The live node to insert.</param>
    /// <param name="forceReadableName">Whether a collision uses a readable numeric suffix.</param>
    /// <remarks>The sibling inherits this node's internal partition. Existing unique names are preserved;
    /// the ordinary overload uses false. Its hierarchy, lifetime, thread and failure rules apply.</remarks>
    public void AddSibling(Node sibling, bool forceReadableName)
    {
        ThrowIfDisposed();

        if (Parent is null)
            throw new InvalidOperationException("A root or detached node cannot add a sibling.");

        Parent.InsertChild(sibling, GetIndex(includeInternal: true) + 1, _internalMode, forceReadableName);
    }

    /// <summary>Removes a direct child without disposing it.</summary>
    /// <param name="child">The node to detach.</param>
    /// <returns><see langword="true"/> when the node was a direct child and was detached; otherwise <see langword="false"/>.</returns>
    /// <remarks>An active subtree exits in reverse sibling order, children before parents. Parent is cleared before
    /// structural callbacks; unreachable owners clear afterward, before TreeExited. Detached removal retains borrowed
    /// authoring owners. Neither form disposes the receiver or its children. An absent direct child returns false.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="child"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread, this parent is exiting or propagating a notification, or the child is in tree lifecycle delivery.</exception>
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
    /// <exception cref="InvalidOperationException">An attached node is mutated off the owner thread or its child order is blocked during notification propagation.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="AggregateException">One or more child-order or tree-change callbacks fail after the order changes.</exception>
    public void MoveChild(Node child, int index)
    {
        EnsureMutable();
        EnsureChildOrderMutable();
        ArgumentNullException.ThrowIfNull(child);

        var oldIndex = _children.IndexOf(child);
        if (oldIndex < 0)
            throw new ArgumentException("The node is not a direct child.", nameof(child));

        var start = child._internalMode switch { InternalMode.Front => 0, InternalMode.Back => _children.Count - _internalBackCount, _ => _internalFrontCount };
        var count = child._internalMode switch { InternalMode.Front => _internalFrontCount, InternalMode.Back => _internalBackCount, _ => ExternalChildCount };
        if (child._internalMode == InternalMode.Disabled && index == count) index--;
        index = start + NormalizeChildIndex(index, count);
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

    /// <summary>Moves this non-root node under a new parent.</summary>
    /// <param name="newParent">The live destination parent.</param>
    /// <param name="keepGlobalTransform">Whether a derived placement model preserves its global transform. The neutral base has no transform. The default is true.</param>
    /// <remarks>Validate the destination and its child-order barrier before removing the old parent. Append as an
    /// ordinary child. After removal callback failures, still attempt insertion and restoration of the reachable outside
    /// owner on ordinary descendants sharing it; aggregate original errors after all possible stages. Local subtree
    /// owners remain valid. Same-parent calls are silent but retain lifetime, capture, thread and lifecycle guards.
    /// A callback-retired destination is not resurrected and may leave a live detached receiver with an explicit error.
    /// Full structural changes and snapshots are cold; derived placement restoration follows attachment.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="newParent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The destination validation rejects this node as its own child.</exception>
    /// <exception cref="InvalidOperationException">
    /// This node has no parent, the move creates a cycle, either attached hierarchy
    /// is accessed off its owner thread, this node or a parent is in protected lifecycle/notification delivery,
    /// or a derived placement model rejects its destination.
    /// </exception>
    /// <exception cref="ObjectDisposedException">This node or <paramref name="newParent"/> is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="AggregateException">One or more structural, lifecycle, notification, or event callbacks fail after reparenting begins.</exception>
    public virtual void Reparent(Node newParent, bool keepGlobalTransform = true)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(newParent);

        if (_isEnteringTree || _isMakingReady || _isExitingTree || _hasPendingTreeExit)
            throw new InvalidOperationException("A node cannot be reparented while its tree lifecycle is in progress.");

        if (Parent is null)
            throw new InvalidOperationException("A root or detached node cannot be reparented.");

        if (ReferenceEquals(Parent, newParent))
            return;

        newParent.ValidateChildForInsertion(this, allowExistingParent: true);
        newParent.EnsureChildOrderMutable();
        var owner = SceneOwner;
        List<Node>? owned = null;
        if (owner is not null && (ReferenceEquals(owner, newParent) || owner.IsAncestorOf(newParent)))
        {
            owned = [];
            CollectOwned(this, owner, owned);
        }

        var oldParent = Parent;
        oldParent.EnsureMutable();
        List<Exception>? errors = null;
        try { oldParent.RemoveChildCore(this); }
        catch (AggregateException error) { CollectException(ref errors, error); }
        try { newParent.InsertChild(this, newParent.ChildInsertionIndex(InternalMode.Disabled)); }
        catch (Exception error) { CollectException(ref errors, error); }
        if (ReferenceEquals(Parent, newParent) && owned is not null)
            foreach (var node in owned)
                try
                {
                    if (!node.IsDisposed && !owner!.IsDisposed && owner.IsAncestorOf(node)) node.Owner = owner;
                }
                catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("One or more reparent callbacks failed.", errors);

        static void CollectOwned(Node node, Node owner, List<Node> result)
        {
            if (ReferenceEquals(node.SceneOwner, owner)) result.Add(node);
            foreach (var child in node._children)
                if (child._internalMode == InternalMode.Disabled) CollectOwned(child, owner, result);
        }
    }

    /// <summary>Gets a direct child by index.</summary>
    /// <param name="index">The child index; negative values count from the end.</param>
    /// <param name="includeInternal">Whether front/back implementation children participate in indexing.</param>
    /// <returns>The selected direct child.</returns>
    /// <exception cref="ArgumentOutOfRangeException">This node has no children or <paramref name="index"/> is outside the valid range.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public Node GetChild(int index, bool includeInternal = false)
    {
        ThrowIfDisposed();
        index = NormalizeChildIndex(index, includeInternal ? _children.Count : ExternalChildCount);
        return _children[index + (includeInternal ? 0 : _internalFrontCount)];
    }

    /// <summary>Gets this node's index in its parent's ordered child list.</summary>
    /// <param name="includeInternal">Whether implementation children participate; required when this node is internal.</param>
    /// <returns>The zero-based sibling index, or <c>-1</c> when this node has no parent.</returns>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    /// <exception cref="InvalidOperationException">An internal node is queried without including internal children.</exception>
    public int GetIndex(bool includeInternal = false)
    {
        ThrowIfDisposed();
        if (!includeInternal && IsInternalChild) throw new InvalidOperationException("An internal child requires includeInternal indexing.");
        return Parent is null ? -1 : Parent._children.IndexOf(this) - (includeInternal ? 0 : Parent._internalFrontCount);
    }

    /// <summary>Returns this node's current configuration warnings for tooling.</summary>
    /// <returns>An empty array by default. Overrides return ordered warning messages and should include base warnings.</returns>
    /// <remarks>This query does not cache results, emit events or require an edited scene. Attached queries run on
    /// the scene owner or this node's current processing group. Consumers may call it after NodeConfigurationWarningChanged to refresh their display.</remarks>
    /// <exception cref="InvalidOperationException">An attached query runs outside the owner/current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public virtual string[] GetConfigurationWarnings()
    {
        ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this); return [];
    }

    /// <summary>Requests a configuration-warning refresh for this node in the selected edited scene.</summary>
    /// <remarks>Emits SceneTree.NodeConfigurationWarningChanged synchronously only when this node is the
    /// EditedSceneRoot or its descendant. Detached nodes and trees without a selected scene emit nothing.
    /// It does not query, cache or compare warning strings. Repeated requests each emit; handler errors propagate.
    /// Delivery is synchronous on the owner or this node's current processing group, without numerical or native permissions.</remarks>
    /// <exception cref="InvalidOperationException">Capture/derived validation rejects access or this is outside the owner/current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    /// <exception cref="Exception">A warning-change subscriber fails.</exception>
    public void UpdateConfigurationWarnings()
    {
        EnsureProcessingAccess(); RenderingServer.NotifyRenderMutation(); Tree?.NotifyConfigurationWarningsChanged(this);
    }

    /// <summary>Determines whether this node is a strict ancestor of another node.</summary>
    /// <param name="node">The node whose parent chain is inspected.</param>
    /// <returns><see langword="true"/> when this node appears in the parent chain; otherwise <see langword="false"/>, including a self query.</returns>
    /// <remarks>Uses structural parents independently of active tree membership. Coordinate reads with topology changes;
    /// this query does not grant mutation access or require the current processing group.</remarks>
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
    /// <param name="pattern">A case-sensitive name pattern: <c>*</c> matches any sequence and <c>?</c> one Unicode character except a period.</param>
    /// <param name="recursive">Whether descendants below direct children are searched, including internal children.</param>
    /// <param name="owned">Whether branches without an owner are skipped; true by default.</param>
    /// <returns>The first matching node in depth-first pre-order, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An attached search runs outside its scene owner or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node or a recursively searched node is disposing on another thread, or has finished disposing.</exception>
    public Node? FindChild(string pattern, bool recursive = true, bool owned = true) => FindChild<Node>(pattern, recursive, owned);

    /// <summary>Finds the first descendant of a requested type whose name matches a wildcard pattern.</summary>
    /// <typeparam name="TNode">The required node subtype.</typeparam>
    /// <param name="pattern">A case-sensitive name pattern: <c>*</c> matches any sequence and <c>?</c> one Unicode character except a period.</param>
    /// <param name="recursive">Whether descendants below direct children are searched, including internal children.</param>
    /// <param name="owned">Whether branches without an owner are skipped; true by default.</param>
    /// <returns>The first typed match in depth-first pre-order, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An attached search runs outside its scene owner or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node or a recursively searched node is disposing on another thread, or has finished disposing.</exception>
    public TNode? FindChild<TNode>(string pattern = "*", bool recursive = true, bool owned = true)
        where TNode : Node
    {
        ThrowIfDisposed();
        Tree?.EnsureProcessingAccess(this);
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        foreach (var child in _children)
        {
            if (owned && child.SceneOwner is null) continue;
            if (child is TNode typedChild && MatchesNodeName(pattern, child._name))
                return typedChild;

            if (recursive && child.FindChild<TNode>(pattern, recursive: true, owned) is { } descendant)
                return descendant;
        }

        return null;
    }

    /// <summary>Finds all descendants whose names match a wildcard pattern.</summary>
    /// <param name="pattern">A case-sensitive name pattern: <c>*</c> matches any sequence and <c>?</c> one Unicode character except a period.</param>
    /// <param name="recursive">Whether descendants below direct children are searched, including internal children.</param>
    /// <param name="owned">Whether branches without an owner are skipped; true by default.</param>
    /// <returns>A read-only snapshot in depth-first pre-order.</returns>
    /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An attached search runs outside its scene owner or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public IReadOnlyList<Node> FindChildren(string pattern, bool recursive = true, bool owned = true)
    {
        ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this); ArgumentException.ThrowIfNullOrEmpty(pattern);
        return FindChildren<Node>(pattern, recursive, owned);
    }

    /// <summary>Finds all descendants of a requested type whose names match a wildcard pattern.</summary>
    /// <typeparam name="TNode">The required node subtype.</typeparam>
    /// <param name="pattern">A case-sensitive name pattern: <c>*</c> matches any sequence and <c>?</c> one Unicode character except a period. An empty pattern selects every name.</param>
    /// <param name="recursive">Whether descendants below direct children are searched, including internal children.</param>
    /// <param name="owned">Whether branches without an owner are skipped; true by default.</param>
    /// <returns>A read-only typed snapshot in depth-first pre-order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An attached search runs outside its scene owner or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public IReadOnlyList<TNode> FindChildren<TNode>(string pattern = "*", bool recursive = true, bool owned = true)
        where TNode : Node
    {
        ThrowIfDisposed();
        Tree?.EnsureProcessingAccess(this);
        ArgumentNullException.ThrowIfNull(pattern);

        var result = new List<TNode>();
        FindChildrenCore(pattern, recursive, owned, result);
        return result.AsReadOnly();
    }

    /// <summary>Finds the nearest ancestor whose name matches a wildcard pattern.</summary>
    /// <param name="pattern">A case-sensitive name pattern: <c>*</c> matches any sequence and <c>?</c> one Unicode character except a period.</param>
    /// <returns>The nearest matching ancestor, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An attached search runs outside its scene owner or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public Node? FindParent(string pattern)
    {
        ThrowIfDisposed();
        Tree?.EnsureProcessingAccess(this);
        ArgumentNullException.ThrowIfNull(pattern);

        for (var current = Parent; current is not null; current = current.Parent)
        {
            if (MatchesNodeName(pattern, current._name))
                return current;
        }

        return null;
    }

    /// <summary>Gets this node's absolute path in its active scene tree.</summary>
    /// <returns>A slash-prefixed path including the root name, or an empty string outside an active tree.</returns>
    /// <remarks>The immutable result is cached until this node's ancestry, tree membership or an ancestor name changes.</remarks>
    /// <exception cref="InvalidOperationException">An attached read runs outside the owner thread or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public string GetPath()
    {
        ThrowIfDisposed();
        Tree?.EnsureProcessingAccess(this);
        if (Tree is null) return string.Empty;
        if (_absolutePathCache is not null) return _absolutePathCache;
        var length = 0;
        for (Node? current = this; current is not null; current = current.Parent)
            length = checked(length + current._name.Length + 1);
        return _absolutePathCache = string.Create(length, this, static (buffer, node) =>
        {
            var index = buffer.Length;
            for (Node? current = node; current is not null; current = current.Parent)
            {
                index -= current._name.Length;
                current._name.AsSpan().CopyTo(buffer[index..]);
                buffer[--index] = '/';
            }
        });
    }

    /// <summary>Builds a relative path from this node to another node in the same hierarchy.</summary>
    /// <param name="node">The destination node.</param>
    /// <param name="useUniquePath">Whether to shorten the path through eligible owner-scoped unique names.</param>
    /// <returns><c>.</c> for this node, otherwise a slash-separated sequence of <c>..</c>, child names and optional owner-scoped unique names.</returns>
    /// <remarks>With <paramref name="useUniquePath"/>, the first eligible unique node on the target side replaces the preceding path. Otherwise an eligible source-side unique node may prefix the upward path, even when that makes the result longer.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The nodes do not share a hierarchy root, or an attached read runs outside the owner thread or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">
    /// This node is disposing on another thread or has finished disposing, or disposal of <paramref name="node"/> has started.
    /// </exception>
    public string GetPathTo(Node node, bool useUniquePath = false)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(node);
        ObjectDisposedException.ThrowIf(node.IsDisposed, node);
        Tree?.EnsureProcessingAccess(this);

        if (ReferenceEquals(this, node))
            return ".";

        var from = GetAncestry();
        var to = node.GetAncestry();
        var common = 0;

        while (common < from.Count && common < to.Count && ReferenceEquals(from[common], to[common]))
            common++;

        if (common == 0)
            throw new InvalidOperationException("Nodes do not share a hierarchy root.");

        var commonParent = from[common - 1];
        var reversed = new List<string>();
        if (useUniquePath)
        {
            var current = node;
            var detected = false;
            while (!ReferenceEquals(current, commonParent))
            {
                if (current._uniqueNameInOwner && ReferenceEquals(current.SceneOwner, SceneOwner))
                {
                    reversed.Add('%' + current.Name);
                    detected = true;
                    break;
                }
                reversed.Add(current.Name);
                current = current.Parent!;
            }
            if (!detected)
            {
                current = this;
                string? detectedName = null;
                var upCount = 0;
                while (!ReferenceEquals(current, commonParent))
                {
                    if (current._uniqueNameInOwner && ReferenceEquals(current.SceneOwner, SceneOwner))
                    { detectedName = current.Name; upCount = 0; }
                    upCount++;
                    current = current.Parent!;
                }
                for (var i = 0; i < upCount; i++) reversed.Add("..");
                if (detectedName is not null) reversed.Add('%' + detectedName);
            }
        }
        else
        {
            for (var index = to.Count - 1; index >= common; index--) reversed.Add(to[index].Name);
            for (var index = from.Count - 1; index >= common; index--) reversed.Add("..");
        }
        reversed.Reverse();
        return string.Join('/', reversed);
    }

    /// <summary>Resolves a required relative or absolute node path.</summary>
    /// <param name="path">A slash-separated path supporting <c>.</c>, <c>..</c>, owner-scoped <c>%Name</c>, and an optional absolute root-name segment.</param>
    /// <returns>The resolved node.</returns>
    /// <remarks>Absolute paths require active scene membership and start with the scene root's name. Colon-separated property subnames are validated for empty components but do not participate in node traversal; a property-only path addresses this node. Empty paths and a lone colon do not resolve; whitespace is matched literally.</remarks>
    /// <exception cref="ArgumentException"><paramref name="path"/> contains an empty intermediate property subname.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An attached lookup runs outside the owner thread or current processing group.</exception>
    /// <exception cref="KeyNotFoundException">No node exists at the requested path.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public Node GetNode(string path) => GetNodeOrNull(path) ?? throw new KeyNotFoundException($"Node path '{path}' was not found from '{GetPath()}'.");

    /// <summary>Resolves a required relative or absolute path to a requested node type.</summary>
    /// <typeparam name="TNode">The required node subtype.</typeparam>
    /// <param name="path">A slash-separated node path.</param>
    /// <returns>The resolved node cast to <typeparamref name="TNode"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> contains an empty intermediate property subname.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidCastException">The resolved node is not a <typeparamref name="TNode"/>.</exception>
    /// <exception cref="InvalidOperationException">An attached lookup runs outside the owner thread or current processing group.</exception>
    /// <exception cref="KeyNotFoundException">No node exists at the requested path.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public TNode GetNode<TNode>(string path)
        where TNode : Node => GetNode(path) as TNode ?? throw new InvalidCastException($"Node at '{path}' is not a {typeof(TNode).Name}.");

    /// <summary>Attempts to resolve a relative or absolute node path.</summary>
    /// <param name="path">A slash-separated path supporting <c>.</c>, <c>..</c>, owner-scoped <c>%Name</c>, and an optional absolute root-name segment.</param>
    /// <returns>The resolved node, or <see langword="null"/> for an empty path or when traversal cannot continue.</returns>
    /// <remarks>Absolute paths require active scene membership and start with the scene root's name. Colon-separated property subnames are validated for empty components but do not participate in node traversal; a property-only path addresses this node. Empty paths and a lone colon do not resolve; whitespace is matched literally.</remarks>
    /// <exception cref="ArgumentException"><paramref name="path"/> contains an empty intermediate property subname.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An attached lookup runs outside the owner thread or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public Node? GetNodeOrNull(string path)
    {
        ThrowIfDisposed();
        Tree?.EnsureProcessingAccess(this);
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0) return null;
        var nodePath = path.AsSpan();
        var subpath = nodePath.IndexOf(':');
        if (subpath >= 0)
        {
            if (nodePath.Contains("::", StringComparison.Ordinal))
                throw new ArgumentException("Node property path components cannot be empty.", nameof(path));
            if (path.Length == 1) return null;
            nodePath = nodePath[..subpath];
        }
        var absolute = path[0] == '/';
        if (absolute && Tree is null) return null;
        Node? current = absolute ? GetHierarchyRoot() : this;
        var needsRoot = absolute;
        while (!nodePath.IsEmpty)
        {
            var end = nodePath.IndexOf('/');
            var part = end < 0 ? nodePath : nodePath[..end];
            nodePath = end < 0 ? default : nodePath[(end + 1)..];
            if (part.IsEmpty) continue;
            if (needsRoot)
            {
                if (!part.SequenceEqual(current._name)) return null;
                needsRoot = false;
            }
            else if (part.SequenceEqual("..")) current = current.Parent;
            else if (!part.SequenceEqual("."))
            {
                if (part[0] == '%') current = current.FindUniqueOwned(part[1..]) ?? current.SceneOwner?.FindUniqueOwned(part[1..]);
                else
                {
                    Node? child = null;
                    foreach (var candidate in current._children)
                        if (part.SequenceEqual(candidate._name)) { child = candidate; break; }
                    current = child;
                }
            }
            if (current is null) return null;
        }
        return needsRoot ? null : current;
    }

    /// <summary>Reports whether a relative or absolute node path resolves from this node.</summary>
    /// <param name="path">A node path.</param>
    /// <returns>True when the target node exists; false when traversal cannot continue.</returns>
    /// <exception cref="ArgumentException">The path contains an empty intermediate property subname.</exception>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="InvalidOperationException">An attached query runs outside the owner thread or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public bool HasNode(string path) => GetNodeOrNull(path) is not null;

    /// <summary>Tests whether this node follows another node in the same active tree's depth-first order.</summary>
    /// <param name="node">The other live node.</param>
    /// <returns>True when this node is later; descendants follow their ancestors.</returns>
    /// <remarks>Includes internal children and reuses ancestry links without allocating a temporary path.</remarks>
    /// <exception cref="ArgumentNullException">The other node is null.</exception>
    /// <exception cref="InvalidOperationException">Either node is detached, belongs to another tree, or access is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">Either node is disposed.</exception>
    public bool IsGreaterThan(Node node)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(node);
        ObjectDisposedException.ThrowIf(node.IsDisposed, node);
        var tree = Tree ?? throw new InvalidOperationException("Tree order requires active nodes.");
        tree.EnsureOwnerThread();
        if (!ReferenceEquals(node.Tree, tree)) throw new InvalidOperationException("Nodes must belong to the same tree.");
        return IsGreaterThanCore(node);
    }

    /// <summary>Returns this node and its descendants as relative paths in depth-first order.</summary>
    /// <returns>One path per line, starting with a dot for this node, with a trailing newline.</returns>
    /// <remarks>Available for detached and attached hierarchies. Attached queries require the owner thread.</remarks>
    /// <exception cref="InvalidOperationException">An attached query runs off-owner.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public string GetTreeString()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        var result = new StringBuilder();
        AppendTreeString(result, ".");
        return result.ToString();
    }

    /// <summary>Returns this node and its descendants as an indented Unicode tree.</summary>
    /// <returns>A tree starting with this node's name and ending in a newline.</returns>
    /// <remarks>Available for detached and attached hierarchies. Attached queries require the owner thread.</remarks>
    /// <exception cref="InvalidOperationException">An attached query runs off-owner.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public string GetTreeStringPretty()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        var result = new StringBuilder();
        AppendTreeStringPretty(result, "", true);
        return result.ToString();
    }

    /// <summary>Writes the relative tree string to standard output.</summary>
    /// <remarks>The tree string already ends in a newline; printing adds another, matching the reference diagnostic.</remarks>
    public void PrintTree() => OS.LogMessage(GetTreeString());

    /// <summary>Writes the indented tree string to standard output.</summary>
    /// <remarks>The tree string already ends in a newline; printing adds another.</remarks>
    public void PrintTreePretty() => OS.LogMessage(GetTreeStringPretty());

    /// <summary>Delivers a notification to this node and then every descendant in parent-first order.</summary>
    /// <param name="what">The notification identifier.</param>
    /// <remarks>Direct child insertion, removal and reordering are rejected while this node is being traversed.
    /// The owner or current processing group delivers callbacks immediately. Inaccessible explicit-group branches
    /// reject without visiting their descendants; later siblings continue. Callback failures are combined afterward.
    /// Individual notification handlers retain their own capture, renderer and physics access requirements.</remarks>
    /// <exception cref="InvalidOperationException">An attached receiver is outside the owner/current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    /// <exception cref="AggregateException">One or more callbacks fail after traversal continues.</exception>
    public void PropagateNotification(int what) => PropagateNotificationCore(what, stopAtWindows: false, processingScope: true);

    internal void PropagateWindowNotification(int what) => PropagateNotificationCore(what, stopAtWindows: true);

    private void PropagateNotificationCore(int what, bool stopAtWindows, bool processingScope = false)
    {
        ThrowIfDisposed();
        if (processingScope) Tree?.EnsureProcessingAccess(this);
        else Tree?.EnsureOwnerThread();
        List<Exception>? errors = null;
        Interlocked.Increment(ref _notificationPropagationDepth);
        try
        {
            try { DispatchNotification(what); }
            catch (Exception error) { CollectException(ref errors, error); }
            foreach (var child in _children)
                if (!child.IsDisposed && ReferenceEquals(child.Parent, this) && (!stopAtWindows || child is not Window))
                    try { child.PropagateNotificationCore(what, stopAtWindows, processingScope); }
                    catch (Exception error) { CollectException(ref errors, error); }
        }
        finally { Interlocked.Decrement(ref _notificationPropagationDepth); }
        ThrowCollected("One or more propagated notification callbacks failed.", errors);
    }

    /// <summary>Adds this node to a case-sensitive group.</summary>
    /// <param name="group">The nonempty group identifier; whitespace and Unicode are preserved.</param>
    /// <param name="persistent">Whether packed scenes containing this node should retain the membership.</param>
    /// <remarks>The first addition chooses persistence. Adding an existing group does not change that choice;
    /// remove the membership before adding it with another persistence policy.</remarks>
    /// <exception cref="ArgumentException"><paramref name="group"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Attached access is outside the owner/current processing group or scene capture is active.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public void AddToGroup(string group, bool persistent = false)
    {
        EnsureProcessingAccess();
        ArgumentException.ThrowIfNullOrEmpty(group);
        if (_groups.TryAdd(group, persistent) && persistent) EmitEditorStateChanged();
    }

    /// <summary>Removes this node from a case-sensitive group.</summary>
    /// <param name="group">The literal group identifier; an empty identifier has no membership.</param>
    /// <returns><see langword="true"/> when membership existed and was removed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Attached access is outside the owner/current processing group or scene capture is active.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public bool RemoveFromGroup(string group)
    {
        EnsureProcessingAccess();
        ArgumentNullException.ThrowIfNull(group);
        if (!_groups.Remove(group, out var persistent)) return false;
        if (persistent) EmitEditorStateChanged();
        return true;
    }

    /// <summary>Determines whether this node belongs to a case-sensitive group.</summary>
    /// <param name="group">The literal group identifier; an empty identifier has no membership.</param>
    /// <returns><see langword="true"/> when this node is a member; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">Attached access is outside the scene owner or current processing group.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public bool IsInGroup(string group)
    {
        ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this);
        ArgumentNullException.ThrowIfNull(group);
        return _groups.ContainsKey(group);
    }

    /// <summary>Returns this node's group memberships.</summary>
    /// <returns>A read-only snapshot sorted using ordinal string order.</returns>
    /// <exception cref="InvalidOperationException">Attached access is outside the scene owner or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public IReadOnlyList<string> GetGroups()
    {
        ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this);
        return Array.AsReadOnly(_groups.Keys.Order(StringComparer.Ordinal).ToArray());
    }

    /// <summary>Determines whether the resolved process mode allows callbacks in the current tree pause state.</summary>
    /// <returns><see langword="false"/> while detached or disabled; otherwise the result of the resolved pause policy.</returns>
    /// <exception cref="InvalidOperationException">Access is outside the scene owner or current processing group.</exception>
    /// <exception cref="ObjectDisposedException">This node or its tree is disposing on another thread, or has finished disposing.</exception>
    public bool CanProcess()
    {
        ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this);
        return Tree is { } tree && CanProcessWhenPaused(tree.Paused);
    }

    /// <summary>Requests ready delivery the next time SceneTree attachment reaches the ready phase.</summary>
    /// <remarks>The method only resets this node's stored ready state; it never delivers ready immediately or changes descendants.
    /// A request during ready takes effect on the next attachment. The owner or current processing group may update it, including on physical nodes, without waking the body.</remarks>
    /// <exception cref="InvalidOperationException">Attached access is outside the owner/current processing group or scene capture is active.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public void RequestReady()
    {
        EnsureProcessingAccess();
        _readyCalled = false;
    }

    /// <summary>Creates a bound tween in this node's tree or the current application scene tree.</summary>
    /// <returns>A running empty tween that halts while this node is detached and is killed when this node is disposed.</returns>
    /// <remarks>An attached node selects its own tree. A detached node selects <see cref="Engine.MainLoop"/> when
    /// it is a <see cref="SceneTree"/> and waits until it joins that tree. The caller must append at least one tweener
    /// before the next eligible matching frame, including a zero-delta frame.</remarks>
    /// <exception cref="InvalidOperationException">No scene tree is available or the call is made off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node or its tree is disposing or disposed.</exception>
    public Tween CreateTween()
    {
        ThrowIfDisposed();
        var tree = Tree ?? Engine.MainLoop as SceneTree ?? throw new InvalidOperationException("No scene tree is available to create the tween.");
        return tree.CreateTween().BindNode(this);
    }

    /// <summary>Atomically requests this node's deferred disposal at a future scene-tree safe point.</summary>
    /// <remarks>
    /// A request made while already detached is queued if the node later enters a tree. Removing the node before its
    /// current tree flushes does not cancel deletion: that tree disposes the detached node at its safe point. If the
    /// node has entered another tree first, the old entry leaves the request intact for the new tree. Repeated calls
    /// are idempotent. Cancellation invalidates that exact request: a later request receives a new FIFO position,
    /// including when it is created during a captured deletion batch. The method may be called from a non-owner thread.
    /// </remarks>
    /// <exception cref="InvalidOperationException">This node is the active scene-tree root.</exception>
    /// <exception cref="ObjectDisposedException">Deletion publication has closed, or this node is disposing on another thread or has finished disposing.</exception>
    public void QueueFree()
    {
        EnsureNotSceneCapture();

        if (Tree?.IsRoot(this) == true)
            throw new InvalidOperationException("The SceneTree root cannot be queued for deletion.");

        var spinner = new SpinWait();

        while (true)
        {
            var state = Volatile.Read(ref _queuedForDeletion);
            var phase = state & 3;
            ObjectDisposedException.ThrowIf((state & 4) != 0, this);
            if (phase is 2 or 3)
                return;

            if (phase == 1)
            {
                spinner.SpinOnce();
                continue;
            }

            var publishing = unchecked((state & ~7L) + 9);
            if (Interlocked.CompareExchange(ref _queuedForDeletion, publishing, state) != state)
                continue;

            try
            {
                var tree = Tree;
                if (tree is null)
                    Interlocked.CompareExchange(ref _queuedForDeletion, publishing + 1, publishing);
                else
                    tree.QueueForDeletion(this, publishing + 1);

                return;
            }
            catch
            {
                Interlocked.CompareExchange(ref _queuedForDeletion, publishing & ~7L, publishing);
                throw;
            }
        }
    }

    /// <summary>Atomically cancels a pending deletion request.</summary>
    /// <returns><see langword="true"/> when a request was pending; otherwise <see langword="false"/>.</returns>
    /// <remarks>A stale entry may remain, but it cannot consume a later reissued request. Returns false once the
    /// current request has been claimed for destruction. This cancels queued node work, not an executing Dispose
    /// or an independent SceneTree.QueueDelete request. Use inherited CancelDisposal only from automatic pre-delete
    /// notification to veto that disposal attempt.</remarks>
    /// <exception cref="ObjectDisposedException">This node is disposing on another thread or has finished disposing.</exception>
    public bool CancelFree()
    {
        EnsureNotSceneCapture();
        while (true)
        {
            var state = Volatile.Read(ref _queuedForDeletion);
            if ((state & 4) != 0 || (state & 3) is 0 or 3) return false;
            if (Interlocked.CompareExchange(ref _queuedForDeletion, state & ~7L, state) == state) return true;
        }
    }

    /// <summary>Creates a reusable factory for packed scenes and duplicates of this exact runtime node type.</summary>
    /// <returns>A non-null factory that creates a fresh node of the exact same runtime type.</returns>
    /// <remarks>
    /// The base implementation supports only an exact <see cref="Node"/>. Derived node types that support packing or duplication must
    /// return a static, non-capturing factory that remains valid after the source node is disposed and creates a live,
    /// detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore
    /// the instance state.
    /// </remarks>
    /// <exception cref="NotSupportedException">A derived node has not explicitly supplied an instancing factory.</exception>
    protected virtual Func<Node> CreateSceneInstanceFactory()
    {
        if (GetType() != typeof(Node))
            throw new NotSupportedException($"{GetType().Name} must override {nameof(CreateSceneInstanceFactory)} to support packed scenes and node duplication.");

        return CreateDefaultSceneNode;
    }

    /// <summary>Called synchronously when this node enters an active scene tree.</summary>
    /// <remarks>
    /// <see cref="Tree"/> is already assigned. The callback runs parent-first, before <see cref="TreeEntered"/>, before
    /// descendants enter, and on the tree owner thread during SceneTree-managed lifecycle. Enter notification dispatch
    /// has completed before this callback. Manual tree notifications do not invoke it.
    /// </remarks>
    protected virtual void OnEnterTree()
    {
    }

    /// <summary>Called synchronously when this node exits an active scene tree.</summary>
    /// <remarks>
    /// Descendants have already exited and <see cref="Tree"/> remains assigned. The callback precedes
    /// <see cref="TreeExiting"/>, the exit notification and engine membership release, and runs on the tree owner thread.
    /// Manual tree notifications do not invoke this callback.
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
    /// <remarks>Ready enables an actual override before OnReady; later callback-switch writes control delivery.
    /// Runs on the selected processing group: the scene owner for a main group or a joined worker for a subthread group.
    /// ProcessDeltaTime is updated before delivery.
    /// Callback failures aggregate after remaining eligible deliveries and frame work. Input, rendering and physical
    /// authoring retain their own access guards.</remarks>
    protected virtual void OnProcess(double delta)
    {
    }

    /// <summary>Called during an eligible host-driven physics-process frame.</summary>
    /// <param name="delta">The finite non-negative physics-step delta in seconds.</param>
    /// <remarks>Ready enables an actual override before OnReady; later callback-switch writes control delivery.
    /// Runs on the selected processing group before the tree-owned numerical step, independently of that solver thread.
    /// PhysicsProcessDeltaTime is updated before delivery; the callback itself does not integrate the world.
    /// Callback failures aggregate after remaining eligible deliveries and frame work. Input, rendering and physical
    /// authoring retain their own access guards.</remarks>
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

    /// <summary>Receives a keyboard, gamepad-button or shortcut event left unhandled by GUI controls.</summary>
    /// <param name="event">The live borrowed event; do not retain or dispose it.</param>
    /// <remarks>Enable <see cref="ShortcutInputEnabled"/> to participate. Delivery is pause-aware and occurs on the scene
    /// owner thread before unhandled-key and general unhandled input. Marking input handled stops later delivery.</remarks>
    protected virtual void OnShortcutInput(InputEvent @event)
    {
    }

    /// <summary>Receives a keyboard event that remains unhandled after GUI and shortcut processing.</summary>
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

    private void EnableOverriddenCallbacks()
    {
        var type = GetType();
        if (!CallbackMasks.TryGetValue(type, out var mask))
        {
            lock (CallbackMasks)
            {
                if (!CallbackMasks.TryGetValue(type, out mask))
                {
                    byte bits = 0;
                    if (((Action<double>)OnProcess).Method.DeclaringType != typeof(Node)) bits |= 1;
                    if (((Action<double>)OnPhysicsProcess).Method.DeclaringType != typeof(Node)) bits |= 2;
                    if (((Action<InputEvent>)OnInput).Method.DeclaringType != typeof(Node)) bits |= 4;
                    if (((Action<InputEvent>)OnShortcutInput).Method.DeclaringType != typeof(Node)) bits |= 8;
                    if (((Action<InputEventKey>)OnUnhandledKeyInput).Method.DeclaringType != typeof(Node)) bits |= 16;
                    if (((Action<InputEvent>)OnUnhandledInput).Method.DeclaringType != typeof(Node)) bits |= 32;
                    mask = new CallbackMask(bits); CallbackMasks.Add(type, mask);
                }
            }
        }
        if ((mask.Bits & 1) != 0) ProcessEnabled = true;
        if ((mask.Bits & 2) != 0) PhysicsProcessEnabled = true;
        if ((mask.Bits & 4) != 0) InputEnabled = true;
        if ((mask.Bits & 8) != 0) ShortcutInputEnabled = true;
        if ((mask.Bits & 16) != 0) UnhandledKeyInputEnabled = true;
        if ((mask.Bits & 32) != 0) UnhandledInputEnabled = true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Calls the base implementation, then maps ready, process and physics-process notifications to typed callbacks.
    /// Pause and application-suspend notifications reset eligible physics presentation history. Enter/exit callbacks
    /// belong to SceneTree-managed transitions. Manual notification does not mutate membership, ready state or deltas.
    /// </remarks>
    protected override void OnNotification(int what)
    {
        var reversed = IsNotificationReversed;

        if (!reversed) base.OnNotification(what);

        switch (what)
        {
            case NotificationReady:
                EnableOverriddenCallbacks();
                OnReady();
                break;
            case NotificationProcess:
                OnProcess(ProcessDeltaTime);
                break;
            case NotificationPhysicsProcess:
                OnPhysicsProcess(PhysicsProcessDeltaTime);
                break;
            case NotificationPaused:
            case NotificationApplicationPaused:
                if (IsPhysicsInterpolatedAndEnabled()) ResetPhysicsInterpolation();
                break;
        }

        if (reversed) base.OnNotification(what);
    }

    /// <inheritdoc />
    protected override void ValidateMutation()
    {
        base.ValidateMutation();
        RenderingServer.ValidateParticleSceneMutation();
        EnsureNotSceneCapture();
    }

    /// <inheritdoc />
    /// <remarks>Appends this class's typed hierarchy, ownership, processing, and automatic translation descriptors to the inherited descriptors.</remarks>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(SceneNodeProperties).Concat(ProcessingGroupProperties);

    /// <inheritdoc />
    /// <remarks>Rejects disposal during tree lifecycle or notification delivery, or of an active tree root, and requires the owner thread for an attached node.</remarks>
    /// <exception cref="InvalidOperationException">This node is in lifecycle or notification delivery, its parent is exiting or propagating a notification, it is an active tree root, or disposal is attempted off the owner thread.</exception>
    protected override void ValidateDisposal()
    {
        RenderingServer.ValidateParticleSceneMutation();
        EnsurePhysicsObjectAccess();
        if (_notificationPropagationDepth != 0 || (Parent?._notificationPropagationDepth ?? 0) != 0)
            throw new InvalidOperationException("A node cannot be disposed during subtree propagation through its parent.");

        if (Volatile.Read(ref _sceneInstantiationDepth) != 0)
            throw new InvalidOperationException("A node cannot be disposed while packed-scene instantiation is active.");

        if (Volatile.Read(ref _sceneCaptureDepth) != 0)
            throw new InvalidOperationException("A node cannot be disposed while a packed-scene capture is active.");

        if (_isEnteringTree || _isMakingReady || _isExitingTree || _hasPendingTreeExit)
            throw new InvalidOperationException("A node cannot be disposed from its in-progress tree lifecycle callbacks.");

        if (Parent?._isExitingTree == true)
            throw new InvalidOperationException("A node cannot be disposed while its parent is exiting a SceneTree.");

        if (Tree?.IsRoot(this) == true)
            throw new InvalidOperationException("An active SceneTree root can only be disposed by disposing its SceneTree.");

        Tree?.EnsureOwnerThread();
        ValidateNativeWindowRetirement();
        base.ValidateDisposal();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Closes queued deletion, detaches this node, attempts disposal of every owned child, clears groups and event
    /// subscribers, and then calls the base implementation. A surviving or failed child is structurally detached with
    /// a guarded unparent transition. Every teardown stage is attempted before failures are
    /// reported together.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        List<Exception>? errors = null;

        if (disposing)
        {
            try { DisposeSceneConnections(); } catch (Exception error) { (errors ??= []).Add(error); }
            ReleaseOwnedNodes();
            SetSceneOwner(null);
            Interlocked.Or(ref _queuedForDeletion, 4);

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
                finally
                {
                    if (ReferenceEquals(child.Parent, this))
                    {
                        try { RemoveChildCore(child, disposingParent: true); }
                        catch (Exception error) { CollectException(ref errors, error); }
                    }
                }
            }

            _children.Clear();
            _internalFrontCount = _internalBackCount = 0;
            _groups.Clear();
            SetSceneOwner(null);

            if (_sharedSceneResourceOwnership is not null)
            {
                try { _sharedSceneResourceOwnership.ReleaseOwner(); } catch (Exception error) { CollectException(ref errors, error); }
                _sharedSceneResourceOwnership = null; _ownedSceneResources = null;
            }
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
            SceneInstancePaths = null; SceneInstanceConnections = null; SpawnSceneIdentity = null;
            _rpcMethods = null; _removedRPCMethods = null;
            try { AdoptSceneFileLease(null); } catch (Exception error) { CollectException(ref errors, error); }

            _publisherChildAdded.Handlers = null;
            _publisherChildRemoved.Handlers = null;
            _publisherChildEnteredTree.Handlers = null;
            _publisherChildExitingTree.Handlers = null;
            _publisherChildOrderChanged.Handlers = null;
            _publisherRenamed.Handlers = null;
            _publisherTreeEntered.Handlers = null;
            _publisherTreeExiting.Handlers = null;
            _physicsObjectBindings?.Clear(); _physicsObjectBindings = null;
            _publisherTreeExited.Handlers = null;
            _publisherReady.Handlers = null;
            _publisherReplacingBy.Handlers = null;
            _publisherEditorDescriptionChanged.Handlers = null;
            _publisherEditorStateChanged.Handlers = null;
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

    internal bool TryConsumeQueuedDeletion(long ticket) =>
        Interlocked.CompareExchange(ref _queuedForDeletion, ticket | 1, ticket) == ticket;

    internal void ReleaseClaimedQueuedDeletion(long ticket) =>
        Interlocked.CompareExchange(ref _queuedForDeletion, ticket & ~7L, ticket | 1);

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

    private List<IDisposable>? _sceneFileLeases;
    internal void AdoptSceneFileLease(IDisposable? lease)
    {
        if (lease is not null) { (_sceneFileLeases ??= []).Add(lease); return; }
        var old = _sceneFileLeases; _sceneFileLeases = null; if (old is null) return;
        List<Exception>? errors = null; foreach (var item in old) try { item.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("File resource graph cleanup failed.", errors);
    }
    internal Resource[] ReleaseSceneResourcesForSave()
    {
        EnsureMutable(); var resources = new List<Resource>(); var pending = new Stack<Node>(); pending.Push(this);
        while (pending.TryPop(out var node))
        {
            node.EnsureMutable();
            if (node._sharedSceneResourceOwnership is { } shared) { shared.TransferResources(); shared.ReleaseOwner(); node._sharedSceneResourceOwnership = null; }
            if (node._ownedSceneResources is { } owned) resources.AddRange(owned); node._ownedSceneResources = null;
            for (var i = 0; i < node.GetChildCount(includeInternal: true); i++) pending.Push(node.GetChild(i, includeInternal: true));
        }
        return resources.ToArray();
    }
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
        if (owned.Count != 0) _sharedSceneResourceOwnership = new ResourceFileOwnership(owned.ToArray());
    }

    internal void SetSceneFilePath(string path)
    {
        EnsureMutable();
        EnsureEditorAuthoringCommit();
        ArgumentNullException.ThrowIfNull(path);
        _sceneFilePath = path;
        EmitEditorStateChanged();
    }

    internal void BeginSceneInstantiation()
    {
        ThrowIfDisposed();
        if (Interlocked.CompareExchange(ref _sceneInstantiationDepth, 1, 0) != 0)
            throw new InvalidOperationException("Packed-scene instantiation is already active for this node.");
    }

    internal virtual void ValidateSceneInstance() { }

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
        var node = InvokeConstructionFactory(factory, out var created, scene: true);
        if (node is not null && !created) throw new InvalidOperationException("A scene factory returned a borrowed node.");
        return node!;
    }

    internal static void EnsureSceneFactoryComplete()
    {
        if (IsConstructingScene)
            throw new InvalidOperationException("A node factory cannot activate a SceneTree before returning its node.");
    }

    private long GetQueuedDeletionTicket()
    {
        var state = Volatile.Read(ref _queuedForDeletion);
        return (state & 4) == 0 && (state & 3) is 1 or 2 ? (state & ~7L) | 2 : 0;
    }

    internal bool TryPublishQueuedDeletion(long ticket) =>
        Volatile.Read(ref _queuedForDeletion) == ticket ||
        Interlocked.CompareExchange(ref _queuedForDeletion, ticket, ticket - 1) == ticket - 1;

    internal IEnumerable<Node> EnumerateDepthFirst()
    {
        yield return this;

        foreach (var child in _children.ToArray())
        {
            foreach (var descendant in child.EnumerateDepthFirst())
                yield return descendant;
        }
    }

    internal void AppendChildrenReversed(List<Node> nodes)
    {
        for (var index = _children.Count - 1; index >= 0; index--) nodes.Add(_children[index]);
    }

    internal void NotifyTreePause(bool paused)
    {
        var previous = CanProcessWhenPaused(!paused);
        var current = CanProcessWhenPaused(paused);
        if (previous != current)
        {
            _processPauseRevision++;
            DispatchNotification(current ? NotificationUnpaused : NotificationPaused);
        }
    }

    private bool CanProcessWhenPaused(bool paused) => ResolveProcessMode() switch
    {
        ProcessMode.Pausable => !paused,
        ProcessMode.WhenPaused => paused,
        ProcessMode.Always => true,
        ProcessMode.Disabled => false,
        _ => throw new InvalidOperationException("Unknown process mode.")
    };

    internal bool HasProcessCallback(bool physics) => physics
        ? _physicsProcessEnabled || _internalPhysicsProcessEnabled
        : _processEnabled || _internalProcessEnabled;

    internal void SetInternalProcessing(bool processEnabled, bool physicsProcessEnabled)
    {
        if (IsDisposed && _isExitingTree && !processEnabled && !physicsProcessEnabled)
        {
            Tree?.EnsureOwnerThread();
            _internalProcessEnabled = _internalPhysicsProcessEnabled = false;
            return;
        }
        EnsureProcessingAccess();
        if (_internalProcessEnabled == processEnabled && _internalPhysicsProcessEnabled == physicsProcessEnabled) return;
        _internalProcessEnabled = processEnabled;
        _internalPhysicsProcessEnabled = physicsProcessEnabled;
        Tree?.InvalidateProcessOrder();
    }

    internal double GetUnscaledProcessDelta(bool physics) =>
        physics ? _unscaledPhysicsProcessDeltaTime : _unscaledProcessDeltaTime;

    internal void RunProcess(double unscaledDelta, bool physics)
    {
        if (physics)
        {
            _unscaledPhysicsProcessDeltaTime = unscaledDelta;
        }
        else
        {
            _unscaledProcessDeltaTime = unscaledDelta;
        }

        var internalEnabled = physics ? _internalPhysicsProcessEnabled : _internalProcessEnabled;
        var expectedTree = Tree;
        var expectedAttachment = TreeAttachmentVersion;
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

        if (!IsDisposed && ReferenceEquals(Tree, expectedTree) && TreeAttachmentVersion == expectedAttachment &&
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
        EnsurePhysicsObjectAccess();
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

    // Layer-specific membership work is separate from manually dispatched notifications.
    internal virtual void OnTreeMembershipChanged(bool entering) { }

    private void EnterTreeCore(SceneTree tree)
    {

        Tree = tree;
        if (Parent is null && _processMode == ProcessMode.Inherit) _processMode = ProcessMode.Pausable;
        _activeViewport = this as Viewport ?? Parent?._activeViewport;
        _absolutePathCache = null;
        TreeAttachmentVersion++;
        tree.BindProcessingGroup(this);
        List<Exception>? errors = null;
        try { OnTreeMembershipChanged(entering: true); }
        catch (Exception error) { CollectException(ref errors, error); }

        try
        {
            DispatchNotification(NotificationEnterTree);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try { OnEnterTree(); }
        catch (Exception error) { CollectException(ref errors, error); }

        if (CanAutoTranslate())
        {
            try { DispatchNotification(NotificationTranslationChanged); }
            catch (Exception error) { CollectException(ref errors, error); }
        }

        try
        {
            BeginEventEmission(_publisherTreeEntered.Handlers)?.Invoke(this);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try { NotifyPhysicsObjectBindings(entering: true); }
        catch (Exception error) { CollectException(ref errors, error); }

        try { tree.NotifyNodeAdded(this); }
        catch (Exception error) { CollectException(ref errors, error); }

        var parent = Parent;
        try { parent?.BeginEventEmission(parent._publisherChildEnteredTree.Handlers)?.Invoke(parent, this); }
        catch (Exception error) { CollectException(ref errors, error); }

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

        var deletionTicket = GetQueuedDeletionTicket();
        if (deletionTicket != 0)
        {
            try
            {
                tree.QueueForDeletion(this, deletionTicket);
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
        HasStartedReady = true;
        List<Exception>? errors = null;

        foreach (var (child, attachment) in _children.Select(static child => (child, child.TreeAttachmentVersion)).ToArray())
        {
            if (IsDisposed || !ReferenceEquals(Tree, expectedTree))
                break;

            if (!ReferenceEquals(child.Parent, this) || !ReferenceEquals(child.Tree, expectedTree) || child.TreeAttachmentVersion != attachment)
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

        try { DispatchNotification(NotificationPostEnterTree); }
        catch (Exception error) { CollectException(ref errors, error); }

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
                BeginEventEmission(_publisherReady.Handlers)?.Invoke(this);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        ThrowCollected("One or more ready callbacks failed.", errors);
    }

    internal void ExitTree(SceneTree tree, bool deferExited = false)
    {
        EnsurePhysicsObjectAccess();
        if (_isEnteringTree)
            throw new InvalidOperationException($"Node '{Name}' cannot exit a SceneTree while it is still entering.");
        if (!ReferenceEquals(Tree, tree) || _isExitingTree) return;

        _isExitingTree = true;
        _hasPendingTreeExit = true;
        List<Exception>? errors = null;
        try
        {
            var children = _children.ToArray();
            for (var i = children.Length - 1; i >= 0; i--)
                try { children[i].ExitTree(tree, deferExited: true); }
                catch (Exception error) { CollectException(ref errors, error); }

            try { OnExitTree(); }
            catch (Exception error) { CollectException(ref errors, error); }
            try { BeginEventEmission(_publisherTreeExiting.Handlers)?.Invoke(this); }
            catch (Exception error) { CollectException(ref errors, error); }
            try { DispatchNotification(NotificationExitTree); }
            catch (Exception error) { CollectException(ref errors, error); }
            try { OnTreeMembershipChanged(entering: false); }
            catch (Exception error) { CollectException(ref errors, error); }
            try { NotifyPhysicsObjectBindings(entering: false); }
            catch (Exception error) { CollectException(ref errors, error); }
            try { tree.NotifyNodeRemoved(this); }
            catch (Exception error) { CollectException(ref errors, error); }
            var parent = Parent;
            try { parent?.BeginEventEmission(parent._publisherChildExitingTree.Handlers)?.Invoke(parent, this); }
            catch (Exception error) { CollectException(ref errors, error); }

            tree.UnbindProcessingGroup(this);
            _activeViewport = null;
            Tree = null;
            HasStartedReady = false;
            _absolutePathCache = null;
            if (!deferExited)
                try { CompleteExitTree(); }
                catch (Exception error) { CollectException(ref errors, error); }
        }
        finally
        {
            _isExitingTree = false;
            HasStartedReady = false;
            if (ReferenceEquals(Tree, tree))
            {
                tree.UnbindProcessingGroup(this);
                _activeViewport = null;
                Tree = null;
                _absolutePathCache = null;
            }
        }
        ThrowCollected("One or more exit-tree callbacks failed.", errors);
    }

    internal void CompleteExitTree()
    {
        if (!_hasPendingTreeExit) return;
        var wasExiting = _isExitingTree;
        _isExitingTree = true;
        List<Exception>? errors = null;
        try
        {
            var children = _children.ToArray();
            for (var i = children.Length - 1; i >= 0; i--)
                try { children[i].CompleteExitTree(); }
                catch (Exception error) { CollectException(ref errors, error); }
            try { BeginEventEmission(_publisherTreeExited.Handlers)?.Invoke(this); }
            catch (Exception error) { CollectException(ref errors, error); }
        }
        finally { _hasPendingTreeExit = false; _isExitingTree = wasExiting; }
        ThrowCollected("One or more after-exit callbacks failed.", errors);
    }

    internal void ResetReadyAfterFailedActivation() => _readyCalled = false;

    internal void DispatchInput(InputEvent @event) => OnInput(@event);

    internal void DispatchShortcutInput(InputEvent @event) => OnShortcutInput(@event);

    internal void DispatchUnhandledKeyInput(InputEventKey @event) => OnUnhandledKeyInput(@event);

    internal void DispatchUnhandledInput(InputEvent @event) => OnUnhandledInput(@event);

    internal static bool IsValidStoredNodeName(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.AsSpan().IndexOfAny(".:/\"%") < 0;

    private static Node CreateDefaultSceneNode() => new();

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

    private static void CaptureProcessStates(Node node, List<(Node Node, bool CanProcess, bool Disabled, ulong PauseRevision, ulong EnableRevision, ulong Attachment)> states, bool paused)
    {
        states.Add((node, node.CanProcessWhenPaused(paused), node.ResolveProcessMode() == ProcessMode.Disabled,
            node._processPauseRevision, node._processEnableRevision, node.TreeAttachmentVersion));
        for (var index = 0; index < node._children.Count; index++)
            if (node._children[index]._processMode == ProcessMode.Inherit) CaptureProcessStates(node._children[index], states, paused);
    }

    /// <summary>Validates that this node may be mutated at the current lifecycle point.</summary>
    /// <remarks>Derived node property setters should call this before changing state that can be stored in a packed scene.
    /// Existing disposal/capture/thread guards run before the virtual ValidateMutation hook. Successful validation conservatively
    /// invalidates active rendering, including custom setters whose resulting value is equal. The inherited hook also
    /// guards affected scene colliders against owner-thread and numerical-borrow violations, including detached attachments.</remarks>
    /// <exception cref="InvalidOperationException">Scene capture is active or an attached node is accessed off the tree owner thread.</exception>
    /// <exception cref="ObjectDisposedException">Disposal has started.</exception>
    protected void EnsureMutable()
    {
        ValidateNodeMutation();
        RenderingServer.NotifyRenderMutation();
    }

    internal void EnsureTransformMutable()
    {
        ThrowIfDisposed(); EnsureNotSceneCapture(); Tree?.EnsureProcessingAccess(this);
        EnsurePhysicsAuthoringAccess(sceneTransform: true); ValidateMutation();
        RenderingServer.NotifyRenderMutation();
    }

    internal void ValidateNodeMutation()
    {
        ThrowIfDisposed();

        EnsureNotSceneCapture();

        Tree?.EnsureOwnerThread();
        EnsurePhysicsAuthoringAccess();
        ValidateMutation();
    }

    private void EnsureNotSceneCapture()
    {
        ThrowIfDisposed();

        if (IsDisposed)
            throw new ObjectDisposedException(GetType().FullName);

        if (Volatile.Read(ref _sceneCaptureDepth) != 0)
            throw new InvalidOperationException("A node cannot be mutated while a packed-scene capture is active.");
    }

    private void PropagatePathRenamed()
    {
        List<Exception>? errors = null;
        try { DispatchNotification(NotificationPathRenamed); }
        catch (Exception error) { CollectException(ref errors, error); }
        if (_children.Count != 0)
        {
            var snapshots = _pathRenameSnapshots ??= [];
            var depth = _pathRenameDepth++;
            if (depth == snapshots.Count) snapshots.Add([]);
            var children = snapshots[depth];
            try
            {
                children.AddRange(_children);
                foreach (var child in children)
                {
                    try { child.PropagatePathRenamed(); }
                    catch (Exception error) { CollectException(ref errors, error); }
                }
            }
            finally { children.Clear(); _pathRenameDepth--; }
        }
        ThrowCollected("One or more path-renamed callbacks failed.", errors);
    }

    private void InsertChild(Node child, int index, InternalMode internalMode = InternalMode.Disabled, bool forceReadableName = false)
    {
        EnsureMutable();
        EnsureChildOrderMutable();
        ValidateChildForInsertion(child, allowExistingParent: false);

        if ((uint)index > (uint)_children.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Insertion index is outside the valid range.");

        child._name = ResolveChildName(child, child._name, forceReadableName);
        var tree = Tree;
        _children.Insert(index, child);
        ChangeInternalChildCount(internalMode, 1);
        child._internalMode = internalMode;
        child.Parent = this;
        child.InvalidatePathCache();
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
            NotifyChildOrderChanged();
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            BeginEventEmission(_publisherChildAdded.Handlers)?.Invoke(this, child);
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

    private bool EnumerateAncestorsForEmbedding()
    { for (Node? node = this; node is not null; node = node.Parent) if (node is Viewport { GUIEmbedSubwindows: true }) return true; return false; }

    private void ValidateChildForInsertion(Node child, bool allowExistingParent)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(child);
        child.EnsureNotSceneCapture();
        child.EnsurePhysicsObjectAccess();
        if (child._isEnteringTree || child._isMakingReady || child._isExitingTree || child._hasPendingTreeExit)
            throw new InvalidOperationException("A node cannot be inserted while its tree lifecycle is in progress.");
        if (child is Window { Visible: true } window && Tree is not null && (window.ForceNative || !EnumerateAncestorsForEmbedding()))
        {
            if (Tree.Root is not Window { NativeDisplay: not null } || RenderingServer.Service is not { } renderer)
                throw new NotSupportedException("Visible independent windows require an active native scene host; otherwise configure an embedding viewport.");
            renderer.EnsureNativeWindowMutation();
        }
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

    }

    private void ValidateNativeWindowRetirement()
    {
        var display = DisplayServer.Service; var renderer = RenderingServer.Service;
        if (display?.HasPendingNativeDialogs != true && renderer?.NativeWindowMutationBlocked != true) return;
        foreach (var node in EnumerateDepthFirst())
            if (node is Window { NativeDisplay: { } context })
            { renderer?.EnsureNativeWindowMutation(); context.ValidateNativeWindowRetirement(); }
    }

    private bool RemoveChildCore(Node child, bool disposingParent = false)
    {
        EnsureChildOrderMutable();
        if (!ReferenceEquals(child.Parent, this))
            return false;

        if (_isExitingTree)
            throw new InvalidOperationException("A child cannot be removed while its parent is exiting a SceneTree.");

        if (child._isEnteringTree || child._isMakingReady || child._isExitingTree)
            throw new InvalidOperationException("A node cannot be structurally removed while its tree lifecycle callbacks are running.");

        child.ValidateNativeWindowRetirement();
        var tree = Tree;
        tree?.BeginNodeLifecycle();
        try
        {
            List<Exception>? errors = null;

            try
            {
                tree?.DetachSubtree(child);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }

            child._isExitingTree = tree is not null || disposingParent;
            try
            {
                _children.Remove(child);
                ChangeInternalChildCount(child._internalMode, -1);
                child.Parent = null;
                child.InvalidatePathCache();

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
                    NotifyChildOrderChanged();
                }
                catch (Exception error)
                {
                    CollectException(ref errors, error);
                }

                try
                {
                    BeginEventEmission(_publisherChildRemoved.Handlers)?.Invoke(this, child);
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

                if (tree is not null)
                {
                    child.ClearInvalidOwnersRecursive();
                    try { tree.CompleteSubtreeExit(child); }
                    catch (Exception error) { CollectException(ref errors, error); }
                }
            }
            finally { child._isExitingTree = false; }

            ThrowCollected("One or more child-removal callbacks failed.", errors);
            return true;
        }
        finally { tree?.EndNodeLifecycle(); }
    }

    private void NotifyChildOrderChanged()
    {
        List<Exception>? errors = null;
        try { RenderingServer.Service?.PublishSceneDrawOrder(this); }
        catch (Exception error) { CollectException(ref errors, error); }
        try { DispatchNotification(NotificationChildOrderChanged); }
        catch (Exception error) { CollectException(ref errors, error); }
        try { BeginEventEmission(_publisherChildOrderChanged.Handlers)?.Invoke(this); }
        catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("One or more child-order callbacks failed.", errors);
    }

    private void ClearInvalidOwnersRecursive()
    {
        foreach (var node in EnumerateDepthFirst())
        {
            if (node.SceneOwner is { } owner && !owner.IsAncestorOf(node))
                node.SetSceneOwner(null);
        }
    }

    private void ClaimUniqueName()
    {
        if (_uniqueNameInOwner && _ownerIdentity.RawTarget is Node owner && !owner.IsDisposalCommitted &&
            owner.FindUniqueOwned(_name, this, reserveClosed: true) is not null)
            _uniqueNameInOwner = false;
    }

    private Node? FindUniqueOwned(ReadOnlySpan<char> name, Node? except = null, bool reserveClosed = false)
    {
        if (_ownedNodes is not { } owned) return null;
        foreach (var identity in owned)
        {
            var target = reserveClosed ? identity.RawTarget : identity.Target;
            if (target is Node node && !node.IsDisposalCommitted && !ReferenceEquals(node, except) && node._uniqueNameInOwner &&
                ReferenceEquals(reserveClosed ? node._ownerIdentity.RawTarget : node.SceneOwner, this) && name.SequenceEqual(node._name)) return node;
        }
        return null;
    }

    private void SetSceneOwner(Node? owner)
    {
        var ownerIdentity = owner?.BorrowIdentity() ?? default;
        var identity = owner is null ? default : BorrowIdentity();
        var next = owner is null ? null : (owner._ownedNodes ??= []);
        if (next is not null)
        {
            for (var i = next.Count - 1; i >= 0; i--)
                if (next[i].IsRetired) next.RemoveAt(i);
            next.EnsureCapacity(next.Count + 1);
        }
        if (_ownerIdentity.RawTarget is Node previous && previous._ownedNodes is { } previousOwned)
            for (var i = previousOwned.Count - 1; i >= 0; i--)
                if (previousOwned[i].ID == InstanceID || previousOwned[i].IsRetired) previousOwned.RemoveAt(i);
        _ownerIdentity = ownerIdentity;
        next?.Add(identity);
    }

    private void ReleaseOwnedNodes()
    {
        if (_ownedNodes is not { } owned) return;
        foreach (var identity in owned)
            if (identity.RawTarget is Node node && node._ownerIdentity.ID == InstanceID) node._ownerIdentity = default;
        owned.Clear();
        _ownedNodes = null;
    }

    private void InvalidatePathCache()
    {
        _absolutePathCache = null;
        foreach (var child in _children) child.InvalidatePathCache();
    }

    private void FindChildrenCore<TNode>(string pattern, bool recursive, bool owned, List<TNode> result)
        where TNode : Node
    {
        ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this);
        foreach (var child in _children)
        {
            if (owned && child.SceneOwner is null) continue;
            if (child is TNode typedChild && (pattern.Length == 0 || MatchesNodeName(pattern, child._name)))
                result.Add(typedChild);

            if (recursive)
                child.FindChildrenCore(pattern, recursive: true, owned, result);
        }
    }

    private static bool MatchesNodeName(string pattern, string name)
    {
        if (pattern.Length == 0 || name.Length == 0) return false;
        var p = 0; var n = 0; var star = -1; var retry = 0;
        while (n < name.Length)
        {
            if (p < pattern.Length && pattern[p] == '*') { star = p++; retry = n; continue; }
            if (p < pattern.Length && (pattern[p] == '?' ? name[n] != '.' : pattern[p] == name[n]))
            {
                n += pattern[p] == '?' ? CharacterWidth(name, n) : 1; p++; continue;
            }
            if (star < 0) return false;
            retry += CharacterWidth(name, retry); n = retry; p = star + 1;
        }
        while (p < pattern.Length && pattern[p] == '*') p++;
        return p == pattern.Length;

        static int CharacterWidth(string text, int at) => char.IsHighSurrogate(text[at]) &&
            at + 1 < text.Length && char.IsLowSurrogate(text[at + 1]) ? 2 : 1;
    }

    private void AppendTreeString(StringBuilder result, string path)
    {
        result.Append(path).Append('\n');
        foreach (var child in _children)
            child.AppendTreeString(result, path == "." ? child.Name : path + "/" + child.Name);
    }

    private void AppendTreeStringPretty(StringBuilder result, string prefix, bool last)
    {
        result.Append(prefix).Append(last ? " ┖╴" : " ┠╴").Append(Name).Append('\n');
        var childPrefix = prefix + (last ? "   " : " ┃ ");
        for (var index = 0; index < _children.Count; index++)
            _children[index].AppendTreeStringPretty(result, childPrefix, index == _children.Count - 1);
    }

    private void EnsureChildOrderMutable()
    {
        for (Node? ancestor = this; ancestor is not null; ancestor = ancestor.Parent)
            if (ancestor._hasPendingTreeExit) throw new InvalidOperationException("Children cannot be changed until subtree exit delivery completes.");
        EnsurePhysicsObjectAccess();
        Tree?.EnsureExtensionHierarchyChange();
        if (_notificationPropagationDepth != 0)
            throw new InvalidOperationException("Children cannot be changed during subtree propagation.");
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

    internal bool ProcessingDisabled => ResolveProcessMode() == ProcessMode.Disabled;

    private ProcessMode ResolveProcessMode()
    {
        if (_processMode != ProcessMode.Inherit)
            return _processMode;

        return Parent?.ResolveProcessMode() ?? ProcessMode.Pausable;
    }

    internal static void CollectException(ref List<Exception>? errors, Exception error)
    {
        errors ??= [];

        if (error is AggregateException aggregate)
            errors.AddRange(aggregate.Flatten().InnerExceptions);
        else
            errors.Add(error);
    }

    internal static void ThrowCollected(string message, List<Exception>? errors)
    {
        if (errors is not null)
            throw new AggregateException(message, errors);
    }

}
