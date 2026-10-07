# Physics sandbox consumer

Last updated: 2026-10-07

[PhysicsSandbox](../../examples/PhysicsSandbox/README.md) is a separate desktop executable consuming only the public runtime. [SandboxWindow](../classes/SandboxWindow.md) owns the fixed 1152×800 native window, OptionButton, pause/step/reset controls, three story actions and labels. [PhysicsScene](../classes/PhysicsScene.md) owns one of twelve complete interactive stories. [TugBody](../classes/TugBody.md) supplies the custom-integrated ship. The runtime never references this example.

The visual reference supplies #25192B plum background, #F9F3EE text, #FD9ECA pink accents and #FCCCDD blush, #A93B71 berry and #F09776 apricot geometry. Both bundled IBM Plex Sans weights remain caller-owned until the window and all controls are disposed. Collision geometry and visual geometry use the same shape resources and effective indexed poses. No backend type, synthetic animation of physics or downloaded sprite is part of the consumer.

## Gameplay capability map

This map covers gameplay mechanism families. It is not a replacement for exact declaration coverage or the runtime conformance suites.

| Implemented mechanism | Exercised interaction |
| --- | --- |
| Dynamic/static contact response, gravity and four-substep integration | Warehouse's 72 crates settle; launched ball and shockwave dismantle the towers |
| Central and positioned forces/impulses, torque and persistent force | Mouse grab, right-click offset impulse, Q/E torque, C upward force |
| Sleep, lock rotation, static/kinematic freeze | Z, L, K and H on the selected body; sleeping tint and direct-state inspection |
| Remove/MakeStatic/KeepActive disabled participation | V cycles the selected body's actual ProcessMode/DisableMode |
| Mass, custom centre of mass and inertia | Tug cargo mass toggle and off-centre cargo; atelier compound creature |
| Material friction/bounce/absorbent contract | Marble shared material edits compare bounce, zero friction and absorption; warehouse has separate friction |
| Circle, rectangle, capsule and convex polygon | Crates, marbles, courier, ship and triangular delivery piece |
| Segment and concave segment collection | Marble ledge and paired-segment collection bowl |
| CollisionPolygon solid concave decomposition | Atelier's compound apricot body |
| Separation ray and one-way response | Courier's slope-aware feet and apricot one-way shelf |
| Manual owner groups and indexed live geometry | Atelier morph/disable buttons preserve owner and shape-slot identity and update visuals |
| Standalone Shape collision | Atelier cursor outline changes color when its shape intersects the creature's shape |
| Scene point/ray/shape queries, contact pairs/rest info and motion tests | Picking; radar cursor volume, contact markers, safe fractions and predicted body motion |
| Cached RayCast and ShapeCast with live mask/exclusions | Radar view and layer buttons; ghost obstacle action adds/removes body exceptions |
| Collision layers/masks and collision exceptions | Radar apricot/berry layers and ghost passage; directional sensor detection |
| Body/shape contact and sleep events | Warehouse flashes and real contact-event counter |
| Scene Area body monitoring | Marble delivery, four parcels and cargo dock |
| Point/directional gravity, falloff, priority and override modes | Garden's two stars, G mixing cycle and attract/repel action |
| Linear/angular damping, body/default combination | Garden's damping mist; bodies' ordinary damping; custom tug damping |
| Grounded/floating MoveAndSlide, contact classification and snap | Courier mode toggle, jumping/slopes/shelves; floating radar probe |
| Animatable platform targeting and character platform carry | Courier's moving/stopped lift |
| Pin angular limits and powered motor | Clockwork's pendulum and motor toggle |
| Groove finite guide and free rotation | Clockwork's constrained apricot slider |
| Damped spring, live coefficients and anchor leverage | Clockwork stiffness toggle and tug tow cable |
| Shared scene/server joint RID construction and clear | Tug cable disconnect/reconnect through PhysicsServer |
| Direct-body custom integration and contact solving | TugBody changes velocity/angular velocity in the post-solver hook while contacts remain native |
| Explicit server body/shape identity and ownership | Atelier's independently owned RID marbles and walls |
| Server Area directional callbacks and gravity fields | Atelier's outlined updraft region counts RID-only entries and modifies gravity |
| Global scene space versus independent space scheduling | Main SceneTree world and explicit atelier SpaceStep; both gated by pause/step |
| Queries while inactive, teardown/reentry and resource release | Pause permits inspection; every dropdown selection/reset destroys and recreates a complete story |
| Physics interpolation | Desktop entry enables the public interpolation setting |

## Adjustable worlds and games

Every story groups its native HSliders into World, Object and Scene tabs. World controls gravity, damping and time scale; Object controls mass, material response, gravity scale and damping. New/reset stories and population growth leave selection empty. Clicking empty field space clears it; the Object tab then shows a selection prompt without controls. One pink selection outline and a named label link the clicked body to the object header. Numeric rows include units, ranges and factory-value ticks. Unsupported object fields are disabled. The main scene slider displays a real scene-specific range/count. Smash adds a second slider for fragment count, alongside impact speed; the retained extra row is hidden in other scenes. The public Area field setters accept a space RID and update that world's defaults; local garden Areas still override/compose them. Atelier parameter edits update its independent world as well. The simulation CanvasLayer uses per-story framing while the interface stays unscaled. A ClipContents Control and counter-translated scene preserve physical world coordinates while clipping story and selection drawing to the 836×536 field. The bike camera follows its chassis, with a recognizable geometric rider/bike and finish flag. The inspector ends at the same height as the field; actions sit immediately underneath. Screen pointers convert through the canvas transform and field bounds reject clicks over the interface.

