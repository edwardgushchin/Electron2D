# Maintaining the Electron2D contract

Last updated: 2026-09-23

This guide describes the implementation and documentation checks used during code changes. It does not define product architecture. [The decision index](decisions/index.md) routes to the accepted ADRs, and the affected class, component, and domain pages describe current behavior. If a rule here conflicts with an accepted ADR, follow the ADR and correct this guide before implementing.

## Public API and coverage

Function, method and property names follow [ADR 0045](decisions/product.md#adr-0045): every acronym stays fully uppercase, including `PNG`, `JPG`, `GL`, `HLSL`, `GLSL`, `GPU`, `API`, `ID`, `UTF8`, and `FPS` (for example, `GetFPS` and `MaxFPS`). Ordinary words retain PascalCase. Existing mixed-case acronym spellings require migration with their callers, XML, class pages and coverage; this rule does not claim that all existing declarations have already been renamed.

Electron2D follows the current official Godot API for implemented 2D concepts where that contract fits accepted Electron2D decisions. Its public and protected API stays inside that semantic capability boundary under [ADR 0004](decisions/product.md#adr-0004). Compare types and inheritance, overloads, properties, events, notifications, constants and enum values, defaults, callbacks, units, coordinate spaces, state changes, lifecycle timing, ordering, errors, and threading. A matching name or compiling signature does not prove behavioral compatibility. Typed C# mappings preserve relevant behavior unless an accepted decision requires an adaptation. Backend convenience, implementation effort, and absence of a current caller alone do not justify narrowing the contract. An Electron2D-only declaration must identify the existing reference concept or library-host lifecycle it projects; otherwise keep it internal or in the executable consumer.

[The coverage index](coverage/index.md) owns the pinned upstream reference, full bidirectional declaration accounting, state vocabulary, blocked-prerequisite rules, completeness checks, regeneration commands, and implementation roadmap. Update affected rows in the same change as public API or behavior. Keep `inventory.md` as the map of implemented Electron2D types, not a second compatibility register. Treat unknown, unreviewed, missing, duplicate, stale, and unjustified extra rows as audit failures. Distinguish complete accounting from semantic compatibility.

`examples/` contains user-facing code for learning and building games. Example code and project files use only public Electron2D API, including bootstrap; implementation backends and their packages stay in the runtime project. Place API probes, injected events, failure fixtures, and conformance assertions in `tests/` or development tools, following [ADR 0027](decisions/product.md#adr-0027). Preserve vendored upstream formatting when updating its source; exclude `src/Vendor/SDL3-CS` from `dotnet format` while verifying Electron2D-owned code.

## Reference API correspondence

The accepted target preserves the complete applicable Godot API and behavior under all existing decisions, including the typed C#, strictly 2D and naming adaptations. [ADR 0008](decisions/scene.md#adr-0008) maps Godot `Node` to `Node` and `Node2D` to `Entity`, with the same applicable API. Keep `CanvasItem` and the separate spatial/UI branches. Audit each member at its proper declaring layer and propagate the renamed types through every API position. Any further divergence needs an accepted decision; missing implementation remains a gap.

## Complete implementation slice

When asked to implement a type or concept, deliver a production-ready vertical slice rather than a compiling skeleton:

1. Read the current owning ADR, existing class/component/domain documents, complete upstream class reference and inheritance chain, and relevant coverage rows. Inventory properties, overloads, events, notifications, callbacks, defaults, state transitions, ordering, ownership, errors, and threading before coding.
2. Classify each item as implemented now, adapted to typed C#, blocked by an exact missing domain/backend/integration or product decision, or permanently excluded by a named accepted decision. For a blocked item, name the trigger and whether it enters the prerequisite's first slice or a separately approved capability. Do not add empty methods, inert state, or false compatibility claims.
3. Implement all behavior in scope, including disposal, callback failure, re-entrancy, thread affinity, and edge cases. Audit foreseeable 2D sibling types under [ADR 0035](decisions/core-math.md#adr-0035); do not add speculative families or empty aliases. Before first public release, correct proven mistakes instead of retaining them solely for compatibility, with boundary tests and updated documentation; avoid unrelated source changes.
4. Check positive, negative, boundary, and failure paths, including exceptions from user callbacks where those callbacks can affect engine state. Compare the compiled public surface and executable semantics with the inventory in both directions, including overloads, defaults, event signatures, constants, and enum values.
5. Update source XML and all affected living documents. Fix known P0/P1/P2 defects in scope and run relevant formatting, build, executable, coverage, and documentation checks before the task's atomic commit. Report implemented behavior, adaptations, blocked work, permanent exclusions, checks, and verification limits separately.

## Living documentation

Every production domain, component, and class, struct, interface, enum, or delegate has a current document in `domains/`, `components/`, or `classes/`. Test-only helpers and generated files need no separate class page. Keep `inventory.md` links synchronized when production types are added, moved, renamed, or removed. Update each affected page's `Last updated` date when its semantic content changes. Never describe proposed or absent behavior as implemented.

Class pages are complete references for Electron2D's actual typed C# API. Use this order, omitting only inapplicable sections:

1. Type name, source/declaration metadata, linked `Inherits` and `Inherited By`, purpose, and detailed `Description` of ownership, lifecycle, ordering, units or coordinates, threading, and caveats.
2. `Tutorials` or `Examples`, with a minimal idiomatic C# example for every nontrivial type; compile it when practical and label any partial snippet or required context.
3. Summary tables for every applicable API category: constructors, properties, methods and overloads, events, enum types and values, constants, operators, indexers, delegates, and protected extension points. Show complete C# signatures, modifiers, types, and defaults.
4. Per-member descriptions with stable anchors under `Property Descriptions`, `Method Descriptions`, `Event Descriptions`, `Enumeration Descriptions`, `Constant Descriptions`, and equivalent applicable sections. Explain parameters, returns, units, ordering, side effects, ownership, exceptions, threading, lifecycle timing, caveats, and related members as applicable.
5. Lifecycle and state transitions, invariants and errors, dependencies and interactions, verified tests, limitations, and relevant decisions. Include inherited behavior that materially affects use. Put excluded or dependency-blocked upstream counterparts in coverage or limitation notes rather than implemented API tables.

Component pages state scope, owned types, runtime flow, dependencies, invariants, current implementation, exclusions, and verification. Domain pages state responsibility, component inventory, public surface, dependency direction, shared invariants, limitations, and relevant ADRs. Keep links navigable. Update affected documents in the same change as the code; revise an owning ADR and decision routing only if the architecture changes.

## Source XML documentation

Document every public production declaration and protected extension point, including namespaces where supported, generic parameters, enum values, delegates, constructors, fields, constants, events, operators, and indexers. Adapt the upstream semantic contract to current Electron2D behavior across the inheritance chain. Do not claim unsupported editor, renderer, input, physics, serialization, networking, or other behavior.

Use `<summary>` for a concise contract; `<param>` and `<typeparam>` for every parameter; `<returns>` or `<value>` for results; `<exception>` for intentionally exposed exceptions; `<remarks>` for lifecycle, ordering, threading, adaptations, and limits; and checked `<see cref="..."/>` and `<paramref name="..."/>` references. XML, Markdown, tests, implementation, and coverage must agree. Compile generated XML with missing or malformed documentation warnings treated as errors. Keep the external reference name out of production code, comments, XML, and all `README.md` files as required by the root instructions.
