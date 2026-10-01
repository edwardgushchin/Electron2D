#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
using System;

namespace Electron2D.NVorbisBindings.Contracts.Ogg
{
    interface IPageReader : IDisposable
    {
        void Lock();
        bool Release();

        long ContainerBits { get; }
        long WasteBits { get; }

        bool ReadNextPage();

        bool ReadPageAt(long offset);
    }
}
