# MenuButton

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.MenuButton`. **Inherits:** [Button](Button.md). **Inherited By:** —. **Source:** [model](../../src/Scene/GUI/MenuButton.cs), [storage](../../src/Scene/GUI/MenuButton.Storage.cs). **Component:** [Command menu buttons](../components/command-menu-buttons.md).

Owns a command PopupMenu with flat, press-edge, toggle and accessibility-focus defaults. GetPopup is a stable borrowed control. AboutToPopup precedes geometry/visibility, ShowPopup anchors beneath the button with RTL alignment, keyboard opening focuses the first eligible command and pointer opening retains grabbed-click behavior. Popup hiding synchronizes pressed state, including failed observers. ItemCount delegates to the live menu model; SetDisableShortcuts gates the owner shortcut stage, and individual menu records control allowed echo. Commands use PopupMenu ID/index events and keep check/state edits explicit.

SwitchOnHover replaces the open menu with a related enabled opt-in menu button during internal processing, without item focus. SceneTree's existing hit test supplies the underlying menu-bar hover while the popup owns input. Nested embedded routing and anchor handling execute through the common Window host; unrelated/disabled/hidden targets stay closed. Failed and reentrant presentation leaves state coherent and reports errors. Node configuration warnings combine button/popup queries.

Scenes store concrete defaults, SwitchOnHover, ItemCount and seven indexed popup fields. Each instance recreates its internal popup; runtime delegates, shortcut-disable policy, metadata and input state remain runtime-only. The full own API executes in the embedded profile. Inherited accessibility/editor/native menu/native child-window capabilities retain separate coverage prerequisites.

## Example

```csharp
var root = new Window { Size = new(640, 360), GUIEmbedSubwindows = true };
var menu = new MenuButton("File") { Position = new(20, 20), SwitchOnHover = true };
menu.GetPopup().AddItem("Open", 42);
menu.GetPopup().IDPressed += id => Console.WriteLine(id);
root.AddChild(menu);
Engine.Run(root);
```

MenuButtonTests covers defaults, commands, shortcuts/echo, pointer and hover gates, nested windows, callback failures/disposal/reentry and fresh-process scenes. MenuButtonRenderingTests verifies real SDL actions, toolbar/menu pixels and 64 warmed caption/focus/render frames on current Linux GPU/compatibility targets. See the component for precise verification limits.

## Constructors

| Complete declaration | Contract |
| --- | --- |
| `public MenuButton()` | Creates a flat, press-edge toggle button with accessibility focus and an owned popup. |
| `public MenuButton(System.String text)` | Creates a command menu button with source text. |

## Properties

| Complete declaration | Contract |
| --- | --- |
| `public System.Int32 ItemCount { get; set; }` | Gets or sets the owned popup's item count, retaining existing records. |
| `public System.Boolean SwitchOnHover { get; set; }` | Gets or sets switching to an enabled related menu button hovered while this menu is open. |

## Events

| Complete declaration | Contract |
| --- | --- |
| `public event System.Action AboutToPopup` | Occurs before popup geometry and visibility change; handlers may populate the owned menu. |

## Methods

| Complete declaration | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Inherited button/scene lifecycle and owned-popup projection. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Inherited button/scene lifecycle and owned-popup projection. |
| `public override System.String[] GetConfigurationWarnings()` | Inherited button/scene lifecycle and owned-popup projection. |
| `public Electron2D.PopupMenu GetPopup()` | Returns the stable borrowed internal popup; preserve its ownership and lifetime. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Inherited button/scene lifecycle and owned-popup projection. |
| `protected override System.Void OnNotification(System.Int32 what)` | Inherited button/scene lifecycle and owned-popup projection. |
| `protected override System.Void OnPressed()` | Inherited button/scene lifecycle and owned-popup projection. |
| `protected override System.Void OnShortcutInput(Electron2D.InputEvent input)` | Inherited button/scene lifecycle and owned-popup projection. |
| `public System.Void SetDisableShortcuts(System.Boolean disabled)` | Disables item shortcuts and the inherited button shortcut input stage. |
| `public System.Void ShowPopup()` | Shows the command menu below the button and focuses its first enabled nonseparator item for keyboard opening. |

## Member reference

### .ctor

```csharp
public MenuButton()
```

Creates a flat, press-edge toggle button with accessibility focus and an owned popup.

### .ctor

```csharp
public MenuButton(System.String text)
```

Creates a command menu button with source text.

| Parameter | Meaning |
| --- | --- |
| `text` | Nonnull caption text. |

### AboutToPopup

```csharp
public event System.Action AboutToPopup
```

Occurs before popup geometry and visibility change; handlers may populate the owned menu.

### CreateSceneInstanceFactory

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```

### Dispose

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

### GetConfigurationWarnings

```csharp
public override System.String[] GetConfigurationWarnings()
```

### GetPopup

```csharp
public Electron2D.PopupMenu GetPopup()
```

Returns the stable borrowed internal popup; preserve its ownership and lifetime.

The command menu.

### GetPropertyDescriptors

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

### OnNotification

```csharp
protected override System.Void OnNotification(System.Int32 what)
```

### OnPressed

```csharp
protected override System.Void OnPressed()
```

### OnShortcutInput

```csharp
protected override System.Void OnShortcutInput(Electron2D.InputEvent input)
```

### SetDisableShortcuts

```csharp
public System.Void SetDisableShortcuts(System.Boolean disabled)
```

Disables item shortcuts and the inherited button shortcut input stage.

| Parameter | Meaning |
| --- | --- |
| `disabled` | Whether shortcut input is disabled. |

### ShowPopup

```csharp
public System.Void ShowPopup()
```

Shows the command menu below the button and focuses its first enabled nonseparator item for keyboard opening.

Detached buttons return without opening. The popup fits the embedding viewport and uses its own minimum width; RTL aligns its trailing edge. Pointer opening retains the popup's grabbed-click policy.

| Exception | Meaning |
| --- | --- |
| `System.InvalidOperationException` | Presentation reenters AboutToPopup. |
| `System.AggregateException` | A presentation or state callback fails after required cleanup. |

### ItemCount

```csharp
public System.Int32 ItemCount { get; set; }
```

Gets or sets the owned popup's item count, retaining existing records.

Zero initially; the popup limit is 65536. New records are plain items with automatic IDs.

| Exception | Meaning |
| --- | --- |
| `System.ArgumentOutOfRangeException` | The count is outside the supported range. |

### SwitchOnHover

```csharp
public System.Boolean SwitchOnHover { get; set; }
```

Gets or sets switching to an enabled related menu button hovered while this menu is open.

False initially; both buttons must opt in.
