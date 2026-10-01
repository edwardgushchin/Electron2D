#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
using System;

namespace Electron2D.NVorbisBindings.Contracts.Ogg
{
    interface IStreamPageReader
    {
        IPacketProvider PacketProvider { get; }

        void AddPage();

        Memory<byte>[] GetPagePackets(int pageIndex);

        int FindPage(long granulePos);

        bool GetPage(int pageIndex, out long granulePos, out bool isResync, out bool isContinuation, out bool isContinued, out int packetCount, out int pageOverhead);

        void SetEndOfStream();

        int PageCount { get; }

        bool HasAllPages { get; }

        long? MaxGranulePosition { get; }

        int FirstDataPageIndex { get; }
    }
}
