# Text patterns component

Last updated: 2026-10-21

Core owns [RegEx](../classes/RegEx.md), [RegExMatch](../classes/RegExMatch.md) and internal [RegExProgram](../classes/RegExProgram.md). [ADR 0112](../decisions/text-patterns.md#adr-0112) selects the complete PCRE2-32 interpreter profile. The existing private text library statically embeds unmodified PCRE2 10.47, source f454e231fe5006dd7ff8f4693fd2b8eb94333429. Six private C exports expose compilation, metadata, matching, substitution, errors and release. No global PCRE2 installation, video initialization or executable-memory JIT is required. Original source fingerprints, prefixed private symbols, build receipts and BSD notices follow the existing native packaging path.

## Behavior and ownership

Compile releases the old program, records the attempted pattern and either publishes the new program or throws ArgumentException with code-point diagnostics. Null validation happens first. Clear releases the program while retaining the last attempted pattern. Uncompiled group count/names are empty observations; search/replacement requires a compiled program. A private SafeHandle releases forgotten programs. One owner serializes compile, queries, searches, replacement and disposal; independent owners execute concurrently.

All region offsets/ends and capture bounds count Unicode code points. A UTF-16 surrogate pair is one position; unmatched units remain data in the default non-UTF profile. Explicit PCRE2 UTF/UCP directives impose native validation and character-class rules. Embedded NUL uses explicit lengths. A region end truncates matching input without changing its original start anchor; the result retains the complete original subject. An offset beyond the search region has no match, while substitution rejects it as an invalid argument.

Mixed named/unnamed captures use source-order numbering. GetNames returns distinct names in native table order. A result's Names contains only participating names; the first participating group wins for a duplicated name. Results retain the immutable subject and copied positions/name mappings, materialize capture text only on query, and remain valid after the program is replaced, cleared or disposed. Strings returns a fresh array. Missing groups return empty strings or -1.

SearchAll advances by one code point after an empty match, including the final empty position. Sub uses native first/global replacement grammar, including numbered/named groups, whole-match aliases, literal dollar and unset/unknown groups becoming empty. It preserves the suffix after end. Native global empty substitution can differ from the repeated-search sequence because PCRE2 also tries a nonempty alternative at the same position.

## Budgets and failures

This is blocking/allocating text work outside frame hot paths. Pattern, subject, replacement and resulting text are bounded to 16,777,216 code points. Native per-call contexts use a 32 MiB match-heap cap and normal PCRE2 work/depth limits. Auto-callouts enforce a five-second deadline, sampled every 4096 calls; substitution sizing retries share their remaining deadline. Native optimizations and one callout interval can delay observation. These are resource bounds, not a real-time scheduling guarantee.

Invalid compile/replacement/explicit UTF data raises ArgumentException. Invalid native regions raise the corresponding typed argument error; exhausted work/depth/heap or output budgets raise InvalidOperationException, allocation failure raises OutOfMemoryException, and deadline expiry raises RegexMatchTimeoutException. Resource failure is never converted into a successful no-match result. Failed searches leave the compiled program reusable. Older native bundles missing this ABI fail explicitly; rebuild target assets rather than falling back to another dialect.

## Verification

`ELECTRON2D_TEST_REGEX=1` exercises public dialect features, Unicode/NUL/malformed text, regions, group numbering/duplicates, replacement, native work/input/output budgets, shared/independent concurrency, retained results and forgotten owner collection. `tools/coverage/regex_oracle.py OUTPUT.json` independently compiles the original interpreter without the engine bridge and emits 297 vectors. Pass that file to the Release test runner with `--regex-oracle` to compare group metadata, all capture values/positions, first/global substitution and invalid inputs.

The separate public-only [RegEx.Consumer](../../tests/RegEx.Consumer/RegEx.Consumer.csproj) parses recursive game configuration and uses its captured values in actual Engine/Label output. Its managed/gpu/compatibility profiles distinguish parsing from rendered pixels. The same public parser also executes in a standalone Linux x64 NativeAOT application. Software Wayland/X11 execution remains distinct from hardware and foreign hosts. Electron2DRegexCheck checks native ABI bounds, buffer sizing, 512 ownership cycles and a short native deadline; the same target runs with AddressSanitizer/UndefinedBehaviorSanitizer and leak detection. Existing text/font consumers remain adjacent regressions because the private text library is shared. Package/private-symbol/source checks do not establish foreign execution, native allocation totals or release acceptance.
