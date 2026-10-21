# ResourceLocaleMap

Last updated: 2026-10-21

Source: [`ResourceLocaleMap.cs`](../../src/Core/IO/ResourceLocaleMap.cs)
Declaration: `internal sealed class ResourceLocaleMap`
Inherits: `System.Object`

Immutable grouped snapshot of typed locale candidates. ResourceLoader retains one snapshot per distinct effective record sequence; unrelated project-setting edits retain its identity. Synchronous and threaded ResourceLoadGraph instances capture that identity and the global culture name.

## Runtime flow and invariants

Records group by exact ordinal source path and retain input order. Resolve uses CompareLocales, ignores nonpositive scores, stops on the first exact score and selects the last tied nonexact score. The selected UID resolves through ResourceUID; a missing selected file returns the original. No chaining or catalog fallback occurs. Matches compares the complete ordered source/locale/target sequence. Map construction is cold; file existence checks and selection may allocate or perform I/O.

## Dependencies, verification and limits

Depends on ResourceLocaleRemap, TranslationServer scoring, ResourceUID and FileAccess. It owns no resources and requires no disposal. Its immutable contents may be read by workers; active scene refresh stays on the scene owner. ResourceLocales.Consumer exercises exact/tied/missing selection, stale culture/configuration rejection and unrelated setting identity. See [localized resources](../components/localized-resources.md), [ADR 0007](../decisions/localization.md#adr-0007) and [ADR 0013](../decisions/resources.md#adr-0013). This internal helper adds no public map API.
