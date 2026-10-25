namespace Electron2D;

internal readonly record struct CanvasModelPose(Vector4 Basis, Vector4 Origin)
{
    internal CanvasModelPose(Transform pose) : this(new(pose.X.X, pose.X.Y, pose.Y.X, pose.Y.Y), new(pose.Origin.X, pose.Origin.Y, 0, 0)) { }
}

internal readonly record struct CanvasMatrices(Transform Model, Transform Canvas)
{
    internal Transform? NormalModel { get; init; }
    internal CanvasMatrices Local(Transform local) => this with { Model = Model * local, NormalModel = NormalModel is { } normal ? normal * local : null };
    internal CanvasMatrices Instance(Transform local) => this with { Model = Model * local, NormalModel = NormalModel ?? Model };
    internal CanvasMatrices? For(MaterialState? material)
    {
        if (material?.Program is not { } program) return null;
        var normals = program.NormalModelBasisUniform is not null || program.NormalCanvasBasisUniform is not null;
        if (!normals && program.ModelMatrixUniform is null && program.CanvasMatrixUniform is null && program.ScreenMatrixUniform is null) return null;
        return new(program.ModelMatrixUniform is not null ? Model : Transform.Identity, normals || program.CanvasMatrixUniform is not null ? Canvas : Transform.Identity)
        { NormalModel = normals ? NormalModel ?? Model : null };
    }
    internal static Vector4 NormalBasis(Transform value)
    {
        var x = value.X.Normalized(); var y = value.Y.Normalized();
        return new(x.X, x.Y, y.X, y.Y);
    }
    internal static void Write(ShaderUniform uniform, byte[][] buffers, Transform value)
    {
        Span<float> columns = stackalloc float[16] { value.X.X, value.X.Y, 0, 0, value.Y.X, value.Y.Y, 0, 0, 0, 0, 1, 0, value.Origin.X, value.Origin.Y, 0, 1 };
        uniform.WriteMatrix(buffers[uniform.Buffer].AsSpan(uniform.Offset, uniform.ElementSize), columns);
    }
}
