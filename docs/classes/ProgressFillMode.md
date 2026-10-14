# ProgressFillMode

Last updated: 2026-10-14

Fill direction for [ProgressBar](ProgressBar.md). This namespace enum permits the FillMode property to keep its name in C#.

```csharp
public enum ProgressFillMode
```

| Value | Identity | Behavior |
| --- | ---: | --- |
| BeginToEnd | 0 | Follows horizontal reading direction. |
| EndToBegin | 1 | Opposes horizontal reading direction. |
| TopToBottom | 2 | Fills downward. |
| BottomToTop | 3 | Fills upward. |

Horizontal directions reverse under RTL. Undefined values reject before mutation; assigning any valid value resets the progress animation position. Both determinate and indeterminate drawing consume this setting.
