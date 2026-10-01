#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
namespace Electron2D.NVorbisBindings.Contracts
{
    interface IMode
    {
        void Init(IPacket packet, int channels, int block0Size, int block1Size, IMapping[] mappings);

        bool Decode(IPacket packet, float[][] buffer, out int packetStartindex, out int packetValidLength, out int packetTotalLength);

        int GetPacketSampleCount(IPacket packet);
    }
}
