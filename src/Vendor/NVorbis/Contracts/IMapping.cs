#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
namespace Electron2D.NVorbisBindings.Contracts
{
    interface IMapping
    {
        void Init(IPacket packet, int channels, IFloor[] floors, IResidue[] residues, IMdct mdct);

        void DecodePacket(IPacket packet, int blockSize, int channels, float[][] buffer);
    }
}
