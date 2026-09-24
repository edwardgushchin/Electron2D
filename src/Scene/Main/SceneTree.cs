using System.Collections.Concurrent;

namespace Electron2D;

/// <summary>Owns one active node hierarchy and coordinates its lifecycle, input, frames, groups, timers, tweens, and deferred work.</summary>
/// <remarks>
/// The creating thread becomes the owner thread for scene mutation, frame execution, flushing, and disposal.
/// Canvas transform notifications coalesce in owner-thread queues. Idle frames deliver after the frame event,
/// after node processing and after timers, tweens and deferred actions; physics frames deliver before the frame
/// event and after those updates. Delivery precedes queued deletion and follows pending-list order;
/// callback failures and cancellation do not skip other pending items.
/// Electron2D does not create a frame-pump thread. A host can drive the loop through <see cref="Engine.AdvanceFrame"/>,
/// call <see cref="MainLoop.Process"/> and <see cref="MainLoop.PhysicsProcess"/> directly, or use this class's wrappers.
/// </remarks>
public sealed partial class SceneTree : MainLoop
{
    private const GroupCallFlags SupportedGroupCallFlags =
        GroupCallFlags.Reverse | GroupCallFlags.Deferred | GroupCallFlags.Unique;

    private static readonly IReadOnlyList<PropertyDescriptor> SceneTreeProperties = Array.AsReadOnly<PropertyDescriptor>(
    [
        new PropertyDescriptor<SceneTree, Node>(nameof(Root), tree => tree.Root),
        new PropertyDescriptor<SceneTree, Node?>(nameof(CurrentScene), tree => tree.CurrentScene, (tree, value) => tree.CurrentScene = value, _ => null),
        new PropertyDescriptor<SceneTree, Node?>(nameof(EditedSceneRoot), tree => tree.EditedSceneRoot, (tree, value) => tree.EditedSceneRoot = value, _ => null),
        new PropertyDescriptor<SceneTree, bool>(nameof(DebugPathsHint), tree => tree.DebugPathsHint, (tree, value) => tree.DebugPathsHint = value, _ => false),
        new PropertyDescriptor<SceneTree, bool>(nameof(AutoAcceptQuit), tree => tree.AutoAcceptQuit, (tree, value) => tree.AutoAcceptQuit = value, _ => true),
        new PropertyDescriptor<SceneTree, bool>(nameof(HasDeferredWork), tree => tree.HasDeferredWork),
        new PropertyDescriptor<SceneTree, ulong>(nameof(ProcessFrameCount), tree => tree.ProcessFrameCount),
        new PropertyDescriptor<SceneTree, ulong>(nameof(PhysicsFrameCount), tree => tree.PhysicsFrameCount),
        new PropertyDescriptor<SceneTree, int>(nameof(NodeCount), tree => tree.NodeCount),
        new PropertyDescriptor<SceneTree, bool>(nameof(Paused), tree => tree.Paused, (tree, value) => tree.Paused = value, _ => false),
        new PropertyDescriptor<SceneTree, bool>(nameof(PhysicsInterpolation), tree => tree.PhysicsInterpolation,
            (tree, value) => tree.PhysicsInterpolation = value, _ => false)
    ]);

    private readonly object _workGate = new();
    private readonly int _ownerThreadId = Environment.CurrentManagedThreadId;
    private readonly HashSet<GroupOperationKey> _uniqueGroupOperations = [];
    private readonly List<Node> _scheduleTraversal = [];
    private readonly List<Node> _inputTraversal = [];
    private Control? _guiFocus;
    private Control? _guiMouseCapture;
    private bool _guiFocusHidden;
    private readonly List<ScheduledNode> _scheduledNodes = [];
    private readonly List<SceneTreeTimer> _timerSnapshot = [];
    private readonly List<SceneTreeTimer> _timers = [];
    private readonly List<Tween> _tweenSnapshot = [];
    private readonly List<Tween> _tweens = [];
    private readonly LinkedList<CanvasItem> _transformChanges = new();
    private LinkedListNode<CanvasItem>? _nextTransformNotification;
    private ConcurrentQueue<Action> _deferred = new();
    private ConcurrentQueue<DeletionRequest> _deletions = new();
    private List<Node>? _activationReadied;
    private bool _acceptingWork = true;
    private bool _constructionComplete;
    private bool _isChangingPause;
    private bool _isDispatchingInput;
    private bool _inputHandled;
    private bool _paused;
    private Node? _editedSceneRoot;
    private bool _debugPathsHint;
    internal readonly Color DebugPathsColor = ProjectSettings.Instance.GetWithOverride(ProjectSettings.DebugPathsColor);
    private bool _autoAcceptQuit = true;
    private int _quitRequested;
    private int _exitCode;
    private int _activeExecution;
    private int _lifecycleExecutionDepth;
    private ulong _processFrameCount;
    private ulong _physicsFrameCount;

    /// <summary>Creates and immediately activates a scene tree rooted at <paramref name="root"/>.</summary>
    /// <param name="root">A live, detached, parentless node that is not queued for deletion.</param>
    /// <remarks>
    /// Construction enters the hierarchy parent-first, dispatches post-enter notifications after each node's
    /// descendants, and then delivers ready child-first. Lifecycle callbacks and event handlers run synchronously.
    /// If activation fails, the tree first stops accepting work, every attached node is exited, ready state consumed
    /// by this attempt is restored, created timers are disposed, created tweens are invalidated, queued work is discarded,
    /// and the supplied hierarchy remains owned by the caller. A reference captured from an activation callback observes
    /// a terminal disposed tree. A root whose automatic translation mode is inherited samples
    /// <see cref="ProjectSettings.RootNodeAutoTranslate"/> before entry.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="root"/> has a parent, belongs to a tree, or is queued for deletion.</exception>
    /// <exception cref="InvalidOperationException">Construction is attempted from a scene factory, the root is being captured or instantiated, or an inactive Window is supplied.</exception>
    /// <exception cref="ObjectDisposedException">Disposal of <paramref name="root"/> has started.</exception>
    /// <exception cref="AggregateException">Activation or rollback callbacks fail.</exception>
    public SceneTree(Node root) : this(root, attachToEngine: false) { }

    internal SceneTree(Node root, bool attachToEngine)
    {
        ArgumentNullException.ThrowIfNull(root);
        Node.EnsureSceneFactoryComplete();
        ObjectDisposedException.ThrowIf(root.IsDisposed, root);
        root.EnsureSceneActivationAvailable();
        if (root is Window window)
            window.EnsureNativeOpen();

        if (root.Parent is not null)
            throw new ArgumentException("A SceneTree root cannot have a parent.", nameof(root));

        if (root.Tree is not null)
            throw new ArgumentException("A SceneTree root cannot already belong to a SceneTree.", nameof(root));

        if (root.IsQueuedForDeletion)
            throw new ArgumentException("A SceneTree root cannot be queued for deletion.", nameof(root));

        Root = root;
        var readied = new List<Node>();
        _activationReadied = readied;
        _activeExecution = 1;

        try
        {
            root.InitializeRootAutoTranslateMode(ProjectSettings.Instance.GetWithOverride(ProjectSettings.RootNodeAutoTranslate));
            _physicsInterpolation = ProjectSettings.Instance.GetWithOverride(ProjectSettings.PhysicsInterpolation);
            if (attachToEngine)
                Engine.Instance.AttachConstructingTree(this);
            Initialize();
            root.EnterTree(this);
            root.MakeReady(readied);
            _constructionComplete = true;
        }
        catch (Exception activationError)
        {
            var errors = new List<Exception>();
            CollectException(errors, activationError);

            lock (_workGate)
            {
                _acceptingWork = false;
                ClearPendingWorkUnderLock();
            }

            CompleteFailedLoopConstruction();

            try
            {
                root.ExitTree(this);
            }
            catch (Exception rollbackError)
            {
                CollectException(errors, rollbackError);
            }

            foreach (var node in readied)
                node.ResetReadyAfterFailedActivation();

            foreach (var timer in _timers.ToArray())
            {
                try
                {
                    timer.Dispose();
                }
                catch (Exception timerError)
                {
                    CollectException(errors, timerError);
                }
            }

            _timers.Clear();
            foreach (var tween in _tweens.ToArray())
            {
                try
                {
                    tween.InvalidateFromTree();
                }
                catch (Exception tweenError)
                {
                    CollectException(errors, tweenError);
                }
            }
            _tweens.Clear();
            ClearPendingWork();
            ClearEventSubscribers();
            throw new AggregateException("SceneTree activation failed and was rolled back.", errors);
        }
        finally
        {
            _activationReadied = null;
            _activeExecution = 0;
        }
    }

