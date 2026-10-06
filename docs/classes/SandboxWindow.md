# SandboxWindow

Last updated: 2026-10-06

**Namespace:** `Electron2D.Examples.PhysicsSandbox`. **Declaration:** `internal sealed partial class SandboxWindow : Window`. **Inherits:** [Window](Window.md). **Source:** [window](../../examples/PhysicsSandbox/SandboxWindow.cs), [parameter controls](../../examples/PhysicsSandbox/SandboxWindow.Parameters.cs). **Component:** [Physics sandbox](../components/physics-sandbox.md).

The example's fixed 1152×800 desktop window. It owns a themed OptionButton for eleven physics stories, debug/pause/step/reset controls, three per-story actions, instructions, live counters and eleven native HSliders for world/selected-body/story settings. Separate CanvasLayers place the scaled simulation below the unscaled interface. It borrows both font weights from the entry point and owns the style resources it creates. Scene selection disposes the previous PhysicsScene before attaching its replacement; pause and debug preferences survive selection and reset.

## Consumer entry

```csharp
using var regular = new FontFile { Data = File.ReadAllBytes("Assets/IBMPlexSans-Regular.ttf") };
using var semibold = new FontFile { Data = File.ReadAllBytes("Assets/IBMPlexSans-SemiBold.ttf") };
using var window = new SandboxWindow(regular, semibold);
Engine.Run(window);
```

This internal constructor is used inside the example executable, not exported by Electron2D.dll. Fonts outlive all labels, dropdown items and rendering. Native startup uses the ordinary Engine.Run lifecycle; a root Window is not a headless SceneTree root. [PhysicsSandboxTests](../../tests/Electron2D.Tests/PhysicsSandboxTests.cs) separates headless scene checks from the native window host.

## Internal consumer surface

| Member | Role |
| --- | --- |
| `SandboxWindow(Font regular, Font semibold)` | Creates a detached native-window configuration and initial warehouse |
| `PhysicsScene Scene` | Current interactive story |
| `int SceneIndex` | Selected story index 0..10 |
| `void SwitchScene(int index)` | Recreates a story, rejecting indices outside 0..10 |

Input handled by GUI controls does not start a stage grab. Root input observes motion and any left release so crossing the toolbar cannot leave a grab stuck. Focus loss releases it too. F3/P/R/period/Escape remain available as unhandled shortcuts, without conflicting with WASD movement. Styles are disposed after the controls release them. The example adds no runtime Window API or storage schema.
