# ElectronObject

Registered extension instances receive `NotificationExtensionReloaded` (2) only after every surviving binding and compatible stored value has been restored. Ordinary CLR updates do not produce this event. Their deterministic disposal follows manager-thread admission, pre-delete veto, binding retirement and owner cleanup; errors preserve committed cleanup. See [module lifetime](../components/extensions.md).

Last updated: 2026-10-28

**Inherits:** —

**Inherited By:** [IP](IP.md), [ThemeDB](ThemeDB.md), [ConfigFile](ConfigFile.md), [DirAccess](DirAccess.md), [DisplayServer](DisplayServer.md), [Engine](Engine.md), [FileAccess](FileAccess.md), [Input](Input.md), [InputMap](InputMap.md), [MainLoop](MainLoop.md), [Node](Node.md), [ProjectSettingsRegistry](ProjectSettingsRegistry.md), [Resource](Resource.md), [SceneState](SceneState.md), [SceneTreeTimer](SceneTreeTimer.md), [Tween](Tween.md), [Tweener](Tweener.md)

- **Source:** [`src/Core/Object/ElectronObject.cs`](../../src/Core/Object/ElectronObject.cs); [notification direction](../../src/Core/Object/ElectronObject.Notifications.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public partial class ElectronObject`

> Provides the common identity, notification, property, localization, and lifetime contract for engine objects.

Plain project subclasses can use [Script.RegisterObject](Script.md#plain-object-sources) for checked compiled source metadata and typed construction. The owner keeps its ordinary fields, notifications and weak physical identity; this adds no live script reassignment.

## Custom typed iteration

Compiled subclasses can supply ordinary C# `GetEnumerator()`/`MoveNext()`/`Current` and optionally `IEnumerable<T>`, retaining their existing object, Resource or Node/body inheritance. The cursor owns its traversal state; acquire a fresh cursor for each independent or nested loop. `foreach` handles ordinary disposal on completion, early exit and exceptions. Use a struct-pattern cursor for allocation-sensitive loops; interface conversion can box that cursor. This is the [typed iteration projection](../decisions/core-object-runtime.md#adr-0005), with no universal element type or dynamic dispatcher on ElectronObject.

The subtype owns element lifetime, mutation/version checks and thread policy. It can apply `ThrowIfDisposed` and existing guarded scene/physics operations, including same-thread teardown inspection. Enumeration does not relax solver or owner-thread restrictions. Interface/iterator-block allocation is distinct from the required zero-allocation warmed physics path.

## Compiled indexed properties

`ElectronObject.GetIndexed` and `SetIndexed` accept a typed `PropertyDescriptor` for the root owner and result value. The supplied descriptor may describe a direct property or a composition built with `PropertyDescriptor.Through`, `Member`, `Element` or `Key`. Names are metadata labels; there is no runtime string setter, reflection scanner, new path wrapper or universal value container. This follows [ADR 0005](../decisions/core-object-runtime.md#adr-0005).

Through resolves the current referenced ElectronObject for each operation. Its setter invokes the child property then the parent property; validation, notifications and committed changes remain visible when an outer setter fails. Both levels must be writable. Member combines supplied typed struct readers/updaters and writes the complete updated value through the parent. Element checks a fixed nonnegative array index on each access; Key uses a typed dictionary key, permits additions on write and retains the dictionary comparer. Collection writes copy the original container before calling the parent setter, so a precommit validation failure cannot modify the original container. Reads and object values remain borrowed. Wrong owners, null/disposed links, missing keys, bounds and callback failures are explicit. Attached Node access follows owner affinity; writes retain scene capture/mutation guards.

Synthetic compositions are non-stored and provide no invented revert default. Save the original owning properties and resource graph, and use their ordinary revert operations. Their compiled accessors also drive the existing typed TweenProperty API. Descriptor construction and collection copies are cold operations; prepared scalar/object reads and writes are checked separately for zero managed allocation.

```csharp
var position = new PropertyDescriptor<Sprite, Vector2>(
    nameof(Sprite.Position), sprite => sprite.Position,
    (sprite, value) => sprite.Position = value);
var horizontal = PropertyDescriptor.Member(position, "X",
    point => point.X, (point, x) => point with { X = x });
sprite.SetIndexed(horizontal, 32f);
float x = sprite.GetIndexed(horizontal);
```

IndexedPropertyTests verifies object rebinding, components, copied arrays/maps/comparers, validation/read-only/lifetime/errors, owner/capture guards, saved scenes and existing Tween integration. Linux Wayland GPU/hardware compatibility hosts check actual atlas-region and Sprite-position pixels. Other platforms, full editor automation, native allocations and human acceptance remain unverified.

## Description

Provides the common identity, notification, property, localization, and lifetime contract for engine objects.

`ElectronObject` is the common base for engine-owned objects. It provides process-local identity, runtime diagnostics, numeric notification dispatch, deterministic cleanup, disposed-state protection, typed property exposure, per-object translation settings, queued-deletion observation, and lifecycle events. It does not emulate a universal-value dynamic API.

The contract uses typed properties and events and deliberately omits dynamic calls, untyped metadata, generic signal
registration, and live script reassignment. Instances have deterministic `IDisposable` lifetime.

The concrete [Script](Script.md) asset/catalog constructs ordinary CLR Node/Resource instances and retains their selected source through GetScript. [Compiled C# initialization](../components/scripting.md#compiled-c-initialization) uses the project constructor/base chain with typed arguments and stable identity. [Source-publication notifications](../components/scripting.md#source-publication-notifications) report actual file/remapping changes. Live assignment and compiled implementation replacement remain open under [ADR 0091](../decisions/scripting.md#adr-0091); registered extension replacement follows its separate module contract above.

There is no engine-owned method/event name registry, connection-flags enum, or dictionary of incoming/outgoing callable connections. Consumer tooling can inspect ordinary CLR type metadata. Typed event accessors, owned `EventConnection` tokens and registered persistent scene bindings retain their separate contracts under [ADRs 0002/0010](../decisions/core-object-runtime.md#adr-0010).


Typed property inspection uses the exact teardown-thread exception from ADR 0009: GetValue, TryGetRevertValue and CanRevert remain usable in pre-delete/cleanup on that thread; foreign-thread and terminal access fail. SetValue and Revert reject once disposal starts. TryGetRevertValue reports factory presence even for a null or unchanged default, while CanRevert and PropertyCanRevert retain the current value-difference policy. Factories run on each request; their results follow delegate ownership with no implicit copy. Validators reject before the setter, and an exception after a setter commits does not undo that write. Ordinary owner setters and getters retain thread, capture and numerical-phase rules.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var node = new Node();
ElectronObject value = node;
Console.WriteLine(value.InstanceID);
```

## Direct construction

`new ElectronObject()` creates a usable root object with independent identity, typed base properties, event/lifetime policy and no Script association. It can be a raw body/Area query target through `PhysicsServer.BodyAttachObject` and `AreaAttachObject`; direct results retain the sampled ID while resolving a live weak object. Disposal or collection invalidates that borrowed target without freeing its independent physics RID. Subclasses retain ordinary base construction. See [the construction contract](../components/object-lifecycle.md#direct-root-construction).

## Constructors

| Member | Description |
| --- | --- |
| [`public ElectronObject()`](#m-electron2d-electronobject-ctor) | Initializes an independent process identity, shared weak diagnostic registration and invocation-local factory provenance. |

## Properties

| Member | Description |
| --- | --- |
| [`protected bool IsNotificationReversed`](#p-electron2d-electronobject-isnotificationreversed) | Reads direction for this object's current invocation on this thread. |
| [`public ulong InstanceID { get; }`](#p-electron2d-electronobject-instanceid) | Gets this object's process-local instance identifier. |
| [`public string ClassName { get; }`](#p-electron2d-electronobject-classname) | Gets the unqualified runtime class name. |
| [`public bool IsDisposed { get; }`](#p-electron2d-electronobject-isdisposed) | Gets whether deterministic disposal has started. |
| [`public bool IsQueuedForDeletion { get; }`](#p-electron2d-electronobject-isqueuedfordeletion) | Atomically observes pending or executing scene-tree deletion. |
| [`public bool CanTranslateMessages { get; set; }`](#p-electron2d-electronobject-cantranslatemessages) | Gets or sets whether this object resolves messages through [`TranslationServer`](TranslationServer.md). |
| [`public virtual string TranslationDomain { get; set; }`](#p-electron2d-electronobject-translationdomain) | Gets or sets the translation domain used by this object; Node overrides it for inheritance. |

## Ordinary event delivery

`SetBlockSignals(true)` suppresses subsequent ordinary event emissions from this object. The initial policy is false. Existing subscriptions stay attached, one-shot tokens stay unconsumed and blocked emissions create no deferred work. Unblocking does not replay dropped emissions. Already accepted deferred callbacks still execute; disposing their token remains the cancellation mechanism. A handler changing the flag affects the next emission, while the selected current snapshot retains its order and exception behavior.

Numeric notifications, virtual processing/input/integration callbacks, actual state changes and resource revisions continue. `Disposed` is an independent deterministic lifecycle event and always performs endpoint cleanup. Internal bookkeeping channels and caller-supplied operation callbacks are independent of ordinary public events. Static service facades use their retained publisher's policy, accessible through the existing `Engine.GetSingleton` identity API.

Custom typed publishers use the protected gateway once at publication. It preserves any concrete delegate signature, including borrowed spans, and allocates nothing. Publishers still validate lifetime/thread access. Ordinary CLR delegate invocation cannot intercept this object policy automatically.

```csharp
sealed class Meter : ElectronObject
{
    public event Action<Meter, int>? Changed;
    public void Update(int value)
    {
        ThrowIfDisposed();
        BeginEventEmission(Changed)?.Invoke(this, value);
    }
}
```

## Methods

| Member | Description |
| --- | --- |
| [`public void CancelDisposal()`](#m-electron2d-electronobject-canceldisposal) | Vetoes the current automatic pre-delete disposal attempt on its exact thread. |
| [`public void SetBlockSignals(bool enable)`](#m-electron2d-electronobject-setblocksignals-system-boolean) | Changes the per-object ordinary emission policy. |
| [`public bool IsBlockingSignals()`](#m-electron2d-electronobject-isblockingsignals) | Reads the atomic policy, initially false. |
| [`protected TDelegate? BeginEventEmission<TDelegate>(TDelegate? handlers) where TDelegate : Delegate`](#m-electron2d-electronobject-begineventemission) | Selects the typed snapshot once for a custom publisher. |
| [`public bool IsClass(string className)`](#m-electron2d-electronobject-isclass-system-string) | Tests the actual compiled class and engine base-class names. |
| [`public void Notify(int what)`](#m-electron2d-electronobject-notify-system-int32) | Synchronously delivers a numeric engine notification to this object. |
| [`public void Notify(int what, bool reversed)`](#m-electron2d-electronobject-notify-system-int32-system-boolean) | Selects ordinary or derived-first numeric delivery. |
| [`public Script? GetScript()`](#m-electron2d-electronobject-getscript) | Gets the borrowed compiled source association. |
| [`public IReadOnlyList<PropertyDescriptor> GetPropertyList()`](#m-electron2d-electronobject-getpropertylist) | Builds the current validated list of typed properties exposed to tooling. |
| [`public bool PropertyCanRevert(PropertyDescriptor property)`](#m-electron2d-electronobject-propertycanrevert-electron2d-propertydescriptor) | Reports whether a tooling property currently differs from its revert value. |
| [`public void RevertProperty(PropertyDescriptor property)`](#m-electron2d-electronobject-revertproperty-electron2d-propertydescriptor) | Restores a tooling property to its current typed revert value. |
| [`public string Tr(string message, string context = null)`](#m-electron2d-electronobject-tr-system-string-system-string) | Translates a singular message using this object's translation domain. |
| [`public string TrN(string singular, string plural, long count, string context = null)`](#m-electron2d-electronobject-trn-system-string-system-string-system-int64-system-string) | Translates a plural message using this object's translation domain. |
| [`public void Dispose()`](#m-electron2d-electronobject-dispose) | Deterministically releases resources owned by this object. |
| [`protected virtual void OnNotification(int what)`](#m-electron2d-electronobject-onnotification-system-int32) | Handles an engine notification delivered to this object. |
| [`protected virtual void ValidateMutation()`](#m-electron2d-electronobject-validatemutation) | Validates that mutable base state may change at the current lifecycle point. |
| [`protected virtual IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#m-electron2d-electronobject-getpropertydescriptors) | Returns the typed properties exposed to tooling before validation. |
| [`protected virtual PropertyDescriptor ValidateProperty(PropertyDescriptor property)`](#m-electron2d-electronobject-validateproperty-electron2d-propertydescriptor) | Validates or customizes one property before it is exposed to tooling. |
| [`protected virtual bool CanRevertProperty(PropertyDescriptor property)`](#m-electron2d-electronobject-canrevertproperty-electron2d-propertydescriptor) | Determines whether a property has a distinct revert value. |
| [`public void NotifyPropertyListChanged()`](#m-electron2d-electronobject-notifypropertylistchanged) | Synchronously raises [`ElectronObject.PropertyListChanged`](ElectronObject.md#e-electron2d-electronobject-propertylistchanged). |
| [`protected void NotifyScriptChanged()`](#m-electron2d-electronobject-notifyscriptchanged) | Synchronously raises [`ElectronObject.ScriptChanged`](ElectronObject.md#e-electron2d-electronobject-scriptchanged). |
| [`protected virtual void Dispose(bool disposing)`](#m-electron2d-electronobject-dispose-system-boolean) | Releases resources owned by a derived class. |
| [`protected virtual void ValidateDisposal()`](#m-electron2d-electronobject-validatedisposal) | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |
| [`protected void ThrowIfDisposed()`](#m-electron2d-electronobject-throwifdisposed) | Rejects access after disposal starts, except on the thread currently running disposal callbacks. |
| [`public override string ToString()`](#m-electron2d-electronobject-tostring) | Returns a diagnostic string containing the runtime class name and instance identifier. |

## Events

| Member | Description |
| --- | --- |
| [`public event Action<ElectronObject> Disposed`](#e-electron2d-electronobject-disposed) | Occurs once after the object's owned resources have been released successfully. |
| [`public event Action<ElectronObject> PropertyListChanged`](#e-electron2d-electronobject-propertylistchanged) | Occurs when [`ElectronObject.NotifyPropertyListChanged`](ElectronObject.md#m-electron2d-electronobject-notifypropertylistchanged) reports that the tooling property list changed. |
| [`public event Action<ElectronObject> ScriptChanged`](#e-electron2d-electronobject-scriptchanged) | Occurs when source publication or a derived scripting component reports a changed script association. |

## Constants

| Member | Description |
| --- | --- |
| [`public const int NotificationPostInitialize = 0`](#f-electron2d-electronobject-notificationpostinitialize) | Identifies the internal initialization boundary before project callbacks are attached. |
| [`public const int NotificationPreDelete = 1`](#f-electron2d-electronobject-notificationpredelete) | Identifies the notification delivered immediately before owned resources are released. |

## Constructor Descriptions

<a id="m-electron2d-electronobject-ctor"></a>
### `public ElectronObject()`

Initializes an independent process identity, shared weak diagnostic registration and invocation-local factory provenance.

## Property Descriptions

<a id="p-electron2d-electronobject-instanceid"></a>
### `public ulong InstanceID { get; }`

Gets this object's process-local instance identifier.

**Value:** A nonzero identifier that is never changed or reused during the current process.

<a id="p-electron2d-electronobject-classname"></a>
### `public string ClassName { get; }`

Gets the unqualified runtime class name.

**Value:** The `Name` of the `Type` returned by `Object.GetType`.

<a id="p-electron2d-electronobject-isdisposed"></a>
### `public bool IsDisposed { get; }`

Gets whether deterministic disposal has started.

**Value:** True during each active attempt and after terminal cleanup. An automatic pre-delete veto restores false only after notification returns.

<a id="p-electron2d-electronobject-isqueuedfordeletion"></a>
### `public bool IsQueuedForDeletion { get; }`

Returns true for this object's accepted `SceneTree.QueueDelete` requests or a node's current `QueueFree` request, including publication and claimed execution. Resources, nodes and other object subclasses inherit the same observation. A queued parent does not mark its children.

Explicit requests are independent: `Node.CancelFree` releases only the cancellable node request, and discarding one tree releases only that tree's requests. A request rejected during execution is consumed. The flag stays true through pre-delete and cleanup, becomes false after terminal disposal, and stays false for direct disposal without a request. Readonly inspection remains available during and after disposal from any thread without backend access. The typed descriptor is readonly, nonstored and has no revert value; ordinary descriptor lifetime guards still apply.

<a id="p-electron2d-electronobject-cantranslatemessages"></a>
### `public bool CanTranslateMessages { get; set; }`

Gets or sets whether this object resolves messages through [`TranslationServer`](TranslationServer.md).

**Value:** `true` by default; `false` to return source messages unchanged.

**Exceptions**

- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.

<a id="p-electron2d-electronobject-translationdomain"></a>
### `public virtual string TranslationDomain { get; set; }`

Gets or sets the translation domain used by this object.

`Node` overrides this property to inherit a parent's domain until explicitly assigned.

**Value:** The case-sensitive domain passed to [`TranslationServer`](TranslationServer.md). The default is an empty string.

**Exceptions**

- `ArgumentNullException`: The assigned value is `null`.
- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.

[Compiled C# initialization](../components/scripting.md#compiled-c-initialization) executes the project constructor/base chain with the stable identity and source association. No separate virtual initializer runs from ElectronObject construction.

## Method Descriptions

<a id="m-electron2d-electronobject-getscript"></a>
### `public Script? GetScript()`

Returns the exact borrowed source asset selected for this owner's compiled construction, or null when none was selected. The association is available during Script.New's derived constructor and survives source edits, memory copies and version-nine scene/resource reconstruction. PackedScene uses the ordinary shared or scene-local resource policy. Earlier archives retain their unrecorded null association.

Reads require no metadata discovery, native device, callbacks or managed allocation. Cancelled disposal preserves the source; committed disposal releases it. Explicitly disposing the borrowed asset does not stop compiled execution and the returned asset may be disposed. The owner never owns that asset. Foreign-thread disposal and completed disposal reject with ObjectDisposedException.


<a id="m-electron2d-electronobject-canceldisposal"></a>
### `public void CancelDisposal()`

Vetoes only the current automatic pre-delete attempt on its exact disposing thread. Call it from `OnNotification(NotificationPreDelete)`. Repeated calls are harmless. Manual notifications without an active attempt, foreign threads, ordinary live calls and calls after cleanup starts have no effect; they cannot arm a future veto or resurrect a terminal object.

During notification, IsDisposed remains true and foreign guarded inspection/weak resolution remain closed. When a vetoed notification returns, the same object becomes live again, retaining InstanceID, subscriptions and owned resources; derived cleanup, Disposed delivery, subscriber clearing and finalization suppression are skipped. A notification exception propagates after restoring live state. User callback side effects are not rolled back. Concurrent or reentrant Dispose returns during that attempt; a later caller may retry. Node.CancelFree retains its separate queued-request cancellation meaning.

```csharp
class ProtectedNode : Node
{
    public bool KeepAlive { get; set; }
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationPreDelete && KeepAlive) CancelDisposal();
    }
}
```


<a id="m-electron2d-electronobject-setblocksignals-system-boolean"></a>
### `public void SetBlockSignals(bool enable)`

True suppresses new ordinary emissions; false enables them. `ValidateMutation` runs before every assignment, including an unchanged value. A rejected write retains the old policy. The flag is atomic base metadata; it neither observes solver state nor introduces additional scene-owner affinity beyond the existing derived validator. Disposing on another thread or terminal disposal throws `ObjectDisposedException`; a derived validator can reject with `InvalidOperationException`. The exact teardown thread retains the base lifecycle access rules.

<a id="m-electron2d-electronobject-isblockingsignals"></a>
### `public bool IsBlockingSignals()`

Returns false initially, then the latest atomic delivery policy. It validates base lifetime without reading scene/backend state. Disposing on another thread or completed disposal throws `ObjectDisposedException`. Each object has its own policy; there is no parent/child propagation or subscription removal.

<a id="m-electron2d-electronobject-begineventemission"></a>
### `protected TDelegate? BeginEventEmission<TDelegate>(TDelegate? handlers) where TDelegate : Delegate`

`handlers` is the concrete captured event delegate or null. Returns that same delegate when enabled, otherwise null. Immediately invoke the returned snapshot with the existing typed arguments. This does not validate publisher lifetime/affinity, allocate, change exception policy or recheck between listeners. A later flag change cannot cancel this selected emission. Null remains null; custom span signatures retain borrowed argument lifetime. This is the typed C# extension under ADRs 0002/0010.

<a id="m-electron2d-electronobject-isclass-system-string"></a>
### `public bool IsClass(string className)`

Returns true for the actual unqualified CLR class name or an ElectronObject ancestor,
using ordinal case-sensitive comparison. Compiled subclasses participate. Interfaces,
System.Object, script/file registry aliases and qualified names do not. Null rejects
with ArgumentNullException; empty, whitespace and unknown names return false. Like
ClassName/InstanceID, this metadata remains readable during/after disposal and from
another thread; it neither prepares nor reads numerical state.

```csharp
using var body = new RigidBody();
bool physical = body.IsClass(nameof(PhysicsBody)); // true
bool resource = body.IsClass(nameof(Resource));    // false
```

<a id="m-electron2d-electronobject-notify-system-int32"></a>
### `public void Notify(int what)`

Synchronously delivers a numeric engine notification to this object.

**Parameters**

- `what`: The notification identifier.

**Exceptions**

- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.
- `Exception`: [`ElectronObject.OnNotification(Int32)`](ElectronObject.md#m-electron2d-electronobject-onnotification-system-int32) throws.

**Remarks:** Delivery uses normal C# virtual dispatch. An override of [`ElectronObject.OnNotification(Int32)`](ElectronObject.md#m-electron2d-electronobject-onnotification-system-int32) is responsible for calling
its base implementation when inherited behavior is required. This overload selects the ordinary direction.

<a id="m-electron2d-electronobject-notify-system-int32-system-boolean"></a>
### `public void Notify(int what, bool reversed)`

Synchronously delivers `what`. With `reversed: true`, each built-in handler finishes its local handling before calling its base. False retains the existing base-call points, including handler-local error aggregation and cleanup. An unhandled exception stops the unvisited chain and propagates unchanged. Numeric delivery remains independent of signal blocking and retains existing lifetime and owner guards.

<a id="p-electron2d-electronobject-isnotificationreversed"></a>
### `protected bool IsNotificationReversed`

True only inside this object's active reversed invocation on the current thread. Nested calls on the same or another object restore the prior context even after failure. Concurrent threads have independent contexts; the value does not flow through asynchronous continuations. Custom compiled subclasses follow the [notification decision](../decisions/core-object-runtime.md#adr-0005) by choosing where to call their base:

```csharp
protected override void OnNotification(int what)
{
    bool reversed = IsNotificationReversed;
    if (!reversed) base.OnNotification(what);
    HandleLocalNotification(what);
    if (reversed) base.OnNotification(what);
}
```

Intentionally omitting a base call suppresses inherited handling, as in ordinary C#. Automatic pre-delete uses reversed delivery; ordinary scene/frame producers use normal delivery. Post-initialization 0 remains reserved and is not emitted automatically.

<a id="m-electron2d-electronobject-getpropertylist"></a>
### `public IReadOnlyList<PropertyDescriptor> GetPropertyList()`

Builds the current validated list of typed properties exposed to tooling.

**Returns:** A read-only snapshot whose property names are unique using ordinal comparison.

**Exceptions**

- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.
- `ArgumentNullException`: [`ElectronObject.GetPropertyDescriptors`](ElectronObject.md#m-electron2d-electronobject-getpropertydescriptors) yielded a null descriptor.
- `ArgumentException`: A validated descriptor is incompatible with this object's runtime type.
- `InvalidOperationException`: Two validated descriptors have the same name.
- `Exception`: A derived property-discovery or validation hook throws.

<a id="m-electron2d-electronobject-propertycanrevert-electron2d-propertydescriptor"></a>
### `public bool PropertyCanRevert(PropertyDescriptor property)`

Reports whether a tooling property currently differs from its revert value.

**Parameters**

- `property`: A descriptor compatible with this object.

**Returns:** `true` when the property has a distinct revert value; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `property` is `null`.
- `ArgumentException`: `property` is not compatible with this object.
- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.
- `Exception`: A descriptor delegate or custom revert-policy override throws.

<a id="m-electron2d-electronobject-revertproperty-electron2d-propertydescriptor"></a>
### `public void RevertProperty(PropertyDescriptor property)`

Restores a tooling property to its current typed revert value.

**Parameters**

- `property`: A writable descriptor compatible with this object.

**Exceptions**

- `ArgumentNullException`: `property` is `null`.
- `ArgumentException`: `property` is not compatible with this object.
- `InvalidOperationException`: The descriptor has no writable revert value.
- `ObjectDisposedException`: Disposal of the object has started.
- `Exception`: A descriptor delegate throws.

<a id="m-electron2d-electronobject-tr-system-string-system-string"></a>
### `public string Tr(string message, string context = null)`

Translates a singular message using this object's translation domain.

**Parameters**

- `message`: The source message.
- `context`: An optional disambiguation context. A null context is equivalent to an empty context.

**Returns:** The resolved translation, or `message` when translation is disabled or no entry exists.

**Exceptions**

- `ArgumentNullException`: `message` is `null`.
- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.

<a id="m-electron2d-electronobject-trn-system-string-system-string-system-int64-system-string"></a>
### `public string TrN(string singular, string plural, long count, string context = null)`

Translates a plural message using this object's translation domain.

**Parameters**

- `singular`: The source singular form.
- `plural`: The source plural form.
- `count`: The quantity passed to the registered plural selector.
- `context`: An optional disambiguation context. A null context is equivalent to an empty context.

**Returns:** The resolved plural translation. Without a matching translation, the singular form is returned only for
`count` equal to `1`; otherwise the plural form is returned.

**Exceptions**

- `ArgumentNullException`: `singular` or `plural` is `null`.
- `InvalidOperationException`: A matching plural selector returns `null`.
- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.
- `Exception`: A matching plural selector throws.

<a id="m-electron2d-electronobject-dispose"></a>
### `public void Dispose()`

Deterministically releases resources owned by this object.

**Exceptions**

- `AggregateException`: Both notification delivery and derived cleanup fail.
- `Exception`: Disposal validation, a pre-delete callback, derived cleanup, or a [`ElectronObject.Disposed`](ElectronObject.md#e-electron2d-electronobject-disposed) handler fails.
Validation failure leaves this caller from starting disposal; a [`ElectronObject.Disposed`](ElectronObject.md#e-electron2d-electronobject-disposed) handler failure occurs
after the final disposed state has been published.

**Remarks:** Committed cleanup is idempotent. Unless automatic pre-delete is vetoed, the winning caller synchronously sends [`ElectronObject.NotificationPreDelete`](ElectronObject.md#f-electron2d-electronobject-notificationpredelete), invokes
[`ElectronObject.Dispose(Boolean)`](ElectronObject.md#m-electron2d-electronobject-dispose-system-boolean), publishes the final state, clears base event subscribers, and suppresses finalization.
Callers that lose the atomic transition return without repeating cleanup, although caller-specific
[`ElectronObject.ValidateDisposal`](ElectronObject.md#m-electron2d-electronobject-validatedisposal) may already have run and may throw before that transition. The disposing thread
may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts. A pre-delete veto restores live state after notification returns, preserving identity, subscribers and resources. Notification errors still propagate after that restoration; a later caller may retry.

<a id="m-electron2d-electronobject-onnotification-system-int32"></a>
### `protected virtual void OnNotification(int what)`

Handles an engine notification delivered to this object.

**Parameters**

- `what`: The notification identifier.

**Remarks:** Derived overrides should call the base implementation unless they intentionally suppress inherited handling. Use `IsNotificationReversed` to put the base call after local handling for derived-first delivery. Automatic pre-delete is reversed; other automatic notifications retain their ordinary direction.

<a id="m-electron2d-electronobject-validatemutation"></a>
### `protected virtual void ValidateMutation()`

Validates that mutable base state may change at the current lifecycle point.

**Exceptions**

- `ObjectDisposedException`: Disposal has started.

**Remarks:** Derived types may reject mutation while they are participating in an atomic operation.

<a id="m-electron2d-electronobject-getpropertydescriptors"></a>
### `protected virtual IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

**Returns:** The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

**Remarks:** Overrides append or replace descriptors; they must not yield null entries.

<a id="m-electron2d-electronobject-validateproperty-electron2d-propertydescriptor"></a>
### `protected virtual PropertyDescriptor ValidateProperty(PropertyDescriptor property)`

Validates or customizes one property before it is exposed to tooling.

**Parameters**

- `property`: The descriptor supplied by [`ElectronObject.GetPropertyDescriptors`](ElectronObject.md#m-electron2d-electronobject-getpropertydescriptors).

**Returns:** The descriptor to expose, a replacement descriptor, or `null` to hide the property.

<a id="m-electron2d-electronobject-canrevertproperty-electron2d-propertydescriptor"></a>
### `protected virtual bool CanRevertProperty(PropertyDescriptor property)`

Determines whether a property has a distinct revert value.

**Parameters**

- `property`: A descriptor already validated for this object.

**Returns:** `true` when the property can currently be reverted; otherwise `false`.

<a id="m-electron2d-electronobject-notifypropertylistchanged"></a>
### `public void NotifyPropertyListChanged()`

Synchronously raises [`ElectronObject.PropertyListChanged`](ElectronObject.md#e-electron2d-electronobject-propertylistchanged). The public publisher runs on the calling thread, honors signal blocking and preserves sender identity. It changes neither properties nor Resource.Changed. Handlers keep ordinary capture, lifetime and numerical access guards. Exact teardown-thread metadata remains readable; terminal/foreign teardown access rejects.

**Exceptions**

- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.
- `Exception`: An event handler throws.

<a id="m-electron2d-electronobject-notifyscriptchanged"></a>
### `protected void NotifyScriptChanged()`

Synchronously raises [`ElectronObject.ScriptChanged`](ElectronObject.md#e-electron2d-electronobject-scriptchanged).

**Remarks:** Publishes the typed notification without changing the source reference. Signal blocking suppresses delivery.

**Exceptions**

- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.
- `Exception`: An event handler throws.

**Remarks:** A future live script-assignment workflow should call this when its script reference changes.

<a id="m-electron2d-electronobject-dispose-system-boolean"></a>
### `protected virtual void Dispose(bool disposing)`

Releases resources owned by a derived class.

**Parameters**

- `disposing`: `true` when called from [`ElectronObject.Dispose`](ElectronObject.md#m-electron2d-electronobject-dispose).

**Remarks:** Overrides release managed resources when `disposing` is true and then call the base implementation.

<a id="m-electron2d-electronobject-validatedisposal"></a>
### `protected virtual void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

**Remarks:** This method can run concurrently in multiple callers and can race with another caller starting disposal.
Overrides must therefore be side-effect-free and tolerate repeated execution.

<a id="m-electron2d-electronobject-throwifdisposed"></a>
### `protected void ThrowIfDisposed()`

Rejects access after disposal starts, except on the thread currently running disposal callbacks.

**Exceptions**

- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.

<a id="m-electron2d-electronobject-tostring"></a>
### `public override string ToString()`

Returns a diagnostic string containing the runtime class name and instance identifier.

**Returns:** A string in the form `<ClassName>#<InstanceID>`.

## Event Descriptions

<a id="e-electron2d-electronobject-disposed"></a>
### `public event Action<ElectronObject> Disposed`

Occurs once after the object's owned resources have been released successfully.

**Remarks:** The event is not raised when the pre-delete notification or derived cleanup throws. The object is already in
its final disposed state when handlers run, and handler exceptions propagate to the disposing caller.

<a id="e-electron2d-electronobject-propertylistchanged"></a>
### `public event Action<ElectronObject> PropertyListChanged`

Occurs when [`ElectronObject.NotifyPropertyListChanged`](ElectronObject.md#m-electron2d-electronobject-notifypropertylistchanged) reports that the tooling property list changed.

**Remarks:** Delivery is synchronous on the notifying thread.

<a id="e-electron2d-electronobject-scriptchanged"></a>
### `public event Action<ElectronObject> ScriptChanged`

Occurs when source publication or a derived scripting component reports a changed script association.

**Remarks:** Trusted file replacement and prepared-reference remapping notify after committing a different source identity and its ownership. PropertyListChanged precedes this event; a resource copy follows with its final Changed. Equal references, initial construction, source text editing and disposal add no signal. [Live assignment](../decisions/scripting.md#live-attachment-and-rebuild-boundaries) remains open.

## Constant Descriptions

<a id="f-electron2d-electronobject-notificationpostinitialize"></a>
### `public const int NotificationPostInitialize = 0`

Identifies the internal initialization boundary before project callbacks are attached.

**Remarks:** Ordinary C# construction initializes engine state without invoking project notification overrides. This identifier
is not emitted automatically to compiled project classes. A host may explicitly send it after construction through
Notify(int) or Notify(int, bool) with their ordinary dispatch and error semantics. See the
[compiled construction boundary](../components/object-lifecycle.md#compiled-construction-notification-boundary).

<a id="f-electron2d-electronobject-notificationpredelete"></a>
### `public const int NotificationPreDelete = 1`

Identifies the notification delivered immediately before owned resources are released.

## Lifecycle contract

`Dispose()` first returns when disposal has already started, then calls `ValidateDisposal()` before that caller attempts the atomic transition from alive to disposing. The winning caller records its thread and sends notification `1` with reversed delivery. A veto restores live state after notification without invoking cleanup; otherwise it calls `Dispose(bool)`, publishes the final disposed state, clears the `Disposed`, `PropertyListChanged`, and `ScriptChanged` subscriber lists, and suppresses finalization. Concurrent calls that reach the transition after another caller return without repeating work.

`IsDisposed` is already `true` during notification and cleanup. `ThrowIfDisposed()` nevertheless permits the recorded disposing thread to inspect guarded state so pre-delete and derived teardown callbacks can identify and detach the object. Other threads are rejected as soon as disposal starts; after final publication every thread, including the former disposing thread, is rejected.

Notification and cleanup exceptions are both preserved: one exception is rethrown with its stack, while simultaneous failures become `AggregateException`. `Disposed` is raised only when both stages complete successfully.

[`EventConnection`](EventConnection.md) can own subscriptions to the object's typed events, consume one emission, or route delivery through an explicit scheduler such as `SceneTree.Defer`. The events themselves continue to pass the publishing object as their argument.

Derived resource owners dispose their resources and call the base override:

```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
        _ownedResource.Dispose();

    base.Dispose(disposing);
}
```

## Invariants and threading

- Instance IDs do not change; parallel allocation is safe.
- Notification delivery is synchronous on the caller's thread.
- Disposal entry and disposed-state publication are thread-safe and cleanup runs at most once.
- The disposing-thread exception is scoped to `ThrowIfDisposed`; it is not a general synchronization guarantee and does not make re-entrant mutation during teardown safe.
- `ValidateDisposal()` may run concurrently in more than one caller and may race with another caller beginning disposal; overrides must be side-effect-free and tolerate that race. A validation failure prevents its own caller from starting disposal but cannot prevent another valid caller.
- Translation configuration and property events have their documented member-level synchronization only; derived mutable state is not made thread-safe. `Node` overrides mutation validation so inherited translation setters cannot change a hierarchy during packed capture.
- Reference equality remains standard .NET reference equality.

## Native resources

The class has no finalizer. Derived SDL resource types must put native handles in `SafeHandle` wrappers and dispose them from `Dispose(bool)`.

## Deliberate omissions

- No `Variant`, `dynamic`, string-based `Get`, `Set`, or `Call`.
- No metadata bag, live script attachment or generic signal registry. Compiled C# Script assets and ordinary constructed callbacks execute independently. `ScriptChanged` reports trusted file/remapping source changes; live assignment/detachment remains unimplemented.
- Arbitrary CLR delegate subscribers are omitted from packed scenes. Authored persistent connections use registered typed SceneConnectionBinding factories under ADR 0010.
- No public object lookup by `InstanceID`.
- Deletion is scheduled by Node.QueueFree or SceneTree.QueueDelete. ElectronObject.IsQueuedForDeletion observes an accepted or executing request for this object; it does not schedule one.

## Verification

The public-only disposal-veto consumer checks automatic/manual/foreign/late windows, callback errors, identity and subscriptions, independent queued attempts, rejected child cleanup, root reactivation and provider retirement. CPU/software-Vulkan/registered owner/separate profiles measure 128 complete veto/simulation/query frames at zero owner/all-thread managed bytes.


[`tests/Electron2D.Tests/Program.cs`](../../tests/Electron2D.Tests/Program.cs) verifies identity, diagnostics, notification dispatch, pre-delete state access on the disposing thread, concurrent idempotent disposal, property-list and script-change event delivery, duplicate subscription/removal, typed event connections, typed property behavior, translation delegation, and invalid access after disposal.


Construction registers one shared weak identity for [object lifetime monitors](../components/object-lifetime-monitors.md); internal BorrowIdentity reuses it. Borrowers copy its immutable instance ID and weak reference without rooting the target; disposal makes resolution null immediately. [ObjectIdentity](ObjectIdentity.md) introduces no public registry or finalizer.

ObjectIdentityTests and the existing instance/lifetime checks exercise concurrent
nonzero identity, diagnostic override dispatch, cleanup errors, registered script
names and the full scene-body/resource ancestry on CPU/GPU, serial/separate.
128 warm/128 measured full frames plus repeated hierarchy reads require 0/0
owner/all-thread managed bytes. Identity stays readable after actual scene retirement.
Software Vulkan execution does not establish hardware/native/foreign or wider
inherited API acceptance.

[ObjectPropertyTests](../../tests/Electron2D.Tests/ObjectPropertyTests.cs) executes discovery snapshots, compatible replacement/filtering and invalid lists, owner policy, live/default/null/array values, readonly/validation/delegate errors and teardown inspection. Explicit CPU/GPU serial and separate-thread profiles use real RigidBody properties, permitted post-solver mass edits, owner/capture rejection and callback-error recovery. Each retains 128 warm/128 measured full frames with typed inspection, velocity writes and reversion at strict zero owner/all-thread managed bytes. GPU runs use CPU-only Vulkan; this does not establish hardware performance, editor automation or native/platform acceptance.

Inherited object translation-domain access and plural lookup execute through [scored runtime lookup](../components/localization.md#scored-runtime-lookup). Catalog selection preserves live edits, callback errors and physical identities with strict zero warmed managed bytes. Singular translation now uses verified [Unicode pseudolocalization](../components/localization.md#unicode-pseudolocalization); the plural path intentionally bypasses those transforms.

The [deferred call/setter contract](../components/scene-tree.md#complete-deferred-call-and-setter-contract) drains nested typed work at the current safe point before queued deletion. Selected physical frames retain exact tick counts and zero warmed managed allocation; platform and full-goal acceptance remain separate.

## Factory construction provenance

The public constructor keeps the existing process-local identity and records this reference in active internal factory scopes. Resource copies, compiled scripts, file factories and scene factories validate against those scopes before acquiring their result. A later global instance number does not prove ownership. The internal nested ConstructionScope uses transient reference-identity sets; closing releases their contents even if an execution context escapes. Ordinary context flow and the engine worker pool carry this capability. Suppressed flow and unrelated threads cannot produce an accepted factory result. See [ADR 0013](../decisions/resources.md#adr-0013). Its closed context retains no recorded objects or sources; an associated owner retains one nullable source reference.
