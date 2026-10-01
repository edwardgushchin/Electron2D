#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
namespace Electron2D.NVorbisBindings.Contracts
{
    interface ICodebook
    {
        void Init(IPacket packet, IHuffman huffman);

        int Dimensions { get; }
        int Entries { get; }
        int MapType { get; }

        float this[int entry, int dim] { get; }

        int DecodeScalar(IPacket packet);
    }
}
