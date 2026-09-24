# LayoutPresetMode

Last updated: 2026-09-24

**Inherits:** —

- **Source:** [`src/Scene/GUI/LayoutPresetMode.cs`](../../src/Scene/GUI/LayoutPresetMode.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum LayoutPresetMode`

Controls which dimensions [`Control.SetOffsetsPreset`](Control.md) and `SetAnchorsAndOffsetsPreset` preserve when placing a control.

| Value | Number | Width | Height |
| --- | ---: | --- | --- |
| `MinSize` | 0 | Intrinsic minimum | Intrinsic minimum |
| `KeepWidth` | 1 | Current size | Intrinsic minimum |
| `KeepHeight` | 2 | Intrinsic minimum | Current size |
| `KeepSize` | 3 | Current size | Current size |

The default is `MinSize`. These modes use `GetMinimumSize`, not `CustomMinimumSize`. A wide preset spans its selected parent axis regardless of the corresponding mode. `ControlLayoutTests` exercises every mode with all sixteen presets in LTR and RTL.
