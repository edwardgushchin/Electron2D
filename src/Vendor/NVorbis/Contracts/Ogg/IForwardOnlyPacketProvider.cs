#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
namespace Electron2D.NVorbisBindings.Contracts.Ogg
{
    interface IForwardOnlyPacketProvider : IPacketProvider
    {
        bool AddPage(byte[] buf, bool isResync);
        void SetEndOfStream();
    }
}
