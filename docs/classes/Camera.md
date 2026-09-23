# Camera

Last updated: 2026-09-23

- Declaration: `public partial class Camera : Entity`
- Sources: [Camera.cs](../../src/Scene/2D/Camera.cs), [Camera.Tracking.cs](../../src/Scene/2D/Camera.Tracking.cs)
- Inherits: [Entity](Entity.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#camera-tracking)

## Description

Follows a spatial node by changing the default canvas transform of its containing Viewport. It draws no geometry itself. The first enabled camera entering an unoccupied viewport becomes current. MakeCurrent switches explicitly; disabling/removing the current camera chooses the first enabled attached camera in current tree order. That replacement calculates its view at its next update. When none remains, CanvasTransform resets to identity. GlobalCanvasTransform is independent.

An enabled camera is not necessarily current. Hidden cameras still track. Internal idle/physics processing is independent of ProcessEnabled/PhysicsProcessEnabled, but follows ProcessMode and tree pause. All cameras receive their selected lane so a later switch has a current delta. Only the current camera changes the viewport. Without position smoothing, inherited transform notifications update the view when the scene delivers them, including in physics mode; the constructor enables NotifyTransformChanges. A transform setter alone queues this update. ForceUpdateTransform consumes a pending notification synchronously, while ForceUpdateScroll explicitly updates tracking regardless of pending notification state. Disabling that notification policy leaves frame-driven updates active.

The camera uses global position and rotation from Entity, including parent and TopLevel behavior. Its node scale/skew do not multiply Zoom; they can affect the inherited global position/rotation. Camera movement does not mutate other node transforms. Use GetTargetPosition for the drag/limit-adjusted destination, GetScreenCenterPosition for the rendered center and GetScreenRotation for the rendered angle; they are cached, not extra tracking updates.

A detached camera may be configured and packed. Activation requires a containing or custom viewport; a neutral Node-root tree has no fallback viewport and rejects camera activation with ordinary scene rollback. Actual membership hooks bind/unbind ownership even when user enter/exit callbacks fail. Manually delivering enter/exit notifications does not create/release ownership. CustomViewport is borrowed, runtime-only and currently restricted to an active viewport in the same tree. Null and non-Viewport Node values restore the default target.

Attached access is owner-thread; setters reject scene capture and disposed state. Nonfinite float/vector configuration fails before mutation. An overflowing tracking calculation preserves the last viewport matrix and cached tracking result; the setting or node transform that triggered the error remains committed. Correct that setting and force/update the camera to recover. No native resources are owned by Camera. Inherited scene disposal owns children, releases viewport selection and leaves borrowed targets alive.

## Example

Configure a root Window before the existing Engine.Run entry point:

```csharp
var window = new Window();
var camera = new Camera
{
    Name = "Camera",
    Position = new Vector2(100, 50),
    Zoom = new Vector2(2, 2),
    PositionSmoothingEnabled = true,
};
window.AddChild(camera);
// Add game entities to window; their logical transforms remain world coordinates.
```

## Enums

- [AnchorModeEnum](Camera.AnchorModeEnum.md): FixedTopLeft = 0, DragCenter = 1.
- [CameraProcessCallback](Camera.CameraProcessCallback.md): Physics = 0, Idle = 1.

## Constructors

| Declaration | Contract |
| --- | --- |
| [`public Camera()`](#camera) | Creates an enabled, centered camera with unit zoom, ignored rotation, default limits and no smoothing. |

## Properties

| Declaration | Contract |
| --- | --- |
| [`public Vector2 Offset { get; set; }`](#offset) | Gets or sets the additional canvas offset. |
| [`public Vector2 Zoom { get; set; }`](#zoom) | Gets or sets the canvas magnification. |
| [`public AnchorModeEnum AnchorMode { get; set; }`](#anchormode) | Gets or sets the camera anchor mode. |
| [`public bool IgnoreRotation { get; set; }`](#ignorerotation) | Gets or sets whether view rotation ignores the node rotation. |
| [`public bool Enabled { get; set; }`](#enabled) | Gets or sets whether this camera can become current. |
| [`public CameraProcessCallback ProcessCallback { get; set; }`](#processcallback) | Gets or sets the internal frame lane for tracking. |
| [`public bool LimitEnabled { get; set; }`](#limitenabled) | Gets or sets whether scroll limits apply. |
| [`public bool LimitSmoothed { get; set; }`](#limitsmoothed) | Gets or sets whether limits constrain the target before smoothing. |
| [`public bool PositionSmoothingEnabled { get; set; }`](#positionsmoothingenabled) | Gets or sets whether position updates interpolate toward the target. |
| [`public float PositionSmoothingSpeed { get; set; }`](#positionsmoothingspeed) | Gets or sets the position smoothing coefficient. |
| [`public bool RotationSmoothingEnabled { get; set; }`](#rotationsmoothingenabled) | Gets or sets whether view rotation interpolates toward the node angle. |
| [`public float RotationSmoothingSpeed { get; set; }`](#rotationsmoothingspeed) | Gets or sets the rotation smoothing coefficient. |
| [`public bool DragHorizontalEnabled { get; set; }`](#draghorizontalenabled) | Gets or sets whether horizontal movement waits for a drag margin. |
| [`public bool DragVerticalEnabled { get; set; }`](#dragverticalenabled) | Gets or sets whether vertical movement waits for a drag margin. |
| [`public float DragHorizontalOffset { get; set; }`](#draghorizontaloffset) | Gets or sets the configured horizontal drag bias. |
| [`public float DragVerticalOffset { get; set; }`](#dragverticaloffset) | Gets or sets the configured vertical drag bias. |
| [`public int LimitLeft { get; set; }`](#limitleft) | Gets or sets the left scroll limit in canvas units. |
| [`public float DragLeftMargin { get; set; }`](#dragleftmargin) | Gets or sets the left drag margin as a fraction of the half-screen extent. |
| [`public int LimitTop { get; set; }`](#limittop) | Gets or sets the top scroll limit in canvas units. |
| [`public float DragTopMargin { get; set; }`](#dragtopmargin) | Gets or sets the top drag margin as a fraction of the half-screen extent. |
| [`public int LimitRight { get; set; }`](#limitright) | Gets or sets the right scroll limit in canvas units. |
| [`public float DragRightMargin { get; set; }`](#dragrightmargin) | Gets or sets the right drag margin as a fraction of the half-screen extent. |
| [`public int LimitBottom { get; set; }`](#limitbottom) | Gets or sets the bottom scroll limit in canvas units. |
| [`public float DragBottomMargin { get; set; }`](#dragbottommargin) | Gets or sets the bottom drag margin as a fraction of the half-screen extent. |
| [`public Node? CustomViewport { get; set; }`](#customviewport) | Gets or sets the borrowed viewport used instead of the containing viewport. |

## Methods

| Declaration | Contract |
| --- | --- |
| [`public int GetLimit(Side side)`](#getlimit) | Returns the specified scroll limit. |
| [`public void SetLimit(Side side, int limit)`](#setlimit) | Changes a scroll limit and refreshes the current view without advancing stored position smoothing. |
| [`public float GetDragMargin(Side side)`](#getdragmargin) | Returns the specified drag margin. |
| [`public void SetDragMargin(Side side, float dragMargin)`](#setdragmargin) | Changes a drag margin for subsequent tracking updates. |
| [`public bool IsCurrent()`](#iscurrent) | Reports whether this is the viewport's current camera. |
| [`public void MakeCurrent()`](#makecurrent) | Selects this enabled camera and immediately updates the viewport canvas. |
| [`public void ForceUpdateScroll()`](#forceupdatescroll) | Forces an immediate tracking update for the current camera. |
| [`public void ResetSmoothing()`](#resetsmoothing) | Updates scrolling, then resets stored position smoothing to its current destination. |
| [`public void Align()`](#align) | Realigns the drag target to the global node position and configured drag offsets, then updates scrolling. |
| [`public Vector2 GetTargetPosition()`](#gettargetposition) | Returns the cached target position before position smoothing and the final view offset. |
| [`public Vector2 GetScreenCenterPosition()`](#getscreencenterposition) | Returns the cached center of the visible screen in canvas coordinates. |
| [`public float GetScreenRotation()`](#getscreenrotation) | Returns the cached view rotation in radians. |

## Protected hooks

| Declaration | Contract |
| --- | --- |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Appends all stored camera settings and the non-stored CustomViewport reference to Entity descriptors. |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#createsceneinstancefactory) | Creates a static exact-type Camera factory for PackedScene. Derived types use the inherited factory override contract. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Inherited disposal releases camera selection and owned children; borrowed viewport references are dropped in finally. |
| [`protected override void OnNotification(int what)`](#onnotification) | Runs inherited handling, then current-camera tracking on the selected internal lane or an unsmoothed transform notification. |

## Constructors descriptions

### Camera

`public Camera()`

Creates an enabled, centered camera with unit zoom, ignored rotation, default limits and no smoothing.

**Remarks:** Enables inherited transform notifications. The camera remains detached until added to a viewport scene.
## Properties descriptions

### Offset

`public Vector2 Offset { get; set; }`

Gets or sets the additional canvas offset.

**Value:** Vector2.Zero initially.

**Remarks:** Finite offset in canvas units, applied after limits. Equal writes do nothing.

**ArgumentOutOfRangeException:** The value is nonfinite or outside the documented contract.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### Zoom

`public Vector2 Zoom { get; set; }`

Gets or sets the canvas magnification.

**Value:** Vector2.One initially.

**Remarks:** Finite, nonzero zoom; negative axes mirror the view. Approximately zero axes are rejected.

**ArgumentOutOfRangeException:** The value is nonfinite or outside the documented contract.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### AnchorMode

`public AnchorModeEnum AnchorMode { get; set; }`

Gets or sets the camera anchor mode.

**Value:** AnchorModeEnum.DragCenter initially.

**Remarks:** Selects centered tracking or a fixed top-left anchor. Changes update the current view.

**ArgumentOutOfRangeException:** The value is nonfinite or outside the documented contract.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### IgnoreRotation

`public bool IgnoreRotation { get; set; }`

Gets or sets whether view rotation ignores the node rotation.

**Value:** true initially.

**Remarks:** True keeps the view unrotated. Enabling resets the cached angle to zero.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### Enabled

`public bool Enabled { get; set; }`

Gets or sets whether this camera can become current.

**Value:** true initially.

**Remarks:** Controls eligibility for the viewport current camera. Disabling the current camera selects the first enabled camera in tree order.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### ProcessCallback

`public CameraProcessCallback ProcessCallback { get; set; }`

Gets or sets the internal frame lane for tracking.

**Value:** CameraProcessCallback.Idle initially.

**Remarks:** Selects the internal process lane, independently of the public processing flags.

**ArgumentOutOfRangeException:** The value is nonfinite or outside the documented contract.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### LimitEnabled

`public bool LimitEnabled { get; set; }`

Gets or sets whether scroll limits apply.

**Value:** true initially.

**Remarks:** Enables all four limits. Offset may move the view beyond them.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### LimitSmoothed

`public bool LimitSmoothed { get; set; }`

Gets or sets whether limits constrain the target before smoothing.

**Value:** false initially.

**Remarks:** Limits the target before position smoothing. Otherwise limits constrain the rendered origin.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### PositionSmoothingEnabled

`public bool PositionSmoothingEnabled { get; set; }`

Gets or sets whether position updates interpolate toward the target.

**Value:** false initially.

**Remarks:** Uses the position smoothing speed on subsequent updates; enabling does not immediately reset the cached position.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### PositionSmoothingSpeed

`public float PositionSmoothingSpeed { get; set; }`

Gets or sets the position smoothing coefficient.

**Value:** 5f initially.

**Remarks:** Nonnegative position smoothing coefficient. Negative finite assignments clamp to zero; each update uses speed times the last selected frame delta.

**ArgumentOutOfRangeException:** The value is nonfinite or outside the documented contract.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### RotationSmoothingEnabled

`public bool RotationSmoothingEnabled { get; set; }`

Gets or sets whether view rotation interpolates toward the node angle.

**Value:** false initially.

**Remarks:** Interpolates along the shortest angle on subsequent updates when IgnoreRotation is false.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### RotationSmoothingSpeed

`public float RotationSmoothingSpeed { get; set; }`

Gets or sets the rotation smoothing coefficient.

**Value:** 5f initially.

**Remarks:** Nonnegative angular smoothing coefficient. Negative finite assignments clamp to zero; weights are not capped at one.

**ArgumentOutOfRangeException:** The value is nonfinite or outside the documented contract.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### DragHorizontalEnabled

`public bool DragHorizontalEnabled { get; set; }`

Gets or sets whether horizontal movement waits for a drag margin.

**Value:** false initially.

**Remarks:** Keeps the current target inside the horizontal drag margins on subsequent updates.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### DragVerticalEnabled

`public bool DragVerticalEnabled { get; set; }`

Gets or sets whether vertical movement waits for a drag margin.

**Value:** false initially.

**Remarks:** Keeps the current target inside the vertical drag margins on subsequent updates.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### DragHorizontalOffset

`public float DragHorizontalOffset { get; set; }`

Gets or sets the configured horizontal drag bias.

**Value:** 0f initially.

**Remarks:** Finite relative horizontal bias; negative uses the right margin, positive the left. Values outside minus one to one are accepted.

**ArgumentOutOfRangeException:** The value is nonfinite or outside the documented contract.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### DragVerticalOffset

`public float DragVerticalOffset { get; set; }`

Gets or sets the configured vertical drag bias.

**Value:** 0f initially.

**Remarks:** Finite relative vertical bias; negative uses the bottom margin, positive the top. Values outside minus one to one are accepted.

**ArgumentOutOfRangeException:** The value is nonfinite or outside the documented contract.

**InvalidOperationException:** Mutation is off-owner, during capture, or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### LimitLeft

`public int LimitLeft { get; set; }`

Gets or sets the left scroll limit in canvas units.

**Value:** -10,000,000 initially. Inverted limits are allowed.

**Remarks:** Offset is applied after limiting. Delegates to GetLimit and SetLimit.

**InvalidOperationException:** Mutation is unavailable or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### DragLeftMargin

`public float DragLeftMargin { get; set; }`

Gets or sets the left drag margin as a fraction of the half-screen extent.

**Value:** 0.2 initially. Any finite value is accepted; margins are applied on the next tracking update.

**ArgumentOutOfRangeException:** The value is nonfinite.

**InvalidOperationException:** Mutation is unavailable.

**ObjectDisposedException:** The camera is disposed.

### LimitTop

`public int LimitTop { get; set; }`

Gets or sets the top scroll limit in canvas units.

**Value:** -10,000,000 initially. Inverted limits are allowed.

**Remarks:** Offset is applied after limiting. Delegates to GetLimit and SetLimit.

**InvalidOperationException:** Mutation is unavailable or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### DragTopMargin

`public float DragTopMargin { get; set; }`

Gets or sets the top drag margin as a fraction of the half-screen extent.

**Value:** 0.2 initially. Any finite value is accepted; margins are applied on the next tracking update.

**ArgumentOutOfRangeException:** The value is nonfinite.

**InvalidOperationException:** Mutation is unavailable.

**ObjectDisposedException:** The camera is disposed.

### LimitRight

`public int LimitRight { get; set; }`

Gets or sets the right scroll limit in canvas units.

**Value:** 10,000,000 initially. Inverted limits are allowed.

**Remarks:** Offset is applied after limiting. Delegates to GetLimit and SetLimit.

**InvalidOperationException:** Mutation is unavailable or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### DragRightMargin

`public float DragRightMargin { get; set; }`

Gets or sets the right drag margin as a fraction of the half-screen extent.

**Value:** 0.2 initially. Any finite value is accepted; margins are applied on the next tracking update.

**ArgumentOutOfRangeException:** The value is nonfinite.

**InvalidOperationException:** Mutation is unavailable.

**ObjectDisposedException:** The camera is disposed.

### LimitBottom

`public int LimitBottom { get; set; }`

Gets or sets the bottom scroll limit in canvas units.

**Value:** 10,000,000 initially. Inverted limits are allowed.

**Remarks:** Offset is applied after limiting. Delegates to GetLimit and SetLimit.

**InvalidOperationException:** Mutation is unavailable or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### DragBottomMargin

`public float DragBottomMargin { get; set; }`

Gets or sets the bottom drag margin as a fraction of the half-screen extent.

**Value:** 0.2 initially. Any finite value is accepted; margins are applied on the next tracking update.

**ArgumentOutOfRangeException:** The value is nonfinite.

**InvalidOperationException:** Mutation is unavailable.

**ObjectDisposedException:** The camera is disposed.

### CustomViewport

`public Node? CustomViewport { get; set; }`

Gets or sets the borrowed viewport used instead of the containing viewport.

**Value:** Null initially. Null or a non-viewport Node selects the containing viewport.

**Remarks:** Runtime-only state, not stored by PackedScene. The target must be active in this tree when the camera is attached; another tree's or detached viewport is unsupported. Changing the target releases this camera from the old viewport and selects it on the new viewport only when no other camera is current.

**NotSupportedException:** The selected viewport is not active in this camera's tree.

**InvalidOperationException:** Mutation is unavailable, or no containing viewport exists.

**ObjectDisposedException:** The camera or assigned node is disposed.
## Methods descriptions

### GetLimit

`public int GetLimit(Side side)`

Returns the specified scroll limit.

**side:** Left, Top, Right or Bottom.

**Returns:** The configured integer limit, in canvas units.

**ArgumentOutOfRangeException:** The side is undefined.

**InvalidOperationException:** The attached camera is queried off-owner.

**ObjectDisposedException:** The camera is disposed.

### SetLimit

`public void SetLimit(Side side, int limit)`

Changes a scroll limit and refreshes the current view without advancing stored position smoothing.

**side:** Left, Top, Right or Bottom.

**limit:** Canvas units; inverted and undersized bounds are accepted and centered by tracking.

**ArgumentOutOfRangeException:** The side is undefined.

**InvalidOperationException:** Mutation is unavailable or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### GetDragMargin

`public float GetDragMargin(Side side)`

Returns the specified drag margin.

**side:** Left, Top, Right or Bottom.

**Returns:** The fraction of the corresponding half-screen extent.

**ArgumentOutOfRangeException:** The side is undefined.

**InvalidOperationException:** The attached camera is queried off-owner.

**ObjectDisposedException:** The camera is disposed.

### SetDragMargin

`public void SetDragMargin(Side side, float dragMargin)`

Changes a drag margin for subsequent tracking updates.

**side:** Left, Top, Right or Bottom.

**dragMargin:** Any finite fraction; the zero-to-one editor range is not a runtime clamp.

**ArgumentOutOfRangeException:** The side is undefined or the value nonfinite.

**InvalidOperationException:** Mutation is unavailable.

**ObjectDisposedException:** The camera is disposed.

### IsCurrent

`public bool IsCurrent()`

Reports whether this is the viewport's current camera.

**Returns:** False while detached or when another camera is current.

**InvalidOperationException:** The attached camera is queried off-owner.

**ObjectDisposedException:** The camera is disposed.

### MakeCurrent

`public void MakeCurrent()`

Selects this enabled camera and immediately updates the viewport canvas.

**InvalidOperationException:** The camera is disabled, detached, off-owner, captured or tracking overflows.

**ObjectDisposedException:** The camera is disposed.

### ForceUpdateScroll

`public void ForceUpdateScroll()`

Forces an immediate tracking update for the current camera.

**Remarks:** Detached or noncurrent cameras do nothing. Uses the last selected frame delta, so repeated calls can advance smoothing again.

**InvalidOperationException:** Mutation is unavailable or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### ResetSmoothing

`public void ResetSmoothing()`

Updates scrolling, then resets stored position smoothing to its current destination.

**Remarks:** The next scroll update uses the reset position. Rotation smoothing is unchanged.

**InvalidOperationException:** Mutation is unavailable or tracking arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### Align

`public void Align()`

Realigns the drag target to the global node position and configured drag offsets, then updates scrolling.

**Remarks:** Requires viewport membership. Smoothing still applies; the node's own transform stays unchanged. An overflowing target or failed tracking update preserves the previous target and view.

**InvalidOperationException:** No viewport is active, mutation is unavailable or arithmetic overflows.

**ObjectDisposedException:** The camera is disposed.

### GetTargetPosition

`public Vector2 GetTargetPosition()`

Returns the cached target position before position smoothing and the final view offset.

**Returns:** Canvas coordinates; zero before the first tracking update.

**InvalidOperationException:** The attached camera is queried off-owner.

**ObjectDisposedException:** The camera is disposed.

### GetScreenCenterPosition

`public Vector2 GetScreenCenterPosition()`

Returns the cached center of the visible screen in canvas coordinates.

**Returns:** The last successfully calculated center, including limits, smoothing, zoom, rotation and offset.

**InvalidOperationException:** The attached camera is queried off-owner.

**ObjectDisposedException:** The camera is disposed.

### GetScreenRotation

`public float GetScreenRotation()`

Returns the cached view rotation in radians.

**Returns:** Zero while rotation is ignored; otherwise the last updated, possibly smoothed rotation.

**InvalidOperationException:** The attached camera is queried off-owner.

**ObjectDisposedException:** The camera is disposed.
## Protected hooks descriptions

### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Appends all stored camera settings and the non-stored CustomViewport reference to Entity descriptors.

**Remarks:** Appends stored camera configuration and the non-stored custom viewport reference.

### CreateSceneInstanceFactory

`protected override Func<Node> CreateSceneInstanceFactory()`

Creates a static exact-type Camera factory for PackedScene. Derived types use the inherited factory override contract.

### Dispose

`protected override void Dispose(bool disposing)`

Inherited disposal releases camera selection and owned children; borrowed viewport references are dropped in finally.

**Remarks:** Inherited disposal releases viewport selection and owned children; finally drops borrowed viewport references.

### OnNotification

`protected override void OnNotification(int what)`

Runs inherited handling, then current-camera tracking on the selected internal lane or an unsmoothed transform notification.

**Remarks:** Updates the current view on the selected internal frame lane and unsmoothed transform notifications. Actual membership, not manually delivered enter/exit notifications, owns viewport registration.

## Tracking details and source audit

The [coverage page](../coverage/classes/Camera2D.md) pins the complete reference; the reviewed source is [camera_2d.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/camera_2d.cpp), with Viewport camera selection and Transform2D scale decomposition audited too. The following details are intentional observable behavior, including reference quirks:

- First activation samples global position directly. Subsequent centered updates constrain the previous target within the drag margins around the node. Explicit drag offsets apply the selected margin and the unscaled half-screen size; automatic dragging uses half-screen size divided by Zoom. Margin/enable changes apply on the next tracking update. Automatic dragging never rewrites configured offsets.
- Position smoothing uses `speed * lastLaneDelta`; rotation uses shortest-angle interpolation with the same style of weight. Weights are not clamped to one. Repeated ForceUpdateScroll can advance smoothing repeatedly using the same delta. Smoothing-enabled setters do not reset state. Zoom, Offset, IgnoreRotation, limit-value and drag-offset changes refresh the displayed view but restore stored position smoothing afterward.
- ResetSmoothing first updates the displayed view, then sets the stored smoothed position to the destination; a subsequent update displays that destination. This preserves the pinned executable order, which is more specific than the reference prose's immediate-reset wording. Align updates the drag target and scrolling, while smoothing still applies.
- LimitSmoothed clamps the target before interpolation; otherwise final limiting uses the projected anchor span. Oversized/inverted limit rectangles center the view. The FixedTopLeft hard-limit path clamps the anchor, with zero centered span. Offset is applied after limits. Arithmetic for integer-bound sums widens to avoid overflow before floating-point conversion.
- Zoom may be negative. With IgnoreRotation true, signs directly mirror axes. With rotation enabled, the pose decomposes scale with reflection on Y before applying the angle; negative X zoom can therefore shift the apparent center, matching the pinned transform composition. Limits operate on the signed/projected span, not an axis-aligned bounding box of a rotated screen.
- CustomViewport's null reset follows the public nullable/default-target contract. It rejects inactive/foreign-tree targets before changing an existing binding. Independent viewport/multiwindow routing is not claimed.

All 24 camera configuration properties are stored. CustomViewport is not stored. Packing never captures the current selection, cached position/angle, viewport matrix, frame delta or runtime binding. The exact-type factory creates a fresh detached Camera; activation computes new tracking state.

## Verification and remaining gaps

[CameraTests](../../tests/Electron2D.Tests/CameraTests.cs) checks defaults, enum/finite/side validation, metadata and packed copying, zoom/rotation/anchor formulas, drag behavior, limiting/inverted/extreme bounds, smoothing and repeated updates, idle/physics/pause behavior, inherited transforms, selection/tree order/reentry, manual notifications, failed exit cleanup, invalid retarget, owner/capture/disposal guards, overflow recovery and zero warmed tracking allocations.

[CameraRenderingTests](../../tests/Electron2D.Tests/CameraRenderingTests.cs) checks native pixels and injected pointer mapping at six stages: centered view, zoom/follow, rotation, explicit camera switch, automatic handoff and identity after disabling all cameras. Retained draw commands are recorded once. Linux Wayland GPU/compatibility and dummy software passed, including both HLSL and GLSL material variants on GPU. The existing warmed renderer allocation check includes an active camera. Other platforms, physical-input/visual acceptance and large-scene performance are unverified.

EditorDrawScreen/EditorDrawLimits/EditorDrawDragMargin require a real edited-scene preview mode, editor viewport ownership and its redraw/settings integration. They remain absent; EditedSceneRoot currently selects warning scope and explicitly does not enable editor behavior. Camera physics interpolation requires the still-absent inherited node interpolation policy/reset path and render-time interpolation integration. Implement these in the first corresponding editor/interpolation slices. CanvasLayer following and [Parallax](Parallax.md) camera updates are implemented. Legacy parallax nodes, offscreen/nested viewports and multiple native windows remain separate dependencies. No inert camera properties stand in for them.
