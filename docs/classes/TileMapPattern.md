# TileMapPattern

Last updated: 2026-10-11

**Namespace:** Electron2D. **Source:** [TileMapPattern.cs](../../src/Scene/Resources/TileMapPattern.cs). **Inherits:** [Resource](Resource.md).

Stores reusable tile stamps independently of any layer or atlas. Coordinates are nonnegative, identities use the tile storage domain, and explicit empty entries differ from omitted cells. The consuming layer's geometry determines capture and placement.

## Syntax

```csharp
public sealed class TileMapPattern : Resource
```

## Description

Cells and extent belong to the pattern; all duplicates own independent dictionaries. Coordinates retain signed 32-bit values with representable nonnegative extents. Explicit empty entries erase destinations on placement, while omitted cells and extra extent margins preserve them. Missing scalar queries return sentinels, and GetUsedCells exports a copied unordered array. Edits, copies and exports can allocate; warmed scalar queries and mapping do not allocate measured owner-thread managed bytes. Resource authors must coordinate mutations with live scene consumers.

TileSet borrows patterns and permits shared references across sets. Insertion requires a distinct nonempty pattern; later edits can make it empty. Removal and set disposal do not dispose borrowed patterns. Disposing a pattern removes observing list entries. Shallow TileSet copies share patterns; deep modes follow Resource's graph policy.

## Example

```csharp
using var stamp = new TileMapPattern();
stamp.SetCell(new(0, 0), sourceID: 0, atlasCoords: new(0, 0), alternativeTile: 0);
stamp.SetCell(new(1, 0)); // Explicitly erase this destination.
stamp.SetSize(new(4, 2));
layer.SetPattern(new(-3, 5), stamp);
layer.UpdateInternals();
```

The layer must have a live TileSet whose source zero resolves the authored tile. These methods compile in TilePatternTests; rendering and generated collision/navigation require the ordinary attached scene/world host.

## Constructors

| Signature | Contract |
| --- | --- |
| public TileMapPattern() | Empty cells, zero extent, ordinary Resource lifetime. |

## Methods

