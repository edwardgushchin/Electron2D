# Navigation source geometry and baking

Last updated: 2026-10-10

## Executable scope

[ADR 0097](../decisions/navigation.md#adr-0097) owns the planar producer connected to existing map iterations. NavigationPolygon owns copied outlines, complete typed selection/partition/clearance/bounds settings and immutable authored/baked geometry. NavigationMeshSourceGeometryData owns copied traversable/obstruction contours and immutable projected entries. Resource duplication and explicit compiled file factories preserve these values; old geometry version-one files remain readable. NavigationProjectedObstruction replaces projected dictionaries with typed copied vertices and Carve.

The installed internal Clipper2 2.0.1 backend performs NonZero union/difference at two decimal places, Miter erosion, carve after erosion, rectangle and border clipping, and hole-preserving triangulation. ConvexPartition greedily merges adjacent triangles only when their union remains simple/convex; Triangulate emits triangles. Output index ordering is backend-specific. Existing graph routing consumes actual produced polygons, not an independent baking-only representation. Generated CellSize metadata must match a consuming map before transactional topology publication; manually authored geometry retains its independent map raster contract.

Finite/count/Clipper range checks reject before replacing published mesh data. Source arrays are immutable snapshots for baking; edits after submission affect later work. Entirely absent traversable input preserves prior geometry; present input clipped/eroded to nothing publishes empty geometry. Preparation failures retain previous mesh data; Changed/completion failures may follow committed state. First rectangle clipping retains fractional/large finite coordinates instead of integer truncation.

## Scene source collection

ParseSourceGeometryData clears and collects root-local source state on the scene owner. Detached root traversal also supports authoring; group modes require an attached tree. Root traversal includes the selected root and descendants; GroupsWithChildren includes grouped roots and descendants; GroupsExplicit includes only grouped nodes. Nested groups can select the same node more than once. Same-destination recursive parsing rejects; a callback failure can leave partial source data and does not change the target mesh.

Built-in collection consumes Polygon contours with actual Offset, separate mesh triangle/strip faces, the visible MultiMesh prefix with per-instance transforms, mask-filtered StaticBody shape owners and owner transforms, tile collision polygons with actual cell transforms/runtime data, and bake-enabled NavigationObstacle discs/contours. Circle contours have twelve sides. Segment/world-boundary/ray shapes contribute no finite area. Tile navigation layers provide traversable outlines, with stored convex faces used when no outlines were authored, independently of collider parsing mode. Mesh faces are separate consistently wound triangles rather than the pinned concatenated-face contour.

Caller-owned parser RIDs register typed three-argument callbacks. Invocation snapshots permit registration changes/freeing during parsing; callbacks run outside the service gate. Every callback receives the selected target, live source destination and node; added points use root-local coordinates. Built-in parsing precedes user callbacks.

## Background and ownership

BakeFromSourceGeometryData completes on the caller. The async operation prepares immutable input on WorkerThreadPool and retires its task on publication/error. SceneTree.Defer publishes on the active or explicit tree owner; owner status polls and Synchronize also pump ready results. Without a tree, the consuming caller publishes. Other scene threads inspect pending state without publishing it. A target permits one pending bake; status is true through queued/prepared work and false before successful callbacks, which may start later work. Worker/closed-owner/disposed-target failures release pending state and surface on the publication pump.

NavigationRegion.BakeNavigationPolygon performs parsing and sync/background baking. An assigned map supplies compatible CellSize. BakeFinished follows geometry publication; existing Changed subscriptions update region topology for the next shared synchronization. Resource ownership and scene-instance file leases preserve fresh loaded input. Pending records are bounded to 128 targets, parser registrations to 1024, and point/node work to 1048576. Invalid submission does not leave a pending target.

The shared publication pump uses prepared bounded storage. Cold parsing/baking/queries/storage may allocate; warmed completed status and queued-frame pumping have separate zero-byte checks. A public-only [BakedRoutes example](../../examples/BakedRoutes/README.md) saves its scene and loads it in another process.

## Verification

NavigationBakingTests executes hole exclusion and obstacle-avoiding map paths, both partitions, fractional clip/border/clearance, ordinary/carve obstruction difference, empty/no-input behavior, failed preparation preservation, masks/root transforms/shape owners, mesh/MultiMesh/tile consumers, group traversal, obstacle bake policies, typed parser reentry, region completion, worker failure/closed owner and completion reentry/failure. Geometry/settings/outlines and source projected values reconstruct from files. 1024 warmed completed status queries allocate zero managed bytes.

Separate processes author and load the PackedScene room. Linux Wayland GPU and compatibility each render thirty frames while both worker lanes are held, then complete the queued bake on owner and verify red wall/green routed pixels at frame forty. After eight warm frames, twenty-one queued-bake active frames allocate zero managed owner bytes. These checks prove the exercised authoring/headless/rendered workflow, not general editor acceptance.

Native/OS allocation totals, foreign platforms, AOT, threaded browser bootstrap, exact cross-backend polygon order and human acceptance remain unverified. Async map-iteration construction and editor/debug producers retain their existing separate coverage triggers. The 3D-server NavigationMesh bridge getter and deprecated outline-conversion alias remain excluded under ADR 0004 with exact source evidence.
