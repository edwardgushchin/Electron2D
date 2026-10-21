# Visual shader graphs

Last updated: 2026-10-22

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

## Numeric ranges and masks

Six definitions execute through the same numeric ports and offline compiler:

| Definition | Input order | Initial scalar defaults | Operation |
| --- | --- | --- | --- |
| VisualShaderNodeClamp | value, minimum, maximum | 0, 0, 1 | Inclusive component clamp; float, signed/unsigned integer and vector roles |
| VisualShaderNodeMix | a, b, weight | 0, 1, 0.5 | Linear interpolation, including extrapolation |
| VisualShaderNodeStep | edge, value | 0, 0 | Zero below the edge; one at or above it |
| VisualShaderNodeSmoothStep | lower edge, upper edge, value | 0, 1, 0.5 | Clamped cubic interpolation between ordered edges |
| VisualShaderNodeMultiplyAdd | a, b, c | 0, 1, 0 | Native shader fma(a, b, c) |
| VisualShaderNodeRemap | value, input minimum, input maximum, output minimum, output maximum | absent, 0, 1, 0, 1 | Unclamped affine interval conversion |

Mix, Step, SmoothStep and Remap share VisualShaderNumericOpType under ADR 0051.
Scalar and vector widths two through four select all-component controls. The
Vector2Scalar/Vector3Scalar/Vector4Scalar layouts keep data vector-valued while
broadcasting scalar controls: Mix weight, Step edge, both SmoothStep edges and
all four Remap bounds. Clamp.OpTypeEnum and MultiplyAdd.OpTypeEnum retain their
distinct valid type sets. All count sentinels reject without mutation.

A changed selector converts every existing default and recreates absent entries
before one Changed emission; an unchanged selector does nothing. Scalars broadcast,
vector narrowing retains leading components and expansion repeats the last
component. Integer conversion truncates toward zero, preserves exact integer to
integer values, and rejects nonrepresentable results atomically. Observer errors
occur after the coherent new layout commits. Copies, stored descriptors and fresh
archives preserve the selector and concrete port types.

Missing defaults recreated by Clamp, Mix, Step and SmoothStep are zero, except
four-component roles use (0,0,0,1). MultiplyAdd recreates zero/one/zero with genuine
zero vectors. Remap recreates zero/zero/one/zero/one, using the homogeneous W for
its four-component zero fallbacks. Removing an entry without changing the selector
leaves it absent; the compiler uses the existing zero-valued port fallback.

```csharp
using var blend = new VisualShaderNodeMix
{
    OpType = VisualShaderNumericOpType.Vector3Scalar
};
blend.SetInputPortDefaultValue(0, new Vector3(.125f, .25f, .5f));
blend.SetInputPortDefaultValue(1, new Vector3(.875f, .75f, .5f));
blend.SetInputPortDefaultValue(2, .375f);
```

Graphs may derive the weight from UV distance and SmoothStep or use these numeric
operations in the vertex stage. Native fma precision remains a backend arithmetic
contract; the software fixture includes an amplified fused-rounding witness.
Remap permits reversed input/output ranges and extrapolation. Clamp bounds must
be ordered and SmoothStep requires increasing edges for its defined result.
Zero-width remap division and invalid native arithmetic domains retain native
nonfinite/undefined behavior; the importer does not replace them with finite
values. The zero-width boundary is verified through import and fresh loading,
without asserting a portable framebuffer value.

## Integer arithmetic and bit patterns

VisualShaderNodeIntOp and VisualShaderNodeUIntOp operate on signed/unsigned
32-bit scalar ports. Their shared VisualShaderIntegerOperator selects addition,
subtraction, multiplication, division, modulo, min/max, bitwise AND/OR/XOR and
left/right shifts. Max is the actual maximum operation; EnumSize is the
nonselectable count bound. Both definitions initially add two explicit zeros.

VisualShaderNodeIntFunc initially selects Sign and supports Abs, Negate, Sign and
BitwiseNot. VisualShaderNodeUIntFunc initially selects Negate and also supports
BitwiseNot. Their different valid sets and numeric identities retain distinct
nested FunctionEnum types. Their Max values are nonselectable count bounds.
Each function has one explicit zero input. Unchanged selectors are quiet;
invalid selectors leave state unchanged, and Changed observers run after commit.
Selectors, all 32 input bits and explicit default absence survive deep/shallow
copies, assignment and fresh compressed big-endian graph archives.

Integer nodes retain their exact port types until an ordinary numeric connection
converts the result. Integer-to-float conversion can discard low bits in large
values. Decode individual bytes with unsigned shifts/AND before normalizing a
color channel when those bits must be observable:

```csharp
using var mask = new VisualShaderNodeUIntOp
{
    Operator = VisualShaderIntegerOperator.BitwiseAnd
};
mask.SetInputPortDefaultValue(0, 0xfedcba98u);
mask.SetInputPortDefaultValue(1, 255u);
using var normalize = new VisualShaderNodeFloatOp
{
    Operator = VisualShaderNodeFloatOp.OperatorEnum.Divide
};
normalize.SetInputPortDefaultValue(1, 255f);
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, mask, Vector2.Zero, 2);
graph.AddNode(VisualShader.Type.Fragment, normalize, Vector2.Zero, 3);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0);
graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, VisualShader.NodeIDOutput, 0);
```

