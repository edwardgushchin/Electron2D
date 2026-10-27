# Extension module decisions

Last updated: 2026-10-27

<a id="adr-0116"></a>
## ADR 0116: Typed function-loaded extension module lifecycle

- Status: Accepted implementation decision for the requested complete inherited contract.
- Scope: Executable extension entry, resource identity and manager initialization/unloading.
- Preserves: [0004](product.md#adr-0004), [0003](core-object-runtime.md#adr-0003),
  [0013](resources.md#adr-0013), [0091](scripting.md#adr-0091),
  [0095](singleton-services.md#adr-0095).

### Decision

Project extension modules and ordinary Script classes are separate roles.
`Extension : Resource` projects the extension resource. `ExtensionManager` uses
static operations backed by one retained service, without a public Instance.
The typed counterpart of `load_extension_from_function` accepts an exact ordinal
address and `Func<Extension, ExtensionInitialization?>`. Null declines loading;
the readonly initialization value supplies the minimum replacement level and
typed ascending/descending callbacks. One namespace-level ExtensionInitializationLevel
serves the resource and initialization value under ADR 0051. Closures replace opaque native userdata.
The entry receives a fresh closed resource. No address is interpreted as an
assembly/file name, and this operation performs no reflection or dynamic loading.

As a library, Electron2D already makes core, server and scene APIs callable without
Engine.Run. A load therefore captures available levels through Scene, or Editor
when the host has set Engine.IsEditorHint at that load's start. Later hint edits
do not reinitialize existing modules. First loading calls Core upward through the
captured level, independently of minimum replacement level; that minimum describes
replacement prerequisites rather than filtering first-load callbacks. Unloading
retires exactly the initialized levels in reverse. This is the explicit library
host projection of the reference manager's current initialization stage.

The first mutating caller owns later mutations. Registry snapshots/borrowed reads
are thread-safe. A module address remains reserved during entry/initialization/
publication/deinitialization; recursive mutation of that address rejects before
effects. Different-address dependency loads remain possible. Callbacks run outside
the registry lock. The loaded event precedes resource-path/registry publication;
the unloading event precedes deinitialization and retirement. A loaded-observer
error propagates after publication. Unloading collects observer/deinitialization
errors, attempts every initialized level and commits retirement before propagating
the original single error or an ordered aggregate.

Entry/initialization/path-publication failure publishes no module and unwinds
initialized levels, including the failed level. Caller callback side effects and
independently loaded dependencies are not general transactions. Rollback disposes
the unpublished resource. Registered module resources are borrowed: explicit
disposal rejects while manager-owned. After unload an external resource reference
may keep its entry open until ordinary Dispose; no implicit CLR assembly unload is
claimed. Duplication and files copy only Resource metadata into a closed resource,
never executable callbacks or a numerical world.

### Boundaries and continuation

This first executable lifecycle does not implement file/native-library discovery,
extension class registration/rebinding, live module replacement or reload events.
Their public operations are absent and their coverage obligations remain open.
In particular, no NotificationExtensionReloaded is exposed/emitted by ordinary
C# code updates. The [producer audit](../components/scripting.md#native-extension-reload-boundary)
still requires class-bound instance restoration before notification. New class
binding must reuse typed factory/property/identity lifetime mechanisms and define
removed classes/methods, stale callbacks and partial replacement failure together.

### Verification

The public-only ExtensionLifecycle consumer exercises lifecycle order, exact
address/resource identity, nested loading, owner guards, callbacks/rollback errors,
closed duplication/archive restoration and a selected CPU/GPU world actually
created and retired by module callbacks. Executed profiles and limits belong in
the [module component](../components/extensions.md); compilation alone is not
reload, platform, native-heap or performance acceptance.
