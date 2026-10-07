# GlobalClassAttribute

**Declaration:** `public sealed class GlobalClassAttribute : Attribute` · **Source:** [Script.cs](../../src/Core/IO/Script.cs) · **Component:** [Scripting](../components/scripting.md)

`public GlobalClassAttribute()` declares the compiled class's global script name. Usage targets classes and is not inherited. Script.GetGlobalName returns the declared CLR class name. Stable source/archive registration IDs remain independently supplied to Script.RegisterNode/RegisterResource.
