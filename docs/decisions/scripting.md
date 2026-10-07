# C# scripting decisions

Last updated: 2026-10-07

This bounded document owns the C# execution model and script-resource identity. [The decision index](index.md) routes other work; implemented behavior remains in class/component/domain documents and [coverage](../coverage/index.md).

Decisions in this log: [0091](#adr-0091).

<a id="adr-0091"></a>
## ADR 0091: Use compiled C# nodes and one concrete Script resource

Last updated: 2026-10-07

- Status: Accepted by the user on 2026-10-01.
- Scope: C# gameplay code, script resource naming/roles, project/editor integration and reference API adaptation.
- Refines: [0001: Typed C#](product.md#adr-0001), [0004: Product/API boundary](product.md#adr-0004), and [0090: Agent-native workflow](agent-native.md#adr-0090).
- Preserves: [0005: Typed properties](core-object-runtime.md#adr-0005), [0013: Resource ownership](resources.md#adr-0013), [0014: Allocation](resources.md#adr-0014), [0023: Packed-scene factories/storage](scene.md#adr-0023), and [0027: Editor/game boundary](product.md#adr-0027).

### Context

Electron2D already executes user C# classes through the ordinary Node lifecycle and typed APIs. A class such as `Player : CharacterBody` can hold per-player state and override frame callbacks. Its code is compiled with the game project and references the runtime assembly.

The reference engine separates Script and CSharpScript resources and binds managed script objects to native scene objects. Electron2D has managed scene objects and selects C# as its only scripting language. A language-independent base resource plus one CSharpScript subclass has no independent role in this product. Conversely, choosing ordinary C# nodes does not implement script assets, editor discovery, file persistence or live script attachment.

### Decision

#### Execution and project integration

- C# is the sole scripting language. GDScript and additional language providers/interpreters are outside this decision. Gameplay instances are ordinary typed Node subclasses, including the existing Entity, physics and Control branches; use their established callbacks, input, events and ownership.
- Compile project `.cs` files through the ordinary .NET project/build pipeline. Game code belongs to the game/consumer assembly, not to `Electron2D.dll`; a DLL per script is not required. This does not add a second per-frame dispatcher, Behaviour/component hierarchy or arbitrary string-based runtime invocation.
- Extend the existing typed scene mechanisms with a stable project type/script identity, exact construction factory, source/type association and explicit stored-property schema. Reuse `CreateSceneInstanceFactory` and `PropertyDescriptor<TOwner, TValue>`; file reconstruction must create the actual user type and restore its typed properties, references and ownership.
- Editor and CLI operations share registration, validation, serialization and build diagnostics under ADR 0090. Automated registration may use build-time generated typed code once its real contract is implemented; this decision does not require a generator framework, runtime reflection scanner or new SDK dependency in advance.

#### One resource identity

- When an executable loader, scene authoring or editor consumer needs code as an asset, implement one **concrete `Script : Resource`** for C#. Do not introduce an abstract language-independent Script base, a separate CSharpScript production type, or a compatibility alias.
- Both reference `Script` and `CSharpScript` map to this single Electron2D `Script`. Keep every applicable inherited/own capability, including typed instance creation, source/type information, properties/methods/events/constants, defaults and diagnostics. The merge adapts type identity/inheritance; it does not authorize missing members or false implementation states.
- The Script resource describes shared source, compiled-type/build association, metadata and typed instance creation. Mutable state of a particular player or other scene object remains in its Node instance. Resource sharing, duplication and disposal follow ADR 0013; script-source lifetime and compiled-type availability must be specified and tested by the first executable resource slice.
- Compiled C# node execution does not require a Script wrapper per frame. Do not add an empty resource solely to reserve its name or hide Node execution behind an otherwise unused resource API. Introduce the resource with an actual loader/editor/authoring path and positive, failure, lifetime and fresh-process reconstruction evidence.
- A scene's serialized type/script identity resolves to registered typed construction and property codecs. Serialized names are data addresses, not a universal `Get/Set/Call`, `Variant`, general `object` or `dynamic` value system. The concrete file format, registry schema, public signatures and build/error policy belong to their executable slices.

#### Compiled source asset contract

- Register one primary `.cs` document per project Node/Resource type with its stable portable type ID, static exact construction factory and explicit typed property descriptors. Abstract project bases have metadata registration without a factory. Reuse ResourceFileTypes construction/storage rather than a second object system. Registration belongs to the host's explicit startup/build integration; project code remains in the consumer assembly.
- Prove the selected source/type association using matching compiled assembly/module identity and portable PDB type documents or method sequence points. Preserve compiler document checksums for every partial-type document. Loading checks all source documents against the compiled checksums and rejects missing, changed, unknown or incompatible associations before cache replacement. File-backed portable-symbol availability is a concrete prerequisite; no AOT/browser or source-free registration is inferred from desktop execution.
- Script.SourceCode is editable resource text; assigning or saving it does not change the loaded CLR type. Source saving is an atomic UTF-8 operation. Rebuild and restart supplies a new compiled class cohort; no Reload success or keep-state/hot migration is claimed from source refresh. Resource duplication/archive storage preserve source text and stable registered class identity. Constructed objects own their ordinary state/lifecycle and keep executing compiled callbacks after Script disposal; registration retains the loaded compiled types for the process lifetime.
- Typed New uses the exact registered factory or a direct caller-supplied C# constructor expression, preserving typed constructor arguments without variadic Variant invocation. Validate the actual constructed type and its user property schema, disposing a returned incompatible instance. Project metadata uses CLR method/event/type information and explicit typed descriptors; constant queries select an exact value type. A property-default query receives a typed descriptor and a compatible live owner context, retaining the lifetime of borrowed resource defaults. Metadata queries are explicit cold operations, not per-frame dispatch.
- ToolAttribute and GlobalClassAttribute describe compiled project class roles/names; they do not install an editor execution scheduler. RichTextLabel may instantiate a compatible registered Resource Script as its owned RichTextEffect and use the ordinary virtual effect hook. The original Script stays borrowed and is not consulted per glyph.

#### Live attachment and rebuild boundaries

- A CLR instance does not change its runtime type when a script is selected. A scene loader may construct a `Player` from the selected identity; it must not claim that an existing CharacterBody object became that CLR subtype while keeping the same managed instance.
- This decision does not settle live GetScript/SetScript semantics, script-instance ownership/identity, replacement, callbacks or failure rollback. Their applicable reference obligations remain in coverage. Before implementing live attachment, define a typed instance/lifetime contract that preserves the required behavior, or obtain an explicit bounded ADR adaptation; do not expose inert setters, silently replace identity or mark these operations Excluded because Node subclassing exists.
- Rebuild and restart is the initial project/scenario workflow. Hot reload and migration of live nodes, fields, subscriptions, pending work and compiled-type lifetimes require a separate executable contract and verification. No transparent assembly unloading or state preservation is claimed here.

### Delivery and acceptance

The first registration/persistence integration must create a user Node type, execute its real callbacks, retain its exact factory identity, save selected properties/references, load in a fresh process and run a scenario against the reconstructed state. It must reject unknown/incompatible identities and invalid stored values and verify construction/activation/disposal failure paths.

The Script resource enters a real code-as-asset consumer: source/compiled-type association, usable metadata and typed construction, with the applicable loader/editor/scene integration. Source changes, failed builds, disposal and missing/incompatible compiled types must have explicit results. Metadata checks alone do not prove runtime behavior or complete scripting API coverage.

### Current implementation and verification boundary

The compiled-source asset slice now provides a concrete Script resource, explicit project Node/Resource registrations, portable-PDB source association, typed metadata/defaults/factories, `.cs` loading/atomic saving and archive identity. It creates actual user classes through existing file factories and ordinary callbacks; RichTextLabel consumes compiled effect resources. Checks and platform limits are recorded in [the scripting component](../components/scripting.md).

Live GetScript/SetScript binding/replacement, implementation Reload and keep-state migration remain unimplemented obligations. RPC metadata requires the scene multiplayer/RPC contract. Deprecated instance_has is excluded with its pinned replacement evidence under ADR 0004. No alternate CSharpScript production type or per-frame wrapper is introduced.

### Consequences

- The product has one script resource name and one C# execution model, without a language-provider subclass that adds no capability.
- Game developers use the existing typed Node API; agent/editor workflows add discoverable source/type metadata and reproducible scene/build integration.
- Resource metadata is separate from per-instance gameplay state, and reference API accounting remains complete across the accepted merge.
- Live script reassignment and hot reload remain explicit work rather than accidental promises of the simpler class/factory model.

### Rejected alternatives

- Abstract Script plus its only CSharpScript subclass: the language-provider distinction is unnecessary for the selected single-language product.
- Empty Script/CSharpScript shells or a per-frame wrapper around existing nodes: these add no executable asset/authoring capability.
- A new general Behaviour hierarchy, interpreter or DLL for every source file: ordinary compiled C# nodes/projects cover the current execution need.
- Declaring all scripting implemented, excluding live attachment, or losing inherited members after merging names: type accounting and Node callbacks do not prove the remaining resource/binding contract.
