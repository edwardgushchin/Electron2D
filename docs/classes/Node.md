# Node

Last updated: 2026-09-24

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** [CanvasItem](CanvasItem.md), [CanvasLayer](CanvasLayer.md), [Timer](Timer.md), [Viewport](Viewport.md)

- **Source:** [Node.cs](../../src/Scene/Main/Node.cs), [Node.Replacement.cs](../../src/Scene/Main/Node.Replacement.cs), [Node.PhysicsInterpolation.cs](../../src/Scene/Main/Node.PhysicsInterpolation.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public partial class Node : ElectronObject`

## Description

The neutral base of every object in a scene tree. Owns ordered children, paths, groups, lifecycle, process/input participation, queued deletion, packed-scene ownership and inherited localization policy. It has no transform, visibility, material or drawing API. Parent/child relationships and callbacks accept Node, so timers and spatial objects share one tree. Attached mutation runs on the tree owner thread; frame callbacks remain explicitly enabled. Tree entry is parent-first, readiness is child-first, and teardown continues through callback failures.

`UniqueNameInOwner` allows an owned node to be resolved through `%Name` from its owner or another node with that same owner. The first node to claim a name keeps it; a later conflicting claim is cleared. Owner and name changes update the claim, and packed scenes restore it after ownership is assigned. `GetPathTo(node, useUniquePath: true)` uses the eligible unique node on the destination side first, or a unique node on the source side when no destination shortcut exists.

`ReplaceBy` swaps a node with a detached replacement at the same sibling index, then moves its children. The old node stays alive and detached. Group copying is optional; owned descendants and scene-local resources follow the replacement.

Physics interpolation keeps logical transforms current while the renderer presents a pose between the two latest physics ticks. A node inherits the nearest ancestor's policy; a root defaults On and Control defaults Off. The tree-wide setting and current renderer decide whether presentation interpolation is active.

## Examples

The snippet uses the Electron2D namespace; attach the hierarchy to a SceneTree or an Engine.Run window to activate it.

```csharp
using var root = new Node { Name = "World" };
root.AddChild(new Electron2D.Timer { Name = "Cooldown" });
root.AddChild(new Entity { Name = "Player", Position = new Vector2(32, 16) });
```

## Constructors

| Member | Contract |
| --- | --- |
| [`public Node()`](#m-electron2d-node-ctor) | Initializes a detached node with its runtime class name and no parent. |

## Properties

| Member | Contract |
| --- | --- |
| [`public NodeAutoTranslateMode AutoTranslateMode { get; set; }`](#p-electron2d-node-autotranslatemode) | Gets or sets the inherited automatic translation policy. |
| [`public int ChildCount { get; }`](#p-electron2d-node-childcount) | Gets the number of direct children. |
| [`public IReadOnlyList<Node> Children { get; }`](#p-electron2d-node-children) | Gets a live read-only view of the ordered direct children. |
| [`public bool InputEnabled { get; set; }`](#p-electron2d-node-inputenabled) | Gets or sets whether this node receives the first input-propagation stage. |
| [`public bool IsInsideTree { get; }`](#p-electron2d-node-isinsidetree) | Gets whether this node currently belongs to a scene tree. |
| [`public bool IsNodeReady { get; }`](#p-electron2d-node-isnodeready) | Gets whether SceneTree-managed ready delivery has occurred since construction or the last ready reset. |
| [`public bool IsQueuedForDeletion { get; }`](#p-electron2d-node-isqueuedfordeletion) | Gets whether deletion has been requested through `Node.QueueFree`. |
| [`public string Name { get; set; }`](#p-electron2d-node-name) | Gets or sets the node name used in sibling lookup and paths. |
| [`public Node? Owner { get; set; }`](#p-electron2d-node-owner) | Gets or sets the ancestor that owns this node for packed-scene storage. |
| [`public bool UniqueNameInOwner { get; set; }`](#p-electron2d-node-uniquenameinowner) | Enables owner-scoped `%Name` lookup when this name is unclaimed. |
| [`public Node? Parent { get; }`](#p-electron2d-node-parent) | Gets the direct parent. |
| [`public PhysicsInterpolationMode PhysicsInterpolationMode { get; set; }`](#p-electron2d-node-physicsinterpolationmode) | Inherits, enables or disables physics presentation interpolation. |
| [`public double PhysicsProcessDeltaTime { get; }`](#p-electron2d-node-physicsprocessdeltatime) | Gets the delta from the most recent SceneTree-managed physics-process frame delivered to this node. |
| [`public bool PhysicsProcessEnabled { get; set; }`](#p-electron2d-node-physicsprocessenabled) | Gets or sets whether this node participates in host-driven physics-process frames. |
| [`public int PhysicsProcessPriority { get; set; }`](#p-electron2d-node-physicsprocesspriority) | Gets or sets this node's ascending physics-process order key. |
| [`public double ProcessDeltaTime { get; }`](#p-electron2d-node-processdeltatime) | Gets the delta from the most recent SceneTree-managed process frame delivered to this node. |
| [`public bool ProcessEnabled { get; set; }`](#p-electron2d-node-processenabled) | Gets or sets whether this node participates in host-driven process frames. |
| [`public ProcessMode ProcessMode { get; set; }`](#p-electron2d-node-processmode) | Gets or sets the pause policy used by both process callback lanes. |
| [`public int ProcessPriority { get; set; }`](#p-electron2d-node-processpriority) | Gets or sets this node's ascending process-frame order key. |
| [`public string SceneFilePath { get; }`](#p-electron2d-node-scenefilepath) | Gets the external resource path from which this scene root was instantiated. |
| [`public SceneTree? Tree { get; }`](#p-electron2d-node-tree) | Gets the active scene tree containing this node. |
| [`public override string TranslationDomain { get; set; }`](#p-electron2d-node-translationdomain) | Gets or explicitly overrides the inherited translation domain. |
| [`public bool UnhandledInputEnabled { get; set; }`](#p-electron2d-node-unhandledinputenabled) | Gets or sets whether this node receives input left unhandled by earlier stages. |
| [`public bool UnhandledKeyInputEnabled { get; set; }`](#p-electron2d-node-unhandledkeyinputenabled) | Gets or sets whether this node receives unhandled keyboard events before general unhandled input. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`public void UpdateConfigurationWarnings()`](#diagnostics-updateconfigurationwarnings) | Requests a configuration-warning refresh for this node in the selected edited scene. |
| [`public virtual string[] GetConfigurationWarnings()`](#diagnostics-getconfigurationwarnings) | Returns this node's current configuration warnings for tooling. |
| [`public void AddChild(Node child)`](#m-electron2d-node-addchild-electron2d-scenenode) | Appends a detached node as the last direct child. |
| [`public void AddSibling(Node sibling)`](#m-electron2d-node-addsibling-electron2d-scenenode) | Inserts a detached node immediately after this node in its parent's child order. |
| [`public void AddToGroup(string group, bool persistent = false)`](#m-electron2d-node-addtogroup-system-string-system-boolean) | Adds this node to a case-sensitive group. |
| [`public string Atr(string message, string? context = null)`](#m-electron2d-node-atr-system-string-system-string) | Translates a singular message when automatic translation is enabled. |
| [`public string AtrN(string singular, string plural, long count, string? context = null)`](#m-electron2d-node-atrn-system-string-system-string-system-int64-system-string) | Translates a plural message when automatic translation is enabled. |
| [`public bool CanAutoTranslate()`](#m-electron2d-node-canautotranslate) | Resolves the nearest ancestor's automatic translation policy. |
| [`public bool CanProcess()`](#m-electron2d-node-canprocess) | Determines whether the resolved process mode allows callbacks in the current tree pause state. |
| [`public bool CancelFree()`](#m-electron2d-node-cancelfree) | Atomically cancels a pending deletion request. |
| [`protected virtual Func<Node> CreateSceneInstanceFactory()`](#m-electron2d-node-createsceneinstancefactory) | Creates a reusable factory for packed-scene instances of this exact runtime node type. |
| [`public Tween CreateTween()`](#m-electron2d-node-createtween) | Creates a tween in this node's scene tree and binds it to this node. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-node-dispose-system-boolean) | Releases resources owned by a derived class. |
| [`protected void EnsureMutable()`](#m-electron2d-node-ensuremutable) | Validates that this node may be mutated at the current lifecycle point. |
| [`public Node FindChild(string pattern, bool recursive = true)`](#m-electron2d-node-findchild-system-string-system-boolean) | Finds the first descendant whose name matches a wildcard pattern. |
| [`public TNode FindChild<TNode>(string pattern = "*", bool recursive = true)`](#m-electron2d-node-findchild-1-system-string-system-boolean) | Finds the first descendant of a requested type whose name matches a wildcard pattern. |
| [`public IReadOnlyList<Node> FindChildren(string pattern, bool recursive = true)`](#m-electron2d-node-findchildren-system-string-system-boolean) | Finds all descendants whose names match a wildcard pattern. |
| [`public IReadOnlyList<TNode> FindChildren<TNode>(string pattern = "*", bool recursive = true)`](#m-electron2d-node-findchildren-1-system-string-system-boolean) | Finds all descendants of a requested type whose names match a wildcard pattern. |
| [`public Node FindParent(string pattern)`](#m-electron2d-node-findparent-system-string) | Finds the nearest ancestor whose name matches a wildcard pattern. |
| [`public Node GetChild(int index)`](#m-electron2d-node-getchild-system-int32) | Gets a direct child by index. |
| [`public IReadOnlyList<string> GetGroups()`](#m-electron2d-node-getgroups) | Returns this node's group memberships. |
| [`public int GetIndex()`](#m-electron2d-node-getindex) | Gets this node's index in its parent's ordered child list. |
| [`public string GetTreeString()`](#m-electron2d-node-gettreestring) | Lists this node and descendants as relative paths in tree order. |
| [`public string GetTreeStringPretty()`](#m-electron2d-node-gettreestringpretty) | Formats this subtree with Unicode branches. |
| [`public Node GetNode(string path)`](#m-electron2d-node-getnode-system-string) | Resolves a required relative or absolute node path. |
| [`public Node GetNodeOrNull(string path)`](#m-electron2d-node-getnodeornull-system-string) | Attempts to resolve a relative or absolute node path. |
| [`public TNode GetNode<TNode>(string path)`](#m-electron2d-node-getnode-1-system-string) | Resolves a required relative or absolute path to a requested node type. |
| [`public string GetPath()`](#m-electron2d-node-getpath) | Builds this node's absolute path from the root of its current hierarchy. |
| [`public string GetPathTo(Node node, bool useUniquePath = false)`](#m-electron2d-node-getpathto-electron2d-node-system-boolean) | Builds a relative path, optionally using owner-scoped unique names. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-node-getpropertydescriptors) | Extends base typed descriptors with neutral name, process, input and automatic translation state for inspection and packed scenes. |
| [`public Viewport GetViewport()`](#m-electron2d-node-getviewport) | Finds this node's nearest viewport, including itself. |
| [`public Window GetWindow()`](#m-electron2d-node-getwindow) | Finds this node's containing window, including itself. |
| [`public bool HasNode(string path)`](#m-electron2d-node-hasnode-system-string) | Tests whether a node path resolves. |
| [`public bool IsAncestorOf(Node node)`](#m-electron2d-node-isancestorof-electron2d-scenenode) | Determines whether this node is a strict ancestor of another node. |
| [`public bool IsGreaterThan(Node node)`](#m-electron2d-node-isgreaterthan-electron2d-node) | Compares two active nodes in depth-first tree order. |
| [`public bool IsPhysicsInterpolated()`](#m-electron2d-node-isphysicsinterpolated) | Gets the resolved node policy independent of the tree-wide switch. |
| [`public bool IsPhysicsInterpolatedAndEnabled()`](#m-electron2d-node-isphysicsinterpolatedandenabled) | Gets whether this active node's presentation is currently interpolated. |
| [`public bool IsInGroup(string group)`](#m-electron2d-node-isingroup-system-string) | Determines whether this node belongs to a case-sensitive group. |
| [`public void MoveChild(Node child, int index)`](#m-electron2d-node-movechild-electron2d-node-system-int32) | Moves a direct child to another sibling index. |
| [`protected virtual void OnEnterTree()`](#m-electron2d-node-onentertree) | Called synchronously when this node enters an active scene tree. |
| [`protected virtual void OnExitTree()`](#m-electron2d-node-onexittree) | Called synchronously when this node exits an active scene tree. |
| [`protected virtual void OnInput(InputEvent event)`](#m-electron2d-node-oninput-electron2d-inputevent) | Receives an input event during the first scene-input propagation stage. |
| [`protected override void OnNotification(int what)`](#m-electron2d-node-onnotification-system-int32) | Handles an engine notification delivered to this object. |
| [`protected virtual void OnPhysicsProcess(double delta)`](#m-electron2d-node-onphysicsprocess-system-double) | Called during an eligible host-driven physics-process frame. |
| [`protected virtual void OnProcess(double delta)`](#m-electron2d-node-onprocess-system-double) | Called during an eligible host-driven process frame. |
| [`protected virtual void OnReady()`](#m-electron2d-node-onready) | Called synchronously when this node receives SceneTree-managed ready delivery. |
| [`protected virtual void OnUnhandledInput(InputEvent event)`](#m-electron2d-node-onunhandledinput-electron2d-inputevent) | Receives an event that remains unhandled after earlier scene-input stages. |
| [`protected virtual void OnUnhandledKeyInput(InputEventKey event)`](#m-electron2d-node-onunhandledkeyinput-electron2d-inputeventkey) | Receives a keyboard event that remains unhandled after the first input stage. |
| [`public void PrintTree()`](#m-electron2d-node-printtree) | Prints relative subtree paths. |
| [`public void PrintTreePretty()`](#m-electron2d-node-printtreepretty) | Prints the indented subtree. |
| [`public void PropagateNotification(int what)`](#m-electron2d-node-propagatenotification-system-int32) | Delivers a notification to this node and its descendants. |
| [`public void QueueFree()`](#m-electron2d-node-queuefree) | Atomically requests this node's deferred disposal at a future scene-tree safe point. |
| [`public bool RemoveChild(Node child)`](#m-electron2d-node-removechild-electron2d-scenenode) | Removes a direct child without disposing it. |
| [`public bool RemoveFromGroup(string group)`](#m-electron2d-node-removefromgroup-system-string) | Removes this node from a case-sensitive group. |
| [`public virtual void Reparent(Node newParent, bool keepGlobalTransform = true)`](#m-electron2d-node-reparent-electron2d-node-system-boolean) | Moves this non-root node under a new parent. |
| [`public void ReplaceBy(Node node, bool keepGroups = false)`](#m-electron2d-node-replaceby-electron2d-node-system-boolean) | Replaces this node in its parent, transferring children and scene ownership. |
| [`public void RequestReady()`](#m-electron2d-node-requestready) | Requests ready delivery the next time SceneTree attachment reaches the ready phase. |
| [`public void ResetPhysicsInterpolation()`](#m-electron2d-node-resetphysicsinterpolation) | Resets this subtree's displayed pose to its current logical transforms. |
| [`public void SetTranslationDomainInherited()`](#m-electron2d-node-settranslationdomaininherited) | Restores inherited translation domain lookup. |
| [`protected override void ValidateDisposal()`](#m-electron2d-node-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |
| [`protected override void ValidateMutation()`](#m-electron2d-node-validatemutation) | Validates that mutable base state may change at the current lifecycle point. |

## Events

| Member | Contract |
| --- | --- |
| [`public event Action<Node, Node>? ChildAdded`](#e-electron2d-node-childadded) | Occurs on the parent after a direct child is structurally attached and child order is reported. |
| [`public event Action<Node, Node>? ChildEnteredTree`](#e-electron2d-node-childenteredtree) | Occurs on the direct parent when a child enters the active tree. |
| [`public event Action<Node, Node>? ChildExitingTree`](#e-electron2d-node-childexitingtree) | Occurs on the direct parent while a child is exiting the active tree. |
| [`public event Action<Node>? ChildOrderChanged`](#e-electron2d-node-childorderchanged) | Occurs after the order or membership of direct children changes. |
| [`public event Action<Node, Node>? ChildRemoved`](#e-electron2d-node-childremoved) | Occurs on the former parent after a direct child is detached and child order is reported. |
| [`public event Action<Node>? Ready`](#e-electron2d-node-ready) | Occurs after child-first ready notification delivery. |
| [`public event Action<Node>? ReplacingBy`](#e-electron2d-node-replacingby) | Occurs after the replacement enters the former parent and before children move. |
| [`public event Action<Node>? Renamed`](#e-electron2d-node-renamed) | Occurs after an active node's own name changes and path notifications propagate. |
| [`public event Action<Node>? TreeEntered`](#e-electron2d-node-treeentered) | Occurs when this node enters an active scene tree. |
| [`public event Action<Node>? TreeExited`](#e-electron2d-node-treeexited) | Occurs after this node has left its scene tree. |
| [`public event Action<Node>? TreeExiting`](#e-electron2d-node-treeexiting) | Occurs while this node is exiting its active scene tree. |

## Constants

| Member | Contract |
| --- | --- |
| [`public const int NotificationApplicationFocusIn = 2016`](#f-electron2d-node-notificationapplicationfocusin) | Identifies that the application received keyboard focus. |
| [`public const int NotificationApplicationFocusOut = 2017`](#f-electron2d-node-notificationapplicationfocusout) | Identifies that the application lost keyboard focus. |
| [`public const int NotificationApplicationPaused = 2015`](#f-electron2d-node-notificationapplicationpaused) | Identifies that the application is about to be suspended. |
| [`public const int NotificationApplicationPipModeEntered = 2019`](#f-electron2d-node-notificationapplicationpipmodeentered) | Identifies that the application entered picture-in-picture mode. |
| [`public const int NotificationApplicationPipModeExited = 2020`](#f-electron2d-node-notificationapplicationpipmodeexited) | Identifies that the application exited picture-in-picture mode. |
| [`public const int NotificationApplicationResumed = 2014`](#f-electron2d-node-notificationapplicationresumed) | Identifies that the application resumed after suspension. |
| [`public const int NotificationChildOrderChanged = 24`](#f-electron2d-node-notificationchildorderchanged) | Identifies the notification sent after the direct child order changes. |
| [`public const int NotificationCrash = 2012`](#f-electron2d-node-notificationcrash) | Identifies a notification delivered immediately before an unrecoverable crash. |
| [`public const int NotificationDisabled = 28`](#f-electron2d-node-notificationdisabled) | Identifies the notification sent when the effective process mode becomes disabled. |
| [`public const int NotificationEnabled = 29`](#f-electron2d-node-notificationenabled) | Identifies the notification sent when the effective process mode stops being disabled. |
| [`public const int NotificationEnterTree = 10`](#f-electron2d-node-notificationentertree) | Identifies the notification sent when a node enters an active `SceneTree`. |
| [`public const int NotificationExitTree = 11`](#f-electron2d-node-notificationexittree) | Identifies the notification sent after descendants exit and before this node leaves its tree. |
| [`public const int NotificationInternalPhysicsProcess = 26`](#f-electron2d-node-notificationinternalphysicsprocess) | Identifies an engine-internal physics-process callback notification. |
| [`public const int NotificationInternalProcess = 25`](#f-electron2d-node-notificationinternalprocess) | Identifies an engine-internal process callback notification. |
| [`public const int NotificationOsImeUpdate = 2013`](#f-electron2d-node-notificationosimeupdate) | Identifies an input-method composition update supplied by the operating system. |
| [`public const int NotificationOsMemoryWarning = 2009`](#f-electron2d-node-notificationosmemorywarning) | Identifies an operating-system low-memory warning propagated by the active scene tree. |
| [`public const int NotificationParented = 18`](#f-electron2d-node-notificationparented) | Identifies the notification sent after a parent reference is assigned. |
| [`public const int NotificationPathRenamed = 23`](#f-electron2d-node-notificationpathrenamed) | Identifies the notification propagated when this node's path changes. |
| [`public const int NotificationPaused = 14`](#f-electron2d-node-notificationpaused) | Identifies the notification sent when the owning tree becomes paused. |
| [`public const int NotificationPhysicsProcess = 16`](#f-electron2d-node-notificationphysicsprocess) | Identifies a physics-process callback notification. |
| [`public const int NotificationPostEnterTree = 27`](#f-electron2d-node-notificationpostentertree) | Identifies the notification sent after this node and its descendants finish entering a tree. |
| [`public const int NotificationProcess = 17`](#f-electron2d-node-notificationprocess) | Identifies a process callback notification. |
| [`public const int NotificationReady = 13`](#f-electron2d-node-notificationready) | Identifies the child-first notification sent when a node becomes ready. |
| [`public const int NotificationResetPhysicsInterpolation = 2001`](#f-electron2d-node-notificationresetphysicsinterpolation) | Identifies a recursive presentation-history reset. |
| [`public const int NotificationSceneInstantiated = 20`](#f-electron2d-node-notificationsceneinstantiated) | Identifies the notification sent to the root after a packed scene is completely instantiated. |
| [`public const int NotificationTextServerChanged = 2018`](#f-electron2d-node-notificationtextserverchanged) | Identifies that the active text service changed. |
| [`public const int NotificationTranslationChanged = 2010`](#f-electron2d-node-notificationtranslationchanged) | Identifies a notification that translated messages may have changed. |
| [`public const int NotificationUnparented = 19`](#f-electron2d-node-notificationunparented) | Identifies the notification sent after a parent reference is cleared. |
| [`public const int NotificationUnpaused = 15`](#f-electron2d-node-notificationunpaused) | Identifies the notification sent when the owning tree resumes from pause. |
| [`public const int NotificationWmAbout = 2011`](#f-electron2d-node-notificationwmabout) | Identifies an operating-system request to show application information. |

## Constructor Descriptions

<a id="m-electron2d-node-ctor"></a>
### `public Node()`

Initializes a detached node with its runtime class name and no parent.

## Property Descriptions

<a id="p-electron2d-node-autotranslatemode"></a>
### `public NodeAutoTranslateMode AutoTranslateMode { get; set; }`

Defaults to `Inherit`. Detached parentless nodes resolve this as enabled; `SceneTree` samples `ProjectSettings.RootNodeAutoTranslate` and assigns `Always` or `Disabled` to a root still set to `Inherit`. Changing the mode notifies this subtree. An active root cannot return to `Inherit`. Unknown values throw `ArgumentOutOfRangeException`; attached changes require the tree owner thread.

<a id="p-electron2d-node-translationdomain"></a>
### `public override string TranslationDomain { get; set; }`

Returns the nearest inherited parent domain, or the main empty domain without a parent. Assigning any string, including empty, creates an explicit override; `SetTranslationDomainInherited` removes it. Active changes notify this node and inheriting descendants while preserving explicit descendant domains. `PackedScene` stores only explicit overrides, so inherited domains remain live after instantiation. A null assignment throws `ArgumentNullException`; attached changes require the owner thread.

<a id="p-electron2d-node-childcount"></a>
### `public int ChildCount { get; }`

Gets the number of direct children.

**Value:** The current child count.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-children"></a>
### `public IReadOnlyList<Node> Children { get; }`

Gets a live read-only view of the ordered direct children.

**Value:** A view backed by this node's child list; later hierarchy changes are visible through it.

<a id="p-electron2d-node-inputenabled"></a>
### `public bool InputEnabled { get; set; }`

Gets or sets whether this node receives the first input-propagation stage.

**Value:** `false` by default.

**Remarks:** Eligible nodes are visited in reverse depth-first order before unhandled-input stages.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-isinsidetree"></a>
### `public bool IsInsideTree { get; }`

Gets whether this node currently belongs to a scene tree.

**Value:** `true` when `Node.Tree` is non-null.

<a id="p-electron2d-node-isnodeready"></a>
### `public bool IsNodeReady { get; }`

Gets whether SceneTree-managed ready delivery has occurred since construction or the last ready reset.

**Value:** The stored ready state. It remains true after detachment until `Node.RequestReady` is called.

**Remarks:** Manual `ElectronObject.Notify(int)` delivery of `Node.NotificationReady` does not change this value.

<a id="p-electron2d-node-isqueuedfordeletion"></a>
### `public bool IsQueuedForDeletion { get; }`

Gets whether deletion has been requested through `Node.QueueFree`.

**Value:** An atomic snapshot of the deletion-request flag.

<a id="p-electron2d-node-name"></a>
### `public string Name { get; set; }`

Gets or sets the node name used in sibling lookup and paths.

**Value:** The nonblank name, initialized to `ElectronObject.ClassName`.

**Remarks:** Names use ordinal equality among siblings and cannot be ., .., or contain /. Renaming to a name already claimed in the same owner scope clears `UniqueNameInOwner`. Renaming an active node propagates `Node.NotificationPathRenamed` through its subtree and then raises `Node.Renamed` on this node.

**System.ArgumentException:** The assigned name is invalid.

**System.InvalidOperationException:** A sibling already has the assigned name, or an attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**System.AggregateException:** One or more path, node-renamed, or tree-renamed callbacks fail after the name changes.

<a id="p-electron2d-node-owner"></a>
### `public Node? Owner { get; set; }`

Gets or sets the ancestor that owns this node for packed-scene storage.

**Value:** An ancestor node, or `null` when this node is not stored by an ancestor scene root.

**Remarks:** A scene root does not own itself. Removing or reparenting a subtree automatically clears owner references that no longer point to an ancestor. Assigning an owner can clear a conflicting unique-name claim.

**System.ArgumentException:** The assigned node is this node or is not an ancestor.

**System.InvalidOperationException:** An attached node is mutated off the tree owner thread or scene capture is active.

**System.ObjectDisposedException:** This node or the assigned owner is disposing or disposed.

<a id="p-electron2d-node-uniquenameinowner"></a>
### `public bool UniqueNameInOwner { get; set; }`

Allows `%Name` lookup from this node's owner and nodes sharing that owner. False by default. The flag can be set before an owner is assigned. A later node with the same owner and name loses its attempted claim and reads false; after the first claim is removed, the later node must set this property again. Changing the owner or name rechecks the claim. PackedScene stores the flag and restores owner-scoped lookup after instantiation.

**System.InvalidOperationException:** An attached mutation is off the scene owner thread or occurs during scene capture.

**System.ObjectDisposedException:** The node is disposed.

<a id="p-electron2d-node-parent"></a>
### `public Node? Parent { get; }`

Gets the direct parent.

**Value:** The owning parent, or `null` while detached.

<a id="p-electron2d-node-physicsinterpolationmode"></a>
### `public PhysicsInterpolationMode PhysicsInterpolationMode { get; set; }`

Defaults to `Inherit`, with a root resolving to On. `Control` starts Off; a descendant may opt back in. Changing an attached enabled policy resets presentation history for the subtree. The selected mode is stored by PackedScene. This property does not change logical transforms or processing callbacks.

**ArgumentOutOfRangeException:** The value is not Inherit, On or Off.

**InvalidOperationException:** Attached mutation is off the owner thread or scene capture is active.

**ObjectDisposedException:** The node is disposed.

<a id="p-electron2d-node-physicsprocessdeltatime"></a>
### `public double PhysicsProcessDeltaTime { get; }`

Gets the delta from the most recent SceneTree-managed physics-process frame delivered to this node.

**Value:** The last delivered physics-process delta in seconds, or zero before the first managed delivery.

**Remarks:** `Engine` applies `Engine.TimeScale` before an Engine-driven delivery.

**Remarks:** Manual `ElectronObject.Notify(int)` delivery does not update this value.

<a id="p-electron2d-node-physicsprocessenabled"></a>
### `public bool PhysicsProcessEnabled { get; set; }`

Gets or sets whether this node participates in host-driven physics-process frames.

**Value:** `false` by default.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-physicsprocesspriority"></a>
### `public int PhysicsProcessPriority { get; set; }`

Gets or sets this node's ascending physics-process order key.

**Value:** Any integer; the default is zero. Equal priorities retain captured tree order.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-processdeltatime"></a>
### `public double ProcessDeltaTime { get; }`

Gets the delta from the most recent SceneTree-managed process frame delivered to this node.

**Value:** The last delivered process delta in seconds, or zero before the first managed delivery.

**Remarks:** `Engine` applies `Engine.TimeScale` before an Engine-driven delivery.

**Remarks:** Manual `ElectronObject.Notify(int)` delivery does not update this value.

<a id="p-electron2d-node-processenabled"></a>
### `public bool ProcessEnabled { get; set; }`

Gets or sets whether this node participates in host-driven process frames.

**Value:** `false` by default.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-processmode"></a>
### `public ProcessMode ProcessMode { get; set; }`

Gets or sets the pause policy used by both process callback lanes.

**Value:** `ProcessMode.Inherit` by default.

**Remarks:** Crossing the effective disabled boundary synchronously notifies this node and affected inheriting descendants.

**System.ArgumentOutOfRangeException:** The assigned enum value is undefined.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**System.Exception:** An enabled or disabled notification callback throws after the mode changes.

<a id="p-electron2d-node-processpriority"></a>
### `public int ProcessPriority { get; set; }`

Gets or sets this node's ascending process-frame order key.

**Value:** Any integer; the default is zero. Equal priorities retain captured tree order.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-scenefilepath"></a>
### `public string SceneFilePath { get; }`

Gets the external resource path from which this scene root was instantiated.

**Value:** The packed-scene path for an instantiated external scene root; otherwise an empty string.

**Remarks:** The value is assigned by packed-scene instantiation and is not inherited by descendants.

<a id="p-electron2d-node-tree"></a>
### `public SceneTree? Tree { get; }`

Gets the active scene tree containing this node.

**Value:** The owning tree, or `null` while detached.

<a id="p-electron2d-node-unhandledinputenabled"></a>
### `public bool UnhandledInputEnabled { get; set; }`

Gets or sets whether this node receives input left unhandled by earlier stages.

**Value:** `false` by default.

**Remarks:** This final stage runs for every event that remains unhandled.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-unhandledkeyinputenabled"></a>
### `public bool UnhandledKeyInputEnabled { get; set; }`

Gets or sets whether this node receives unhandled keyboard events before general unhandled input.

**Value:** `false` by default.

**Remarks:** The stage is skipped for non-keyboard events and after `SceneTree.SetInputAsHandled`.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

## Method Descriptions

<a id="m-electron2d-node-atr-system-string-system-string"></a>
### `public string Atr(string message, string? context = null)`

Returns the source message when `CanAutoTranslate()` is false; otherwise delegates to inherited `Tr`, including its per-object message enablement, domain and context behavior. A null message throws `ArgumentNullException`.

<a id="m-electron2d-node-atrn-system-string-system-string-system-int64-system-string"></a>
### `public string AtrN(string singular, string plural, long count, string? context = null)`

Returns singular only for count one, or plural otherwise, while automatic translation is disabled. Otherwise delegates to inherited `TrN`. Null source forms throw `ArgumentNullException`.

<a id="m-electron2d-node-canautotranslate"></a>
### `public bool CanAutoTranslate()`

Returns whether this node or its nearest non-inheriting ancestor uses `Always`. Detached roots with `Inherit` resolve as enabled. This policy is separate from per-object `CanTranslateMessages`.

<a id="m-electron2d-node-settranslationdomaininherited"></a>
### `public void SetTranslationDomainInherited()`

Clears an explicit domain override so this node resolves its parent's current domain, or the main empty domain when parentless. Affected active inheriting descendants receive `NotificationTranslationChanged`; explicit descendant overrides stop propagation. Attached changes require the owner thread.

<a id="m-electron2d-node-hasnode-system-string"></a>
### `public bool HasNode(string path)`

Returns whether `path` resolves through the relative or absolute node-path rules, including owner-scoped `%Name`. Detached hierarchies support relative paths; absolute paths require active tree membership and a root-name segment. Rejects blank paths and off-owner attached queries.

<a id="m-electron2d-node-isgreaterthan-electron2d-node"></a>
### `public bool IsGreaterThan(Node node)`

Returns whether this node follows `node` in depth-first order. A descendant follows its ancestor; a node does not follow itself. Both nodes must be live in the same active tree, and the query must run on its owner thread. Sibling reordering immediately changes the result.

<a id="m-electron2d-node-isphysicsinterpolated"></a>
### `public bool IsPhysicsInterpolated()`

Returns the inherited node policy without checking the tree-wide switch. An inherited root resolves On; Off on an ancestor propagates until a descendant selects On. Attached reads require the scene owner thread. Disposed nodes throw `ObjectDisposedException`.

<a id="m-electron2d-node-isphysicsinterpolatedandenabled"></a>
### `public bool IsPhysicsInterpolatedAndEnabled()`

Returns true only while this node belongs to a scene tree with `SceneTree.PhysicsInterpolation` enabled and its inherited policy resolves On. Detached nodes return false. Attached reads require the scene owner thread; disposed nodes throw `ObjectDisposedException`.

<a id="m-electron2d-node-gettreestring"></a>
### `public string GetTreeString()`

Returns this subtree in depth-first order as relative paths, starting with `.` and ending every line with `\n`. A detached subtree is supported; attached queries require the owner thread.

<a id="m-electron2d-node-gettreestringpretty"></a>
### `public string GetTreeStringPretty()`

Returns this subtree with Unicode branch characters, names, depth-first order and a final newline. A detached subtree is supported; attached queries require the owner thread.

<a id="m-electron2d-node-printtree"></a>
### `public void PrintTree()`

Writes `GetTreeString()` followed by an additional line break to standard output.

<a id="m-electron2d-node-printtreepretty"></a>
### `public void PrintTreePretty()`

Writes `GetTreeStringPretty()` followed by an additional line break to standard output.

<a id="m-electron2d-node-propagatenotification-system-int32"></a>
### `public void PropagateNotification(int what)`

Delivers the notification to this node first, then each current descendant in depth-first order. The traversed node's direct children cannot be inserted, removed, reordered or disposed while it is visited. Callback failures are collected after other descendants have been attempted. Attached delivery requires the owner thread. Manual notification does not change scene lifecycle state.

<a id="diagnostics-updateconfigurationwarnings"></a>
### `public void UpdateConfigurationWarnings()`

Requests a configuration-warning refresh for this node in the selected edited scene.

Contract: Emits SceneTree.NodeConfigurationWarningChanged synchronously only when this node is the EditedSceneRoot or its descendant. Detached nodes and trees without a selected scene emit nothing. It does not query, cache or compare warning strings. Repeated requests each emit; handler errors propagate.

InvalidOperationException: Mutation is unavailable or this is not the scene owner thread.

ObjectDisposedException: This node is disposed.

Exception: A warning-change subscriber fails.


<a id="diagnostics-getconfigurationwarnings"></a>
### `public virtual string[] GetConfigurationWarnings()`

Returns this node's current configuration warnings for tooling.

Returns: An empty array by default. Overrides return ordered warning messages and should include base warnings.

Contract: This query does not cache results, emit events or require an edited scene. Attached queries run on the scene owner thread. Consumers may call it after NodeConfigurationWarningChanged to refresh their display.

InvalidOperationException: An attached query runs off the scene owner thread.

ObjectDisposedException: This node is disposed.


<a id="m-electron2d-node-addchild-electron2d-scenenode"></a>
### `public void AddChild(Node child)`

Appends a detached node as the last direct child.

**Parameter `child`:** The live node to adopt.

**Remarks:** If this node is active, the child's subtree enters immediately and receives ready where eligible.

**System.ArgumentNullException:** `child` is `null`.

**System.ArgumentException:** `child` is this node.

**System.InvalidOperationException:** The operation would create a cycle, the child already has a parent or tree, a sibling name conflicts, or mutation occurs off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing, or disposal of `child` has started.

**System.AggregateException:** One or more structural, lifecycle, notification, or event callbacks fail after insertion begins.

<a id="m-electron2d-node-addsibling-electron2d-scenenode"></a>
### `public void AddSibling(Node sibling)`

Inserts a detached node immediately after this node in its parent's child order.

**Parameter `sibling`:** The live node to insert.

**System.ArgumentNullException:** `sibling` is `null`.

**System.ArgumentException:** `sibling` is the destination parent.

**System.InvalidOperationException:** This node has no parent, insertion would create a cycle, the sibling is already attached, a name conflicts, or mutation occurs off the owner thread.

**System.ObjectDisposedException:** This node or its parent is disposing on another thread or has finished disposing, or disposal of `sibling` has started.

**System.AggregateException:** One or more structural, lifecycle, notification, or event callbacks fail after insertion begins.

<a id="m-electron2d-node-addtogroup-system-string-system-boolean"></a>
### `public void AddToGroup(string group, bool persistent = false)`

Adds this node to a case-sensitive group.

**Parameter `group`:** The nonblank group name.

**Parameter `persistent`:** Whether packed scenes containing this node should retain the membership.

**Remarks:** Adding an existing membership keeps it persistent once persistence has been requested.

**System.ArgumentException:** `group` is empty or whitespace.

**System.ArgumentNullException:** `group` is `null`.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-canprocess"></a>
### `public bool CanProcess()`

Determines whether the resolved process mode allows callbacks in the current tree pause state.

**Returns:** `false` while detached or disabled; otherwise the result of the resolved pause policy.

**System.InvalidOperationException:** An invalid inherited process mode cannot be resolved.

**System.ObjectDisposedException:** This node or its tree is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-cancelfree"></a>
### `public bool CancelFree()`

Atomically cancels a pending deletion request.

**Returns:** `true` when a request was pending; otherwise `false`.

**Remarks:** A stale queue entry may remain, but the tree ignores it when flushing.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-createsceneinstancefactory"></a>
### `protected virtual Func<Node> CreateSceneInstanceFactory()`

Creates a reusable factory for packed-scene instances of this exact runtime node type.

**Returns:** A non-null factory that creates a fresh node of the exact same runtime type.

**Remarks:** The base implementation supports only an exact `Node`. Derived node types that can be packed must return a static, non-capturing factory that remains valid after the source node is disposed and creates a live, detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore the instance state.

**System.NotSupportedException:** A derived node has not explicitly supplied an instancing factory.

<a id="m-electron2d-node-createtween"></a>
### `public Tween CreateTween()`

Creates a tween in this node's scene tree and binds it to this node.

**Returns:** A running empty tween that halts while this node is detached and is killed when this node is disposed.

**Remarks:** The caller must append at least one tweener before the next matching frame, including a zero-delta frame.

**System.InvalidOperationException:** The node is detached or the call is made off the tree owner thread.

**System.ObjectDisposedException:** The node or its tree is disposing or disposed.

<a id="m-electron2d-node-dispose-system-boolean"></a>
### `protected override void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Remarks:** Cancels queued deletion, detaches this node, recursively disposes every owned child, clears groups and event subscribers, and then calls the base implementation. Every teardown stage is attempted before failures are reported together.

<a id="m-electron2d-node-ensuremutable"></a>
### `protected void EnsureMutable()`

Validates that this node may be mutated at the current lifecycle point.

**Remarks:** Derived node property setters should call this before changing state that can be stored in a packed scene.

**System.InvalidOperationException:** Scene capture is active or an attached node is accessed off the tree owner thread.

**System.ObjectDisposedException:** Disposal has started.

<a id="m-electron2d-node-findchild-system-string-system-boolean"></a>
### `public Node FindChild(string pattern, bool recursive = true)`

Finds the first descendant whose name matches a wildcard pattern.

**Parameter `pattern`:** A nonblank simple expression using * and ?; matching is case-insensitive.

**Parameter `recursive`:** Whether descendants below direct children are searched.

**Returns:** The first matching node in depth-first pre-order, or `null`.

**System.ArgumentException:** `pattern` is empty or whitespace.

**System.ArgumentNullException:** `pattern` is `null`.

**System.ObjectDisposedException:** This node or a recursively searched node is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-findchild-1-system-string-system-boolean"></a>
### `public TNode FindChild<TNode>(string pattern = "*", bool recursive = true)`

Finds the first descendant of a requested type whose name matches a wildcard pattern.

**Type parameter `TNode`:** The required node subtype.

**Parameter `pattern`:** A nonblank simple expression using * and ?; matching is case-insensitive.

**Parameter `recursive`:** Whether descendants below direct children are searched.

**Returns:** The first typed match in depth-first pre-order, or `null`.

**System.ArgumentException:** `pattern` is empty or whitespace.

**System.ArgumentNullException:** `pattern` is `null`.

**System.ObjectDisposedException:** This node or a recursively searched node is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-findchildren-system-string-system-boolean"></a>
### `public IReadOnlyList<Node> FindChildren(string pattern, bool recursive = true)`

Finds all descendants whose names match a wildcard pattern.

**Parameter `pattern`:** A nonblank simple expression using * and ?; matching is case-insensitive.

**Parameter `recursive`:** Whether descendants below direct children are searched.

**Returns:** A read-only snapshot in depth-first pre-order.

**System.ArgumentException:** `pattern` is empty or whitespace.

**System.ArgumentNullException:** `pattern` is `null`.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-findchildren-1-system-string-system-boolean"></a>
### `public IReadOnlyList<TNode> FindChildren<TNode>(string pattern = "*", bool recursive = true)`

Finds all descendants of a requested type whose names match a wildcard pattern.

**Type parameter `TNode`:** The required node subtype.

**Parameter `pattern`:** A nonblank simple expression using * and ?; matching is case-insensitive.

**Parameter `recursive`:** Whether descendants below direct children are searched.

**Returns:** A read-only typed snapshot in depth-first pre-order.

**System.ArgumentException:** `pattern` is empty or whitespace.

**System.ArgumentNullException:** `pattern` is `null`.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-findparent-system-string"></a>
### `public Node FindParent(string pattern)`

Finds the nearest ancestor whose name matches a wildcard pattern.

**Parameter `pattern`:** A nonblank simple expression using * and ?; matching is case-insensitive.

**Returns:** The nearest matching ancestor, or `null`.

**System.ArgumentException:** `pattern` is empty or whitespace.

**System.ArgumentNullException:** `pattern` is `null`.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getchild-system-int32"></a>
### `public Node GetChild(int index)`

Gets a direct child by index.

**Parameter `index`:** The child index; negative values count from the end.

**Returns:** The selected direct child.

**System.ArgumentOutOfRangeException:** This node has no children or `index` is outside the valid range.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getgroups"></a>
### `public IReadOnlyList<string> GetGroups()`

Returns this node's group memberships.

**Returns:** A read-only snapshot sorted using ordinal string order.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getindex"></a>
### `public int GetIndex()`

Gets this node's index in its parent's ordered child list.

**Returns:** The zero-based sibling index, or -1 when this node has no parent.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getnode-system-string"></a>
### `public Node GetNode(string path)`

Resolves a required relative or absolute node path, including owner-scoped `%Name` segments.

**Parameter `path`:** A nonblank slash-separated path supporting ., .., `%Name`, and an optional absolute root-name segment.

**Returns:** The resolved node.

**Remarks:** Absolute paths require an active scene tree and the root-name segment; detached hierarchies support relative paths.

**System.ArgumentException:** `path` is empty or whitespace.

**System.ArgumentNullException:** `path` is `null`.

**System.InvalidOperationException:** An attached lookup runs off the scene owner thread.

**KeyNotFoundException:** No node exists at the requested path.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getnodeornull-system-string"></a>
### `public Node GetNodeOrNull(string path)`

Attempts to resolve a relative or absolute node path, including owner-scoped `%Name` segments.

**Parameter `path`:** A nonblank slash-separated path supporting ., .., `%Name`, and an optional absolute root-name segment.

**Returns:** The resolved node, or `null` when traversal cannot continue.

**Remarks:** Absolute paths require an active scene tree and the root-name segment; detached hierarchies support relative paths.

**System.ArgumentException:** `path` is empty or whitespace.

**System.ArgumentNullException:** `path` is `null`.

**System.InvalidOperationException:** An attached lookup runs off the scene owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getnode-1-system-string"></a>
### `public TNode GetNode<TNode>(string path)`

Resolves a required relative or absolute path to a requested node type.

**Type parameter `TNode`:** The required node subtype.

**Parameter `path`:** A nonblank slash-separated node path.

**Returns:** The resolved node cast to `TNode`.

**System.ArgumentException:** `path` is empty or whitespace.

**System.ArgumentNullException:** `path` is `null`.

**System.InvalidCastException:** The resolved node is not a `TNode`.

**System.InvalidOperationException:** An attached lookup runs off the scene owner thread.

**KeyNotFoundException:** No node exists at the requested path.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getpath"></a>
### `public string GetPath()`

Builds this node's absolute path from the root of its current hierarchy.

**Returns:** A slash-prefixed path that includes the hierarchy root name.

**Remarks:** The path is available for both attached and detached hierarchies.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getpathto-electron2d-node-system-boolean"></a>
### `public string GetPathTo(Node node, bool useUniquePath = false)`

Builds a relative path from this node to another node in the same hierarchy. With `useUniquePath`, an eligible owner-scoped unique destination replaces the preceding route; otherwise an eligible unique source may prefix the upward route, even if it makes the path longer.

**Parameter `node`:** The destination node.

**Parameter `useUniquePath`:** Whether to use owner-scoped `%Name` segments.

**Returns:** . for this node, otherwise a slash-separated sequence of .., child names and optional unique names.

**System.ArgumentNullException:** `node` is `null`.

**System.InvalidOperationException:** The nodes do not share a hierarchy root.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing, or disposal of `node` has started.

<a id="m-electron2d-node-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Extends base typed descriptors with neutral name, unique-name, process and input state for inspection and packed scenes.

**Remarks:** Appends this class's typed hierarchy, ownership, and processing descriptors to the inherited descriptors.

<a id="m-electron2d-node-getviewport"></a>
### `public Viewport GetViewport()`

Finds this node's nearest viewport, including itself.

**Returns:** The nearest viewport ancestor, or null in a hierarchy without a viewport.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-node-getwindow"></a>
### `public Window GetWindow()`

Finds this node's containing window, including itself.

**Returns:** The nearest window ancestor, or null in a hierarchy without a window.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-node-isancestorof-electron2d-scenenode"></a>
### `public bool IsAncestorOf(Node node)`

Determines whether this node is a strict ancestor of another node.

**Parameter `node`:** The node whose parent chain is inspected.

**Returns:** `true` when this node appears in the parent chain; otherwise `false`.

**System.ArgumentNullException:** `node` is `null`.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-isingroup-system-string"></a>
### `public bool IsInGroup(string group)`

Determines whether this node belongs to a case-sensitive group.

**Parameter `group`:** The nonblank group name.

**Returns:** `true` when this node is a member; otherwise `false`.

**System.ArgumentException:** `group` is empty or whitespace.

**System.ArgumentNullException:** `group` is `null`.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-movechild-electron2d-node-system-int32"></a>
### `public void MoveChild(Node child, int index)`

Moves a direct child to another sibling index.

**Parameter `child`:** The direct child to reorder.

**Parameter `index`:** The destination index; negative values count from the end, with -1 selecting the last position.

**System.ArgumentNullException:** `child` is `null`.

**System.ArgumentException:** `child` is not a direct child.

**System.ArgumentOutOfRangeException:** `index` does not resolve to an existing child position.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.AggregateException:** One or more child-order or tree-change callbacks fail after the order changes.

<a id="m-electron2d-node-onentertree"></a>
### `protected virtual void OnEnterTree()`

Called synchronously when this node enters an active scene tree.

**Remarks:** `Node.Tree` is already assigned. The callback runs parent-first, before `Node.TreeEntered`, before descendants enter, and on the tree owner thread during SceneTree-managed lifecycle.

<a id="m-electron2d-node-onexittree"></a>
### `protected virtual void OnExitTree()`

Called synchronously when this node exits an active scene tree.

**Remarks:** Descendants have already exited and `Node.Tree` remains assigned. The callback precedes `Node.TreeExiting` and runs on the tree owner thread during SceneTree-managed lifecycle.

<a id="m-electron2d-node-oninput-electron2d-inputevent"></a>
### `protected virtual void OnInput(InputEvent event)`

Receives an input event during the first scene-input propagation stage.

**Parameter `event`:** The live caller-owned event being dispatched.

**Remarks:** The callback runs synchronously on the scene-tree owner thread when `Node.InputEnabled` is true and `Node.CanProcess` allows the node. Call `SceneTree.SetInputAsHandled` to stop later stages.

<a id="m-electron2d-node-onnotification-system-int32"></a>
### `protected override void OnNotification(int what)`

Handles an engine notification delivered to this object.

**Remarks:** Calls the base implementation, then maps enter, exit, ready, process, and physics-process notification IDs to the corresponding typed virtual callbacks. Manual `ElectronObject.Notify(int)` calls invoke those callbacks but do not mutate tree membership, ready state, or delta values.

<a id="m-electron2d-node-onphysicsprocess-system-double"></a>
### `protected virtual void OnPhysicsProcess(double delta)`

Called during an eligible host-driven physics-process frame.

**Parameter `delta`:** The finite non-negative physics-step delta in seconds.

**Remarks:** The callback is not auto-enabled by overriding it; `Node.PhysicsProcessEnabled` must be true. It executes on the tree owner thread after `Node.PhysicsProcessDeltaTime` is updated and does not perform simulation.

<a id="m-electron2d-node-onprocess-system-double"></a>
### `protected virtual void OnProcess(double delta)`

Called during an eligible host-driven process frame.

**Parameter `delta`:** The finite non-negative frame delta in seconds.

**Remarks:** The callback is not auto-enabled by overriding it; `Node.ProcessEnabled` must be true. It executes on the tree owner thread after `Node.ProcessDeltaTime` is updated.

<a id="m-electron2d-node-onready"></a>
### `protected virtual void OnReady()`

Called synchronously when this node receives SceneTree-managed ready delivery.

**Remarks:** Children are ready first. The callback precedes `Node.Ready`, is one-shot until `Node.RequestReady`, and runs on the owner thread during SceneTree-managed delivery. Manual notification runs on its caller's thread.

<a id="m-electron2d-node-onunhandledinput-electron2d-inputevent"></a>
### `protected virtual void OnUnhandledInput(InputEvent event)`

Receives an event that remains unhandled after earlier scene-input stages.

**Parameter `event`:** The live caller-owned event being dispatched.

**Remarks:** The callback runs synchronously on the scene-tree owner thread when `Node.UnhandledInputEnabled` is true and `Node.CanProcess` allows the node.

<a id="m-electron2d-node-onunhandledkeyinput-electron2d-inputeventkey"></a>
### `protected virtual void OnUnhandledKeyInput(InputEventKey event)`

Receives a keyboard event that remains unhandled after the first input stage.

**Parameter `event`:** The live caller-owned keyboard event being dispatched.

**Remarks:** The callback runs synchronously on the scene-tree owner thread when `Node.UnhandledKeyInputEnabled` is true and `Node.CanProcess` allows the node.

<a id="m-electron2d-node-queuefree"></a>
### `public void QueueFree()`

Atomically requests this node's deferred disposal at a future scene-tree safe point.

**Remarks:** A request made while already detached is queued if the node later enters a tree. Removing the node before its current tree flushes does not cancel deletion: that tree disposes the detached node at its safe point. If the node has entered another tree first, the old entry leaves the request intact for the new tree. Repeated calls are idempotent. The method may be called from a non-owner thread.

**System.InvalidOperationException:** This node is the active scene-tree root.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-removechild-electron2d-scenenode"></a>
### `public bool RemoveChild(Node child)`

Removes a direct child without disposing it.

**Parameter `child`:** The node to detach.

**Returns:** `true` when the node was a direct child and was detached; otherwise `false`.

**Remarks:** An active subtree exits its tree child-first before the parent reference is cleared.

**System.ArgumentNullException:** `child` is `null`.

**System.InvalidOperationException:** An attached node is mutated off the owner thread, this parent is exiting, or the child is in tree lifecycle delivery.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.AggregateException:** One or more lifecycle, notification, or event callbacks fail after removal begins.

<a id="m-electron2d-node-removefromgroup-system-string"></a>
### `public bool RemoveFromGroup(string group)`

Removes this node from a case-sensitive group.

**Parameter `group`:** The nonblank group name.

**Returns:** `true` when membership existed and was removed; otherwise `false`.

**System.ArgumentException:** `group` is empty or whitespace.

**System.ArgumentNullException:** `group` is `null`.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-reparent-electron2d-node-system-boolean"></a>
### `public virtual void Reparent(Node newParent, bool keepGlobalTransform = true)`

Moves this non-root node under a new parent.

**Parameter `newParent`:** The live destination parent.

**Parameter `keepGlobalTransform`:** Whether a derived placement model preserves its global transform. The neutral base has no transform. The default is true.

**Remarks:** The operation detaches first and then appends to `newParent`; callback failures are not rolled back.

**System.ArgumentNullException:** `newParent` is `null`.

**System.ArgumentException:** The destination validation rejects this node as its own child.

**System.InvalidOperationException:** This node has no parent, the move creates a cycle, a destination child name conflicts, either attached hierarchy is accessed off its owner thread, this node or its current parent is in protected tree lifecycle delivery, or a derived placement model rejects its destination.

**System.ObjectDisposedException:** This node or `newParent` is disposing on another thread or has finished disposing.

**System.AggregateException:** One or more structural, lifecycle, notification, or event callbacks fail after reparenting begins.

<a id="m-electron2d-node-replaceby-electron2d-node-system-boolean"></a>
### `public void ReplaceBy(Node node, bool keepGroups = false)`

Replaces this node at its sibling index with a live detached node. The replacement keeps its existing children, then receives this node's direct children in order. Descendants owned by this node become owned by the replacement; other still-valid ancestor owners are restored. A packed-scene root transfers ownership of its scene-local resources and their `GetLocalScene()` association.

**Parameter `node`:** The parentless replacement. It must not conflict with another sibling name or with the names of children to be transferred.

**Parameter `keepGroups`:** Copies group memberships and their persistent flags when true; the default is false.

**Remarks:** The original node remains detached and is not disposed. `ReplacingBy` runs after the replacement enters the former parent and before children move. An active `SceneTree.Root` has stable identity and cannot be replaced. Removing a selected `CurrentScene` or `EditedSceneRoot` clears that selection; callers may select the replacement afterward. Existing typed event subscriptions keep their original targets. Callback failures are collected while structural work continues; a child that cannot enter the replacement is restored under the old node.

**System.ArgumentNullException:** `node` is null.

**System.ArgumentException:** `node` is this node.

**System.InvalidOperationException:** A node is attached, names conflict, a protected lifecycle is running, the active root is selected, or the caller is off the scene owner thread.

**System.ObjectDisposedException:** Either node is disposed.

**System.AggregateException:** One or more structural or lifecycle callbacks fail after replacement begins.

<a id="m-electron2d-node-requestready"></a>
### `public void RequestReady()`

Requests ready delivery the next time SceneTree attachment reaches the ready phase.

**Remarks:** The method only resets stored ready state; it never delivers ready immediately.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-resetphysicsinterpolation"></a>
### `public void ResetPhysicsInterpolation()`

When this node is attached to a tree with interpolation enabled, resets its canvas pose and every descendant to their current logical transforms. Delivers `NotificationResetPhysicsInterpolation` parent-first, even to descendants that opt out of interpolation. Pause and application-suspend notifications also reset eligible subtrees. Detached or tree-wide-disabled calls have no effect. Attached calls require the owner thread; callback failures aggregate after later descendants are attempted.

<a id="m-electron2d-node-validatedisposal"></a>
### `protected override void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

**Remarks:** Rejects disposal during tree lifecycle delivery or of an active tree root, and requires the owner thread for an attached node.

**System.InvalidOperationException:** This node is in lifecycle delivery, its parent is exiting, it is an active tree root, or disposal is attempted off the owner thread.

<a id="m-electron2d-node-validatemutation"></a>
### `protected override void ValidateMutation()`

Validates that mutable base state may change at the current lifecycle point.

## Event Descriptions

<a id="e-electron2d-node-childadded"></a>
### `public event Action<Node, Node>? ChildAdded`

Occurs on the parent after a direct child is structurally attached and child order is reported.

**Remarks:** The first argument is the publishing parent and the second is the child. Delivery is synchronous and precedes active-tree attachment of the child's subtree.

<a id="e-electron2d-node-childenteredtree"></a>
### `public event Action<Node, Node>? ChildEnteredTree`

Occurs on the direct parent when a child enters the active tree.

**Remarks:** The first argument is the publishing parent and the second is the entering child. Delivery follows that child's enter notification and event.

<a id="e-electron2d-node-childexitingtree"></a>
### `public event Action<Node, Node>? ChildExitingTree`

Occurs on the direct parent while a child is exiting the active tree.

**Remarks:** The first argument is the publishing parent and the second is the exiting child. Descendants have already exited, and the child's `Node.Tree` is still set.

<a id="e-electron2d-node-childorderchanged"></a>
### `public event Action<Node>? ChildOrderChanged`

Occurs after the order or membership of direct children changes.

**Remarks:** The argument is this parent node. Delivery is synchronous after `Node.NotificationChildOrderChanged`.

<a id="e-electron2d-node-childremoved"></a>
### `public event Action<Node, Node>? ChildRemoved`

Occurs on the former parent after a direct child is detached and child order is reported.

**Remarks:** The first argument is the publishing former parent and the second is the removed child. Delivery is synchronous, and structural changes are not rolled back if a handler throws.

<a id="e-electron2d-node-ready"></a>
### `public event Action<Node>? Ready`

Occurs after child-first ready notification delivery.

**Remarks:** SceneTree-managed delivery occurs once until `Node.RequestReady` resets the ready state.

<a id="e-electron2d-node-replacingby"></a>
### `public event Action<Node>? ReplacingBy`

Occurs after the replacement enters the former parent and before children move. The argument is the replacement; the publishing node is already detached. A throwing handler does not stop the remaining transfer, and its exception is included in the final aggregate.

<a id="e-electron2d-node-renamed"></a>
### `public event Action<Node>? Renamed`

Occurs after an active node's own name changes and path notifications propagate.

**Remarks:** The argument is this node. Detached-node renames do not raise the event.

<a id="e-electron2d-node-treeentered"></a>
### `public event Action<Node>? TreeEntered`

Occurs when this node enters an active scene tree.

**Remarks:** Delivery follows `Node.NotificationEnterTree` and precedes descendant entry.

<a id="e-electron2d-node-treeexited"></a>
### `public event Action<Node>? TreeExited`

Occurs after this node has left its scene tree.

**Remarks:** `Node.Tree` is already `null` when handlers run.

<a id="e-electron2d-node-treeexiting"></a>
### `public event Action<Node>? TreeExiting`

Occurs while this node is exiting its active scene tree.

**Remarks:** Descendants have exited, `Node.NotificationExitTree` has run, and `Node.Tree` remains available.

## Constant Descriptions

<a id="f-electron2d-node-notificationapplicationfocusin"></a>
### `public const int NotificationApplicationFocusIn = 2016`

Identifies that the application received keyboard focus.

<a id="f-electron2d-node-notificationapplicationfocusout"></a>
### `public const int NotificationApplicationFocusOut = 2017`

Identifies that the application lost keyboard focus.

<a id="f-electron2d-node-notificationapplicationpaused"></a>
### `public const int NotificationApplicationPaused = 2015`

Identifies that the application is about to be suspended.

<a id="f-electron2d-node-notificationapplicationpipmodeentered"></a>
### `public const int NotificationApplicationPipModeEntered = 2019`

Identifies that the application entered picture-in-picture mode.

<a id="f-electron2d-node-notificationapplicationpipmodeexited"></a>
### `public const int NotificationApplicationPipModeExited = 2020`

Identifies that the application exited picture-in-picture mode.

<a id="f-electron2d-node-notificationapplicationresumed"></a>
### `public const int NotificationApplicationResumed = 2014`

Identifies that the application resumed after suspension.

<a id="f-electron2d-node-notificationchildorderchanged"></a>
### `public const int NotificationChildOrderChanged = 24`

Identifies the notification sent after the direct child order changes.

<a id="f-electron2d-node-notificationcrash"></a>
### `public const int NotificationCrash = 2012`

Identifies a notification delivered immediately before an unrecoverable crash.

<a id="f-electron2d-node-notificationdisabled"></a>
### `public const int NotificationDisabled = 28`

Identifies the notification sent when the effective process mode becomes disabled.

<a id="f-electron2d-node-notificationenabled"></a>
### `public const int NotificationEnabled = 29`

Identifies the notification sent when the effective process mode stops being disabled.

<a id="f-electron2d-node-notificationentertree"></a>
### `public const int NotificationEnterTree = 10`

Identifies the notification sent when a node enters an active `SceneTree`.

<a id="f-electron2d-node-notificationexittree"></a>
### `public const int NotificationExitTree = 11`

Identifies the notification sent after descendants exit and before this node leaves its tree.

<a id="f-electron2d-node-notificationinternalphysicsprocess"></a>
### `public const int NotificationInternalPhysicsProcess = 26`

Identifies an engine-internal physics-process callback notification.

**Remarks:** Built-in node logic uses this lane independently of `Node.PhysicsProcessEnabled`.

<a id="f-electron2d-node-notificationinternalprocess"></a>
### `public const int NotificationInternalProcess = 25`

Identifies an engine-internal process callback notification.

**Remarks:** Built-in node logic uses this lane independently of `Node.ProcessEnabled`.

<a id="f-electron2d-node-notificationosimeupdate"></a>
### `public const int NotificationOsImeUpdate = 2013`

Identifies an input-method composition update supplied by the operating system.

<a id="f-electron2d-node-notificationosmemorywarning"></a>
### `public const int NotificationOsMemoryWarning = 2009`

Identifies an operating-system low-memory warning propagated by the active scene tree.

<a id="f-electron2d-node-notificationparented"></a>
### `public const int NotificationParented = 18`

Identifies the notification sent after a parent reference is assigned.

<a id="f-electron2d-node-notificationpathrenamed"></a>
### `public const int NotificationPathRenamed = 23`

Identifies the notification propagated when this node's path changes.

<a id="f-electron2d-node-notificationpaused"></a>
### `public const int NotificationPaused = 14`

Identifies the notification sent when the owning tree becomes paused.

<a id="f-electron2d-node-notificationphysicsprocess"></a>
### `public const int NotificationPhysicsProcess = 16`

Identifies a physics-process callback notification.

<a id="f-electron2d-node-notificationpostentertree"></a>
### `public const int NotificationPostEnterTree = 27`

Identifies the notification sent after this node and its descendants finish entering a tree.

<a id="f-electron2d-node-notificationprocess"></a>
### `public const int NotificationProcess = 17`

Identifies a process callback notification.

<a id="f-electron2d-node-notificationready"></a>
### `public const int NotificationReady = 13`

Identifies the child-first notification sent when a node becomes ready.

<a id="f-electron2d-node-notificationresetphysicsinterpolation"></a>
### `public const int NotificationResetPhysicsInterpolation = 2001`

Identifies a recursive request to discard historical presentation poses while retaining current logical transforms.

<a id="f-electron2d-node-notificationsceneinstantiated"></a>
### `public const int NotificationSceneInstantiated = 20`

Identifies the notification sent to the root after a packed scene is completely instantiated.

<a id="f-electron2d-node-notificationtextserverchanged"></a>
### `public const int NotificationTextServerChanged = 2018`

Identifies that the active text service changed.

<a id="f-electron2d-node-notificationtranslationchanged"></a>
### `public const int NotificationTranslationChanged = 2010`

Identifies a notification that translated messages may have changed.

<a id="f-electron2d-node-notificationunparented"></a>
### `public const int NotificationUnparented = 19`

Identifies the notification sent after a parent reference is cleared.

<a id="f-electron2d-node-notificationunpaused"></a>
### `public const int NotificationUnpaused = 15`

Identifies the notification sent when the owning tree resumes from pause.

<a id="f-electron2d-node-notificationwmabout"></a>
### `public const int NotificationWmAbout = 2011`

Identifies an operating-system request to show application information.

## Ownership, errors and dependencies

The parent owns its children; SceneTree owns the active root. PackedScene capture uses explicit stored descriptors and static exact-type factories. Scene-local resources belong to the instantiated root; externally supplied textures/materials are borrowed. Mutations honor scene capture, lifetime and owner-thread guards. Callback failures are reported after the documented committed state; cleanup attempts every owned stage. See [Node](Node.md) for inherited lifecycle and [the scene hierarchy component](../components/scene-hierarchy.md) for cross-layer flow.

## Verification and limits

[SceneHierarchyTests](../../tests/Electron2D.Tests/SceneHierarchyTests.cs) verifies inheritance, neutral API boundaries, direct custom CanvasItem transforms, mixed parenting, notifications, timer/tween scheduling, packed factories/state, deletion and failure continuation. [NodeReplacementTests](../../tests/Electron2D.Tests/NodeReplacementTests.cs) checks sibling order, groups, ownership, selected-scene clearing, scene-local resources, owner-thread rejection and callback failures. [NodeLocalizationTests](../../tests/Electron2D.Tests/NodeLocalizationTests.cs) checks inherited/explicit domains, automatic translation modes, root setting, entry/change notifications, callback failure continuation, thread affinity and packed state. Existing [runtime checks](../../tests/Electron2D.Tests/Program.cs) retain lifecycle, input, math and ownership coverage. [SceneHierarchyRenderingTests](../../tests/Electron2D.Tests/SceneHierarchyRenderingTests.cs) verifies mixed-tree pixels and a direct CanvasItem drawing texture through both GPU and compatibility backends on Linux Wayland. This does not establish visual owner acceptance or other platforms.

[PhysicsInterpolationTests](../../tests/Electron2D.Tests/PhysicsInterpolationTests.cs) checks mode identities and inheritance, reset/pause, current versus presented transforms, Control opt-in, camera, packed state, owner guards and zero managed bytes over 128 warmed active ticks. [Native interpolation pixels](../../tests/Electron2D.Tests/PhysicsInterpolationNativeTests.cs) pass for a moving canvas item and default Idle camera on dummy compatibility and Linux Wayland compatibility/GPU. Physical timing and other platforms remain unverified.

The hierarchy is implemented; complete reference API parity is not claimed. Missing GUI, canvas policies, rendering primitives, scene-file authoring and other capabilities remain classified per member in [coverage](../coverage/index.md). No inert compatibility members are added.

## Canvas membership integration

SceneTree activation uses an internal layer hook separately from public numeric notification dispatch. CanvasItem attaches before the tree-enter callback and detaches after the tree-exit callback. Failures join existing lifecycle error aggregation and activation rollback. Manual tree notifications still call their typed callbacks without changing membership.

## Relevant decisions

- [0008: Node, CanvasItem and Entity](../decisions/scene.md#adr-0008)
- [0004: Product scope and API correspondence](../decisions/product.md#adr-0004)
- [0023: Typed packed scenes](../decisions/scene.md#adr-0023)
- [0028: Rendering](../decisions/rendering.md#adr-0028)

## Configuration diagnostics

GetConfigurationWarnings is an immediate typed virtual query, callable without a selected editor scene. UpdateConfigurationWarnings requests a refresh; it does not call the query. Derived setters should explicitly request refresh when their warning conditions change. An empty result is valid, and repeated refresh requests are not deduplicated. Attach a listener to SceneTree.NodeConfigurationWarningChanged and select EditedSceneRoot to consume these events. The listener can query the current array; its exceptions follow ordinary synchronous C# event delivery. No scene dock, script tool mode or accessibility diagnostics are implemented by this API.

[SceneDiagnosticsTests](../../tests/Electron2D.Tests/SceneDiagnosticsTests.cs) verifies the selected-subtree boundary, detached/invalid/disposed nodes, query and subscriber failures, thread affinity and removal cleanup.
