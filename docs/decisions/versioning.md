# Electron2D versioning decisions

Last updated: 2026-10-06

This bounded document owns product versioning and compatibility milestones. Use [the decision index](index.md) for other architectural domains; class pages describe the version metadata actually exposed by the runtime.

Decisions in this log: [0096](#adr-0096).

<a id="adr-0096"></a>
## ADR 0096: Use semantic product versions with separate runtime and editor milestones

Last updated: 2026-10-06

- Status: Accepted; the current unnumbered alpha suffix was selected by the user on 2026-10-06.
- Scope: Electron2D product releases, public compatibility, development channels and runtime/editor milestones.
- Preserves: [0004: Runtime product boundary](product.md#adr-0004), [0021: Platform verification](product.md#adr-0021), [0027: Editor/game boundary](product.md#adr-0027) and [0090: Agent-native workflows](agent-native.md#adr-0090).

### Context

The engine needs a version that communicates compatibility and distinguishes ongoing runtime development, a stable complete runtime API and a delivered editor. The runtime and its separately shipped editor share one public engine contract, while native dependencies have their own package versions. The SDK's default assembly metadata is not a product release decision.

### Decision

Electron2D uses [Semantic Versioning 2.0.0](https://semver.org/), in the form `MAJOR.MINOR.PATCH`, with one product version for the runtime and the editor delivered with that release. The editor remains a separate executable consumer; a shared release number does not merge its code or dependencies into `Electron2D.dll`.

| Product milestone | Version and acceptance boundary |
| --- | --- |
| Current runtime development | `0.1.0-alpha` is the accepted development version; the public API is not yet stable. |
| Complete stable runtime API | `1.0.0`, after the entire accepted runtime scope is implemented, behaviorally verified, documented and stabilized. Accepted exclusions remain excluded; editor-only obligations remain tracked for the editor milestone. |
| Complete editor with compatible runtime | The next `1.x.0` release, for example `1.1.0` if it directly follows `1.0.0`. The editor must execute its accepted visual and programmatic workflows through the public runtime API. |
| Incompatible public contract | The next major release, for example `2.0.0` after the `1.x` series. Delivering an editor alone does not require a major increment. |

The public compatibility contract includes documented public/protected API, observable behavior and supported saved project/resource data. After `1.0.0`:

- Increment `PATCH` for backward-compatible bug fixes.
- Increment `MINOR` for backward-compatible functionality or public deprecations, and reset `PATCH` to zero.
- Increment `MAJOR` for incompatible contract changes, including changes requiring consumer code changes or an explicit migration of previously supported saved data; reset `MINOR` and `PATCH` to zero. A transparent compatible loader upgrade does not itself require a major increment.

During `0.x` development, new functionality or incompatible changes increment `MINOR`; compatible fixes increment `PATCH`. Describe incompatible changes explicitly. A minor version is not a percentage of API completion, and individual implementation commits do not each require a product release.

Prerelease candidates use `alpha`, `beta` and `rc` channels before removing the suffix for a stable release; a numeric candidate identifier is optional. The current development channel is unnumbered `alpha`. Alpha permits incomplete work and evolving contracts; beta has the planned release scope implemented and undergoing stabilization; a release candidate has passed the required release checks with no known blocking defect. A new published candidate must have a distinct product version, channel or candidate identifier. Build metadata such as `+<source-revision>` may identify a build but does not change version precedence or replace a unique published package version. Never replace the contents of an already published version.

Runtime API completion is judged against the accepted product scope and its coverage obligations, not declaration counts alone. The runtime milestone does not claim an implemented editor, full external-reference parity or verification on every target. Release verification follows the active platform gate in ADR 0021; the editor milestone additionally requires the executable authoring and observation workflows in ADR 0090. Missing capabilities and unverified platforms remain explicit.

### Platform package versions and minimum engine

The package family is `Electron2D` plus `Electron2D.{Platform}` under ADR 0012, matching the user's chosen naming model. A platform package's version identifies its minimum supported Electron2D product version, including the complete prerelease suffix. For example, `Electron2D.Linux` `0.1.0-alpha` requires `Electron2D` `0.1.0-alpha` or later; `Electron2D.Windows` `1.4.2` requires Electron2D `1.4.2` or later. The generated NuGet dependency and native manifest record the minimum. Restore must reject an explicitly selected older engine.

Use matching product/platform release versions by default. The minimum dependency is a lower bound, not proof of compatibility with every future native ABI or product release. A different combination requires verified compatibility. A changed published native payload requires a new product patch or prerelease candidate and newly supported baseline; never replace the contents of an existing version. Internal SDL, ICU, FAudio, FreeType, HarfBuzz and OpenSSL versions remain pinned implementation details rather than platform package version numbers. Serialized file schemas retain their independent technical versions and loader compatibility rules.

### Current implementation and verification boundary

The shared source [`tools/native-package.props`](../../tools/native-package.props) assigns `Electron2DVersion` `0.1.0-alpha`. The managed runtime and first-party consumers use this value; platform packages use it as their version and minimum-engine dependency. Numeric assembly/file versions are `0.1.0.0`, and informational metadata includes the prerelease label and supplied source revision. [`EngineVersionInfo`](../classes/EngineVersionInfo.md) reports the loaded assembly's actual metadata.

Local package/consumer checks verify this delivery boundary. Publication, complete RID CI and target-native execution remain separate gates under ADR 0021. Product metadata does not establish a stable runtime or delivered editor.

### Consequences

- Consumers can distinguish compatibility changes from product milestones and development channels.
- Runtime `1.0.0` can precede the editor; compatible editor delivery belongs in the `1.x` series.
- The first stable release establishes compatibility obligations for subsequent releases.
- Packaging, schema evolution and platform verification retain their existing owners and independent evidence.

### Rejected alternatives

- Calendar versions: they identify release time but do not encode the chosen API compatibility contract.
- Automatically reserve `2.0.0` for editor completion: the editor is additional compatible functionality unless its implementation actually changes the public contract incompatibly.
- Derive the product version from API coverage percentages or native package versions: these measure different contracts and cannot establish release stability.
- Treat the SDK's default `1.0.0` as a stable product release: the runtime scope is still incomplete and no such compatibility commitment has been accepted.
