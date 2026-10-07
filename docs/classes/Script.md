# Script

Last updated: 2026-10-07

**Inherits:** [Resource](Resource.md), ElectronObject · **Inherited By:** —

**Declaration:** `public sealed partial class Script : Resource` · **Source:** [Script.cs](../../src/Core/IO/Script.cs), [Script.Catalog.cs](../../src/Core/IO/Script.Catalog.cs) · **Component:** [Scripting](../components/scripting.md)

A compiled C# source asset for a registered project Node/Resource class. The host supplies stable identity, a static exact factory and typed user property descriptors. Loading `.cs` verifies matching portable compiler symbols and source checksums. Project code remains in its assembly and executes normal typed callbacks.

```csharp
// At project startup; sourcePath belongs to the compiled project class:
Script.RegisterNode("game.Player", sourcePath, CreatePlayer, Player.HealthProperty);
using var script = ResourceLoader.Load<Script>(sourcePath);
using Player player = script.New<Player>();
// The player uses ordinary Node lifecycle and its declared C# fields/methods.
```

## API summary

| Signature | Contract |
| --- | --- |
| `public Script()` | Unbound source asset; text initially empty. |
| `public static void RegisterNode<TNode>(string id, string sourcePath, Func<TNode> factory, params PropertyDescriptor[] properties) where TNode : Node` | Registers source association, static exact scene factory and user schema. |
| `public static void RegisterResource<TResource>(string id, string sourcePath, Func<TResource> factory, params PropertyDescriptor[] properties) where TResource : Resource` | Same contract for archived Resource/effect instances. |
| `public static void RegisterAbstract<T>(string id, string sourcePath, params PropertyDescriptor[] properties) where T : ElectronObject` | Abstract project Node/Resource metadata without a factory. |
| `public string SourceCode { get; set; }` | Editable text; does not reload compiled implementation; eight MiB UTF-8 budget. |
| `public string CompiledTypeID { get; }` | Stable portable registration ID; empty when unbound. |
| `public Type? CompiledType { get; }` | Associated CLR class; null when unbound. |
| `public bool CanInstantiate()` | Concrete factory availability; constructor failure remains possible. |
| `public bool HasSourceCode()` | Nonempty resource text. |
| `public bool IsAbstract()` | Compiled class abstract flag; false when unbound. |
| `public bool IsTool()` | Inherited ToolAttribute declaration. |
| `public string GetGlobalName()` | CLR class name with its own GlobalClassAttribute; otherwise empty. |
| `public Type? GetInstanceBaseType()` | First inherited native runtime class. |
| `public Script? GetBaseScript()` | Cached source resource for a directly registered project base; null otherwise. |
| `public T New<T>() where T : ElectronObject` | Caller-owned exact instance via registered factory. |
| `public T New<T>(Func<T>? constructor) where T : ElectronObject` | Typed constructor arguments via direct C# expression; null uses default factory. |
| `public PropertyDescriptor[] GetScriptPropertyList()` | Copied explicit typed user schema including registered project bases. |
| `public TValue GetPropertyDefaultValue<TOwner,TValue>(PropertyDescriptor<TOwner,TValue> property, TOwner owner) where TOwner : ElectronObject` | Declared typed default in a live compatible owner context. |
| `public MethodInfo[] GetScriptMethodList()` | Copied CLR project method metadata, including project bases. |
| `public bool HasScriptMethod(string methodName)` | Project method-name query; no invocation. |
| `public EventInfo[] GetScriptSignalList()` | Copied CLR project event metadata. |
| `public bool HasScriptSignal(string signalName)` | Project event-name query. |
| `public Dictionary<string,TValue> GetScriptConstantMap<TValue>()` | Copied constants of one exact value type; object queries rejected. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Inherited storage plus source text and compiled type ID. |
| `protected override Resource CreateDuplicateInstance()` | Exact Script duplicate factory. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?,Resource?> duplicate, Func<Resource?,Resource?> force)` | Copies text and immutable registration identity. |
| `protected override void Dispose(bool disposing)` | Clears instance metadata/text; compiled catalog remains process-held. |

## Contracts and limitations

Node/resource registration follows the same static factory and portable ID rules as ResourceFileTypes. Properties must belong to the closed project class or project base and match constructed object descriptors. Factories must return a live exact class instance; a mismatched returned object is disposed. Project constructors may throw; no unknown object created only inside a throwing user delegate can be recovered by the runtime.

The source/type association is proven by assembly MVID and matching portable PDB documents. All partial documents retain compiler checksums and are validated on uncached source load. Missing/unmatched symbols, missing/changed source or an unknown registration fail before replacing a cached wrapper. Rebuild and restart publishes a different compiled cohort. Source editing/saving leaves the loaded class unchanged. New instances remain independent caller-owned objects and continue ordinary callbacks after Script disposal.

Defaults require a registered descriptor and compatible live owner. Missing declared defaults fail explicitly; borrowed resource values retain that owner's lifetime. Metadata queries allocate snapshots and do not execute a universal getter/caller. Private/project methods and events are represented as CLR metadata without treating native inherited members as script declarations. Constants merge inherited project declarations by name, with the most derived declaration first.

Unbound metadata returns empty/null/false as documented, and New fails. Disposed resources reject metadata, source and creation queries. Duplicate/archive identity shares the process-held compiled registration; archive loading requires the host's corresponding registration. Source saving uses the existing atomic resource write protocol. No CLR type reassignment, implementation Reload/keep-state migration, scene RPC configuration or complete editor authoring is implemented by these source/factory APIs. Exact remaining dependencies stay in coverage and ADR 0091.

[ScriptTests](../../tests/Electron2D.Tests/ScriptTests.cs) is the executable source/type/factory/storage scenario; [Scripting](../components/scripting.md) records native, allocation and platform limits.