This produces 0x98/255 in RGB after ordinary source save/import. The fixtures
also derive an unsigned integer seed from UV, combine multiply/add/XOR/shift
operations and render the resulting bytes. These are native shader computations;
CLR does not evaluate a graph per frame.

Signed division truncates toward zero. Unsigned negation and ordinary integer
add/subtract/multiply retain their 32-bit native representation. Signed right
shift extends the sign bit; unsigned right shift zero-fills. Shift counts must
be below 32. Division/modulo by zero, signed minimum divided by minus one and
out-of-range shifts are native domain boundaries. The graph contract does not require maintenance8, so negative signed modulo
is outside its portable result contract. Abs of the signed minimum is retained as an additional
native boundary rather than a fabricated positive value. These ten archived
boundary graphs are imported and freshly loaded without executing them in the
portable framebuffer oracle. See the [SPIR-V instruction rules](https://registry.khronos.org/SPIR-V/specs/unified1/SPIRV.html)
and [Vulkan execution environment](https://docs.vulkan.org/spec/latest/appendices/spirvenv.html).

The 258 integer programs expose all four result bytes, every binary/unary
selection, zero/sign/large-value cases, bit 31, signed/unsigned right shifts,
constructor defaults, removed defaults and both canvas stages. Integer channels
use a tighter 0.001 readback tolerance, sufficient to detect one-byte differences
on normalized and half-float targets. The wider fixture grid accommodates these
programs without changing their local geometry or UV coordinates.

## Comparisons and conditional results

VisualShaderNodeCompare and VisualShaderNodeSwitch share VisualShaderValueType:
Float, Int, UInt, Vector2, Vector3, Vector4, Boolean and Matrix. This selector excludes
sampled images; VisualShaderNode.PortType remains the broader port contract with
a separate pending sampled-image role. Max=8 is a nonselectable logical bound. Matrix=7 carries copied numeric
System.Numerics.Matrix4x4 values; its graph operations are described below.

Compare initially tests two zero floats for equality with an explicit 0.00001
absolute tolerance. Float Equal uses abs(a-b) < tolerance, and NotEqual negates
that test. The tolerance input is active only for these two functions; its typed
stored default and absence survive all other function/type selections and fresh
archives. Integer comparisons retain all bits. Vector equality/ordering tests
components exactly and reduces them with Condition All or Any. Boolean and matrix Equal/NotEqual compare directly; their four ordering
functions produce false.

Changing Type converts only the two operands and recreates missing operands.
A failed integer conversion changes no field or default. A missing Vector4
operand uses (0,0,0,1); expansion of a present vector repeats its last component.
Function and Condition retain their values independently of the selected type.
Invalid selectors leave the complete definition unchanged. Repeated selection
is quiet, and Changed observers execute after the new state commits.

Switch inputs are condition, true value and false value. Its initial defaults
are false, 1.0 and 0.0. Changing OpType converts/recreates only the value inputs;
the condition retains both its Boolean value and its explicit presence/absence.
Boolean value conversions use true/false fallbacks. Floating/vector selection
uses native mix(falseValue, trueValue, float(condition)); integer/Boolean/matrix selection uses the exact selected operand. Native nonfinite interpolation
behavior is retained, so this is not a promise to suppress a nonfinite unused
floating operand.

```csharp
using var compare = new VisualShaderNodeCompare
{
    Function = VisualShaderNodeCompare.FunctionEnum.GreaterThan
};
compare.SetInputPortDefaultValue(0, .75f);
compare.SetInputPortDefaultValue(1, .5f);
using var select = new VisualShaderNodeSwitch
{
    OpType = VisualShaderValueType.Vector3
};
select.SetInputPortDefaultValue(1, new Vector3(.25f, .5f, .75f));
select.SetInputPortDefaultValue(2, Vector3.Zero);
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, compare, Vector2.Zero, 2);
graph.AddNode(VisualShader.Type.Fragment, select, Vector2.Zero, 3);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0);
graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, VisualShader.NodeIDOutput, 0);
```

Ordinary source save/import makes this a native material program. The executing
fixture also supplies UV.x to the comparison, so its branch changes across the
quad without a per-frame CLR graph evaluator.

VisualShaderNodeIf has six inputs: a, b, tolerance, equal, greater and lesser.
The first three are floats and the results are Vector3. Its explicit defaults
are zero except for tolerance 0.00001. It selects equal when abs(a-b) < tolerance,
otherwise lesser when a < b, otherwise greater. At equal inputs and a zero or
negative tolerance it therefore selects greater. Removed defaults retain the
ordinary zero fallback instead of recreating constructor values.

VisualShaderNodeIs classifies a floating result with IsInf (initial) or IsNaN
and produces a Boolean scalar. Authored defaults remain finite. The software
fixtures also classify native overflow and positive/negative/zero divided by
zero in both stages. These tests describe the observed software backend; the
producer arithmetic's native domain rules are not promoted to portable
cross-backend guarantees.

The 581 new programs cover every applicable comparison function/type/reduction,
all Switch types and both choices, every If branch and strict-tolerance boundary,
initial/removed defaults, both canvas stages and UV-driven composition. Integer
selection uses the existing full-byte readback; Vector4 has explicit W witnesses.
Ordinary condition colors allow 0.0021 readback error for half a UNORM8 step; byte-encoded integer colors retain the stricter 0.001 bound. Fresh archives verify selectors, dormant tolerance and exact typed operands. A
stored edge into a subsequently hidden tolerance port is rejected on load/import,
and failed import preserves the existing target artifact.

## Numeric matrix data

Matrix ports carry all sixteen finite System.Numerics.Matrix4x4 components as
copied numeric data. Matrix and nonmatrix ports are incompatible. Ordinary
CanConnectNodes/ConnectNodes enforce this rule. ConnectNodesForced retains its
authoring bypass; importing an incompatible edge fails before replacing an
existing artifact. Missing matrix defaults evaluate as identity, while the
explicit default table still distinguishes absence from a stored identity.
Switch/Compare type changes use identity on entry to Matrix and the destination
fallback on exit; they preserve condition/tolerance values and absence.

The carrier's [row,column] components use explicit row order in portable
archives and explicit column order in GLSL mat4 literals. No native memory
layout or scene transform convention is inferred. All sixteen components,
including the fourth row, survive compressed big-endian archives and copies.
The general value codec also supports nullable matrices and typed matrix arrays;
byte-level tests verify both byte orders and truncated payload rejection.

| Definition | Native numeric behavior |
| --- | --- |
| MatrixConstant | Copied finite Matrix4x4, initially identity; approximate-equal writes preserve the original value. |
| MatrixOp | A*B, B*A, both component multiplication orders, addition, both subtraction orders and both component division orders. |
| MatrixFunc | Inverse or transpose; initially inverse. |
| MatrixVecMult | Matrix/vector in both orders, with explicit homogeneous 1 or 0, returning xyz. |
| MatrixCompose | Three numeric columns with w=0 and one final column with w=1; inputs initially zero. |
| MatrixDecompose | xyz of each of the four matrix columns; matrix input initially identity. |
| Determinant | The full four-by-four determinant; matrix input initially identity. |
| OuterProduct | outerProduct(vec4(a,0),vec4(b,0)); inputs initially zero. |

These are VisualShaderNode-prefixed Resource types. Three selector-bearing
nodes keep their distinct OperatorEnum/FunctionEnum sets and count sentinels.
Unchanged selections are quiet; invalid selections preserve state, and Changed
observers run after commit. Matrix operation/function inputs initially contain
identity; the vector input of MatrixVecMult is zero. Their aliases, selectors,
values and default absence use the ordinary graph copy/archive contract.

```csharp
using M4 = System.Numerics.Matrix4x4;
var values = M4.Identity;
values.M14 = .25f;
values.M24 = .5f;
values.M34 = .75f;
using var constant = new VisualShaderNodeMatrixConstant { Constant = values };
using var product = new VisualShaderNodeMatrixVecMult();
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, constant, Vector2.Zero, 2);
graph.AddNode(VisualShader.Type.Fragment, product, Vector2.Zero, 3);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 3, 0);
graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/matrix-source.e2dres", SaverFlags.BundleResources);
```

After ordinary source import this produces (.25,.5,.75) in RGB through the
matrix times (0,0,0,1) data operation. It does not create a scene transform.
Canvas/model/screen matrix input names still require their renderer ABI producer;
parameter declarations still require their symbol/scope/qualifier metadata path.

The 585 matrix programs exercise every matrix arithmetic/function/vector-product
selection, all decomposition outputs, all sixteen comparison components, both
canvas stages, initial/removed defaults and a UV-controlled matrix choice.
Independent double-precision matrix multiplication, elimination and determinant
oracles drive framebuffer expectations. Nonsymmetric matrices and both vector
orders expose literal row/column layout; determinant/comparison witnesses retain
the fourth row and final component. A single quad checks both spatial branches.
Singular inverse and component division by zero are separate import/fresh-load
boundaries; their undefined native results are not portable pixel assertions.

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
compressed big-endian data. One thousand eight hundred thirty rendered imported programs include the 57 foundation programs,
170 unary-function programs, 41 vector-dataflow programs, 138 range/mask programs, 258 integer programs and 581 comparison/conditional programs and 585 matrix programs.
The separate zero-width Remap boundary and ten integer boundary programs verify import and fresh load without portable undefined-domain pixel assertions. Unary coverage includes every scalar/vector function, all vector widths,
negative/clamped/round-even cases, both canvas stages, and a normalization witness
for every vec4 function that makes its fourth component observable in RGB. Dataflow
covers all widths/components, dormant defaults, scalar eta, reflection boundaries,
fan-out, direct alpha and UV-driven vertex/fragment geometry. Range checks cover all
38 selectable layouts and every pair of layout transitions, exact integer conversion
and transactional overflow, all operation families in both stages, mixed controls,
W witnesses, initial defaults and UV-derived masks. Software Vulkan Wayland/X11 and HDR/MSAA
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
