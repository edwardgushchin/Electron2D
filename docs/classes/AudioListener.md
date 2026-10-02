# AudioListener

Last updated: 2026-10-02

**Inherits:** [Entity](Entity.md), CanvasItem, Node, ElectronObject

**Source:** [AudioListener.cs](../../src/Scene/Audio/AudioListener.cs)

**Declaration:** `public sealed class AudioListener : Entity`

**Component:** [Audio playback](../components/audio-playback.md)

## Description

Selects the listening origin and orientation for 2D spatial players in its root [Viewport](Viewport.md). A root scene viewport enables 2D listening when its `SceneTree` starts. Without an explicit current listener, its client center is the listening point. One listener may be current per viewport. Selecting another listener clears the previous request. A detached current request activates on tree entry; the current listener retains its request across exit and reentry. Listener position and rotation are inherited Entity transforms. The node borrows its viewport and owns no audio device or stream.

## Example

```csharp
var listener = new AudioListener { Position = new Vector2(100, 50) };
window.AddChild(listener);
listener.MakeCurrent();
// After use: listener.ClearCurrent();
```

Attach the window through `Engine.Run`; tree mutations occur on the scene owner thread.

## API summary

| Member | Contract |
| --- | --- |
| `public AudioListener()` | Creates a detached, noncurrent listener. |
| `public bool Current { get; set; }` | Reads viewport ownership while attached, or the requested state while detached; assignment calls `MakeCurrent` or `ClearCurrent`. |
| `public void MakeCurrent()` | Claims the containing viewport immediately, or records a detached request. |
| `public void ClearCurrent()` | Releases the viewport and clears a pending request. |
| `public bool IsCurrent()` | Reports the same state as `Current`. |
| `protected override void OnEnterTree()` / `OnExitTree()` | Claim or release a requested viewport during lifecycle changes. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores the current request for `PackedScene`. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Recreates an exact listener instance. |

## Member descriptions

### `Current`, `MakeCurrent`, `ClearCurrent` and `IsCurrent`

A detached `MakeCurrent` is remembered. On entry it claims the nearest root viewport. Calling it on another listener in that viewport removes the first listener's request. `ClearCurrent` never changes another listener. Exiting a tree releases current ownership while retaining the exiting listener's request for reentry; a noncurrent listener exits without a request. A packed detached listener stores its requested `Current` value; copying an ordinary noncurrent listener does not choose it. Operations reject disposed nodes and attached off-owner access; mutations also reject scene capture.

## Verification and limits

[AudioSpatialTests](../../tests/Electron2D.Tests/AudioSpatialTests.cs) checks request/clear, `PackedScene`, explicit listener movement, viewport ownership and real stereo PCM through Linux Wayland GPU and compatibility hosts. Physical listening and other platforms are unverified. Embedded viewports and 3D listeners remain outside the current viewport runtime and 2D product scope respectively. [Coverage](../coverage/classes/AudioListener2D.md) records the mapped reference members.
