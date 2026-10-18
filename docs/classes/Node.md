# Node

Last updated: 2026-10-18

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** [InstancePlaceholder](InstancePlaceholder.md), [AudioStreamPlayer](AudioStreamPlayer.md), [CanvasItem](CanvasItem.md), [CanvasLayer](CanvasLayer.md), [ShaderGlobalsOverride](ShaderGlobalsOverride.md), [Timer](Timer.md), [Viewport](Viewport.md)

- **Source:** [Node.cs](../../src/Scene/Main/Node.cs), [Node.Replacement.cs](../../src/Scene/Main/Node.Replacement.cs), [Node.PhysicsInterpolation.cs](../../src/Scene/Main/Node.PhysicsInterpolation.cs), [Node.Diagnostics.cs](../../src/Scene/Main/Node.Diagnostics.cs), [Node.Names.cs](../../src/Scene/Main/Node.Names.cs), [Node.TreeMembership.cs](../../src/Scene/Main/Node.TreeMembership.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public partial class Node : ElectronObject`

Authoring guards inspect affected scene colliders and their actual physical attachments, including detached descendants and colliders imported into caller-owned spaces. Numerical hooks cannot mutate their node/subtree authoring; hierarchy, entry and disposal reject borrowed physical contexts before structural changes. Engine pose publication retains its ordinary step phase. [Common space bindings](../components/physics-registered-adapter.md#common-space-bindings) verifies these paths.

## Deferred scene authoring

`GetSceneInstanceLoadPlaceholder()` and `SetSceneInstanceLoadPlaceholder(bool)` control future capture of an actual nonroot scene instance with a scene file path. The setter changes authoring state; it does not replace the live node. [Deferred scenes](../components/deferred-scenes.md) restores a transform-free InstancePlaceholder and later explicitly materializes its sibling through the existing scene/resource lifecycle.

## Retained canvas presentation

Changing PhysicsInterpolationMode republishes the effective source policy for affected borrowed canvas items and resets their retained histories. Explicit renderer state remains session-local; a node reset updates its own source/retained presentation endpoints and propagated descendants. See [contract and native verification](../components/canvas-rendering.md#retained-canvas-item-interpolation).

The inherited [CancelDisposal](ElectronObject.md#m-electron2d-electronobject-canceldisposal) vetoes automatic pre-delete independently of CancelFree. A direct veto leaves the node and owned subtree attached and alive. Queued deletion already detaches the node before attempting disposal; its consumed request is released after a veto and the live subtree can be reattached. When an owning parent is destroyed, a child that vetoes or rejects disposal survives detached; unparent callbacks remain inside the teardown barrier, errors aggregate and siblings continue.

Vetoable pre-delete retains authored reverse ownership and unique-name reservations, including a temporarily closed detached authoring owner. Ordinary live lookup hides closed targets; another authoring write cannot prune or steal their registration before the attempt commits.

## Resource endpoints and subtree operations

`GetNodeAndResource(string path)` resolves slash-separated node names followed by colon-separated typed property names. It returns a C# tuple `(Node? Node, Resource? Resource, string RemainingPath)` rather than a heterogeneous array. Each resource-valued property becomes the next owner. The result retains the last borrowed resource and stops at the first existing scalar, node, other non-resource value or null, returning that component and later components as a colon-prefixed remainder. It does not validate the remainder. A missing node/property returns a null Node; a resource already found remains in the tuple. `HasNodeAndResource` tests the Node field, so an existing null property is a successful remainder. Getter/schema errors remain visible; disposed resolved resources reject access.

Node names retain ordinary relative, absolute active-tree and owner-unique rules. `:Texture` addresses the calling node; a single trailing colon is accepted. Empty intermediate components reject. GetNode/GetNodeOrNull/HasNode now parse the same subname syntax and use only its node portion. All resource property access uses the exposed `PropertyDescriptor<TOwner,TValue>` schema, including derived types and read-only descriptors. This is a cold query that discovers descriptors; no hot path allocation claim is made.

`PropagateCall(Action<Node>, bool parentFirst=false)` runs a compiled operation on every node in a subtree, including internal children. Generic overloads filter by a Node role and optionally pass one typed argument, including tuples. Children-first is the default; parent-first visits each parent before descendants. Ordered child traversal uses the existing hierarchy barrier: changing the currently traversed child list, reparenting/disposal through that parent, and disposal of the current node reject, while ordinary properties may change. Mutating a descendant before its own traversal retains the existing live hierarchy semantics. Nested calls retain independent depth. Callback failures are combined after the remaining nodes are attempted, and finally restores every traversal guard. Attached calls require the scene owner.

SubtreeResourceAuthoringTests verifies path/error/null/last-resource semantics, custom typed getters, relative/absolute/unique names, all operation orders and roles, typed arguments, internal children, structural/disposal guards, error continuation, guard release and 1000 warmed propagation calls with zero managed allocation. Edited Sprite/AtlasTexture state survives `.e2dscene` save/load/reconstruction. Linux Wayland GPU and hardware compatibility hosts verify edited atlas pixels and propagated positions. Other platforms, full editor automation, large hierarchy stack limits, native allocation and human acceptance remain unverified. Node duplicate and persistent connections remain their own executable prerequisites.

## Scene authoring state

`EditorDescription` stores nonnull text and emits `EditorDescriptionChanged(this)` after an ordinal change, including detached nodes. Equal writes are silent; blocking signals suppresses delivery while preserving the committed value. Callback errors propagate after commitment and nested edits observe the committed text. `DisplayFolded` defaults false, remains transient, and changes neither processing, visibility nor physics.

`SceneTree.EditedSceneRoot` explicitly selects the authoring subtree. `IsPartOfEditedScene` includes that live root and its descendants; selection of the tree root is allowed. Detachment clears membership. This context creates no editor UI and changes no simulation policy.

`SetEditableInstance(node, isEditable)` requires a live strict descendant. Its flag belongs to the target and follows reparenting; any current ancestor can query it with `IsEditableInstance`. Null queries return false; invalid nonnull relationships throw `ArgumentException`. Disabling also unfolds the receiver. Both mutation guards run before writes. Every accepted call, even an equal flag, requests `EditorStateChanged` on the selected target.

Selected nodes also emit `EditorStateChanged(this)` for attached process-mode changes, nonnull owner changes, unique-name changes, successful persistent group add/remove, source-path assignment and authored connection changes. Folding, selection, ordinary runtime groups and CLR subscriptions emit no state event. Delivery is synchronous on the mutating thread outside internal locks and respects signal blocking. A process-mode callback failure joins other notification failures; owner/unique/group operations retain committed progress when a callback throws. A failed authored subscription rolls back without a second authoring event; rollback errors are preserved. Manual disconnection emits once, while consumed one-shots and endpoint teardown do not.

Attached reads require the owner or current processing group. Description/folding/editability writes require the scene owner and reject capture or physical borrowing. Persistent group changes retain current-group access and deliver state events on that worker. Committed disposal clears the new events; canceled disposal preserves them.

Application culture and pseudolocalization changes now deliver `NotificationTranslationChanged` through the current `Engine.MainLoop`, including paused or automatically disabled nodes. `AutoTranslateMode` still controls `Atr`/`AtrN` source fallback independently of explicit `Tr`/`TrN`. Independent library trees use explicit notification. See [delivery, failure and allocation verification](../components/localization.md#application-translation-notifications).

## Description

The neutral base of every object in a scene tree. Owns ordered children, paths, groups, lifecycle, process/input participation, queued deletion, packed-scene ownership and inherited localization policy. It has no transform, visibility, material or drawing API. Parent/child relationships and callbacks accept Node, so timers and spatial objects share one tree. Physical/hierarchy mutation runs on the tree owner thread; local grouping and processing metadata also allow the current processing group; ready notification enables overridden frame/input callbacks before OnReady, and callers can disable them explicitly. Tree entry is parent-first, readiness is child-first, and teardown continues through callback failures.

`UniqueNameInOwner` allows an owned node to be resolved through `%Name` from its owner or another node with that same owner. The first node to claim a name keeps it; a later conflicting claim is cleared. Owner and name changes update the claim, and packed scenes restore it after ownership is assigned. `GetPathTo(node, useUniquePath: true)` uses the eligible unique node on the destination side first, or a unique node on the source side when no destination shortcut exists.

`ReplaceBy` swaps a node with a detached replacement at the same sibling index, then moves its children. The old node stays alive and detached. Ownership failures are collected with transfer errors after every saved owner is attempted. Group copying is optional; owned descendants and scene-local resources follow the replacement.

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
| [`public string EditorDescription { get; set; }`](#p-electron2d-node-editordescription) | Stored description; emits after an ordinal change. |
| [`public bool DisplayFolded { get; set; }`](#p-electron2d-node-displayfolded) | Transient authoring presentation. |
| [`public bool IsPartOfEditedScene { get; }`](#p-electron2d-node-ispartofeditedscene) | Membership in the explicitly selected subtree. |
| [`public NodeAutoTranslateMode AutoTranslateMode { get; set; }`](#p-electron2d-node-autotranslatemode) | Gets or sets the inherited automatic translation policy. |
| [`public int ChildCount { get; }`](#p-electron2d-node-childcount) | Gets the ordinary direct-child count. |
| [`public IReadOnlyList<Node> Children { get; }`](#p-electron2d-node-children) | Gets a live read-only view of ordinary direct children. |
| [`public bool InputEnabled { get; set; }`](#p-electron2d-node-inputenabled) | Gets or sets whether this node receives the first input-propagation stage. |
| [`public bool IsInsideTree { get; }`](#p-electron2d-node-isinsidetree) | Gets whether this node currently belongs to a scene tree. |
| [`public bool IsNodeReady { get; }`](#p-electron2d-node-isnodeready) | Gets whether SceneTree-managed ready delivery has occurred since construction or the last ready reset. |
| [`public string Name { get; set; }`](#p-electron2d-node-name) | Gets or sets the node name used in sibling lookup and paths. |
| [`public Node? Owner { get; set; }`](#p-electron2d-node-owner) | Gets or sets the borrowed authoring owner used for packed-scene storage. |
| [`public bool UniqueNameInOwner { get; set; }`](#p-electron2d-node-uniquenameinowner) | Enables owner-scoped `%Name` lookup when this name is unclaimed. |
| [`public Node? Parent { get; }`](#p-electron2d-node-parent) | Gets the direct structural parent. |
| [`public PhysicsInterpolationMode PhysicsInterpolationMode { get; set; }`](#p-electron2d-node-physicsinterpolationmode) | Inherits, enables or disables physics presentation interpolation. |
| [`public double PhysicsProcessDeltaTime { get; }`](#p-electron2d-node-physicsprocessdeltatime) | Gets the current or most recent physics-frame duration of the attached scene tree. |
| [`public bool PhysicsProcessEnabled { get; set; }`](#p-electron2d-node-physicsprocessenabled) | Gets or sets whether this node participates in host-driven physics-process frames. |
| [`public int PhysicsProcessPriority { get; set; }`](#p-electron2d-node-physicsprocesspriority) | Gets or sets this node's ascending physics-process order key. |
| [`public double ProcessDeltaTime { get; }`](#p-electron2d-node-processdeltatime) | Gets the current or most recent process-frame duration of the attached scene tree. |
| [`public bool ProcessEnabled { get; set; }`](#p-electron2d-node-processenabled) | Gets or sets whether this node participates in host-driven process frames. |
| [`public ProcessMode ProcessMode { get; set; }`](#p-electron2d-node-processmode) | Gets or sets the pause policy used by both process callback lanes. |
| [`public int ProcessPriority { get; set; }`](#p-electron2d-node-processpriority) | Gets or sets this node's ascending process-frame order key. |
| [`public string SceneFilePath { get; set; }`](#p-electron2d-node-scenefilepath) | Gets or sets this node's scene source path metadata. |
| [`public SceneTree? Tree { get; }`](#p-electron2d-node-tree) | Gets the active scene tree containing this node. |
| [`public override string TranslationDomain { get; set; }`](#p-electron2d-node-translationdomain) | Gets or explicitly overrides the inherited translation domain. |
| [`public bool ShortcutInputEnabled { get; set; }`](#p-electron2d-node-shortcutinputenabled) | Selects the post-GUI shortcut stage; ready enables an overridden callback. |
| [`public bool UnhandledInputEnabled { get; set; }`](#p-electron2d-node-unhandledinputenabled) | Gets or sets whether this node receives input left unhandled by earlier stages. |
| [`public bool UnhandledKeyInputEnabled { get; set; }`](#p-electron2d-node-unhandledkeyinputenabled) | Gets or sets whether this node receives unhandled keyboard events before general unhandled input. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`public void SetEditableInstance(Node node, bool isEditable)`](#m-electron2d-node-seteditableinstance-electron2d-node-system-boolean) | Sets the strict descendant's flag; false also unfolds the receiver. |
| [`public bool IsEditableInstance(Node? node)`](#m-electron2d-node-iseditableinstance-electron2d-node) | Queries the target flag through a current ancestor; null is false. |
| [`public void UpdateConfigurationWarnings()`](#diagnostics-updateconfigurationwarnings) | Requests a configuration-warning refresh for this node in the selected edited scene. |
| [`public virtual string[] GetConfigurationWarnings()`](#diagnostics-getconfigurationwarnings) | Returns this node's current configuration warnings for tooling. |
| [`public void AddChild(Node child, InternalMode internalMode = InternalMode.Disabled)`](#m-electron2d-node-addchild-electron2d-scenenode) | Adds an ordinary or front/back internal direct child. |
| [`public void AddChild(Node child, bool forceReadableName, InternalMode internalMode = InternalMode.Disabled)`](#m-electron2d-node-addchild-readable) | Adds with readable or generated collision names. |
| [`public void AddSibling(Node sibling)`](#m-electron2d-node-addsibling-electron2d-scenenode) | Inserts a detached node immediately after this node in its parent's child order. |
| [`public void AddSibling(Node sibling, bool forceReadableName)`](#m-electron2d-node-addsibling-readable) | Selects naming while preserving immediate sibling order and internal mode. |
| [`public void AddToGroup(string group, bool persistent = false)`](#m-electron2d-node-addtogroup-system-string-system-boolean) | Adds this node to a case-sensitive group. |
| [`public string Atr(string message, string? context = null)`](#m-electron2d-node-atr-system-string-system-string) | Translates a singular message when automatic translation is enabled. |
| [`public string AtrN(string singular, string plural, long count, string? context = null)`](#m-electron2d-node-atrn-system-string-system-string-system-int64-system-string) | Translates a plural message when automatic translation is enabled. |
| [`public bool CanAutoTranslate()`](#m-electron2d-node-canautotranslate) | Resolves the nearest ancestor's automatic translation policy. |
| [`public bool CanProcess()`](#m-electron2d-node-canprocess) | Determines whether the resolved process mode allows callbacks in the current tree pause state. |
| [`public bool CancelFree()`](#m-electron2d-node-cancelfree) | Atomically cancels a pending deletion request. |
| [`protected virtual Func<Node> CreateSceneInstanceFactory()`](#m-electron2d-node-createsceneinstancefactory) | Creates a reusable factory for packed-scene instances of this exact runtime node type. |
| [`public Tween CreateTween()`](#m-electron2d-node-createtween) | Creates a bound tween in this node's tree or the current application scene tree. |
| [`protected override void Dispose(bool disposing)`](#m-electron2d-node-dispose-system-boolean) | Releases resources owned by a derived class. |
| [`protected void EnsureMutable()`](#m-electron2d-node-ensuremutable) | Validates that this node may be mutated at the current lifecycle point. |
| [`public Node FindChild(string pattern, bool recursive = true, bool owned = true)`](#m-electron2d-node-findchild-system-string-system-boolean-system-boolean) | Finds the first descendant whose name matches a wildcard pattern. |
| [`public TNode FindChild<TNode>(string pattern = "*", bool recursive = true, bool owned = true)`](#m-electron2d-node-findchild-1-system-string-system-boolean-system-boolean) | Finds the first descendant of a requested type whose name matches a wildcard pattern. |
| [`public IReadOnlyList<Node> FindChildren(string pattern, bool recursive = true, bool owned = true)`](#m-electron2d-node-findchildren-system-string-system-boolean-system-boolean) | Finds all descendants whose names match a wildcard pattern. |
| [`public IReadOnlyList<TNode> FindChildren<TNode>(string pattern = "*", bool recursive = true, bool owned = true)`](#m-electron2d-node-findchildren-1-system-string-system-boolean-system-boolean) | Finds all descendants of a requested type whose names match a wildcard pattern. |
| [`public Node FindParent(string pattern)`](#m-electron2d-node-findparent-system-string) | Finds the nearest ancestor whose name matches a wildcard pattern. |
| [`public Node GetChild(int index, bool includeInternal = false)`](#m-electron2d-node-getchild-system-int32) | Gets a direct child from the selected view. |
| `public int GetChildCount(bool includeInternal = false)` | Counts ordinary or all direct children. |
| `public Node[] GetChildren(bool includeInternal = false)` | Returns a caller-owned child snapshot. |
| [`public IReadOnlyList<string> GetGroups()`](#m-electron2d-node-getgroups) | Returns this node's group memberships. |
| [`public static ulong[] GetOrphanNodeIDs()`](#m-electron2d-node-getorphannodeids) | Samples live detached node IDs in Debug; returns an empty array in Release. |
| [`public int GetIndex(bool includeInternal = false)`](#m-electron2d-node-getindex) | Gets this node's index in its selected sibling partition. |
| [`public string GetTreeString()`](#m-electron2d-node-gettreestring) | Lists this node and descendants as relative paths in tree order. |
| [`public string GetTreeStringPretty()`](#m-electron2d-node-gettreestringpretty) | Formats this subtree with Unicode branches. |
| [`public Node GetNode(string path)`](#m-electron2d-node-getnode-system-string) | Resolves a required relative or absolute node path. |
| [`public Node GetNodeOrNull(string path)`](#m-electron2d-node-getnodeornull-system-string) | Attempts to resolve a relative or absolute node path. |
| [`public TNode GetNode<TNode>(string path)`](#m-electron2d-node-getnode-1-system-string) | Resolves a required relative or absolute path to a requested node type. |
| [`public string GetPath()`](#m-electron2d-node-getpath) | Gets this node's cached absolute path in its active scene tree. |
| [`public string GetPathTo(Node node, bool useUniquePath = false)`](#m-electron2d-node-getpathto-electron2d-node-system-boolean) | Builds a relative path, optionally using owner-scoped unique names. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-node-getpropertydescriptors) | Extends base typed descriptors with neutral name, process, input and automatic translation state for inspection and packed scenes. |
| [`public Viewport? GetViewport()`](#m-electron2d-node-getviewport) | Returns this node's cached active viewport, including itself when it is a viewport. |
| [`public Window? GetWindow()`](#m-electron2d-node-getwindow) | Finds this node's active containing window through viewport parents. |
| [`public Window? GetLastExclusiveWindow()`](#m-electron2d-node-getlastexclusivewindow) | Finds the last active exclusive transient child from the containing window. |
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
| [`protected virtual void OnShortcutInput(InputEvent event)`](#m-electron2d-node-onshortcutinput-electron2d-inputevent) | Receives eligible post-GUI key, gamepad-button and shortcut events. |
| [`protected virtual void OnUnhandledInput(InputEvent event)`](#m-electron2d-node-onunhandledinput-electron2d-inputevent) | Receives an event that remains unhandled after earlier scene-input stages. |
| [`protected virtual void OnUnhandledKeyInput(InputEventKey event)`](#m-electron2d-node-onunhandledkeyinput-electron2d-inputeventkey) | Receives a keyboard event that remains unhandled after the first input stage. |
| [`public void PrintTree()`](#m-electron2d-node-printtree) | Prints relative subtree paths. |
| [`public static void PrintOrphanNodes()`](#m-electron2d-node-printorphannodes) | Prints sampled detached node IDs, hierarchy names and compiled types in Debug. |
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
| [`public event Action<Node>? EditorDescriptionChanged`](#e-electron2d-node-editordescriptionchanged) | Description committed; sender is this node. |
| [`public event Action<Node>? EditorStateChanged`](#e-electron2d-node-editorstatechanged) | Selected node authoring state committed. |
| [`public event Action<Node, Node>? ChildAdded`](#e-electron2d-node-childadded) | Occurs on the parent after a direct child is structurally attached and child order is reported. |
| [`public event Action<Node, Node>? ChildEnteredTree`](#e-electron2d-node-childenteredtree) | Occurs on the direct parent when a child enters the active tree. |
| [`public event Action<Node, Node>? ChildExitingTree`](#e-electron2d-node-childexitingtree) | Occurs on the direct parent while a child is exiting the active tree. |
| [`public event Action<Node>? ChildOrderChanged`](#e-electron2d-node-childorderchanged) | Occurs after the order or membership of direct children changes. |
| [`public event Action<Node, Node>? ChildRemoved`](#e-electron2d-node-childremoved) | Occurs on the former parent after a direct child is detached and child order is reported. |
| [`public event Action<Node>? Ready`](#e-electron2d-node-ready) | Occurs after child-first ready notification delivery. |
| [`public event Action<Node>? ReplacingBy`](#e-electron2d-node-replacingby) | Occurs after the replacement enters the former parent and before children move. |
| [`public event Action<Node>? Renamed`](#e-electron2d-node-renamed) | Occurs after an active node accepts a name assignment and path notifications propagate. |
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
| [`public const int NotificationDragBegin = 21`](#f-electron2d-node-notificationdragbegin) | Delivered after a root-viewport GUI drag payload becomes active. |
| [`public const int NotificationDragEnd = 22`](#f-electron2d-node-notificationdragend) | Delivered after drag result commits and payload/preview clear. |
| [`public const int NotificationCrash = 2012`](#f-electron2d-node-notificationcrash) | Identifies a notification delivered immediately before an unrecoverable crash. |
| [`public const int NotificationDisabled = 28`](#f-electron2d-node-notificationdisabled) | Identifies the notification sent when the effective process mode becomes disabled. |
| [`public const int NotificationEnabled = 29`](#f-electron2d-node-notificationenabled) | Identifies the notification sent when the effective process mode stops being disabled. |
| [`public const int NotificationEnterTree = 10`](#f-electron2d-node-notificationentertree) | Identifies the notification sent when a node enters an active `SceneTree`. |
| [`public const int NotificationExitTree = 11`](#f-electron2d-node-notificationexittree) | Identifies the notification sent after descendants exit and before this node leaves its tree. |
| `public bool InternalProcessEnabled { get; set; }` | Controls independent internal idle-frame notifications; runtime state. |
| `public bool InternalPhysicsProcessEnabled { get; set; }` | Controls independent internal fixed-frame notifications; runtime state. |
| [`public const int NotificationInternalPhysicsProcess = 26`](#f-electron2d-node-notificationinternalphysicsprocess) | Identifies an engine-internal physics-process callback notification. |
| [`public const int NotificationInternalProcess = 25`](#f-electron2d-node-notificationinternalprocess) | Identifies an engine-internal process callback notification. |
| [`public const int NotificationOsImeUpdate = 2013`](#f-electron2d-node-notificationosimeupdate) | Identifies an input-method composition update supplied by the operating system. |
| [`public const int NotificationOsMemoryWarning = 2009`](#f-electron2d-node-notificationosmemorywarning) | Identifies an operating-system low-memory warning propagated by the active scene tree. |
| [`public const int NotificationParented = 18`](#f-electron2d-node-notificationparented) | Identifies the notification sent after a parent reference is assigned. |
| [`public const int NotificationPathRenamed = 23`](#f-electron2d-node-notificationpathrenamed) | Identifies the notification propagated after an accepted name assignment, including an unchanged name. |
| [`public const int NotificationPaused = 14`](#f-electron2d-node-notificationpaused) | Identifies the notification sent when the owning tree becomes paused. |
| [`public const int NotificationPhysicsProcess = 16`](#f-electron2d-node-notificationphysicsprocess) | Identifies a physics-process callback notification. |
| [`public const int NotificationPostEnterTree = 27`](#f-electron2d-node-notificationpostentertree) | Identifies the child-first notification sent on every attachment after descendant ready delivery and before this node's optional ready notification. |
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

Gets the number of ordinary direct children. `GetChildCount(true)` includes front and back internal children.

**Value:** The current child count.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-children"></a>
### `public IReadOnlyList<Node> Children { get; }`

Gets a live read-only view of ordinary direct children. Internal children remain in the engine hierarchy and are exposed through `GetChildren(true)` or `GetChild(index, true)`.

**Value:** A view backed by this node's child list; later hierarchy changes are visible through it.

<a id="p-electron2d-node-inputenabled"></a>
### `public bool InputEnabled { get; set; }`

Gets or sets whether this node receives the first input-propagation stage.

**Value:** False initially; ready notification enables an overridden callback before OnReady.

**Remarks:** Eligible nodes are visited in reverse depth-first order before unhandled-input stages. Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot. Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation. Packed scenes store this configuration before ready applies automatic callback preparation.

**System.InvalidOperationException:** Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-isinsidetree"></a>
### `public bool IsInsideTree { get; }`

Gets whether this node currently belongs to a scene tree.

**Value:** true when Node.Tree is non-null.

**Remarks:** Structural parentage alone does not imply active membership. This stored observation remains available after disposal; coordinate reads with tree transitions.

<a id="p-electron2d-node-isnodeready"></a>
### `public bool IsNodeReady { get; }`

Gets whether SceneTree-managed ready delivery has occurred since construction or the last ready reset.

**Value:** The stored ready state. It remains true after detachment until `Node.RequestReady` is called.

**Remarks:** Manual `ElectronObject.Notify(int)` delivery of `Node.NotificationReady` does not change this value. Attached reads require the owner/current processing group; detached/disposed nodes retain this stored observation.

**System.InvalidOperationException:** Attached access is outside the owner or current processing group.

<a id="p-electron2d-node-isqueuedfordeletion"></a>
The inherited [ElectronObject.IsQueuedForDeletion](ElectronObject.md#p-electron2d-electronobject-isqueuedfordeletion) covers both QueueFree and explicit SceneTree.QueueDelete, including claimed execution until terminal cleanup. Cancellation only consumes the current unclaimed QueueFree generation; children are marked only by their own requests.

<a id="p-electron2d-node-name"></a>
### `public string Name { get; set; }`

Gets or sets the node name used in sibling lookup and paths.

**Value:** The nonblank name, initialized to `ElectronObject.ClassName`.

**Remarks:** Every `.`, `:`, `@`, `/`, `"` and `%` in an authored name becomes `_` before readable sibling collision resolution. Unicode and other characters remain exact. Every accepted assignment, including the same input, propagates `Node.NotificationPathRenamed` through the subtree, even while detached. An active node then raises `Node.Renamed`, `SceneTree.NodeRenamed` and `SceneTree.TreeChanged`. Names and path caches commit before observers; errors are combined after remaining descendants and event phases are attempted. The first snapshot capacity growth and new normalized/collision strings allocate; prepared assignments of existing clean names do not. Renaming to a name already claimed in the same owner scope clears `UniqueNameInOwner`. Scene reconstruction preserves validated stored internal names directly, including generated `@Class@number` names.

**System.ArgumentException:** The assigned name is null, empty or whitespace-only.

**System.InvalidOperationException:** An attached node is mutated off the owner thread.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**System.AggregateException:** One or more path, node-renamed, or tree-renamed callbacks fail after the assignment commits.

<a id="p-electron2d-node-owner"></a>
### `public Node? Owner { get; set; }`

Gets or sets the borrowed authoring owner used for packed-scene storage.

**Value:** The live owner, or null when unassigned, disposed or collected. Assignment requires an ancestor.

**Remarks:** Owner does not select a physical world or own node lifetime. Detached topology changes retain the previous owner and its unique-name registration, even when it is no longer an ancestor. Active removal clears unreachable owners after structural callbacks, before TreeExited. Owner disposal releases metadata without disposing separately parented nodes. Reparent restores its reachable outside owner only on ordinary descendants sharing it. Reads require a live receiver and coordination with topology/owner mutation. Attached writes require the scene owner thread; capture rejects writes, including unchanged values. Invalid assignments preserve the prior owner. Assigning a new owner may revoke UniqueNameInOwner when that owner already has a live claim.

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

Gets the direct structural parent.

**Value:** The parent, or null for a parentless node. A descendant may retain a parent while outside every tree.

**Remarks:** This stored observation remains available after disposal. Coordinate reads with topology mutation; it is not an atomic hierarchy snapshot or permission to mutate another scene.

<a id="p-electron2d-node-physicsinterpolationmode"></a>
### `public PhysicsInterpolationMode PhysicsInterpolationMode { get; set; }`

Defaults to `Inherit` for ordinary nodes, with a root resolving to On. `Control` starts Off; `Viewport`, including `Window` and `SubViewport`, starts On independently of ancestors. A descendant may opt back in. Changing an attached enabled policy resets presentation history for the subtree. The selected mode is stored by PackedScene. This property does not change logical transforms or processing callbacks.

**ArgumentOutOfRangeException:** The value is not Inherit, On or Off.

**InvalidOperationException:** Attached mutation is off the owner thread or scene capture is active.

**ObjectDisposedException:** The node is disposed.

<a id="p-electron2d-node-physicsprocessdeltatime"></a>
### `public double PhysicsProcessDeltaTime { get; }`

Gets the current or most recent physics-frame duration of the attached scene tree.

**Value:** The tree's scaled physics duration in seconds, or zero while detached or before its first physics frame.

**Remarks:** `Engine` applies `Engine.TimeScale` before an Engine-driven delivery.

**Remarks:** All attached nodes observe the same lane duration, including disabled/paused nodes. SceneTree commits it before frame-start callbacks. Manual `ElectronObject.Notify(int)` does not advance it. Attached reads require the owner thread; disposed reads reject.

<a id="p-electron2d-node-physicsprocessenabled"></a>
### `public bool PhysicsProcessEnabled { get; set; }`

Gets or sets whether this node participates in host-driven physics-process frames.

**Value:** False initially; ready notification enables an overridden callback before OnReady.

**Remarks:** A node absent from the captured physics schedule starts on the next physics frame. Disabling a captured node suppresses later delivery immediately. An existing internal participant may enable its public callback in that frame. Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot. Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation. Packed scenes store this configuration before ready applies automatic callback preparation.

**System.InvalidOperationException:** Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-physicsprocesspriority"></a>
### `public int PhysicsProcessPriority { get; set; }`

Gets or sets this node's ascending physics-process order key.

**Value:** Any integer; the default is zero. Equal priorities retain captured tree order within the group.

**Remarks:** Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot. Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.

**System.InvalidOperationException:** Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-processdeltatime"></a>
### `public double ProcessDeltaTime { get; }`

Gets the current or most recent process-frame duration of the attached scene tree.

**Value:** The tree's scaled process duration in seconds, or zero while detached or before its first process frame.

**Remarks:** `Engine` applies `Engine.TimeScale` before an Engine-driven delivery.

**Remarks:** All attached nodes observe the same lane duration, including disabled/paused nodes. SceneTree commits it before frame-start callbacks. Manual `ElectronObject.Notify(int)` does not advance it. Attached reads require the owner thread; disposed reads reject.

<a id="p-electron2d-node-processenabled"></a>
### `public bool ProcessEnabled { get; set; }`

Gets or sets whether this node participates in host-driven process frames.

**Value:** False initially; ready notification enables an overridden callback before OnReady.

**Remarks:** A node absent from the captured idle schedule starts on the next idle frame. Disabling a captured node suppresses later delivery immediately. An existing internal participant may enable its public callback in that frame. Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot. Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation. Packed scenes store this configuration before ready applies automatic callback preparation.

**System.InvalidOperationException:** Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-processmode"></a>
### `public ProcessMode ProcessMode { get; set; }`

Gets or sets the pause policy used by both process callback lanes.

**Value:** `ProcessMode.Inherit` initially; an entering root becomes `Pausable`.

**Remarks:** Detached changes are silent. An attached root rejects `Inherit`. Attached changes notify this node and inheriting descendants parent-first, including internal children and stopping at explicit modes. Changed callback eligibility delivers `Paused/Unpaused` before a changed disabled state delivers `Disabled/Enabled`. Scene collision objects apply [DisableMode](CollisionObject.md#disablemode); policy changes preflight solver ownership before committing. Callback failures preserve the committed mode and aggregate after remaining notifications. Nested policy/pause changes suppress stale delivery, and moved or retired attachments skip. Reads allow the owner/current processing group; writes require the scene owner. Prepared subtree capacity and callback depth are retained. See [pause contract](../components/scene-tree.md#process-policy-and-physics-pause).

**System.ArgumentOutOfRangeException:** The assigned enum value is undefined.

**System.InvalidOperationException:** Access is outside the scene owner/current processing group, mutation is off the owner thread, an attached root selects Inherit, or physical participation is borrowed.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

**System.AggregateException:** Transition callbacks throw after remaining affected notifications are attempted.

<a id="p-electron2d-node-processpriority"></a>
### `public int ProcessPriority { get; set; }`

Gets or sets this node's ascending process-frame order key.

**Value:** Any integer; the default is zero. Equal priorities retain captured tree order within the group.

**Remarks:** Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot. Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.

**System.InvalidOperationException:** Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-scenefilepath"></a>
### `public string SceneFilePath { get; set; }`

Gets or sets this node's scene source path metadata.

**Value:** Non-null text, initially empty. External packed-scene instantiation assigns its resource path to the root.

**Remarks:** The value is not inherited. Assignment does not load a file or replace the linked template. Every valid write, including an equal value, emits `EditorStateChanged` in the selected edited subtree; blocking suppresses delivery, and observer errors propagate after commitment. The ordinary typed descriptor is writable and nonstored. PackedScene separately retains nonroot origin metadata; a reconstructed root takes its template's path. Attached writes require the scene owner and reject capture or physical borrowing. Lifetime is rechecked after derived validation. Coordinated reads need a live receiver.

**System.ArgumentNullException:** The assigned value is null.

**System.InvalidOperationException:** Mutation is unavailable or attached access is off the scene owner.

**System.ObjectDisposedException:** The node is disposing or disposed.

<a id="p-electron2d-node-tree"></a>
### `public SceneTree? Tree { get; }`

Gets the active scene tree containing this node.

**Value:** The associated tree, or null outside a tree.

**Remarks:** Entry assigns this association before callbacks. Departure retains it through NodeRemoved and ChildExitingTree, then clears the whole departing subtree before TreeExited. Stored observation remains available after disposal; coordinate it with topology mutation.

<a id="p-electron2d-node-unhandledinputenabled"></a>
### `public bool UnhandledInputEnabled { get; set; }`

Gets or sets whether this node receives input left unhandled by earlier stages.

**Value:** False initially; ready notification enables an overridden callback before OnReady.

**Remarks:** This final stage runs for every event that remains unhandled. Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot. Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation. Packed scenes store this configuration before ready applies automatic callback preparation.

**System.InvalidOperationException:** Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

<a id="p-electron2d-node-unhandledkeyinputenabled"></a>
### `public bool UnhandledKeyInputEnabled { get; set; }`

Gets or sets whether this node receives unhandled keyboard events before general unhandled input.

**Value:** False initially; ready notification enables an overridden callback before OnReady.

**Remarks:** The stage is skipped for non-keyboard events and after SceneTree.SetInputAsHandled. Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot. Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation. Packed scenes store this configuration before ready applies automatic callback preparation.

**System.InvalidOperationException:** Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.

**System.ObjectDisposedException:** The node is disposing on another thread or has finished disposing.

## Method Descriptions

<a id="m-electron2d-node-atr-system-string-system-string"></a>
### `public string Atr(string message, string? context = null)`

Returns the source message when `CanAutoTranslate()` is false; otherwise delegates to inherited `Tr`, including its per-object message enablement, domain and context behavior. A null message throws `ArgumentNullException`. On a Control, a null or empty context selects that control's local `TranslationContext`; a nonempty argument overrides it. Ordinary nodes keep their supplied context.

<a id="m-electron2d-node-atrn-system-string-system-string-system-int64-system-string"></a>
### `public string AtrN(string singular, string plural, long count, string? context = null)`

Returns singular only for count one, or plural otherwise, while automatic translation is disabled. Otherwise delegates to inherited `TrN`, using the same Control default-context rule as `Atr`. Null source forms throw `ArgumentNullException`.

<a id="m-electron2d-node-canautotranslate"></a>
### `public bool CanAutoTranslate()`

Returns whether this node or its nearest non-inheriting ancestor uses `Always`. Detached roots with `Inherit` resolve as enabled. This policy is separate from per-object `CanTranslateMessages`.

<a id="m-electron2d-node-settranslationdomaininherited"></a>
### `public void SetTranslationDomainInherited()`

Clears an explicit domain override so this node resolves its parent's current domain, or the main empty domain when parentless. Affected active inheriting descendants receive `NotificationTranslationChanged`; explicit descendant overrides stop propagation. Attached changes require the owner thread.

<a id="m-electron2d-node-hasnode-system-string"></a>
### `public bool HasNode(string path)`

Returns whether `path` resolves through the relative or absolute node-path rules, including owner-scoped `%Name`. Detached hierarchies support relative paths; absolute paths require active tree membership and a root-name segment. Empty paths and a lone colon return false; whitespace is matched literally. Null and empty intermediate property subnames reject. Attached queries require the owner thread or current processing group.

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

<a id="m-electron2d-node-getorphannodeids"></a>
### `public static ulong[] GetOrphanNodeIDs()`

Debug returns a caller-owned array of live nodes outside active SceneTrees, sorted
by InstanceID. Detached roots, ordinary/internal descendants and physical nodes
bound directly to a space all participate. Disposing and collected objects do
not. The weak registry does not extend node lifetime. Release returns an empty
array and does not register nodes. This cold diagnostic allocates in Debug and
does not provide an atomic snapshot during concurrent lifetime/tree transitions.

<a id="m-electron2d-node-printorphannodes"></a>
### `public static void PrintOrphanNodes()`

Debug emits one row per sampled node through OS.LogMessage: ID, root-relative
hierarchy name and actual compiled class name, in ID order. All text is captured
before logging, so logger edits/disposal cannot invalidate later rows. Release
emits nothing to either the console or registered loggers. No live script-source
association is inferred. See [orphan diagnostics](../components/scene-hierarchy.md#orphan-node-diagnostics).

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
### `public void AddChild(Node child, InternalMode internalMode = InternalMode.Disabled)`

Adds a detached node to the end of the selected ordinary, internal-front or internal-back partition. Unique names stay unchanged; collisions receive a generated `@Class@number` name. See [InternalMode](Node.InternalMode.md). Ordinary children stay between front and back children; all children still enter, process, draw and receive notifications.

**Parameter `child`:** The live node to adopt.

**Parameter `internalMode`:** Disabled by default; Front and Back are the two internal partitions. Undefined values throw `ArgumentOutOfRangeException` before insertion.

**Remarks:** If this node is active, the child's subtree enters immediately and receives ready where eligible.

**System.ArgumentNullException:** `child` is `null`.

**System.ArgumentException:** `child` is this node.

**System.InvalidOperationException:** The operation would create a cycle, the child already has a parent or tree, or mutation occurs off the owner thread.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing, or disposal of `child` has started.

**System.AggregateException:** One or more structural, lifecycle, notification, or event callbacks fail after insertion begins.

<a id="m-electron2d-node-addchild-readable"></a>
### `public void AddChild(Node child, bool forceReadableName, InternalMode internalMode = InternalMode.Disabled)`

Uses the same insertion and validation contract. With `forceReadableName=true`,
collisions advance a readable decimal suffix (starting at 2), using the live
ProjectSettings.NodeNameNumberSeparator. Existing suffix width and arbitrarily
long decimal carry are preserved. False selects the ordinary generated policy.
The existing InternalMode overload retains its call positions and false policy.
Naming commits before parenting/entry callbacks. See [child names](../components/scene-hierarchy.md#child-name-collisions).

<a id="m-electron2d-node-addsibling-electron2d-scenenode"></a>
### `public void AddSibling(Node sibling)`

Inserts a detached node immediately after this node in its parent's child order.

**Parameter `sibling`:** The live node to insert.

**System.ArgumentNullException:** `sibling` is `null`.

**System.ArgumentException:** `sibling` is the destination parent.

**System.InvalidOperationException:** This node has no parent, insertion would create a cycle, the sibling is already attached, or mutation occurs off the owner thread.

**System.ObjectDisposedException:** This node or its parent is disposing on another thread or has finished disposing, or disposal of `sibling` has started.

**System.AggregateException:** One or more structural, lifecycle, notification, or event callbacks fail after insertion begins.

<a id="m-electron2d-node-addsibling-readable"></a>
### `public void AddSibling(Node sibling, bool forceReadableName)`

Inserts immediately after this node in its current internal/ordinary partition,
using the same readable/generated naming policy as AddChild. Names are unique
across all three partitions. The original overload selects false. Its validation,
callback failure and lifetime rules remain in force.

<a id="m-electron2d-node-addtogroup-system-string-system-boolean"></a>
### `public void AddToGroup(string group, bool persistent = false)`

Adds this node to a case-sensitive group.

**Parameter `group`:** The nonempty group identifier; whitespace and Unicode are preserved.

**Parameter `persistent`:** Whether packed scenes containing this node should retain the membership.

**Remarks:** The first addition chooses persistence. Repeated addition leaves it unchanged; remove/readd selects another policy.

**System.ArgumentException:** `group` is empty.

**System.ArgumentNullException:** `group` is `null`.

**System.InvalidOperationException:** Attached access is outside the owner/current processing group or capture is active.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-canprocess"></a>
### `public bool CanProcess()`

Determines whether the resolved process mode allows callbacks in the current tree pause state.

**Returns:** `false` while detached or disabled; otherwise the result of the resolved pause policy.

**System.InvalidOperationException:** Access is outside the scene owner or current processing group.

**System.ObjectDisposedException:** This node or its tree is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-cancelfree"></a>
### `public bool CancelFree()`

Atomically cancels a pending deletion request.

**Returns:** `true` when a request was pending; otherwise `false`.

**Remarks:** Cancellation invalidates exactly the current request, so a stale entry cannot consume a later reissued request. A later QueueFree gets a new FIFO position. Returns false once the request has been claimed for destruction. This does not veto an executing Dispose or cancel an independent SceneTree.QueueDelete.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-createsceneinstancefactory"></a>
### `protected virtual Func<Node> CreateSceneInstanceFactory()`

Creates a reusable factory for packed-scene instances of this exact runtime node type.

**Returns:** A non-null factory that creates a fresh node of the exact same runtime type.

**Remarks:** The base implementation supports only an exact `Node`. Derived node types that can be packed must return a static, non-capturing factory that remains valid after the source node is disposed and creates a live, detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore the instance state.

**System.NotSupportedException:** A derived node has not explicitly supplied an instancing factory.

<a id="m-electron2d-node-createtween"></a>
### `public Tween CreateTween()`

Creates a bound tween in this node's tree or the current application scene tree.

**Returns:** A running empty tween that halts while this node is detached and is killed when this node is disposed.

**Remarks:** An attached node selects its own tree. A detached node selects `Engine.MainLoop` when it is a `SceneTree` and waits until it joins that tree. The caller must append at least one tweener before the next eligible matching frame, including a zero-delta frame.

**System.InvalidOperationException:** No scene tree is available or the call is made off its owner thread.

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

<a id="m-electron2d-node-findchild-system-string-system-boolean-system-boolean"></a>
### `public Node FindChild(string pattern, bool recursive = true, bool owned = true)`

Finds the first descendant whose name matches a wildcard pattern.

**Parameter `pattern`:** A case-sensitive name pattern: * matches any sequence and ? one Unicode character except a period.

**Parameter `recursive`:** Whether descendants below direct children are searched, including internal children.

**Parameter `owned`:** Whether branches without an owner are skipped; true by default.

**Returns:** The first matching node in depth-first pre-order, or `null`.

**System.ArgumentException:** `pattern` is empty.

**System.ArgumentNullException:** `pattern` is `null`.

**System.InvalidOperationException:** An attached search runs outside its scene owner or current processing group.

**System.ObjectDisposedException:** This node or a recursively searched node is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-findchild-1-system-string-system-boolean-system-boolean"></a>
### `public TNode FindChild<TNode>(string pattern = "*", bool recursive = true, bool owned = true)`

Finds the first descendant of a requested type whose name matches a wildcard pattern.

**Type parameter `TNode`:** The required node subtype.

**Parameter `pattern`:** A case-sensitive name pattern: * matches any sequence and ? one Unicode character except a period.

**Parameter `recursive`:** Whether descendants below direct children are searched, including internal children.

**Parameter `owned`:** Whether branches without an owner are skipped; true by default.

**Returns:** The first typed match in depth-first pre-order, or `null`.

**System.ArgumentException:** `pattern` is empty.

**System.ArgumentNullException:** `pattern` is `null`.

**System.InvalidOperationException:** An attached search runs outside its scene owner or current processing group.

**System.ObjectDisposedException:** This node or a recursively searched node is disposing on another thread, or has finished disposing.

<a id="m-electron2d-node-findchildren-system-string-system-boolean-system-boolean"></a>
### `public IReadOnlyList<Node> FindChildren(string pattern, bool recursive = true, bool owned = true)`

Finds all descendants whose names match a wildcard pattern.

**Parameter `pattern`:** A case-sensitive name pattern: * matches any sequence and ? one Unicode character except a period.

**Parameter `recursive`:** Whether descendants below direct children are searched, including internal children.

**Parameter `owned`:** Whether branches without an owner are skipped; true by default.

**Returns:** A read-only snapshot in depth-first pre-order.

**System.ArgumentException:** `pattern` is empty.

**System.ArgumentNullException:** `pattern` is `null`.

**System.InvalidOperationException:** An attached search runs outside its scene owner or current processing group.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-findchildren-1-system-string-system-boolean-system-boolean"></a>
### `public IReadOnlyList<TNode> FindChildren<TNode>(string pattern = "*", bool recursive = true, bool owned = true)`

Finds all descendants of a requested type whose names match a wildcard pattern.

**Type parameter `TNode`:** The required node subtype.

**Parameter `pattern`:** A case-sensitive name pattern: * matches any sequence and ? one Unicode character except a period. An empty pattern selects every name.

**Parameter `recursive`:** Whether descendants below direct children are searched, including internal children.

**Parameter `owned`:** Whether branches without an owner are skipped; true by default.

**Returns:** A read-only typed snapshot in depth-first pre-order.

**System.ArgumentNullException:** `pattern` is `null`.

**System.InvalidOperationException:** An attached search runs outside its scene owner or current processing group.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-findparent-system-string"></a>
### `public Node FindParent(string pattern)`

Finds the nearest ancestor whose name matches a wildcard pattern.

**Parameter `pattern`:** A case-sensitive name pattern: * matches any sequence and ? one Unicode character except a period.

**Returns:** The nearest matching ancestor, or `null`.

**System.ArgumentNullException:** `pattern` is `null`.

**System.InvalidOperationException:** An attached search runs outside its scene owner or current processing group.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getchild-system-int32"></a>
### `public Node GetChild(int index, bool includeInternal = false)`

Gets a direct child by index.

**Parameter `index`:** The index in the selected view; negative values count from the end.

**Parameter `includeInternal`:** False by default. True includes front and back internal children in scene order.

**Returns:** The selected direct child.

**System.ArgumentOutOfRangeException:** This node has no children or `index` is outside the valid range.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

### `public int GetChildCount(bool includeInternal = false)` and `public Node[] GetChildren(bool includeInternal = false)`

`GetChildCount` returns the count in the selected view. `GetChildren` returns a new caller-owned array of borrowed node identities in that order. The default view omits front/back internal children; `includeInternal: true` includes every direct child. Attached queries use the scene owner thread, and disposed nodes reject both methods.

<a id="m-electron2d-node-getgroups"></a>
### `public IReadOnlyList<string> GetGroups()`

Returns this node's group memberships.

**Returns:** A read-only snapshot sorted using ordinal string order.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.InvalidOperationException:** Attached access is outside the scene owner/current processing group.

<a id="m-electron2d-node-getindex"></a>
### `public int GetIndex(bool includeInternal = false)`

Gets this node's index in its parent's ordered child list.

**Returns:** The zero-based ordinary sibling index, or full scene index when `includeInternal` is true; -1 when detached. An internal child queried without `includeInternal` throws `InvalidOperationException`.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

### Internal child partitions

`AddChild` inserts Front children before ordinary children and Back children afterward. `AddSibling` preserves the current node's partition. `MoveChild` indexes and reorders only within its child's partition; the ordinary range accepts its end index as its last position. `RemoveChild` detaches without disposal, and a later ordinary `AddChild` resets the detached node's mode. `Reparent` also makes it ordinary. Ordinary scene capture omits internal children; a container constructor reconstructs its own internals on instantiation. Internal nodes remain in lifecycle, processing, theme, rendering and input traversals. [ScrollInternalNodeTests](../../tests/Electron2D.Tests/ScrollInternalNodeTests.cs) checks both views, ordering, movement, process callbacks and packed-scene omission.

<a id="m-electron2d-node-getnode-system-string"></a>
### `public Node GetNode(string path)`

Resolves a required relative or absolute node path, including owner-scoped `%Name` segments.

**Parameter `path`:** A slash-separated path supporting ., .., `%Name`, and an optional absolute root-name segment.

**Returns:** The resolved node.

**Remarks:** Absolute paths require an active scene tree and the root-name segment; detached hierarchies support relative paths. Repeated separators are ignored. Valid colon-separated property subnames do not participate in node traversal; property-only paths address this node. Empty paths and a lone colon do not resolve. Required lookup throws KeyNotFoundException on a miss. Exact lookups include internal and unowned nodes and allocate no managed memory.

**System.ArgumentException:** `path` contains an empty intermediate property subname.

**System.ArgumentNullException:** `path` is `null`.

**System.InvalidOperationException:** An attached lookup runs outside the owner thread or current processing group.

**KeyNotFoundException:** No node exists at the requested path.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getnodeornull-system-string"></a>
### `public Node GetNodeOrNull(string path)`

Attempts to resolve a relative or absolute node path, including owner-scoped `%Name` segments.

**Parameter `path`:** A slash-separated path supporting ., .., `%Name`, and an optional absolute root-name segment.

**Returns:** The resolved node, or `null` for an empty path, a lone colon or when traversal cannot continue.

**Remarks:** Absolute paths require an active scene tree and the root-name segment; detached hierarchies support relative paths. Repeated separators are ignored. Valid colon-separated property subnames do not participate in node traversal; property-only paths address this node. Empty paths and a lone colon do not resolve. Required lookup throws KeyNotFoundException on a miss. Exact lookups include internal and unowned nodes and allocate no managed memory.

**System.ArgumentException:** `path` contains an empty intermediate property subname.

**System.ArgumentNullException:** `path` is `null`.

**System.InvalidOperationException:** An attached lookup runs outside the owner thread or current processing group.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getnode-1-system-string"></a>
### `public TNode GetNode<TNode>(string path)`

Resolves a required relative or absolute path to a requested node type.

**Type parameter `TNode`:** The required node subtype.

**Parameter `path`:** A slash-separated node path.

**Returns:** The resolved node cast to `TNode`.

**System.ArgumentException:** `path` contains an empty intermediate property subname.

**System.ArgumentNullException:** `path` is `null`.

**System.InvalidCastException:** The resolved node is not a `TNode`.

**System.InvalidOperationException:** An attached lookup runs outside the owner thread or current processing group.

**KeyNotFoundException:** No node exists at the requested path.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getpath"></a>
### `public string GetPath()`

Gets this node's cached absolute path in its active scene tree.

**Returns:** A slash-prefixed path including the root name, or an empty string outside an active tree.

**Remarks:** The immutable string is cached until ancestry, tree membership or an ancestor name changes. Cache invalidation precedes path notifications; exit callbacks retain the path until membership ends. Attached reads require the owner thread or current processing group.

**System.InvalidOperationException:** An attached read runs outside the owner thread or current processing group.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-getpathto-electron2d-node-system-boolean"></a>
### `public string GetPathTo(Node node, bool useUniquePath = false)`

Builds a relative path from this node to another node in the same hierarchy. With `useUniquePath`, an eligible owner-scoped unique destination replaces the preceding route; otherwise an eligible unique source may prefix the upward route, even if it makes the path longer.

**Parameter `node`:** The destination node.

**Parameter `useUniquePath`:** Whether to use owner-scoped `%Name` segments.

**Returns:** . for this node, otherwise a slash-separated sequence of .., child names and optional unique names.

**System.ArgumentNullException:** `node` is `null`.

**System.InvalidOperationException:** The nodes do not share a hierarchy root, or the attached receiver is accessed outside the owner thread or current processing group.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing, or disposal of `node` has started.

<a id="m-electron2d-node-getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Extends base typed descriptors with neutral name, unique-name, process and input state for inspection and packed scenes.

**Remarks:** Appends this class's typed hierarchy, ownership, and processing descriptors to the inherited descriptors.

<a id="m-electron2d-node-getviewport"></a>
### `public Viewport? GetViewport()`

Returns the cached nearest active viewport, including this node when it is a
Viewport. Returns null while detached, even for Viewport/Window instances, or
when an active hierarchy has no viewport. A nested SubViewport selects itself;
its ordinary descendants share that association.

The reference is published before enter callbacks, remains available during exit
callbacks and is cleared before TreeExited. Reparenting and cross-tree transfer
recompute it on entry. Manual lifecycle notifications do not change association;
PackedScene reconstructs it during activation rather than storing a live pointer.
Reading the cached borrowed reference is allowed from another thread or processing
group. It neither traverses the hierarchy nor grants permission to operate on the
returned viewport. Coordinate reads, world/backend mutation and GUI access retain
their own guards and lifetime rules.

**System.ObjectDisposedException:** The node is disposed.

<a id="m-electron2d-node-getwindow"></a>
### `public Window? GetWindow()`

Returns the active containing Window, including this node when it is a Window.
Walks the cached viewport-parent chain, crossing nested SubViewport boundaries
until it reaches a Window. Returns null while detached or without a containing
Window, including an offscreen root. Visibility does not remove an attached
window's association. The result remains available during exit callbacks and
clears before TreeExited.

The scene owner or this node's current processing group may query its window.
Reading an own-group node does not require joining the containing window's group.
Other processing scopes and unrelated threads throw InvalidOperationException.
The returned window remains borrowed and retains its own operation guards.

**System.ObjectDisposedException:** The node is disposed.

**System.InvalidOperationException:** The node is outside the permitted scene processing scope.

<a id="m-electron2d-node-getlastexclusivewindow"></a>
### `public Window? GetLastExclusiveWindow()`

Returns the containing Window or the last child reached through its active
exclusive transient chain. Returns null outside the active scene tree or when
there is no containing Window. It uses the same active containing window as
`GetWindow()`.

A visible attached Window joins this chain only when both Transient and Exclusive
are enabled. Focus/display order does not select the chain. The latest conflicting
activation wins with a diagnostic; retiring that selection does not reactivate
older siblings. Visibility, flag changes, detach/reparent and disposal update the
links before scene/visibility/focus observers. All five Window.PopupExclusive
entry points use this same query.

The query allocates no managed storage and accepts the scene owner or this node's
own processing group. Other processing scopes throw InvalidOperationException;
finished disposal throws ObjectDisposedException. Independent native child-window
support remains a separate Window capability.

<a id="m-electron2d-node-isancestorof-electron2d-scenenode"></a>
### `public bool IsAncestorOf(Node node)`

Determines whether this node is a strict ancestor of another node.

**Parameter `node`:** The node whose parent chain is inspected.

**Returns:** true when this node appears in the parent chain; otherwise false, including a self query.

**Remarks:** Uses structural parents independently of active tree membership. Coordinate reads with topology changes; this query does not grant mutation access or require the current processing group.

**System.ArgumentNullException:** node is null.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-isingroup-system-string"></a>
### `public bool IsInGroup(string group)`

Determines whether this node belongs to a case-sensitive group.

**Parameter `group`:** The literal group identifier; an empty identifier has no membership.

**Returns:** `true` when this node is a member; otherwise `false`.

**System.ArgumentNullException:** `group` is `null`.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.InvalidOperationException:** Attached access is outside the scene owner/current processing group.

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

**Remarks:** `Node.Tree` is already assigned. The complete enter notification returns before this hook; then TreeEntered, tree NodeAdded and parent ChildEnteredTree run before descendants. Managed transitions run on the owner thread; manual enter notification does not invoke this hook.

<a id="m-electron2d-node-onexittree"></a>
### `protected virtual void OnExitTree()`

Called synchronously when this node exits an active scene tree.

**Remarks:** Descendants have exited in reverse sibling order and Tree remains assigned. This hook precedes TreeExiting, exit notification and engine membership/physical release. Manual exit notification does not invoke it. Failures cannot suppress remaining exit stages.

<a id="m-electron2d-node-oninput-electron2d-inputevent"></a>
### `protected virtual void OnInput(InputEvent event)`

Receives an input event during the first scene-input propagation stage.

**Parameter `event`:** The live caller-owned event being dispatched.

**Remarks:** The callback runs synchronously on the scene-tree owner thread when `Node.InputEnabled` is true and `Node.CanProcess` allows the node. Call `SceneTree.SetInputAsHandled` to stop later stages.

<a id="m-electron2d-node-onnotification-system-int32"></a>
### `protected override void OnNotification(int what)`

Handles an engine notification delivered to this object.

**Remarks:** Maps ready/process/physics-process notifications to typed callbacks. Enter/exit hooks belong to managed tree transitions and are not called by manual tree notification. Manual ready still runs OnReady without consuming managed readiness; process notifications do not advance stored deltas. Pause/application-suspend notifications retain interpolation reset policy.

<a id="m-electron2d-node-onphysicsprocess-system-double"></a>
### `protected virtual void OnPhysicsProcess(double delta)`

Called during an eligible host-driven physics-process frame.

**Parameter `delta`:** The finite non-negative physics-step delta in seconds.

**Remarks:** Ready enables an actual override before OnReady; later callback-switch writes control delivery. Runs on the selected processing group before the tree-owned numerical step, independently of that solver thread. PhysicsProcessDeltaTime is updated before delivery; the callback itself does not integrate the world. Callback failures aggregate after remaining eligible deliveries and frame work. Input, rendering and physical authoring retain their own access guards.

<a id="m-electron2d-node-onprocess-system-double"></a>
### `protected virtual void OnProcess(double delta)`

Called during an eligible host-driven process frame.

**Parameter `delta`:** The finite non-negative frame delta in seconds.

**Remarks:** Ready enables an actual override before OnReady; later callback-switch writes control delivery. Runs on the selected processing group: the scene owner for a main group or a joined worker for a subthread group. ProcessDeltaTime is updated before delivery. Callback failures aggregate after remaining eligible deliveries and frame work. Input, rendering and physical authoring retain their own access guards.

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

**Remarks:** A request made while already detached is queued if the node later enters a tree. Removing the node before its current tree flushes does not cancel deletion: that tree disposes the detached node at its safe point. If the node has entered another tree first, the old entry leaves the request intact for the new tree. Repeated calls are idempotent. Cancellation and reissue create a new request identity and FIFO position. A request reissued during a captured deletion batch waits for the next batch. The method may be called from a non-owner thread.

**System.InvalidOperationException:** This node is the active scene-tree root.

**System.ObjectDisposedException:** Deletion publication has closed, or this node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-removechild-electron2d-scenenode"></a>
### `public bool RemoveChild(Node child)`

Removes a direct child without disposing it.

**Parameter `child`:** The node to detach.

**Returns:** true when the node was a direct child and was detached; otherwise false.

**Remarks:** An active subtree exits in reverse sibling order, children before parents. Parent is cleared before structural callbacks; unreachable owners clear afterward, before TreeExited. Detached removal retains borrowed authoring owners. Neither form disposes the receiver or its children. An absent direct child returns false.

**System.ArgumentNullException:** child is null.

**System.InvalidOperationException:** An attached node is mutated off the owner thread, this parent is exiting or propagating a notification, or the child is in tree lifecycle delivery.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

**System.AggregateException:** One or more lifecycle, notification, or event callbacks fail after removal begins.

<a id="m-electron2d-node-removefromgroup-system-string"></a>
### `public bool RemoveFromGroup(string group)`

Removes this node from a case-sensitive group.

**Parameter `group`:** The literal group identifier; an empty identifier has no membership.

**Returns:** `true` when membership existed and was removed; otherwise `false`.

**System.ArgumentNullException:** `group` is `null`.

**System.InvalidOperationException:** Attached access is outside the scene owner/current processing group or capture is active.

**System.ObjectDisposedException:** This node is disposing on another thread or has finished disposing.

<a id="m-electron2d-node-reparent-electron2d-node-system-boolean"></a>
### `public virtual void Reparent(Node newParent, bool keepGlobalTransform = true)`

Moves this non-root node under a new parent.

**Parameter `newParent`:** The live destination parent.

**Parameter `keepGlobalTransform`:** Whether a derived placement model preserves its global transform. The neutral base has no transform. The default is true.

**Remarks:** Validate the destination and its child-order barrier before removing the old parent. Append as an ordinary child. After removal callback failures, still attempt insertion and restoration of the reachable outside owner on ordinary descendants sharing it; aggregate original errors after all possible stages. Local subtree owners remain valid. Same-parent calls are silent but retain lifetime, capture, thread and lifecycle guards. A callback-retired destination is not resurrected and may leave a live detached receiver with an explicit error. Full structural changes and snapshots are cold; derived placement restoration follows attachment.

**System.ArgumentNullException:** newParent is null.

**System.ArgumentException:** The destination validation rejects this node as its own child.

**System.InvalidOperationException:** This node has no parent, the move creates a cycle, either attached hierarchy is accessed off its owner thread, this node or a parent is in protected lifecycle/notification delivery, or a derived placement model rejects its destination.

**System.ObjectDisposedException:** This node or newParent is disposing on another thread or has finished disposing.

**System.AggregateException:** One or more structural, lifecycle, notification, or event callbacks fail after reparenting begins.

<a id="m-electron2d-node-replaceby-electron2d-node-system-boolean"></a>
### `public void ReplaceBy(Node node, bool keepGroups = false)`

Replaces this node at its sibling index with a live detached node. The replacement keeps its existing children, then receives this node's direct children in order. Descendants owned by this node become owned by the replacement; other still-valid ancestor owners are restored. A packed-scene root transfers ownership of its scene-local resources and their `GetLocalScene()` association.

**Parameter `node`:** The parentless replacement. It must not conflict with another sibling name or with the names of children to be transferred.

**Parameter `keepGroups`:** Copies group memberships and their persistent flags when true; the default is false.

**Remarks:** The original node remains detached and is not disposed. `ReplacingBy` runs after the replacement enters the former parent and before children move. An active `SceneTree.Root` has stable identity and cannot be replaced. Removing a selected `CurrentScene` or `EditedSceneRoot` clears that selection; callers may select the replacement afterward. Existing typed event subscriptions keep their original targets. Callback failures are collected while structural work continues; a child that cannot enter the replacement is restored under the old node.

**System.ArgumentNullException:** `node` is null.

**System.ArgumentException:** `node` is this node.

**System.InvalidOperationException:** A replacement is attached or an ancestor, a protected lifecycle is running, the active root is selected, or the caller is off the scene owner thread.

**System.ObjectDisposedException:** Either node is disposed.

**System.AggregateException:** One or more structural or lifecycle callbacks fail after replacement begins.

<a id="m-electron2d-node-requestready"></a>
### `public void RequestReady()`

Requests ready delivery the next time SceneTree attachment reaches the ready phase.

**Remarks:** The method rearms only this node; it never delivers ready immediately or changes descendants. A request during ready applies on the next attachment. The owner/current processing group may update it, including on a physical node, without waking it.

**System.InvalidOperationException:** Attached access is outside the owner/current processing group or scene capture is active.

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

**Remarks:** The first argument is the publishing parent and the second is the entering child. Delivery follows that child's enter notification, hook, TreeEntered and tree NodeAdded.

<a id="e-electron2d-node-childexitingtree"></a>
### `public event Action<Node, Node>? ChildExitingTree`

Occurs on the direct parent while a child is exiting the active tree.

**Remarks:** The first argument is the publishing parent and the second is the exiting child. Descendants have exited. Exit notification, physical release and tree NodeRemoved have run, and the child's Tree is still set.

<a id="e-electron2d-node-childorderchanged"></a>
### `public event Action<Node>? ChildOrderChanged`

Occurs after the order or membership of direct children changes.

**Remarks:** The argument is this parent node. Delivery follows complete NotificationChildOrderChanged dispatch. Drawing-order publication, notification and signal are each attempted after earlier failures; errors aggregate after the committed mutation. Unchanged moves and absent removals are silent. Normal C# subscriber ordering applies.

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

Occurs after an active node accepts a name assignment and path notifications propagate.

**Remarks:** The argument is this node. Unchanged names also raise the event; detached assignments only propagate path notifications.

<a id="e-electron2d-node-treeentered"></a>
### `public event Action<Node>? TreeEntered`

Occurs when this node enters an active scene tree.

**Remarks:** Delivery follows the complete enter notification and OnEnterTree, then tree NodeAdded and parent ChildEnteredTree follow before descendant entry.

<a id="e-electron2d-node-treeexited"></a>
### `public event Action<Node>? TreeExited`

Occurs after this node has left its scene tree.

**Remarks:** The complete departing subtree has no Tree association. Ordinary removal clears the subtree root Parent and invalid owners first. A separate reverse child-first pass delivers this event; pending lifecycle barriers reject reentry, hierarchy escape and disposal through the transition.

<a id="e-electron2d-node-treeexiting"></a>
### `public event Action<Node>? TreeExiting`

Occurs while this node is exiting its active scene tree.

**Remarks:** Descendants and OnExitTree have completed. Tree remains available and physical bindings are live; exit notification and engine membership release follow this event.

## Constant Descriptions

<a id="f-electron2d-node-notificationdragbegin"></a><a id="f-electron2d-node-notificationdragend"></a>
### `NotificationDragBegin = 21` and `NotificationDragEnd = 22`

The root SceneTree propagates Begin after committing a typed GUI drag payload, and End after a drop or cancellation commits success/failure and destroys the preview. Begin observers can read the active payload through the root Viewport; End observers see no active payload and can query the retained success result. Callback failures are reported after the remaining eligible notifications and mandatory cleanup are attempted. Nested viewport and cross-window drag remain separate capabilities.

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

Identifies an input-method composition update supplied by the operating system. During Engine.Run, all live root-scene nodes receive it after DisplayServer commits `IMEGetText` and `IMEGetSelection` and before the focused Control's typed composition callback. The notification also runs without a GUI focus owner; callback failures do not suppress later eligible nodes or the focused text-control phase.

<a id="f-electron2d-node-notificationosmemorywarning"></a>
### `public const int NotificationOsMemoryWarning = 2009`

Identifies an operating-system low-memory warning propagated by the active scene tree.

<a id="f-electron2d-node-notificationparented"></a>
### `public const int NotificationParented = 18`

Identifies the notification sent after a parent reference is assigned.

<a id="f-electron2d-node-notificationpathrenamed"></a>
### `public const int NotificationPathRenamed = 23`

Identifies the notification propagated after an accepted name assignment, including an unchanged name.

<a id="f-electron2d-node-notificationpaused"></a>
### `public const int NotificationPaused = 14`

Identifies the notification sent when the owning tree becomes paused.

<a id="f-electron2d-node-notificationphysicsprocess"></a>
### `public const int NotificationPhysicsProcess = 16`

Identifies a physics-process callback notification.

<a id="f-electron2d-node-notificationpostentertree"></a>
### `public const int NotificationPostEnterTree = 27`

Identifies the child-first notification sent on every attachment after descendant ready delivery and before this node's optional ready notification.

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

ProcessMode transition snapshots now reuse preorder lists separately for each active callback reentrancy depth, preserving pre-mutation physics guards and failure continuation. Prepare each used subtree capacity/depth before a measured interval; [ScreenVisibilityTests](../../tests/Electron2D.Tests/ScreenVisibilityTests.cs) verifies 64 warmed nested changes with zero managed bytes, while [screen enablers](VisibleOnScreenEnabler.md) drive the same setter from actual render transitions. Physics resource attachment/removal retains its explicit lifecycle allocation boundary.

## Shortcut input stage

`public bool ShortcutInputEnabled { get; set; }` defaults to false. `protected virtual void OnShortcutInput(InputEvent @event)` receives borrowed key, gamepad-button and direct shortcut events after GUI delivery and before unhandled-key/general input. Ready enables an actual override before OnReady; later writes can disable it. Delivery follows reverse captured hierarchy order, owner-thread and CanProcess policy; handling stops later stages, while callback errors are collected after eligible delivery. All six public callback switches have stored typed descriptors; internal processing switches remain runtime-only. See [Shortcut](Shortcut.md), [ShortcutTests](../../tests/Electron2D.Tests/ShortcutTests.cs) and [ADR 0038](../decisions/input.md#adr-0038).

Node.EnsureMutable retains its disposal/capture/tree-thread guards and then dispatches the virtual ValidateMutation hook. This lets subclasses with retained native ownership reject mutations after tree exit; AudioLifetimeTests checks detached audio registration and rejection before state changes.

SubViewport children are now allowed; native Window children still reject. GetWindow finds the containing Window across offscreen viewport boundaries, while GetViewport retains the nearest viewport. Root/native scene input skips nested viewport descendants; explicit PushInput routes its own scene scope. See [offscreen targets](../components/canvas-rendering.md#offscreen-canvas-targets) for scope, tests and remaining prerequisites.

## Constant summary

| Complete C# signature | Contract |
| --- | --- |
| `public const System.Int32 NotificationVPMouseEnter = 1010` | Identifies pointer entry into an embedded viewport. |
| `public const System.Int32 NotificationVPMouseExit = 1011` | Identifies pointer exit from an embedded viewport. |

## Constant Descriptions

<a id="member-8df3365d1c51"></a>
### NotificationVPMouseEnter

`public const System.Int32 NotificationVPMouseEnter = 1010`

Identifies pointer entry into an embedded viewport.

<a id="member-25e717c6e08a"></a>
### NotificationVPMouseExit

`public const System.Int32 NotificationVPMouseExit = 1011`

Identifies pointer exit from an embedded viewport.

## Scene multiplayer integration

Nearest-branch assignment, automatic idle polling and typed RPC/authority configuration execute through [the component](../components/scene-multiplayer.md). RPC method tokens/codecs replace string/reflection invocation; configure derived nodes in their construction/factory for PackedScene reconstruction. Authority/configuration changes remain local and are not automatically replicated. API/transport ownership remains borrowed for caller-supplied interfaces.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.MultiplayerAPI Multiplayer { get;  }` | Gets the interface assigned to the nearest containing scene branch. |

## Property Descriptions

<a id="member-5bdf4007b028"></a>
### Multiplayer

`public Electron2D.MultiplayerAPI Multiplayer { get;  }`

Gets the interface assigned to the nearest containing scene branch.

Value: The default interface or most specific custom override; detached access fails.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Int32 GetMultiplayerAuthority()` | Gets the local node's configured multiplayer authority. |
| `public Electron2D.RPCRegistration[] GetNodeRPCConfig()` | Returns caller-owned typed RPC configuration snapshots. |
| `public System.Boolean IsMultiplayerAuthority()` | Reports whether this node's authority matches its assigned interface identity. |
| `public System.Void RPCConfig(Electron2D.RPCMethod method, System.Nullable<Electron2D.RPCOptions> options = null)` | Configures a typed callable and its invocation policy. |
| `public System.Void RPCID<TNode, T>(System.Int32 id, RPCMethod<TNode, T> method, T arguments)` | Projects inherited lifecycle behavior for this concrete type. |
| `public System.Void RPC<TNode, T>(RPCMethod<TNode, T> method, T arguments)` | Projects inherited lifecycle behavior for this concrete type. |
| `public System.Void SetMultiplayerAuthority(System.Int32 id, System.Boolean recursive = true)` | Sets a positive authority identity, optionally recursively for current descendants. |

## Method Descriptions

<a id="member-9a171ae4c306"></a>
### GetMultiplayerAuthority

`public System.Int32 GetMultiplayerAuthority()`

Gets the local node's configured multiplayer authority.

Returns: Server identity one initially.

<a id="member-6c34da278ad1"></a>
### GetNodeRPCConfig

`public Electron2D.RPCRegistration[] GetNodeRPCConfig()`

Returns caller-owned typed RPC configuration snapshots.

Returns: Configured immutable tokens and copied policies.

<a id="member-01e4daf317d2"></a>
### IsMultiplayerAuthority

`public System.Boolean IsMultiplayerAuthority()`

Reports whether this node's authority matches its assigned interface identity.

Returns: False for detached nodes.

<a id="member-3775a292b1ed"></a>
### RPCConfig

`public System.Void RPCConfig(Electron2D.RPCMethod method, System.Nullable<Electron2D.RPCOptions> options = null)`

Configures a typed callable and its invocation policy.

method: Immutable receiver/codec token, unique per node by ID.

options: Policy; null removes the configuration.

Remarks: Tokens/closures are borrowed configuration. Configure derived nodes in their typed scene factory/constructor for scene reconstruction.

<a id="member-f6a5c8552a83"></a>
### RPCID

`public System.Void RPCID<TNode, T>(System.Int32 id, RPCMethod<TNode, T> method, T arguments)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-e2a3f0bff655"></a>
### RPC

`public System.Void RPC<TNode, T>(RPCMethod<TNode, T> method, T arguments)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-06979d4dfd98"></a>
### SetMultiplayerAuthority

`public System.Void SetMultiplayerAuthority(System.Int32 id, System.Boolean recursive = true)`

Sets a positive authority identity, optionally recursively for current descendants.

id: Positive peer identity.

recursive: True applies to current descendants; later children retain their own default/configuration.

Remarks: This local configuration does not replicate itself; participants must agree separately.

SetMultiplayerAuthority is now virtual so MultiplayerSynchronizer stops/rebinds its concrete configuration around authority changes. Spawner/synchronizer lifecycle and [typed replication](../components/scene-replication.md) reuse the existing attached owner/capture guards; authority does not automatically replicate. In-memory source provenance stays internal.

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

## Low-level primitive integration

Structural child order changes now notify the active renderer to republish registered source draw-index overrides below this parent, before ordinary child-order notifications. Source Node order and existing event/capture/owner behavior remain authoritative. See [the executable primitive contract](../components/canvas-rendering.md#low-level-primitive-commands).


Externally associated physics bodies observe this node’s tree entry/exit while attached. Membership updates scene contact/overlap signals even while raw bodies remain in their space. Reverse observers are removed on detach/rebind and cleared at disposal; throwing handlers do not replay committed transitions. See [object associations](../components/physics-object-bindings.md).

During active-tree disposal, internal exit handlers may disable both internal
processing lanes without passing the ordinary disposed-object mutation guard.
The exception is limited to owner-thread tree exit and disabling; reenabling or
mutating a disposed node remains rejected. RayCast and ShapeCast disposal exercise it.

## Tree-order comparison cost

IsGreaterThan compares active ancestry without temporary list allocation. Internal
front/back children participate in the actual sibling order. Existing owner,
shared-tree and disposed-object checks remain enforced. Sorted physical pointer
selection uses this operation; PhysicsPickingTests covers internal siblings and
ancestor/descendant ordering during warmed allocation checks.

Scene hierarchy insertion/removal/reorder and node disposal reject while a physics extension hook borrows a world in this tree. This preflight runs before child-order or lifecycle mutation, preserving body attachment identity. Ordinary built-in post-solver callbacks retain their existing lifecycle behavior. [Body extension checks](../components/physics-body-extensions.md) exercise these guards.

Hierarchy and physics authoring scans skip only when the process has no scene/raw
collider attachment and no scene geometry preflight borrow. Tree extension guards
still execute first. Any attachment or preflight retains the full actual-space,
ancestor/descendant and external-object binding checks, including detached nodes.
This conservative check avoids repeated empty scans during bulk construction and
final disposal; it does not index individual subtrees. See the [lifecycle
measurement](../components/physics-scene-performance.md#empty-hierarchy-guard-scans).

Animation controllers use the same owner-thread and scene-capture mutation guards as other nodes. Built-in scene-state validation finishes after stored values/reference restoration and before resource setup or scene-instantiated notifications.

Internal engine mouse motion refreshes GUI and physics hover without invoking the user OnInput, OnShortcutInput, OnUnhandledKeyInput or OnUnhandledInput callbacks. Ordinary device events retain their existing propagation order.

## Independent internal processing

`public bool InternalProcessEnabled { get; set; }` and `public bool InternalPhysicsProcessEnabled { get; set; }` project each internal-lane setter/query pair. Plain Node defaults are false; built-in nodes may enable their own work. Each switch preserves the other lane and the ordinary ProcessEnabled/PhysicsProcessEnabled flags. Enabled internal notifications run before ordinary callbacks in their selected idle/fixed frame. An internal callback can enable or disable the ordinary callback for the same frame. Detach/dispose prevents further ordinary delivery; failures still attempt the remaining ordinary/sibling callbacks under the scene-tree cleanup policy.

The resolved ProcessMode and SceneTree pause policy gate both lanes. Internal processing is independent of ordinary callback enablement, and does not bypass pausing. Detached nodes retain the runtime switches but receive no frames. Setters allow the scene owner or current processing group and reject scene capture; coordinated stored-value reads and disposed-access rejection apply to all eight switches and both priorities. Typed property descriptors expose writable runtime state with IsStored false. Ordinary PackedScene capture omits the flags, while constructors rebuild built-in internal behavior. No hidden thread or scheduler is added.

Changing a built-in node's internal switches directly affects its implementation: for example it suspends and resumes Timer countdowns. Such nodes can overwrite the switch when their playback/callback policy changes. Do not disable internal work unless that is the intended effect. See [Node](Node.md) and [Timer](Timer.md).

NodeInternalProcessingTests verifies both lanes, order and current-state mutation, pause modes, reattachment, errors, ownership/disposal, runtime scene omission and actual idle/physics Timer consumers. Its 1024 warmed set/query/frame operations allocate zero managed owner bytes. Native GPU and compatibility checks verify red pixels, a frozen frame with the internal lane disabled, resumed blue pixels and fixed ticks. Foreign/editor/native allocator and broad load verification remain separate.

## Window notification constants

All constants below are `public const int` fields inherited by physical scene nodes.
The containing [Window](Window.md#containing-window-notifications) owns automatic
delivery; manual DispatchNotification does not create a native event.

| Declaration | Value | Delivery |
| --- | --- | --- |
| [`public const int NotificationWmMouseEnter`](#notificationwmmouseenter) | 1002 | Containing-window subtree, stopping before nested windows. |
| [`public const int NotificationWmMouseExit`](#notificationwmmouseexit) | 1003 | Containing-window subtree, stopping before nested windows. |
| [`public const int NotificationWmWindowFocusIn`](#notificationwmwindowfocusin) | 1004 | Containing-window subtree, stopping before nested windows. |
| [`public const int NotificationWmWindowFocusOut`](#notificationwmwindowfocusout) | 1005 | Containing-window subtree, stopping before nested windows. |
| [`public const int NotificationWmCloseRequest`](#notificationwmcloserequest) | 1006 | Containing-window subtree, stopping before nested windows. |
| [`public const int NotificationWmSizeChanged`](#notificationwmsizechanged) | 1008 | Owning Window only. |
| [`public const int NotificationWmDpiChange`](#notificationwmdpichange) | 1009 | Containing-window subtree, stopping before nested windows. |
| [`public const int NotificationWmPositionChanged`](#notificationwmpositionchanged) | 1012 | Containing-window subtree, stopping before nested windows. |

### NotificationWmMouseEnter

`public const int NotificationWmMouseEnter = 1002`. The pointer enters the containing window. Repeated entry is silent.

### NotificationWmMouseExit

`public const int NotificationWmMouseExit = 1003`. The pointer leaves the containing window. Content MouseExited and hover/picking cleanup precede delivery; repeated exit is silent.

### NotificationWmWindowFocusIn

`public const int NotificationWmWindowFocusIn = 1004`. The containing window gains keyboard focus, before FocusEntered.

### NotificationWmWindowFocusOut

`public const int NotificationWmWindowFocusOut = 1005`. The containing window loses keyboard focus, before FocusExited.

### NotificationWmCloseRequest

`public const int NotificationWmCloseRequest = 1006`. Window closure is requested, before CloseRequested and the root quit decision. Embedded requests do not quit the host automatically.

### NotificationWmSizeChanged

`public const int NotificationWmSizeChanged = 1008`. The owning Window updates its size/content policy, after SizeChanged. This identity is not propagated to descendants.

### NotificationWmDpiChange

`public const int NotificationWmDpiChange = 1009`. SDL reports a containing-window display content-scale change, before DpiChanged. Physical DPI measurement is not implied.

### NotificationWmPositionChanged

`public const int NotificationWmPositionChanged = 1012`. An observed native rectangle changes the containing window client position. Placement requests do not emit; Wayland does not expose global positioning.

Non-size automatic delivery is owner-thread and parent-first; nested windows own
their independent event scopes. Callback errors preserve remaining receivers and
typed delegates, then aggregate. The same hierarchy barrier protects child lists
as PropagateNotification. See WindowNotificationTests for embedded, dummy native,
physical receivers, failure/teardown and 1024 zero-allocation operations.

<a id="p-electron2d-node-shortcutinputenabled"></a>
### `public bool ShortcutInputEnabled { get; set; }`

Gets or sets whether this node receives keyboard, gamepad-button and shortcut events after GUI handling.

**Value:** False initially; ready notification enables an overridden callback before OnReady.

**Remarks:** The stage precedes unhandled-key and general unhandled input and obeys the node's process policy. Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot. Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation. Packed scenes store this configuration before ready applies automatic callback preparation.

**System.InvalidOperationException:** Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.

**System.ObjectDisposedException:** The node has finished disposing.

<a id="m-electron2d-node-onshortcutinput-electron2d-inputevent"></a>
### `protected virtual void OnShortcutInput(InputEvent event)`

Receives the live borrowed event when the shortcut stage remains unhandled. The
caller owns its lifetime. Call Tree.SetInputAsHandled to suppress later stages.
Ordinary typed virtual dispatch runs on the scene owner; exceptions join the
existing continued-delivery aggregate. Overriding enables the lane at ready.

## Captured processing membership

Each idle/physics schedule captures only nodes whose public or internal callback is enabled for that lane. Enabling a previously absent participant during traversal starts it on the next frame. Disabling a captured participant suppresses its remaining delivery immediately; current process-mode/pause eligibility is still rechecked. An already captured internal participant may enable its own public callback before that callback in the same frame. Priority or hierarchy-order writes apply to the next captured schedule. A node that exits and reenters, even into the same tree, cannot revive an old entry or continue the old attachment after its internal callback. Both schedules retain capacity and rebuild after membership changes.

[Public scheduling consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.NodeScheduling.cs) exercise neutral, spatial and physical descendants, all four processing flags, priority changes, removal/reentry, error continuation and real selected solver frames/queries. Exact evidence and limits are in `bin/physics-node-scheduling/2026-10-15/validation.json`. Owned process-thread groups and cross-group deferred delivery remain their separate open coverage obligations.

## Typed thread-safe and processing-group messages

| Method | Contract |
| --- | --- |
| `CallThreadSafe<TNode>(Action<TNode> operation)` | Executes on an accessible owner, otherwise queues the operation. |
| `SetThreadSafe<TNode, TValue>(Action<TNode, TValue> setter, TValue value)` | Same policy with a captured typed value and ordinary setter validation. |
| `NotifyThreadSafe(int what)` | Same policy for ordinary numeric virtual notification dispatch. |
| `CallDeferredThreadGroup<TNode>(Action<TNode> operation)` | Always queues to the attached node's processing owner. |
| `SetDeferredThreadGroup<TNode, TValue>(Action<TNode, TValue> setter, TValue value)` | Same deferred policy for a typed property setter. |
| `NotifyDeferredThreadGroup(int what)` | Same deferred policy for a numeric notification. |

Every typed receiver parameter must accept this node (`TNode : Node`), and null delegates reject before queueing. These calls return no operation result. The current implicit main group drains FIFO before and after callbacks in both frame lanes, including disabled and paused nodes. Messages queued during a drain join that drain; failures are aggregated while later messages and frame work continue. A pre-group operation can enable processing before membership capture, and post-group changes precede physics integration. Ordinary `SceneTree.Defer`/`FlushDeferred` retain their separate captured-batch behavior.

The receiver is an immutable weak identity; disposal/collection skips delivery without retaining or retargeting it. A live receiver moved to another tree owner is requeued there. Detached thread-safe calls execute under ordinary detached-node access and caller coordination; deferred group calls require membership. Finalization closes queue acceptance and discards retained values/delegates without delivery. Prepared generic slots avoid repeated capture allocation; first type/capacity growth and caller callback allocations remain separate. None of these operations grants direct off-owner access to physical or rendering state.

[Public message consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ThreadMessages.cs) exercise all six members, live physical motion/queries, worker submissions, phase/order, recursive/error delivery, weak receiver retirement, transfer and closure. Current evidence: `bin/physics-thread-messages/2026-10-15/validation.json`. The current explicit-group profile below implements selection/order/masks. These methods retain Partial coverage for general in-group inherited scene/backend access.

## Explicit processing groups

`ProcessThreadGroup` selects `Node.ProcessThreadGroupMode.Inherit` (0, default), `MainThread` (1) or `SubThread` (2). The nearest explicit ancestor owns inherited descendants; nested owners create independent boundaries. `ProcessThreadGroupOrder` is a stored signed integer, zero initially. Lower order completes first; equal-order worker stages complete before main groups. Worker groups use retained WorkerThreadPool jobs and the frame joins all runners before the next stage or physical integration. Callback priority and equal-priority tree order remain inside each group.

`ProcessThreadMessages` uses `Node.ProcessThreadMessageFlags.Process` (1), `Physics` (2) and `All` (3); zero initially permits no message-only lane. A group with callbacks already drains messages in that lane, even when its mask is zero. The implicit main group always services both lanes. A group with neither callbacks nor permitted queued work does not execute. The scene owner or current group may change the order and message mask on its own members, including physical descendants. Order applies to the next captured frame; masks affect later eligibility and never interrupt a current drain. Role changes/rebinding and hierarchy remain owner operations; replacement/exit discards a retired group's pending queue. New groups wait for a later captured frame.

Current-group worker callbacks may observe their deltas and change their own processing flags/priorities/order/message mask under the ordinary validation hook. Coordinated stored-value reads remain available. Nested pool jobs helped synchronously on that worker retain the active scene group and immediate thread-safe delivery; deferred group messages still queue. A later ordinary pool job has no scene-group permission. Writes to another group's processing metadata and reads of its frame delta are rejected. Typed calls are immediate for the scene owner or current target group; other callers queue to the destination group. General Node/CanvasItem/Entity access, rendering and physical backend mutation retain their current owner guards and remaining Partial coverage. A main collector after the worker barrier can apply computed results to actual physics.

[Public group consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ProcessGroups.cs) prove parallelism with two independent workers, inherited member order, main collection at equal order, current-group access, masks, error draining, reconfiguration, queue retirement, packed/fresh-process settings and full selected physical frames/queries with zero owner/all-thread allocations. Evidence: `bin/physics-process-groups/2026-10-15/validation.json`.


Runtime messages now use [diagnostic delivery](../components/diagnostics.md). Engine output flags affect its physical stream; typed logger callbacks retain delivery. Host Console writers retain ordinary C# semantics.

[Node group membership](../components/node-groups.md) preserves first-add persistence, literal identifiers,
current processing-group access and exact physical/file consumers. Physical
mutations retain their owner guard before virtual consumer hooks. Runtime
Node duplication retains its separate ADR 0091 binding/identity prerequisite.

The [entry/readiness/departure contract](../components/scene-hierarchy.md#entry-readiness-and-departure) specifies parent/tree signal order, entry-added and ready-added children, consumed readiness, stale attachment snapshots, failure cleanup, physical release and processing-group ready metadata. The public-only lifecycle consumer retains a real selected solver and fresh-process file activation.

The [membership and authoring-owner contract](../components/scene-hierarchy.md#membership-and-authoring-ownership) separates structural parents, active tree membership, borrowed owner registrations and physical worlds. Detached ownership and owner expiry are exercised through public CPU/software-GPU/registered managed and NativeAOT consumers.

<a id="p-electron2d-node-internalprocessenabled"></a>
### `public bool InternalProcessEnabled { get; set; }`

Gets or sets whether this node receives internal idle-frame notifications.

**Value:** False for a plain Node. Built-in subclasses may enable their own internal work.

**Remarks:** This lane is independent of ProcessEnabled and runs before ordinary processing. The resolved ProcessMode and tree pause state still gate both lanes. Changes are runtime state, not stored scene data. Disabling a built-in node's internal work can suspend animation, timers or other implementation logic. Participation is captured together with ordinary idle processing. A newly enabled node waits for the next idle snapshot; a node already included through its ordinary callback can enable internal delivery in this frame. Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot. Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.

**System.InvalidOperationException:** Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.

**System.ObjectDisposedException:** The node is disposed.

<a id="p-electron2d-node-internalphysicsprocessenabled"></a>
### `public bool InternalPhysicsProcessEnabled { get; set; }`

Gets or sets whether this node receives internal fixed-frame notifications.

**Value:** False for a plain Node. Built-in subclasses may enable their own internal work.

**Remarks:** This lane is independent of PhysicsProcessEnabled and runs before ordinary physics processing. The resolved ProcessMode and tree pause state still apply. This runtime switch is not stored in scenes. Disabling built-in internal physics can suspend casts, timers or other implementation logic. Participation is captured together with ordinary physics processing. A newly enabled node waits for the next physics snapshot; a node already included through its ordinary callback can enable internal delivery in this frame. Reads require a live receiver and caller coordination with metadata changes; they are not an atomic snapshot. Writes require the scene owner or current processing group, including on physical nodes, and reject scene capture even for unchanged values. These local switches do not authorize physical, rendering or hierarchy mutation.

**System.InvalidOperationException:** Mutation is outside the scene owner/current processing group, scene capture is active, or a derived validation hook rejects it.

**System.ObjectDisposedException:** The node is disposed.

The complete [processing metadata and callback contract](../components/scene-tree.md#complete-processing-metadata-and-callbacks) covers coordinated reads, current-group configuration, all six stored callback switches, signed priorities, input delivery and allocation-free worker schedule rebuilding. Physical, hierarchy and renderer mutation retain their own guards.

Scene-authored global shader values now use [ShaderGlobalsOverride](ShaderGlobalsOverride.md). Its typed portable values, tree-order arbitration and renderer-session restoration are described in the [global parameter component](../components/global-shader-parameters.md#scene-owned-overrides).

## Authoring member descriptions

<a id="p-electron2d-node-editordescription"></a>
### `public string EditorDescription { get; set; }`

Initially empty. Null throws ArgumentNullException. Only different ordinal values emit; state commits before synchronous callback errors. Stored with scenes.

<a id="p-electron2d-node-displayfolded"></a>
### `public bool DisplayFolded { get; set; }`

Initially false; transient and silent. Runtime visibility, process eligibility and physics are unchanged.

<a id="p-electron2d-node-ispartofeditedscene"></a>
### `public bool IsPartOfEditedScene { get; }`

True for the live selected root and descendants. False when detached or unselected. Reads use owner/current-group access.

<a id="m-electron2d-node-seteditableinstance-electron2d-node-system-boolean"></a>
### `public void SetEditableInstance(Node node, bool isEditable)`

Null throws ArgumentNullException; self or unrelated targets throw ArgumentException. Both nodes must permit authoring before fields change. The target flag follows reparenting; false unfolds this receiver. Equal writes still request the target state event.

<a id="m-electron2d-node-iseditableinstance-electron2d-node"></a>
### `public bool IsEditableInstance(Node? node)`

Null returns false. Any live strict ancestor reads the same target flag; invalid nonnull relationships throw ArgumentException.

<a id="e-electron2d-node-editordescriptionchanged"></a>
### `public event Action<Node>? EditorDescriptionChanged`

Emits this node after a changed description, independently of selection. Signal blocking applies.

<a id="e-electron2d-node-editorstatechanged"></a>
### `public event Action<Node>? EditorStateChanged`

Emits this selected node after the supported authoring mutations listed in Scene authoring state. Delivery uses the mutating thread and committed state; signal blocking applies.

Successful EnsureMutable validation conservatively reports a rendering change, including equal assignments and detached authoring. Custom setters should retain the existing owner/capture guards. Repeated internal no-op setters can therefore prevent idle rendering; actual producers should avoid unnecessary publication. See [low-processor scheduling](../components/low-processor-rendering.md).

Inherited object translation-domain access and plural lookup execute through [scored runtime lookup](../components/localization.md#scored-runtime-lookup). Catalog selection preserves live edits, callback errors and physical identities with strict zero warmed managed bytes. Singular translation now uses verified [Unicode pseudolocalization](../components/localization.md#unicode-pseudolocalization); the plural path intentionally bypasses those transforms.
