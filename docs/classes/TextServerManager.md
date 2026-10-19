# TextServerManager

Last updated: 2026-10-19

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [TextServerManager.cs](../../src/Servers/Text/TextServerManager.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class Electron2D.TextServerManager`

## Description

A permanent process-wide static service, registered with Engine under its type name. Discovery returns the same retained TextServer object without initializing native fonts. The built-in implementation is named FreeType/HarfBuzz. Index ordering is stable for this current single-interface profile. FindInterface uses exact ordinal names and returns null for an unknown name; invalid indexes throw. GetInterfaces returns an independent array. Manager/interface disposal is rejected. External provider registration, replacement and events retain their separate extension/ownership prerequisite; they are not no-op methods.

## Example

Complete service-discovery snippet.

```csharp
var text = TextServerManager.GetPrimaryInterface();
foreach (var provider in TextServerManager.GetInterfaces())
    Console.WriteLine(provider.GetName());
```

## Methods

| Signature | Purpose |
| --- | --- |
| `public static Electron2D.TextServer FindInterface(System.String name)` | Finds an interface by its exact name. |
| `public static Electron2D.TextServer GetInterface(System.Int32 index)` | Returns the number of registered interfaces. |
| `public static System.Int32 GetInterfaceCount()` | Returns the number of registered interfaces. |
| `public static Electron2D.TextServer[] GetInterfaces()` | Returns registered interfaces in current index order. |
| `public static Electron2D.TextServer GetPrimaryInterface()` | Returns the primary text implementation. |

## Protected integration

ValidateDisposal rejects disposal of the retained object. Native source ownership, font-cache/extension gaps and verification limits are described in [the component contract](../components/text-server.md).
