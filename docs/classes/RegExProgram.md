# RegExProgram

Last updated: 2026-10-21

Internal sealed SafeHandle in [RegExProgram.cs](../../src/Core/String/RegExProgram.cs). Owns one private PCRE2 program, copied name metadata and the managed/native conversion boundary. Its nested Text captures code points and original UTF-16 boundaries while preserving unmatched units and NUL. RegEx serializes access; each native match/substitution owns and releases its context. Substitution retries retain the remaining deadline, and outputs are bounded before allocation. Captured results retain copied bounds and the original immutable subject, materializing text on demand without borrowing native memory. See [text patterns](../components/text-patterns.md) for budgets, failures, source provenance and verification limits.
