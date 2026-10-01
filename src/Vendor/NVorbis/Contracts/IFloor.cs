#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
namespace Electron2D.NVorbisBindings.Contracts
{
    interface IFloor
    {
        void Init(IPacket packet, int channels, int block0Size, int block1Size, ICodebook[] codebooks);

        IFloorData Unpack(IPacket packet, int blockSize, int channel);

        void Apply(IFloorData floorData, int blockSize, float[] residue);
    }
}
