# HSeparator

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public class HSeparator : Separator`. **Inherits:** [Separator](Separator.md). **Inherited By:** —. **Source:** [Separator.cs](../../src/Scene/GUI/Separator.cs). **Component:** [GUI primitives](../components/gui-primitives.md).

Concrete horizontal visual separator. Its intrinsic minimum is `(3, separation)`, with built-in separation four. Drawing uses its own matching gray line theme and the shared integer-centering contract. Styles remain borrowed; inherited owner/lifetime/input/layout/theme behavior applies. See [VSeparator](VSeparator.md) for the other axis.

## Example

```csharp
var rule = new HSeparator { Size = new(100, 20), MouseFilter = MouseFilter.Ignore };
```

Attach to a control/container or viewport. The axis remains fixed; changing StyleBoxLine orientation changes the style's drawing, not this control's centering axis.

## Constructors

| Signature | Description |
| --- | --- |
| `public HSeparator()` | Creates the fixed horizontal axis with inherited control defaults. |

## Protected extension points

| Signature | Description |
| --- | --- |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Creates this exact built-in type; derived user types supply their own factory. |

## Method Descriptions

### CreateSceneInstanceFactory()

PackedScene and the registered file factory preserve this concrete type and inherited stored authoring. No borrowed style ownership is transferred. Unsupported derived types fail through the inherited exact-factory rule.

## Verification and limits

Managed geometry/theme/lifetime/file and real GPU/compatibility pixel checks cover this axis; see [the component](../components/gui-primitives.md). Other platforms, native allocator counts, accessibility and final owner acceptance remain unverified.
