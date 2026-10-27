# Function-loaded extension modules

Last updated: 2026-10-27

[ADR 0116](../decisions/extensions.md#adr-0116) owns the typed library-host contract.
The public-only [reload consumer](../../tests/ExtensionReload.Consumer/Program.cs)
and [source/factory checks](../../tests/Electron2D.Tests/ExtensionSourceTests.cs)
exercise the class-bound producer, separately from ordinary compiled source updates.

## Module entry and levels

`ExtensionManager.LoadExtensionFromFunction` takes an ordinal address and a typed
entry receiving its fresh closed `Extension`. Null declines loading; a valid
`ExtensionInitialization` supplies minimum-level metadata and ascending/descending
callbacks. First load calls Core, Servers and Scene, plus Editor captured from the
host hint. Its minimum does not skip earlier first-load callbacks. Addresses do
not identify a file or choose a CLR type through reflection.

Loaded notification follows initialization. On first load it precedes resource
path/registry publication; on replacement the same registered resource already
exists and all surviving instance state precedes delivery. Observer failures on
a successful load/replacement propagate after publication. Unloading notification
runs before reverse cleanup while lookup still resolves the resource. Cleanup
attempts every initialized level and preserves original errors/ordered aggregates.
The first mutating caller owns registry mutation and admitted class factories.
Reads use synchronized borrowed lookup and independent address snapshots.

Initialized levels are tracked independently from the host's available level.
Replacement deinitializes every initialized level, reopens the same retained
function entry, then initializes from the reopened minimum through the available
level. An earlier-than-Scene minimum returns NeedsRestart with an open, uninitialized
entry and retained retry state. A higher minimum can leave initialization holes;
those levels are not spuriously deinitialized later. The source
[manager](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/extension/gdextension_manager.cpp#L41)
owns the first-load/replacement and individual/batch event distinctions.

The manager is a permanent reserved Engine singleton with static operations and
shared EventPublisher membership/signal blocking. Managed resource disposal
rejects before mutation. Unload removes registration but an external resource
reference can keep its open entry until Dispose. Duplication/archive restoration
produce closed resources without executable callbacks, factories or worlds.

## Typed class binding

`Extension.RegisterClass<T>` installs one unique portable class ID through existing
ResourceFileTypes. A concrete class supplies a static method-group constructor;
a null factory is reserved for an abstract parent. Registered project ancestors
must precede descendants; built-in engine types cannot be redefined. Bind callbacks
install module functions/defaults in a stable CLR shell; clear callbacks retire
those functions, including partially failed binds. Parent bindings run first and
clear in reverse. There is no second per-frame dispatcher or untyped call channel.
Changing the CLR shell itself requires a new host cohort rather than retyping an
existing object.

`Extension.New<T>` and ordinary validated file/scene/copy/Script factories admit
instances, including CLR descendants of registered shells. Direct CLR construction
alone does not establish that role. Exact fresh-result and borrowed-result guards
run before binding. Constructor/binding scopes prevent participating modules from
retiring recursively. Script source identity remains available during construction;
module functions are installed after the constructor. Script metadata excludes
native-shell declarations and reports that shell as the instance base. The Scripts
flag removes script descendants while retaining a constructible native shell.
A removed native base makes dependent Script.CanInstantiate false and rejects
new construction before constructors.

Admitted instances use weak lifetime tracking. Their deterministic Dispose runs
on the manager thread; pre-delete veto preserves binding and owner identity.
Committed cleanup clears once and unregisters before owner cleanup, retaining both
errors if necessary. GC adds no arbitrary managed finalizer callback. Project
callbacks still own their native resources and side effects. Ordinary factories
without a module dependency retain their existing concurrency contract; module
factories reject worker-thread construction, including temporary archive factories.

## Replacement and stored state

`ReloadExtension` captures affected live instances, including registered descendants
in other modules, then clears old bindings and retires initialization. All surviving
class bindings finish before any stored state is restored. All compatible stored
values finish before automatic `ElectronObject.NotificationExtensionReloaded` (2).
Disposed/collected owners and removed classes are skipped. Newly created instances
from reinitialization are not old reload recipients. Reusing a removed class ID in another module does not adopt old instances.
Missing registered parents leave descendants cleared and reject further construction.

Existing PropertyDescriptor storage supplies typed scalar/resource/array snapshots.
Old defaults are omitted so newly bound defaults take effect; nondefault values
restore by exact compatible property name/type. Removed properties are discarded.
Live node references use weak identities, including across temporary renames;
file storage retains its separate path-based representation. Equal current values
skip their setters, preserving unchanged contact, sleeping and joint state.

Capture failure leaves the old generation running. Failures after retirement
attempt all replacement cleanup and leave a closed registered resource with the
original state retained for explicit retry. A CLR ABI mismatch follows this error
path. NeedsRestart is the separate open-but-uninitialized outcome. Unloading clears
pending state. Arbitrary callback effects and independent dependency loads are not
transactions. Attached owners require a quiescent scene lifecycle; participating
module retirement cannot reenter capture, binding, restoration or notification.

Successful individual replacement sends ExtensionLoaded after instance notices.
`ReloadExtensions(changedAddresses)` snapshots/validates the host's changed set,
deduplicates addresses, attempts each loaded entry and then sends ExtensionsReloaded
once, including failed or restart-required attempts. Unknown addresses return
NotLoaded; empty/unknown-only sets send nothing. Batch failures do not skip later
entries and propagate after completion. The host owns change detection. No file
watcher or ordinary CLR-wide reload notification is installed.

## Verification and limits

The reload consumer keeps actual scene bodies, contacts, sleeping state and a pin
joint on the explicitly selected CPU/GPU world. It checks stable object IDs/RIDs,
pose/velocity, nondefault/default state, array/resource/node identities, real changed
physics callbacks and zero managed owner/all-thread bytes over 128 complete physical
frames after 128 warm frames. A 60-unit/s body travels 128 +/- .02 scene units in
128 ticks at 1/60 s; the bound covers accumulated float position rounding. Every
notification verifies the other instances are already restored. Archive factories
execute in a fresh process. Ordinary unadmitted CLR instances receive no automatic
extension notification.

Failure cases cover capture, binding, restoration, deinitialization, declined entry,
ABI mismatch, explicit retry, removed classes/parents, new/dead owners, foreign
mutation, recursive disposal/factory operations, signal blocking, batch completion
and released callback/module captures. The CLR source check additionally exercises
Script metadata/source identity, script stripping, resource duplication and
pre-delete veto/error/foreign-disposal behavior. Receipts and immutable cohorts
live under `bin/extension-reload/2026-10-27/`; prior lifecycle-only receipts remain
under `bin/extension-lifecycle/2026-10-27/`.

File/native discovery, real assembly unloading, arbitrary native-module code,
foreign platforms and native heap costs remain separate qualifications. These
functional/allocation checks do not certify mass-scene throughput, full networking,
window FPS or the complete physics goal. Live SetScript/ScriptChanged and the
current/peak total-allocation producers retain their independent open contracts.
