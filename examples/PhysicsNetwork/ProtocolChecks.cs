using System.Buffers.Binary;

namespace Electron2D.Examples.PhysicsNetwork;

internal static class ProtocolChecks
{
    internal static void Run()
    {
        var encoded = new byte[32]; var writer = new WireWriter(encoded);
        var expected = new InputCommand(17, 10, 2, 3, new(.5f, true)); writer.Input(expected);
        var reader = new WireReader(encoded); Check(reader.Input() == expected, "Input wire round trip"); reader.End();
        for (var i = 0; i < encoded.Length; i++)
        {
            var rejected = false;
            try { var truncated = new WireReader(encoded.AsSpan(0, i)); _ = truncated.Input(); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Every truncated input rejects");
        }
        foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, 1.1f, -1.1f })
        {
            BinaryPrimitives.WriteSingleLittleEndian(encoded.AsSpan(24), invalid); RejectInput(encoded);
        }
        BinaryPrimitives.WriteSingleLittleEndian(encoded.AsSpan(24), .5f);
        BinaryPrimitives.WriteUInt32LittleEndian(encoded.AsSpan(28), 2); RejectInput(encoded);
        BinaryPrimitives.WriteUInt32LittleEndian(encoded.AsSpan(28), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(encoded.AsSpan(16), 0); RejectInput(encoded);
        BinaryPrimitives.WriteUInt32LittleEndian(encoded.AsSpan(16), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(encoded.AsSpan(20), 0); RejectInput(encoded);
    }
    private static void RejectInput(byte[] bytes)
    {
        try { var reader = new WireReader(bytes); _ = reader.Input(); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Malformed game input was accepted.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
