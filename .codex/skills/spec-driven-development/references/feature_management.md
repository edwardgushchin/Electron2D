# Feature Management Guide

This guide describes how to track, reorder, and manage features naturally while using Spec-Driven Development.

## Source of Truth

Spec Kit conventions:
- Each feature is identified by a slug like `<###-feature-name>`; in StellarDown the canonical folder lives under `.temp/spec-kit/<###-feature-name>/`
- The current active feature is usually inferred from the Git branch name (e.g., `003-keyboard-shortcuts`)

For StellarDown, the canonical storage location is `.temp/spec-kit/`; root `specs/` is only a compatibility mirror if some external tool requires it.

## Stages and Progress

| Stage | Progress | Evidence |
|------:|---------:|----------|
| Specified | 20% | `spec.md` exists |
| Planned | 40% | `plan.md` exists |
| Tasked | 60% | `tasks.md` exists |
| In Progress | 80% | implementation started (code changed) |
| Complete | 100% | implementation done + tests pass |

## Hybrid Reporting Approach

- After every SDD command, include a brief status line in the 10-point summary.
- Show the full dashboard only when asked (or when the user runs `/speckit.status`, if that alias exists in the host runtime).

### Brief Status Line Format

```text
📊 Feature Status: <feature> (<stage>) → Next: <next-feature>
   Progress: [●●○○○] <pct>% | Completed: <n> of <total> features | Dependencies: <dep> ✅/⏸️
```

## Status Computation (Recommended)

When asked to compute status:

1. Identify feature folder (branch name, `SPECIFY_FEATURE`, or user-provided feature)
2. Check artifact presence (`spec.md`, `plan.md`, `tasks.md`)
3. Check whether implementation has started (git diff touches non-spec files)
4. Check whether tests/build are green (if tooling available)

## Natural Language Operations

Detection patterns (examples):

- "Move <feature> before/after <other>" → reorder
- "Add <feature>" / "We need a feature for <X>" → add
- "Let's do <feature> first" → move to top priority
- "Skip <feature> for now" → defer
- "We finished <feature>" → mark complete
- "What features depend on <feature>?" → dependency tree
- "Show feature status" → dashboard

### Add a feature

Ask:
1. Priority (High/Medium/Low)
2. Dependencies (if any)
3. One-paragraph description

Then:
- Create a new feature entry in the upcoming list (or create a new `.temp/spec-kit/<###-name>/spec.md` skeleton)
- Update the dashboard output

Suggested questions:

```text
1) Priority? (High/Medium/Low)
2) Depends on anything? (list)
3) One-paragraph description?
```

### Reorder features

Workflow:
1. Show current order (numbered)
2. Propose new order
3. Confirm
4. Apply change to whatever feature list artifact you maintain

## Quick Feature Operations

### Move to top

If user says "Let's do X first", show current order, propose moving X to slot 1, confirm, then apply.

### Defer

If user says "Skip X for now", mark X as deferred/blocked with a reason and show updated dashboard.

### Remove (destructive)

If user says "Remove feature X":

1. Warn that this may delete the canonical feature folder (and any mirrored `specs/<feature>/`, if present).
2. Show dependencies impacted.
3. Ask for explicit confirmation.

If confirmed, remove feature from the feature list and optionally delete the spec folder.

### Dependency tree

Represent as:

```text
feature-a
├─ Depends on: feature-b ✅
└─ Blocks: feature-c ⏸️
```

### Can we start <feature>?

Show readiness check:

```text
📊 Can we start profile-management?
   Checking dependencies...
   ✅ user-authentication (complete)
   ✅ database-setup (complete)

   All dependencies satisfied. Ready to proceed.
```

### Circular dependency detection

If you detect a cycle, stop and ask the user to resolve it before proceeding.

Example output:

```text
⚠️ Circular dependency detected
   feature-a depends on feature-b
   feature-b depends on feature-c
   feature-c depends on feature-a
```

## Suggested Dashboard Output

When user requests `/speckit.status` (or the equivalent natural-language status request):

```text
📊 Project Feature Status Dashboard

🎯 CURRENT FEATURE
├─ <feature> (<stage> - <pct>%)
│  Blockers: <none|...>
│  Dependencies: <...>

✅ COMPLETED FEATURES
└─ ...

📋 UPCOMING FEATURES
└─ ...

⚠️ BLOCKED FEATURES
└─ ...
```

## Where to Store the Feature List (Recommended)

Pick ONE canonical index and keep it updated:

- `.temp/spec-kit/FEATURES.md`

If some external tool also needs `specs/FEATURES.md`, treat it as a mirror of the canonical `.temp/spec-kit/FEATURES.md`, not as a second editable source.

Keep it simple:

```text
1. 001-user-authentication (depends on: database-setup)
2. 002-profile-management (depends on: 001-user-authentication)
3. 003-admin-dashboard (depends on: 001-user-authentication)
```

If you do not have a feature index yet, generate it by listing `.temp/spec-kit/*/` directories (or a synced `specs/*/` mirror) sorted by numeric prefix.