    /// <summary>Gets the root node owned by this tree.</summary>
    /// <value>The immutable root reference. Tree finalization exits and recursively disposes this hierarchy.</value>
    public Node Root { get; }

    /// <summary>Gets an advisory snapshot indicating whether deferred actions or deletions are queued.</summary>
    /// <value><see langword="true"/> when either concurrent queue is currently nonempty.</value>
    /// <remarks>This property is not a synchronization barrier and may change immediately after it is read.</remarks>
    internal bool HasDeferredWork => !_deferred.IsEmpty || !_deletions.IsEmpty;

    /// <summary>Gets the number of completed process-frame attempts.</summary>
    /// <value>The number of valid calls to <see cref="ProcessFrame"/>, including calls that reported callback failures.</value>
    /// <exception cref="ObjectDisposedException">The tree is disposing on another thread or has finished disposing.</exception>
    internal ulong ProcessFrameCount
    {
        get
        {
            ThrowIfDisposed();
            return _processFrameCount;
        }
    }

    /// <summary>Gets the number of completed physics-frame attempts.</summary>
    /// <value>The number of valid calls to <see cref="PhysicsFrame"/>, including calls that reported callback failures.</value>
    /// <exception cref="ObjectDisposedException">The tree is disposing on another thread or has finished disposing.</exception>
    internal ulong PhysicsFrameCount
    {
        get
        {
            ThrowIfDisposed();
            return _physicsFrameCount;
        }
    }

    /// <summary>Gets the number of nodes currently inside this tree.</summary>
    /// <value>The current hierarchy size, including <see cref="Root"/>, or zero after finalization releases the hierarchy.</value>
    /// <exception cref="InvalidOperationException">The property is read from a thread other than the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree is disposing on another thread or has finished disposing.</exception>
    public int NodeCount
    {
        get
        {
            ThrowIfDisposed();
            EnsureOwnerThread();
            return Root.IsDisposed ? 0 : Root.EnumerateDepthFirst().Count();
        }
    }

    /// <summary>Gets or sets whether pause-aware processing and timers are paused.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>
    /// On an actual change, the new state is stored first and the hierarchy is synchronously traversed depth-first.
    /// Each node's child snapshot is taken only after that node's notification returns, so re-entrant hierarchy changes
    /// can affect the remainder of the same traversal. A live attached node is notified at most once even when it is
    /// reparented, and removed or disposed candidates are skipped. Notification failures do not roll the state back and
    /// are collected after traversal completes.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The setter is used after finalization, or tree disposal has started or finished.</exception>
    /// <exception cref="InvalidOperationException">The setter is called from another thread or a pause callback requests the opposite state.</exception>
    /// <exception cref="AggregateException">One or more node notification handlers fail.</exception>
    public bool Paused
    {
        get
        {
            ThrowIfDisposed();
            return _paused;
        }
        set
        {
            ThrowIfDisposed();
            EnsureOwnerThread();
            EnsureAcceptingWork();

            if (_paused == value)
                return;

            if (_isChangingPause)
                throw new InvalidOperationException("The pause state cannot be changed re-entrantly during pause notification delivery.");

            List<Exception>? errors = null;
            _isChangingPause = true;

            try
            {
                _paused = value;
                var notified = new HashSet<Node>();

                foreach (var node in Root.EnumerateDepthFirst())
                {
                    if (!notified.Add(node) || node.IsDisposed || !ReferenceEquals(node.Tree, this))
                        continue;

                    try
                    {
                        node.DispatchNotification(value ? Node.NotificationPaused : Node.NotificationUnpaused);
                    }
                    catch (Exception error)
                    {
                        CollectException(ref errors, error);
                    }
                }
            }
            finally
            {
                _isChangingPause = false;
            }

            ThrowCollected("One or more pause-state notifications failed.", errors);
        }
    }

    /// <summary>Gets or selects the root of the scene whose configuration warnings are being inspected.</summary>
    /// <value>Null initially. A nonnull value must be a live node in this tree; this tree's Root is allowed.</value>
    /// <remarks>This is a borrowed tooling selection, independent of packed-scene ownership. It enables warning
    /// change events only for the selected subtree, without enabling an editor or changing processing. Exiting the
    /// tree clears the selection before NodeRemoved. Selection itself emits no warning-change event.</remarks>
    /// <exception cref="ArgumentException">The selected node belongs to another tree or is detached.</exception>
    /// <exception cref="ObjectDisposedException">The tree or selected node is disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the scene owner thread.</exception>
    public Node? EditedSceneRoot
    {
        get { EnsureOwnerThread(); EnsureAcceptingWork(); return _editedSceneRoot; }
        set
        {
            EnsureOwnerThread(); EnsureAcceptingWork();
            if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
            if (value is not null && !ReferenceEquals(value.Tree, this)) throw new ArgumentException("The edited scene must belong to this tree.", nameof(value));
            _editedSceneRoot = value;
        }
    }

    /// <summary>Gets or sets whether paths draw their curves and tangent direction markers.</summary>
    /// <value>False initially.</value>
    /// <remarks>Changes invalidate all attached Path nodes, including hidden ones. Drawing uses the existing
    /// canvas pipeline, visibility and transforms. Color is sampled from ProjectSettings.DebugPathsColor at tree
    /// construction. This optional diagnostic works in all build configurations; it creates no editor.</remarks>
    /// <exception cref="ObjectDisposedException">The tree is finalized or disposed.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the scene owner thread.</exception>
    public bool DebugPathsHint
    {
        get { EnsureOwnerThread(); EnsureAcceptingWork(); return _debugPathsHint; }
        set
        {
            EnsureOwnerThread(); EnsureAcceptingWork();
            if (_debugPathsHint == value) return;
            _debugPathsHint = value;
            foreach (var node in Root.EnumerateDepthFirst())
                if (node is Path path) path.InvalidateCanvas();
        }
    }

    /// <summary>Occurs when a node in EditedSceneRoot's subtree requests a configuration-warning refresh.</summary>
    /// <remarks>Arguments are this tree and the requesting node. Delivery is synchronous on the owner thread,
    /// without automatic warning evaluation or deduplication. A throwing subscriber stops later subscribers.
    /// A consumer queries Node.GetConfigurationWarnings; no scene dock or editor UI is created.</remarks>
    public event Action<SceneTree, Node>? NodeConfigurationWarningChanged;

    /// <summary>Occurs after a node enters this tree.</summary>
    /// <remarks>
    /// The first argument is this tree and the second is the entering node. Delivery is synchronous after the node's
    /// own enter event and before descendant entry. A throwing subscriber stops later subscribers of this event
    /// invocation, but the failure is aggregated after remaining lifecycle work.
    /// </remarks>
    public event Action<SceneTree, Node>? NodeAdded;

    /// <summary>Occurs after a node exits this tree.</summary>
    /// <remarks>
    /// The node's <see cref="Node.Tree"/> is already <see langword="null"/> when handlers run. Delivery is child-first;
    /// a throwing subscriber stops later subscribers of this event invocation, but the failure is aggregated after
    /// remaining exit work.
    /// </remarks>
    public event Action<SceneTree, Node>? NodeRemoved;

    /// <summary>Occurs after an active node is renamed.</summary>
    /// <remarks>
    /// Path-change notifications and the node's own renamed event run first. <see cref="TreeChanged"/> is still
    /// attempted when this event invocation fails, and both failures are aggregated. A throwing subscriber prevents
    /// later subscribers of this event invocation from running.
    /// </remarks>
    public event Action<SceneTree, Node>? NodeRenamed;

