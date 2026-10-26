# AccessibilitySupport

Last updated: 2026-10-26

- Declaration: `public enum AccessibilitySupport`
- Source: [ProjectSettings.Accessibility.cs](../../src/Core/Config/ProjectSettings.Accessibility.cs)
- Used by: [ProjectSettings](ProjectSettings.md), [Native accessibility](../components/accessibility.md)

## Values

| Value | Number | Behavior |
| --- | ---: | --- |
| <a id="auto"></a>`Auto` | 0 | Samples the host accessibility-enabled setting when a display opens. |
| <a id="always"></a>`Always` | 1 | Publishes this application's automatic scene output without changing desktop settings. |
| <a id="disabled"></a>`Disabled` | 2 | No native bridge or automatic semantic output for this display lifetime. |

Use `ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always)` before Engine.Run. Auto is the registered default; invalid values reject without committing. Changes apply to the next display lifetime. Always requests output but cannot manufacture support when native libraries/bus are missing. Manual element producers may establish supported output in Auto even when the desktop setting is off. IsSupported alone does not enable accessibility-only focus.
