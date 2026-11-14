# GlobalScriptClass

A snapshot entry returned by [ProjectSettings.GetGlobalClassList](ProjectSettings.md). The [scripting component](../components/scripting.md#global-class-discovery) defines which compiled registrations appear.

| Property | Value |
| --- | --- |
| Name | Declared global class name. |
| BaseType | Nearest global project class, engine class or registered extension base as a CLR type. |
| Language | C#. |
| Path | Source under the current project's resource root. Pass it to ResourceLoader.Load<Script>. |
| IconPath | Project-relative icon path, or empty. |
| IsAbstract | Whether the compiled class is abstract. |
| IsTool | Whether the class has its own or an inherited ToolAttribute. |
