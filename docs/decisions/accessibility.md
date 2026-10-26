# Native accessibility and semantic scene output

Last updated: 2026-10-26

<a id="adr-0114"></a>
## ADR 0114: Typed accessibility elements with a native assistive-technology bridge

### Status

Accepted. This implements the current service ownership required by [ADR 0041](display.md#adr-0041), without reviving its retired DisplayServer API. Public operations follow [ADR 0095](singleton-services.md#adr-0095); scene ownership, compiled callbacks and allocation rules retain ADRs 0006, 0010, 0011 and 0014.

### Decision

AccessibilityServer is a static public service backed by a permanent internal ElectronObject. It owns session-unique element RIDs, typed roles/flags, parent/child relationships and registered compiled actions. RIDs are distinct from rendering/physics resources and never reused. Window roots are owned by the native window; ordinary elements by their producer, while sub-elements retire with their semantic parent. Stale/wrong-kind identities, foreign windows, cycles, incompatible action signatures and nonfinite geometry fail explicitly. Native failure must not publish an apparently supported service.

The first executable profile connects the core element tree, names/descriptions, bounds, checked/disabled/hidden state, focus and no-argument activation to Linux AT-SPI through the system GIO/GLib ABI. It exports the actual Accessible, Application, Component, Action and Cache protocols, registers with the session accessibility registry and emits topology/property/state/focus changes. The adapter owns its connection and private main context. Native requests run on the display owner during the existing event pump; application callbacks never run on a bus worker thread or cross an unmanaged exception boundary. Callback failures follow the display pump's aggregated exception policy. Retiring a window removes its tree and retiring the display releases all native registrations and connection/context ownership.

The runtime keeps one managed assembly. This Linux adapter uses the host's optional native GIO/GLib libraries and accessibility bus; missing libraries, absent bus and other targets report unavailable without a simulated native bridge. No desktop settings are changed. Native support, actual application registration, semantic output and a real assistive-client request remain distinct verification gates.

Automatic scene output uses Node element identity and queued accessibility-update notifications. Control adds stored, translatable accessible name/description. A visible Label uses its displayed text; a Button uses its displayed caption unless an explicit accessible name overrides it. An icon-only action needs an authored name, not an implementation identifier. Hidden/detached/disposed controls cannot be focused or activated. Existing keyboard focus and button activation paths own behavior; accessibility requests reuse them. Accessibility-only focus becomes eligible while the accessibility runtime is active. Scene copying/files retain authored metadata, never process-local element RIDs or callbacks.

The typed project policy supports automatic activation from the host's accessibility-enabled setting, explicit always-on output and disabled output. Tests use their own application's policy and never enable a desktop screen reader globally. Cold registration, topology changes and native queries may allocate; unchanged warmed frame work must preserve the existing zero-managed-allocation boundary. Native allocator totals remain a separate measurement.

Only behavior with a complete native consumer is exposed in each connected slice. Rich editable text/selection, value controls, tables, relationship families and payload-carrying actions retain exact follow-up dependencies. Their declarations must not become inert setters, invented generic value containers or unsupported callbacks. The complete applicable reference family remains the target; a partial first profile does not redefine that target.

### Verification

Require a separately launched AT-SPI client to discover the application and controls, read names/roles/states and window-relative bounds, request focus and activate an ordinary button through the native bus. Exercise live property/topology updates, borrowed/stale/wrong-kind RIDs, sub-element ownership, invalid geometry/cycles, callback failure/reentry, window/display retirement and fresh reopening. Verify authored metadata through duplication and a fresh-process scene archive. Exercise both current canvas backends and warmed active/idle intervals; report software-native, physical-device, foreign-platform and human screen-reader acceptance separately.

### Rejected alternatives

AccessKit C 0.23.1 was evaluated against its pinned C/Unix adapter sources. Its Unix backend follows the desktop accessibility-enabled state and the C API exposes no per-application force-enable option. The current host has that desktop state disabled. Changing global desktop preferences or adding an ineffective always-on project switch is not an acceptable implementation of the application policy. The direct GIO adapter can publish this application's tree without changing that preference; native discovery and actions must be verified independently.
