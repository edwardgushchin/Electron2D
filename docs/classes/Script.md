# Script

Registered extension shells form native bases for script metadata and construction. Their properties/methods are separate from Script declarations; `GetInstanceBaseType` returns the shell, and an unavailable shell makes `CanInstantiate` false. Validated `New`, copy and file factories bind that ancestor while preserving the exact Script association. See [extension class binding](../components/extensions.md#typed-class-binding).

Last updated: 2026-10-28

**Inherits:** [Resource](Resource.md), ElectronObject · **Inherited By:** —

**Declaration:** `public sealed partial class Script : Resource` · **Source:** [Script.cs](../../src/Core/IO/Script.cs), [Script.Catalog.cs](../../src/Core/IO/Script.Catalog.cs), [Script.RPC.cs](../../src/Core/IO/Script.RPC.cs) · **Component:** [Scripting](../components/scripting.md)

A compiled C# source asset for a registered project engine-object class or a live typed-binding code declaration. The host supplies stable identity, an optional static exact default factory and typed user property descriptors. Loading `.cs` verifies matching portable compiler symbols and source checksums. Project code remains in its assembly and executes normal typed callbacks.

```csharp
// At project startup; sourcePath belongs to the compiled project class:
Script.RegisterNode("game.Player", sourcePath, CreatePlayer, Player.HealthProperty);
using var script = ResourceLoader.Load<Script>(sourcePath);
using Player player = script.New<Player>();
// The player uses ordinary Node lifecycle and its declared C# fields/methods.
```

Constructed owners expose this exact borrowed asset through [ElectronObject.GetScript](ElectronObject.md#m-electron2d-electronobject-getscript), including inside their derived constructor. Copies and file scenes retain the source under [the compiled owner contract](../components/scripting.md#compiled-owner-source-identity).

[Compiled initialization](../components/scripting.md#compiled-c-initialization) uses ordinary C# constructors once, before schema/state restoration. Classes with required arguments may register a null default factory and use `New(() => new Player(arguments))`; implicit creation and absent reconstruction factories reject. Constructor exceptions retain their identity and ordinary C# partial-construction ownership.

## Plain object sources

RegisterObject covers compiled project classes directly based on ElectronObject and their project bases. Their ordinary methods/notifications, typed descriptors and initial GetScript identity use the same construction path. RegisterAbstract also accepts those bases. Node/Resource classes must use their dedicated registration methods so their file/scene factories are retained. Plain owners acquire no scene, Resource persistence or RPC role; their Script asset can still be copied or archived and create fresh owners. [The plain-object contract](../components/scripting.md#plain-object-sources) keeps live SetScript and source-free NativeAOT prerequisites separate.

## API summary

| Signature | Contract |
| --- | --- |
| `public Script()` | Unbound source asset; text initially empty. |
| `public static void RegisterNode<TNode>(string id, string sourcePath, Func<TNode>? factory, params PropertyDescriptor[] properties) where TNode : Node` | Registers source/schema; a null default factory requires explicit typed construction. |
| `public static void RegisterResource<TResource>(string id, string sourcePath, Func<TResource>? factory, params PropertyDescriptor[] properties) where TResource : Resource` | Same nullable default-factory contract for Resource/effect classes. |
| `public static void RegisterObject<TObject>(string id, string sourcePath, Func<TObject>? factory, params PropertyDescriptor[] properties) where TObject : ElectronObject` | Plain project objects outside Node/Resource; same source/schema/constructor policy, without an owner archive factory. |
| `public static void RegisterBinding<TOwner, TCode>(string id, string sourcePath, Func<TOwner>? factory, Action<TOwner> bind, Action<TOwner> clear, params PropertyDescriptor[] properties) where TOwner : ElectronObject where TCode : class` | Checked project code installs/retires typed functions on a stable ElectronObject/Node/Resource owner. Resource copies/cache replacement follow the [Resource binding contract](../components/scripting.md#resource-bindings); source assets cannot themselves own bindings. Its code type and owner type are separate; file factories remain explicit. |
| `public static void RegisterBindingWithRPC<TNode, TCode>(string id, string sourcePath, Func<TNode>? factory, Action<TNode> bind, Action<TNode> clear, ReadOnlySpan<RPCRegistration> rpcConfiguration, params PropertyDescriptor[] properties) where TNode : Node where TCode : class` | [Live code RPC declarations](../components/scripting.md#live-binding-rpc), validated against the stable owner role and retained with the installed binding. |
| `public static void RegisterAbstract<T>(string id, string sourcePath, params PropertyDescriptor[] properties) where T : ElectronObject` | Abstract project engine-object metadata without a factory. |
| `public static void RegisterNodeWithRPC<TNode>(string id, string sourcePath, Func<TNode>? factory, ReadOnlySpan<RPCRegistration> rpcConfiguration, params PropertyDescriptor[] properties) where TNode : Node` | Copies validated class RPC defaults with the existing exact source/factory registration. |
| `public static void RegisterAbstractWithRPC<TNode>(string id, string sourcePath, ReadOnlySpan<RPCRegistration> rpcConfiguration, params PropertyDescriptor[] properties) where TNode : Node` | Abstract Node source and inherited RPC defaults without a factory. |
| `public RPCRegistration[] GetRPCConfig()` | Effective code/base declarations followed by unshadowed owner-class defaults for live bindings; caller-owned snapshot. |
| `public string SourceCode { get; set; }` | Editable text; does not reload compiled implementation; eight MiB UTF-8 budget. |
| `public string CompiledTypeID { get; }` | Stable portable registration ID; empty when unbound. |
| `public Type? CompiledType { get; }` | Associated CLR class; null when unbound. |
| `public bool CanInstantiate()` | Concrete compiled-code and construction-role availability; abstract binding owners are attachment-only. Required arguments may still need an explicit typed constructor. |
| `public bool HasSourceCode()` | Nonempty resource text. |
| `public bool IsAbstract()` | Compiled class abstract flag; false when unbound. |
| `public bool IsTool()` | Inherited ToolAttribute declaration. |
| `public string GetGlobalName()` | CLR class name with its own GlobalClassAttribute; otherwise empty. |
| `public Type? GetInstanceBaseType()` | First inherited native runtime class. |
| `public Script? GetBaseScript()` | Cached source resource for a directly registered project base; null otherwise. |
| `public T New<T>() where T : ElectronObject` | Caller-owned exact instance via registered factory. |
| `public T New<T>(Func<T>? constructor) where T : ElectronObject` | Typed constructor arguments via direct C# expression; null uses default factory. |
| `public PropertyDescriptor[] GetScriptPropertyList()` | Copied explicit typed code schema including compatible registered bases. |
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

Live binding declarations may use runtime owner types. Their typed schema extends ordinary owner descriptors; compatible registered code bases merge with derived name priority. Binding initialization precedes typed copy/archive restoration, and discovery follows the installed registration through source edits or disposal. See [native property schemas](../components/scripting.md#native-property-schemas).

Compiled Node/resource registration follows the same static factory and portable ID rules as ResourceFileTypes. Properties must belong to the closed project class or project base and match constructed object descriptors. Factories must return a live exact class instance; a mismatched returned object is disposed. Project constructors may throw; no unknown object created only inside a throwing user delegate can be recovered by the runtime.

The source/type association is proven by assembly MVID and matching portable PDB documents. All partial documents retain compiler checksums and are validated on uncached source load. Missing/unmatched symbols, missing/changed source or an unknown registration fail before replacing a cached wrapper. Rebuild and restart publishes a different compiled cohort. Source editing/saving leaves the loaded class unchanged. New instances remain independent caller-owned objects and continue ordinary callbacks after Script disposal.

Defaults require a registered descriptor and compatible live owner. Missing declared defaults fail explicitly; borrowed resource values retain that owner's lifetime. Metadata queries allocate snapshots and do not execute a universal getter/caller. Private/project methods and events are represented as CLR metadata without treating native inherited members as script declarations. Constants merge inherited project declarations by name, with the most derived declaration first.

Unbound metadata returns empty/null/false as documented, and New fails. Disposed resources reject metadata, source and creation queries. Duplicate/archive identity shares the process-held compiled registration; archive loading requires the host's corresponding registration. Source saving uses the existing atomic resource write protocol. No CLR type reassignment, implementation Reload/keep-state migration or complete editor authoring is implemented by these source/factory APIs. Exact remaining dependencies stay in coverage and ADR 0091.

Ordinary nested Resource copies retain this same Script instance for all deep modes and path classifications. Explicit Script root/force duplication produces independent source wrappers borrowing the same registration. A prior forced copy never remaps ordinary script edges. Scene-local flags control per-scene copies; non-local script references remain shared, and isolated authoring preserves captured text independently of future caller edits. Scene disposal releases acquired wrappers while preserving borrowed scripts and other instances.

[ScriptTests](../../tests/Electron2D.Tests/ScriptTests.cs) is the executable source/type/factory/storage scenario; [Scripting](../components/scripting.md) records native, allocation and platform limits.

RegisterObject, RegisterNode, RegisterResource and RegisterAbstract declare their file-backed assembly/PDB and untrimmed metadata prerequisites. Registration captures complete project methods, events and literal fields once; queries copy that retained metadata. Source association in embedded NativeAOT assemblies remains unavailable. Static scene factories do not require source registration.

[Factory result ownership](../components/scripting.md#factory-result-ownership) protects borrowed physical/script resources and live nodes from invalid Script.New and file-factory results. CLR source-asset checks retain actual CPU/software-GPU/registered simulation and zero warmed managed bytes; NativeAOT source registration and full live-script/duplication acceptance remain open.

Factory results require [construction provenance](ElectronObject.md#factory-construction-provenance) in the current invocation. Unrelated concurrent construction cannot transfer caller ownership; nested factories, ordinary Task context flow and engine worker tasks remain supported.

## Compiled RPC defaults

RegisterNodeWithRPC/RegisterAbstractWithRPC validate compatible Node receiver roles, distinct method IDs, nonnull tokens and at most 65536 declarations before publishing registration. Inputs and metadata results are copied; immutable callback/codec tokens and their captured state remain catalog-owned for the process lifetime. No metadata prototype or constructor is invoked. Resource and unbound source assets return empty RPC snapshots.

Derived IDs shadow base declarations; a subsequently registered base is observed on later queries and calls. Ordinary construction, Script.New and file-backed scenes use these defaults through the existing Node multiplayer operations. Per-node RPCConfig changes override policies; null removes/suppresses a class default. These instance changes do not mutate this Script and must be authored again in the constructor/factory to survive reconstruction. Editing SourceCode leaves the compiled RPC declarations unchanged.

[ScriptRPCTests](../../tests/Electron2D.Tests/ScriptRPCTests.cs) verifies metadata without construction, inherited and late-base declarations, overrides/removal, invalid registration rollback, disposed capture release, compressed big-endian fresh-process scenes, real native WS/ENet calls and warmed owner-thread allocation. Its native window scenario reads actual RPC-changed pixels on software GPU and Compatibility renderers; foreign/hardware, routed transport and owner acceptance remain separate.
