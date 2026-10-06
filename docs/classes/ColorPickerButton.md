# ColorPickerButton

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.ColorPickerButton`. **Base:** `Electron2D.Button`. **Source:** [source](../../src/Scene/GUI/ColorPickerButton.cs). **Component:** [Color authoring](../components/color-authoring.md).

A themed swatch button with a lazy required borrowed ColorPicker/PopupPanel. Accept commits the focused field; Escape restores the opening color. Use an embedding viewport.

```csharp
var root = new Window { Size = new(800, 800), GUIEmbedSubwindows = true };
var button = new ColorPickerButton { Position = new(30, 30), Size = new(160, 40) };
button.ColorChanged += color => Console.WriteLine(color);
root.AddChild(button);
Engine.Run(root);
```


## Constructors

| Complete declaration | Contract |
| --- | --- |
| `public ColorPickerButton()` | Creates a black color button with toggle mode and both alpha/intensity editing enabled. |
| `public ColorPickerButton(System.String text)` | Creates a color button with source text. Nonnull initial caption. |

## Properties

| Complete declaration | Contract |
| --- | --- |
| `public Electron2D.Color Color { get; set; }` | Gets or sets the finite selected color without ColorChanged. Opaque black initially; HDR channels are preserved. |
| `public System.Boolean EditAlpha { get; set; }` | Gets or sets alpha channel visibility in the owned picker. True initially. |
| `public System.Boolean EditIntensity { get; set; }` | Gets or sets intensity editing in the owned picker. True initially. |

## Events

| Complete declaration | Contract |
| --- | --- |
| `public event System.Action<Electron2D.Color> ColorChanged` | Occurs after an editor color change or Escape restoration. |
| `public event System.Action PickerCreated` | Occurs once after the required picker and popup have been created. |
| `public event System.Action PopupClosed` | Occurs after an opened picker popup closes. |

## Members and lifecycle

| Complete declaration | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Extends the inherited typed lifecycle contract. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Extends the inherited typed lifecycle contract. |
| `public Electron2D.ColorPicker GetPicker()` | Creates if necessary and returns the stable required borrowed color editor. The button-owned picker. |
| `public Electron2D.PopupPanel GetPopup()` | Creates if necessary and returns the stable required borrowed popup. The button-owned popup. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Extends the inherited typed lifecycle contract. |
| `protected override System.Void OnDraw()` | Extends the inherited typed lifecycle contract. |
| `protected override System.Void OnGUIInput(Electron2D.InputEvent input)` | Extends the inherited typed lifecycle contract. |
| `protected override System.Void OnNotification(System.Int32 what)` | Extends the inherited typed lifecycle contract. |
| `protected override System.Void OnPressed()` | Extends the inherited typed lifecycle contract. |
