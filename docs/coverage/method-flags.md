# MethodFlags applicability

Last updated: 2026-11-16

The pinned declaration is [method_info.h, lines 36–46](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/object/method_info.h#L36-L46), revision `ed1daf0bf001b61586d9930840f2f1394092c079`. The nine values include the DEFAULT alias; they are not nine independent bits. The canonical decisions for all ten rows live in `tools/coverage/overrides_method_flags.json` and generate [@GlobalScope](classes/@GlobalScope.md).

[ADR 0001](../decisions/product.md#adr-0001) uses typed C#; [0002](../decisions/product.md#adr-0002) and [0010](../decisions/core-object-runtime.md#adr-0010) use typed events and owned connections. None requires dynamic method invocation for a signal target. [0091](../decisions/scripting.md#adr-0091) nevertheless retains explicit cold Script metadata. Excluding Object's dynamic `get_method_list` therefore does not exclude Script's applicable method properties.

| Declaration | State | Semantic result and consumer |
| --- | --- | --- |
| MethodFlags | Excluded | Numeric binding transport, not a missing CLR enum. Individual applicable properties remain below. |
| NORMAL = 1 | Excluded | Default binding hint in MethodInfo/MethodBind. May coexist with static/const bits; not a predicate for a nonvirtual instance method. |
| DEFAULT = 1 | Excluded | NORMAL alias, not optional argument defaults. CLR ParameterInfo retains those separately. |
| EDITOR = 2 | Blocked | Editor-method classification has no proven CLR projection. ToolAttribute describes a class and IsEditorHint describes a host. Trigger: an ADR 0027 editor method discovery/action consumer defining and checking per-method eligibility with Script metadata. |
| CONST = 4 | Excluded | C++ receiver qualifier from MethodBind, not purity, IsStatic, readonly parameters or a getter. DocData prints it and GDScript enum completion filters it. C# has no const class receiver contract; each typed query keeps its own behavior requirements. |
| VIRTUAL = 8 | Implemented | MethodInfo.IsVirtual, IsFinal and GetBaseDefinition describe project virtual declarations/overrides. A sealed override remains CLR virtual but cannot be overridden again. This does not automatically identify an engine callback. |
| VARARG = 16 | Implemented | Variable argument syntax adapts to typed params. ParameterInfo's ParamArrayAttribute distinguishes it from a fixed array; CallingConventions.VarArgs does not identify C# params. Arbitrary Variant arguments remain outside ADR 0001. |
| STATIC = 32 | Implemented | MethodInfo.IsStatic and compiled receiver-free calls. Both static and instance declarations survive registration. |
| OBJECT_CORE = 64 | Excluded | ClassDB tags core virtuals so the extension JSON API dump omits them. CLR declaring type is provenance, not an export suppression bit. No such binding dump is required by ADR 0091. |
| VIRTUAL_REQUIRED = 128 | Implemented | IsAbstract identifies a required implementation; the compiler enforces it. A concrete override is not abstract. |

No implementation trigger applies to the five excluded transport/language/export rows. EDITOR remains in the denominator. This audit adds four Implemented rows, removes five applicable rows and moves one row to Blocked: **14,467/18,339 = 78.89%**, with 570 Partial, 133 Unimplemented, 3,169 Blocked, 8,954 Excluded and zero unmapped declarations. The actual baseline register was 14,463/18,344; older introductory checkpoints in the index are historical.

## Producers and consumers

All links below use the same pin:

- [MethodBind hint composition](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/object/method_bind.h#L106) combines default, const, vararg and static information.
- [ClassDB virtual registration](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/object/class_db.cpp#L1961-L1974) sets virtual/core flags.
- [DocData qualifiers](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/doc_data.cpp#L129-L159) consume virtual, required, const, vararg and static.
- [Extension API export](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/extension/extension_api_dump.cpp#L1028-L1035) consumes core suppression and required/static/const qualifiers.
- [GDScript analyzer](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/gdscript/gdscript_analyzer.cpp#L3636-L3662) checks static and virtual call rules and variable argument counts. Its interpreter-specific behavior is outside the C# language boundary.

## Public consumer and lifetime

`ScriptTests.MethodMetadata` uses public registration, ResourceLoader, Script.New, duplication and GetScriptMethodList. It checks ordinary/default-argument signatures, public/private methods, static/instance methods, inherited virtual and abstract declarations, sealed implementation, params versus fixed arrays and zero/multiple argument compiled calls. Accessors and engine-base methods stay outside project metadata. Snapshot mutation cannot alter the catalog or a duplicate wrapper. Source text edits do not replace the compiled cohort; disposing a wrapper rejects new queries while acquired MethodInfo and compiled instances remain valid. Duplicate registration rejects; an unbound Script returns an empty list.

Registration reflects the complete project declaration family once. It requires a file-backed compiled assembly, matching portable PDBs and untrimmed method/event/constant declarations, as stated by RequiresAssemblyFiles and RequiresUnreferencedCode. Script source registration in an embedded NativeAOT image is not supported by this contract. Reading an existing snapshot does not establish trim or AOT discovery support. Ordinary source-free compiled callbacks remain a separate capability. No runtime implementation or public API shape changes in this audit.
