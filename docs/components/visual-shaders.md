# Visual shader graphs

Last updated: 2026-10-21

## Scope and owned types

`VisualShader : Shader` carries typed canvas vertex/fragment graph source and the
last successfully imported immutable shader program. It uses the ordinary
ShaderMaterial, retained canvas, resource-file and shader-import pipelines.
Editing a graph is source authoring; drawing never runs a graph interpreter or
source compiler.

The current definition family is VisualShaderNode, VisualShaderNodeConstant,
Float/Int/UInt/Boolean/Vec2/Vec3/Vec4/Color constant nodes, FloatOp, VectorBase,
VectorOp, FloatFunc, VectorFunc, VectorCompose/VectorDecompose, DotProduct,
VectorDistance, VectorLen, VectorRefract, Input and Output. VisualShaderConnection copies one directed edge.
VisualShaderPortDefault and its generic entry expose heterogeneous numeric
input defaults without an untyped public value accessor. Three/four-component
vectors are numeric shader data, not spatial scene types.

## Author, import and execute

```csharp
using var graph = new VisualShader();
using var color = new VisualShaderNodeColorConstant
{
    Constant = new Color(.25f, .5f, .75f, 1)
};
graph.AddNode(VisualShader.Type.Fragment, color, new Vector2(10, 20), 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/color-source.e2dres", SaverFlags.BundleResources);
```

The pinned standalone importer accepts:

```text
Electron2D.ShaderImport color-source.e2dres canvas color.e2dres --visual-shader
```

It loads the source through the typed resource loader, generates deterministic
GLSL, and sends both stages through the existing source compilation, SPIR-V
validation/reflection and paired canvas-interface validators. It then atomically
saves a bundled compressed resource retaining both the editable graph and its
compiled program. Existing output bytes and timestamps remain unchanged when
the resulting artifact is identical. Invalid graph data, cycles, unsupported
producers and compiler errors preserve the last valid output.

```csharp
using var shader = ResourceLoader.Load<VisualShader>("res://Shaders/color.e2dres");
using var material = new ShaderMaterial { Shader = shader };
item.Material = material;
```

`item` is a live caller-owned CanvasItem. An edited graph emits Changed after
commitment, but its old compiled program remains in use until reimport/reload
or explicit inherited Shader binary replacement. A newly created graph carries
the ordinary default canvas program. The current graph importer requires the
canvas domain; inherited binary Shader APIs retain their own domain contracts.

The SDK target uses the existing item with an explicit graph profile:

```xml
<Electron2DShader Include="Shaders/color-source.e2dres"
                  Profile="visual-shader" Stage="canvas"
                  TargetPath="Shaders/color.e2dres" />
```

The standard ShaderImport executable path and target import apply. Normal builds
validate every source, including edits with preserved timestamps. Build and
publish copy the compiled `.e2dres` content; publish with `--no-build` reuses the
last successful artifact, and Clean removes generated intermediate resources.
Paths containing spaces use the same argument-list and relative-target guards
as ordinary shader inputs.

## Graph and numeric semantics

Each stage owns output ID zero; ID one is reserved and added IDs start at two.
IDs and connection arrays are returned as independent copies. IDs are ordered;
connections retain authoring order. The next valid ID follows the current
largest ID rather than filling holes. Positions are finite graph-coordinate
metadata, independent of native geometry. The file/source budget is 4096 nodes
and 32768 connections per stage, with the existing eight-MiB source budget.

Added definitions are borrowed. A compiled registered factory supplies
ReplaceNode, with no assembly loading or arbitrary reflected construction.
Same-type replacement is unchanged; other replacements preserve positions and
stored edges. The graph owns factory-created replacements until their last
graph alias disappears. Removing ordinary added definitions never disposes
them. Shared definitions have one change subscription, regardless of alias
count. Graph disposal detaches all borrowed definitions and attempts every
owned output/replacement cleanup.

Ordinary connections require live ports, no duplicate, no self edge and no
cycle. Forced connections preserve invalid cyclic source for editing; import
rejects a cycle reachable from the output. Several edges may target one input;
the last authored edge supplies its value. Removing a node removes its incident
edges. Replacement can leave an edge whose port no longer exists; import rejects
that source explicitly. Reachability/topological generation is iterative and
does not consume the CLR call stack per graph depth.

