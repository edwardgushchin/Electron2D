# FileDialogOptionValue

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public struct Electron2D.FileDialogOptionValue`. **Inherits:** `System.ValueType`. **Inherited By:** —. **Source:** [source](../../src/Scene/GUI/FileDialogOptionValue.cs). **Component:** [File dialogs](../components/file-dialogs.md).

Immutable result of a configured checkbox or choice. IsCheckbox distinguishes the case; SelectedIndex retains the checkbox 0/1 or selected choice index. Checked throws InvalidOperationException for a choice. The dialog returns an owned read-only dictionary snapshot, never object/Variant payloads.

## Properties

| Complete declaration | Contract |
| --- | --- |
| `public System.Boolean Checked { get;  }` | Gets the checkbox value. value: True when the checkbox is checked. System.InvalidOperationException: This result belongs to a choice option. |
| `public System.Boolean IsCheckbox { get;  }` | Reports whether this result is a checkbox rather than a choice index. value: The kind of the authoring option. |
| `public System.Int32 SelectedIndex { get;  }` | Gets the selected choice index, or zero/one for an unchecked/checked checkbox. value: The committed option result. |

## Storage, verification and decisions

FileDialogTests, DisplayServerDialogTests and FileDialogRenderingTests exercise the connected owner workflow, saved fresh-process scenes, current Linux native keyboard/pointer/pixels and retained rendering. Their exact measured boundaries and absent native chooser/platform prerequisites are recorded in the [component](../components/file-dialogs.md). [ADR 0051](../decisions/enum-identities.md#adr-0051) controls enum identity; [ADR 0095](../decisions/singleton-services.md#adr-0095) controls static retained service access.
