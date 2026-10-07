# SandboxWindow

Last updated: 2026-10-07

**Namespace:** `Electron2D.Examples.PhysicsSandbox`. **Declaration:** `internal sealed partial class SandboxWindow : Window`. **Inherits:** [Window](Window.md). **Source:** [window](../../examples/PhysicsSandbox/SandboxWindow.cs), [parameter controls](../../examples/PhysicsSandbox/SandboxWindow.Parameters.cs). **Component:** [Physics sandbox](../components/physics-sandbox.md).

The example's fixed 1152×800 desktop window. It owns a themed OptionButton for twelve physics stories, pause/step/reset controls, three per-story actions, instructions, live counters and native HSliders for world/selected-body/story settings. World/Object/Scene tabs keep the inspector sparse. Rows show units, ranges and default markers. Smash adds impact speed and fragment count under Scene; the second row is hidden in other stories. The twelve retained HSliders avoid construction during tab changes. Scenes start without a selected body; empty field clicks clear selection and replace the Object sliders with a selection prompt. A named selection outline connects the object tab to the field. Separate CanvasLayers place the scene-specific framing below the unscaled interface; a ClipContents Control clips every scene descendant, including selection. The field and inspector finish together, directly above the action dock. The primary scene action has an accent fill. It borrows both font weights from the entry point and owns the style resources it creates. Scene selection disposes the previous PhysicsScene before attaching its replacement; pause preferences survive selection and reset.

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
| `int SceneIndex` | Selected story index 0..11 |
| `void SwitchScene(int index)` | Recreates a story, rejecting indices outside 0..11 |

Input handled by GUI controls does not start a stage grab. Root input observes motion and any left release so crossing the toolbar cannot leave a grab stuck. Focus loss releases it too. P/R/period/Escape remain available as unhandled shortcuts, without conflicting with WASD movement. Styles are disposed after the controls release them. The example adds no runtime Window API or storage schema.

The host temporarily sets Engine.MaxPhysicsStepsPerFrame to one and restores the captured value on disposal. Heavy loads preserve input/render opportunities by dropping catch-up work; simulation can run slower than wall time. The native profile records that budget separately from FPS and fixed-step cost.
