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
    private static VisualShader CreateVisualShaderFileResource() => new();
    private static VisualShaderNodeOutput CreateVisualShaderNodeOutputFileResource() => new();
    private static VisualShaderNodeInput CreateVisualShaderNodeInputFileResource() => new();
    private static VisualShaderNodeFloatOp CreateVisualShaderNodeFloatOpFileResource() => new();
    private static VisualShaderNodeFloatConstant CreateVisualShaderNodeFloatConstantFileResource() => new();
    private static VisualShaderNodeIntConstant CreateVisualShaderNodeIntConstantFileResource() => new();
    private static VisualShaderNodeUIntConstant CreateVisualShaderNodeUIntConstantFileResource() => new();
    private static VisualShaderNodeBooleanConstant CreateVisualShaderNodeBooleanConstantFileResource() => new();
    private static VisualShaderNodeVec2Constant CreateVisualShaderNodeVec2ConstantFileResource() => new();
    private static VisualShaderNodeVec3Constant CreateVisualShaderNodeVec3ConstantFileResource() => new();
    private static VisualShaderNodeVec4Constant CreateVisualShaderNodeVec4ConstantFileResource() => new();
    private static VisualShaderNodeColorConstant CreateVisualShaderNodeColorConstantFileResource() => new();
}
