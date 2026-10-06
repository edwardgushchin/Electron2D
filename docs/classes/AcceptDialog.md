# AcceptDialog

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.AcceptDialog`. **Inherits:** [Window](Window.md). **Inherited By:** [ConfirmationDialog](ConfirmationDialog.md). **Source:** [implementation](../../src/Scene/GUI/AcceptDialog.cs). **Component:** [Embedded dialogs](../components/dialogs.md).

An embedded notification dialog owning a panel, message label and button row. Attach under GUIEmbedSubwindows and call PopupCentered or another inherited Window.Popup method. It starts hidden, exclusive and transient with wrapped controls and an Alert! title. Borrowed required controls must stay alive and owned. Custom buttons can be detached with RemoveButton without disposal, which disconnects their dialog callbacks. Registered LineEdit submission confirms unless OK is disabled. The independently configurable non-echo ui_close_dialog action and close requests cancel.

DialogHideOnOK controls hiding before OnOKPressed and Confirmed. Cancellation emits Canceled before OnCancelPressed and deferred hiding. CustomAction precedes OnCustomAction without automatic hiding. Required delivery continues after callback failure, and reopening supersedes obsolete deferred hiding. Panel padding, buttons_min_width/buttons_min_height and buttons_separation constrain actual content/button layout. KeepTitleVisible expands embedded title width; decorated positioning is clamped separately. Packed scenes restore scalar policies and recreate internal nodes, while custom buttons and registrations remain runtime configuration. Native child-window and inherited capabilities retain their coverage prerequisites.

## Example

```csharp
var root = new Window { Size = new(640, 360), GUIEmbedSubwindows = true };
var dialog = new AcceptDialog { DialogText = "Apply these settings?" };
dialog.Confirmed += () => Console.WriteLine("Accepted");
root.AddChild(dialog);
root.Ready += _ => dialog.PopupCentered(new(360, 180));
Engine.Run(root);
```

DialogTests checks owned controls, defaults, event order, registered text, custom/cancel buttons, callback failures/reopening, themes and fresh-process storage. DialogRenderingTests checks real SDL actions and themed pixels on current Linux GPU/compatibility targets. See the component for verification scope and inherited prerequisites.

## Constructors

| Complete declaration | Contract |
| --- | --- |
| `public AcceptDialog()` | Creates a hidden, exclusive, transient dialog with wrapped controls and the title Alert!. |

## Properties

| Complete declaration | Contract |
| --- | --- |
| `public System.Boolean DialogAutowrap { get; set; }` | Gets or sets whether the message wraps at word boundaries. |
| `public System.Boolean DialogCloseOnEscape { get; set; }` | Gets or sets cancellation by the independently configurable ui_close_dialog action. |
| `public System.Boolean DialogHideOnOK { get; set; }` | Gets or sets whether accepting hides this dialog before the hook and Confirmed event. |
| `public System.String DialogText { get; set; }` | Gets or sets the message displayed by the owned label. |
| `public System.String OKButtonText { get; set; }` | Gets or sets the explicit OK caption; empty uses the subclass default. |

## Events

| Complete declaration | Contract |
| --- | --- |
| `public event System.Action Canceled` | Occurs after cancellation is scheduled, before the cancellation hook and deferred hiding. |
| `public event System.Action Confirmed` | Occurs after optional hiding and the confirmation hook. |
| `public event System.Action<System.String> CustomAction` | Occurs before the custom action hook; custom actions do not automatically hide. |

## Methods

| Complete declaration | Contract |
| --- | --- |
| `public Electron2D.Button AddButton(System.String text, System.Boolean right = false, System.String action = "")` | Adds an owned button and optional custom action on either side of the existing buttons. |
| `public Electron2D.Button AddCancelButton(System.String name)` | Adds an owned cancellation button using the configured or platform OK/Cancel order. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Inherited layout, lifetime or typed scene projection; see the owning base contract. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Inherited layout, lifetime or typed scene projection; see the owning base contract. |
| `public Electron2D.Label GetLabel()` | Returns the borrowed, required message label; hide it instead of removing or disposing it. |
| `public Electron2D.Button GetOKButton()` | Returns the borrowed, required OK button; its Disabled state also gates registered text submission. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited layout, lifetime or typed scene projection; see the owning base contract. |
| `protected virtual System.Void OnCancelPressed()` | Runs after Canceled and before deferred hiding. |
| `protected virtual System.Void OnCustomAction(System.String action)` | Runs after CustomAction without automatic hiding. |
| `protected override Electron2D.Vector2 OnGetContentsMinimumSize()` | Inherited layout, lifetime or typed scene projection; see the owning base contract. |
| `protected override System.Void OnInput(Electron2D.InputEvent input)` | Inherited layout, lifetime or typed scene projection; see the owning base contract. |
| `protected override System.Void OnNotification(System.Int32 what)` | Inherited layout, lifetime or typed scene projection; see the owning base contract. |
| `protected virtual System.Void OnOKPressed()` | Runs after optional hiding and before Confirmed. |
| `public System.Void RegisterTextEnter(Electron2D.LineEdit lineEdit)` | Connects a borrowed text field's submission to confirmation; repeated registration is idempotent. |
| `public System.Void RemoveButton(Electron2D.Button button)` | Detaches a custom button without disposing it and disconnects all dialog-owned callbacks. |
| `protected System.Void SetDefaultOKText(System.String text)` | Changes the default OK caption used when OKButtonText is empty. |

## Member reference

### .ctor

```csharp
public AcceptDialog()
```

Creates a hidden, exclusive, transient dialog with wrapped controls and the title Alert!.

### Canceled

```csharp
public event System.Action Canceled
```

Occurs after cancellation is scheduled, before the cancellation hook and deferred hiding.

### Confirmed

```csharp
public event System.Action Confirmed
```

Occurs after optional hiding and the confirmation hook.

### CustomAction

```csharp
public event System.Action<System.String> CustomAction
```

Occurs before the custom action hook; custom actions do not automatically hide.

### AddButton

```csharp
public Electron2D.Button AddButton(System.String text, System.Boolean right = false, System.String action = "")
```

Adds an owned button and optional custom action on either side of the existing buttons.

The borrowed button; RemoveButton transfers its ownership to the caller.

| Parameter | Meaning |
| --- | --- |
| `text` | The button caption. |
| `right` | True appends on the right; false prepends on the left before direction mirroring. |
| `action` | An action identifier; empty emits no custom action. |

| Exception | Trigger |
| --- | --- |
| `System.ArgumentNullException` | A string is null. |
| `System.AggregateException` | An insertion callback fails after the live button and action commit. |

### AddCancelButton

```csharp
public Electron2D.Button AddCancelButton(System.String name)
```

Adds an owned cancellation button using the configured or platform OK/Cancel order.

The borrowed cancellation button; RemoveButton transfers ownership.

| Parameter | Meaning |
| --- | --- |
| `name` | The caption; empty displays Cancel. |

| Exception | Trigger |
| --- | --- |
| `System.ArgumentNullException` | The name is null. |
| `System.AggregateException` | An insertion callback fails after the live cancel button commits. |

### CreateSceneInstanceFactory

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```

