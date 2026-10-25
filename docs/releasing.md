# Releasing Electron2D

Last updated: 2026-10-25

This is the maintainer runbook for future releases. Product versions follow
[ADR 0096](decisions/versioning.md#adr-0096); platform acceptance follows
[ADR 0021](decisions/product.md#adr-0021). [Maintaining](maintaining.md),
[native delivery](native-packaging.md) and [platform verification](platform-verification.md)
retain their implementation, package and execution checks. This runbook changes
the order and reuse of release work, not those acceptance boundaries.

The required pipeline is: prepare the complete candidate, build its packages,
qualify those packages, promote the same files, verify public delivery. The
source implements artifact promotion and CI publication;
the [automation status](#automation-status-and-transition) distinguishes local
verification from the still-required first hosted run. See [development CI](development-ci.md)
for the fast loop and complete qualification triggers.

## Prepare before freezing the candidate

- Keep the existing platform CI healthy during ordinary development. Diagnose
  compiler, shader, native ABI and allocation failures with focused reproducers
  on the affected platform before starting another complete release run.
- Choose the version and scope under ADR 0096. Update shared metadata,
  compatibility/migration notes and release notes before final qualification.
- Audit every subsystem against the current `docs/domains/` and
  `docs/components/` pages. Review graphics, scene/animation, physics, GUI,
  audio/video, input, navigation, resources/I/O, localization, networking and
  tooling. Update the feature summaries, installation instructions and relevant
  limitations in all five README locales: `README.md`, `README.ru.md`,
  `README.es.md`, `README.pt-BR.md` and `README.zh-CN.md`. A badge/version edit
  does not complete this review. Describe implemented capabilities and link
  their boundaries; do not claim whole-subsystem completion from API counts.
- Finish XML/class/component/domain/coverage changes and prepare the Wiki from
  the candidate assembly. Follow the existing pre-commit checks in `AGENTS.md`;
  keep generated Wiki output and release receipts outside engine commits.
- Use a clean, isolated checkout. Preserve unrelated dirty work. Record the
  full candidate SHA, version, intended tag and required checks before running
  qualification. All release jobs must check out that SHA; a moving `main` or
  a successful run for another revision cannot identify the candidate.

## Build once and qualify

1. Produce the managed package and all seven platform packages from the
   candidate with the pinned SDK/workloads/toolchains. Include every managed
   framework and the browser runtime asset described in native delivery.
2. Reuse existing native/shader caches only with their matching source and
   toolchain identity. Native cache hits still require source receipts, binary
   hashes, architecture, loader/export and package/license audits. Native
   source/build-recipe changes invalidate the relevant cache. Do not add an
   unconditional publication rebuild to an already qualified package set.
3. Run the configured Build/Tests matrix once on the final candidate, together
   with the applicable native/rendered checks required by the active platform
   gate. A focused pass is diagnostic evidence, not a replacement for the final
   matrix. Keep physical-device, physical audio, hardware performance and owner
   acceptance limits explicit; software-rendered CI does not establish them.
4. Exercise the actual release `.nupkg` files through fresh package-only
   consumers, including the applicable target profiles. Retain empty-cache
   restore, selected-RID/native manifest checks, compiler-blocked desktop
   consumers, self-contained publish and native/browser execution checks.
   Successful source or ProjectReference builds alone do not qualify newly
   packed binaries. Store the package hashes alongside their consumer results.
5. Freeze the successful package files and evidence together. Record repository,
   candidate SHA, workflow run ID and attempt, artifact IDs, package identities,
   versions, sizes and SHA-256 values. Record toolchain/source fingerprints and
   the exact check outcomes and limits. Retain the artifacts through publication
   and public verification; export receipts to durable release assets before
   temporary Actions artifacts expire.

A change to source, version, workflow, packaging or toolchain creates a new
candidate and requires the affected checks and final qualification again. Finish
release documentation beforehand. Save test receipts as artifacts instead of
creating a post-test documentation commit that moves the candidate. Do not use
`[skip ci]` to make an untested release SHA appear qualified.

## Promote the qualified files

- The publishing job must accept an explicit successful qualification run and
  artifact IDs, verify their repository, trusted workflow/ref, candidate SHA and
  manifest, and reject missing, expired, ambiguous or hash-mismatched input.
  Never select the latest successful run by name or consume untrusted fork
  artifacts. [Cross-run artifact download](https://github.com/actions/download-artifact#download-artifacts-from-other-workflow-runs-or-repositories)
  supports explicit run/artifact selection; downloading alone does not establish
  that the selected packages passed qualification.
- Publish those exact eight packages using the existing NuGet Trusted Publishing
  policy. Preserve signatures, notices and source/binary audits. The promotion
  job must not rebuild or repack. A replacement payload requires a distinct
  version and renewed qualification under ADR 0096.
- Create the GitHub draft for the tag resolving to the qualified SHA. Upload the
  qualified packages, other declared assets and checksums directly from CI,
  avoiding a download/upload round trip through a maintainer workstation.
  Separate independent asset transfer and NuGet public-feed checks so they can
  overlap; keep the draft unpublished until every completion check passes.
- Use one resumable sequence with explicit candidate/run/artifact inputs and
  retained per-step receipts. Reuse existing workflows and validators before
  adding release helpers; do not create a new set of one-off observer scripts
  for each release.

## Verify public delivery and finish

- Wait for every expected package/version to appear in the public NuGet feed.
  Use a deadline and bounded polling; an upload response is not public indexing.
- Download all published packages and compare every original entry with the
  qualified payload. Account explicitly for NuGet's repository signature;
  retain original/public whole-file hashes and signature verification results.
  Do not require equal whole-file hashes after repository signing.
- Restore the exact published package graph with an empty cache and only the
  public feed, then execute the applicable public package consumers. Record
  restore/package graph evidence separately from target execution.
- Verify the tag SHA and the complete GitHub asset inventory, sizes and hashes.
  Publish the draft only after the required checks and public delivery pass.
  Report the release URL, version, SHA, verification scope and remaining limits.
- Publish the generated Wiki only from the qualified assembly/XML after checking
  its clone diff under `AGENTS.md`. Record Wiki publication separately; a local
  generated candidate is not a published Wiki.

## Resume after a failure

| Failure | Continuation |
| --- | --- |
| Runner setup, network or other infrastructure failure at the same SHA | Preserve the original failure and rerun only failed/specific jobs and their required dependents. Verify the combined final status and exact artifact identities. |
| Runtime, shader, package or test defect | Reproduce on the affected platform, fix the cause, then qualify the new candidate. Do not weaken the check to obtain a green release. |
| Missing/expired or mismatched package artifact | Stop promotion. Produce and qualify a new artifact set; do not silently rebuild inside publication. |
| Partial NuGet publication or uncertain upload response | Query the exact identities/versions and compare public payloads first. Publish only missing qualified files. A mismatched existing version blocks the release and requires a new version; never overwrite or blindly skip a conflict. |
| NuGet indexing delay | Continue public-feed verification from its receipt without rebuilding, repacking or uploading again. A deadline leaves publication incomplete. |
| Interrupted GitHub asset upload | Reconcile server names, sizes and SHA-256 against the expected inventory; continue only missing files. Stop on a mismatch before replacement. |

[GitHub reruns](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/re-run-workflows-and-jobs)
retain the original SHA/ref. For example, `gh run rerun RUN_ID --failed` reuses
that run; it does not test a later fix. Read the original logs before deciding
whether a same-SHA retry is justified, and keep failed attempts visible.

## Automation status and transition

As of 2026-10-25, the workflow source implements the following pipeline. Local
regressions and a hosted GitHub run establish different evidence; the first
hosted execution is required after pushing this implementation.

| Area | Implemented workflow source | Verification/remaining boundary |
| --- | --- | --- |
| Development feedback | Independent fast build/API/Wiki/shared public contracts; documentation-only and draft changes avoid the native matrix. | No native or platform claim from a fast result. Unknown changes default to full qualification. |
| Qualification | Complete managed/platform production and configured Build/Tests matrices; test consumers select exact candidate packages with isolated restore. | Native, rendered, device and hardware scope remains as documented per profile. |
| Caches | Existing native receipts/checks remain; managed production reuses the pinned shader tool cache. | Manual `clean` and weekly clean qualification bypass native binary caches. |
| Promotion | Explicit successful trusted main CI run, one artifact ID, candidate SHA, source receipts and package hashes; no rebuild/repack. | Only full successful runs produce `release-qualified`; expired artifacts require qualification again. |
| Public delivery | Reconciled partial NuGet pushes, indexing/payload/signature checks, fresh public-only graph restore and CI-hosted GitHub draft/assets/checksums. | A published package requires actual successful execution; source/local tests alone do not establish availability. |
| Merge policy | Stable `Development` summary reflects planned scope. | Remote main was unprotected at inspection; server policy activation and latest ready-PR qualification must be verified separately. |

For a release whose final SHA has only documentation changes, explicitly run CI
with `profile=full`. A fast-only run cannot qualify a release. Supply that full
run ID as `qualification_run` to **Publish Electron2D packages**, along with the
owner's NuGet profile name. The workflow checks out the qualified revision,
reconciles public package bytes before retrying and publishes the GitHub draft
only after verification. It never replaces an immutable mismatched package or
asset. Complete release notes and verify Wiki publication separately.

Keep this status synchronized with workflow changes and retain the hosted run
URLs and actual outcomes. Do not describe local validation as a completed
cross-platform run, remote merge-policy enforcement or a published release.

Record UTC start/end times for qualification, packaging, publication, indexing
and asset transfer, including retries. The target for a ready candidate is one
full qualification cycle plus promotion/public verification. Measure the result
before claiming a speedup; keep runner queue and infrastructure delays distinct
from build/test time. Speed does not waive release checks or acceptance limits.
