#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
namespace Electron2D.NVorbisBindings.Contracts
{
    interface IFactory
    {
        ICodebook CreateCodebook();
        IFloor CreateFloor(IPacket packet);
        IResidue CreateResidue(IPacket packet);
        IMapping CreateMapping(IPacket packet);
        IMode CreateMode();
        IMdct CreateMdct();
        IHuffman CreateHuffman();
    }
}
