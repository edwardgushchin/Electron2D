# MenuBar

Last updated: 2026-10-10

**Namespace:** `Electron2D`. **Declaration:** `public partial class Electron2D.MenuBar : Control`. **Source:** [model](../../src/Scene/GUI/MenuBar.cs), [layout](../../src/Scene/GUI/MenuBar.Layout.cs), [input](../../src/Scene/GUI/MenuBar.Input.cs), [storage](../../src/Scene/GUI/MenuBar.Storage.cs). **Component:** [Menu strips](../components/menu-strips.md).

## Overview

Displays a themed horizontal menu strip for its direct ordinary PopupMenu children.

## Syntax

```csharp
public class Electron2D.MenuBar
```

### Remarks

Menus retain child identity across reordering. Titles follow each popup's Title or Name unless explicitly overridden. Header scene fields follow their popup when packing prunes siblings. Popup nodes retain normal scene ownership. Presentation uses the embedding viewport; system global menus require a native menu backend. Attached access requires the scene owner thread.

**Inherits:** [Control](Control.md)

[Detailed engine reference](https://github.com/edwardgushchin/Electron2D/blob/main/docs/classes/MenuBar.md)

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`public MenuBar()`](#member-f6d0352e29c1) | constructor | Creates an empty menu strip with accessibility focus and shortcut input enabled. |
| [`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`](#member-26e1c66eb814) | method | Inherited control/scene hook implementing menu layout, input, storage or lifecycle. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-b0f3981fb1b4) | method | Inherited control/scene hook implementing menu layout, input, storage or lifecycle. |
| [`public System.Int32 GetMenuCount()`](#member-16667b107381) | method | Returns the number of direct ordinary PopupMenu children, including hidden and disabled menus. |
| [`public Electron2D.PopupMenu GetMenuPopup(System.Int32 menu)`](#member-161dce18e2a5) | method | Returns a borrowed popup without changing its parent or lifetime. |
| [`public System.String GetMenuTitle(System.Int32 menu)`](#member-94bdaac69abc) | method | Returns the explicit title or the child's current Title/Name. |
| [`public System.String GetMenuTooltip(System.Int32 menu)`](#member-3c157eb48234) | method | Returns one menu's tooltip. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-283a704948eb) | method | Inherited control/scene hook implementing menu layout, input, storage or lifecycle. |
| [`public System.Boolean IsMenuDisabled(System.Int32 menu)`](#member-11b543773acc) | method | Returns whether user opening and shortcuts are disabled for one menu. |
| [`public System.Boolean IsMenuHidden(System.Int32 menu)`](#member-94deee251dc7) | method | Returns whether one menu is omitted from the strip and shortcuts. |
| [`public System.Boolean IsNativeMenu()`](#member-cd7b1d2c755b) | method | Reports whether this strip currently uses a system global menu. |
| [`protected override System.Void OnGUIInput(Electron2D.InputEvent input)`](#member-657f4844fbde) | method | Inherited control/scene hook implementing menu layout, input, storage or lifecycle. |
| [`protected override Electron2D.Vector2 OnGetMinimumSize()`](#member-d4f7ff75889f) | method | Inherited control/scene hook implementing menu layout, input, storage or lifecycle. |
| [`protected override System.String OnGetTooltip(Electron2D.Vector2 atPosition)`](#member-bce4cf303fb7) | method | Inherited control/scene hook implementing menu layout, input, storage or lifecycle. |
| [`protected override System.Void OnNotification(System.Int32 what)`](#member-0e7cfc474fea) | method | Inherited control/scene hook implementing menu layout, input, storage or lifecycle. |
| [`protected override System.Void OnShortcutInput(Electron2D.InputEvent input)`](#member-b09c45a4697e) | method | Inherited control/scene hook implementing menu layout, input, storage or lifecycle. |
| [`public System.Void SetDisableShortcuts(System.Boolean disabled)`](#member-5b4fa1d451ff) | method | Disables the owner shortcut stage without changing individual popup item records. |
| [`public System.Void SetMenuDisabled(System.Int32 menu, System.Boolean disabled)`](#member-4e892e06c5a3) | method | Changes the disabled flag and closes this menu if necessary. |
| [`public System.Void SetMenuHidden(System.Int32 menu, System.Boolean hidden)`](#member-1f3f32a56417) | method | Changes header visibility without changing child membership. |
| [`public System.Void SetMenuTitle(System.Int32 menu, System.String title)`](#member-b247a91e66e1) | method | Overrides one menu's title; matching the child's current Title/Name restores automatic naming. |
| [`public System.Void SetMenuTooltip(System.Int32 menu, System.String tooltip)`](#member-4361b2daf742) | method | Changes one menu's tooltip. |
| [`protected override System.Void ValidateDisposal()`](#member-15c03cea707e) | method | Inherited control/scene hook implementing menu layout, input, storage or lifecycle. |
| [`protected override System.Void ValidateMutation()`](#member-fac331a622a5) | method | Inherited control/scene hook implementing menu layout, input, storage or lifecycle. |
| [`public System.Boolean Flat { get; set; }`](#member-17687b9e7263) | property | Gets or sets suppression of item decorations while retaining text and layout margins. |
| [`public System.String Language { get; set; }`](#member-296f9787b1aa) | property | Gets or sets the shaping language; empty uses the current translation locale. |
| [`public System.Boolean SwitchOnHover { get; set; }`](#member-994778c94772) | property | Gets or sets hover switching while a menu in this strip is open. |
| [`public Electron2D.TextDirection TextDirection { get; set; }`](#member-61a0c49e9c07) | property | Gets or sets the base writing direction independently of visual layout direction. |

## Member Details

<a id="member-f6d0352e29c1"></a>
### `MenuBar()`

Kind: `constructor`

```csharp
public MenuBar()
```

#### Summary

Creates an empty menu strip with accessibility focus and shortcut input enabled.

<a id="member-26e1c66eb814"></a>
### `CreateSceneInstanceFactory()`

Kind: `method`

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```

<a id="member-b0f3981fb1b4"></a>
### `Dispose(System.Boolean)`

Kind: `method`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

<a id="member-16667b107381"></a>
### `GetMenuCount()`

Kind: `method`

```csharp
public System.Int32 GetMenuCount()
```

#### Summary

Returns the number of direct ordinary PopupMenu children, including hidden and disabled menus.

#### Returns

The current menu count.

<a id="member-161dce18e2a5"></a>
### `GetMenuPopup(System.Int32)`

Kind: `method`

```csharp
public Electron2D.PopupMenu GetMenuPopup(System.Int32 menu)
```

#### Summary

Returns a borrowed popup without changing its parent or lifetime.

#### Returns

The corresponding child.

#### Parameters

- `menu`: A valid zero-based menu index.

<a id="member-94bdaac69abc"></a>
### `GetMenuTitle(System.Int32)`

Kind: `method`

```csharp
public System.String GetMenuTitle(System.Int32 menu)
```

#### Summary

Returns the explicit title or the child's current Title/Name.

#### Returns

The untranslated title.

#### Parameters

- `menu`: A valid menu index.

<a id="member-3c157eb48234"></a>
### `GetMenuTooltip(System.Int32)`

Kind: `method`

```csharp
public System.String GetMenuTooltip(System.Int32 menu)
```

#### Summary

Returns one menu's tooltip.

#### Returns

Empty initially.

#### Parameters

- `menu`: A valid menu index.

<a id="member-283a704948eb"></a>
### `GetPropertyDescriptors()`

Kind: `method`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-11b543773acc"></a>
### `IsMenuDisabled(System.Int32)`

Kind: `method`

```csharp
public System.Boolean IsMenuDisabled(System.Int32 menu)
```

#### Summary

Returns whether user opening and shortcuts are disabled for one menu.

#### Returns

False initially.

#### Parameters

- `menu`: A valid menu index.

<a id="member-94deee251dc7"></a>
### `IsMenuHidden(System.Int32)`

Kind: `method`

```csharp
public System.Boolean IsMenuHidden(System.Int32 menu)
```

#### Summary

Returns whether one menu is omitted from the strip and shortcuts.

#### Returns

False initially.

#### Parameters

- `menu`: A valid menu index.

<a id="member-cd7b1d2c755b"></a>
### `IsNativeMenu()`

Kind: `method`

```csharp
public System.Boolean IsNativeMenu()
```

#### Summary

Reports whether this strip currently uses a system global menu.

#### Returns

False for the current embedded menu backend.

<a id="member-657f4844fbde"></a>
### `OnGUIInput(Electron2D.InputEvent)`

Kind: `method`

```csharp
protected override System.Void OnGUIInput(Electron2D.InputEvent input)
```

<a id="member-d4f7ff75889f"></a>
### `OnGetMinimumSize()`

Kind: `method`

```csharp
protected override Electron2D.Vector2 OnGetMinimumSize()
```

<a id="member-bce4cf303fb7"></a>
### `OnGetTooltip(Electron2D.Vector2)`

Kind: `method`

```csharp
protected override System.String OnGetTooltip(Electron2D.Vector2 atPosition)
```

<a id="member-0e7cfc474fea"></a>
### `OnNotification(System.Int32)`

Kind: `method`

```csharp
protected override System.Void OnNotification(System.Int32 what)
```

<a id="member-b09c45a4697e"></a>
### `OnShortcutInput(Electron2D.InputEvent)`

Kind: `method`

```csharp
protected override System.Void OnShortcutInput(Electron2D.InputEvent input)
```

<a id="member-5b4fa1d451ff"></a>
### `SetDisableShortcuts(System.Boolean)`

Kind: `method`

```csharp
public System.Void SetDisableShortcuts(System.Boolean disabled)
```

#### Summary

Disables the owner shortcut stage without changing individual popup item records.

#### Parameters

- `disabled`: Whether shortcuts are disabled.

<a id="member-4e892e06c5a3"></a>
### `SetMenuDisabled(System.Int32, System.Boolean)`

Kind: `method`

```csharp
public System.Void SetMenuDisabled(System.Int32 menu, System.Boolean disabled)
```

#### Summary

Changes the disabled flag and closes this menu if necessary.

#### Parameters

- `menu`: A valid menu index.
- `disabled`: Whether interaction is disabled.

<a id="member-1f3f32a56417"></a>
### `SetMenuHidden(System.Int32, System.Boolean)`

Kind: `method`

```csharp
public System.Void SetMenuHidden(System.Int32 menu, System.Boolean hidden)
```

#### Summary

Changes header visibility without changing child membership.

#### Parameters

- `menu`: A valid menu index.
- `hidden`: Whether the header is hidden.

<a id="member-b247a91e66e1"></a>
### `SetMenuTitle(System.Int32, System.String)`

Kind: `method`

```csharp
public System.Void SetMenuTitle(System.Int32 menu, System.String title)
```

#### Summary

Overrides one menu's title; matching the child's current Title/Name restores automatic naming.

#### Parameters

- `menu`: A valid menu index.
- `title`: Nonnull source text.

<a id="member-4361b2daf742"></a>
### `SetMenuTooltip(System.Int32, System.String)`

Kind: `method`

```csharp
public System.Void SetMenuTooltip(System.Int32 menu, System.String tooltip)
```

#### Summary

Changes one menu's tooltip.

#### Parameters

- `menu`: A valid menu index.
- `tooltip`: Nonnull source text.

<a id="member-15c03cea707e"></a>
### `ValidateDisposal()`

Kind: `method`

```csharp
protected override System.Void ValidateDisposal()
```

<a id="member-fac331a622a5"></a>
### `ValidateMutation()`

Kind: `method`

```csharp
protected override System.Void ValidateMutation()
```

<a id="member-17687b9e7263"></a>
### `Flat`

Kind: `property`

```csharp
public System.Boolean Flat { get; set; }
```

#### Summary

Gets or sets suppression of item decorations while retaining text and layout margins.

#### Value

False initially.

<a id="member-296f9787b1aa"></a>
### `Language`

Kind: `property`

```csharp
public System.String Language { get; set; }
```

#### Summary

Gets or sets the shaping language; empty uses the current translation locale.

#### Value

Empty initially. NUL is rejected.

<a id="member-994778c94772"></a>
### `SwitchOnHover`

Kind: `property`

```csharp
public System.Boolean SwitchOnHover { get; set; }
```

#### Summary

Gets or sets hover switching while a menu in this strip is open.

#### Value

True initially. Hidden, disabled and obscured targets are skipped.

<a id="member-61a0c49e9c07"></a>
### `TextDirection`

Kind: `property`

```csharp
public Electron2D.TextDirection TextDirection { get; set; }
```

#### Summary

Gets or sets the base writing direction independently of visual layout direction.

#### Value

Auto initially; Inherited follows layout direction.
