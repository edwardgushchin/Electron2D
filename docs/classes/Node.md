# Node

Last updated: 2026-09-21

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** [Timer](Timer.md)

- **Source:** [`src/Scene/Main/Node.cs`](../../src/Scene/Main/Node.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Node : ElectronObject`

> Provides Electron2D's unified hierarchical, input-aware game object and 2D transform type.

## Description

Provides Electron2D's unified hierarchical, input-aware game object and 2D transform type.

A parent owns its children. An active [`SceneTree`](SceneTree.md) owns its root and therefore the whole hierarchy. A node owns no renderer or native SDL handle. Its complete spatial surface uses engine-owned [`Vector2`](Vector2.md) and [`Transform`](Transform.md) values.

Any self-contained root and its owned descendants can be captured by [`PackedScene`](PackedScene.md) as a reusable scene. Instantiation returns an independent detached hierarchy; lifecycle begins only after explicit attachment to a `SceneTree`.

The type combines ordered child ownership, tree lifecycle, paths, groups, processing, typed input callbacks, queued
deletion, visibility, Z ordering, and 2D spatial state. Logical visibility and Z state do not render anything until
a renderer domain is added.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var root = new Node { Name = "World" };
root.AddChild(new Node { Name = "Player", Position = new Vector2(32f, 16f) });
```

## Constructors

| Member | Description |
| --- | --- |
| [`public Node()`](#m-electron2d-node-ctor) | Initializes a detached node with its runtime class name and an identity transform. |

## Properties

| Member | Description |
| --- | --- |
| [`public string Name { get; set; }`](#p-electron2d-node-name) | Gets or sets the node name used in sibling lookup and paths. |
| [`public Node Parent { get; }`](#p-electron2d-node-parent) | Gets the direct parent. |
| [`public string SceneFilePath { get; }`](#p-electron2d-node-scenefilepath) | Gets the external resource path from which this scene root was instantiated. |
| [`public Node Owner { get; set; }`](#p-electron2d-node-owner) | Gets or sets the ancestor that owns this node for packed-scene storage. |
| [`public IReadOnlyList<Node> Children { get; }`](#p-electron2d-node-children) | Gets a live read-only view of the ordered direct children. |
| [`public int ChildCount { get; }`](#p-electron2d-node-childcount) | Gets the number of direct children. |
| [`public SceneTree Tree { get; }`](#p-electron2d-node-tree) | Gets the active scene tree containing this node. |
| [`public bool IsInsideTree { get; }`](#p-electron2d-node-isinsidetree) | Gets whether this node currently belongs to a scene tree. |
| [`public bool IsNodeReady { get; }`](#p-electron2d-node-isnodeready) | Gets whether SceneTree-managed ready delivery has occurred since construction or the last ready reset. |
| [`public bool IsQueuedForDeletion { get; }`](#p-electron2d-node-isqueuedfordeletion) | Gets whether deletion has been requested through [`Node.QueueFree`](Node.md#m-electron2d-node-queuefree). |
| [`public Transform Transform { get; set; }`](#p-electron2d-node-transform) | Gets or sets the affine transform relative to the parent. |
| [`public Transform GlobalTransform { get; set; }`](#p-electron2d-node-globaltransform) | Gets or sets the affine transform in hierarchy-global coordinates. |
| [`public Vector2 Position { get; set; }`](#p-electron2d-node-position) | Gets or sets local translation in pixels or other host-defined 2D units. |
| [`public Vector2 GlobalPosition { get; set; }`](#p-electron2d-node-globalposition) | Gets or sets translation in hierarchy-global coordinates. |
| [`public float Rotation { get; set; }`](#p-electron2d-node-rotation) | Gets or sets local rotation in radians. |
| [`public float RotationDegrees { get; set; }`](#p-electron2d-node-rotationdegrees) | Gets or sets local rotation in degrees. |
| [`public float GlobalRotation { get; set; }`](#p-electron2d-node-globalrotation) | Gets or sets hierarchy-global rotation in radians. |
| [`public float GlobalRotationDegrees { get; set; }`](#p-electron2d-node-globalrotationdegrees) | Gets or sets hierarchy-global rotation in degrees. |
| [`public Vector2 Scale { get; set; }`](#p-electron2d-node-scale) | Gets or sets local scale. |
| [`public Vector2 GlobalScale { get; set; }`](#p-electron2d-node-globalscale) | Gets or sets hierarchy-global scale. |
| [`public float Skew { get; set; }`](#p-electron2d-node-skew) | Gets or sets the local skew angle in radians. |
| [`public float GlobalSkew { get; set; }`](#p-electron2d-node-globalskew) | Gets or sets the hierarchy-global skew angle in radians. |
| [`public bool TopLevel { get; set; }`](#p-electron2d-node-toplevel) | Gets or sets whether this node ignores its parent's transform. |
| [`public bool Visible { get; set; }`](#p-electron2d-node-visible) | Gets or sets this node's local logical visibility. |
| [`public bool IsVisibleInTree { get; }`](#p-electron2d-node-isvisibleintree) | Gets whether this node is active and locally visible through its complete ancestor chain. |
| [`public int ZIndex { get; set; }`](#p-electron2d-node-zindex) | Gets or sets this node's local Z-order value. |
| [`public bool ZAsRelative { get; set; }`](#p-electron2d-node-zasrelative) | Gets or sets whether effective Z order accumulates ancestor Z values. |
| [`public bool NotifyLocalTransformChanges { get; set; }`](#p-electron2d-node-notifylocaltransformchanges) | Gets or sets whether local transform changes dispatch [`Node.NotificationLocalTransformChanged`](Node.md#f-electron2d-node-notificationlocaltransformchanged). |
| [`public bool NotifyTransformChanges { get; set; }`](#p-electron2d-node-notifytransformchanges) | Gets or sets whether global transform changes dispatch [`Node.NotificationTransformChanged`](Node.md#f-electron2d-node-notificationtransformchanged). |
| [`public NodeProcessMode ProcessMode { get; set; }`](#p-electron2d-node-processmode) | Gets or sets the pause policy used by both process callback lanes. |
| [`public bool ProcessEnabled { get; set; }`](#p-electron2d-node-processenabled) | Gets or sets whether this node participates in host-driven process frames. |
| [`public bool PhysicsProcessEnabled { get; set; }`](#p-electron2d-node-physicsprocessenabled) | Gets or sets whether this node participates in host-driven physics-process frames. |
| [`public bool InputEnabled { get; set; }`](#p-electron2d-node-inputenabled) | Gets or sets whether this node receives the first input-propagation stage. |
| [`public bool UnhandledInputEnabled { get; set; }`](#p-electron2d-node-unhandledinputenabled) | Gets or sets whether this node receives input left unhandled by earlier stages. |
| [`public bool UnhandledKeyInputEnabled { get; set; }`](#p-electron2d-node-unhandledkeyinputenabled) | Gets or sets whether this node receives unhandled keyboard events before general unhandled input. |
| [`public int ProcessPriority { get; set; }`](#p-electron2d-node-processpriority) | Gets or sets this node's ascending process-frame order key. |
| [`public int PhysicsProcessPriority { get; set; }`](#p-electron2d-node-physicsprocesspriority) | Gets or sets this node's ascending physics-process order key. |
| [`public double ProcessDeltaTime { get; }`](#p-electron2d-node-processdeltatime) | Gets the delta from the most recent SceneTree-managed process frame delivered to this node. |
| [`public double PhysicsProcessDeltaTime { get; }`](#p-electron2d-node-physicsprocessdeltatime) | Gets the delta from the most recent SceneTree-managed physics-process frame delivered to this node. |

## Methods

| Member | Description |
| --- | --- |
| [`public void AddChild(Node child)`](#m-electron2d-node-addchild-electron2d-node) | Appends a detached node as the last direct child. |
| [`public void AddSibling(Node sibling)`](#m-electron2d-node-addsibling-electron2d-node) | Inserts a detached node immediately after this node in its parent's child order. |
| [`public bool RemoveChild(Node child)`](#m-electron2d-node-removechild-electron2d-node) | Removes a direct child without disposing it. |
| [`public void MoveChild(Node child, int index)`](#m-electron2d-node-movechild-electron2d-node-system-int32) | Moves a direct child to another sibling index. |
| [`public void MoveToFront()`](#m-electron2d-node-movetofront) | Moves this node to the last position among its siblings. |
| [`public void Reparent(Node newParent, bool keepGlobalTransform = true)`](#m-electron2d-node-reparent-electron2d-node-system-boolean) | Moves this non-root node under a new parent. |
| [`public Node GetChild(int index)`](#m-electron2d-node-getchild-system-int32) | Gets a direct child by index. |
| [`public int GetIndex()`](#m-electron2d-node-getindex) | Gets this node's index in its parent's ordered child list. |
| [`public bool IsAncestorOf(Node node)`](#m-electron2d-node-isancestorof-electron2d-node) | Determines whether this node is a strict ancestor of another node. |
| [`public Node FindChild(string pattern, bool recursive = true)`](#m-electron2d-node-findchild-system-string-system-boolean) | Finds the first descendant whose name matches a wildcard pattern. |
| [`public TNode FindChild<TNode>(string pattern = "*", bool recursive = true)`](#m-electron2d-node-findchild-1-system-string-system-boolean) | Finds the first descendant of a requested type whose name matches a wildcard pattern. |
| [`public IReadOnlyList<Node> FindChildren(string pattern, bool recursive = true)`](#m-electron2d-node-findchildren-system-string-system-boolean) | Finds all descendants whose names match a wildcard pattern. |
| [`public IReadOnlyList<TNode> FindChildren<TNode>(string pattern = "*", bool recursive = true)`](#m-electron2d-node-findchildren-1-system-string-system-boolean) | Finds all descendants of a requested type whose names match a wildcard pattern. |
| [`public Node FindParent(string pattern)`](#m-electron2d-node-findparent-system-string) | Finds the nearest ancestor whose name matches a wildcard pattern. |
| [`public string GetPath()`](#m-electron2d-node-getpath) | Builds this node's absolute path from the root of its current hierarchy. |
| [`public string GetPathTo(Node node)`](#m-electron2d-node-getpathto-electron2d-node) | Builds a relative path from this node to another node in the same hierarchy. |
| [`public Node GetNode(string path)`](#m-electron2d-node-getnode-system-string) | Resolves a required relative or absolute node path. |
| [`public TNode GetNode<TNode>(string path)`](#m-electron2d-node-getnode-1-system-string) | Resolves a required relative or absolute path to a requested node type. |
| [`public Node GetNodeOrNull(string path)`](#m-electron2d-node-getnodeornull-system-string) | Attempts to resolve a relative or absolute node path. |
| [`public void AddToGroup(string group, bool persistent = false)`](#m-electron2d-node-addtogroup-system-string-system-boolean) | Adds this node to a case-sensitive group. |
| [`public bool RemoveFromGroup(string group)`](#m-electron2d-node-removefromgroup-system-string) | Removes this node from a case-sensitive group. |
| [`public bool IsInGroup(string group)`](#m-electron2d-node-isingroup-system-string) | Determines whether this node belongs to a case-sensitive group. |
| [`public IReadOnlyList<string> GetGroups()`](#m-electron2d-node-getgroups) | Returns this node's group memberships. |
| [`public bool CanProcess()`](#m-electron2d-node-canprocess) | Determines whether the resolved process mode allows callbacks in the current tree pause state. |
| [`public void RequestReady()`](#m-electron2d-node-requestready) | Requests ready delivery the next time SceneTree attachment reaches the ready phase. |
| [`public Tween CreateTween()`](#m-electron2d-node-createtween) | Creates a tween in this node's scene tree and binds it to this node. |
| [`public void QueueFree()`](#m-electron2d-node-queuefree) | Atomically requests this node's deferred disposal at a future scene-tree safe point. |
| [`public bool CancelFree()`](#m-electron2d-node-cancelfree) | Atomically cancels a pending deletion request. |
| [`public void Show()`](#m-electron2d-node-show) | Sets [`Node.Visible`](Node.md#p-electron2d-node-visible) to `true`. |
| [`public void Hide()`](#m-electron2d-node-hide) | Sets [`Node.Visible`](Node.md#p-electron2d-node-visible) to `false`. |
| [`public void ApplyScale(Vector2 ratio)`](#m-electron2d-node-applyscale-electron2d-vector2) | Component-multiplies the local scale by a ratio. |
| [`public void Rotate(float radians)`](#m-electron2d-node-rotate-system-single) | Adds an angle to the local rotation. |
| [`public void Translate(Vector2 offset)`](#m-electron2d-node-translate-electron2d-vector2) | Moves this node by an offset rotated by its local rotation. |
| [`public void GlobalTranslate(Vector2 offset)`](#m-electron2d-node-globaltranslate-electron2d-vector2) | Moves this node by a hierarchy-global offset. |
| [`public void MoveLocalX(float delta, bool scaled = false)`](#m-electron2d-node-movelocalx-system-single-system-boolean) | Moves this node along its local X basis axis. |
| [`public void MoveLocalY(float delta, bool scaled = false)`](#m-electron2d-node-movelocaly-system-single-system-boolean) | Moves this node along its local Y basis axis. |
| [`public float GetAngleTo(Vector2 globalPoint)`](#m-electron2d-node-getangleto-electron2d-vector2) | Computes the signed angle from this node's global positive X direction to a global point. |
| [`public void LookAt(Vector2 globalPoint)`](#m-electron2d-node-lookat-electron2d-vector2) | Rotates this node so its positive local X direction points at a global point. |
| [`public Vector2 ToGlobal(Vector2 localPoint)`](#m-electron2d-node-toglobal-electron2d-vector2) | Transforms a point from this node's local coordinates to hierarchy-global coordinates. |
| [`public Vector2 ToLocal(Vector2 globalPoint)`](#m-electron2d-node-tolocal-electron2d-vector2) | Transforms a point from hierarchy-global coordinates to this node's local coordinates. |
| [`public Transform GetRelativeTransformToParent(Node parent)`](#m-electron2d-node-getrelativetransformtoparent-electron2d-node) | Returns this node's transform relative to an ancestor. |
| [`protected virtual Func<Node> CreateSceneInstanceFactory()`](#m-electron2d-node-createsceneinstancefactory) | Creates a reusable factory for packed-scene instances of this exact runtime node type. |
| [`protected virtual void OnEnterTree()`](#m-electron2d-node-onentertree) | Called synchronously when this node enters an active scene tree. |
| [`protected virtual void OnExitTree()`](#m-electron2d-node-onexittree) | Called synchronously when this node exits an active scene tree. |
| [`protected virtual void OnReady()`](#m-electron2d-node-onready) | Called synchronously when this node receives SceneTree-managed ready delivery. |
| [`protected virtual void OnProcess(double delta)`](#m-electron2d-node-onprocess-system-double) | Called during an eligible host-driven process frame. |
| [`protected virtual void OnPhysicsProcess(double delta)`](#m-electron2d-node-onphysicsprocess-system-double) | Called during an eligible host-driven physics-process frame. |
| [`protected virtual void OnInput(InputEvent event)`](#m-electron2d-node-oninput-electron2d-inputevent) | Receives an input event during the first scene-input propagation stage. |
| [`protected virtual void OnUnhandledKeyInput(InputEventKey event)`](#m-electron2d-node-onunhandledkeyinput-electron2d-inputeventkey) | Receives a keyboard event that remains unhandled after the first input stage. |
| [`protected virtual void OnUnhandledInput(InputEvent event)`](#m-electron2d-node-onunhandledinput-electron2d-inputevent) | Receives an event that remains unhandled after earlier scene-input stages. |
| [`protected override void OnNotification(int what)`](#m-electron2d-node-onnotification-system-int32) | Handles an engine notification delivered to this object. |
| [`protected override void ValidateMutation()`](#m-electron2d-node-validatemutation) | Validates that mutable base state may change at the current lifecycle point. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-node-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |
| [`protected override void ValidateDisposal()`](#m-electron2d-node-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-node-dispose-system-boolean) | Releases resources owned by a derived class. |
| [`protected void EnsureMutable()`](#m-electron2d-node-ensuremutable) | Validates that this node may be mutated at the current lifecycle point. |

## Events

| Member | Description |
| --- | --- |
| [`public event Action<Node, Node> ChildAdded`](#e-electron2d-node-childadded) | Occurs on the parent after a direct child is structurally attached and child order is reported. |
| [`public event Action<Node, Node> ChildRemoved`](#e-electron2d-node-childremoved) | Occurs on the former parent after a direct child is detached and child order is reported. |
| [`public event Action<Node, Node> ChildEnteredTree`](#e-electron2d-node-childenteredtree) | Occurs on the direct parent when a child enters the active tree. |
| [`public event Action<Node, Node> ChildExitingTree`](#e-electron2d-node-childexitingtree) | Occurs on the direct parent while a child is exiting the active tree. |
| [`public event Action<Node> ChildOrderChanged`](#e-electron2d-node-childorderchanged) | Occurs after the order or membership of direct children changes. |
| [`public event Action<Node> Renamed`](#e-electron2d-node-renamed) | Occurs after an active node's own name changes and path notifications propagate. |
| [`public event Action<Node> TreeEntered`](#e-electron2d-node-treeentered) | Occurs when this node enters an active scene tree. |
| [`public event Action<Node> TreeExiting`](#e-electron2d-node-treeexiting) | Occurs while this node is exiting its active scene tree. |
| [`public event Action<Node> TreeExited`](#e-electron2d-node-treeexited) | Occurs after this node has left its scene tree. |
| [`public event Action<Node> Ready`](#e-electron2d-node-ready) | Occurs after child-first ready notification delivery. |
| [`public event Action<Node> VisibilityChanged`](#e-electron2d-node-visibilitychanged) | Occurs after local or inherited logical visibility is propagated to this node. |
| [`public event Action<Node> LocalTransformChanged`](#e-electron2d-node-localtransformchanged) | Occurs after this node's local transform actually changes. |
| [`public event Action<Node> TransformChanged`](#e-electron2d-node-transformchanged) | Occurs when this node's global transform is affected by a local or ancestor change. |

## Constants

| Member | Description |
| --- | --- |
| [`public const int NotificationEnterTree = 10`](#f-electron2d-node-notificationentertree) | Identifies the notification sent when a node enters an active [`SceneTree`](SceneTree.md). |
| [`public const int NotificationExitTree = 11`](#f-electron2d-node-notificationexittree) | Identifies the notification sent after descendants exit and before this node leaves its tree. |
| [`public const int NotificationReady = 13`](#f-electron2d-node-notificationready) | Identifies the child-first notification sent when a node becomes ready. |
| [`public const int NotificationPaused = 14`](#f-electron2d-node-notificationpaused) | Identifies the notification sent when the owning tree becomes paused. |
| [`public const int NotificationUnpaused = 15`](#f-electron2d-node-notificationunpaused) | Identifies the notification sent when the owning tree resumes from pause. |
| [`public const int NotificationPhysicsProcess = 16`](#f-electron2d-node-notificationphysicsprocess) | Identifies a physics-process callback notification. |
| [`public const int NotificationProcess = 17`](#f-electron2d-node-notificationprocess) | Identifies a process callback notification. |
| [`public const int NotificationParented = 18`](#f-electron2d-node-notificationparented) | Identifies the notification sent after a parent reference is assigned. |
| [`public const int NotificationUnparented = 19`](#f-electron2d-node-notificationunparented) | Identifies the notification sent after a parent reference is cleared. |
| [`public const int NotificationSceneInstantiated = 20`](#f-electron2d-node-notificationsceneinstantiated) | Identifies the notification sent to the root after a packed scene is completely instantiated. |
| [`public const int NotificationPathRenamed = 23`](#f-electron2d-node-notificationpathrenamed) | Identifies the notification propagated when this node's path changes. |
| [`public const int NotificationChildOrderChanged = 24`](#f-electron2d-node-notificationchildorderchanged) | Identifies the notification sent after the direct child order changes. |
| [`public const int NotificationInternalProcess = 25`](#f-electron2d-node-notificationinternalprocess) | Identifies an engine-internal process callback notification. |
| [`public const int NotificationInternalPhysicsProcess = 26`](#f-electron2d-node-notificationinternalphysicsprocess) | Identifies an engine-internal physics-process callback notification. |
| [`public const int NotificationPostEnterTree = 27`](#f-electron2d-node-notificationpostentertree) | Identifies the notification sent after this node and its descendants finish entering a tree. |
| [`public const int NotificationDisabled = 28`](#f-electron2d-node-notificationdisabled) | Identifies the notification sent when the effective process mode becomes disabled. |
| [`public const int NotificationEnabled = 29`](#f-electron2d-node-notificationenabled) | Identifies the notification sent when the effective process mode stops being disabled. |
| [`public const int NotificationVisibilityChanged = 31`](#f-electron2d-node-notificationvisibilitychanged) | Identifies the notification propagated after local or inherited visibility changes. |
| [`public const int NotificationLocalTransformChanged = 35`](#f-electron2d-node-notificationlocaltransformchanged) | Identifies a local-transform change notification when local notification delivery is enabled. |
| [`public const int NotificationTransformChanged = 2000`](#f-electron2d-node-notificationtransformchanged) | Identifies a global-transform change notification when global notification delivery is enabled. |
| [`public const int NotificationOsMemoryWarning = 2009`](#f-electron2d-node-notificationosmemorywarning) | Identifies an operating-system low-memory warning propagated by the active scene tree. |
| [`public const int NotificationTranslationChanged = 2010`](#f-electron2d-node-notificationtranslationchanged) | Identifies a notification that translated messages may have changed. |
| [`public const int NotificationWmAbout = 2011`](#f-electron2d-node-notificationwmabout) | Identifies an operating-system request to show application information. |
| [`public const int NotificationCrash = 2012`](#f-electron2d-node-notificationcrash) | Identifies a notification delivered immediately before an unrecoverable crash. |
| [`public const int NotificationOsImeUpdate = 2013`](#f-electron2d-node-notificationosimeupdate) | Identifies an input-method composition update supplied by the operating system. |
| [`public const int NotificationApplicationResumed = 2014`](#f-electron2d-node-notificationapplicationresumed) | Identifies that the application resumed after suspension. |
| [`public const int NotificationApplicationPaused = 2015`](#f-electron2d-node-notificationapplicationpaused) | Identifies that the application is about to be suspended. |
| [`public const int NotificationApplicationFocusIn = 2016`](#f-electron2d-node-notificationapplicationfocusin) | Identifies that the application received keyboard focus. |
| [`public const int NotificationApplicationFocusOut = 2017`](#f-electron2d-node-notificationapplicationfocusout) | Identifies that the application lost keyboard focus. |
| [`public const int NotificationTextServerChanged = 2018`](#f-electron2d-node-notificationtextserverchanged) | Identifies that the active text service changed. |
| [`public const int NotificationApplicationPipModeEntered = 2019`](#f-electron2d-node-notificationapplicationpipmodeentered) | Identifies that the application entered picture-in-picture mode. |
| [`public const int NotificationApplicationPipModeExited = 2020`](#f-electron2d-node-notificationapplicationpipmodeexited) | Identifies that the application exited picture-in-picture mode. |
| [`public const int MinimumZIndex = -4096`](#f-electron2d-node-minimumzindex) | Specifies the smallest supported local or effective Z index. |
| [`public const int MaximumZIndex = 4095`](#f-electron2d-node-maximumzindex) | Specifies the largest supported local or effective Z index. |

## Constructor Descriptions

<a id="m-electron2d-node-ctor"></a>
### `public Node()`

Initializes a detached node with its runtime class name and an identity transform.

## Property Descriptions

<a id="p-electron2d-node-name"></a>
### `public string Name { get; set; }`

Gets or sets the node name used in sibling lookup and paths.

**Value:** The nonblank name, initialized to [`ElectronObject.ClassName`](ElectronObject.md#p-electron2d-electronobject-classname).

**Exceptions**

- `ArgumentException`: The assigned name is invalid.
- `InvalidOperationException`: A sibling already has the assigned name, or an attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.
- `AggregateException`: One or more path, node-renamed, or tree-renamed callbacks fail after the name changes.

**Remarks:** Names use ordinal equality among siblings and cannot be `.`, `..`, or contain `/`. Renaming an
active node propagates [`Node.NotificationPathRenamed`](Node.md#f-electron2d-node-notificationpathrenamed) through its subtree and then raises
[`Node.Renamed`](Node.md#e-electron2d-node-renamed) on this node.

<a id="p-electron2d-node-parent"></a>
### `public Node Parent { get; }`

Gets the direct parent.

**Value:** The owning parent, or `null` while detached.

<a id="p-electron2d-node-scenefilepath"></a>
### `public string SceneFilePath { get; }`

Gets the external resource path from which this scene root was instantiated.

**Value:** The packed-scene path for an instantiated external scene root; otherwise an empty string.

**Remarks:** The value is assigned by packed-scene instantiation and is not inherited by descendants.

<a id="p-electron2d-node-owner"></a>
### `public Node Owner { get; set; }`

Gets or sets the ancestor that owns this node for packed-scene storage.

**Value:** An ancestor node, or `null` when this node is not stored by an ancestor scene root.

**Exceptions**

- `ArgumentException`: The assigned node is this node or is not an ancestor.
- `InvalidOperationException`: An attached node is mutated off the tree owner thread or scene capture is active.
- `ObjectDisposedException`: This node or the assigned owner is disposing or disposed.

**Remarks:** A scene root does not own itself. Removing or reparenting a subtree automatically clears owner references that
no longer point to an ancestor.

<a id="p-electron2d-node-children"></a>
### `public IReadOnlyList<Node> Children { get; }`

Gets a live read-only view of the ordered direct children.

**Value:** A view backed by this node's child list; later hierarchy changes are visible through it.

<a id="p-electron2d-node-childcount"></a>
### `public int ChildCount { get; }`

Gets the number of direct children.

**Value:** The current child count.

**Exceptions**

- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-tree"></a>
### `public SceneTree Tree { get; }`

Gets the active scene tree containing this node.

**Value:** The owning tree, or `null` while detached.

<a id="p-electron2d-node-isinsidetree"></a>
### `public bool IsInsideTree { get; }`

Gets whether this node currently belongs to a scene tree.

**Value:** `true` when [`Node.Tree`](Node.md#p-electron2d-node-tree) is non-null.

<a id="p-electron2d-node-isnodeready"></a>
### `public bool IsNodeReady { get; }`

Gets whether SceneTree-managed ready delivery has occurred since construction or the last ready reset.

**Value:** The stored ready state. It remains true after detachment until [`Node.RequestReady`](Node.md#m-electron2d-node-requestready) is called.

**Remarks:** Manual [`ElectronObject.Notify(Int32)`](ElectronObject.md#m-electron2d-electronobject-notify-system-int32) delivery of [`Node.NotificationReady`](Node.md#f-electron2d-node-notificationready) does not change this value.

<a id="p-electron2d-node-isqueuedfordeletion"></a>
### `public bool IsQueuedForDeletion { get; }`

Gets whether deletion has been requested through [`Node.QueueFree`](Node.md#m-electron2d-node-queuefree).

**Value:** An atomic snapshot of the deletion-request flag.

<a id="p-electron2d-node-transform"></a>
### `public Transform Transform { get; set; }`

Gets or sets the affine transform relative to the parent.

**Value:** A finite [`Transform`](Transform.md); the default is [`Transform.Identity`](Transform.md#p-electron2d-transform-identity).

**Exceptions**

- `ArgumentOutOfRangeException`: An assigned transform component is NaN or infinite.
- `InvalidOperationException`: An attached node is mutated from a thread other than the tree owner.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.
- `Exception`: A transform notification or event handler throws after the transform changes.

<a id="p-electron2d-node-globaltransform"></a>
### `public Transform GlobalTransform { get; set; }`

Gets or sets the affine transform in hierarchy-global coordinates.

**Value:** The local transform composed with non-top-level ancestors.

**Exceptions**

- `ArgumentOutOfRangeException`: An assigned transform component is NaN or infinite.
- `InvalidOperationException`: The parent transform is singular, or an attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node or an ancestor is disposing on another thread, or has finished disposing.
- `Exception`: A transform notification or event handler throws after the transform changes.

<a id="p-electron2d-node-position"></a>
### `public Vector2 Position { get; set; }`

Gets or sets local translation in pixels or other host-defined 2D units.

**Value:** The translation component of [`Node.Transform`](Node.md#p-electron2d-node-transform).

**Exceptions**

- `ArgumentOutOfRangeException`: An assigned component is NaN or infinite.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.
- `Exception`: A transform notification or event handler throws after the position changes.

<a id="p-electron2d-node-globalposition"></a>
### `public Vector2 GlobalPosition { get; set; }`

Gets or sets translation in hierarchy-global coordinates.

**Value:** The translation component of [`Node.GlobalTransform`](Node.md#p-electron2d-node-globaltransform).

**Exceptions**

- `ArgumentOutOfRangeException`: An assigned component is NaN or infinite.
- `InvalidOperationException`: The parent transform is singular, or an attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node or an ancestor is disposing on another thread, or has finished disposing.
- `Exception`: A transform notification or event handler throws after the position changes.

<a id="p-electron2d-node-rotation"></a>
### `public float Rotation { get; set; }`

Gets or sets local rotation in radians.

**Value:** The canonical rotation decomposed from [`Node.Transform`](Node.md#p-electron2d-node-transform).

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned angle is NaN or infinite.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.
- `Exception`: A transform notification or event handler throws after the rotation changes.

<a id="p-electron2d-node-rotationdegrees"></a>
### `public float RotationDegrees { get; set; }`

Gets or sets local rotation in degrees.

**Value:** [`Node.Rotation`](Node.md#p-electron2d-node-rotation) converted between radians and degrees.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned angle is NaN or infinite.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.
- `Exception`: A transform notification or event handler throws after the rotation changes.

<a id="p-electron2d-node-globalrotation"></a>
### `public float GlobalRotation { get; set; }`

Gets or sets hierarchy-global rotation in radians.

**Value:** The canonical rotation decomposed from [`Node.GlobalTransform`](Node.md#p-electron2d-node-globaltransform).

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned angle is NaN or infinite.
- `InvalidOperationException`: The parent transform is singular, or an attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node or an ancestor is disposing on another thread, or has finished disposing.
- `Exception`: A transform notification or event handler throws after the rotation changes.

<a id="p-electron2d-node-globalrotationdegrees"></a>
### `public float GlobalRotationDegrees { get; set; }`

Gets or sets hierarchy-global rotation in degrees.

**Value:** [`Node.GlobalRotation`](Node.md#p-electron2d-node-globalrotation) converted between radians and degrees.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned angle is NaN or infinite.
- `InvalidOperationException`: The parent transform is singular, or an attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node or an ancestor is disposing on another thread, or has finished disposing.
- `Exception`: A transform notification or event handler throws after the rotation changes.

<a id="p-electron2d-node-scale"></a>
### `public Vector2 Scale { get; set; }`

Gets or sets local scale.

**Value:** The canonical scale decomposed from [`Node.Transform`](Node.md#p-electron2d-node-transform).

**Exceptions**

- `ArgumentOutOfRangeException`: An assigned component is NaN or infinite.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.
- `Exception`: A transform notification or event handler throws after the scale changes.

**Remarks:** Equivalent reflected matrices can decompose to a different but equivalent rotation, scale, and skew tuple.

<a id="p-electron2d-node-globalscale"></a>
### `public Vector2 GlobalScale { get; set; }`

Gets or sets hierarchy-global scale.

**Value:** The canonical scale decomposed from [`Node.GlobalTransform`](Node.md#p-electron2d-node-globaltransform).

**Exceptions**

- `ArgumentOutOfRangeException`: An assigned component is NaN or infinite.
- `InvalidOperationException`: The parent transform is singular, or an attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node or an ancestor is disposing on another thread, or has finished disposing.
- `Exception`: A transform notification or event handler throws after the scale changes.

**Remarks:** Equivalent reflected matrices can decompose to a different but equivalent rotation, scale, and skew tuple.

<a id="p-electron2d-node-skew"></a>
### `public float Skew { get; set; }`

Gets or sets the local skew angle in radians.

**Value:** The canonical angle between the transformed basis axes relative to an unskewed basis.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned angle is NaN or infinite.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.
- `Exception`: A transform notification or event handler throws after the skew changes.

<a id="p-electron2d-node-globalskew"></a>
### `public float GlobalSkew { get; set; }`

Gets or sets the hierarchy-global skew angle in radians.

**Value:** The canonical skew decomposed from [`Node.GlobalTransform`](Node.md#p-electron2d-node-globaltransform).

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned angle is NaN or infinite.
- `InvalidOperationException`: The parent transform is singular, or an attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node or an ancestor is disposing on another thread, or has finished disposing.
- `Exception`: A transform notification or event handler throws after the skew changes.

<a id="p-electron2d-node-toplevel"></a>
### `public bool TopLevel { get; set; }`

Gets or sets whether this node ignores its parent's transform.

**Value:** `false` by default.

**Exceptions**

- `InvalidOperationException`: The parent transform is singular when disabling top-level mode, or mutation occurs off the owner thread.
- `ObjectDisposedException`: This node or an ancestor is disposing on another thread, or has finished disposing.
- `Exception`: A transform notification or event handler throws after the mode changes.

**Remarks:** The current global transform is preserved when the mode changes.

<a id="p-electron2d-node-visible"></a>
### `public bool Visible { get; set; }`

Gets or sets this node's local logical visibility.

**Value:** `true` by default.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.
- `Exception`: A visibility notification or event handler throws after visibility changes.

**Remarks:** An actual change synchronously propagates visibility notifications and events through all descendants.

<a id="p-electron2d-node-isvisibleintree"></a>
### `public bool IsVisibleInTree { get; }`

Gets whether this node is active and locally visible through its complete ancestor chain.

**Value:** `true` only inside a tree when this node and every ancestor are visible.

**Exceptions**

- `ObjectDisposedException`: This node or a queried ancestor is disposing on another thread, or has finished disposing.

<a id="p-electron2d-node-zindex"></a>
### `public int ZIndex { get; set; }`

Gets or sets this node's local Z-order value.

**Value:** An integer from [`Node.MinimumZIndex`](Node.md#f-electron2d-node-minimumzindex) through [`Node.MaximumZIndex`](Node.md#f-electron2d-node-maximumzindex); the default is zero.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned value is outside the supported range.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-zasrelative"></a>
### `public bool ZAsRelative { get; set; }`

Gets or sets whether effective Z order accumulates ancestor Z values.

**Value:** `true` by default.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-notifylocaltransformchanges"></a>
### `public bool NotifyLocalTransformChanges { get; set; }`

Gets or sets whether local transform changes dispatch [`Node.NotificationLocalTransformChanged`](Node.md#f-electron2d-node-notificationlocaltransformchanged).

**Value:** `false` by default. [`Node.LocalTransformChanged`](Node.md#e-electron2d-node-localtransformchanged) is raised regardless.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-notifytransformchanges"></a>
### `public bool NotifyTransformChanges { get; set; }`

Gets or sets whether global transform changes dispatch [`Node.NotificationTransformChanged`](Node.md#f-electron2d-node-notificationtransformchanged).

**Value:** `false` by default. [`Node.TransformChanged`](Node.md#e-electron2d-node-transformchanged) is raised regardless.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-processmode"></a>
### `public NodeProcessMode ProcessMode { get; set; }`

Gets or sets the pause policy used by both process callback lanes.

**Value:** [`NodeProcessMode.Inherit`](NodeProcessMode.md#f-electron2d-nodeprocessmode-inherit) by default.

**Exceptions**

- `ArgumentOutOfRangeException`: The assigned enum value is undefined.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.
- `Exception`: An enabled or disabled notification callback throws after the mode changes.

**Remarks:** Crossing the effective disabled boundary synchronously notifies this node and affected inheriting descendants.

<a id="p-electron2d-node-processenabled"></a>
### `public bool ProcessEnabled { get; set; }`

Gets or sets whether this node participates in host-driven process frames.

**Value:** `false` by default.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-physicsprocessenabled"></a>
### `public bool PhysicsProcessEnabled { get; set; }`

Gets or sets whether this node participates in host-driven physics-process frames.

**Value:** `false` by default.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-inputenabled"></a>
### `public bool InputEnabled { get; set; }`

Gets or sets whether this node receives the first input-propagation stage.

**Value:** `false` by default.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.

**Remarks:** Eligible nodes are visited in reverse depth-first order before unhandled-input stages.

<a id="p-electron2d-node-unhandledinputenabled"></a>
### `public bool UnhandledInputEnabled { get; set; }`

Gets or sets whether this node receives input left unhandled by earlier stages.

**Value:** `false` by default.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.

**Remarks:** This final stage runs for every event that remains unhandled.

<a id="p-electron2d-node-unhandledkeyinputenabled"></a>
### `public bool UnhandledKeyInputEnabled { get; set; }`

Gets or sets whether this node receives unhandled keyboard events before general unhandled input.

**Value:** `false` by default.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.

**Remarks:** The stage is skipped for non-keyboard events and after [`SceneTree.SetInputAsHandled`](SceneTree.md#m-electron2d-scenetree-setinputashandled).

<a id="p-electron2d-node-processpriority"></a>
### `public int ProcessPriority { get; set; }`

Gets or sets this node's ascending process-frame order key.

**Value:** Any integer; the default is zero. Equal priorities retain captured tree order.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-physicsprocesspriority"></a>
### `public int PhysicsProcessPriority { get; set; }`

Gets or sets this node's ascending physics-process order key.

**Value:** Any integer; the default is zero. Equal priorities retain captured tree order.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-processdeltatime"></a>
### `public double ProcessDeltaTime { get; }`

Gets the delta from the most recent SceneTree-managed process frame delivered to this node.

**Value:** The last delivered process delta in seconds, or zero before the first managed delivery.

**Remarks:** [`Engine`](Engine.md) applies [`Engine.TimeScale`](Engine.md#p-electron2d-engine-timescale) before an Engine-driven delivery.

<a id="p-electron2d-node-physicsprocessdeltatime"></a>
### `public double PhysicsProcessDeltaTime { get; }`

Gets the delta from the most recent SceneTree-managed physics-process frame delivered to this node.

**Value:** The last delivered physics-process delta in seconds, or zero before the first managed delivery.

**Remarks:** [`Engine`](Engine.md) applies [`Engine.TimeScale`](Engine.md#p-electron2d-engine-timescale) before an Engine-driven delivery.

## Method Descriptions

<a id="m-electron2d-node-addchild-electron2d-node"></a>
### `public void AddChild(Node child)`

Appends a detached node as the last direct child.

**Parameters**

- `child`: The live node to adopt.

**Exceptions**

- `ArgumentNullException`: `child` is `null`.
- `ArgumentException`: `child` is this node.
- `InvalidOperationException`: The operation would create a cycle, the child already has a parent or tree, a sibling name conflicts, or mutation
occurs off the owner thread.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing, or disposal of `child` has started.
- `AggregateException`: One or more structural, lifecycle, notification, or event callbacks fail after insertion begins.

**Remarks:** If this node is active, the child's subtree enters immediately and receives ready where eligible.

<a id="m-electron2d-node-addsibling-electron2d-node"></a>
### `public void AddSibling(Node sibling)`

Inserts a detached node immediately after this node in its parent's child order.

**Parameters**

- `sibling`: The live node to insert.

**Exceptions**

- `ArgumentNullException`: `sibling` is `null`.
- `ArgumentException`: `sibling` is the destination parent.
- `InvalidOperationException`: This node has no parent, insertion would create a cycle, the sibling is already attached, a name conflicts, or
mutation occurs off the owner thread.
- `ObjectDisposedException`: This node or its parent is disposing on another thread or has finished disposing, or disposal of
`sibling` has started.
- `AggregateException`: One or more structural, lifecycle, notification, or event callbacks fail after insertion begins.

<a id="m-electron2d-node-removechild-electron2d-node"></a>
### `public bool RemoveChild(Node child)`

Removes a direct child without disposing it.

**Parameters**

- `child`: The node to detach.

**Returns:** `true` when the node was a direct child and was detached; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `child` is `null`.
- `InvalidOperationException`: An attached node is mutated off the owner thread, this parent is exiting, or the child is in tree lifecycle delivery.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.
- `AggregateException`: One or more lifecycle, notification, or event callbacks fail after removal begins.

**Remarks:** An active subtree exits its tree child-first before the parent reference is cleared.

<a id="m-electron2d-node-movechild-electron2d-node-system-int32"></a>
### `public void MoveChild(Node child, int index)`

Moves a direct child to another sibling index.

**Parameters**

- `child`: The direct child to reorder.
- `index`: The destination index; negative values count from the end, with `-1` selecting the last position.

**Exceptions**

- `ArgumentNullException`: `child` is `null`.
- `ArgumentException`: `child` is not a direct child.
- `ArgumentOutOfRangeException`: `index` does not resolve to an existing child position.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.
- `AggregateException`: One or more child-order or tree-change callbacks fail after the order changes.

<a id="m-electron2d-node-movetofront"></a>
### `public void MoveToFront()`

Moves this node to the last position among its siblings.

**Exceptions**

- `InvalidOperationException`: An attached parent is mutated off the owner thread.
- `ObjectDisposedException`: This node or its parent is disposing on another thread or has finished disposing.
- `AggregateException`: One or more child-order or tree-change callbacks fail after the order changes.

**Remarks:** A detached or hierarchy-root node is left unchanged.

<a id="m-electron2d-node-reparent-electron2d-node-system-boolean"></a>
### `public void Reparent(Node newParent, bool keepGlobalTransform = true)`

Moves this non-root node under a new parent.

**Parameters**

- `newParent`: The live destination parent.
- `keepGlobalTransform`: Whether to preserve the complete current global transform. The default is `true`.

**Exceptions**

- `ArgumentNullException`: `newParent` is `null`.
- `ArgumentException`: The destination validation rejects this node as its own child.
- `InvalidOperationException`: This node has no parent, the move creates a cycle, a destination child name conflicts, either attached hierarchy
is accessed off its owner thread, this node or its current parent is in protected tree lifecycle delivery, or the destination parent transform is singular while
`keepGlobalTransform` is true.
- `ObjectDisposedException`: This node or `newParent` is disposing on another thread or has finished disposing.
- `AggregateException`: One or more structural, lifecycle, notification, or event callbacks fail after reparenting begins.

**Remarks:** The operation detaches first and then appends to `newParent`; callback failures are not rolled back.

<a id="m-electron2d-node-getchild-system-int32"></a>
### `public Node GetChild(int index)`

Gets a direct child by index.

**Parameters**

- `index`: The child index; negative values count from the end.

**Returns:** The selected direct child.

**Exceptions**

- `ArgumentOutOfRangeException`: This node has no children or `index` is outside the valid range.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getindex"></a>
### `public int GetIndex()`

Gets this node's index in its parent's ordered child list.

**Returns:** The zero-based sibling index, or `-1` when this node has no parent.

**Exceptions**

- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-isancestorof-electron2d-node"></a>
### `public bool IsAncestorOf(Node node)`

Determines whether this node is a strict ancestor of another node.

**Parameters**

- `node`: The node whose parent chain is inspected.

**Returns:** `true` when this node appears in the parent chain; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `node` is `null`.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-findchild-system-string-system-boolean"></a>
### `public Node FindChild(string pattern, bool recursive = true)`

Finds the first descendant whose name matches a wildcard pattern.

**Parameters**

- `pattern`: A nonblank simple expression using `*` and `?`; matching is case-insensitive.
- `recursive`: Whether descendants below direct children are searched.

**Returns:** The first matching node in depth-first pre-order, or `null`.

**Exceptions**

- `ArgumentException`: `pattern` is empty or whitespace.
- `ArgumentNullException`: `pattern` is `null`.
- `ObjectDisposedException`: This node or a recursively searched node is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-findchild-1-system-string-system-boolean"></a>
### `public TNode FindChild<TNode>(string pattern = "*", bool recursive = true)`

Finds the first descendant of a requested type whose name matches a wildcard pattern.

**Type parameters**

- `TNode`: The required node subtype.

**Parameters**

- `pattern`: A nonblank simple expression using `*` and `?`; matching is case-insensitive.
- `recursive`: Whether descendants below direct children are searched.

**Returns:** The first typed match in depth-first pre-order, or `null`.

**Exceptions**

- `ArgumentException`: `pattern` is empty or whitespace.
- `ArgumentNullException`: `pattern` is `null`.
- `ObjectDisposedException`: This node or a recursively searched node is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-findchildren-system-string-system-boolean"></a>
### `public IReadOnlyList<Node> FindChildren(string pattern, bool recursive = true)`

Finds all descendants whose names match a wildcard pattern.

**Parameters**

- `pattern`: A nonblank simple expression using `*` and `?`; matching is case-insensitive.
- `recursive`: Whether descendants below direct children are searched.

**Returns:** A read-only snapshot in depth-first pre-order.

**Exceptions**

- `ArgumentException`: `pattern` is empty or whitespace.
- `ArgumentNullException`: `pattern` is `null`.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-findchildren-1-system-string-system-boolean"></a>
### `public IReadOnlyList<TNode> FindChildren<TNode>(string pattern = "*", bool recursive = true)`

Finds all descendants of a requested type whose names match a wildcard pattern.

**Type parameters**

- `TNode`: The required node subtype.

**Parameters**

- `pattern`: A nonblank simple expression using `*` and `?`; matching is case-insensitive.
- `recursive`: Whether descendants below direct children are searched.

**Returns:** A read-only typed snapshot in depth-first pre-order.

**Exceptions**

- `ArgumentException`: `pattern` is empty or whitespace.
- `ArgumentNullException`: `pattern` is `null`.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-findparent-system-string"></a>
### `public Node FindParent(string pattern)`

Finds the nearest ancestor whose name matches a wildcard pattern.

**Parameters**

- `pattern`: A nonblank simple expression using `*` and `?`; matching is case-insensitive.

**Returns:** The nearest matching ancestor, or `null`.

**Exceptions**

- `ArgumentException`: `pattern` is empty or whitespace.
- `ArgumentNullException`: `pattern` is `null`.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getpath"></a>
### `public string GetPath()`

Builds this node's absolute path from the root of its current hierarchy.

**Returns:** A slash-prefixed path that includes the hierarchy root name.

**Exceptions**

- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

**Remarks:** The path is available for both attached and detached hierarchies.

<a id="m-electron2d-node-getpathto-electron2d-node"></a>
### `public string GetPathTo(Node node)`

Builds a relative path from this node to another node in the same hierarchy.

**Parameters**

- `node`: The destination node.

**Returns:** `.` for this node, otherwise a slash-separated sequence of `..` and child names.

**Exceptions**

- `ArgumentNullException`: `node` is `null`.
- `InvalidOperationException`: The nodes do not share a hierarchy root.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing, or disposal of `node` has started.

<a id="m-electron2d-node-getnode-system-string"></a>
### `public Node GetNode(string path)`

Resolves a required relative or absolute node path.

**Parameters**

- `path`: A nonblank slash-separated path supporting `.`, `..`, and an optional absolute root-name segment.

**Returns:** The resolved node.

**Exceptions**

- `ArgumentException`: `path` is empty or whitespace.
- `ArgumentNullException`: `path` is `null`.
- `Collections.Generic.KeyNotFoundException`: No node exists at the requested path.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

**Remarks:** Absolute paths are resolved from the hierarchy root even when the hierarchy is detached.

<a id="m-electron2d-node-getnode-1-system-string"></a>
### `public TNode GetNode<TNode>(string path)`

Resolves a required relative or absolute path to a requested node type.

**Type parameters**

- `TNode`: The required node subtype.

**Parameters**

- `path`: A nonblank slash-separated node path.

**Returns:** The resolved node cast to `TNode`.

**Exceptions**

- `ArgumentException`: `path` is empty or whitespace.
- `ArgumentNullException`: `path` is `null`.
- `InvalidCastException`: The resolved node is not a `TNode`.
- `Collections.Generic.KeyNotFoundException`: No node exists at the requested path.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getnodeornull-system-string"></a>
### `public Node GetNodeOrNull(string path)`

Attempts to resolve a relative or absolute node path.

**Parameters**

- `path`: A nonblank slash-separated path supporting `.`, `..`, and an optional absolute root-name segment.

**Returns:** The resolved node, or `null` when traversal cannot continue.

**Exceptions**

- `ArgumentException`: `path` is empty or whitespace.
- `ArgumentNullException`: `path` is `null`.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

**Remarks:** Absolute paths are resolved from the hierarchy root even when the hierarchy is detached.

<a id="m-electron2d-node-addtogroup-system-string-system-boolean"></a>
### `public void AddToGroup(string group, bool persistent = false)`

Adds this node to a case-sensitive group.

**Parameters**

- `group`: The nonblank group name.
- `persistent`: Whether packed scenes containing this node should retain the membership.

**Exceptions**

- `ArgumentException`: `group` is empty or whitespace.
- `ArgumentNullException`: `group` is `null`.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

**Remarks:** Adding an existing membership keeps it persistent once persistence has been requested.

<a id="m-electron2d-node-removefromgroup-system-string"></a>
### `public bool RemoveFromGroup(string group)`

Removes this node from a case-sensitive group.

**Parameters**

- `group`: The nonblank group name.

**Returns:** `true` when membership existed and was removed; otherwise `false`.

**Exceptions**

- `ArgumentException`: `group` is empty or whitespace.
- `ArgumentNullException`: `group` is `null`.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-isingroup-system-string"></a>
### `public bool IsInGroup(string group)`

Determines whether this node belongs to a case-sensitive group.

**Parameters**

- `group`: The nonblank group name.

**Returns:** `true` when this node is a member; otherwise `false`.

**Exceptions**

- `ArgumentException`: `group` is empty or whitespace.
- `ArgumentNullException`: `group` is `null`.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getgroups"></a>
### `public IReadOnlyList<string> GetGroups()`

Returns this node's group memberships.

**Returns:** A read-only snapshot sorted using ordinal string order.

**Exceptions**

- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-canprocess"></a>
### `public bool CanProcess()`

Determines whether the resolved process mode allows callbacks in the current tree pause state.

**Returns:** `false` while detached or disabled; otherwise the result of the resolved pause policy.

**Exceptions**

- `InvalidOperationException`: An invalid inherited process mode cannot be resolved.
- `ObjectDisposedException`: This node or its tree is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-requestready"></a>
### `public void RequestReady()`

Requests ready delivery the next time SceneTree attachment reaches the ready phase.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

**Remarks:** The method only resets stored ready state; it never delivers ready immediately.

<a id="m-electron2d-node-createtween"></a>
### `public Tween CreateTween()`

Creates a tween in this node's scene tree and binds it to this node.

**Returns:** A running empty tween that halts while this node is detached and is killed when this node is disposed.

**Exceptions**

- `InvalidOperationException`: The node is detached or the call is made off the tree owner thread.
- `ObjectDisposedException`: The node or its tree is disposing or disposed.

**Remarks:** The caller must append at least one tweener before the next matching frame, including a zero-delta frame.

<a id="m-electron2d-node-queuefree"></a>
### `public void QueueFree()`

Atomically requests this node's deferred disposal at a future scene-tree safe point.

**Exceptions**

- `InvalidOperationException`: This node is the active scene-tree root.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

**Remarks:** A request made while already detached is queued if the node later enters a tree. Removing the node before its
current tree flushes does not cancel deletion: that tree disposes the detached node at its safe point. If the
node has entered another tree first, the old entry leaves the request intact for the new tree. Repeated calls
are idempotent. The method may be called from a non-owner thread.

<a id="m-electron2d-node-cancelfree"></a>
### `public bool CancelFree()`

Atomically cancels a pending deletion request.

**Returns:** `true` when a request was pending; otherwise `false`.

**Exceptions**

- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.

**Remarks:** A stale queue entry may remain, but the tree ignores it when flushing.

<a id="m-electron2d-node-show"></a>
### `public void Show()`

Sets [`Node.Visible`](Node.md#p-electron2d-node-visible) to `true`.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.
- `Exception`: A visibility notification or event handler throws after visibility changes.

<a id="m-electron2d-node-hide"></a>
### `public void Hide()`

Sets [`Node.Visible`](Node.md#p-electron2d-node-visible) to `false`.

**Exceptions**

- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.
- `Exception`: A visibility notification or event handler throws after visibility changes.

<a id="m-electron2d-node-applyscale-electron2d-vector2"></a>
### `public void ApplyScale(Vector2 ratio)`

Component-multiplies the local scale by a ratio.

**Parameters**

- `ratio`: The finite X and Y scale ratios.

**Exceptions**

- `ArgumentOutOfRangeException`: A ratio component is NaN or infinite.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.
- `Exception`: A transform notification or event handler throws after the scale changes.

<a id="m-electron2d-node-rotate-system-single"></a>
### `public void Rotate(float radians)`

Adds an angle to the local rotation.

**Parameters**

- `radians`: The finite angle in radians.

**Exceptions**

- `ArgumentOutOfRangeException`: `radians` is NaN or infinite.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.
- `Exception`: A transform notification or event handler throws after the rotation changes.

<a id="m-electron2d-node-translate-electron2d-vector2"></a>
### `public void Translate(Vector2 offset)`

Moves this node by an offset rotated by its local rotation.

**Parameters**

- `offset`: The finite local-space offset.

**Exceptions**

- `ArgumentOutOfRangeException`: An offset component is NaN or infinite.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.
- `Exception`: A transform notification or event handler throws after the position changes.

**Remarks:** Scale and skew do not affect the offset.

<a id="m-electron2d-node-globaltranslate-electron2d-vector2"></a>
### `public void GlobalTranslate(Vector2 offset)`

Moves this node by a hierarchy-global offset.

**Parameters**

- `offset`: The finite global-space offset.

**Exceptions**

- `ArgumentOutOfRangeException`: An offset component is NaN or infinite.
- `InvalidOperationException`: The parent transform is singular, or mutation occurs off the owner thread.
- `ObjectDisposedException`: This node or an ancestor is disposing on another thread, or has finished disposing.
- `Exception`: A transform notification or event handler throws after the position changes.

<a id="m-electron2d-node-movelocalx-system-single-system-boolean"></a>
### `public void MoveLocalX(float delta, bool scaled = false)`

Moves this node along its local X basis axis.

**Parameters**

- `delta`: The finite signed distance.
- `scaled`: Whether scale magnitude is retained. By default the axis is normalized.

**Exceptions**

- `ArgumentOutOfRangeException`: `delta` is NaN or infinite.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.
- `Exception`: A transform notification or event handler throws after the position changes.

**Remarks:** A near-zero normalized axis causes no movement.

<a id="m-electron2d-node-movelocaly-system-single-system-boolean"></a>
### `public void MoveLocalY(float delta, bool scaled = false)`

Moves this node along its local Y basis axis.

**Parameters**

- `delta`: The finite signed distance.
- `scaled`: Whether scale magnitude is retained. By default the axis is normalized.

**Exceptions**

- `ArgumentOutOfRangeException`: `delta` is NaN or infinite.
- `InvalidOperationException`: An attached node is mutated off the owner thread.
- `ObjectDisposedException`: This node is disposing on another thread or has finished disposing.
- `Exception`: A transform notification or event handler throws after the position changes.

**Remarks:** A near-zero normalized axis causes no movement.

<a id="m-electron2d-node-getangleto-electron2d-vector2"></a>
### `public float GetAngleTo(Vector2 globalPoint)`

Computes the signed angle from this node's global positive X direction to a global point.

**Parameters**

- `globalPoint`: The finite point in hierarchy-global coordinates.

**Returns:** A normalized angle in radians, or zero when the point equals [`Node.GlobalPosition`](Node.md#p-electron2d-node-globalposition).

**Exceptions**

- `ArgumentOutOfRangeException`: A point component is NaN or infinite.
- `ObjectDisposedException`: This node or an ancestor is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-lookat-electron2d-vector2"></a>
### `public void LookAt(Vector2 globalPoint)`

Rotates this node so its positive local X direction points at a global point.

**Parameters**

- `globalPoint`: The finite target point in hierarchy-global coordinates.

**Exceptions**

- `ArgumentOutOfRangeException`: A point component is NaN or infinite.
- `InvalidOperationException`: The parent transform is singular, or mutation occurs off the owner thread.
- `ObjectDisposedException`: This node or an ancestor is disposing on another thread, or has finished disposing.
- `Exception`: A transform notification or event handler throws after the rotation changes.

**Remarks:** A target equal to [`Node.GlobalPosition`](Node.md#p-electron2d-node-globalposition) leaves rotation unchanged.

<a id="m-electron2d-node-toglobal-electron2d-vector2"></a>
### `public Vector2 ToGlobal(Vector2 localPoint)`

Transforms a point from this node's local coordinates to hierarchy-global coordinates.

**Parameters**

- `localPoint`: The finite local point.

**Returns:** The point transformed by [`Node.GlobalTransform`](Node.md#p-electron2d-node-globaltransform).

**Exceptions**

- `ArgumentOutOfRangeException`: A point component is NaN or infinite.
- `ObjectDisposedException`: This node or an ancestor is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-tolocal-electron2d-vector2"></a>
### `public Vector2 ToLocal(Vector2 globalPoint)`

Transforms a point from hierarchy-global coordinates to this node's local coordinates.

**Parameters**

- `globalPoint`: The finite global point.

**Returns:** The point transformed by the inverse global transform.

**Exceptions**

- `ArgumentOutOfRangeException`: A point component is NaN or infinite.
- `InvalidOperationException`: The global transform is singular.
- `ObjectDisposedException`: This node or an ancestor is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-getrelativetransformtoparent-electron2d-node"></a>
### `public Transform GetRelativeTransformToParent(Node parent)`

Returns this node's transform relative to an ancestor.

**Parameters**

- `parent`: This node itself or a strict ancestor.

**Returns:** Identity for this node; otherwise the global transform expressed relative to `parent`.

**Exceptions**

- `ArgumentNullException`: `parent` is `null`.
- `ArgumentException`: `parent` is not an ancestor of this node.
- `InvalidOperationException`: The ancestor's global transform is singular.
- `ObjectDisposedException`: This node, `parent`, or a queried ancestor is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-createsceneinstancefactory"></a>
### `protected virtual Func<Node> CreateSceneInstanceFactory()`

Creates a reusable factory for packed-scene instances of this exact runtime node type.

**Returns:** A non-null factory that creates a fresh node of the exact same runtime type.

**Exceptions**

- `NotSupportedException`: A derived node has not explicitly supplied an instancing factory.

**Remarks:** The base implementation supports only an exact [`Node`](Node.md). Derived node types that can be packed must
return a static, non-capturing factory that remains valid after the source node is disposed and creates a live,
detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore
the instance state.

<a id="m-electron2d-node-onentertree"></a>
### `protected virtual void OnEnterTree()`

Called synchronously when this node enters an active scene tree.

**Remarks:** [`Node.Tree`](Node.md#p-electron2d-node-tree) is already assigned. The callback runs parent-first, before [`Node.TreeEntered`](Node.md#e-electron2d-node-treeentered), before
descendants enter, and on the tree owner thread during SceneTree-managed lifecycle.

<a id="m-electron2d-node-onexittree"></a>
### `protected virtual void OnExitTree()`

Called synchronously when this node exits an active scene tree.

**Remarks:** Descendants have already exited and [`Node.Tree`](Node.md#p-electron2d-node-tree) remains assigned. The callback precedes
[`Node.TreeExiting`](Node.md#e-electron2d-node-treeexiting) and runs on the tree owner thread during SceneTree-managed lifecycle.

<a id="m-electron2d-node-onready"></a>
### `protected virtual void OnReady()`

Called synchronously when this node receives SceneTree-managed ready delivery.

**Remarks:** Children are ready first. The callback precedes [`Node.Ready`](Node.md#e-electron2d-node-ready), is one-shot until [`Node.RequestReady`](Node.md#m-electron2d-node-requestready),
and runs on the owner thread during SceneTree-managed delivery. Manual notification runs on its caller's thread.

<a id="m-electron2d-node-onprocess-system-double"></a>
### `protected virtual void OnProcess(double delta)`

Called during an eligible host-driven process frame.

**Parameters**

- `delta`: The finite non-negative frame delta in seconds.

**Remarks:** The callback is not auto-enabled by overriding it; [`Node.ProcessEnabled`](Node.md#p-electron2d-node-processenabled) must be true. It executes on
the tree owner thread after [`Node.ProcessDeltaTime`](Node.md#p-electron2d-node-processdeltatime) is updated.

<a id="m-electron2d-node-onphysicsprocess-system-double"></a>
### `protected virtual void OnPhysicsProcess(double delta)`

Called during an eligible host-driven physics-process frame.

**Parameters**

- `delta`: The finite non-negative physics-step delta in seconds.

**Remarks:** The callback is not auto-enabled by overriding it; [`Node.PhysicsProcessEnabled`](Node.md#p-electron2d-node-physicsprocessenabled) must be true. It executes
on the tree owner thread after [`Node.PhysicsProcessDeltaTime`](Node.md#p-electron2d-node-physicsprocessdeltatime) is updated and does not perform simulation.

<a id="m-electron2d-node-oninput-electron2d-inputevent"></a>
### `protected virtual void OnInput(InputEvent event)`

Receives an input event during the first scene-input propagation stage.

**Parameters**

- `event`: The live caller-owned event being dispatched.

**Remarks:** The callback runs synchronously on the scene-tree owner thread when [`Node.InputEnabled`](Node.md#p-electron2d-node-inputenabled) is true and
[`Node.CanProcess`](Node.md#m-electron2d-node-canprocess) allows the node. Call [`SceneTree.SetInputAsHandled`](SceneTree.md#m-electron2d-scenetree-setinputashandled) to stop later stages.

<a id="m-electron2d-node-onunhandledkeyinput-electron2d-inputeventkey"></a>
### `protected virtual void OnUnhandledKeyInput(InputEventKey event)`

Receives a keyboard event that remains unhandled after the first input stage.

**Parameters**

- `event`: The live caller-owned keyboard event being dispatched.

**Remarks:** The callback runs synchronously on the scene-tree owner thread when [`Node.UnhandledKeyInputEnabled`](Node.md#p-electron2d-node-unhandledkeyinputenabled) is
true and [`Node.CanProcess`](Node.md#m-electron2d-node-canprocess) allows the node.

<a id="m-electron2d-node-onunhandledinput-electron2d-inputevent"></a>
### `protected virtual void OnUnhandledInput(InputEvent event)`

Receives an event that remains unhandled after earlier scene-input stages.

**Parameters**

- `event`: The live caller-owned event being dispatched.

**Remarks:** The callback runs synchronously on the scene-tree owner thread when [`Node.UnhandledInputEnabled`](Node.md#p-electron2d-node-unhandledinputenabled) is true
and [`Node.CanProcess`](Node.md#m-electron2d-node-canprocess) allows the node.

<a id="m-electron2d-node-onnotification-system-int32"></a>
### `protected override void OnNotification(int what)`

Handles an engine notification delivered to this object.

**Parameters**

- `what`: The notification identifier.

**Remarks:** Derived overrides should call the base implementation unless they intentionally suppress inherited handling.

Calls the base implementation, then maps enter, exit, ready, process, and physics-process notification IDs to
the corresponding typed virtual callbacks. Manual [`ElectronObject.Notify(Int32)`](ElectronObject.md#m-electron2d-electronobject-notify-system-int32) calls invoke those
callbacks but do not mutate tree membership, ready state, or delta values.

<a id="m-electron2d-node-validatemutation"></a>
### `protected override void ValidateMutation()`

Validates that mutable base state may change at the current lifecycle point.

**Exceptions**

- `ObjectDisposedException`: Disposal has started.

**Remarks:** Derived types may reject mutation while they are participating in an atomic operation.

<a id="m-electron2d-node-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

Appends this class's typed hierarchy, spatial, visibility, and processing descriptors to the inherited descriptors.

<a id="m-electron2d-node-validatedisposal"></a>
### `protected override void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

**Exceptions**

- `InvalidOperationException`: This node is in lifecycle delivery, its parent is exiting, it is an active tree root, or disposal is attempted off the owner thread.

**Remarks:** This method can run concurrently in multiple callers and can race with another caller starting disposal.
Overrides must therefore be side-effect-free and tolerate repeated execution.

Rejects disposal during tree lifecycle delivery or of an active tree root, and requires the owner thread for an attached node.

<a id="m-electron2d-node-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

Cancels queued deletion, detaches this node, recursively disposes every owned child, clears groups and event
subscribers, and then calls the base implementation. Every teardown stage is attempted before failures are
reported together.

<a id="m-electron2d-node-ensuremutable"></a>
### `protected void EnsureMutable()`

Validates that this node may be mutated at the current lifecycle point.

**Exceptions**

- `InvalidOperationException`: Scene capture is active or an attached node is accessed off the tree owner thread.
- `ObjectDisposedException`: Disposal has started.

**Remarks:** Derived node property setters should call this before changing state that can be stored in a packed scene.

## Event Descriptions

<a id="e-electron2d-node-childadded"></a>
### `public event Action<Node, Node> ChildAdded`

Occurs on the parent after a direct child is structurally attached and child order is reported.

**Remarks:** The first argument is the publishing parent and the second is the child. Delivery is synchronous and precedes
active-tree attachment of the child's subtree.

<a id="e-electron2d-node-childremoved"></a>
### `public event Action<Node, Node> ChildRemoved`

Occurs on the former parent after a direct child is detached and child order is reported.

**Remarks:** The first argument is the publishing former parent and the second is the removed child. Delivery is synchronous,
and structural changes are not rolled back if a handler throws.

<a id="e-electron2d-node-childenteredtree"></a>
### `public event Action<Node, Node> ChildEnteredTree`

Occurs on the direct parent when a child enters the active tree.

**Remarks:** The first argument is the publishing parent and the second is the entering child. Delivery follows that child's
enter notification and event.

<a id="e-electron2d-node-childexitingtree"></a>
### `public event Action<Node, Node> ChildExitingTree`

Occurs on the direct parent while a child is exiting the active tree.

**Remarks:** The first argument is the publishing parent and the second is the exiting child. Descendants have already exited,
and the child's [`Node.Tree`](Node.md#p-electron2d-node-tree) is still set.

<a id="e-electron2d-node-childorderchanged"></a>
### `public event Action<Node> ChildOrderChanged`

Occurs after the order or membership of direct children changes.

**Remarks:** The argument is this parent node. Delivery is synchronous after [`Node.NotificationChildOrderChanged`](Node.md#f-electron2d-node-notificationchildorderchanged).

<a id="e-electron2d-node-renamed"></a>
### `public event Action<Node> Renamed`

Occurs after an active node's own name changes and path notifications propagate.

**Remarks:** The argument is this node. Detached-node renames do not raise the event.

<a id="e-electron2d-node-treeentered"></a>
### `public event Action<Node> TreeEntered`

Occurs when this node enters an active scene tree.

**Remarks:** Delivery follows [`Node.NotificationEnterTree`](Node.md#f-electron2d-node-notificationentertree) and precedes descendant entry.

<a id="e-electron2d-node-treeexiting"></a>
### `public event Action<Node> TreeExiting`

Occurs while this node is exiting its active scene tree.

**Remarks:** Descendants have exited, [`Node.NotificationExitTree`](Node.md#f-electron2d-node-notificationexittree) has run, and [`Node.Tree`](Node.md#p-electron2d-node-tree) remains available.

<a id="e-electron2d-node-treeexited"></a>
### `public event Action<Node> TreeExited`

Occurs after this node has left its scene tree.

**Remarks:** [`Node.Tree`](Node.md#p-electron2d-node-tree) is already `null` when handlers run.

<a id="e-electron2d-node-ready"></a>
### `public event Action<Node> Ready`

Occurs after child-first ready notification delivery.

**Remarks:** SceneTree-managed delivery occurs once until [`Node.RequestReady`](Node.md#m-electron2d-node-requestready) resets the ready state.

<a id="e-electron2d-node-visibilitychanged"></a>
### `public event Action<Node> VisibilityChanged`

Occurs after local or inherited logical visibility is propagated to this node.

**Remarks:** Delivery follows [`Node.NotificationVisibilityChanged`](Node.md#f-electron2d-node-notificationvisibilitychanged) and continues through descendants.

<a id="e-electron2d-node-localtransformchanged"></a>
### `public event Action<Node> LocalTransformChanged`

Occurs after this node's local transform actually changes.

**Remarks:** The event is always enabled; numeric local-transform notification delivery is separately configurable.

<a id="e-electron2d-node-transformchanged"></a>
### `public event Action<Node> TransformChanged`

Occurs when this node's global transform is affected by a local or ancestor change.

**Remarks:** Propagation stops at top-level descendants. The event is independent of numeric transform notifications.

## Constant Descriptions

<a id="f-electron2d-node-notificationentertree"></a>
### `public const int NotificationEnterTree = 10`

Identifies the notification sent when a node enters an active [`SceneTree`](SceneTree.md).

<a id="f-electron2d-node-notificationexittree"></a>
### `public const int NotificationExitTree = 11`

Identifies the notification sent after descendants exit and before this node leaves its tree.

<a id="f-electron2d-node-notificationready"></a>
### `public const int NotificationReady = 13`

Identifies the child-first notification sent when a node becomes ready.

<a id="f-electron2d-node-notificationpaused"></a>
### `public const int NotificationPaused = 14`

Identifies the notification sent when the owning tree becomes paused.

<a id="f-electron2d-node-notificationunpaused"></a>
### `public const int NotificationUnpaused = 15`

Identifies the notification sent when the owning tree resumes from pause.

<a id="f-electron2d-node-notificationphysicsprocess"></a>
### `public const int NotificationPhysicsProcess = 16`

Identifies a physics-process callback notification.

<a id="f-electron2d-node-notificationprocess"></a>
### `public const int NotificationProcess = 17`

Identifies a process callback notification.

<a id="f-electron2d-node-notificationparented"></a>
### `public const int NotificationParented = 18`

Identifies the notification sent after a parent reference is assigned.

<a id="f-electron2d-node-notificationunparented"></a>
### `public const int NotificationUnparented = 19`

Identifies the notification sent after a parent reference is cleared.

<a id="f-electron2d-node-notificationsceneinstantiated"></a>
### `public const int NotificationSceneInstantiated = 20`

Identifies the notification sent to the root after a packed scene is completely instantiated.

<a id="f-electron2d-node-notificationpathrenamed"></a>
### `public const int NotificationPathRenamed = 23`

Identifies the notification propagated when this node's path changes.

<a id="f-electron2d-node-notificationchildorderchanged"></a>
### `public const int NotificationChildOrderChanged = 24`

Identifies the notification sent after the direct child order changes.

<a id="f-electron2d-node-notificationinternalprocess"></a>
### `public const int NotificationInternalProcess = 25`

Identifies an engine-internal process callback notification.

**Remarks:** Built-in node logic uses this lane independently of [`Node.ProcessEnabled`](Node.md#p-electron2d-node-processenabled).

<a id="f-electron2d-node-notificationinternalphysicsprocess"></a>
### `public const int NotificationInternalPhysicsProcess = 26`

Identifies an engine-internal physics-process callback notification.

**Remarks:** Built-in node logic uses this lane independently of [`Node.PhysicsProcessEnabled`](Node.md#p-electron2d-node-physicsprocessenabled).

<a id="f-electron2d-node-notificationpostentertree"></a>
### `public const int NotificationPostEnterTree = 27`

Identifies the notification sent after this node and its descendants finish entering a tree.

<a id="f-electron2d-node-notificationdisabled"></a>
### `public const int NotificationDisabled = 28`

Identifies the notification sent when the effective process mode becomes disabled.

<a id="f-electron2d-node-notificationenabled"></a>
### `public const int NotificationEnabled = 29`

Identifies the notification sent when the effective process mode stops being disabled.

<a id="f-electron2d-node-notificationvisibilitychanged"></a>
### `public const int NotificationVisibilityChanged = 31`

Identifies the notification propagated after local or inherited visibility changes.

<a id="f-electron2d-node-notificationlocaltransformchanged"></a>
### `public const int NotificationLocalTransformChanged = 35`

Identifies a local-transform change notification when local notification delivery is enabled.

<a id="f-electron2d-node-notificationtransformchanged"></a>
### `public const int NotificationTransformChanged = 2000`

Identifies a global-transform change notification when global notification delivery is enabled.

<a id="f-electron2d-node-notificationosmemorywarning"></a>
### `public const int NotificationOsMemoryWarning = 2009`

Identifies an operating-system low-memory warning propagated by the active scene tree.

<a id="f-electron2d-node-notificationtranslationchanged"></a>
### `public const int NotificationTranslationChanged = 2010`

Identifies a notification that translated messages may have changed.

<a id="f-electron2d-node-notificationwmabout"></a>
### `public const int NotificationWmAbout = 2011`

Identifies an operating-system request to show application information.

<a id="f-electron2d-node-notificationcrash"></a>
### `public const int NotificationCrash = 2012`

Identifies a notification delivered immediately before an unrecoverable crash.

<a id="f-electron2d-node-notificationosimeupdate"></a>
### `public const int NotificationOsImeUpdate = 2013`

Identifies an input-method composition update supplied by the operating system.

<a id="f-electron2d-node-notificationapplicationresumed"></a>
### `public const int NotificationApplicationResumed = 2014`

Identifies that the application resumed after suspension.

<a id="f-electron2d-node-notificationapplicationpaused"></a>
### `public const int NotificationApplicationPaused = 2015`

Identifies that the application is about to be suspended.

<a id="f-electron2d-node-notificationapplicationfocusin"></a>
### `public const int NotificationApplicationFocusIn = 2016`

Identifies that the application received keyboard focus.

<a id="f-electron2d-node-notificationapplicationfocusout"></a>
### `public const int NotificationApplicationFocusOut = 2017`

Identifies that the application lost keyboard focus.

<a id="f-electron2d-node-notificationtextserverchanged"></a>
### `public const int NotificationTextServerChanged = 2018`

Identifies that the active text service changed.

<a id="f-electron2d-node-notificationapplicationpipmodeentered"></a>
### `public const int NotificationApplicationPipModeEntered = 2019`

Identifies that the application entered picture-in-picture mode.

<a id="f-electron2d-node-notificationapplicationpipmodeexited"></a>
### `public const int NotificationApplicationPipModeExited = 2020`

Identifies that the application exited picture-in-picture mode.

<a id="f-electron2d-node-minimumzindex"></a>
### `public const int MinimumZIndex = -4096`

Specifies the smallest supported local or effective Z index.

<a id="f-electron2d-node-maximumzindex"></a>
### `public const int MaximumZIndex = 4095`

Specifies the largest supported local or effective Z index.

## Inherited API

Public and protected members inherited from [ElectronObject](ElectronObject.md). Their lifecycle and error contracts remain applicable unless this page states an override.

## Lifecycle and state transitions

For SceneTree-managed attachment, enter is parent-first, post-enter follows descendant entry, and ready is child-first. Ready is one-shot unless `RequestReady()` is called before a later attachment. Exit is child-first. Lifecycle phases attempt all applicable node and tree events before aggregating failures; exit always clears membership. Constructor activation rollback additionally restores ready flags newly consumed by that attempt. Manual inherited `Notify(int)` calls the mapped callback on the caller's thread but does not change membership/readiness or raise the corresponding tree event.

Toggling `SceneTree.Paused` sends paused/unpaused notifications. A process-mode change that crosses effective `Disabled` sends disabled/enabled notification to the node and affected inheriting descendants. `ProcessFrame` and `PhysicsFrame` invoke nodes whose public or engine-internal lane is enabled and that remain live, attached, and eligible when their captured turn arrives. Internal notification `25` or `26` runs before the same node's public notification `17` or `16`; failures are collected while both phases are attempted. A public lane must have been enabled at capture and remain enabled after the internal phase; newly enabling it does not inject work, while disabling it, detaching, or disposing skips delivery. MainLoop system notifications `2009..2020` are propagated by the owning tree through a depth-first snapshot with lifetime and membership revalidation.

Parsed input uses a separate synchronous snapshot. Eligible nodes run child-first/reverse depth-first through `OnInput`, then keyboard-only `OnUnhandledKeyInput`, then `OnUnhandledInput`. Calling `SceneTree.SetInputAsHandled()` stops the current traversal and skips later stages. Membership, disposal, enable flags, and `CanProcess()` are rechecked immediately before each callback; failures are aggregated without undoing committed input state.

Direct disposal and queued deletion both detach an active node and attempt to dispose every member of its complete owned subtree. An instantiated packed-scene root additionally owns every resource duplicate created for that instance and disposes them after child-node cleanup. Cleanup failures are aggregated after structural state, child/resource lifetimes, groups, and subscribers reach their final state. The disposal thread may inspect node state from pre-delete and exit callbacks; other threads observe disposal as started and are rejected.

Packed capture marks the complete source hierarchy before reading factories or stored properties. While marked, hierarchy mutation, ordinary node/base-property mutation, disposal, `QueueFree`, and `CancelFree` are rejected. The mark is always released after success or failure. Packed-scene factory execution also carries an execution-context-local barrier: `SceneTree` construction and `Node.EnterTree` reject lifecycle activation until the factory returns, including attempts to attach into an existing active tree. Instantiation then restores stored properties before parenting, persistent groups before owner assignment, and resources before delivering `NotificationSceneInstantiated` to the root alone. It returns a detached hierarchy; later `SceneTree` enter/ready behavior is unchanged.

Every node created by `PackedScene.Instantiate()` is also marked unfinished until final topology validation completes. During that interval direct disposal and entry into any `SceneTree`, as either a root or an attached child, are rejected. A failed instantiation removes the mark before rollback so ordinary recursive disposal can reclaim the partial hierarchy.

## Invariants, errors, and threading

- Child names are ordinal-unique; invalid names, cycles, multiple parents, direct insertion of an already tree-attached child, disposed children, invalid indices, and unrelated path roots are rejected. `Reparent` is the supported cross-tree move operation when both owner-thread contracts are satisfied.
- An active `SceneTree` root can be destroyed only through `SceneTree.Dispose()`; direct disposal and `QueueFree()` are rejected without changing its lifetime.
- Structural steps and synchronous events are not transactional; an event/callback exception can occur after a documented state change. Implemented cleanup continues to a coherent endpoint rather than rolling the mutation back.
- Removing, reparenting, or disposing a node during its active enter/ready/exit delivery is rejected; recursive exit is ignored, and stale child/ready snapshots revalidate the originally expected tree membership.
- An exiting or disposing parent rejects removal/reparent operations from descendant lifecycle, pre-delete, and cleanup callbacks, so children cannot escape tree or node disposal ownership.
- Attached mutable state requires the `SceneTree` owner thread. Detached mutable state has no built-in synchronization.
- `Owner` is null or a strict ancestor. Removing/reparenting a subtree clears owner references that no longer point to an ancestor.
- Derived nodes are not packable by default. Their factory must be static, source-independent, and return a fresh live exact-type node; constructor-dependent state belongs in stored typed properties.
- A packed-scene node cannot enter an active tree or be disposed until its instantiation barrier is removed; final topology validation and rollback prevent callback-created hierarchy escape from surviving the operation.
- `QueueFree` and `CancelFree` are atomic request operations usable from other threads; actual deletion runs on the owner thread. Detachment does not cancel deletion, while transfer to another tree transfers consumption of the request.
- Access after disposal throws where the member checks lifetime. Simple relationship/status properties (`Parent`, `Children`, `Tree`, `IsInsideTree`, `IsNodeReady`, `IsQueuedForDeletion`) expose their final stored state directly.
- Engine-internal process enablement and original frame deltas are in-assembly integration state for built-in nodes such as [`Timer`](Timer.md); they are not public gameplay switches or protected override points.

## Dependencies and interactions

`Node` depends on `ElectronObject`, `MainLoop` notification identifiers, `PropertyDescriptor`, `NodeProcessMode`, `SceneTree`, typed `InputEvent` values, [`Tween`](Tween.md), the Resource base for owned scene duplicates, [`Mathf`](Mathf.md), `Vector2`, `Transform`, LINQ, `FileSystemName`, and atomic operations. Degree/radian conversion and scalar transform math use the canonical `Mathf` contract. It does not depend on SDL3-CS, a native input backend, renderer, audio, collision physics, scene file serialization, or a scripting runtime.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies lifecycle order, activation/ready rollback, stale snapshot rejection, lifecycle re-entry guards, failure-continuing exit and recursive disposal, disposing-parent mutation rejection, hierarchy validation, reparenting, owner cleanup, paths/search/persistent groups, packed capture and instantiation guards/factories/escape rollback/resource ownership, node/tree event order, child order and sender-first child event arguments, transform behavior, visibility and Z state, spatial helpers, pause modes/priorities/scaled and original deltas, internal-before-public processing and failure continuation, three-stage reverse input ordering/handled state/re-entry/failure continuation/state-before-callback, attached/detached tween creation and bound lifetime, inherited disable/enable notifications, MainLoop system aliases and tree propagation, owner-thread rejection, direct disposal, detached/cross-tree queued deletion, and queued recursive disposal.

There is no renderer-backed canvas behavior, native system-event creation, GUI/viewport consumption, focus synchronization, collision/rigid-body physics, scene file loader/saver, inherited/nested scene authoring, editable-instance metadata, persistent event endpoint schema, RPC/multiplayer, public control of internal processing, process/input auto-enable by override detection, unique-name shorthand, or separate spatial-node subclass. Visibility and Z are currently logical state only. Hardware/input-routing gaps use ADR 0038's exact triggers.

## Relevant decisions

- [0008: Unified Node combines Node and Node2D](../decisions/scene.md#adr-0008)
- [0026: Separate Transform foundational type](../decisions/core-math.md#adr-0026)
- [0029: Typed Transform value and affine semantics](../decisions/core-math.md#adr-0029)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
- [0037: Typed SceneTree tween scheduling](../decisions/scene.md#adr-0037)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)