### Dispose

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

### GetLabel

```csharp
public Electron2D.Label GetLabel()
```

Returns the borrowed, required message label; hide it instead of removing or disposing it.

The stable internal message label.

### GetOKButton

```csharp
public Electron2D.Button GetOKButton()
```

Returns the borrowed, required OK button; its Disabled state also gates registered text submission.

The stable internal OK button.

### GetPropertyDescriptors

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

### OnCancelPressed

```csharp
protected virtual System.Void OnCancelPressed()
```

Runs after Canceled and before deferred hiding.

### OnCustomAction

```csharp
protected virtual System.Void OnCustomAction(System.String action)
```

Runs after CustomAction without automatic hiding.

| Parameter | Meaning |
| --- | --- |
| `action` | The custom button's action identifier. |

### OnGetContentsMinimumSize

```csharp
protected override Electron2D.Vector2 OnGetContentsMinimumSize()
```

### OnInput

```csharp
protected override System.Void OnInput(Electron2D.InputEvent input)
```

### OnNotification

```csharp
protected override System.Void OnNotification(System.Int32 what)
```

### OnOKPressed

```csharp
protected virtual System.Void OnOKPressed()
```

Runs after optional hiding and before Confirmed.

### RegisterTextEnter

```csharp
public System.Void RegisterTextEnter(Electron2D.LineEdit lineEdit)
```

Connects a borrowed text field's submission to confirmation; repeated registration is idempotent.

| Parameter | Meaning |
| --- | --- |
| `lineEdit` | A live text field, which need not be a direct child. |

| Exception | Trigger |
| --- | --- |
| `System.ArgumentNullException` | The field is null. |
| `System.ObjectDisposedException` | The field is disposed. |
| `System.InvalidOperationException` | The field belongs to a different scene owner thread. |

### RemoveButton

```csharp
public System.Void RemoveButton(Electron2D.Button button)
```

Detaches a custom button without disposing it and disconnects all dialog-owned callbacks.

| Parameter | Meaning |
| --- | --- |
| `button` | A live button added by AddButton or AddCancelButton. |

| Exception | Trigger |
| --- | --- |
| `System.ArgumentException` | The button is not owned by this row, or is the required OK button. |
| `System.ArgumentNullException` | The button is null. |
| `System.AggregateException` | A removal callback fails after the ownership transfer is completed. |

### SetDefaultOKText

```csharp
protected System.Void SetDefaultOKText(System.String text)
```

Changes the default OK caption used when OKButtonText is empty.

| Parameter | Meaning |
| --- | --- |
| `text` | The subclass default caption. |

| Exception | Trigger |
| --- | --- |
| `System.ArgumentNullException` | The caption is null. |

### DialogAutowrap

```csharp
public System.Boolean DialogAutowrap { get; set; }
```

Gets or sets whether the message wraps at word boundaries.

False initially.

### DialogCloseOnEscape

```csharp
public System.Boolean DialogCloseOnEscape { get; set; }
```

Gets or sets cancellation by the independently configurable ui_close_dialog action.

True initially; echoed keys are ignored.

### DialogHideOnOK

```csharp
public System.Boolean DialogHideOnOK { get; set; }
```

Gets or sets whether accepting hides this dialog before the hook and Confirmed event.

True initially; false permits validation in the confirmation handler.

### DialogText

```csharp
public System.String DialogText { get; set; }
```

Gets or sets the message displayed by the owned label.

An empty string initially.

| Exception | Trigger |
| --- | --- |
| `System.ArgumentNullException` | The message is null. |

### OKButtonText

```csharp
public System.String OKButtonText { get; set; }
```

Gets or sets the explicit OK caption; empty uses the subclass default.

An empty string initially, displaying OK.

| Exception | Trigger |
| --- | --- |
| `System.ArgumentNullException` | The caption is null. |


FileDialog uses the ordered native cancellation hook to emit Canceled and run OnCancelPressed without fabricating embedded visibility; callback failures remain aggregated. See the [file-dialog component](../components/file-dialogs.md) for its exercised flow and limits.
