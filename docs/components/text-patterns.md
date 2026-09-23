# Text patterns component

Last updated: 2026-09-23

## Scope and owned types

Core owns [RegEx](../classes/RegEx.md) and [RegExMatch](../classes/RegExMatch.md) for compiled text matching, capture inspection and replacement. Both use managed ElectronObject lifetime. Their implementation uses the .NET regex library already in the runtime; no native package, SDL backend, scene, asset loader or renderer is required.

## Flow and invariants

Compile atomically replaces the program; failure leaves no compiled program and preserves attempted pattern text for diagnostics. Search snapshots a compiled program before executing. Region bounds preserve the original start anchor and original subject positions; results retain copies of group values and bounds after the owner is cleared or disposed. Results are caller-owned disposable objects. Empty matches advance during repeated search. Regular-expression work allocates and may time out after five seconds, so it belongs outside real-time frame hot paths.

## Current implementation and limits

The public search, repeat-search, substitution, capture, name and lifecycle paths are executable and covered by [RegExTests](../../tests/Electron2D.Tests/RegExTests.cs). The current dialect is .NET, whereas the pinned reference uses PCRE2; non-BMP indexing, some pattern and replacement forms, duplicate names and mixed-group numbering can differ. The exact outstanding per-member work is in [RegEx](../coverage/classes/RegEx.md) and [RegExMatch](../coverage/classes/RegExMatch.md) coverage. Managed tests passed on Linux/.NET 8; other runtime targets have not been verified.
