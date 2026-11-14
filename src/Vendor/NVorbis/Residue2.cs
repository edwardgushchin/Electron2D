#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
using Electron2D.NVorbisBindings.Contracts;

namespace Electron2D.NVorbisBindings
{
    // all channels in one pass, interleaved
    class Residue2 : Residue0
    {
        public override void Decode(IPacket packet, bool[] doNotDecodeChannel, int blockSize, float[][] buffer)
        {
            DecodeCore(packet, doNotDecodeChannel, blockSize * buffer.Length, buffer, 1, true);
        }

        protected override bool WriteVectors(ICodebook codebook, IPacket packet, float[][] residue, int channel, int offset, int partitionSize)
        {
            var channels = residue.Length;
            var chPtr = offset % channels;
            offset /= channels;
            for (int c = 0; c < partitionSize;)
            {
                var entry = codebook.DecodeScalar(packet);
                if (entry == -1)
                {
                    return true;
                }
                for (var d = 0; d < codebook.Dimensions && c < partitionSize; d++, c++)
                {
                    residue[chPtr][offset] += codebook[entry, d];
                    if (++chPtr == channels)
                    {
                        chPtr = 0;
                        offset++;
                    }
                }
            }

            return false;
        }
    }
}
