namespace Electron2D;

public static partial class ResourceFileTypes
{
    private static void RegisterVisualShaderResources()
    {
        RegisterResource("VisualShader", CreateVisualShaderFileResource);
        RegisterResource("VisualShaderNodeOutput", CreateVisualShaderNodeOutputFileResource);
        RegisterResource("VisualShaderNodeInput", CreateVisualShaderNodeInputFileResource);
        RegisterResource("VisualShaderNodeFloatOp", CreateVisualShaderNodeFloatOpFileResource);
        RegisterResource("VisualShaderNodeVectorOp", CreateVisualShaderNodeVectorOpFileResource);
        RegisterResource("VisualShaderNodeFloatFunc", CreateVisualShaderNodeFloatFuncFileResource);
        RegisterResource("VisualShaderNodeVectorFunc", CreateVisualShaderNodeVectorFuncFileResource);
        RegisterResource("VisualShaderNodeVectorCompose", CreateVisualShaderNodeVectorComposeFileResource);
        RegisterResource("VisualShaderNodeVectorDecompose", CreateVisualShaderNodeVectorDecomposeFileResource);
        RegisterResource("VisualShaderNodeDotProduct", CreateVisualShaderNodeDotProductFileResource);
        RegisterResource("VisualShaderNodeVectorDistance", CreateVisualShaderNodeVectorDistanceFileResource);
        RegisterResource("VisualShaderNodeVectorLen", CreateVisualShaderNodeVectorLenFileResource);
        RegisterResource("VisualShaderNodeVectorRefract", CreateVisualShaderNodeVectorRefractFileResource);
        RegisterResource("VisualShaderNodeClamp", CreateVisualShaderNodeClampFileResource);
        RegisterResource("VisualShaderNodeMix", CreateVisualShaderNodeMixFileResource);
        RegisterResource("VisualShaderNodeStep", CreateVisualShaderNodeStepFileResource);
        RegisterResource("VisualShaderNodeSmoothStep", CreateVisualShaderNodeSmoothStepFileResource);
        RegisterResource("VisualShaderNodeMultiplyAdd", CreateVisualShaderNodeMultiplyAddFileResource);
        RegisterResource("VisualShaderNodeRemap", CreateVisualShaderNodeRemapFileResource);
        RegisterResource("VisualShaderNodeIntOp", CreateVisualShaderNodeIntOpFileResource);
        RegisterResource("VisualShaderNodeUIntOp", CreateVisualShaderNodeUIntOpFileResource);
        RegisterResource("VisualShaderNodeIntFunc", CreateVisualShaderNodeIntFuncFileResource);
        RegisterResource("VisualShaderNodeUIntFunc", CreateVisualShaderNodeUIntFuncFileResource);
        RegisterResource("VisualShaderNodeFloatConstant", CreateVisualShaderNodeFloatConstantFileResource);
        RegisterResource("VisualShaderNodeIntConstant", CreateVisualShaderNodeIntConstantFileResource);
        RegisterResource("VisualShaderNodeUIntConstant", CreateVisualShaderNodeUIntConstantFileResource);
        RegisterResource("VisualShaderNodeBooleanConstant", CreateVisualShaderNodeBooleanConstantFileResource);
        RegisterResource("VisualShaderNodeVec2Constant", CreateVisualShaderNodeVec2ConstantFileResource);
        RegisterResource("VisualShaderNodeVec3Constant", CreateVisualShaderNodeVec3ConstantFileResource);
        RegisterResource("VisualShaderNodeVec4Constant", CreateVisualShaderNodeVec4ConstantFileResource);
        RegisterResource("VisualShaderNodeColorConstant", CreateVisualShaderNodeColorConstantFileResource);
    }
    private static VisualShaderNodeVectorOp CreateVisualShaderNodeVectorOpFileResource() => new();
    private static VisualShaderNodeFloatFunc CreateVisualShaderNodeFloatFuncFileResource() => new();
    private static VisualShaderNodeVectorFunc CreateVisualShaderNodeVectorFuncFileResource() => new();
    private static VisualShaderNodeVectorCompose CreateVisualShaderNodeVectorComposeFileResource() => new();
    private static VisualShaderNodeVectorDecompose CreateVisualShaderNodeVectorDecomposeFileResource() => new();
    private static VisualShaderNodeDotProduct CreateVisualShaderNodeDotProductFileResource() => new();
    private static VisualShaderNodeVectorDistance CreateVisualShaderNodeVectorDistanceFileResource() => new();
    private static VisualShaderNodeVectorLen CreateVisualShaderNodeVectorLenFileResource() => new();
    private static VisualShaderNodeVectorRefract CreateVisualShaderNodeVectorRefractFileResource() => new();
    private static VisualShader CreateVisualShaderFileResource() => new();
    private static VisualShaderNodeOutput CreateVisualShaderNodeOutputFileResource() => new();
    private static VisualShaderNodeInput CreateVisualShaderNodeInputFileResource() => new();
    private static VisualShaderNodeFloatOp CreateVisualShaderNodeFloatOpFileResource() => new();
    private static VisualShaderNodeClamp CreateVisualShaderNodeClampFileResource() => new();
    private static VisualShaderNodeMix CreateVisualShaderNodeMixFileResource() => new();
    private static VisualShaderNodeStep CreateVisualShaderNodeStepFileResource() => new();
    private static VisualShaderNodeSmoothStep CreateVisualShaderNodeSmoothStepFileResource() => new();
    private static VisualShaderNodeMultiplyAdd CreateVisualShaderNodeMultiplyAddFileResource() => new();
    private static VisualShaderNodeRemap CreateVisualShaderNodeRemapFileResource() => new();
    private static VisualShaderNodeIntOp CreateVisualShaderNodeIntOpFileResource() => new();
    private static VisualShaderNodeUIntOp CreateVisualShaderNodeUIntOpFileResource() => new();
    private static VisualShaderNodeIntFunc CreateVisualShaderNodeIntFuncFileResource() => new();
    private static VisualShaderNodeUIntFunc CreateVisualShaderNodeUIntFuncFileResource() => new();
    private static VisualShaderNodeFloatConstant CreateVisualShaderNodeFloatConstantFileResource() => new();
    private static VisualShaderNodeIntConstant CreateVisualShaderNodeIntConstantFileResource() => new();
    private static VisualShaderNodeUIntConstant CreateVisualShaderNodeUIntConstantFileResource() => new();
    private static VisualShaderNodeBooleanConstant CreateVisualShaderNodeBooleanConstantFileResource() => new();
    private static VisualShaderNodeVec2Constant CreateVisualShaderNodeVec2ConstantFileResource() => new();
    private static VisualShaderNodeVec3Constant CreateVisualShaderNodeVec3ConstantFileResource() => new();
    private static VisualShaderNodeVec4Constant CreateVisualShaderNodeVec4ConstantFileResource() => new();
    private static VisualShaderNodeColorConstant CreateVisualShaderNodeColorConstantFileResource() => new();
}
