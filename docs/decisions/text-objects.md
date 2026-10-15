# Standalone text objects

Last updated: 2026-10-15

<a id="adr-0107"></a>
## ADR 0107: Typed retained single-line text buffers

- Status: Accepted
- Scope: complete `TextLine : ElectronObject` and its inline key/metadata family; shared backend preparation for TextParagraph

### Decision

Expose every applicable pinned TextLine member through the current FreeType/HarfBuzz/TextLayout backend. Draw/DrawOutline receive a recording CanvasItem, following Font's accepted typed canvas substitution. Font resources remain borrowed; segments retain their exact font/size/language and typed metadata. AddString has the ordinary no-metadata overload and an inferred generic metadata overload; TryGetSpanMetadata and SpanCount make retained values usable without Variant/reflection. TextObjectKey is a caller-owned reference-identity token for every add/resize/has/rect/list position, including duplicate copies. No universal key/value dispatch is introduced.

GetRID is a stable borrowed identity of the owned full shaped-buffer and display-policy pair, registered weakly in the internal text owner; actual drawing resolves that identity. Disposal unregisters it and retires only this object's layout/neutral-font state. Future public TextServer buffer operations must use the same owner and complete their own executable slice; no public Instance or partial façade is added.

Preserve defaults, concatenated multi-size/language segments, all inline alignment/length/baseline policies, BIDI overrides, tabs, inferred direction, metrics, hit positions, trimming/ellipsis/justification and horizontal/vertical rendering. Size and width use the source's ceiled trimmed metrics; ascent/descent remain fractional. Inline rectangles and caret geometry refer to the complete shaped buffer, with the same display alignment offset. Keep full and display layouts independently retained to preserve that distinction. An empty buffer has zero geometry. Inline-only buffers use an internal zero-source Font owner instead of inventing an external font requirement. Multiple replacement scalars occupy one logical object and one advance.

The creating thread owns all access. Mutation/disposal/reentry is rejected while native/layout work is active. Font generations and retained multi-font read scopes invalidate/retry coherent shape state; disposed borrowed fonts fail explicitly. Authoring/settings/output arrays are cold; prepared metrics/hit/draw reuse workspaces. Text and object expansion are bounded to 1,048,576 Unicode scalars and 64 MiB UTF-16 text. Invalid missing glyph display and control display are independent real policies. Add/resize returns false only for duplicate/missing keys; an empty string is a successful no-op, while invalid typed inputs throw before publication. Explicit BIDI ranges must cover the current buffer contiguously; clear them before appending further content.

Duplicate copies every policy and authored span/object metadata, shares borrowed fonts/keys/payload values and owns a separate buffer/RID/workspace. Clear removes authored content/objects/BIDI contexts and keeps line policies/tabs. Typed descriptors expose live nonstored line settings; the transient layout/delegates are not serialized. Scene/file authoring consumers store their source data/font resources and reconstruct runtime text objects. Existing Label/Font/RichText consumers retain their defaults; backend additions are opt-in. TextParagraph remains a separate dependent complete slice. ADRs 0001/0003/0004/0005/0014/0021/0046/0051/0090 apply; native visual, independent package/NativeAOT, foreign-platform/native-allocation/performance/owner acceptance each require evidence.
