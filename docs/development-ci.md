# Development feedback and package qualification

Last updated: 2026-10-25

The [CI workflow](../.github/workflows/ci.yml) separates fast development feedback
from complete package qualification. This preserves the configured platform
matrix and the acceptance boundaries in [ADR 0021](decisions/product.md#adr-0021).
The workflow source is implemented; a successful GitHub run is still required
after pushing these changes. Local checks do not establish hosted execution or
hardware acceptance.

## When work runs

| Change or event | Fast feedback | Complete qualification |
| --- | --- | --- |
| Root/documentation Markdown only | Yes | No |
| Code in a draft PR | Yes | On marking ready, or explicit manual `full` |
| Code in a ready PR or on `main` | Yes | Yes |
| Unrecognized changes or unavailable base diff | Yes | Yes; never assume documentation-only |
| Manual `fast` | Yes | No; cannot produce publishable artifacts |
| Manual `full` | Yes | Yes, with validated native caches |
| Manual `clean`, or Monday 03:17 UTC | Yes | Yes, bypassing native binary caches |

The documentation-only allowlist covers root `.md` files and Markdown under
`docs/`. Package README files under `tools/`, licenses, coverage snapshots,
workflow changes and unknown paths retain complete qualification. Draft PRs
receive fast feedback while implementation is in progress; do not use a draft
result as evidence that platform execution passed.

A new revision cancels superseded CI work for the same ref. Ready-for-review and
converted-to-draft events refresh the plan. There is no new full-matrix local
pre-commit requirement. Measure runner queues and cached/cold durations before
claiming a speedup.

## Fast development loop

`Fast feedback` starts independently of native production. It checks pipeline
failure boundaries, existing CI contracts, the Release runtime, shared public contracts for typed data/settings/theme and
headless CPU physics without a platform package, compiled public API/coverage and Wiki generation. Existing shader tool caches avoid rebuilding
host compilers on every run. It does not certify native behavior or platforms.

For a local change, use the relevant existing feature selectors first, with
actual failure/cleanup and prepared active/idle allocation checks for changed
hot paths. The pipeline's own small checks are:

```bash
python3 -B tools/ci/test_pipeline.py
python3 -B tools/ci/test_checks.py
dotnet run --project tests/Electron2D.FastTests -c Release
```

Keep the existing commit checks in `AGENTS.md`. Every discovered release defect
needs a regression that fails on the broken implementation and runs in the
ordinary relevant profile. Retain untraced execution alongside diagnostic
profilers; do not substitute relaxed allocation budgets or compiler-only shader
checks for the native contract.

## Complete qualification

The reusable native workflow produces the final managed package alongside all
seven platform packages. Its desktop source/ProjectReference consumers remain;
the package consumers now use the supplied final managed package without
repacking it. The full RID Build matrix retains source compilation and analyzers.
The Tests matrix uses `Electron2DUsePackage=true` through the shared consumer
props, including indirect test consumers, and executes the same existing suites,
trimming/AOT and Android/Apple/browser profiles against the package set.

Candidate package source mapping and an isolated cache prevent a public package
with the same version from replacing the CI candidate. The complete managed
package contains desktop, Android, iOS, tvOS and browser assets. Native receipts,
exports, architecture, licenses and cache validation retain their existing checks.
Physical Apple profiles remain unsigned build-only; rendered and hardware checks
retain their independent scope.

Only a full successful run creates `release-qualified`: eight original packages
and `qualification.json` with repository, source SHA, run/attempt and per-file
identity, size and SHA-256. It is retained for 30 days. Missing, changed, incomplete
or expired artifacts require qualification again. `Development` summarizes the
planned scope; `Build` and `Tests` are skipped on fast-only runs and cannot be
presented as a complete matrix success.

## Main branch and publication

On 2026-10-25 the remote `main` had no branch protection or repository rulesets.
Remote CI sees only pushed revisions. A local merge does not establish hosted
verification; push focused feature work regularly when authorized under
`AGENTS.md`, rather than first exposing accumulated engine changes at release.
The source change alone does not enforce merge policy. After a hosted validation,
configure the stable `Development` status as required, protect updates through
ready PRs and prevent bypass of code qualification. Check the latest ready-PR
run, rather than an earlier draft success for the same SHA. This server-side
activation remains separate from local workflow validation.

The publishing workflow takes an explicit successful full CI run on trusted
`main`. It selects one `release-qualified` artifact by ID, checks out that run's
SHA and verifies the manifest/package bytes before obtaining the short-lived
NuGet credential. It neither builds nor repacks. Existing matching public
payloads are reconciled before retrying a partial push; mismatched immutable
versions fail. Public indexing, entry comparisons, repository signatures and a
fresh public-only restore of all eight identities are then checked. GitHub assets
are uploaded directly from CI into a reconciled draft with package, qualification,
public-verification and checksum files; a mismatch stops publication.

See [Releasing](releasing.md) for release notes, version selection, GitHub assets,
Wiki publication and the remaining manual acceptance steps. A fast-only CI result
cannot authorize publication.
