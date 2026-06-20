using System;

namespace Electron2D;

/// <summary>
/// Утилиты для формирования ключа сортировки спрайтов.
/// 16 бит — слой (ZIndex), 16 бит — порядок внутри слоя.
/// </summary>
public static class DrawOrder
{
    public static uint Pack(ushort layer, ushort orderInLayer) => ((uint)layer << 16) | orderInLayer;

    public static void Unpack(uint sortKey, out ushort layer, out ushort orderInLayer)
    {
        layer = (ushort)(sortKey >> 16);
        orderInLayer = (ushort)sortKey;
    }

    public static uint PackClamped(int layer, int orderInLayer)
    {
        layer = Math.Clamp(layer, 0, ushort.MaxValue);
        orderInLayer = Math.Clamp(orderInLayer, 0, ushort.MaxValue);
        return Pack((ushort)layer, (ushort)orderInLayer);
    }
}