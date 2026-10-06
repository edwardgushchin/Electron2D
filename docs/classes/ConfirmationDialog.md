# ConfirmationDialog

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.ConfirmationDialog`. **Inherits:** [AcceptDialog](AcceptDialog.md). **Inherited By:** —. **Source:** [implementation](../../src/Scene/GUI/ConfirmationDialog.cs). **Component:** [Embedded dialogs](../components/dialogs.md).

A concrete AcceptDialog with a borrowed built-in cancel button. It starts with Please Confirm..., a (200, 70) minimum, (200, 100) client area and Cancel caption. Acceptance, deferred cancellation, custom actions, theme layout, close action, owner/lifetime guards and callback failure/reopen semantics follow AcceptDialog. CancelButtonText reflects the actual cancel button text. Packed and fresh file scenes recreate independent cancel/OK controls and restore captions/policies. Custom registrations remain runtime configuration.

## Example

```csharp
var root = new Window { Size = new(640, 360), GUIEmbedSubwindows = true };
var dialog = new ConfirmationDialog { DialogText = "Apply these settings?" };
dialog.Confirmed += () => Console.WriteLine("Accepted");
root.AddChild(dialog);
root.Ready += _ => dialog.PopupCentered(new(360, 180));
Engine.Run(root);
```

DialogTests checks owned controls, defaults, event order, registered text, custom/cancel buttons, callback failures/reopening, themes and fresh-process storage. DialogRenderingTests checks real SDL actions and themed pixels on current Linux GPU/compatibility targets. See the component for verification scope and inherited prerequisites.

## Constructors

| Complete declaration | Contract |
| --- | --- |
| `public ConfirmationDialog()` | Creates a hidden confirmation dialog with a 200 by 70 minimum and a 200 by 100 client area. |

## Properties

| Complete declaration | Contract |
| --- | --- |
| `public System.String CancelButtonText { get; set; }` | Gets or sets the owned cancellation button's caption. |

## Events

| Complete declaration | Contract |
| --- | --- |

## Methods

| Complete declaration | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Inherited layout, lifetime or typed scene projection; see the owning base contract. |
| `public Electron2D.Button GetCancelButton()` | Returns the borrowed built-in cancellation button. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited layout, lifetime or typed scene projection; see the owning base contract. |

## Member reference

### .ctor

```csharp
public ConfirmationDialog()
```

Creates a hidden confirmation dialog with a 200 by 70 minimum and a 200 by 100 client area.

### CreateSceneInstanceFactory

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```

### GetCancelButton

```csharp
public Electron2D.Button GetCancelButton()
```

Returns the borrowed built-in cancellation button.

The stable cancel button, subject to RemoveButton ownership transfer.

### GetPropertyDescriptors

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

### CancelButtonText

```csharp
public System.String CancelButtonText { get; set; }
```

Gets or sets the owned cancellation button's caption.

Cancel initially.

| Exception | Trigger |
| --- | --- |
| `System.ArgumentNullException` | The caption is null. |

