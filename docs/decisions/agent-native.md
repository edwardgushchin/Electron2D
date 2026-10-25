# Agent-native development decisions

Last updated: 2026-10-25

This bounded document owns the product's agent-native development contract. Use [the decision index](index.md) for other architectural domains; implemented behavior remains documented in class, component and domain pages.

Decisions in this log: [0090](#adr-0090).

<a id="adr-0090"></a>
## ADR 0090: Make Electron2D agent-native

Last updated: 2026-10-25

- Status: Accepted by the user on 2026-10-01; live project collaboration, in-editor agent control and replacement of the existing batch interface accepted on 2026-10-25.
- Scope: Game and engine development workflows, public API/documentation, project and scene authoring, editor automation, batch execution and observable verification.
- Refines: [0004: Product scope](product.md#adr-0004) and [0027: Editor/game boundary](product.md#adr-0027).
- Preserves: [0001: Typed C#](product.md#adr-0001), [0014: Resource lifetime and hot-path allocation](resources.md#adr-0014), [0015: Host lifecycle](core-object-runtime.md#adr-0015), [0016: Scheduling](core-object-runtime.md#adr-0016), [0021: Platform verification](product.md#adr-0021), [0023: Typed packed scenes](scene.md#adr-0023), and [0028: Renderer capabilities](rendering.md#adr-0028).

### Context

Electron2D must be designed and optimized for collaboration with AI coding agents, including Codex, Claude and other clients. An agent needs to understand the applicable contract, modify a project, build it, run a scenario, observe the result, and correct failures. A compiling patch alone cannot establish game behavior or appearance.

The existing typed C# surface, persistence, scene lifecycle, undo history, capture and executable checks provide foundations. Independent file edits and compiled batch runs do not share an editor's unsaved state. The development experience must support a human and an agent working in the same open project, with changes visible in the editor and agent control available inside its window.

### Decision

**Electron2D is an agent-native 2D engine. A live project session is the shared authority for human and agent authoring. The editor provides agent control, while editor UI, external agents and operation clients use the same project operations to create, inspect, modify, build, run and verify a project.** The editor remains usable without an agent. This is an accepted product contract, not a claim that the complete workflow is implemented today.

#### Shared authoring operations and product boundaries

- Project creation/opening, scene/resource/code authoring, validation, import, build, run and export operations must be usable without driving editor widgets. UI and programmatic clients enter the same session operations, validation and persistence rules. A session can run without editor UI; this is the same authoring model with a different host.
- Editor operations belong to the separately shipped editor application under ADR 0027. Development tools may be separate executable consumers under ADR 0004. Portable scene/resource loading, saving and execution belong to their runtime domains and remain part of `Electron2D.dll`. Runtime code never depends on an agent, editor or game assembly.
- The session and its operations are shared editor/tooling responsibilities, with no UI dependency in their executable core. Their assembly layout is established by the first implementation slice; the role names in this decision do not add public runtime types or mandate a new framework, daemon or assembly per service.
- Every first-party editor/tool/game consumes the public runtime API. Missing reusable capabilities must enter their owning runtime slices; agent integration grants no blanket internal access, reflection bypass, backend exposure or duplicate scene/physics/render implementation.
- Documented operation requests/results are independent of their transport. A CLI is an optional client of those operations, not the semantic authority or a separate authoring path. No particular agent, model provider, network service or SDK is required.
- The current `tools/Batch` commands, compiled scenario SDK and version-one request/report formats are not compatibility requirements for this architecture. The session/job implementation may replace and remove them together with their affected consumers and documentation. Reuse executable runtime capabilities and useful checks where they fit; do not add aliases, adapters or a parallel legacy workflow solely to retain this tool interface. This decision does not remove the existing tool by itself.
- C# authoring and scenario programs use ordinary compiled .NET projects and typed APIs. [ADR 0091](scripting.md#adr-0091) selects C# as the only scripting language and one future concrete Script resource, preserving ordinary Node execution and explicit missing binding/authoring capabilities. This decision does not introduce an interpreter or universal runtime invocation system.

The shared live-scene save operation is ResourceSaver.SaveScene under ADR 0013. It projects the authoring host's pre-save/capture/write/post-save lifecycle without requiring editor widgets or a runtime dependency on the editor assembly. Only this explicit authoring operation publishes Node's editor save notifications; ordinary Pack, PackForSave and resource Save retain their existing contracts. This reusable operation does not establish a unified project CLI, visual editor or complete agent workflow.

#### Session ownership and concurrent authoring

- One authoritative owner admits mutations for a project root. With an open editor, external operation clients attach to its session. Without an editor, an operation host can own the same session core. Discovery, owner lease, authentication, expiry and crash recovery must prevent two independent writers; an unreachable or ambiguous live owner must not silently cause a writable headless fallback.
- The session owns open documents, unsaved scene/resource/code state, stable document/object identities, selection context, persistence state, diagnostics and operation history. Reuse the accepted typed archive and public runtime capabilities; this decision does not select a replacement file format. File paths and node names alone cannot identify objects across rename/reparent operations.
- Human and agent changes are serialized through the session's owner thread. Network/model work and build/game processes cannot hold that thread while waiting. Inspecting a scene or editing its authored data must not implicitly activate gameplay callbacks.
- Mutations name their origin and expected document/object revision. Validate a bounded change group before applying it, publish its changes as one committed operation and return the resulting revisions. Reject stale overlapping edits with useful conflict information. A stale whole-document replacement must never overwrite a newer manual buffer. Unrelated editing can continue while the agent reasons or a job executes.
- Revision identities remain monotonic across undo/redo; the cursor-like `UndoRedo.Version` is not a concurrency token. Use [ADR 0105](undo-history.md#adr-0105) for the reusable history mechanism. Its callbacks are not automatic transactions: the session must establish its own validation, recovery and partial-failure boundaries before promising all-or-nothing authoring. Saving multiple files also needs an explicit recoverable publication contract; atomic replacement of one file does not establish group atomicity.
- An agent change group participates in the editor's normal history. Selective undo checks affected state against later human edits and reports a conflict when necessary; it cannot restore an old snapshot over unrelated or overlapping manual work. Cancellation stops pending work at a defined commit boundary and reports any already committed effects.
- Unsaved changes are visible to session clients and become dirty documents. Applying them does not implicitly save a human's buffers. External disk changes are reconciled against the persisted base and live revisions; clean documents can refresh, independent edits can merge when supported, and unresolved overlaps remain explicit conflicts. A file watcher is an ingress for these changes, not the session's authority.
- Committed authoring changes notify scene tree, inspector, resource/code views and viewport in the next editor dispatch cycle without a save/reopen cycle. Preserve unaffected selection and editor state. Immediate document/UI updates do not imply live CLR code replacement: [ADR 0091](scripting.md#adr-0091) initially uses build and restart; hot reload and state migration require their own executable contract.

#### Agent control and engine tools

There are two distinct integration boundaries:

| Boundary | Responsibility |
| --- | --- |
| Editor to agent executor | Start/resume a conversation, supply task/selection context, stream activity, handle user input and permission requests, interrupt work and retire the process/session. ACP is the preferred initial protocol candidate; a provider-specific adapter can map an executor's supported contract. |
| Agent to Electron2D operations | Discover and invoke typed project/scene/resource/code operations, query current state and diagnostics, submit jobs and retrieve results/artifacts. MCP is the interoperable tool boundary; local UI clients invoke the same operations without a transport round trip. |

ACP and MCP are complementary, not alternative implementations of project semantics. Negotiate capabilities and pin exercised protocol/executor versions. Neither protocol supplies scene revisions, conflict handling, undo or a security sandbox. The first actual integration must verify its authentication, buffer access, cancellation, failure and cleanup behavior; a manifest, fake executor or launched terminal alone is insufficient.

The editor owns its agent panel: task input, attached selection/context, conversation history, operation/change timeline, job progress, relevant diagnostics/images, interruption and safe undo. The executor can run in a separate local process while this panel is the user's complete interaction surface. Model calls, credentials, provider packages and agent lifecycle stay outside `Electron2D.dll`. Reuse a suitable executor's agent loop behind the adapter rather than binding the project model to one provider or constructing a general-purpose orchestration framework first.

Expose clear semantic tools backed by operation-specific typed models. Discover the full applicable operation surface progressively, with a compact active set, scoped search/inspection, bounded responses and actionable errors. Include names and relevant context alongside stable identities. Tool descriptions document prerequisites, effects and failure behavior; publish only executable capabilities, without successful placeholders. Transport field names do not authorize arbitrary runtime `Get/Set/Call`.

Start with one agent alongside the human. Permissions cover authorized task scope; routine reversible changes need not prompt for every call. Destructive/external effects follow explicit policy and remain attributable. Source code, tool output and imported assets are data, not authority to expand permissions. Direct agent/shell file writes cannot bypass unsaved-buffer conflict checks: route them through the session or reconcile them as external edits. Concurrent multi-agent mutation, remote collaboration and automatic conflict resolution require measured need and separate verified capabilities.

#### Snapshot jobs and observable results

Build, test, run and export jobs consume an immutable input snapshot that includes the applicable source/resource/settings closure and unsaved document/code overlays. Materializing it in temporary storage must not mark project buffers saved or advance their persisted revisions. A job must not silently test older disk content while the editor displays newer input.

Each job identifies its input snapshot, document/content revisions, build configuration and engine/project identities. Results and artifacts retain that identity and become visibly stale when relevant current inputs differ. A running job does not block further authoring. Job status distinguishes queued, running, cancelled, failed and completed; enqueue or process start is not successful verification. Deadline/cancellation and owned-process cleanup include inherited workers and failure recovery.

The job layer uses public scheduling, input, persistence and capture capabilities with the modes below. It is shared by editor and no-UI hosts; it does not preserve the old compiled batch API or introduce another game loop.

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
- Project/scene/resource files must be inspectable and versioned, produce stable focused diffs, and preserve type identity, hierarchy, ownership, typed stored properties, node/resource references and shared resource identity. The concrete format, codecs, type/factory registration and migration rules are specified in the first serializer slice under ADR 0023. ADR 0013 now selects the first binary typed archive version; its public typed metadata/state inspection and explicit schema document are available. Textual authoring/diff export remains a separate tooling capability.
- CLI and UI use the same serializer/loader. File writes must avoid losing the previous saved data on failure; recovery and validation boundaries are tested. This does not change the existing clear-on-attempt `PackedScene.Pack` contract. A saved result must load correctly in a fresh process rather than relying on in-memory factories or caches.
- Structured transport/file values map to operation-specific typed models and approved property codecs. Their serialized field names do not create an untyped runtime value or arbitrary member invocation boundary.
- API discovery and documentation must provide compact, version-matched access to signatures, defaults, units, coordinate spaces, lifecycle/ownership, ordering, errors, executable examples and backend limits. Generated declarations and coverage states remain distinct from semantic verification; unknown/unverified behavior must not appear as supported.
- Diagnostics identify the operation and affected project file, scene/node/resource, source location or backend stage where available. Stable diagnostic identities and structured fields accompany readable messages. Inspection and evidence must remain useful to both the agent and the human reviewer.

### Delivery and acceptance

Implement connected executable slices under the [maintenance contract](../maintaining.md). Start the session foundation with the next project-authoring slice, before project management, scene inspector or code editing acquires a private UI-only document model. Existing persistence, history, scheduling and capture are sufficient foundations to begin; completion of the entire API census, renderer feature backlog or all-platform matrix is not an entry gate. Missing capabilities needed by the selected slice still enter their owning runtime domains first.

| Stage and entry point | Deliverable and exit evidence |
| --- | --- |
| 1. Next project-authoring slice, before a functional editor project workflow | A no-UI session core with one owner, stable identities, unsaved scene/code documents, bounded typed operations, revisions, events, persistence/recovery and grouped history. Real tests cover independent owner processes, two operation clients, stale/conflicting edits, undo after manual work, external changes, failed apply/save and fresh-process reopen. No agent/model connection is needed to establish these deterministic guarantees. |
| 2. First functional editor project workflow, using stage 1 | Minimal open project, scene tree/inspector/code views and preview subscribe to the shared session; manual and programmatic changes appear together without forced save. Snapshot jobs exercise an unsaved edit, build, fresh game execution, actual capture, stale results, deadline/cancel and cleanup. Replace/remove the existing Batch controller/SDK/formats and affected consumers/docs in this job slice, without a compatibility bridge. Runtime entry points retain their separate owning contracts. |
| 3. First agent-enabled editor milestone, after stages 1–2 and before editor polish | A real local operations transport and one real agent adapter, preferably ACP where supported, connected to the editor's task/conversation panel. Verify selection context, unsaved reads/writes, meaningful progress, interruption, disconnect/reconnect, missing capability and executor failure. Run the co-development acceptance scenario below with the stated executor/model/tool versions; a fake adapter remains only a deterministic test fixture. |
| 4. After the first co-development milestone | Expand semantic tool coverage and context discovery, measure repair quality and regressions, then qualify installed distribution, other agents/platforms and larger workloads. Add parallel agent execution or hot reload only through their own justified and verified slices. |

The first bounded implementation target is one existing typed scene and one C# source buffer: open/inspect them, change an Entity transform through typed session operations, preserve an unsaved manual edit, reject an overlapping stale edit, undo safely, then save/reopen in a fresh process. Establish actual owner exclusion across processes. This proves stage 1's document/operation boundary before building a full project manager, plugin framework, language service or agent transport.

The minimum co-development acceptance scenario opens a real project, makes a human edit without saving, asks the agent from inside the editor to change another part, and shows both in the normal views. An overlapping stale edit must conflict without data loss. Undo/cancellation preserves later manual work. Build/run verifies the intended unsaved snapshot, returns state assertions and actual rendered evidence, and marks older evidence stale after another edit. Include invalid input, callback/build/runtime failure, process disconnect, recovery and cleanup. Repeat the shared operation path without editor UI. This scenario, not terminal launch or panel screenshots, establishes the first integrated capability.

The [coverage roadmap](../coverage/roadmap.md#live-project-and-agent-delivery) tracks this dependency order without promoting absent editor APIs. Human assessment of game feel and visual quality remains separate from automated correctness.

Measure verified task success, repair iterations, elapsed time, required context, manual interventions, diff size and regressions using stated agent/model/tool versions and fixed tasks. A provider adapter, screenshot, synthetic benchmark or successful build alone does not prove the complete agent-native workflow.

### Current implementation and verification boundary

The minimal [editor startup screen](../components/editor-startup.md) supplies a native branded window using the public runtime API; it has no project-authoring workflow. The [compiled project batch runner](../components/project-batch.md) currently creates/opens source-SDK projects, builds and launches fresh scenario processes, authors detached scenes, performs headless stepping and captures native images with version-one results. It uses public runtime APIs but has no live editor session or unsaved-buffer integration. It is existing implementation evidence, not the target operation boundary or a compatibility obligation.

Typed archives, ResourceSaver.SaveScene, public history/property descriptors, MainLoop/Engine scheduling and typed Input are runtime foundations. Public Viewport.GetTexture/ViewportTexture.GetImage, independent offscreen targets, RenderingServer.ForceDraw and the movie-writer lifecycle provide native output. Callback-first Engine overloads commit input before the first native step. [Low-processor rendering](../components/low-processor-rendering.md) preserves input/processing while skipping unchanged output; its RGB probe remains distinct from agent acceptance.

The live project session, shared editor views, revision/conflict integration, snapshot-backed session jobs, MCP operations transport, ACP/executor adapter and in-editor conversation/control are not implemented by this decision. Installed distribution, import/export, general scene text authoring, foreign-platform qualification and measured agent/human acceptance remain separate work. The archived predecessor's workspace models, in-process MCP adapter and panel harness inform the design; they are not imported code or proof of a current live integration.

Missing capabilities remain gaps with exact triggers in [coverage](../coverage/index.md). Platform claims follow ADR 0021; zero-allocation claims follow ADR 0014. The project-batch integration checks exercise create/open, a focused compiled edit, build, fresh-process authoring/simulation and actual rendered artifacts. Documentation/link/decision checks verify this record only; neither establishes improved agent performance or human visual acceptance.

### Consequences

- New runtime and authoring capabilities must be discoverable, executable and observable through the relevant supported tooling path as that path is implemented; absent prerequisites stay explicit.
- Human and agent authoring have one live semantic authority, and user review receives attributed changes plus evidence tied to the input actually exercised.
- Headless and visual verification remain separate capabilities, preserving honest backend and platform reporting.
- The design improves development by human programmers as well as coding agents, while retaining the runtime's typed and performance boundaries.

### Rejected alternatives

- GUI-only authoring requiring an agent to locate and operate widgets for ordinary project/scene changes: it leaves programmatic operation and repeatable validation undefined.
- Separate agent-only engine semantics or a privileged private API: it bypasses the ordinary game contract and permits tools to succeed where consumers cannot.
- Universal dynamic runtime dispatch or a mandatory embedded AI provider: neither is required for compiled typed authoring and both change existing product boundaries.
- Treating no-render execution, compilation or software-only screenshots as proof of GPU/visual behavior: these verify different contracts.
- Publishing placeholder tooling or claiming agent-native completion from this ADR: capability acceptance requires the executable end-to-end workflow above.
- Keeping a second disk/batch authoring authority or preserving the existing tool SDK solely for compatibility: either can diverge from the editor's unsaved project state.
- Starting with a general multi-agent framework, mandatory cloud account or a task board before a real co-development slice: these do not establish the shared authoring contract.

### Research references

Reviewed on 2026-10-25. These sources inform separation of application state, agent execution, tools and editor context. They do not supply Electron2D's transaction guarantees, select a mandatory provider or establish tested integration:

- [OpenAI: application-owned context/tools and a reusable agent executor](https://developers.openai.com/blog/codex-as-a-platform).
- [Anthropic: semantic tools, bounded results and evaluation on real tasks](https://www.anthropic.com/research/writing-tools-for-agents).
- [Anthropic: retrieve relevant context as needed](https://www.anthropic.com/research/effective-context-engineering-for-ai-agents).
- [Google: typed function calls and validation](https://ai.google.dev/gemini-api/docs/function-calling).
- [ACP: editor/agent control and complementary MCP services](https://agentclientprotocol.com/get-started/architecture), [client-backed unsaved text access](https://agentclientprotocol.com/protocol/v1/file-system).
- [Zed: external executors with an in-editor conversation surface](https://zed.dev/docs/ai/external-agents).
- [Gemini CLI: IDE context, native diffs and ACP](https://geminicli.com/docs/ide-integration/).
- [Unity: current CLI/MCP operation routing](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli). Transport choices can evolve independently of authoring semantics.
