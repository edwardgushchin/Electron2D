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

### Registered class binding and replacement

The executable continuation uses typed `RegisterClass<T>` factories plus bind/clear
callbacks. It reuses ResourceFileTypes identity and fresh exact construction,
ElectronObject's weak lifetime and PropertyDescriptor storage. A class has one
module owner; its registered CLR ancestors bind first and clear last. Abstract
registered parents provide binding without an instantiation factory. Direct CLR
construction alone does not establish an extension instance. Explicit New and resource/scene factories reuse the same exact-construction
validation; copy and Script factories also admit registered native ancestors. Registration
belongs to initialization; factories and replacement belong to the manager owner.

The CLR type is a stable ABI shell. Binding installs typed module functions in
that shell; clearing retires those functions. This is an explicit extension role,
separate from ordinary Script classes, and adds no per-frame dispatcher. Replacing
the CLR type itself requires a restart; source text or ordinary assembly metadata
changes do not implicitly rebind extension instances. Project callbacks own their
side effects and must clear every module capture they install.

Replacement reuses the original function loader and Extension resource. It first
captures nondefault stored properties of affected live instances, including
registered descendants in other modules. Replacement clears old instance bindings and deinitializes every initialized
level before reopening the entry. The reopened minimum controls initialization;
a minimum earlier than the available Scene stage returns NeedsRestart with an
open but uninitialized entry and retained retry state. Only levels actually
initialized are later deinitialized. The host availability captured by the first
load remains independent of this initialized-level set. All surviving class bindings are installed before any
state restoration; all compatible stored state is restored before notifications.
Node references use live weak identities, independent of temporary names or
hierarchy paths; file storage keeps its separate path-based representation.
Removed classes, IDs transferred to another module, or descendants with missing parents remain cleared and receive
no reload notification. Removed property names/types are discarded; newly
introduced/default properties retain the new binding's defaults. Disposed or
collected owners are skipped at every phase; newly created instances are not old
instance reload recipients.

Capture failure leaves the old module untouched. A failure after retirement
attempts cleanup of the replacement and leaves a closed registered resource for
an explicit retry; it sends no successful reload notification. Arbitrary callback
side effects are not transactions. Notification/observer failures occur after
the restored replacement commits and do not revert it. Retry preserves captured
state. Explicit unloading discards pending replacement state and retires classes.
The single replacement sends ExtensionUnloading before clearing/deinitialization
and ExtensionLoaded after successful instance notification. ExtensionsReloaded
belongs to the explicit host-supplied changed-address batch, once after all
attempts, including failed/restart-required entries. The batch does not install a
file watcher or reload ordinary CLR scripts. Attached instance replacement and
unloading reject during scene lifecycle execution or borrowed physical hooks.

### Boundaries and continuation

File/native-library discovery and actual CLR assembly unloading remain absent.
The typed function loader, stable class shells, instance restoration and reload
producer execute independently of those file/ABI hosts. Ordinary source edits or
CLR metadata updates do not automatically send NotificationExtensionReloaded.
A compiled Script descendant constructed through a validated factory participates
through its registered extension base; an unrelated CLR instance does not.

Factories bind only after exact fresh construction succeeds, before storage/copy
restoration or scene activation. Source association still precedes the derived
constructor; module functions become available when that factory completes.
Script metadata stops at the native shell and keeps its own source/schema role.
Removing Node's Scripts flag retains the nearest constructible native shell.
Weak type-role metadata remembers an unloaded native shell without retaining its
module/callbacks; missing native bases reject new construction before constructors.

Deterministic disposal of admitted instances uses the manager thread. Validation
and pre-delete veto precede binding retirement. Committed cleanup clears the
binding once, even if owner cleanup later fails; clear errors do not skip owner
cleanup. Collection retains no process-wide strong owner or module capture and
adds no managed callback on the finalizer thread. Module callbacks remain trusted
code responsible for native ownership and their own thread/side-effect policy.
Participating modules cannot retire recursively during capture, rebinding,
restoration, instance notification or deterministic disposal. Class construction
rejects during capture/retirement; initialization may construct new instances.

### Verification

The public-only ExtensionLifecycle consumer exercises lifecycle order, exact
address/resource identity, nested loading, owner guards, callbacks/rollback errors,
closed duplication/archive restoration and a selected CPU/GPU world actually
created and retired by module callbacks. Executed profiles and limits belong in
the [module component](../components/extensions.md); compilation alone is not
reload, platform, native-heap or performance acceptance.
