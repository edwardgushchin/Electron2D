# ElectronObject

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** [ConfigFile](ConfigFile.md), [DirAccess](DirAccess.md), [DisplayServer](DisplayServer.md), [Engine](Engine.md), [FileAccess](FileAccess.md), [Input](Input.md), [InputMap](InputMap.md), [MainLoop](MainLoop.md), [Node](Node.md), [ProjectSettings](ProjectSettings.md), [Resource](Resource.md), [SceneState](SceneState.md), [SceneTreeTimer](SceneTreeTimer.md), [Tween](Tween.md), [Tweener](Tweener.md)

- **Source:** [`src/Core/Object/ElectronObject.cs`](../../src/Core/Object/ElectronObject.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class ElectronObject`

> Provides the common identity, notification, property, localization, and lifetime contract for engine objects.

## Description

Provides the common identity, notification, property, localization, and lifetime contract for engine objects.

`ElectronObject` is the common base for engine-owned objects. It provides process-local identity, runtime diagnostics, numeric notification dispatch, deterministic cleanup, disposed-state protection, typed property exposure, per-object translation settings, and lifecycle events. It does not emulate a universal-value dynamic API.

The contract uses typed properties and events and deliberately omits dynamic calls, untyped metadata, generic signal
registration, and script storage. Instances have deterministic `IDisposable` lifetime.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
using var node = new Node();
ElectronObject value = node;
Console.WriteLine(value.InstanceId);
```

## Constructors

| Member | Description |
| --- | --- |
| [`protected ElectronObject()`](#m-electron2d-electronobject-ctor) | Initializes a new ElectronObject instance. |

## Properties

| Member | Description |
| --- | --- |
| [`public ulong InstanceId { get; }`](#p-electron2d-electronobject-instanceid) | Gets this object's process-local instance identifier. |
| [`public string ClassName { get; }`](#p-electron2d-electronobject-classname) | Gets the unqualified runtime class name. |
| [`public bool IsDisposed { get; }`](#p-electron2d-electronobject-isdisposed) | Gets whether deterministic disposal has started. |
| [`public bool CanTranslateMessages { get; set; }`](#p-electron2d-electronobject-cantranslatemessages) | Gets or sets whether this object resolves messages through [`TranslationServer`](TranslationServer.md). |
| [`public string TranslationDomain { get; set; }`](#p-electron2d-electronobject-translationdomain) | Gets or sets the translation domain used by this object. |

## Methods

| Member | Description |
| --- | --- |
| [`public void Notify(int what)`](#m-electron2d-electronobject-notify-system-int32) | Synchronously delivers a numeric engine notification to this object. |
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
| [`protected void NotifyPropertyListChanged()`](#m-electron2d-electronobject-notifypropertylistchanged) | Synchronously raises [`ElectronObject.PropertyListChanged`](ElectronObject.md#e-electron2d-electronobject-propertylistchanged). |
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
| [`public event Action<ElectronObject> ScriptChanged`](#e-electron2d-electronobject-scriptchanged) | Occurs when a derived scripting component reports that this object's script attachment changed. |

## Constants

| Member | Description |
| --- | --- |
| [`public const int NotificationPostInitialize = 0`](#f-electron2d-electronobject-notificationpostinitialize) | Identifies the post-initialization notification. |
| [`public const int NotificationPreDelete = 1`](#f-electron2d-electronobject-notificationpredelete) | Identifies the notification delivered immediately before owned resources are released. |

## Constructor Descriptions

<a id="m-electron2d-electronobject-ctor"></a>
### `protected ElectronObject()`

Initializes a new ElectronObject instance.

## Property Descriptions

<a id="p-electron2d-electronobject-instanceid"></a>
### `public ulong InstanceId { get; }`

Gets this object's process-local instance identifier.

**Value:** A nonzero identifier that is never changed or reused during the current process.

<a id="p-electron2d-electronobject-classname"></a>
### `public string ClassName { get; }`

Gets the unqualified runtime class name.

**Value:** The `Name` of the `Type` returned by `Object.GetType`.

<a id="p-electron2d-electronobject-isdisposed"></a>
### `public bool IsDisposed { get; }`

Gets whether deterministic disposal has started.

**Value:** `true` from the moment a caller wins the disposal transition; otherwise `false`.

<a id="p-electron2d-electronobject-cantranslatemessages"></a>
### `public bool CanTranslateMessages { get; set; }`

Gets or sets whether this object resolves messages through [`TranslationServer`](TranslationServer.md).

**Value:** `true` by default; `false` to return source messages unchanged.

**Exceptions**

- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.

<a id="p-electron2d-electronobject-translationdomain"></a>
### `public string TranslationDomain { get; set; }`

Gets or sets the translation domain used by this object.

**Value:** The case-sensitive domain passed to [`TranslationServer`](TranslationServer.md). The default is an empty string.

**Exceptions**

- `ArgumentNullException`: The assigned value is `null`.
- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.

## Method Descriptions

<a id="m-electron2d-electronobject-notify-system-int32"></a>
### `public void Notify(int what)`

Synchronously delivers a numeric engine notification to this object.

**Parameters**

- `what`: The notification identifier.

**Exceptions**

- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.
- `Exception`: [`ElectronObject.OnNotification(Int32)`](ElectronObject.md#m-electron2d-electronobject-onnotification-system-int32) throws.

**Remarks:** Delivery uses normal C# virtual dispatch. An override of [`ElectronObject.OnNotification(Int32)`](ElectronObject.md#m-electron2d-electronobject-onnotification-system-int32) is responsible for calling
its base implementation when inherited behavior is required.

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
- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.
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

**Remarks:** Disposal is idempotent. The winning caller synchronously sends [`ElectronObject.NotificationPreDelete`](ElectronObject.md#f-electron2d-electronobject-notificationpredelete), invokes
[`ElectronObject.Dispose(Boolean)`](ElectronObject.md#m-electron2d-electronobject-dispose-system-boolean), publishes the final state, clears base event subscribers, and suppresses finalization.
Callers that lose the atomic transition return without repeating cleanup, although caller-specific
[`ElectronObject.ValidateDisposal`](ElectronObject.md#m-electron2d-electronobject-validatedisposal) may already have run and may throw before that transition. The disposing thread
may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

<a id="m-electron2d-electronobject-onnotification-system-int32"></a>
### `protected virtual void OnNotification(int what)`

Handles an engine notification delivered to this object.

**Parameters**

- `what`: The notification identifier.

**Remarks:** Derived overrides should call the base implementation unless they intentionally suppress inherited handling.

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
### `protected void NotifyPropertyListChanged()`

Synchronously raises [`ElectronObject.PropertyListChanged`](ElectronObject.md#e-electron2d-electronobject-propertylistchanged).

**Exceptions**

- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.
- `Exception`: An event handler throws.

<a id="m-electron2d-electronobject-notifyscriptchanged"></a>
### `protected void NotifyScriptChanged()`

Synchronously raises [`ElectronObject.ScriptChanged`](ElectronObject.md#e-electron2d-electronobject-scriptchanged).

**Exceptions**

- `ObjectDisposedException`: The object is disposing on another thread or has finished disposing.
- `Exception`: An event handler throws.

**Remarks:** A future scripting component should call this when its script reference changes.

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

**Returns:** A string in the form `<ClassName>#<InstanceId>`.

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

Occurs when a derived scripting component reports that this object's script attachment changed.

**Remarks:** Electron2D does not yet provide a script attachment. The event reserves the typed notification contract for a
future scripting component and is currently raised only through [`ElectronObject.NotifyScriptChanged`](ElectronObject.md#m-electron2d-electronobject-notifyscriptchanged).

## Constant Descriptions

<a id="f-electron2d-electronobject-notificationpostinitialize"></a>
### `public const int NotificationPostInitialize = 0`

Identifies the post-initialization notification.

**Remarks:** Electron2D does not dispatch this notification from the base constructor because invoking virtual members during
construction is unsafe; a host may dispatch it explicitly after construction.

<a id="f-electron2d-electronobject-notificationpredelete"></a>
### `public const int NotificationPreDelete = 1`

Identifies the notification delivered immediately before owned resources are released.

## Lifecycle contract

`Dispose()` first returns when disposal has already started, then calls `ValidateDisposal()` before that caller attempts the atomic transition from alive to disposing. The winning caller records its thread, sends notification `1`, calls `Dispose(bool)`, publishes the final disposed state, clears the `Disposed`, `PropertyListChanged`, and `ScriptChanged` subscriber lists, and suppresses finalization. Concurrent calls that reach the transition after another caller return without repeating work.

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
- No metadata bag, script attachment, script runtime, or generic signal registry. `ScriptChanged` is the typed notification contract reserved for the confirmed future scripting component; nothing raises it automatically yet.
- No persistent event connections; in-memory packed scenes intentionally omit subscribers because a typed stable endpoint schema does not yet exist.
- No global registry or lookup by `InstanceId`.
- No queued deletion; that behavior belongs to [`Node`](Node.md) and [`SceneTree`](SceneTree.md).

## Verification

[`tests/Electron2D.Tests/Program.cs`](../../tests/Electron2D.Tests/Program.cs) verifies identity, diagnostics, notification dispatch, pre-delete state access on the disposing thread, concurrent idempotent disposal, property-list and script-change event delivery, duplicate subscription/removal, typed event connections, typed property behavior, translation delegation, and invalid access after disposal.
