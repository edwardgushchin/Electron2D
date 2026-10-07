namespace Electron2D;

public partial class Polygon
{
    private readonly List<(string Path, float[] Weights)> _bones = [];
    private string _skeletonPath = string.Empty;
    private WeakReference<Electron2D.Skeleton>? _skinSkeleton;
    private ulong _skinPathRevision;
    private SceneTree? _skinTree;
    internal ulong SkinRevision;
    /// <summary>Gets or sets the scene path of the borrowed skeleton.</summary>
    /// <value>Empty initially. Paths resolve relative to this polygon; missing or detached skeletons leave its geometry undeformed.</value>
    /// <remarks>Inverted polygons ignore skinning. Bone paths are relative to the selected Skeleton.</remarks>
    public string Skeleton { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _skeletonPath; } set { EnsureMutable(); ValidateSkinPath(value); _skeletonPath = value; _skinSkeleton = null; _skinTree = null; SkinRevision++; InvalidateCanvas(); } }
    private static void ValidateSkinPath(string path) { ArgumentNullException.ThrowIfNull(path); if (path.Contains('\0') || path.Length > 65536) throw new ArgumentException("Invalid skin path.", nameof(path)); }
    private int BoneIndex(int index) { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); if ((uint)index >= (uint)_bones.Count) throw new ArgumentOutOfRangeException(nameof(index)); return index; }
    private static float[] CopyWeights(float[] weights) { ArgumentNullException.ThrowIfNull(weights); if (weights.Length > 65536) throw new ArgumentOutOfRangeException(nameof(weights)); var copy = (float[])weights.Clone(); foreach (var weight in copy) if (!float.IsFinite(weight)) throw new ArgumentException("Skin weights must be finite.", nameof(weights)); return copy; }
    /// <summary>Appends a bone path and copied per-source-vertex weights.</summary><param name="path">Path relative to the selected skeleton.</param><param name="weights">Finite weights; nonpositive values do not contribute. At most four strongest positive influences survive per vertex.</param>
    /// <remarks>Weights must match the selected source point count to participate; mismatched records remain authored. Entries are bounded to 4096.</remarks>
    public void AddBone(string path, float[] weights) { EnsureMutable(); ValidateSkinPath(path); var copy = CopyWeights(weights); if (_bones.Count == 4096) throw new InvalidOperationException("Bone record budget exceeded."); _bones.Add((path, copy)); SkinRevision++; InvalidateCanvas(); }
    /// <summary>Removes every authored bone record.</summary>
    public void ClearBones() { EnsureMutable(); _bones.Clear(); SkinRevision++; InvalidateCanvas(); }
    /// <summary>Removes an indexed record and compacts later records.</summary><param name="index">Valid record index.</param>
    public void EraseBone(int index) { EnsureMutable(); _bones.RemoveAt(BoneIndex(index)); SkinRevision++; InvalidateCanvas(); }
    /// <summary>Returns the number of authored records, independent of resolved palette membership.</summary><returns>Zero initially.</returns>
    public int GetBoneCount() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _bones.Count; }
    /// <summary>Returns a record's path relative to the skeleton.</summary><param name="index">Valid record index.</param><returns>The authored path.</returns>
    public string GetBonePath(int index) => _bones[BoneIndex(index)].Path;
    /// <summary>Returns an independent copy of a record's weights.</summary><param name="index">Valid record index.</param><returns>Copied finite weights.</returns>
    public float[] GetBoneWeights(int index) => (float[])_bones[BoneIndex(index)].Weights.Clone();
    /// <summary>Replaces a record path.</summary><param name="index">Valid record index.</param><param name="path">Scene path relative to the skeleton.</param>
    public void SetBonePath(int index, string path) { EnsureMutable(); BoneIndex(index); ValidateSkinPath(path); _bones[index] = (path, _bones[index].Weights); SkinRevision++; InvalidateCanvas(); }
    /// <summary>Replaces a record's copied weights.</summary><param name="index">Valid record index.</param><param name="weights">Finite values; mismatched counts do not contribute.</param>
    public void SetBoneWeights(int index, float[] weights) { EnsureMutable(); BoneIndex(index); var copy = CopyWeights(weights); _bones[index] = (_bones[index].Path, copy); SkinRevision++; InvalidateCanvas(); }
    internal int SkinInputLength => _invertEnabled || _polygons.Length == 0 ? Math.Max(0, _vertices.Length - _internalVertexCount) : _vertices.Length;
    internal (string Path, float[] Weights) SkinRecord(int index) => _bones[index];
    internal Electron2D.Skeleton? ResolveSkinSkeleton()
    {
        if (_invertEnabled || _skeletonPath.Length == 0 || _bones.Count == 0 || Tree is null) return null;
        if (!ReferenceEquals(_skinTree, Tree) || _skinPathRevision != Tree.PathRevision || _skinSkeleton is null)
        {
            var skeleton = GetNodeOrNull(_skeletonPath) as Electron2D.Skeleton;
            _skinSkeleton = skeleton is null ? null : new(skeleton); _skinPathRevision = Tree.PathRevision; _skinTree = Tree;
        }
        if (_skinSkeleton is null || !_skinSkeleton.TryGetTarget(out var target) || target.IsDisposed || !ReferenceEquals(target.Tree, Tree)) return null;
        if (!ReferenceEquals(target.CanvasViewport, CanvasViewport) || !ReferenceEquals(target.GetCanvasLayerNode(), GetCanvasLayerNode())) throw new InvalidOperationException("Skinning requires one viewport coordinate space.");
        return target;
    }
    internal override void OnTreeMembershipChanged(bool entering) { base.OnTreeMembershipChanged(entering); _skinTree = null; _skinSkeleton = null; }
    private byte[] SaveBones()
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); writer.Write(0x31424b53u); writer.Write(_bones.Count);
        foreach (var bone in _bones) { writer.Write(bone.Path); writer.Write(bone.Weights.Length); foreach (var weight in bone.Weights) writer.Write(weight); if (stream.Length > 64 * 1024 * 1024) throw new InvalidOperationException("Skin archive exceeds its byte budget."); }
        return stream.ToArray();
    }
    private void LoadBones(byte[] bytes)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length == 0) { ClearBones(); return; }
        if (bytes.Length < 8 || bytes.Length > 64 * 1024 * 1024) throw new InvalidDataException("Invalid skin archive length.");
        using var stream = new MemoryStream(bytes, false); using var reader = new BinaryReader(stream);
        if (reader.ReadUInt32() != 0x31424b53u) throw new InvalidDataException("Invalid skin archive version.");
        var count = reader.ReadInt32(); if (count < 0 || count > 4096) throw new InvalidDataException("Invalid bone record count.");
        var records = new List<(string Path, float[] Weights)>(count);
        for (var i = 0; i < count; i++) { var path = reader.ReadString(); ValidateSkinPath(path); var length = reader.ReadInt32(); if (length < 0 || length > 65536 || stream.Length - stream.Position < length * 4L) throw new InvalidDataException("Invalid skin weight length."); var weights = new float[length]; for (var j = 0; j < length; j++) { weights[j] = reader.ReadSingle(); if (!float.IsFinite(weights[j])) throw new InvalidDataException("Nonfinite skin weight."); } records.Add((path, weights)); }
        if (stream.Position != stream.Length) throw new InvalidDataException("Trailing skin bytes."); _bones.Clear(); _bones.AddRange(records); SkinRevision++; InvalidateCanvas();
    }
    private static readonly PropertyDescriptor[] SkinProperties =
    [
        new PropertyDescriptor<Polygon, string>(nameof(Skeleton), p => p.Skeleton, (p, v) => p.Skeleton = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<Polygon, byte[]>("_bone_weights", p => p.SaveBones(), (p, v) => p.LoadBones(v), _ => [], stored: true)
    ];
}
