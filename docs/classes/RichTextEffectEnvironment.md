# RichTextEffectEnvironment

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.RichTextEffectEnvironment`. **Source:** [source](../../src/Scene/Resources/RichTextEffectEnvironment.cs). **Component:** [Rich text](../components/rich-text.md).

## Description

Dedicated named effect arguments with exact generic types, borrowed value ownership, numeric coercion and mixed array element queries. Set<T>/TryGet<T> retain the exact T; AddElement<T>/TryGetElement<T> express mixed arrays. ParseExpressionsForValues uses long integers, double fractions, bool, Color and string node paths; malformed name=value assignments end parsing after earlier entries. This context is runtime effect input, not a universal scene property or dynamic invocation protocol.

## Members

| Declaration | Contract |
| --- | --- |
| [`public RichTextEffectEnvironment()`](#member-eb9539641f1e) | Creates an empty argument context. |
| [`public System.Void AddElement<T>(System.String name, T value)`](#member-5675de7014f9) | Appends one exact typed value to a mixed argument array. |
| [`public System.Void Clear()`](#member-a168622ff60e) | Clears all arguments without disposing their values. |
| [`public System.Int32 GetElementCount(System.String name)`](#member-d165fa08e7a1) | Gets the length of a mixed argument array, or zero for another value. |
| [`public System.String[] GetKeys()`](#member-f63f9da33e37) | Gets an independent key array. |
| [`public System.Boolean Remove(System.String name)`](#member-4f6ada8b859c) | Removes a named argument. |
| [`public System.Void Set<T>(System.String name, T value)`](#member-723499bdef11) | Sets a borrowed argument under its exact type. |
| [`public System.Boolean TryGetElement<T>(System.String name, System.Int32 index, out T value)`](#member-839e1b5c4162) | Reads one mixed array element under its exact type. |
| [`public System.Boolean TryGetNumber(System.String name, out System.Double value)`](#member-4ee8b2f506da) | Reads a numeric parameter using the supported numeric argument representations. |
| [`public System.Boolean TryGet<T>(System.String name, out T value)`](#member-efd5fba7ba23) | Reads an argument only when its stored type is exactly T. |
| [`public System.Int32 Count { get;  }`](#member-ee0e9f205c55) | Gets the number of named arguments. |

## Member descriptions

<a id="member-eb9539641f1e"></a>

### RichTextEffectEnvironment()

`public RichTextEffectEnvironment()`

Creates an empty argument context.

<a id="member-5675de7014f9"></a>

### AddElement(System.String, T)

`public System.Void AddElement<T>(System.String name, T value)`

Appends one exact typed value to a mixed argument array.

**T:** Element type.

**Name:** Nonnull name.

**Value:** Borrowed element.

<a id="member-a168622ff60e"></a>

### Clear()

`public System.Void Clear()`

Clears all arguments without disposing their values.

<a id="member-d165fa08e7a1"></a>

### GetElementCount(System.String)

`public System.Int32 GetElementCount(System.String name)`

Gets the length of a mixed argument array, or zero for another value.

**Name:** Nonnull name.

**Returns:** Element count.

<a id="member-f63f9da33e37"></a>

### GetKeys()

`public System.String[] GetKeys()`

Gets an independent key array.

**Returns:** Ordinal argument names.

<a id="member-4f6ada8b859c"></a>

### Remove(System.String)

`public System.Boolean Remove(System.String name)`

Removes a named argument.

**Name:** Nonnull name.

**Returns:** Whether it existed.

<a id="member-723499bdef11"></a>

### Set(System.String, T)

`public System.Void Set<T>(System.String name, T value)`

Sets a borrowed argument under its exact type.

**T:** Argument type.

**Name:** Nonnull name.

**Value:** Borrowed value.

<a id="member-839e1b5c4162"></a>

### TryGetElement(System.String, System.Int32, ref T)

`public System.Boolean TryGetElement<T>(System.String name, System.Int32 index, out T value)`

Reads one mixed array element under its exact type.

**T:** Stored type.

**Name:** Argument name.

**Index:** Nonnegative element index.

**Value:** Borrowed element or default.

**Returns:** Whether that exact element exists.

<a id="member-4ee8b2f506da"></a>

### TryGetNumber(System.String, ref System.Double)

`public System.Boolean TryGetNumber(System.String name, out System.Double value)`

Reads a numeric parameter using the supported numeric argument representations.

**Name:** Argument name.

**Value:** Number or zero.

**Returns:** Whether a numeric value exists.

<a id="member-efd5fba7ba23"></a>

### TryGet(System.String, ref T)

`public System.Boolean TryGet<T>(System.String name, out T value)`

Reads an argument only when its stored type is exactly T.

**T:** Stored type.

**Name:** Nonnull name.

**Value:** Borrowed value or default.

**Returns:** Whether the exact value exists.

<a id="member-ee0e9f205c55"></a>

### Count

`public System.Int32 Count { get;  }`

Gets the number of named arguments.

**Value:** Current key count.

## Verification and limits

See the rich-text component for actual tests, native artifacts, prepared allocation bounds and exact missing dependencies.
