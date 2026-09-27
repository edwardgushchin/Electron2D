# CheckBox

Last updated: 2026-09-27

**Inherits:** [Button](Button.md) · **Inherited By:** —

**Declaration:** `public class CheckBox : Button` · **Source:** [CheckBox.cs](../../src/Scene/GUI/CheckBox.cs) · **Component:** [GUI controls](../components/canvas-rendering.md)

## Description

A [Button](Button.md) with `ToggleMode = true` and `Alignment = Left` by default. Its themed indicator precedes the content and moves to the opposite side in RTL. Assigning a [ButtonGroup](ButtonGroup.md) selects radio imagery; input/group behavior is inherited from BaseButton.

The reserved indicator size is the per-axis maximum over checked, unchecked, radio and disabled icons, then limited by `icon_max_width`. The selected icon keeps its own aspect ratio and size within that reservation. Vertical placement truncates the centering offset to an integer before adding `check_v_offset`. The indicator is recorded after Button text/background, using `checkbox_checked_color` or `checkbox_unchecked_color`.

Theme resources remain borrowed. Exact scene factories preserve CheckBox, and typed descriptors override the inherited alignment/toggle revert defaults. Derived user classes must provide their own scene factory.

## Example

```csharp
var control = new CheckBox("Enable sound");
control.Toggled += enabled => SetSoundEnabled(enabled);
root.AddChild(control);
```

The surrounding application supplies `root` and the callback.

## API summary

| Signature | Contract |
| --- | --- |
| `public CheckBox()` | [CheckBox](#checkbox): Creates an empty checkbox with toggle mode enabled. |
| `public CheckBox(string text)` | [CheckBox](#checkbox-2): Creates a checkbox with untranslated text. |
| `protected override Vector2 OnGetMinimumSize()` | [OnGetMinimumSize](#ongetminimumsize): Overrides the inherited node/control contract. |
| `protected override void OnNotification(int what)` | [OnNotification](#onnotification): Overrides the inherited node/control contract. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | [CreateSceneInstanceFactory](#createsceneinstancefactory): Overrides the inherited node/control contract. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | [GetPropertyDescriptors](#getpropertydescriptors): Overrides the inherited node/control contract. |

## Member descriptions

<a id="checkbox"></a>
### CheckBox

`public CheckBox()`

Creates an empty checkbox with toggle mode enabled.


<a id="checkbox-2"></a>
### CheckBox

`public CheckBox(string text)`

Creates a checkbox with untranslated text.

- `text`: Initial text; may be empty.

Errors: `ArgumentNullException` — The text is null..

<a id="ongetminimumsize"></a>
### OnGetMinimumSize

`protected override Vector2 OnGetMinimumSize()`

Implements the inherited [Button](Button.md), [Control](Control.md) and [Node](Node.md) contract for the behavior described above.


<a id="onnotification"></a>
### OnNotification

`protected override void OnNotification(int what)`

Implements the inherited [Button](Button.md), [Control](Control.md) and [Node](Node.md) contract for the behavior described above.


<a id="createsceneinstancefactory"></a>
### CreateSceneInstanceFactory

`protected override Func<Node> CreateSceneInstanceFactory()`

Implements the inherited [Button](Button.md), [Control](Control.md) and [Node](Node.md) contract for the behavior described above.


<a id="getpropertydescriptors"></a>
### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Implements the inherited [Button](Button.md), [Control](Control.md) and [Node](Node.md) contract for the behavior described above.


## Theme and verification

The built-in theme supplies checked/unchecked and radio checked/unchecked icons with disabled counterparts, four-unit content margins, white indicator colors and zero vertical offset.

[ButtonTests](../../tests/Electron2D.Tests/ButtonTests.cs) passes indicator maxima, integer centering, radio selection, typed subtype defaults and storage. [ButtonRenderingTests](../../tests/Electron2D.Tests/ButtonRenderingTests.cs) passes checkbox/radio/RTL pixels on Linux Wayland GPU and compatibility and 64 measured active frames after warmup at zero managed bytes. The inherited [Button residency contract](Button.md#description) includes every indicator state. These checks do not establish accessibility-service or other-platform support. See [coverage](../coverage/classes/CheckBox.md).