Constants preserve exact integer/Boolean values and finite float components.
Approximately equal floating constants and equal integral/Boolean constants do
not change stored data or emit Changed. FloatOp supports add/subtract/multiply/
divide, floor-based modulo, power, min/max, atan2 and step. VectorOp supplies
these component-wise operations plus reflect and the three-component cross
product. Unsupported cross widths reject explicitly. Shader arithmetic retains
its native undefined/nonfinite-result rules; authoring rejects nonfinite input
constants/defaults, not every possible arithmetic result.

Defaults have an explicit absence state. Removing/clearing entries makes the
typed nullable getter return null; source generation then applies its input
fallback. Numeric definitions initialize their own defaults. Whole-table
replacement validates all immutable entries before commitment. Four-component
Color aliases normalize to Vector4 table entries; typed getters accept either
carrier. Changing vector width preserves previous components and repeats the
last previous component when expanding an authored default. Connection widening
instead appends zero components, matching the graph's numeric conversion rule.
Scalar reads use the first vector component; vector-to-Boolean conversion tests
all components, while numeric scalar-to-Boolean uses a positive comparison.

Source snapshots, copy and archive operations retain actual default presence,
positions, edges and resource aliases. Reserved outputs are always independent
between graph copies; ordinary shallow copies borrow added definitions, and
deep copies use the usual resource policy. Owned factory replacements copy
independently with their internal aliases preserved. Stored default/node/edge
tables validate bounds, duplicates, shapes and completion before publication.

## Unary numeric functions and selectors

VisualShaderNodeFloatFunc supplies 32 scalar functions. VisualShaderNodeVectorFunc
supplies those numeric roles plus normalization, at two/three/four components.
The scalar/vector FunctionEnum domains retain their distinct numeric identities;
select them through the owning Function property. Numeric casts do not translate
between these selector domains. Max is nonselectable; unknown/sentinel requests
fail before changing state or emitting Changed. Unchanged selections emit nothing.

Both families supply trigonometric/inverse/hyperbolic functions, natural/base-two
exponentials and logarithms, square root/inverse square root, abs/sign/negate,
reciprocal/one-minus, degree/radian conversion, floor/ceil/round/round-even/trunc,
fraction and saturation. Vector functions apply component-wise except Normalize,
which uses every selected component in its length, including W at width four.
Round uses the shader backend's nearest-integer tie direction; RoundEven has ties
to even. Zero normalization, zero reciprocal, invalid logarithm/square-root/inverse
function domains and overflow retain native shader behavior. They are not converted
into authoring exceptions or fabricated zero results. Authored inputs remain finite.

The existing Operator properties use owner-specific OperatorEnum types; vector
width is selected by OpType with VisualShaderNodeVectorBase.OpTypeEnum. This
preserves owning property names under ADR 0051. Stored selector fields use those
same Operator, OpType and Function names. The former selector/type spellings are
removed without aliases; resources using the previous selector schema require
reauthoring/reimport. Ordinary archive format versions are unchanged.

```csharp
using var sine = new VisualShaderNodeVectorFunc
{
    OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2,
    Function = VisualShaderNodeVectorFunc.FunctionEnum.Sin
};
sine.SetInputPortDefaultValue(0, new Vector2(.25f, .75f));
```

Add this definition to a graph, connect its output and use the same source-save,
offline import and compiled material path shown above. Duplication and fresh
archives retain Function, OpType, exact defaults and explicit default absence.

## Vector dataflow and geometry

VectorCompose assembles scalar inputs in X/Y/Z/W order at the selected OpType.
VectorDecompose exposes that same component order as separate scalar outputs.
Connections can fan out from several outputs of the same node into several
inputs of one consumer; the shared compiler orders the complete producer before
its consumers. Width changes retain stored edges. Import explicitly rejects an
edge to a removed output until the source graph is repaired.

A composition definition has four typed scalar default slots even while only
two or three components are connected. The fourth default is initially absent;
the initial X/Y/Z defaults are zero. Get/set/default-table/copy/archive operations
retain dormant component values. Narrowing does not erase Z/W. Widening recreates
missing active defaults as zero while keeping existing reserved values. Clearing
the table removes dormant values too. Graph connection validation still uses only
the selected active port count. This does not create arbitrary unbounded ports.

