# Electron2D localization decisions

Last updated: 2026-09-24

This bounded log owns the complete architectural records for localization. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0007](#adr-0007).

<a id="adr-0007"></a>
## ADR 0007: Use a typed process-wide translation service

Last updated: 2026-09-24

- Status: Accepted
- Scope: Runtime message translation

### Context

Godot `Object` provides per-object translation enablement, a domain, and `tr`/`tr_n`. Electron2D needs the same caller-facing capability without adding a Variant-based API, a catalog-file format, or a localization dependency before an asset system exists.

### Decision

- `TranslationServer` is a process-wide in-memory catalog selected by one `CultureInfo`. It owns a registry of `TranslationDomain` objects, including the main domain named `""`. Domains register borrowed `Translation : Resource` catalogs without owning their disposal. Resource edits become visible immediately; removal, clearing, or disposal stops their lookup. A removed domain remains usable independently; a disposed registered domain leaves the registry.
- Registered domain lookups and server lookups share direct entries, resource priority, locale override, and enablement. Standalone domains resolve their own resource catalogs. A domain override replaces the selected culture for that domain.
- Keys are culture, domain, context, and source text; lookup falls back through parent cultures to invariant culture.
- Existing direct registrations take priority within a locale. Resource catalogs follow in reverse registration order; every locale is checked before moving to its parent.
- `ElectronObject` stores a translation-enabled flag and domain and exposes typed `Tr`/`TrN` methods.
- Missing messages return the source text.
- Each domain offers configurable singular-message pseudolocalization: accents, doubled vowels, fake bidirectional controls, placeholder preservation, override, expansion, prefix, and suffix. The main domain is exposed through server convenience members. Plural results are not pseudolocalized, matching the pinned reference behavior. Locale matching and Unicode transformations are tracked as Partial where exact reference parity is not yet established.
- Nine typed built-in `ProjectSettings` definitions configure main-domain pseudolocalization. `Engine.Start` and `Engine.Run` sample their active values at startup. `TranslationServer.ReloadPseudolocalization()` reapplies the eight transformation options but preserves the current runtime enabled flag; enabling or disabling during a run is an explicit server operation. Already constructed scene trees may have completed ready callbacks before a manual `Engine.Start`. Asset remaps and scene translation-change notification are deferred until those integrations exist.
- Direct plural registrations receive `Func<long, string>`. Resource catalogs use copied plural-form lists and a typed `Func<long, int>` selector; English catalogs use the source English fallback when no selector is assigned. Non-English catalogs with multiple forms require a selector and fail explicitly without one. This projects the plural-rule capability without exposing a string-expression evaluator; built-in rules for other locales and the reference textual override remain coverage gaps.
- Catalog state is lock-protected; resource lookup and its overridable callbacks run outside the server lock. Direct plural selectors retain their existing locked-lookup contract. The global enabled flag is internal and uses volatile access. Applications control translation per object through `ElectronObject`.

### Consequences

- The current runtime supports deterministic direct and resource-backed translation lookup without `Variant` or another package. Typed resource copying preserves independent message containers.
- Catalog loading, locale negotiation, formatting, and CLDR rules remain separate future concerns.
- Direct plural selectors run during locked lookup and should be short; resource selectors run after snapshotting state. Recursive selector logic remains the caller's responsibility.
- Global catalog state must be restored or cleared by isolated tests and applications that replace languages.

### Rejected alternatives

- Return `object`/`Variant` translations: rejected by ADR 0001 and unnecessary for string messages.
- Hard-code English singular/plural rules for every locale: rejected because that would be incorrect for many languages.
- Add a catalog library now: rejected because no asset format or deployment requirement has selected one.