    /// <summary>Occurs before the idle transform-delivery phase and eligible node process callbacks.</summary>
    /// <remarks>A throwing subscriber stops later subscribers of this invocation; node callbacks, timers, tweens, and the deferred safe point are still attempted.</remarks>
    public event Action<SceneTree>? ProcessFrameStarted;

    /// <summary>Occurs after pending transform delivery and before eligible node physics-process callbacks.</summary>
    /// <remarks>A throwing subscriber stops later subscribers of this invocation; node callbacks, timers, tweens, and the deferred safe point are still attempted.</remarks>
    public event Action<SceneTree>? PhysicsFrameStarted;

    /// <summary>Occurs after the active hierarchy is structurally changed or an active node is renamed.</summary>
    /// <remarks>Delivery is synchronous after the corresponding state change. A throwing subscriber prevents later subscribers of that invocation; the state change is not rolled back.</remarks>
    public event Action<SceneTree>? TreeChanged;

    /// <summary>Thread-safely queues an action for a future deferred flush while the tree remains live.</summary>
    /// <param name="action">The action to invoke.</param>
    /// <remarks>
    /// An action queued during a captured flush batch waits for a later flush. Queue acceptance is atomic with the
    /// start of tree finalization: a successful call is either captured by a future flush or intentionally discarded by
    /// later finalization; a call that loses that race throws and does not enqueue.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    public void Defer(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        EnqueueAction(action);
    }

    /// <summary>Thread-safely queues a typed setter invocation for a future deferred flush.</summary>
    /// <typeparam name="T">The value type accepted by the setter.</typeparam>
    /// <param name="setter">The setter to invoke.</param>
    /// <param name="value">The value captured for the deferred invocation.</param>
    /// <remarks>Has the same atomic lifetime and captured-batch behavior as <see cref="Defer"/>.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="setter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    public void SetDeferred<T>(Action<T> setter, T value)
    {
        ArgumentNullException.ThrowIfNull(setter);
        Defer(() => setter(value));
    }

    /// <summary>Creates a one-shot timer owned and processed by this tree.</summary>
    /// <param name="timeSeconds">The finite non-negative delay in seconds.</param>
    /// <param name="processAlways">Whether the timer advances while <see cref="Paused"/> is true.</param>
    /// <param name="processInPhysics">Whether the timer advances after physics callbacks instead of process callbacks.</param>
    /// <param name="ignoreTimeScale">Whether to use the original frame delta when Engine drives the tree.</param>
    /// <returns>The live timer. It is automatically disposed after timeout delivery or when this tree is finalized.</returns>
    /// <remarks>
    /// Timers are updated after node callbacks and before deferred work. A timer created during node callbacks can be
    /// included in that frame's timer phase; a timer created by another timer waits for the next matching frame. Time
    /// advances only from supplied frame deltas. When <see cref="Engine"/> drives the tree, timers normally use deltas
    /// scaled by <see cref="Engine.TimeScale"/>; <paramref name="ignoreTimeScale"/> selects the original lane delta,
    /// including when the time scale is zero. Direct callers supply the same delta for both modes. There is no internal clock. Keeping a
    /// managed reference does not keep an expired timer alive: timeout delivery is followed by deterministic disposal.
    /// A zero duration expires during the next matching frame, not during this method call.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeSeconds"/> is negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">The method is called from a thread other than the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    public SceneTreeTimer CreateTimer(double timeSeconds, bool processAlways = true, bool processInPhysics = false, bool ignoreTimeScale = false)
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        EnsureAcceptingWork();
        SceneTreeTimer.ValidateTime(timeSeconds, nameof(timeSeconds));

        var timer = new SceneTreeTimer(this, timeSeconds, processAlways, processInPhysics, ignoreTimeScale);
        _timers.Add(timer);
        return timer;
    }

    /// <summary>Creates a valid tween processed by this tree.</summary>
    /// <returns>A running empty tween that starts on the next matching frame after tweeners are appended.</returns>
    /// <remarks>
    /// The tween is not bound to a node. It is advanced after node callbacks and lightweight timers and before deferred
    /// work. A tween created during another tween's callback waits for the next matching frame.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The method is called from a thread other than the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    public Tween CreateTween()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        EnsureAcceptingWork();
        var tween = new Tween(this);
        _tweens.Add(tween);
        return tween;
    }

    /// <summary>Returns the tweens currently registered for processing.</summary>
    /// <returns>A read-only snapshot in creation order, including paused, stopped, just-finished, and killed tweens awaiting their next matching step.</returns>
    /// <exception cref="InvalidOperationException">The method is called from a thread other than the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    public IReadOnlyList<Tween> GetProcessedTweens()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        return Array.AsReadOnly(_tweens.Where(tween => !tween.IsDisposed).ToArray());
    }

    /// <summary>Runs one host-driven process frame, process timers, process tweens, and one deferred safe point.</summary>
    /// <param name="delta">Elapsed process time in seconds; it must be finite and non-negative.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">The method is called off the owner thread, re-entered, called before initialization or after finalization, or called during node lifecycle or pause delivery.</exception>
    /// <exception cref="ObjectDisposedException">Tree disposal has started or finished.</exception>
    /// <exception cref="AggregateException">One or more frame events, node callbacks, timers, tweens, or deferred operations fail.</exception>
    public void ProcessFrame(double delta) => _ = Process(delta);

    /// <summary>Runs one host-driven physics-process frame, physics timers, physics tweens, and one deferred safe point.</summary>
    /// <param name="delta">Elapsed physics-step time in seconds; it must be finite and non-negative.</param>
    /// <remarks>This callback lane does not perform collision or rigid-body simulation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">The method is called off the owner thread, re-entered, called before initialization or after finalization, or called during node lifecycle or pause delivery.</exception>
    /// <exception cref="ObjectDisposedException">Tree disposal has started or finished.</exception>
    /// <exception cref="AggregateException">One or more frame events, node callbacks, timers, tweens, or deferred operations fail.</exception>
    public void PhysicsFrame(double delta) => _ = PhysicsProcess(delta);

    /// <summary>Gets or sets whether a root window close request automatically requests quit after its signal.</summary>
    /// <value>True by default.</value>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree is finalized or disposed.</exception>
    public bool AutoAcceptQuit
    {
        get { EnsureOwnerThread(); EnsureAcceptingWork(); return _autoAcceptQuit; }
        set { EnsureOwnerThread(); EnsureAcceptingWork(); _autoAcceptQuit = value; }
    }

    /// <summary>Requests termination after the current callback or frame, with the supplied exit code.</summary>
    /// <param name="exitCode">The code returned by Engine.Run; defaults to zero.</param>
    /// <remarks>May be called from any thread. The latest accepted request supplies the exit code. It does not
    /// terminate the process or dispose the tree synchronously. Manual MainLoop.Process/PhysicsProcess calls
    /// return true once quit is requested; an embedding host remains responsible for stopping its loop.</remarks>
    /// <exception cref="ObjectDisposedException">The tree no longer accepts work.</exception>
    public void Quit(int exitCode = 0)
    {
        lock (_workGate)
        {
            EnsureAcceptingWorkUnderLock();
            Volatile.Write(ref _exitCode, exitCode);
            Volatile.Write(ref _quitRequested, 1);
        }
    }

    internal void AcceptWindowClose()
    {
        EnsureOwnerThread();
        lock (_workGate)
        {
            EnsureAcceptingWorkUnderLock();
            if (_autoAcceptQuit && _quitRequested == 0)
            {
                Volatile.Write(ref _exitCode, 0);
                Volatile.Write(ref _quitRequested, 1);
            }
        }
    }

    internal bool QuitRequested => Volatile.Read(ref _quitRequested) != 0;
    internal int ExitCode => Volatile.Read(ref _exitCode);

    /// <summary>Marks the input event currently being dispatched as handled.</summary>
    /// <remarks>
    /// Handling stops the current stage immediately and skips every later input stage. The flag belongs only to the
    /// active synchronous dispatch and is reset before the next event.
    /// </remarks>
    /// <exception cref="InvalidOperationException">No input event is currently being dispatched or the caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    public void SetInputAsHandled()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        if (!_isDispatchingInput)
            throw new InvalidOperationException("Input can only be marked handled during scene input dispatch.");
        _inputHandled = true;
    }

    /// <summary>Gets whether the input event currently being dispatched has been handled.</summary>
    /// <returns><see langword="true"/> only after <see cref="SetInputAsHandled"/> during active dispatch.</returns>
    /// <exception cref="InvalidOperationException">No input event is currently being dispatched or the caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    public bool IsInputHandled()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        if (!_isDispatchingInput)
            throw new InvalidOperationException("Input handled state exists only during scene input dispatch.");
        return _inputHandled;
    }

    internal bool HasGUIFocus(Control control, bool ignoreHiddenFocus)
    {
        EnsureOwnerThread();
        return ReferenceEquals(_guiFocus, control) && (!ignoreHiddenFocus || !_guiFocusHidden);
    }

    internal Control? GetGUIFocusOwner(Viewport viewport)
    {
        EnsureOwnerThread();
        return ReferenceEquals(Root, viewport) ? _guiFocus : null;
    }

    internal void ReleaseGUIFocus(Viewport viewport)
    {
        EnsureOwnerThread();
        if (ReferenceEquals(Root, viewport) && _guiFocus is { } focused)
            ReleaseGUIFocus(focused);
    }

    internal void SetGUIFocus(Control control, bool hideFocus)
    {
        EnsureOwnerThread();
        if (Root is not Viewport viewport || !ReferenceEquals(control.GetViewport(), viewport) ||
            !ReferenceEquals(control.Tree, this) || !control.IsVisibleInTree || control.EffectiveFocusMode == FocusMode.None)
            return;
        if (ReferenceEquals(_guiFocus, control))
        {
            if (_guiFocusHidden != hideFocus) { _guiFocusHidden = hideFocus; control.QueueRedraw(); }
            return;
        }
        var previous = _guiFocus;
        _guiFocus = control;
        _guiFocusHidden = hideFocus;
        List<Exception>? errors = null;
        try { previous?.NotifyFocusExited(); } catch (Exception error) { CollectException(ref errors, error); }
        if (ReferenceEquals(_guiFocus, control))
        {
            try { viewport.NotifyGUIFocusChanged(control); } catch (Exception error) { CollectException(ref errors, error); }
            if (ReferenceEquals(_guiFocus, control))
                try { control.NotifyFocusEntered(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("GUI focus callbacks failed.", errors);
    }

    internal void ReleaseGUIFocus(Control control)
    {
        EnsureOwnerThread();
        if (ReferenceEquals(_guiMouseCapture, control)) _guiMouseCapture = null;
        if (!ReferenceEquals(_guiFocus, control)) return;
        _guiFocus = null;
        _guiFocusHidden = false;
        control.NotifyFocusExited();
    }

    internal void RefreshGUIFocus()
    {
        EnsureOwnerThread();
        if (_guiFocus is { } focused && focused.EffectiveFocusMode == FocusMode.None)
            ReleaseGUIFocus(focused);
    }

    /// <summary>Returns every current node in a group in depth-first pre-order.</summary>
    /// <param name="group">The nonblank, case-sensitive group name.</param>
    /// <returns>A read-only snapshot of matching nodes.</returns>
    /// <exception cref="ArgumentException"><paramref name="group"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The method is called from a thread other than the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    public IReadOnlyList<Node> GetNodesInGroup(string group)
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        ValidateGroup(group);
        return Array.AsReadOnly(GetNodesInGroupCore(group));
    }

    /// <summary>Returns the first current node in a group using depth-first pre-order.</summary>
    /// <param name="group">The nonblank, case-sensitive group name.</param>
    /// <returns>The first matching node, or <see langword="null"/> when the group has no current member.</returns>
    /// <exception cref="ArgumentException"><paramref name="group"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The method is called from a thread other than the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    public Node? GetFirstNodeInGroup(string group)
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        ValidateGroup(group);
        return Root.EnumerateDepthFirst().FirstOrDefault(node => node.IsInGroup(group));
    }

    /// <summary>Gets the number of current nodes in a group.</summary>
    /// <param name="group">The nonblank, case-sensitive group name.</param>
    /// <returns>The current number of matching nodes.</returns>
    /// <exception cref="ArgumentException"><paramref name="group"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The method is called from a thread other than the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    public int GetNodeCountInGroup(string group) => GetNodesInGroup(group).Count;

    /// <summary>Determines whether this tree currently contains a node in a group.</summary>
    /// <param name="group">The nonblank, case-sensitive group name.</param>
    /// <returns><see langword="true"/> when at least one current node belongs to the group.</returns>
    /// <exception cref="ArgumentException"><paramref name="group"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The method is called from a thread other than the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    public bool HasGroup(string group) => GetFirstNodeInGroup(group) is not null;

    /// <summary>Invokes a typed action for each current node in a group.</summary>
    /// <param name="group">The nonblank, case-sensitive group name.</param>
    /// <param name="action">The action to invoke for every eligible node.</param>
    /// <param name="flags">Ordering and scheduling behavior.</param>
    /// <remarks>
    /// Immediate operations require the owner thread. Deferred operations may be requested from any thread and execute
    /// during a future flush. The selected nodes are captured when the operation executes; each is revalidated before
    /// invocation. All selected callbacks are attempted before failures are reported. Deferred callback failures are
    /// reported by the future flush or frame, not by this scheduling call.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="group"/> is blank or <paramref name="flags"/> is invalid.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An immediate operation is called off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    /// <exception cref="AggregateException">One or more node callbacks fail during execution.</exception>
    public void CallGroup(string group, Action<Node> action, GroupCallFlags flags = GroupCallFlags.Default)
    {
        ValidateGroup(group);
        ArgumentNullException.ThrowIfNull(action);
        ValidateGroupCallFlags(flags);
        ScheduleGroupOperation(
            flags,
            new GroupOperationKey(GroupOperationKind.Call, group, action),
            () => ExecuteGroup(group, flags, action));
    }

    /// <summary>Applies a typed value through a setter for each current node in a group.</summary>
    /// <typeparam name="T">The value type accepted by the setter.</typeparam>
    /// <param name="group">The nonblank, case-sensitive group name.</param>
    /// <param name="setter">The typed setter to invoke for every eligible node.</param>
    /// <param name="value">The value captured for this operation.</param>
    /// <param name="flags">Ordering and scheduling behavior.</param>
    /// <remarks>
    /// With <see cref="GroupCallFlags.Unique"/>, the group and setter identify equality; differing values do not create
    /// additional queued operations, and the first accepted value is retained. Immediate execution requires the owner
    /// thread. Deferred execution may be requested from another thread, resolves membership when it starts, revalidates
    /// each candidate, attempts every selected setter, and then aggregates failures through the future flush or frame.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="group"/> is blank or <paramref name="flags"/> is invalid.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> or <paramref name="setter"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An immediate operation is called off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    /// <exception cref="AggregateException">One or more setter calls fail during execution.</exception>
    public void SetGroup<T>(string group, Action<Node, T> setter, T value, GroupCallFlags flags = GroupCallFlags.Default)
    {
        ValidateGroup(group);
        ArgumentNullException.ThrowIfNull(setter);
        ValidateGroupCallFlags(flags);
        ScheduleGroupOperation(
            flags,
            new GroupOperationKey(GroupOperationKind.Set, group, setter),
            () => ExecuteGroup(group, flags, node => setter(node, value)));
    }

    /// <summary>Delivers a numeric notification to each current node in a group.</summary>
    /// <param name="group">The nonblank, case-sensitive group name.</param>
    /// <param name="notification">The notification identifier.</param>
    /// <param name="flags">Ordering and scheduling behavior.</param>
    /// <remarks>
    /// Immediate execution requires the owner thread. Deferred execution may be requested from another thread, resolves
    /// membership when it starts, revalidates each candidate, attempts every selected notification, and then aggregates
    /// failures through the future flush or frame. Equal deferred unique operations are coalesced by group and
    /// notification identifier.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="group"/> is blank or <paramref name="flags"/> is invalid.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="group"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">An immediate operation is called off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    /// <exception cref="AggregateException">One or more notification callbacks fail during execution.</exception>
    public void NotifyGroup(string group, int notification, GroupCallFlags flags = GroupCallFlags.Default)
    {
        ValidateGroup(group);
        ValidateGroupCallFlags(flags);
        ScheduleGroupOperation(
            flags,
            new GroupOperationKey(GroupOperationKind.Notify, group, notification),
            () => ExecuteGroup(group, flags, node => node.Notify(notification)));
    }

    /// <summary>Thread-safely queues an engine object for deterministic disposal at a future deletion phase.</summary>
    /// <param name="instance">The live object to dispose.</param>
    /// <remarks>
    /// A node attached to this tree is detached before disposal. Detached objects are allowed. This method does not
    /// provide cancellation; use <see cref="Node.QueueFree"/> and <see cref="Node.CancelFree"/> for cancellable node
    /// deletion.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is this tree.</exception>
    /// <exception cref="InvalidOperationException">The object is this tree's root or is a node attached to another tree.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, tree disposal has started or finished, or object disposal has started or finished.</exception>
    public void QueueDelete(ElectronObject instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ObjectDisposedException.ThrowIf(instance.IsDisposed, instance);

        if (ReferenceEquals(instance, this))
            throw new ArgumentException("A SceneTree cannot queue itself for deletion.", nameof(instance));

        if (instance is Node node)
        {
            if (ReferenceEquals(node, Root))
                throw new InvalidOperationException("The SceneTree root cannot be queued for deletion.");

            if (node.Tree is not null && !ReferenceEquals(node.Tree, this))
                throw new InvalidOperationException("A node owned by another SceneTree cannot be queued here.");
        }

        EnqueueDeletion(new DeletionRequest(instance, RequiresNodeRequest: false));
    }

    /// <summary>Executes one captured deferred-action batch followed by one captured deletion batch.</summary>
    /// <remarks>
    /// Every captured entry is attempted. Deferred actions queued after action capture wait for a later flush. The
    /// deletion batch is captured after those actions, so deletion requested by a captured action runs in the same
    /// flush. Re-entrant execution and execution during node lifecycle or pause delivery are rejected. Failures are
    /// reported after both phases.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The method is called off the owner thread, re-entered, or called during node lifecycle or pause delivery.</exception>
    /// <exception cref="ObjectDisposedException">The tree has been finalized, or disposal has started or finished.</exception>
    /// <exception cref="AggregateException">One or more actions or deletions fail.</exception>
    public void FlushDeferred()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        EnsureAcceptingWork();
        BeginExecution();
        List<Exception>? errors = null;

        try
        {
            FlushDeferredCore(ref errors);
        }
        finally
        {
            EndExecution();
        }

        ThrowCollected("One or more deferred SceneTree operations failed.", errors);
    }

    /// <inheritdoc />
    /// <remarks>Appends this class's typed ownership, current scene, frame, queue, count, and pause descriptors.</remarks>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(SceneTreeProperties);

    /// <inheritdoc />
    /// <remarks>Runs the process-frame pipeline and returns whether quit was requested.</remarks>
    protected override bool OnProcess(double delta)
    {
        RunFrame(delta, CurrentUnscaledFrameDelta, physics: false);
        return QuitRequested;
    }

    /// <inheritdoc />
    /// <remarks>Runs the physics-frame pipeline and returns whether quit was requested.</remarks>
    protected override bool OnPhysicsProcess(double delta)
    {
        RunFrame(delta, CurrentUnscaledFrameDelta, physics: true);
        return QuitRequested;
    }

    internal override void ValidateInputEventDispatch()
    {
        base.ValidateInputEventDispatch();
        EnsureAcceptingWork();
        EnsureExecutionAvailable();
    }

    internal override void DispatchInputEvent(InputEvent @event)
    {
        if (Root is Viewport viewport) DispatchViewportInput(viewport, @event, inLocalCoordinates: false);
        else DispatchLocalInputEvent(@event);
    }

    internal void DispatchViewportInput(Viewport viewport, InputEvent inputEvent, bool inLocalCoordinates)
    {
        ArgumentNullException.ThrowIfNull(inputEvent); inputEvent.EnsureUsable();
        ThrowIfDisposed(); EnsureOwnerThread(); EnsureAcceptingWork(); EnsureExecutionAvailable();
        if (!ReferenceEquals(viewport.Tree, this)) throw new InvalidOperationException("Viewport is not attached to this tree.");
        var localized = inLocalCoordinates ? inputEvent : viewport.MakeViewportInputLocal(inputEvent);
        try { DispatchLocalInputEvent(localized); }
        finally { if (!ReferenceEquals(localized, inputEvent)) localized.Dispose(); }
    }

    private void DispatchLocalInputEvent(InputEvent @event)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();
        EnsureOwnerThread();
        EnsureAcceptingWork();
        BeginExecution();
        _isDispatchingInput = true;
        _inputHandled = false;
        List<Exception>? errors = null;

        try
        {
            CaptureInputNodes();
            DispatchInputStage(@event, InputStage.Input, ref errors);

            if (Root is Viewport hoverViewport && @event is InputEventMouse hoverMouse)
                UpdateGUIHover(hoverViewport, hoverMouse.Position, ref errors);

            if (!_inputHandled && Root is Viewport viewport)
                DispatchGUIInput(viewport, @event, ref errors);

            if (!_inputHandled && @event is InputEventKey key)
                DispatchInputStage(key, InputStage.UnhandledKey, ref errors);

            if (!_inputHandled)
                DispatchInputStage(@event, InputStage.Unhandled, ref errors);

            if (_guiHoverRefreshPending && _guiHoverKnown && _guiHoverViewport is { } refreshViewport)
                UpdateGUIHover(refreshViewport, _guiHoverPosition, ref errors);
        }
        finally
        {
            _inputTraversal.Clear();
            _inputHandled = false;
            _isDispatchingInput = false;
            EndExecution();
        }

        ThrowCollected("One or more scene input callbacks failed.", errors);
    }

    /// <inheritdoc />
    /// <remarks>
    /// System lifecycle notifications are propagated through a depth-first snapshot of the live hierarchy after
    /// inherited handling. Removed or disposed candidates are skipped and callback failures are reported together.
    /// </remarks>
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);

        if (!IsSystemNotification(what) || Root.IsDisposed || !ReferenceEquals(Root.Tree, this))
            return;

        EnsureOwnerThread();
        List<Exception>? errors = null;

        foreach (var node in Root.EnumerateDepthFirst().ToArray())
        {
            if (node.IsDisposed || !ReferenceEquals(node.Tree, this))
                continue;

            try
            {
                node.DispatchNotification(what);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        ThrowCollected("One or more system notifications failed.", errors);
    }

    /// <inheritdoc />
    /// <remarks>Rejects finalization during construction, a frame, input dispatch, a flush, lifecycle, pause, or scene-change delivery.</remarks>
    /// <exception cref="InvalidOperationException">Construction is incomplete or execution is active.</exception>
    protected override void ValidateFinalization()
    {
        if (ReferenceEquals(Engine.Instance.MainLoop, this) && Engine.Instance.OwnsWindowRun)
            throw new InvalidOperationException("Request Quit while Engine.Run owns the scene lifecycle.");
        if (!_constructionComplete)
            throw new InvalidOperationException("A SceneTree cannot be finalized before construction completes.");

        if (_activeExecution != 0 || _lifecycleExecutionDepth != 0 || _isChangingPause || _sceneChangePreparing)
            throw new InvalidOperationException("A SceneTree cannot be finalized from one of its frame, input, flush, lifecycle, or pause callbacks.");

        base.ValidateFinalization();
    }

    /// <inheritdoc />
    /// <remarks>Requires the owner thread and rejects disposal re-entered from a frame, input, flush, lifecycle, pause, or scene-change callback.</remarks>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or execution is active.</exception>
    protected override void ValidateDisposal()
    {
        if (ReferenceEquals(Engine.Instance.MainLoop, this) && Engine.Instance.OwnsWindowRun)
            throw new InvalidOperationException("Request Quit while Engine.Run owns the scene lifecycle.");
        EnsureOwnerThread();

        if (_activeExecution != 0 || _lifecycleExecutionDepth != 0 || _isChangingPause || _sceneChangePreparing)
            throw new InvalidOperationException("A SceneTree cannot be disposed from one of its frame, input, flush, lifecycle, or pause callbacks.");

        base.ValidateDisposal();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Atomically closes the work queues, exits and recursively disposes the root and any pending scene, disposes active timers, invalidates
    /// active tweens, clears event subscribers, and attempts every teardown stage before reporting collected failures.
    /// </remarks>
    protected override void OnFinalize()
    {
        List<Exception>? errors = null;

        lock (_workGate)
        {
            _acceptingWork = false;
            ClearPendingWorkUnderLock();
        }

        try
        {
            _lifecycleExecutionDepth++;

            try
            {
                Root.ExitTree(this);
            }
            finally
            {
                _lifecycleExecutionDepth--;
            }
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            Root.Dispose();
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        DisposePendingScenes(ref errors);

        foreach (var timer in _timers.ToArray())
        {
            try
            {
                timer.Dispose();
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        _timers.Clear();
        foreach (var tween in _tweens.ToArray())
        {
            try
            {
                tween.InvalidateFromTree();
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        _tweens.Clear();
        _scheduleTraversal.Clear();
        _scheduledNodes.Clear();
        _timerSnapshot.Clear();
        _tweenSnapshot.Clear();

        lock (_workGate)
            ClearPendingWorkUnderLock();

        ClearEventSubscribers();

        try
        {
            base.OnFinalize();
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        ThrowCollected("One or more SceneTree teardown operations failed.", errors);
    }

    internal bool IsRoot(Node node) => ReferenceEquals(Root, node);

    internal void QueueForDeletion(Node node)
    {
        lock (_workGate)
        {
            EnsureAcceptingWorkUnderLock();

            if (node.TryPublishQueuedDeletion())
                _deletions.Enqueue(new DeletionRequest(node, RequiresNodeRequest: true));
        }
    }

    internal void AttachSubtree(Node node)
    {
        EnsureOwnerThread();
        _lifecycleExecutionDepth++;

        try
        {
            node.EnterTree(this);

            if (ReferenceEquals(node.Tree, this))
                node.MakeReady(_activationReadied);
        }
        finally
        {
            _lifecycleExecutionDepth--;
        }
    }

    internal void DetachSubtree(Node node)
    {
        EnsureOwnerThread();
        _lifecycleExecutionDepth++;

        try
        {
            node.ExitTree(this);
        }
        finally
        {
            _lifecycleExecutionDepth--;
        }
    }

    internal bool IsOwnerThread => Environment.CurrentManagedThreadId == _ownerThreadId;

    internal void EnsureOwnerThread()
    {
        if (!IsOwnerThread)
            throw new InvalidOperationException("SceneTree mutation and execution must run on its owner thread.");
    }

    internal void RemoveTimer(SceneTreeTimer timer)
    {
        EnsureOwnerThread();
        _timers.Remove(timer);
    }

    internal void RemoveTween(Tween tween)
    {
        EnsureOwnerThread();
        _tweens.Remove(tween);
    }

    internal void CompleteTween(Tween tween)
    {
        EnsureOwnerThread();
        _tweens.Remove(tween);
        tween.InvalidateFromTree();
    }

    internal void NotifyNodeAdded(Node node) => NodeAdded?.Invoke(this, node);

    internal void NotifyNodeRemoved(Node node)
    {
        if (ReferenceEquals(_editedSceneRoot, node)) _editedSceneRoot = null;
        if (ReferenceEquals(_currentScene, node)) _currentScene = null;
        NodeRemoved?.Invoke(this, node);
    }

    internal void NotifyConfigurationWarningsChanged(Node node)
    {
        if (_editedSceneRoot is { } root && ReferenceEquals(root.Tree, this) &&
            (ReferenceEquals(root, node) || root.IsAncestorOf(node)))
            NodeConfigurationWarningChanged?.Invoke(this, node);
    }

    internal void NotifyNodeRenamed(Node node)
    {
        List<Exception>? errors = null;

        try
        {
            NodeRenamed?.Invoke(this, node);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            TreeChanged?.Invoke(this);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        ThrowCollected("One or more node-renamed tree events failed.", errors);
    }

    internal void NotifyTreeChanged() => TreeChanged?.Invoke(this);

    private void RunFrame(double delta, double unscaledDelta, bool physics)
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        EnsureAcceptingWork();

        if (!double.IsFinite(delta) || delta < 0d)
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Frame delta must be finite and non-negative.");

        BeginExecution();
        _inPhysicsFrame = physics;
        List<Exception>? errors = null;

        try
        {
            if (physics && _physicsInterpolation) CapturePhysicsInterpolation(start: true, ref errors);
            if (physics)
                _physicsFrameCount++;
            else
                _processFrameCount++;

            if (physics) FlushTransformNotifications(ref errors);
            try
            {
                if (physics)
                    PhysicsFrameStarted?.Invoke(this);
                else
                    ProcessFrameStarted?.Invoke(this);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }

            if (!physics) FlushTransformNotifications(ref errors);
            _scheduledNodes.Clear();
            CaptureScheduledNodes(physics);
            _scheduledNodes.Sort();

            foreach (var item in _scheduledNodes)
            {
                var node = item.Node;
                if (node.IsDisposed || !ReferenceEquals(node.Tree, this) || !node.CanProcess())
                    continue;

                if (!node.HasProcessCallback(physics))
                    continue;

                try
                {
                    node.RunProcess(delta, unscaledDelta, physics);
                }
                catch (Exception error)
                {
                    CollectException(ref errors, error);
                }
            }

            if (!physics) FlushTransformNotifications(ref errors);
            ProcessTimers(delta, unscaledDelta, physics, ref errors);
            ProcessTweens(delta, unscaledDelta, physics, ref errors);
            FlushDeferredCore(ref errors, flushTransforms: true);
            if (physics && _physicsInterpolation) CapturePhysicsInterpolation(start: false, ref errors);
        }
        finally
        {
            _inPhysicsFrame = false;
            _scheduledNodes.Clear();
            _scheduleTraversal.Clear();
            _timerSnapshot.Clear();
            _tweenSnapshot.Clear();
            EndExecution();
        }

        ThrowCollected(
            physics ? "One or more physics-frame operations failed." : "One or more process-frame operations failed.",
            errors);
    }

    private void ProcessTimers(double delta, double unscaledDelta, bool physics, ref List<Exception>? errors)
    {
        _timerSnapshot.Clear();

        foreach (var timer in _timers)
        {
            if (timer.ProcessInPhysics == physics)
                _timerSnapshot.Add(timer);
        }

        foreach (var timer in _timerSnapshot)
        {
            if (timer.IsDisposed || !timer.Advance(timer.IgnoreTimeScale ? unscaledDelta : delta, _paused))
                continue;

            _timers.Remove(timer);

            try
            {
                timer.Expire();
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }
    }

    private void ProcessTweens(double delta, double unscaledDelta, bool physics, ref List<Exception>? errors)
    {
        _tweenSnapshot.Clear();
        var expectedMode = physics ? Tween.TweenProcessMode.Physics : Tween.TweenProcessMode.Idle;

        foreach (var tween in _tweens)
        {
            if (!tween.IsDisposed && tween.ProcessMode == expectedMode)
                _tweenSnapshot.Add(tween);
        }

        foreach (var tween in _tweenSnapshot)
        {
            if (tween.IsDisposed || tween.IsNested || tween.ProcessMode != expectedMode ||
                !tween.CanProcess(_paused))
                continue;

            try
            {
                if (tween.Advance(tween.IgnoreTimeScale ? unscaledDelta : delta))
                    continue;
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }

            _tweens.Remove(tween);
            try
            {
                tween.InvalidateFromTree();
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }
    }

    private void CaptureScheduledNodes(bool physics)
    {
        _scheduleTraversal.Clear();
        _scheduleTraversal.Add(Root);
        var order = 0;

        while (_scheduleTraversal.Count != 0)
        {
            var last = _scheduleTraversal.Count - 1;
            var node = _scheduleTraversal[last];
            _scheduleTraversal.RemoveAt(last);
            _scheduledNodes.Add(new ScheduledNode(
                node,
                physics ? node.PhysicsProcessPriority : node.ProcessPriority,
                order++));

            var children = node.Children;
            for (var index = children.Count - 1; index >= 0; index--)
                _scheduleTraversal.Add(children[index]);
        }
    }

    private void CaptureInputNodes()
    {
        _inputTraversal.Clear();
        _scheduleTraversal.Clear();
        _scheduleTraversal.Add(Root);

        while (_scheduleTraversal.Count != 0)
        {
            var last = _scheduleTraversal.Count - 1;
            var node = _scheduleTraversal[last];
            _scheduleTraversal.RemoveAt(last);
            _inputTraversal.Add(node);

            var children = node.Children;
            for (var index = children.Count - 1; index >= 0; index--)
                _scheduleTraversal.Add(children[index]);
        }

        _scheduleTraversal.Clear();
    }

    private void DispatchInputStage(InputEvent @event, InputStage stage, ref List<Exception>? errors)
    {
        for (var index = _inputTraversal.Count - 1; index >= 0 && !_inputHandled; index--)
        {
            var node = _inputTraversal[index];
            if (node.IsDisposed || !ReferenceEquals(node.Tree, this) || !node.CanProcess())
                continue;

            var enabled = stage switch
            {
                InputStage.Input => node.InputEnabled,
                InputStage.UnhandledKey => node.UnhandledKeyInputEnabled,
                InputStage.Unhandled => node.UnhandledInputEnabled,
                _ => false,
            };

            if (!enabled)
                continue;

            try
            {
                switch (stage)
                {
                    case InputStage.Input:
                        node.DispatchInput(@event);
                        break;
                    case InputStage.UnhandledKey:
                        node.DispatchUnhandledKeyInput((InputEventKey)@event);
                        break;
                    case InputStage.Unhandled:
                        node.DispatchUnhandledInput(@event);
                        break;
                }
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }
    }

    private void DispatchGUIInput(Viewport viewport, InputEvent inputEvent, ref List<Exception>? errors)
    {
        if (inputEvent is InputEventMouse mouse)
        {
            var captured = _guiMouseCapture;
            if (captured is not null && (!ReferenceEquals(captured.Tree, this) || !captured.IsVisibleInTree || captured.EffectiveMouseFilter == MouseFilter.Ignore))
                captured = _guiMouseCapture = null;

            var release = mouse is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false };
            var press = mouse is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true };
            var target = captured is not null && (release || mouse is InputEventMouseMotion { ButtonMask: var mask } && (mask & MouseButtonMask.Left) != 0)
                ? captured : FindMouseControl(viewport, mouse.Position, ref errors);
            if (press) _guiMouseCapture = target;
            if (release) _guiMouseCapture = null;
            if (target is null) return;
            if (press && target.EffectiveFocusMode != FocusMode.None)
                try { SetGUIFocus(target, hideFocus: true); } catch (Exception error) { CollectException(ref errors, error); }

            for (Control? current = target; current is not null && !_inputHandled;)
            {
                if (current.IsDisposed || !ReferenceEquals(current.Tree, this) || !ReferenceEquals(current.GetViewport(), viewport)) break;
                var next = current.TopLevel ? null : current.Parent as Control;
                if (current.EffectiveMouseFilter != MouseFilter.Ignore && current.IsVisibleInTree)
                {
                    var filter = current.EffectiveMouseFilter;
                    var forcePassWheel = current.MouseForcePassScrollEvents;
                    try
                    {
                        using var local = current.MakeInputLocal(mouse);
                        if (local is InputEventMouse localMouse)
                        {
                            var canvasPosition = current.GetCanvasTransform().AffineInverse() * mouse.Position;
                            if (!canvasPosition.IsFinite()) throw new ArgumentOutOfRangeException(nameof(mouse), "Canvas input coordinates must be finite.");
                            localMouse.GlobalPosition = canvasPosition;
                        }
                        current.DispatchGUIInput(local);
                    }
                    catch (Exception error) { CollectException(ref errors, error); }
                    var wheel = mouse is InputEventMouseButton { ButtonIndex: >= MouseButton.WheelUp and <= MouseButton.WheelRight };
                    if (filter == MouseFilter.Stop && (!wheel || !forcePassWheel) && !_inputHandled)
                        SetInputAsHandled();
                }
                current = next;
            }
            return;
        }

        if (inputEvent is not (InputEventKey or InputEventJoypadButton or InputEventJoypadMotion or InputEventAction)) return;
        var focused = _guiFocus;
        if (focused is not null && (focused.IsDisposed || !ReferenceEquals(focused.Tree, this) || !focused.IsVisibleInTree || focused.EffectiveFocusMode == FocusMode.None || !ReferenceEquals(focused.GetViewport(), viewport)))
        {
            try { ReleaseGUIFocus(focused); } catch (Exception error) { CollectException(ref errors, error); }
            focused = null;
        }
        if (focused is not null)
            try { focused.DispatchGUIInput(inputEvent); } catch (Exception error) { CollectException(ref errors, error); }
        if (!_inputHandled)
            try { NavigateGUIFocus(viewport, inputEvent); } catch (Exception error) { CollectException(ref errors, error); }
    }

    private Control? FindMouseControl(Viewport viewport, Vector2 point, ref List<Exception>? errors)
    {
        Control? best = null;
        var bestLayer = int.MinValue;
        var bestZ = int.MinValue;
        for (var index = _inputTraversal.Count - 1; index >= 0; index--)
        {
            if (_inputTraversal[index] is not Control control || control.IsDisposed || !ReferenceEquals(control.Tree, this) ||
                !control.IsVisibleInTree || control.EffectiveMouseFilter == MouseFilter.Ignore || !ReferenceEquals(control.GetViewport(), viewport)) continue;
            try
            {
                var clipped = false;
                for (var ancestor = control.GetParentItem(); ancestor is not null; ancestor = ancestor.GetParentItem())
                    if (ancestor is Control { ClipContents: true } parent && !parent.ContainsClipPoint(point))
                    { clipped = true; break; }
                if (clipped) continue;
                if (!control.HitTest(point)) continue;
                var layer = control.GetCanvasLayerNode()?.Layer ?? 0;
                var z = control.EffectiveZIndex;
                if (best is null || layer > bestLayer || layer == bestLayer && z > bestZ)
                { best = control; bestLayer = layer; bestZ = z; }
            }
            catch (Exception error) { CollectException(ref errors, error); }
        }
        return best;
    }

    private void FlushDeferredCore(ref List<Exception>? errors, bool flushTransforms = false)
    {
        ConcurrentQueue<Action>? actions = null;

        lock (_workGate)
        {
            if (!_deferred.IsEmpty)
            {
                actions = _deferred;
                _deferred = new ConcurrentQueue<Action>();
            }
        }

        while (actions?.TryDequeue(out var action) == true)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        if (flushTransforms) FlushTransformNotifications(ref errors);

        ConcurrentQueue<DeletionRequest>? deletions = null;

        lock (_workGate)
        {
            if (!_deletions.IsEmpty)
            {
                deletions = _deletions;
                _deletions = new ConcurrentQueue<DeletionRequest>();
            }
        }

        while (deletions?.TryDequeue(out var request) == true)
        {
            try
            {
                ExecuteDeletion(request);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }
    }

    private void ExecuteDeletion(DeletionRequest request)
    {
        if (request.Instance.IsDisposed)
            return;

        if (request.Instance is not Node node)
        {
            request.Instance.Dispose();
            return;
        }

        if (node.Tree is not null && !ReferenceEquals(node.Tree, this))
        {
            if (request.RequiresNodeRequest)
                return;

            throw new InvalidOperationException("A queued node became owned by another SceneTree before deletion.");
        }

        if (request.RequiresNodeRequest)
        {
            if (!node.TryConsumeQueuedDeletion())
                return;

            if (node.Tree is not null && !ReferenceEquals(node.Tree, this))
                return;
        }

        if (ReferenceEquals(node, Root))
            throw new InvalidOperationException("The SceneTree root cannot be deleted independently.");

        List<Exception>? errors = null;

        try
        {
            node.Parent?.RemoveChild(node);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        try
        {
            node.Dispose();
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        ThrowCollected("One or more queued node-deletion operations failed.", errors);
    }

    private void ExecuteGroup(string group, GroupCallFlags flags, Action<Node> operation)
    {
        EnsureOwnerThread();
        var nodes = GetNodesInGroupCore(group);

        if ((flags & GroupCallFlags.Reverse) != 0)
            Array.Reverse(nodes);

        List<Exception>? errors = null;

        foreach (var node in nodes)
        {
            if (node.IsDisposed || !ReferenceEquals(node.Tree, this) || !node.IsInGroup(group))
                continue;

            try
            {
                operation(node);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }

        ThrowCollected("One or more scene-group operations failed.", errors);
    }

    private void ScheduleGroupOperation(GroupCallFlags flags, GroupOperationKey key, Action operation)
    {
        if ((flags & GroupCallFlags.Deferred) == 0)
        {
            ThrowIfDisposed();
            EnsureOwnerThread();
            EnsureAcceptingWork();
            operation();
            return;
        }

        if ((flags & GroupCallFlags.Unique) == 0)
        {
            Defer(operation);
            return;
        }

        lock (_workGate)
        {
            EnsureAcceptingWorkUnderLock();

            if (!_uniqueGroupOperations.Add(key))
                return;

            _deferred.Enqueue(() =>
            {
                lock (_workGate)
                    _uniqueGroupOperations.Remove(key);

                operation();
            });
        }
    }

    private void EnqueueAction(Action action)
    {
        lock (_workGate)
        {
            EnsureAcceptingWorkUnderLock();
            _deferred.Enqueue(action);
        }
    }

    private void EnqueueDeletion(DeletionRequest request)
    {
        lock (_workGate)
        {
            EnsureAcceptingWorkUnderLock();
            _deletions.Enqueue(request);
        }
    }

    private void EnsureAcceptingWork()
    {
        lock (_workGate)
            EnsureAcceptingWorkUnderLock();
    }

    private void EnsureAcceptingWorkUnderLock()
    {
        if (!_acceptingWork || IsDisposed)
            throw new ObjectDisposedException(GetType().FullName);
    }

    private void BeginExecution()
    {
        EnsureExecutionAvailable();
        _activeExecution = 1;
    }

    internal void QueueTransformNotification(CanvasItem item)
    {
        if (item.TransformQueueEntry.List is null) _transformChanges.AddLast(item.TransformQueueEntry);
    }

    internal void DeliverTransformNotification(CanvasItem item)
    {
        _lifecycleExecutionDepth++;
        try { item.DispatchNotification(CanvasItem.NotificationTransformChanged); }
        finally { _lifecycleExecutionDepth--; }
    }

    internal void CancelTransformNotification(CanvasItem item)
    {
        var entry = item.TransformQueueEntry;
        if (ReferenceEquals(_nextTransformNotification, entry)) _nextTransformNotification = entry.Next;
        entry.List?.Remove(entry);
    }

    private void FlushTransformNotifications(ref List<Exception>? errors)
    {
        var entry = _transformChanges.First;
        try
        {
            while (entry is not null)
            {
                // Capture the successor before callbacks. Cancellation advances this cursor before unlinking.
                _nextTransformNotification = entry.Next;
                _transformChanges.Remove(entry);
                var item = entry.Value;
                if (!item.IsDisposed && ReferenceEquals(item.Tree, this))
                {
                    try { DeliverTransformNotification(item); }
                    catch (Exception error) { CollectException(ref errors, error); }
                }
                entry = _nextTransformNotification;
            }
        }
        finally { _nextTransformNotification = null; }
    }

    internal void RenderCanvas(RenderingServer renderer, double step)
    {
        ThrowIfDisposed(); EnsureOwnerThread(); EnsureAcceptingWork(); BeginExecution();
        try { renderer.Render(this, step); }
        finally { EndExecution(); }
    }

    private void EnsureExecutionAvailable()
    {
        if (_activeExecution != 0 || _lifecycleExecutionDepth != 0 || _isChangingPause)
            throw new InvalidOperationException("SceneTree frame, input, and flush execution cannot run re-entrantly or during lifecycle or pause delivery.");
    }

    private void EndExecution() => _activeExecution = 0;

    private void ClearPendingWork()
    {
        lock (_workGate)
            ClearPendingWorkUnderLock();
    }

    private void ClearPendingWorkUnderLock()
    {
        _deferred = new ConcurrentQueue<Action>();
        _deletions = new ConcurrentQueue<DeletionRequest>();
        _uniqueGroupOperations.Clear();
        _transformChanges.Clear(); _nextTransformNotification = null;
    }

    private void ClearEventSubscribers()
    {
        NodeAdded = null;
        NodeRemoved = null;
        NodeRenamed = null;
        NodeConfigurationWarningChanged = null;
        _editedSceneRoot = null;
        ProcessFrameStarted = null;
        PhysicsFrameStarted = null;
        TreeChanged = null;
        SceneChanged = null;
        _currentScene = null;
    }

    private Node[] GetNodesInGroupCore(string group) =>
        Root.EnumerateDepthFirst().Where(node => node.IsInGroup(group)).ToArray();

    private static void ValidateGroup(string group) => ArgumentException.ThrowIfNullOrWhiteSpace(group);

    private static void ValidateGroupCallFlags(GroupCallFlags flags)
    {
        if ((flags & ~SupportedGroupCallFlags) != 0)
            throw new ArgumentOutOfRangeException(nameof(flags), flags, "The group call flags contain an unsupported value.");

        if ((flags & GroupCallFlags.Unique) != 0 && (flags & GroupCallFlags.Deferred) == 0)
            throw new ArgumentException("Unique group calls must also be deferred.", nameof(flags));
    }

    private static bool IsSystemNotification(int what) => what is
        NotificationOsMemoryWarning or
        NotificationTranslationChanged or
        NotificationWmAbout or
        NotificationCrash or
        NotificationOsImeUpdate or
        NotificationApplicationResumed or
        NotificationApplicationPaused or
        NotificationApplicationFocusIn or
        NotificationApplicationFocusOut or
        NotificationTextServerChanged or
        NotificationApplicationPipModeEntered or
        NotificationApplicationPipModeExited;

    private static void CollectException(ref List<Exception>? errors, Exception error)
    {
        errors ??= [];
        CollectException(errors, error);
    }

    private static void CollectException(List<Exception> errors, Exception error)
    {
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

    private readonly record struct ScheduledNode(Node Node, int Priority, int Order) : IComparable<ScheduledNode>
    {
        public int CompareTo(ScheduledNode other)
        {
            var priority = Priority.CompareTo(other.Priority);
            return priority != 0 ? priority : Order.CompareTo(other.Order);
        }
    }

    private readonly record struct DeletionRequest(ElectronObject Instance, bool RequiresNodeRequest);

    private enum InputStage
    {
        Input,
        UnhandledKey,
        Unhandled,
    }

    private readonly record struct GroupOperationKey(GroupOperationKind Kind, string Group, object Identity);

    private enum GroupOperationKind
    {
        Call,
        Set,
        Notify
    }
}
