# ButtonActionMode

Last updated: 2026-09-27

**Declaration:** `public enum ButtonActionMode` · **Source:** [BaseButton.cs](../../src/Scene/GUI/BaseButton.cs)

Used by [BaseButton.ActionMode](BaseButton.md#actionmode). The enum is global because C# does not allow a property and nested type with the same name.

| Member | Value | Behavior |
| --- | ---: | --- |
| ButtonPress | 0 | Activate when eligible input is pressed. |
| ButtonRelease | 1 | Activate on release after an eligible press while still inside; default. |

## Member descriptions

### ButtonPress

The Pressed hook/event runs during the down edge. A toggle releases its attempt/contact after this activation while retaining persistent selection; eventual input release still ends holding.

### ButtonRelease

The Pressed hook/event runs before ButtonUp during the release edge. Releasing outside cancels activation independently of KeepPressedOutside.

Unknown numeric values are retained by the property and activate on neither edge. Shortcuts activate immediately on their pressed event independently of ActionMode.
