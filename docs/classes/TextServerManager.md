# TextServerManager

Last updated: 2026-10-29

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Source:** [TextServerManager.cs](../../src/Servers/Text/TextServerManager.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class Electron2D.TextServerManager`

## Description

A permanent process-wide static service registered with Engine. Its initial ordered registrations are TextServerAdvanced, TextServerFallback and TextServerDummy, with Advanced selected; additional instances remain independent. ProjectSettings.TextDriver can select a registered exact name before startup themes and the main loop. Discovery returns retained TextServer objects without initializing native fonts. Registration appends once, exact ordinal name lookup returns the first match, and GetInterfaces copies the ordered array. AddInterface/RemoveInterface publish committed registration events. SetPrimaryInterface selects a live registered or unregistered implementation; null clears selection and makes GetPrimaryInterface fail until another selection. Active consumer reads prevent selection/removal/disposal. Removal runs cleanup before committing removal and preserves cleanup/event errors. External objects remain caller-owned. See the [complete provider lifetime contract](../components/text-server.md#external-implementations).

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
| `public static Electron2D.TextServer GetInterface(System.Int32 index)` | Returns the registered interface at the current index. |
| `public static System.Int32 GetInterfaceCount()` | Returns the number of registered interfaces. |
| `public static Electron2D.TextServer[] GetInterfaces()` | Returns registered interfaces in current index order. |
| `public static Electron2D.TextServer GetPrimaryInterface()` | Returns the primary text implementation. |
| `public static System.Void AddInterface(Electron2D.TextServer implementation)` | Retains a registration and delivers InterfaceAdded after insertion. |
| `public static System.Void RemoveInterface(Electron2D.TextServer implementation)` | Cleans up and removes a quiescent nonprimary registration. |
| `public static System.Void SetPrimaryInterface(Electron2D.TextServer implementation)` | Selects or clears the primary and invalidates provider-dependent caches. |

## Events

| Signature | Purpose |
| --- | --- |
| `public static event System.Action<System.String> InterfaceAdded` | Reports the name after registration commits. |
| `public static event System.Action<System.String> InterfaceRemoved` | Reports the name after removal commits, including cleanup failure. |

## Protected integration

ValidateDisposal rejects disposal of the retained object. Native source ownership, external callback lifetimes and verification limits are described in [the component contract](../components/text-server.md).
