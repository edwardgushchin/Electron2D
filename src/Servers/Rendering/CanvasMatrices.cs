namespace Electron2D;

internal readonly record struct CanvasModelPose(Vector4 Basis, Vector4 Origin)
{
    internal CanvasModelPose(Transform pose) : this(new(pose.X.X, pose.X.Y, pose.Y.X, pose.Y.Y), new(pose.Origin.X, pose.Origin.Y, 0, 0)) { }
}

internal readonly record struct CanvasMatrices(Transform Model, Transform Canvas)
{
    internal CanvasMatrices Local(Transform local) => this with { Model = Model * local };
    internal CanvasMatrices? For(MaterialState? material) => material?.Program is { } program &&
        (program.ModelMatrixUniform is not null || program.CanvasMatrixUniform is not null || program.ScreenMatrixUniform is not null) ? new(program.ModelMatrixUniform is not null ? Model : Transform.Identity, program.CanvasMatrixUniform is not null ? Canvas : Transform.Identity) : null;
    internal static void Write(ShaderUniform uniform, byte[][] buffers, Transform value)
    {
        Span<float> columns = stackalloc float[16] { value.X.X, value.X.Y, 0, 0, value.Y.X, value.Y.Y, 0, 0, 0, 0, 1, 0, value.Origin.X, value.Origin.Y, 0, 1 };
        uniform.WriteMatrix(buffers[uniform.Buffer].AsSpan(uniform.Offset, uniform.ElementSize), columns);
    }
}
