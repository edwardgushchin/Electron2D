# ADR 0021: Cross-platform runtime target matrix

Last updated: 2026-09-21

## Status

Accepted.

## Context

The product is a 2D engine whose runtime must be usable on desktop and mobile systems. Earlier decisions fixed the 2D-only scope, the single Electron2D-owned assembly, and the future SDL3-CS host boundary, but did not define the complete supported target matrix. Without an explicit matrix, platform-specific code could accidentally turn a development host into the product boundary or make unverified portability claims.

## Decision

The Electron2D runtime targets exactly these operating-system families:

- Linux;
- Windows;
- macOS;
- Android;
- iOS.

One public runtime API and one set of documented semantics applies across all five targets. Platform-specific implementation belongs behind internal backends or host integration boundaries and must not create divergent public type sets. Portable .NET facilities and the selected low-level backend are preferred; direct native calls are used only for capabilities they cannot provide.

An unavailable platform capability must fail explicitly with the documented exception or capability result. Empty implementations, silent no-ops, and success results without performed work are prohibited.

Target intent, implemented code, successful compilation, application packaging, automated tests, and native-device verification are separate states. Documentation must state each state accurately. A feature may be described as cross-platform verified only after it has run on every applicable target.

The one-assembly rule continues to cover Electron2D-owned runtime code. Native libraries, approved external managed dependencies, platform application hosts, signing, and store packaging remain deployment concerns and are not implied to be contained in `Electron2D.dll`.

A future editor or development tool may intentionally support fewer host platforms when its document and ADR state that narrower boundary. That exception cannot leak a desktop-only requirement into the game runtime or its public data model.

## Current implementation boundary

The current project targets `net8.0` and its executable verification has run on Linux. Some file-system code contains macOS and Windows backends, but they have not been exercised on native hosts. There is no SDL application host, mobile target project, Android package, iOS application bundle, signing pipeline, or five-platform CI matrix. Therefore this ADR establishes the required product target, not a claim that distributable applications for all five platforms already exist.

## Consequences

- Every runtime domain and component must preserve the five-platform contract as it evolves.
- Platform-specific dependencies require an accepted packaging and lifecycle decision before integration.
- Platform support reports must distinguish compilation from native execution and packaging.
- Mobile lifecycle, permissions, storage, input, suspension, and graphics integration remain work for their owning future domains rather than placeholders in Core.
- A platform-specific optimization is acceptable only when a portable behaviorally equivalent path or an explicit documented capability boundary remains.

## Rejected alternatives

- Treat desktop support as the product and add mobile later: rejected because Android and iOS are first-class runtime targets.
- Maintain separate public APIs per operating system: rejected because game code needs one portable engine contract.
- Claim support from successful compilation alone: rejected because native dependencies, lifecycle, packaging, and device behavior remain unverified.
- Require the future editor to run on mobile: rejected because editor-host support is separate from runtime portability.

## Related decisions

- [0004: 2D API in one Electron2D-owned assembly](0004-2d-api-single-assembly.md)
- [0012: External runtime dependencies and Box2D.NET](0012-external-runtime-dependencies.md)
- [0014: Managed Resource lifetime and realtime allocation](0014-managed-resource-lifetime.md)
- [0015: Main-loop lifecycle and host boundary](0015-main-loop-contract.md)
- [0016: Process-wide Engine runtime and host-driven scheduling](0016-engine-runtime.md)
- [0017: Source-tree module layout](0017-source-tree-layout.md)
- [0020: Typed file access and transformed-file containers](0020-file-access.md)
