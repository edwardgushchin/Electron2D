# Function-loaded extension modules

Last updated: 2026-10-27

[ADR 0116](../decisions/extensions.md#adr-0116) owns the library-host projection.
This is the first executing module lifecycle prerequisite for the
[extension-reload producer](scripting.md#native-extension-reload-boundary).

## Executable boundary

`ExtensionManager.LoadExtensionFromFunction` accepts an ordinal address and a typed
entry receiving its fresh closed `Extension` resource. A nullable
`ExtensionInitialization` result can decline loading. A valid result supplies two
callbacks and one shared `ExtensionInitializationLevel` value contract. First load
calls Core, Servers and Scene, plus Editor when captured from the host hint. The
minimum level remains readable replacement metadata; first loading does not skip
earlier levels. The reference
[function loader and manager](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/extension/gdextension_manager.cpp#L125)
provide this entry/lifecycle concept without requiring an address to name a file.

Loaded notification follows initialization and precedes resource-path/registry
publication. Its observer errors propagate after publication. Unloading notification
runs while lookup still resolves the same resource, before reverse deinitialization
and retirement. Initialization failure unwinds the failed level and every earlier
initialized level. Cleanup continues across errors and preserves the original
single exception or an ordered aggregate. Different-address dependency loading is
allowed; the currently transitioning address cannot mutate recursively. Caller
callback side effects and independently loaded dependencies are not general
transactions.

The registry captures its first mutating caller as owner. Reads return borrowed
resources or independent address snapshots. The manager is a permanent reserved
Engine singleton with static operations; its ordinary events use the shared
publisher/membership and signal-blocking contract. A registered resource cannot be
explicitly disposed. Unloading retires module initialization but an external
resource reference can retain its open callbacks until Dispose, matching the
separate resource lifetime. Duplication and resource files preserve base metadata
in a closed resource and never copy/activate executable callbacks or a physical
world. Project-derived resources still need their own exact construction/copy
implementation.

## Public consumer and verification

[ExtensionLifecycle.Consumer](../../tests/ExtensionLifecycle.Consumer/Program.cs)
uses only public runtime APIs. Its external entry actually creates an awake circular
body and selected raw CPU/GPU space at Scene, then retires them at reverse Scene.
The example explicitly activates raw spaces; creation alone is not a solved tick.
After 128 warm steps, 128 measured complete step/getter intervals require 0/0
owner/all-thread managed bytes. A 60-unit/s body travels 128 ± .01 scene units over
128 ticks at 1/60 second; this bound allows accumulated single-precision position
rounding and does not relax collision/solver tolerances.

Six CLR and the same six strict NativeAOT profiles pass: CPU/no-GPU and GPU with ordinary/editor levels, plus each
backend with a separate numerical thread. CPU profiles use dummy video and invalid
Vulkan ICD paths. NativeAOT publication treats compiler and trimming warnings as errors. All selected backends/fallback identities are asserted. The same
consumer verifies entry decline, invalid data, original callback errors, failure
after real numerical-world creation, failed-level rollback, observer error commit,
continued cleanup, nested different-address loads, same-address recursion, foreign
mutations, registry reads, signal suppression and exact EventConnection membership
retirement. Duplication/archive checks restore closed resources; no callback is
replayed by storage. Receipts, source/binary identities and outputs remain under
`bin/extension-lifecycle/2026-10-27/`.

Initial candidates exposed one real missing resource-copy override. After its fix,
the fixture's wrong file suffix and missing raw-space activation were corrected;
those latter failures were fixture omissions, not changes to storage or physics
semantics. The early failed receipts remain alongside the successful matrix.

## Open work

File/native-library discovery, extension class registration/rebinding, state
restoration and live module replacement remain absent. There is no inert file-load
or reload operation and no ordinary-CLR-wide reload notification. The applicable
coverage rows remain open. This lifecycle does not close inherited live SetScript,
ScriptChanged, current/peak allocation producers or the three base-class audits.
Physics declaration coverage stays 1362/1369 (99.49%), seven open, 92 Excluded.
Native-heap, foreign-platform, arbitrary module code and full-goal numerical/network/
window/performance acceptance remain separate.