| Signature | Contract |
| --- | --- |
| [public int GetCellAlternativeTile(Vector2i coords)](#getcellalternativetile) | Returns the stored alternative (including transform flags), or -1 for an absent coordinate. coords is in pattern cell space; query does not mutate state. |
| [public Vector2i GetCellAtlasCoords(Vector2i coords)](#getcellatlascoords) | Returns the stored atlas coordinate, or (-1,-1) for an absent coordinate. This identity is independent of any current atlas. |
| [public int GetCellSourceID(Vector2i coords)](#getcellsourceid) | Returns the stored source identity, or -1 for an absent coordinate. An explicit empty coordinate still satisfies HasCell. |
| [public Vector2i GetSize()](#getsize) | Returns the cell extent including retained margins. Size is independent of IsEmpty when the last coordinate was removed without recomputation. |
| [public Vector2i[] GetUsedCells()](#getusedcells) | Returns an independent array of all stored coordinates, including explicit empty cells. Ordering is unspecified; changing the array does not edit the pattern. |
| [public bool HasCell(Vector2i coords)](#hascell) | Returns whether coords is stored; explicit empty assignments return true. Negative or absent query coordinates return false. |
| [public bool IsEmpty()](#isempty) | Returns whether no coordinates are stored. A pattern containing only explicit empty cells is not empty. |
| [public void RemoveCell(Vector2i coords, bool updateSize)](#removecell) | Removes a stored coordinate; absence throws ArgumentException. updateSize=true recomputes extent from remaining cells and discards margins, becoming zero after the last cell; false retains extent. Emits Changed after the complete edit. |
| [public void SetCell(Vector2i coords, int sourceID = -1, Vector2i? atlasCoords = null, int alternativeTile = -1)](#setcell) | coords must be nonnegative and its extent representable. Source, atlas components and alternative are -1 or 0..65534; null atlasCoords means (-1,-1). Invalid input throws ArgumentOutOfRangeException; extent overflow throws OverflowException before state changes. Assignments retain explicit emptiness, expand size and emit Changed even when repeated. Identifiers need not resolve in a current TileSet. |
| [public void SetSize(Vector2i size)](#setsize) | Sets nonnegative extent containing every stored cell. Unused margins may shrink; excluding a cell or negative size throws ArgumentOutOfRangeException without mutation. Every accepted call emits Changed. |

## Constructor Descriptions

### TileMapPattern

`public TileMapPattern()` creates an ordinary live Resource with no cells and zero extent. No source, layer or native backend is needed; callers dispose standalone patterns after removing borrowed list references or completing their use.

## Method Descriptions

### GetCellAlternativeTile

`public int GetCellAlternativeTile(Vector2i coords)`

Returns the stored alternative (including transform flags), or -1 for an absent coordinate. coords is in pattern cell space; query does not mutate state. Every operation on a disposed pattern throws ObjectDisposedException.

### GetCellAtlasCoords

`public Vector2i GetCellAtlasCoords(Vector2i coords)`

Returns the stored atlas coordinate, or (-1,-1) for an absent coordinate. This identity is independent of any current atlas. Every operation on a disposed pattern throws ObjectDisposedException.

### GetCellSourceID

`public int GetCellSourceID(Vector2i coords)`

Returns the stored source identity, or -1 for an absent coordinate. An explicit empty coordinate still satisfies HasCell. Every operation on a disposed pattern throws ObjectDisposedException.

### GetSize

`public Vector2i GetSize()`

Returns the cell extent including retained margins. Size is independent of IsEmpty when the last coordinate was removed without recomputation. Every operation on a disposed pattern throws ObjectDisposedException.

### GetUsedCells

`public Vector2i[] GetUsedCells()`

Returns an independent array of all stored coordinates, including explicit empty cells. Ordering is unspecified; changing the array does not edit the pattern. Every operation on a disposed pattern throws ObjectDisposedException.

### HasCell

`public bool HasCell(Vector2i coords)`

Returns whether coords is stored; explicit empty assignments return true. Negative or absent query coordinates return false. Every operation on a disposed pattern throws ObjectDisposedException.

### IsEmpty

`public bool IsEmpty()`

Returns whether no coordinates are stored. A pattern containing only explicit empty cells is not empty. Every operation on a disposed pattern throws ObjectDisposedException.

### RemoveCell

`public void RemoveCell(Vector2i coords, bool updateSize)`

Removes a stored coordinate; absence throws ArgumentException. updateSize=true recomputes extent from remaining cells and discards margins, becoming zero after the last cell; false retains extent. Emits Changed after the complete edit. Every operation on a disposed pattern throws ObjectDisposedException.

### SetCell

`public void SetCell(Vector2i coords, int sourceID = -1, Vector2i? atlasCoords = null, int alternativeTile = -1)`

coords must be nonnegative and its extent representable. Source, atlas components and alternative are -1 or 0..65534; null atlasCoords means (-1,-1). Invalid input throws ArgumentOutOfRangeException; extent overflow throws OverflowException before state changes. Assignments retain explicit emptiness, expand size and emit Changed even when repeated. Identifiers need not resolve in a current TileSet. Every operation on a disposed pattern throws ObjectDisposedException.

### SetSize

`public void SetSize(Vector2i size)`

Sets nonnegative extent containing every stored cell. Unused margins may shrink; excluding a cell or negative size throws ArgumentOutOfRangeException without mutation. Every accepted call emits Changed. Every operation on a disposed pattern throws ObjectDisposedException.

## Protected Resource hooks

| Signature | Contract |
| --- | --- |
| protected override Resource CreateDuplicateInstance() | Constructs a fresh exact-type pattern for inherited duplication. |
| protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force) | Copies all value cells and extent independently; the resource has no nested references. Graph mode/delegates remain the inherited protocol. |
| protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() | Adds stored Cells (int[] six-field records) followed by Size (Vector2i). Array writes validate complete records, coordinates, identities and duplicate keys before replacement. Size restoration preserves margins. |

## Lifetime, storage and verification

Inherited Resource duplication, CopyFromResource, change batching, file ownership and disposal apply. Cells store x/y/source/atlas-x/atlas-y/alternative as full typed integers; Size stores the explicit extent. File schemas never expose arbitrary reflection or backend types. The compiled factory supports standalone files and scene graphs in fresh processes. Cell edits emit Changed; existing TileSet patterns are independently read at placement rather than live cell links.

TilePatternTests covers 144 captures/576 placements across all 48 grid profiles, explicit empty cells/margins, input/storage failures, copies, borrowed disposal, listener failure, CPU/GPU collision/navigation, baking and fresh-process scene placement. Native GPU/compatibility checks consume captured/stored/re-placed stamps on both physics backends. See [the tile component](../components/tiles.md#reusable-patterns) and [ADR 0101](../decisions/tiles.md#adr-0101) for scope/results. Native/all-thread allocator totals, other platforms, dense-map throughput, editor workflow and human acceptance remain separate verification boundaries.
