# PropertyHint

Last updated: 2026-10-22

Source: [PropertyHint.cs](../../src/Core/Object/PropertyHint.cs).

```csharp
public enum Electron2D.PropertyHint
```

## Description

Advisory property metadata for typed tooling consumers.

Hints describe presentation and do not validate or clamp runtime values. Providers and consumers retain their own executing controls; the enum does not create an editor or a resource selector.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.PropertyHint ArrayType = 31`](#member-cc7fc190992a) | Advisory array type metadata. |
| [`public const Electron2D.PropertyHint ColorNoAlpha = 21`](#member-3ed00e3908c1) | Advisory color no alpha metadata. |
| [`public const Electron2D.PropertyHint DictionaryType = 38`](#member-1072487d1e0b) | Advisory dictionary type metadata. |
| [`public const Electron2D.PropertyHint Dir = 14`](#member-93ea6c63461d) | Advisory dir metadata. |
| [`public const Electron2D.PropertyHint Enum = 2`](#member-e57d56ad00ed) | Advisory enum metadata. |
| [`public const Electron2D.PropertyHint EnumSuggestion = 3`](#member-18cc46bb1cfe) | Advisory enum suggestion metadata. |
| [`public const Electron2D.PropertyHint ExpEasing = 4`](#member-9b19e0d6063e) | Advisory exp easing metadata. |
| [`public const Electron2D.PropertyHint Expression = 19`](#member-8d6e5dd0a114) | Advisory expression metadata. |
| [`public const Electron2D.PropertyHint File = 13`](#member-7b8b66c88618) | Advisory file metadata. |
| [`public const Electron2D.PropertyHint FilePath = 44`](#member-05cfec1a31bf) | Advisory file path metadata. |
| [`public const Electron2D.PropertyHint Flags = 6`](#member-65e2c5d42626) | Advisory flags metadata. |
| [`public const Electron2D.PropertyHint GlobalDir = 16`](#member-f74254a69bac) | Advisory global dir metadata. |
| [`public const Electron2D.PropertyHint GlobalFile = 15`](#member-c467532e8a52) | Advisory global file metadata. |
| [`public const Electron2D.PropertyHint GlobalSaveFile = 28`](#member-147cb94f6b91) | Advisory global save file metadata. |
| [`public const Electron2D.PropertyHint GroupEnable = 42`](#member-0b75b909c6b5) | Advisory group enable metadata. |
| [`public const Electron2D.PropertyHint HideQuaternionEdit = 35`](#member-3c9f261102b3) | Advisory quaternion-value presentation for caller-provided typed data. |
| [`public const Electron2D.PropertyHint InputName = 43`](#member-1b2e22cb32c2) | Advisory input name metadata. |
| [`public const Electron2D.PropertyHint IntIsPointer = 30`](#member-1a9a6f339fba) | Advisory int is pointer metadata. |
| [`public const Electron2D.PropertyHint Layers2DNavigation = 9`](#member-fb638d0aa9e4) | Advisory layers 2d navigation metadata. |
| [`public const Electron2D.PropertyHint Layers2DPhysics = 8`](#member-165c73f28bec) | Advisory layers 2d physics metadata. |
| [`public const Electron2D.PropertyHint Layers2DRender = 7`](#member-434c7ebb0849) | Advisory layers 2d render metadata. |
| [`public const Electron2D.PropertyHint LayersAvoidance = 37`](#member-23e678b55303) | Advisory layers avoidance metadata. |
| [`public const Electron2D.PropertyHint Link = 5`](#member-0635f4195953) | Advisory link metadata. |
| [`public const Electron2D.PropertyHint LocaleID = 32`](#member-a1d2650a7a58) | Advisory locale id metadata. |
| [`public const Electron2D.PropertyHint LocalizableString = 33`](#member-f05232106fd2) | Advisory localizable string metadata. |
| [`public const Electron2D.PropertyHint Max = 45`](#member-7719375d2ac8) | The nonselectable hint count. |
| [`public const Electron2D.PropertyHint MultilineText = 18`](#member-ea423b2a643e) | Advisory multiline text metadata. |
| [`public const Electron2D.PropertyHint NodePathValidTypes = 26`](#member-afdda3055c29) | Advisory node path valid types metadata. |
| [`public const Electron2D.PropertyHint NodeType = 34`](#member-0d72279bed84) | Advisory node type metadata. |
| [`public const Electron2D.PropertyHint None = 0`](#member-559ae29e103d) | Advisory none metadata. |
| [`public const Electron2D.PropertyHint ObjectID = 22`](#member-39c6c0c04d2f) | Advisory object id metadata. |
| [`public const Electron2D.PropertyHint ObjectTooBig = 25`](#member-451af04d525f) | Advisory object too big metadata. |
| [`public const Electron2D.PropertyHint OneShot = 40`](#member-42173807d2bd) | Advisory oneshot metadata. |
| [`public const Electron2D.PropertyHint Password = 36`](#member-c146e8faa356) | Advisory password metadata. |
| [`public const Electron2D.PropertyHint PlaceholderText = 20`](#member-5330d90f383e) | Advisory placeholder text metadata. |
| [`public const Electron2D.PropertyHint Range = 1`](#member-47e36a34a565) | Advisory range metadata. |
| [`public const Electron2D.PropertyHint ResourceType = 17`](#member-6b6d958c97a3) | Advisory resource type metadata. |
| [`public const Electron2D.PropertyHint SaveFile = 27`](#member-772015d8624f) | Advisory save file metadata. |
| [`public const Electron2D.PropertyHint ToolButton = 39`](#member-92d6b5df6f94) | Advisory tool button metadata. |
| [`public const Electron2D.PropertyHint TypeString = 23`](#member-01c449556047) | Advisory type string metadata. |

<a id="member-cc7fc190992a"></a>
### `ArrayType`

```csharp
public const Electron2D.PropertyHint ArrayType = 31
```

Advisory array type metadata.

<a id="member-3ed00e3908c1"></a>
### `ColorNoAlpha`

```csharp
public const Electron2D.PropertyHint ColorNoAlpha = 21
```

Advisory color no alpha metadata.

<a id="member-1072487d1e0b"></a>
### `DictionaryType`

```csharp
public const Electron2D.PropertyHint DictionaryType = 38
```

Advisory dictionary type metadata.

<a id="member-93ea6c63461d"></a>
### `Dir`

```csharp
public const Electron2D.PropertyHint Dir = 14
```

Advisory dir metadata.

<a id="member-e57d56ad00ed"></a>
### `Enum`

```csharp
public const Electron2D.PropertyHint Enum = 2
```

Advisory enum metadata.

<a id="member-18cc46bb1cfe"></a>
### `EnumSuggestion`

```csharp
public const Electron2D.PropertyHint EnumSuggestion = 3
```

Advisory enum suggestion metadata.

<a id="member-9b19e0d6063e"></a>
### `ExpEasing`

```csharp
public const Electron2D.PropertyHint ExpEasing = 4
```

Advisory exp easing metadata.

<a id="member-8d6e5dd0a114"></a>
### `Expression`

```csharp
public const Electron2D.PropertyHint Expression = 19
```

Advisory expression metadata.

<a id="member-7b8b66c88618"></a>
### `File`

```csharp
public const Electron2D.PropertyHint File = 13
```

Advisory file metadata.

<a id="member-05cfec1a31bf"></a>
### `FilePath`

```csharp
public const Electron2D.PropertyHint FilePath = 44
```

Advisory file path metadata.

<a id="member-65e2c5d42626"></a>
### `Flags`

```csharp
public const Electron2D.PropertyHint Flags = 6
```

Advisory flags metadata.

<a id="member-f74254a69bac"></a>
### `GlobalDir`

```csharp
public const Electron2D.PropertyHint GlobalDir = 16
```

Advisory global dir metadata.

<a id="member-c467532e8a52"></a>
### `GlobalFile`

```csharp
public const Electron2D.PropertyHint GlobalFile = 15
```

Advisory global file metadata.

<a id="member-147cb94f6b91"></a>
### `GlobalSaveFile`

```csharp
public const Electron2D.PropertyHint GlobalSaveFile = 28
```

Advisory global save file metadata.

<a id="member-0b75b909c6b5"></a>
### `GroupEnable`

```csharp
public const Electron2D.PropertyHint GroupEnable = 42
```

Advisory group enable metadata.

<a id="member-3c9f261102b3"></a>
### `HideQuaternionEdit`

```csharp
public const Electron2D.PropertyHint HideQuaternionEdit = 35
```

Advisory quaternion-value presentation for caller-provided typed data.

<a id="member-1b2e22cb32c2"></a>
### `InputName`

```csharp
public const Electron2D.PropertyHint InputName = 43
```

Advisory input name metadata.

<a id="member-1a9a6f339fba"></a>
### `IntIsPointer`

```csharp
public const Electron2D.PropertyHint IntIsPointer = 30
```

Advisory int is pointer metadata.

<a id="member-fb638d0aa9e4"></a>
### `Layers2DNavigation`

```csharp
public const Electron2D.PropertyHint Layers2DNavigation = 9
```

Advisory layers 2d navigation metadata.

<a id="member-165c73f28bec"></a>
### `Layers2DPhysics`

```csharp
public const Electron2D.PropertyHint Layers2DPhysics = 8
```

Advisory layers 2d physics metadata.

<a id="member-434c7ebb0849"></a>
### `Layers2DRender`

```csharp
public const Electron2D.PropertyHint Layers2DRender = 7
```

Advisory layers 2d render metadata.

<a id="member-23e678b55303"></a>
### `LayersAvoidance`

```csharp
public const Electron2D.PropertyHint LayersAvoidance = 37
```

Advisory layers avoidance metadata.

<a id="member-0635f4195953"></a>
### `Link`

```csharp
public const Electron2D.PropertyHint Link = 5
```

Advisory link metadata.

<a id="member-a1d2650a7a58"></a>
### `LocaleID`

```csharp
public const Electron2D.PropertyHint LocaleID = 32
```

Advisory locale id metadata.

<a id="member-f05232106fd2"></a>
### `LocalizableString`

```csharp
public const Electron2D.PropertyHint LocalizableString = 33
```

Advisory localizable string metadata.

<a id="member-7719375d2ac8"></a>
### `Max`

```csharp
public const Electron2D.PropertyHint Max = 45
```

The nonselectable hint count.

<a id="member-ea423b2a643e"></a>
### `MultilineText`

```csharp
public const Electron2D.PropertyHint MultilineText = 18
```

Advisory multiline text metadata.

<a id="member-afdda3055c29"></a>
### `NodePathValidTypes`

```csharp
public const Electron2D.PropertyHint NodePathValidTypes = 26
```

Advisory node path valid types metadata.

<a id="member-0d72279bed84"></a>
### `NodeType`

```csharp
public const Electron2D.PropertyHint NodeType = 34
```

Advisory node type metadata.

<a id="member-559ae29e103d"></a>
### `None`

```csharp
public const Electron2D.PropertyHint None = 0
```

Advisory none metadata.

<a id="member-39c6c0c04d2f"></a>
### `ObjectID`

```csharp
public const Electron2D.PropertyHint ObjectID = 22
```

Advisory object id metadata.

<a id="member-451af04d525f"></a>
### `ObjectTooBig`

```csharp
public const Electron2D.PropertyHint ObjectTooBig = 25
```

Advisory object too big metadata.

<a id="member-42173807d2bd"></a>
### `OneShot`

```csharp
public const Electron2D.PropertyHint OneShot = 40
```

Advisory oneshot metadata.

<a id="member-c146e8faa356"></a>
### `Password`

```csharp
public const Electron2D.PropertyHint Password = 36
```

Advisory password metadata.

<a id="member-5330d90f383e"></a>
### `PlaceholderText`

```csharp
public const Electron2D.PropertyHint PlaceholderText = 20
```

Advisory placeholder text metadata.

<a id="member-47e36a34a565"></a>
### `Range`

```csharp
public const Electron2D.PropertyHint Range = 1
```

Advisory range metadata.

<a id="member-6b6d958c97a3"></a>
### `ResourceType`

```csharp
public const Electron2D.PropertyHint ResourceType = 17
```

Advisory resource type metadata.

<a id="member-772015d8624f"></a>
### `SaveFile`

```csharp
public const Electron2D.PropertyHint SaveFile = 27
```

Advisory save file metadata.

<a id="member-92d6b5df6f94"></a>
### `ToolButton`

```csharp
public const Electron2D.PropertyHint ToolButton = 39
```

Advisory tool button metadata.

<a id="member-01c449556047"></a>
### `TypeString`

```csharp
public const Electron2D.PropertyHint TypeString = 23
```

Advisory type string metadata.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed parameter definitions, copied defaults/hints, storage qualifiers, graph-local references and portable archives. The executable fixtures also observe real software-rendered pixels, typed updates/reset and all sixteen instance slots. See [the component contract](../components/visual-shaders.md#parameters-and-graph-local-references) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