VectorLen returns the selected vector's length; VectorDistance returns distance
between its two vectors. DotProduct has the fixed three-component numeric input
contract and scalar output, independently of the width-selected family. Ordinary
numeric connection conversion can supply planar vectors with a zero third
component. All operations consume actual selected components, including W.

VectorRefract consumes incident and normal vectors plus scalar eta at input two.
Width changes convert the vector inputs and leave eta's scalar value and explicit
absence intact. The native refract expression returns zero under total internal
reflection; zero/negative ratios retain their numeric shader behavior. Incident
and normal normalization remain caller responsibilities. Authoring rejects
nonfinite numeric inputs without fabricating runtime arithmetic results.

```csharp
using var distance = new VisualShaderNodeVectorDistance
{
    OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2
};
distance.SetInputPortDefaultValue(1, new Vector2(.5f, .5f));
// Connect a UV Input definition to input zero for a radial-distance material.
```

Use the existing save/import/load path. Composition/decomposition also permits
routing an authored W value to fragment alpha or another scalar operation;
geometry expressions execute in either canvas stage. The public fixtures include
UV-driven radial distance, separate alpha blending and a vertex UV transform.

## Canvas boundary

Vertex inputs follow the existing supplied framebuffer-pixel canvas ABI.
Current vertex outputs execute position, UV, RGB and alpha. Fragment outputs
execute RGB and alpha; unconnected outputs preserve the incoming color/sample.
The RGB output accepts numeric conversions and discards a four-component input's
fourth component. Author alpha separately or connect a scalar operation.

Input executes color, UV, vertex, time and raw instance custom data, plus
fragment coordinates, screen pixel size/UV and vertex ID in their appropriate
stages. Unknown names retain the scalar-zero role. Known matrix/image/light,
logical command texture-size, point and additional mesh/instance-ID producers
remain separate graph integrations and reject explicitly when selected. In
particular, physical textureSize is not substituted for a missing logical
command-texture-size producer.

Point-size, normal/normal-map/depth, light-vertex and shadow-vertex output ports
retain their documented port indices, but selecting them rejects import until
their actual graph-to-canvas ABI integration exists. The class coverage remains
Partial. Particle/light/blit graph domains, custom varying transport, parameter/
image nodes and editor frame/preview consumers remain explicit prerequisites.
Sky/volumetric scene domains and deprecated inert graph offset are excluded.

## Verification and limits

The public `tests/VisualShader.Consumer` covers ownership, aliases, factory
replacement, invalid requests, snapshots, default presence/conversion, change
semantics, shallow/deep/copy operations, and fresh resource archives, including
compressed big-endian data. Two hundred sixty-eight imported programs include the 57 foundation programs,
170 unary-function programs and 41 vector-dataflow programs. Unary coverage includes every scalar/vector function, all vector widths,
negative/clamped/round-even cases, both canvas stages, and a normalization witness
for every vec4 function that makes its fourth component observable in RGB. Dataflow
covers all widths/components, dormant defaults, scalar eta, reflection boundaries,
fan-out, direct alpha and UV-driven vertex/fragment geometry. Software Vulkan Wayland/X11 and HDR/MSAA
readback is checked in CLR and strict full-trim NativeAOT. Normalized byte targets
clamp output; HDR half-float readback retains signed/out-of-unit arithmetic.
Each native profile measures 64 active and 64 idle complete frames at zero
owner/all-thread managed bytes after preparation, with retained draw counts.

`tools/shaders/check_graphs.py` exercises direct import, unchanged output,
cyclic/truncated/invalid-domain rejection, fresh public load and the actual SDK
build/edit/error/recovery/publish/Clean path. Shader import is cold allocating
work. Physical GPU, native-heap allocation, foreign host toolchains/platforms,
editor usability and human visual acceptance are separate from this evidence.

## Decisions

- [Typed shaders and rendering](../decisions/rendering.md#adr-0028)
- [Typed C#](../decisions/product.md#adr-0001)
- [Resource lifetime and archives](../decisions/resources.md#adr-0013)
- [Realtime allocation](../decisions/resources.md#adr-0014)
- [Programmatic authoring and rendered evidence](../decisions/agent-native.md#adr-0090)
