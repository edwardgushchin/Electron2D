# FileDialogOption

Last updated: 2026-10-28

**Namespace:** `Electron2D`. **Declaration:** `public sealed class FileDialogOption`. **Inherits:** `System.Object`. **Source:** [source](../../src/Scene/GUI/FileDialogOption.cs). **Component:** [native file choosers](../components/native-file-portal.md#native-checkbox-and-choice-controls).

Immutable input for DisplayServer.FileDialogWithOptionsShow. FileDialog constructs the same request description from its stored authoring options. This data value has no native owner or disposal lifecycle.

## API

| Declaration | Contract |
| --- | --- |
| `public FileDialogOption(string name, IReadOnlyList<string> values, int defaultValueIndex = 0)` | Copies nonnull/nonempty name and captions without NUL. Empty values create a checkbox. Clamps the initial index to 0/1 or the choice range. Null arguments/captions throw ArgumentNullException; empty/NUL strings throw ArgumentException. |
| `public string Name { get; }` | The immutable label and result key. Duplicate names are allowed; the last configured option determines the result value. |
| `public int DefaultValueIndex { get; }` | Clamped initial selection. |
| `public string[] GetValues()` | An independent caption copy; empty for a checkbox. |

## Verification and decisions

PortalFileChooserTests checks defensive copying, defaults, validation and actual native request/result behavior. The [component](../components/native-file-portal.md) records protocol, rendered-host and installed-portal evidence plus platform limits. [ADR 0004](../decisions/product.md#adr-0004) owns typed input projection and [ADR 0040](../decisions/display.md#adr-0040) owns the native dialog contract.
