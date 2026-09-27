# CheckButton

Last updated: 2026-09-27

**Inherits:** [Button](Button.md) · **Inherited By:** —

**Declaration:** `public class CheckButton : Button` · **Source:** [CheckButton.cs](../../src/Scene/GUI/CheckButton.cs) · **Component:** [GUI controls](../components/canvas-rendering.md)

## Description

A [Button](Button.md) with `ToggleMode = true` and `Alignment = Left` by default. Its switch indicator follows the content, reversing placement under RTL. It selects the enabled/disabled and mirrored/unmirrored icon pair before computing the pair's maximum size. `icon_max_width` preserves aspect ratio while limiting that reservation.

The selected checked/unchecked texture is recorded after Button text/background. Vertical centering retains fractional coordinates before adding `check_v_offset`, unlike CheckBox's integer centering. Colors come from `button_checked_color` and `button_unchecked_color`.

Theme resources remain borrowed. Exact scene factories preserve CheckButton, and typed descriptors override the inherited alignment/toggle revert defaults. Derived user classes must provide their own scene factory.

## Example

```csharp
var control = new CheckButton("Enable sound");
control.Toggled += enabled => SetSoundEnabled(enabled);
root.AddChild(control);
```

The surrounding application supplies `root` and the callback.

## API summary

| Signature | Contract |
| --- | --- |
| `public CheckButton()` | [CheckButton](#checkbutton): Creates an empty check button with toggle mode enabled. |
| `public CheckButton(string text)` | [CheckButton](#checkbutton-2): Creates a check button with untranslated text. |
| `protected override Vector2 OnGetMinimumSize()` | [OnGetMinimumSize](#ongetminimumsize): Overrides the inherited node/control contract. |
| `protected override void OnNotification(int what)` | [OnNotification](#onnotification): Overrides the inherited node/control contract. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | [CreateSceneInstanceFactory](#createsceneinstancefactory): Overrides the inherited node/control contract. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | [GetPropertyDescriptors](#getpropertydescriptors): Overrides the inherited node/control contract. |

## Member descriptions

<a id="checkbutton"></a>
### CheckButton

`public CheckButton()`

Creates an empty check button with toggle mode enabled.


<a id="checkbutton-2"></a>
### CheckButton

`public CheckButton(string text)`

Creates a check button with untranslated text.

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

The built-in theme supplies checked/unchecked switch icons with disabled and mirrored counterparts, content margins (6,4,6,4), white indicator colors and zero vertical offset.

[ButtonTests](../../tests/Electron2D.Tests/ButtonTests.cs) passes active icon-pair sizing, fractional centering, mirrored selection, typed subtype defaults and storage. [ButtonRenderingTests](../../tests/Electron2D.Tests/ButtonRenderingTests.cs) passes switch/RTL pixels on Linux Wayland GPU and compatibility and 64 measured active frames after warmup at zero managed bytes. The inherited [Button residency contract](Button.md#description) includes every indicator state. These checks do not establish accessibility-service or other-platform support. See [coverage](../coverage/classes/CheckButton.md).
