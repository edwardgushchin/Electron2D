# Agent-native development decisions

Last updated: 2026-10-01

This bounded document owns the product's agent-native development contract. Use [the decision index](index.md) for other architectural domains; implemented behavior remains documented in class, component and domain pages.

Decisions in this log: [0090](#adr-0090).

<a id="adr-0090"></a>
## ADR 0090: Make Electron2D agent-native

Last updated: 2026-10-01

- Status: Accepted by the user on 2026-10-01.
- Scope: Game and engine development workflows, public API/documentation, project and scene authoring, editor automation, batch execution and observable verification.
- Refines: [0004: Product scope](product.md#adr-0004) and [0027: Editor/game boundary](product.md#adr-0027).
- Preserves: [0001: Typed C#](product.md#adr-0001), [0014: Resource lifetime and hot-path allocation](resources.md#adr-0014), [0015: Host lifecycle](core-object-runtime.md#adr-0015), [0016: Scheduling](core-object-runtime.md#adr-0016), [0021: Platform verification](product.md#adr-0021), [0023: Typed packed scenes](scene.md#adr-0023), and [0028: Renderer capabilities](rendering.md#adr-0028).

### Context

Electron2D must be designed and optimized for collaboration with AI coding agents, including Codex, Claude and other clients. An agent needs to understand the applicable contract, modify a project, build it, run a scenario, observe the result, and correct failures. A compiling patch alone cannot establish game behavior or appearance.

The existing typed C# surface, scene lifecycle, documentation and executable checks provide foundations. The missing project authoring, file serialization, batch host and public visual observation capabilities must become executable product features with explicit dependencies. Their absence is not an architectural exclusion.

### Decision

**Electron2D is an agent-native 2D engine. The supported development workflow must allow a coding agent to create, inspect, modify, build, run and verify projects through documented programmatic operations, with a reviewable result for the human developer.** This is a product requirement, not a claim that the complete workflow is implemented today.

#### Shared authoring operations and product boundaries

- Project creation/opening, scene/resource authoring, validation, import, build, run and export operations must be usable without driving editor widgets. The command-line/batch entry point and visual editor share the same authoring operations, validation and persistence rules. Pure window/UI presentation remains specific to the visual frontend.
- Editor operations belong to the separately shipped editor application under ADR 0027. Development tools may be separate executable consumers under ADR 0004. Portable scene/resource loading, saving and execution belong to their runtime domains and remain part of `Electron2D.dll`. Runtime code never depends on an agent, editor or game assembly.
- Every first-party editor/tool/game consumes the public runtime API. Missing reusable capabilities must enter their owning runtime slices; agent integration grants no blanket internal access, reflection bypass, backend exposure or duplicate scene/physics/render implementation.
- Clients use a documented CLI and machine-readable request/result contracts. Provider-specific integrations may adapt those contracts; no particular agent, model provider, network service, SDK, daemon or connection protocol is required by this decision. Command spelling and transport versions are established by their first executable slices.
- C# authoring and scenario programs use ordinary compiled .NET projects and typed APIs. [ADR 0091](scripting.md#adr-0091) selects C# as the only scripting language and one future concrete Script resource, preserving ordinary Node execution and explicit missing binding/authoring capabilities. This decision does not introduce an interpreter or universal runtime invocation system.

#### Three distinct execution modes

| Mode | Required services and behavior | Verification output |
| --- | --- | --- |
| Authoring without editor UI | Project/scene/resource operations, serializers, import/build tools; scene data can be edited without activating gameplay callbacks or opening editor windows | Changed files, validation/build diagnostics and operation results |
| Headless simulation | Real scene lifecycle, input state/routing, fixed/process steps and applicable simulation services; no window or renderer is required | State observations, events, scenario assertions and exit status |
| Rendered batch | Applicable simulation plus an actual selected renderer, explicitly configured target dimensions and capture timing | Images or recordings, backend/capability identity and scenario results |

Headless simulation must not claim visual verification or return a fake successful capture. A hidden native window, software fallback and a GPU offscreen target are different capabilities. Rendered batch must use the actual renderer and preserve its documented precision/capability limits; independent offscreen targets require their own complete lifecycle, sampling and readback slice. Unsupported requests fail explicitly.

#### Runtime and scenario contract

- Batch execution shares ordinary lifecycle semantics: ownership, activation/Ready ordering, process/physics ordering, pause/time-scale policy, deferred work, input edges, exceptions, quit and teardown. It must not approximate these with a second game loop or enable a hidden owner thread. GUI/Window-dependent operations require an explicit headless viewport/host capability rather than assuming native services exist.
- The host provides an explicit timestep, step/frame budget and stopping conditions. Input is injected through the real state-and-delivery boundary before the specified step. Event-only viewport delivery is not equivalent to committing global Input state.
- Batch jobs define deadline/cancellation and isolate game execution so a non-returning user callback cannot indefinitely block the authoring frontend. The tooling host owns process/job control; this does not add a hidden game thread or change runtime callback semantics.
- Repeatability claims name the engine/build revision, project/resources, scenario, random seed, timestep, platform and backend. Fixed stepping does not by itself promise bit-identical results across platforms, native libraries or renderers.
- Each run returns an explicit result and meaningful exit code. The first tooling slice defines versioned structured diagnostics, a separate log channel, observations/assertions and artifact references; callback failure, exhausted execution budget or missing capability cannot produce a successful result. Application console output must not corrupt the machine-readable result.
- Capture has a defined frame/step and completion boundary and uses public runtime capabilities. Internal test readback does not establish a consumer-facing capture API. Debug observations are opt-in; reporting, serialization and transport stay outside measured engine hot paths unless allocation budgets are independently proven.

#### Typed files, diagnostics and discoverability

- Agent-native design preserves the full applicable API, strictly 2D scope, one-runtime-assembly boundary and typed C# adaptations. It does not authorize narrowing the contract, inert aliases, `Variant`, general `object`/`dynamic` payloads or string-based runtime `Get/Set/Call`.
- Project/scene/resource files must be inspectable and versioned, produce stable focused diffs, and preserve type identity, hierarchy, ownership, typed stored properties, node/resource references and shared resource identity. The concrete format, codecs, type/factory registration and migration rules are specified in the first serializer slice under ADR 0023. No extension or JSON scene schema is selected here.
- CLI and UI use the same serializer/loader. File writes must avoid losing the previous saved data on failure; recovery and validation boundaries are tested. This does not change the existing clear-on-attempt `PackedScene.Pack` contract. A saved result must load correctly in a fresh process rather than relying on in-memory factories or caches.
- Structured transport/file values map to operation-specific typed models and approved property codecs. Their serialized field names do not create an untyped runtime value or arbitrary member invocation boundary.
- API discovery and documentation must provide compact, version-matched access to signatures, defaults, units, coordinate spaces, lifecycle/ownership, ordering, errors, executable examples and backend limits. Generated declarations and coverage states remain distinct from semantic verification; unknown/unverified behavior must not appear as supported.
- Diagnostics identify the operation and affected project file, scene/node/resource, source location or backend stage where available. Stable diagnostic identities and structured fields accompany readable messages. Inspection and evidence must remain useful to both the agent and the human reviewer.

### Delivery and acceptance

Implement connected executable slices, retaining the current capability-first selection and [maintenance contract](../maintaining.md). The dependency order is:

1. A batch runner for a compiled C# scene factory/project, with real lifecycle, fixed stepping, input, bounded completion and a structured report. A scene-file loader is not required to prove this first loop.
2. A concrete typed project/scene/resource file format, loader/saver and common authoring operations, including fresh-process reconstruction and failure recovery.
3. Public viewport image/capture capabilities on the implemented renderer, followed by independently verified offscreen rendering. Capture preserves backend limitations and does not rely on test-only internals.
4. A visual editor using the same proven authoring operations. Its UI remains self-hosted through Electron2D and does not establish a competing runtime model.

The first complete agent workflow must create/open a real project, make a focused change, build, run a reproducible scenario and return observations, diagnostics and relevant artifacts. It must exercise invalid input, callback/build/runtime failure and cleanup as well as success. Acceptance scenarios and changes to those scenarios remain reviewable. Human assessment of game feel and visual quality is separate from automated correctness.

Measure verified task success, repair iterations, elapsed time, required context, manual interventions, diff size and regressions using stated agent/model/tool versions and fixed tasks. A provider adapter, screenshot, synthetic benchmark or successful build alone does not prove the complete agent-native workflow.

### Current implementation and verification boundary

This change records architecture only; it adds no CLI command, editor executable, serializer, capture API or runtime behavior. Existing MainLoop/Engine manual scheduling, typed Input delivery and in-memory PackedScene are foundations. Engine.Run(Window) still owns a native window and real-time clock. ResourceLoader supports image textures and font files; scene-file loading/saving is absent. Renderer readback is internal to tests, while public Viewport.GetTexture/ViewportTexture and independent offscreen output remain missing. Existing domain-specific development tools do not constitute the unified project workflow.

Missing capabilities remain gaps with exact triggers in [coverage](../coverage/index.md). Platform claims follow ADR 0021; zero-allocation claims follow ADR 0014. Documentation/link/decision checks verify this record only, not an implemented agent workflow or improved agent performance.

### Consequences

- New runtime and authoring capabilities must be discoverable, executable and observable through the relevant supported tooling path as that path is implemented; absent prerequisites stay explicit.
- CLI and editor behavior have one semantic authority, and user review receives changes plus evidence of the exercised contract.
- Headless and visual verification remain separate capabilities, preserving honest backend and platform reporting.
- The design improves development by human programmers as well as coding agents, while retaining the runtime's typed and performance boundaries.

### Rejected alternatives

- GUI-only authoring requiring an agent to locate and operate widgets for ordinary project/scene changes: it leaves programmatic operation and repeatable validation undefined.
- Separate agent-only engine semantics or a privileged private API: it bypasses the ordinary game contract and permits tools to succeed where consumers cannot.
- Universal dynamic runtime dispatch or a mandatory embedded AI provider: neither is required for compiled typed authoring and both change existing product boundaries.
- Treating no-render execution, compilation or software-only screenshots as proof of GPU/visual behavior: these verify different contracts.
- Publishing placeholder tooling or claiming agent-native completion from this ADR: capability acceptance requires the executable end-to-end workflow above.

### Research references

The reviewed command-line/resource workflow informs the execution-mode distinction; it does not change Electron2D's API correspondence or select another engine's file format.

- [Command-line execution and batch import/export](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html)
- [Scene packing and resource persistence](https://docs.godotengine.org/en/stable/classes/class_packedscene.html)
- [Headless display and rendering limits](https://docs.godotengine.org/en/stable/classes/class_displayserver.html)
