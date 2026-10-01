#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
using System;

namespace Electron2D.NVorbisBindings.Contracts.Ogg
{
    interface IPacketReader
    {
        Memory<byte> GetPacketData(int pagePacketIndex);

        void InvalidatePacketCache(IPacket packet);
    }
}
