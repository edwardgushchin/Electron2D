# Text pattern decisions

Last updated: 2026-10-21

<a id="adr-0112"></a>
## ADR 0112: Private PCRE2 text patterns

Status: Accepted.

The existing RegEx/RegExMatch declarations expose a working .NET matcher, but that dialect cannot execute the applicable pinned patterns, duplicate-name rules, replacement grammar or code-point indexing. Those differences remain implementation gaps under ADR 0004, rather than an accepted substitute dialect.

Use unmodified PCRE2 10.47 interpreter sources from f454e231fe5006dd7ff8f4693fd2b8eb94333429, the pinned comparison's dependency. Build only the 32-bit code-unit/Unicode profile into the existing private text bridge on every native producer. Disable executable-memory JIT. Prefix every embedded interpreter symbol independently of application copies; export only six new engine C functions. Deliver its original BSD license through the existing notice mechanism and fingerprint its source closure and native receipts. No platform/global PCRE2 installation is required.

Keep the complete typed RegEx/RegExMatch family. Pattern/subject/replacement strings remain ordinary C# strings, but offsets, region ends and capture positions count Unicode code points, with surrogate pairs forming one position. Preserve unmatched UTF-16 units in the non-UTF interpreter profile, and allow explicit PCRE2 UTF/UCP directives to impose their own validation/class semantics. NUL uses explicit lengths. Native matching uses DUPNAMES; mixed captures follow source order and a duplicated name identifies the first participating group. Match results copy all values/positions and remain independent after program replacement/disposal. Clear releases the program while retaining the last attempted pattern text.

Use ordinary managed exceptions for invalid compilation, replacement and native failures. Substitution uses the pinned unset/unknown-group-to-empty behavior and retains the suffix outside the requested end. Repeated search advances one code point after an empty match, including a final empty match; substitution follows PCRE2's global replacement algorithm. A region offset does not move the original start anchor.

Serialize operations on one RegEx owner, allowing independent instances to run concurrently. A private SafeHandle releases the program even after forgotten managed disposal; the owned native matching context is per call and never escapes. Retain the five-second match/substitution deadline through native auto-callouts, with ordinary PCRE2 work/depth limits and a 32 MiB match-heap cap. Compile/match/substitution inputs and output use a documented 16,777,216-code-point bound. Resource/deadline failure must throw rather than silently report no match. Text matching is cold blocking/allocating work, outside prepared frame hot paths.

Verify direct native oracle vectors independently of the managed wrapper, dialect/group/replacement/Unicode/region edge cases, concurrency/lifetime, native budgets and source/package/private-symbol boundaries. Exercise a compiled public scene consumer using the existing Engine/Label path when claiming authoring/rendered integration. Source-native Linux results do not establish old bundle support, foreign execution, native allocation totals or release acceptance. ADRs 0001/0003/0004/0012/0014/0021/0030/0045/0090/0095 retain their existing boundaries.
