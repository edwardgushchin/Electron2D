# SpinBox

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.SpinBox`. **Inherits:** [Range](Range.md). **Inherited By:** —. **Source:** [model](../../src/Scene/GUI/SpinBox.cs), [input/layout](../../src/Scene/GUI/SpinBox.Input.cs), [storage](../../src/Scene/GUI/SpinBox.Storage.cs). **Component:** [Numeric input](../components/numeric-input.md).

An editable shared-range field with owned borrowed LineEdit, numeric formulas, held/dragged arrows and localized generated numbers. The required field uses SpinBoxInnerLineEdit; configure its text/background/selection properties without disposing or reparenting it. Step defaults one and vertical sizing Fill. The component records exact input, parsing, theme, ordering, storage and native limits.

```csharp
var root = new Window { Size = new(640, 360) };
var number = new SpinBox { Position = new(30, 30), Size = new(240, 40),
    Step = .01, MaxValue = 1000, Prefix = "Speed", Suffix = "px/s" };
number.ValueChanged += value => Console.WriteLine(value);
root.AddChild(number);
Engine.Run(root);
```

## Constructors

| Complete declaration | Contract |
| --- | --- |
| `public SpinBox()` | Creates an editable field with Step=1, empty affixes and vertical Fill sizing. |

## Properties

| Complete declaration | Contract |
| --- | --- |
| `public Electron2D.HorizontalAlignment Alignment { get; set; }` | Gets or sets the borrowed field's text alignment. value: Left initially. |
| `public System.Boolean CustomArrowRound { get; set; }` | Gets or sets arrow-grid snapping before the ordinary Range snap. value: False initially. |
| `public System.Double CustomArrowStep { get; set; }` | Gets or sets the arrow increment; zero uses Range.Step. value: Zero initially; finite signed increments are retained. System.ArgumentOutOfRangeException: The increment is nonfinite. |
| `public System.Boolean Editable { get; set; }` | Gets or sets editing and pointer-step availability; disabling cancels repeat and capture. value: True initially. |
| `public System.String Prefix { get; set; }` | Gets or sets a prefix shown with a separating space outside editing. value: Empty initially. |
| `public System.Boolean SelectAllOnFocus { get; set; }` | Gets or sets selecting all numeric text when the field gains editing focus. value: False initially. |
| `public System.String Suffix { get; set; }` | Gets or sets a suffix shown with a separating space outside editing. value: Empty initially. |
| `public System.Boolean UpdateOnTextChanged { get; set; }` | Gets or sets deferred evaluation after user text changes. value: False initially. Intermediate formulas can be replaced by their current result. |

## Methods

| Complete declaration | Contract |
| --- | --- |
| `public System.Void Apply()` | Evaluates the current field text and refreshes it, preserving value after malformed expressions. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Inherited range/control/scene lifecycle extension point; see the owning base reference. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Inherited range/control/scene lifecycle extension point; see the owning base reference. |
| `public Electron2D.LineEdit GetLineEdit()` | Returns the stable required numeric input control. returns: A borrowed owned LineEdit; preserve its parent and lifetime. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited range/control/scene lifecycle extension point; see the owning base reference. |
| `protected override System.Void OnGUIInput(Electron2D.InputEvent input)` | Inherited range/control/scene lifecycle extension point; see the owning base reference. |
| `protected override Electron2D.Vector2 OnGetMinimumSize()` | Inherited range/control/scene lifecycle extension point; see the owning base reference. |
| `protected override System.Void OnNotification(System.Int32 what)` | Inherited range/control/scene lifecycle extension point; see the owning base reference. |
| `protected override System.Void OnValueChanged(System.Double newValue)` | Inherited range/control/scene lifecycle extension point; see the owning base reference. |

## Lifetime, errors, scenes and verification

Attached operations require the scene owner and respect capture/mutation guards. Required internal-field disposal is rejected while SpinBox remains live. CustomArrowStep rejects nonfinite input. Bad formulas preserve the prior value; failed observers remain aggregated after committed value/text. Release/hide/exit/readonly/focus loss cancel active repeat/capture and restore native mouse mode. Precise warp requires an advertised DisplayServer.MouseWarp capability. Formula evaluation uses the private scalar data profile; generic object/string/collection scripting is not claimed.

PackedScene and fresh-process resource files recreate the owned field and typed defaults. Runtime input/cache/delegates are not stored. SpinBoxTests and SpinBoxRenderingTests cover the connected field, locale, sharing, failure and native workflows; see the [component](../components/numeric-input.md) for the exact prepared managed-allocation boundary and remaining platform/semantic prerequisites.
