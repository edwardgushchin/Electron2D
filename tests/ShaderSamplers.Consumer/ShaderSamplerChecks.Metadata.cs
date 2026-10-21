using System.Buffers.Binary;
using Electron2D;

internal static partial class ShaderSamplerChecks
{
    private static void MalformedMetadata(Shader shader, PropertyDescriptor<Shader, byte[]> descriptor, byte[] valid)
    {
        void Invalid(byte[] value) => Reject<Exception>(() => descriptor.SetValue(shader, value));
        void Int(int offset, int value) { var bytes = valid.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), value); Invalid(bytes); }
        Int(0, 2); Int(4, -1); Int(4, 33); Int(8, int.MaxValue);
        var nameLength = BinaryPrimitives.ReadInt32LittleEndian(valid.AsSpan(8)); var index = 12 + nameLength;
        var utf8 = valid.ToArray(); utf8[12] = 255; Invalid(utf8);
        var missing = valid.ToArray(); missing[12] = (byte)'x'; Invalid(missing);
        Int(index, 1); Int(index + 4, 99); Int(index + 4 + 9 * 4, 4);
        Int(index + 4 + 5 * 4, BitConverter.SingleToInt32Bits(float.NaN));
        Invalid([.. valid, 0]);
        var duplicate = new byte[valid.Length * 2 - 8]; valid.CopyTo(duplicate, 0); valid.AsSpan(8).CopyTo(duplicate.AsSpan(valid.Length));
        BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(4), 2); Invalid(duplicate);
        using var retained = shader.GetTextureSamplerState("TEXTURE")!; Check(retained.MinLOD == 1, "Malformed complete metadata preserves the previous table.");
        descriptor.SetValue(shader, []); Check(shader.GetTextureSamplerState("TEXTURE") is null, "Empty stored metadata clears the table."); descriptor.SetValue(shader, valid);
    }
}
