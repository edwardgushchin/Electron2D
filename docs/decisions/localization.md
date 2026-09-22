# Electron2D localization decisions

Last updated: 2026-09-22

This bounded log owns the complete architectural records for localization. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0007](#adr-0007).

<a id="adr-0007"></a>
## ADR 0007: Use a typed process-wide translation service

Last updated: 2026-09-22

- Status: Accepted
- Scope: Runtime message translation

### Context

Godot `Object` provides per-object translation enablement, a domain, and `tr`/`tr_n`. Electron2D needs the same caller-facing capability without adding a Variant-based API, a catalog-file format, or a localization dependency before an asset system exists.

### Decision

- `TranslationServer` is a process-wide in-memory catalog selected by one `CultureInfo`.
- Keys are culture, domain, context, and source text; lookup falls back through parent cultures to invariant culture.
- `ElectronObject` stores a translation-enabled flag and domain and exposes typed `Tr`/`TrN` methods.
- Missing messages return the source text.
- Plural registrations receive `Func<long, string>` so catalog owners provide language-specific rules rather than relying on an incomplete engine heuristic.
- Catalog access is lock-protected; the global enabled flag is internal and uses volatile access. Applications control translation per object through `ElectronObject`.

### Consequences

- The current runtime supports deterministic translation lookup without `Variant` or another package.
- Catalog loading, locale negotiation, formatting, and CLDR rules remain separate future concerns.
- Plural selectors run during locked lookup and should be short; recursive selector logic remains the caller's responsibility.
- Global catalog state must be restored or cleared by isolated tests and applications that replace languages.

### Rejected alternatives

- Return `object`/`Variant` translations: rejected by ADR 0001 and unnecessary for string messages.
- Hard-code English singular/plural rules for every locale: rejected because that would be incorrect for many languages.
- Add a catalog library now: rejected because no asset format or deployment requirement has selected one.
