# ToolAttribute

**Declaration:** `public sealed class ToolAttribute : Attribute` · **Source:** [Script.cs](../../src/Core/IO/Script.cs) · **Component:** [Scripting](../components/scripting.md)

`public ToolAttribute()` marks a compiled project class as an editor tool declaration. Attribute usage targets classes and is inherited by derived project classes. Script.IsTool reads that metadata. This attribute does not install an editor scheduler or change ordinary Node execution.