Material readouts show friction/bounce magnitudes; per-body edits retain the sign encoding rough/absorbent mixing rather than clearing that policy or clamping a rough tire's readout to zero.

Stress uses 64–1,024 actual circles with sleeping disabled, shared material and eight-vertex visual dots. A motorcycle couples a chassis and two driven wheels through groove guides and damped springs; ramps and a finish sensor provide its objective. Slingshot towers consist of ordinary timber bodies and blush targets; dragging/releasing changes projectile velocity, the ballistic preview uses current world gravity and the bird gravity scale (it approximates motion without damping), and target displacement/velocity drives scoring. Smash uses a 12 kg square to scatter up to 65,536 initially sleeping 0.0045 kg square fragments (9,600 by default) in zero gravity. Its launch/rebuild actions reuse bodies; resizing releases removed identities and rebuilds the wall, while a radial shockwave uses ordinary body impulses. A bounded arena keeps the cloud interactive and a counter records fragments displaced from their homes. These are executable consumer mechanics, not additional runtime APIs.

## Runtime flow and invariants

Input reaching a toolbar control stays in the GUI path; physics grabbing starts from unhandled stage input. Root input also observes pointer movement and releases an existing grab on any left release, including a release consumed by the toolbar. Focus loss cancels it. The force uses the selected attachment's inverse mass and point velocity; its magnitude is capped. A custom-integrated body receives an instantaneous impulse, because that body intentionally omits ordinary force integration. Frozen kinematic grabbing changes the target pose and relies on the real kinematic solver lane.

Parcel callbacks latch collection through visibility; sensing is disabled in the next fixed callback after overlap delivery returns. A re-entry cannot count twice.

The scene fixed callback sets its local space activity, applies controls/story updates and explicitly steps only its caller-owned independent world. Pausing leaves handles, pending state and queries live; skipped time is not accumulated. One-step requests are consumed once. Switching preserves the UI pause setting and constructs a fresh story. Each scene releases explicit body/Area/space RIDs before disposing borrowed shape/query resources. Resources are disposed after scene children detach; fonts/styles are released only after the controls disappear.

[Measured performance and allocation boundary](physics-sandbox-performance.md).

## Verification and limits

[PhysicsSandboxTests](../../tests/Electron2D.Tests/PhysicsSandboxTests.cs) supplies focused headless stories/actions, finite solver state, pause/step, physical grabbing, freeze and repeated scene/RID cleanup. Its native mode runs the actual Engine window and captures all twelve scene views, with real native input delivery for dropdown selection and toolbar controls. The profiler in PhysicsSandboxTests.Profile.cs records mean/p95 fixed-step and real-frame times plus managed bytes on the owner thread, with 1600/256 physics warmup/samples and 768/192 native warmup/samples per story. Repeated impulses and a moving cursor keep the measurement active. Each full frame and render pass is measured, including UI/scene drawing; any nonzero managed owner-thread allocation fails the gate. Construction, body additions, configuration edits, transitions and fresh native input-event construction are outside its prepared-work interval. Native hover is cleared before trial warmup; the moving query pointer is exercised through the scene API. This does not claim allocation-free native event construction or tooltip creation. Automatic marble feed recycles delivered pieces. Numeric readouts use stack formatting and glyph draws; queries fill retained buffers. Contact caps prepare storage; contacts resolve collider RIDs directly and empty integration dispatch skips redundant synchronization. Solver compaction and new contact/island handles reuse prepared instances and sleeping island buffers survive wakeup until world disposal. Renderer readback remains test-only. No consumer capture API or unified agent/project workflow is claimed.

The profile remains bounded by the owning [physics decisions](../decisions/physics.md). Missing public CCD modes, infinite boundary resources, virtual tile identities, input-picking signals and joint tuning do not become implemented through the example. Local Linux x64/Wayland GPU and compatibility runs passed all twelve scene captures and native dropdown/pause/step controls. The Debug build configuration passes headless checks; the deferred-mass regression is tracked in PhysicsMassProfileTests. Cross-platform execution, physical game feel, exhaustive overlap-specific behavior and human game-feel acceptance remain separate gates. Native/GPU allocation is unmeasured. Area audio routing is a cross-domain audio behavior rather than a sandbox physics objective.

Smash adds the twelfth story: a 9,600-fragment sleeping wall (64–65,536 configurable), one heavy block, launch/rebuild/shockwave actions and live speed/count controls. Fragment tint follows public sleep and linear/angular travel relative to size: muted while asleep, pink while awake/slow, apricot while fast. Geometry uses 20× world coordinates with 0.05 presentation scale, and parent/shape/material sharing is bounded to 128-body groups. One retained multiline-color batch renders square silhouettes with interpolation. The host permits at most one fixed interval per render frame; overload slows simulation rather than accumulating several catch-up intervals before rendering.
