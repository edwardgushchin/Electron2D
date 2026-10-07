# RichTextMetadata

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RichTextMetadata`. **Source:** [source](../../src/Scene/GUI/RichTextMetadata.cs). **Component:** [Rich text](../components/rich-text.md).

## Description

Immutable exact generic borrowed link metadata. PushMeta<T> creates one span identity; click and hover events receive it and TryGet<T> succeeds only for that stored type. BBCode url tags store strings. Values are runtime input payloads rather than file scene configuration.

## Members

| Declaration | Contract |
| --- | --- |
| [`public System.Boolean TryGet<T>(out T value)`](#member-1d78298620e6) | Reads the payload under its original exact type. |

## Member descriptions

<a id="member-1d78298620e6"></a>

### TryGet(ref T)

`public System.Boolean TryGet<T>(out T value)`

Reads the payload under its original exact type.

**T:** Stored type.

**Value:** Borrowed value or default.

**Returns:** Whether T matches.

## Verification and limits

See the rich-text component for actual tests, native artifacts, prepared allocation bounds and exact missing dependencies.
