# Electron2D localization decisions

Last updated: 2026-10-13

This bounded log owns the complete architectural records for localization. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0007](#adr-0007).

<a id="adr-0007"></a>
## ADR 0007: Use a typed process-wide translation service

Last updated: 2026-10-13

- Status: Accepted
- Scope: Runtime message translation

### Context

Godot `Object` provides per-object translation enablement, a domain, and `tr`/`tr_n`. Electron2D needs the same caller-facing capability without adding a Variant-based API, a catalog-file format, or a localization dependency before an asset system exists.

### Decision

- `TranslationServer` is a process-wide in-memory catalog selected by one `CultureInfo`. It owns a registry of `TranslationDomain` objects, including the main domain named `""`. Domains register borrowed `Translation : Resource` catalogs without owning their disposal. Resource edits become visible immediately; removal, clearing, or disposal stops their lookup. A removed domain remains usable independently; a disposed registered domain leaves the registry.
- Registered domain lookups and server lookups share direct entries, resource priority, locale override, and enablement. Standalone domains resolve their own resource catalogs. A domain override replaces the selected culture for that domain.
- Keys are culture, domain, context, and source text. At startup the typed project test locale overrides the current managed UI culture when nonempty; the typed fallback locale defaults to `en` and can be disabled with an empty value. Both settings are sampled at Engine startup. Callers may change `TranslationServer.Culture` during a run.
- Exact direct registrations and resource catalogs are checked for the selected locale and its parents. Direct registrations take priority within each exact locale; later resource registrations win ties. If no exact entry resolves, resource catalogs with a matching language compete by locale score. The configured fallback locale is then searched the same way, followed by the source text. Standalone domains also use the configured fallback. `CompareLocales` and `GetToolLocale` expose the supported score/selection behavior. .NET locale parsing is the current typed boundary; reference locale aliases, default script/country tables and exact score parity remain Partial.
- `ElectronObject` stores a translation-enabled flag and domain and exposes typed `Tr`/`TrN` methods.
- `Node` inherits its translation domain from its parent until an explicit assignment; `SetTranslationDomainInherited` restores this behavior. Its `AutoTranslateMode` resolves through the nearest ancestor, and `Atr`/`AtrN` apply that policy before ordinary object translation. A scene root samples the typed `root_node_auto_translate` project setting when its tree starts. Tree entry, mode changes and active inherited-domain changes deliver translation-change notifications.
- The permanent typed project root layout selector uses 0 application locale, 1 LTR, 2 RTL and 3 system UI locale. A separate forced RTL setting affects inherited and application/system locale modes, while explicit LTR/RTL remain authoritative. Each SceneTree samples both policies with feature overrides before root entry; detached nodes use current project values. Window and Control share domain-aware ancestry and root resolution. Local translation/layout notifications reflow descendants; global locale/catalog edits still require explicit scene notification.
- Missing messages return the source text.
- Each domain offers configurable singular-message pseudolocalization: accents, doubled vowels, fake bidirectional controls, placeholder preservation, override, expansion, prefix, and suffix. The main domain is exposed through server convenience members. Plural results are not pseudolocalized, matching the pinned reference behavior. Locale matching and Unicode transformations are tracked as Partial where exact reference parity is not yet established.
- Eleven typed built-in `ProjectSettings` definitions configure locale selection and main-domain pseudolocalization; a twelfth configures initial root-node automatic translation and two more configure root/forced layout direction. `Engine.Start` and `Engine.Run` sample the catalog settings at startup, while `SceneTree` samples the root mode at construction. `TranslationServer.ReloadPseudolocalization()` reapplies the eight transformation options but preserves the current runtime enabled flag; enabling or disabling during a run is an explicit server operation. Already constructed scene trees may have completed ready callbacks before a manual `Engine.Start`. Asset remaps and automatic translation-change notification on global locale changes remain deferred.
- Direct plural registrations receive `Func<long, string>`. Resource catalogs use copied plural-form lists and an optional typed `Func<long, int>` selector. Known locales use fixed language/region gettext rules when no caller selector is assigned; unknown multi-form rules fail explicitly. Locale assignment prepares the rule, duplication preserves the optional caller override and recomputes automatic state, and warm selection allocates no managed storage. Caller delegates remain borrowed and copied by reference.
- Catalog state is lock-protected; resource lookup and its overridable callbacks run outside the server lock. Direct plural selectors retain their existing locked-lookup contract. The global enabled flag is internal and uses volatile access. Applications control translation per object through `ElectronObject`.

### Consequences

- The current runtime supports deterministic direct and resource-backed translation lookup without `Variant` or another package. Typed resource copying preserves independent message containers.
- The locale catalog supplies copied language/script/country code arrays, English names, aliases, supported-script/regional defaults and gettext definitions. StandardizeLocale preserves pinned parsing/casing/modifiers without requiring a .NET culture entry; CompareLocales uses its default script/country identity. Runtime catalog Locale still uses the accepted CultureInfo role. Fixed typed rule selectors replace runtime expression evaluation. Four two-form predicates that incorrectly returned index one for singular are corrected against integer cardinal examples in CLDR48. This is a fixed gettext table, not arbitrary CLDR decimal-category support. General catalog-file import and full locale negotiation remain separate concerns. The numeric-input consumer now implements FormatNumber/ParseNumber/GetPercentSign using the pinned numeral-system table; this current formatting capability does not imply broader locale negotiation.
- Direct plural selectors run during locked lookup and should be short; resource selectors run after snapshotting state. Recursive selector logic remains the caller's responsibility.
- Global catalog state must be restored or cleared by isolated tests and applications that replace languages.

### Rejected alternatives

- Return `object`/`Variant` translations: rejected by ADR 0001 and unnecessary for string messages.
- Hard-code English singular/plural rules for every locale: rejected because that would be incorrect for many languages.
- Add a catalog library now: rejected because no asset format or deployment requirement has selected one.
