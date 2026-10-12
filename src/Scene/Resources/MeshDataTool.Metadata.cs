namespace Electron2D;

public sealed partial class MeshDataTool
{
    private sealed class MetadataValue<T>(T value) { internal T Value = value; }
    private static void SetMetadata<T>(object?[] slots, int index, T value) { if (slots[index] is MetadataValue<T> typed) typed.Value = value; else slots[index] = new MetadataValue<T>(value); }
    private static T Metadata<T>(object?[] slots, int index) => slots[index] is MetadataValue<T> typed ? typed.Value : throw new KeyNotFoundException("No metadata of the requested exact type is stored.");
    private static bool TryMetadata<T>(object?[] slots, int index, out T value) { if (slots[index] is MetadataValue<T> typed) { value = typed.Value; return true; } value = default!; return false; }
    /// <summary>Stores a runtime-only exactly typed vertex annotation.</summary>
    /// <typeparam name="T">The type used again when reading; same-type replacements reuse their holder.</typeparam>
    /// <param name="index">Existing vertex index.</param>
    /// <param name="value">Borrowed value, including an explicitly stored null.</param>
    public void SetVertexMeta<T>(int index, T value) { lock (_gate) { Vertex(index); SetMetadata(_vertexMeta, index, value); } }
    /// <summary>Gets vertex metadata using its exact stored type.</summary>
    /// <typeparam name="T">The exact stored type.</typeparam>
    /// <param name="index">Existing vertex index.</param>
    /// <returns>Stored value, including null if explicitly stored.</returns>
    /// <exception cref="KeyNotFoundException">Metadata is absent or has another type.</exception>
    public T GetVertexMeta<T>(int index) { lock (_gate) { Vertex(index); return Metadata<T>(_vertexMeta, index); } }
    /// <summary>Tries to read vertex metadata, distinguishing absent data from an explicitly stored null.</summary>
    /// <typeparam name="T">The exact stored type.</typeparam>
    /// <param name="index">Existing vertex index.</param>
    /// <param name="value">Stored value, or default on failure.</param>
    /// <returns>Whether an annotation of this exact type is stored.</returns>
    public bool TryGetVertexMeta<T>(int index, out T value) { lock (_gate) { Vertex(index); return TryMetadata(_vertexMeta, index, out value); } }
    /// <summary>Stores a runtime-only exactly typed edge annotation.</summary>
    /// <typeparam name="T">The exact type used again when reading.</typeparam>
    /// <param name="index">Existing edge index.</param>
    /// <param name="value">Borrowed value, not copied or disposed.</param>
    public void SetEdgeMeta<T>(int index, T value) { lock (_gate) { EdgeAt(index); SetMetadata(_edgeMeta, index, value); } }
    /// <summary>Gets edge metadata using its exact stored type.</summary>
    /// <typeparam name="T">The exact stored type.</typeparam>
    /// <param name="index">Existing edge index.</param>
    /// <returns>The stored value.</returns>
    /// <exception cref="KeyNotFoundException">Metadata is absent or has another type.</exception>
    public T GetEdgeMeta<T>(int index) { lock (_gate) { EdgeAt(index); return Metadata<T>(_edgeMeta, index); } }
    /// <summary>Tries to read an edge's exactly typed runtime annotation.</summary>
    /// <typeparam name="T">The exact stored type.</typeparam>
    /// <param name="index">Existing edge index.</param>
    /// <param name="value">Stored value, or default on failure.</param>
    /// <returns>Whether this type was stored, including a null value.</returns>
    public bool TryGetEdgeMeta<T>(int index, out T value) { lock (_gate) { EdgeAt(index); return TryMetadata(_edgeMeta, index, out value); } }
    /// <summary>Stores a runtime-only exactly typed face annotation.</summary>
    /// <typeparam name="T">The exact type used again when reading.</typeparam>
    /// <param name="index">Existing face index.</param>
    /// <param name="value">Borrowed value, not copied or disposed.</param>
    public void SetFaceMeta<T>(int index, T value) { lock (_gate) { FaceAt(index); SetMetadata(_faceMeta, index, value); } }
    /// <summary>Gets face metadata using its exact stored type.</summary>
    /// <typeparam name="T">The exact stored type.</typeparam>
    /// <param name="index">Existing face index.</param>
    /// <returns>The stored value.</returns>
    /// <exception cref="KeyNotFoundException">Metadata is absent or has another type.</exception>
    public T GetFaceMeta<T>(int index) { lock (_gate) { FaceAt(index); return Metadata<T>(_faceMeta, index); } }
    /// <summary>Tries to read a face's exactly typed runtime annotation.</summary>
    /// <typeparam name="T">The exact stored type.</typeparam>
    /// <param name="index">Existing face index.</param>
    /// <param name="value">Stored value, or default on failure.</param>
    /// <returns>Whether this type was stored, including a null value.</returns>
    public bool TryGetFaceMeta<T>(int index, out T value) { lock (_gate) { FaceAt(index); return TryMetadata(_faceMeta, index, out value); } }
}
