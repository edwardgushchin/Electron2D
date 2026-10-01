#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
using System.Collections.Generic;

namespace Electron2D.NVorbisBindings.Contracts
{
    interface IHuffman
    {
        int TableBits { get; }
        IReadOnlyList<HuffmanListNode> PrefixTree { get; }
        IReadOnlyList<HuffmanListNode> OverflowList { get; }

        void GenerateTable(IReadOnlyList<int> value, int[] lengthList, int[] codeList);
    }
}
