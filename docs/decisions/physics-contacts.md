# Contact solver decisions

Last updated: 2026-10-09

<a id="adr-0098"></a>
## ADR 0098: World contact solver settings and shape correction

- Status: Accepted
- Scope: Shape custom solver bias, contact correction and world solver iteration policy
- Depends on: [0054](physics.md#adr-0054), [0063](physics.md#adr-0063), [0069](physics.md#adr-0069), [0087](physics-joints.md#adr-0087), [0089](physics-activity.md#adr-0089)

### Decision

- Expose Shape.CustomSolverBias (default zero) and typed SpaceGet/SetContactDefaultBias
  and SpaceGet/SetContactMaxAllowedPenetration. Shape zero inherits the world's bias;
  one nonzero shape overrides it, and two nonzero shape biases use their arithmetic
  mean. Bias is a finite fraction in [0,1]. Allowed penetration is a finite nonnegative
  distance in scene units within the backend range. It is correction slack, not a
  collision filter, query margin, bounce coefficient or contact reporting threshold.
- Sample Physics2DDefaultContactBias=0.8 and Physics2DContactMaxAllowedPenetration=0.3
  project values with feature overrides at world creation. Existing worlds retain
  their values. Follow existing typed space operations instead of numeric selectors.
  Owner/phase validation and invalid/equal writes preserve the existing contract.
- Treat bias as the nominal outer-tick penetration correction fraction. Distribute
  it across actual substep/CCD durations with `1 - (1 - bias)^(interval / tick)` so
  extra intervals do not repeatedly apply a whole tick's correction. CCD can clip
  already prepared correction velocity before re-solving, within numerical tolerance. Solvers may
  defer convergence for coupled constraints and retain their finite correction speed
  limits. Physical normal/friction impulses remain distinct from correction; reported
  impulse totals must still satisfy actual velocity/momentum changes.
- Shape policy edits retain fixture/shape identity, geometric revisions, mass profiles,
  contact events and one-way history. Publish a separate policy epoch before Changed
  callbacks; lazy backend preparation must observe it even if a subscriber throws.
  Wake affected bodies/contact neighbours without rebuilding geometry. Resource copies,
  storage and every built-in shape preserve inherited bias state.
- Expose SpaceGet/SetSolverIterations and Physics2DSolverIterations (default 16).
  Capture the positive integer project setting with feature overrides at world creation;
  typed integer access replaces the numeric selector. The pinned [setting registration](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/physics_2d/physics_server_2d.cpp#L810) specifies a minimum of one and permits greater counts. Reject nonpositive values before
  mutation. Do not silently clamp large positive requests. The count controls complete
  contact and joint sweeps per scheduled substep, independently of elapsed time and
  substep count. CPU uses that many correction-enabled and relaxation sweeps; resident
  GPU uses that many physical/position constraint sweeps with its existing independent
  algorithms. Preserve force, correction and CCD budgets rather than multiplying them
  by the count. Changed counts wake dynamics; equal writes preserve sleep and timers.
- CPU stage descriptors are reused across sweeps. Their storage must not grow with
  iteration count. A 48-bit phase ordinal plus 16-bit stage index replaces the former
  16-bit phase protocol, retaining failure cancellation and work stealing. This is an
  internal scheduler change, not a public threading guarantee. Large iteration counts
  intentionally cost more work; device submission failures retain fail-closed behavior.
- CPU scalar/SIMD and legacy GPU-stage constraint preparation consume the same mixed
  bias and slack. Independent GPU contacts consume resident per-shape policy and world
  settings; no host pose mirror or CPU contact calculation is introduced. Public
  independent-GPU world binding remains an open requirement.

### Remaining work and verification

Contact recycling/max-separation, collision priority,
renderer debug drawing, backend extensions and public GPU binding remain separate
open capabilities. Acceptance requires executable correction, lifecycle, copy/storage,
material/impulse and warmed allocation checks on CPU and independent GPU, including
legacy stage preservation; documentation records actual measurements after those checks.
