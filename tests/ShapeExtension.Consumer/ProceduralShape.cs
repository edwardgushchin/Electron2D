using Electron2D;

internal sealed class ProceduralShape(ShapeGeometry geometry) : Shape
{
    private ShapeGeometry _geometry = geometry;
    internal int Builds { get; private set; }
    internal Action? OnBuild { get; set; }
    internal bool ReturnNull { get; set; }
    internal void Invalidate() => EmitGeometryChanged();
    internal void Update(ShapeGeometry geometry) { ThrowIfDisposed(); _geometry = geometry; EmitGeometryChanged(); }
    protected override ShapeGeometry GetGeometryCore() { Builds++; OnBuild?.Invoke(); return ReturnNull ? null! : _geometry; }
    protected override Resource CreateDuplicateInstance() => new ProceduralShape(_geometry);
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode,
        Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> forceDuplicate)
    {
        base.CopyCustomStateTo(target, deep, mode, duplicate, forceDuplicate);
        ((ProceduralShape)target).Update(_geometry);
    }
}
