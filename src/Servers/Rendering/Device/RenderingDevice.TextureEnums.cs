namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    /// <summary>Encoded pixel and compressed-block formats queried against the active device.</summary>
    public enum DataFormat
    {
        /// <summary>A1R5G5B5UNORMPACK16 encoded pixel/block format; query support before creation.</summary>
        A1R5G5B5UNORMPACK16 = 7,
        /// <summary>A2B10G10R10SINTPACK32 encoded pixel/block format; query support before creation.</summary>
        A2B10G10R10SINTPACK32 = 68,
        /// <summary>A2B10G10R10SNORMPACK32 encoded pixel/block format; query support before creation.</summary>
        A2B10G10R10SNORMPACK32 = 64,
        /// <summary>A2B10G10R10SSCALEDPACK32 encoded pixel/block format; query support before creation.</summary>
        A2B10G10R10SSCALEDPACK32 = 66,
        /// <summary>A2B10G10R10UINTPACK32 encoded pixel/block format; query support before creation.</summary>
        A2B10G10R10UINTPACK32 = 67,
        /// <summary>A2B10G10R10UNORMPACK32 encoded pixel/block format; query support before creation.</summary>
        A2B10G10R10UNORMPACK32 = 63,
        /// <summary>A2B10G10R10USCALEDPACK32 encoded pixel/block format; query support before creation.</summary>
        A2B10G10R10USCALEDPACK32 = 65,
        /// <summary>A2R10G10B10SINTPACK32 encoded pixel/block format; query support before creation.</summary>
        A2R10G10B10SINTPACK32 = 62,
        /// <summary>A2R10G10B10SNORMPACK32 encoded pixel/block format; query support before creation.</summary>
        A2R10G10B10SNORMPACK32 = 58,
        /// <summary>A2R10G10B10SSCALEDPACK32 encoded pixel/block format; query support before creation.</summary>
        A2R10G10B10SSCALEDPACK32 = 60,
        /// <summary>A2R10G10B10UINTPACK32 encoded pixel/block format; query support before creation.</summary>
        A2R10G10B10UINTPACK32 = 61,
        /// <summary>A2R10G10B10UNORMPACK32 encoded pixel/block format; query support before creation.</summary>
        A2R10G10B10UNORMPACK32 = 57,
        /// <summary>A2R10G10B10USCALEDPACK32 encoded pixel/block format; query support before creation.</summary>
        A2R10G10B10USCALEDPACK32 = 59,
        /// <summary>A8B8G8R8SINTPACK32 encoded pixel/block format; query support before creation.</summary>
        A8B8G8R8SINTPACK32 = 55,
        /// <summary>A8B8G8R8SNORMPACK32 encoded pixel/block format; query support before creation.</summary>
        A8B8G8R8SNORMPACK32 = 51,
        /// <summary>A8B8G8R8SRGBPACK32 encoded pixel/block format; query support before creation.</summary>
        A8B8G8R8SRGBPACK32 = 56,
        /// <summary>A8B8G8R8SSCALEDPACK32 encoded pixel/block format; query support before creation.</summary>
        A8B8G8R8SSCALEDPACK32 = 53,
        /// <summary>A8B8G8R8UINTPACK32 encoded pixel/block format; query support before creation.</summary>
        A8B8G8R8UINTPACK32 = 54,
        /// <summary>A8B8G8R8UNORMPACK32 encoded pixel/block format; query support before creation.</summary>
        A8B8G8R8UNORMPACK32 = 50,
        /// <summary>A8B8G8R8USCALEDPACK32 encoded pixel/block format; query support before creation.</summary>
        A8B8G8R8USCALEDPACK32 = 52,
        /// <summary>ASTC10X10SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC10X10SFLOATBLOCK = 229,
        /// <summary>ASTC10X10SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC10X10SRGBBLOCK = 179,
        /// <summary>ASTC10X10UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC10X10UNORMBLOCK = 178,
        /// <summary>ASTC10X5SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC10X5SFLOATBLOCK = 226,
        /// <summary>ASTC10X5SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC10X5SRGBBLOCK = 173,
        /// <summary>ASTC10X5UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC10X5UNORMBLOCK = 172,
        /// <summary>ASTC10X6SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC10X6SFLOATBLOCK = 227,
        /// <summary>ASTC10X6SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC10X6SRGBBLOCK = 175,
        /// <summary>ASTC10X6UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC10X6UNORMBLOCK = 174,
        /// <summary>ASTC10X8SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC10X8SFLOATBLOCK = 228,
        /// <summary>ASTC10X8SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC10X8SRGBBLOCK = 177,
        /// <summary>ASTC10X8UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC10X8UNORMBLOCK = 176,
        /// <summary>ASTC12X10SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC12X10SFLOATBLOCK = 230,
        /// <summary>ASTC12X10SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC12X10SRGBBLOCK = 181,
        /// <summary>ASTC12X10UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC12X10UNORMBLOCK = 180,
        /// <summary>ASTC12X12SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC12X12SFLOATBLOCK = 231,
        /// <summary>ASTC12X12SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC12X12SRGBBLOCK = 183,
        /// <summary>ASTC12X12UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC12X12UNORMBLOCK = 182,
        /// <summary>ASTC4X4SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC4X4SFLOATBLOCK = 218,
        /// <summary>ASTC4X4SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC4X4SRGBBLOCK = 157,
        /// <summary>ASTC4X4UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC4X4UNORMBLOCK = 156,
        /// <summary>ASTC5X4SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC5X4SFLOATBLOCK = 219,
        /// <summary>ASTC5X4SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC5X4SRGBBLOCK = 159,
        /// <summary>ASTC5X4UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC5X4UNORMBLOCK = 158,
        /// <summary>ASTC5X5SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC5X5SFLOATBLOCK = 220,
        /// <summary>ASTC5X5SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC5X5SRGBBLOCK = 161,
        /// <summary>ASTC5X5UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC5X5UNORMBLOCK = 160,
        /// <summary>ASTC6X5SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC6X5SFLOATBLOCK = 221,
        /// <summary>ASTC6X5SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC6X5SRGBBLOCK = 163,
        /// <summary>ASTC6X5UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC6X5UNORMBLOCK = 162,
        /// <summary>ASTC6X6SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC6X6SFLOATBLOCK = 222,
        /// <summary>ASTC6X6SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC6X6SRGBBLOCK = 165,
        /// <summary>ASTC6X6UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC6X6UNORMBLOCK = 164,
        /// <summary>ASTC8X5SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC8X5SFLOATBLOCK = 223,
        /// <summary>ASTC8X5SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC8X5SRGBBLOCK = 167,
        /// <summary>ASTC8X5UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC8X5UNORMBLOCK = 166,
        /// <summary>ASTC8X6SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC8X6SFLOATBLOCK = 224,
        /// <summary>ASTC8X6SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC8X6SRGBBLOCK = 169,
        /// <summary>ASTC8X6UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC8X6UNORMBLOCK = 168,
        /// <summary>ASTC8X8SFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC8X8SFLOATBLOCK = 225,
        /// <summary>ASTC8X8SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC8X8SRGBBLOCK = 171,
        /// <summary>ASTC8X8UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ASTC8X8UNORMBLOCK = 170,
        /// <summary>B10G11R11UFLOATPACK32 encoded pixel/block format; query support before creation.</summary>
        B10G11R11UFLOATPACK32 = 121,
        /// <summary>B10X6G10X6R10X6G10X6422UNORM4PACK16 encoded pixel/block format; query support before creation.</summary>
        B10X6G10X6R10X6G10X6422UNORM4PACK16 = 195,
        /// <summary>B12X4G12X4R12X4G12X4422UNORM4PACK16 encoded pixel/block format; query support before creation.</summary>
        B12X4G12X4R12X4G12X4422UNORM4PACK16 = 205,
        /// <summary>B16G16R16G16422UNORM encoded pixel/block format; query support before creation.</summary>
        B16G16R16G16422UNORM = 212,
        /// <summary>B4G4R4A4UNORMPACK16 encoded pixel/block format; query support before creation.</summary>
        B4G4R4A4UNORMPACK16 = 2,
        /// <summary>B5G5R5A1UNORMPACK16 encoded pixel/block format; query support before creation.</summary>
        B5G5R5A1UNORMPACK16 = 6,
        /// <summary>B5G6R5UNORMPACK16 encoded pixel/block format; query support before creation.</summary>
        B5G6R5UNORMPACK16 = 4,
        /// <summary>B8G8R8A8SINT encoded pixel/block format; query support before creation.</summary>
        B8G8R8A8SINT = 48,
        /// <summary>B8G8R8A8SNORM encoded pixel/block format; query support before creation.</summary>
        B8G8R8A8SNORM = 44,
        /// <summary>B8G8R8A8SRGB encoded pixel/block format; query support before creation.</summary>
        B8G8R8A8SRGB = 49,
        /// <summary>B8G8R8A8SSCALED encoded pixel/block format; query support before creation.</summary>
        B8G8R8A8SSCALED = 46,
        /// <summary>B8G8R8A8UINT encoded pixel/block format; query support before creation.</summary>
        B8G8R8A8UINT = 47,
        /// <summary>B8G8R8A8UNORM encoded pixel/block format; query support before creation.</summary>
        B8G8R8A8UNORM = 43,
        /// <summary>B8G8R8A8USCALED encoded pixel/block format; query support before creation.</summary>
        B8G8R8A8USCALED = 45,
        /// <summary>B8G8R8G8422UNORM encoded pixel/block format; query support before creation.</summary>
        B8G8R8G8422UNORM = 185,
        /// <summary>B8G8R8SINT encoded pixel/block format; query support before creation.</summary>
        B8G8R8SINT = 34,
        /// <summary>B8G8R8SNORM encoded pixel/block format; query support before creation.</summary>
        B8G8R8SNORM = 30,
        /// <summary>B8G8R8SRGB encoded pixel/block format; query support before creation.</summary>
        B8G8R8SRGB = 35,
        /// <summary>B8G8R8SSCALED encoded pixel/block format; query support before creation.</summary>
        B8G8R8SSCALED = 32,
        /// <summary>B8G8R8UINT encoded pixel/block format; query support before creation.</summary>
        B8G8R8UINT = 33,
        /// <summary>B8G8R8UNORM encoded pixel/block format; query support before creation.</summary>
        B8G8R8UNORM = 29,
        /// <summary>B8G8R8USCALED encoded pixel/block format; query support before creation.</summary>
        B8G8R8USCALED = 31,
        /// <summary>BC1RGBASRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        BC1RGBASRGBBLOCK = 133,
        /// <summary>BC1RGBAUNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        BC1RGBAUNORMBLOCK = 132,
        /// <summary>BC1RGBSRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        BC1RGBSRGBBLOCK = 131,
        /// <summary>BC1RGBUNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        BC1RGBUNORMBLOCK = 130,
        /// <summary>BC2SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        BC2SRGBBLOCK = 135,
        /// <summary>BC2UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        BC2UNORMBLOCK = 134,
        /// <summary>BC3SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        BC3SRGBBLOCK = 137,
        /// <summary>BC3UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        BC3UNORMBLOCK = 136,
        /// <summary>BC4SNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        BC4SNORMBLOCK = 139,
        /// <summary>BC4UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        BC4UNORMBLOCK = 138,
        /// <summary>BC5SNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        BC5SNORMBLOCK = 141,
        /// <summary>BC5UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        BC5UNORMBLOCK = 140,
        /// <summary>BC6HSFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        BC6HSFLOATBLOCK = 143,
        /// <summary>BC6HUFLOATBLOCK encoded pixel/block format; query support before creation.</summary>
        BC6HUFLOATBLOCK = 142,
        /// <summary>BC7SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        BC7SRGBBLOCK = 145,
        /// <summary>BC7UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        BC7UNORMBLOCK = 144,
        /// <summary>D16UNORM encoded pixel/block format; query support before creation.</summary>
        D16UNORM = 123,
        /// <summary>D16UNORMS8UINT encoded pixel/block format; query support before creation.</summary>
        D16UNORMS8UINT = 127,
        /// <summary>D24UNORMS8UINT encoded pixel/block format; query support before creation.</summary>
        D24UNORMS8UINT = 128,
        /// <summary>D32SFLOAT encoded pixel/block format; query support before creation.</summary>
        D32SFLOAT = 125,
        /// <summary>D32SFLOATS8UINT encoded pixel/block format; query support before creation.</summary>
        D32SFLOATS8UINT = 129,
        /// <summary>E5B9G9R9UFLOATPACK32 encoded pixel/block format; query support before creation.</summary>
        E5B9G9R9UFLOATPACK32 = 122,
        /// <summary>EACR11G11SNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        EACR11G11SNORMBLOCK = 155,
        /// <summary>EACR11G11UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        EACR11G11UNORMBLOCK = 154,
        /// <summary>EACR11SNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        EACR11SNORMBLOCK = 153,
        /// <summary>EACR11UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        EACR11UNORMBLOCK = 152,
        /// <summary>ETC2R8G8B8A1SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ETC2R8G8B8A1SRGBBLOCK = 149,
        /// <summary>ETC2R8G8B8A1UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ETC2R8G8B8A1UNORMBLOCK = 148,
        /// <summary>ETC2R8G8B8A8SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ETC2R8G8B8A8SRGBBLOCK = 151,
        /// <summary>ETC2R8G8B8A8UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ETC2R8G8B8A8UNORMBLOCK = 150,
        /// <summary>ETC2R8G8B8SRGBBLOCK encoded pixel/block format; query support before creation.</summary>
        ETC2R8G8B8SRGBBLOCK = 147,
        /// <summary>ETC2R8G8B8UNORMBLOCK encoded pixel/block format; query support before creation.</summary>
        ETC2R8G8B8UNORMBLOCK = 146,
        /// <summary>G10X6B10X6G10X6R10X6422UNORM4PACK16 encoded pixel/block format; query support before creation.</summary>
        G10X6B10X6G10X6R10X6422UNORM4PACK16 = 194,
        /// <summary>G10X6B10X6R10X62PLANE420UNORM3PACK16 encoded pixel/block format; query support before creation.</summary>
        G10X6B10X6R10X62PLANE420UNORM3PACK16 = 197,
        /// <summary>G10X6B10X6R10X62PLANE422UNORM3PACK16 encoded pixel/block format; query support before creation.</summary>
        G10X6B10X6R10X62PLANE422UNORM3PACK16 = 199,
        /// <summary>G10X6B10X6R10X63PLANE420UNORM3PACK16 encoded pixel/block format; query support before creation.</summary>
        G10X6B10X6R10X63PLANE420UNORM3PACK16 = 196,
        /// <summary>G10X6B10X6R10X63PLANE422UNORM3PACK16 encoded pixel/block format; query support before creation.</summary>
        G10X6B10X6R10X63PLANE422UNORM3PACK16 = 198,
        /// <summary>G10X6B10X6R10X63PLANE444UNORM3PACK16 encoded pixel/block format; query support before creation.</summary>
        G10X6B10X6R10X63PLANE444UNORM3PACK16 = 200,
        /// <summary>G12X4B12X4G12X4R12X4422UNORM4PACK16 encoded pixel/block format; query support before creation.</summary>
        G12X4B12X4G12X4R12X4422UNORM4PACK16 = 204,
        /// <summary>G12X4B12X4R12X42PLANE420UNORM3PACK16 encoded pixel/block format; query support before creation.</summary>
        G12X4B12X4R12X42PLANE420UNORM3PACK16 = 207,
        /// <summary>G12X4B12X4R12X42PLANE422UNORM3PACK16 encoded pixel/block format; query support before creation.</summary>
        G12X4B12X4R12X42PLANE422UNORM3PACK16 = 209,
        /// <summary>G12X4B12X4R12X43PLANE420UNORM3PACK16 encoded pixel/block format; query support before creation.</summary>
        G12X4B12X4R12X43PLANE420UNORM3PACK16 = 206,
        /// <summary>G12X4B12X4R12X43PLANE422UNORM3PACK16 encoded pixel/block format; query support before creation.</summary>
        G12X4B12X4R12X43PLANE422UNORM3PACK16 = 208,
        /// <summary>G12X4B12X4R12X43PLANE444UNORM3PACK16 encoded pixel/block format; query support before creation.</summary>
        G12X4B12X4R12X43PLANE444UNORM3PACK16 = 210,
        /// <summary>G16B16G16R16422UNORM encoded pixel/block format; query support before creation.</summary>
        G16B16G16R16422UNORM = 211,
        /// <summary>G16B16R162PLANE420UNORM encoded pixel/block format; query support before creation.</summary>
        G16B16R162PLANE420UNORM = 214,
        /// <summary>G16B16R162PLANE422UNORM encoded pixel/block format; query support before creation.</summary>
        G16B16R162PLANE422UNORM = 216,
        /// <summary>G16B16R163PLANE420UNORM encoded pixel/block format; query support before creation.</summary>
        G16B16R163PLANE420UNORM = 213,
        /// <summary>G16B16R163PLANE422UNORM encoded pixel/block format; query support before creation.</summary>
        G16B16R163PLANE422UNORM = 215,
        /// <summary>G16B16R163PLANE444UNORM encoded pixel/block format; query support before creation.</summary>
        G16B16R163PLANE444UNORM = 217,
        /// <summary>G8B8G8R8422UNORM encoded pixel/block format; query support before creation.</summary>
        G8B8G8R8422UNORM = 184,
        /// <summary>G8B8R82PLANE420UNORM encoded pixel/block format; query support before creation.</summary>
        G8B8R82PLANE420UNORM = 187,
        /// <summary>G8B8R82PLANE422UNORM encoded pixel/block format; query support before creation.</summary>
        G8B8R82PLANE422UNORM = 189,
        /// <summary>G8B8R83PLANE420UNORM encoded pixel/block format; query support before creation.</summary>
        G8B8R83PLANE420UNORM = 186,
        /// <summary>G8B8R83PLANE422UNORM encoded pixel/block format; query support before creation.</summary>
        G8B8R83PLANE422UNORM = 188,
        /// <summary>G8B8R83PLANE444UNORM encoded pixel/block format; query support before creation.</summary>
        G8B8R83PLANE444UNORM = 190,
        /// <summary>Nonselectable upper bound; DataFormat.Max also means no view override.</summary>
        Max = 232,
        /// <summary>R10X6G10X6B10X6A10X6UNORM4PACK16 encoded pixel/block format; query support before creation.</summary>
        R10X6G10X6B10X6A10X6UNORM4PACK16 = 193,
        /// <summary>R10X6G10X6UNORM2PACK16 encoded pixel/block format; query support before creation.</summary>
        R10X6G10X6UNORM2PACK16 = 192,
        /// <summary>R10X6UNORMPACK16 encoded pixel/block format; query support before creation.</summary>
        R10X6UNORMPACK16 = 191,
        /// <summary>R12X4G12X4B12X4A12X4UNORM4PACK16 encoded pixel/block format; query support before creation.</summary>
        R12X4G12X4B12X4A12X4UNORM4PACK16 = 203,
        /// <summary>R12X4G12X4UNORM2PACK16 encoded pixel/block format; query support before creation.</summary>
        R12X4G12X4UNORM2PACK16 = 202,
        /// <summary>R12X4UNORMPACK16 encoded pixel/block format; query support before creation.</summary>
        R12X4UNORMPACK16 = 201,
        /// <summary>R16G16B16A16SFLOAT encoded pixel/block format; query support before creation.</summary>
        R16G16B16A16SFLOAT = 96,
        /// <summary>R16G16B16A16SINT encoded pixel/block format; query support before creation.</summary>
        R16G16B16A16SINT = 95,
        /// <summary>R16G16B16A16SNORM encoded pixel/block format; query support before creation.</summary>
        R16G16B16A16SNORM = 91,
        /// <summary>R16G16B16A16SSCALED encoded pixel/block format; query support before creation.</summary>
        R16G16B16A16SSCALED = 93,
        /// <summary>R16G16B16A16UINT encoded pixel/block format; query support before creation.</summary>
        R16G16B16A16UINT = 94,
        /// <summary>R16G16B16A16UNORM encoded pixel/block format; query support before creation.</summary>
        R16G16B16A16UNORM = 90,
        /// <summary>R16G16B16A16USCALED encoded pixel/block format; query support before creation.</summary>
        R16G16B16A16USCALED = 92,
        /// <summary>R16G16B16SFLOAT encoded pixel/block format; query support before creation.</summary>
        R16G16B16SFLOAT = 89,
        /// <summary>R16G16B16SINT encoded pixel/block format; query support before creation.</summary>
        R16G16B16SINT = 88,
        /// <summary>R16G16B16SNORM encoded pixel/block format; query support before creation.</summary>
        R16G16B16SNORM = 84,
        /// <summary>R16G16B16SSCALED encoded pixel/block format; query support before creation.</summary>
        R16G16B16SSCALED = 86,
        /// <summary>R16G16B16UINT encoded pixel/block format; query support before creation.</summary>
        R16G16B16UINT = 87,
        /// <summary>R16G16B16UNORM encoded pixel/block format; query support before creation.</summary>
        R16G16B16UNORM = 83,
        /// <summary>R16G16B16USCALED encoded pixel/block format; query support before creation.</summary>
        R16G16B16USCALED = 85,
        /// <summary>R16G16SFLOAT encoded pixel/block format; query support before creation.</summary>
        R16G16SFLOAT = 82,
        /// <summary>R16G16SINT encoded pixel/block format; query support before creation.</summary>
        R16G16SINT = 81,
        /// <summary>R16G16SNORM encoded pixel/block format; query support before creation.</summary>
        R16G16SNORM = 77,
        /// <summary>R16G16SSCALED encoded pixel/block format; query support before creation.</summary>
        R16G16SSCALED = 79,
        /// <summary>R16G16UINT encoded pixel/block format; query support before creation.</summary>
        R16G16UINT = 80,
        /// <summary>R16G16UNORM encoded pixel/block format; query support before creation.</summary>
        R16G16UNORM = 76,
        /// <summary>R16G16USCALED encoded pixel/block format; query support before creation.</summary>
        R16G16USCALED = 78,
        /// <summary>R16SFLOAT encoded pixel/block format; query support before creation.</summary>
        R16SFLOAT = 75,
        /// <summary>R16SINT encoded pixel/block format; query support before creation.</summary>
        R16SINT = 74,
        /// <summary>R16SNORM encoded pixel/block format; query support before creation.</summary>
        R16SNORM = 70,
        /// <summary>R16SSCALED encoded pixel/block format; query support before creation.</summary>
        R16SSCALED = 72,
        /// <summary>R16UINT encoded pixel/block format; query support before creation.</summary>
        R16UINT = 73,
        /// <summary>R16UNORM encoded pixel/block format; query support before creation.</summary>
        R16UNORM = 69,
        /// <summary>R16USCALED encoded pixel/block format; query support before creation.</summary>
        R16USCALED = 71,
        /// <summary>R32G32B32A32SFLOAT encoded pixel/block format; query support before creation.</summary>
        R32G32B32A32SFLOAT = 108,
        /// <summary>R32G32B32A32SINT encoded pixel/block format; query support before creation.</summary>
        R32G32B32A32SINT = 107,
        /// <summary>R32G32B32A32UINT encoded pixel/block format; query support before creation.</summary>
        R32G32B32A32UINT = 106,
        /// <summary>R32G32B32SFLOAT encoded pixel/block format; query support before creation.</summary>
        R32G32B32SFLOAT = 105,
        /// <summary>R32G32B32SINT encoded pixel/block format; query support before creation.</summary>
        R32G32B32SINT = 104,
        /// <summary>R32G32B32UINT encoded pixel/block format; query support before creation.</summary>
        R32G32B32UINT = 103,
        /// <summary>R32G32SFLOAT encoded pixel/block format; query support before creation.</summary>
        R32G32SFLOAT = 102,
        /// <summary>R32G32SINT encoded pixel/block format; query support before creation.</summary>
        R32G32SINT = 101,
        /// <summary>R32G32UINT encoded pixel/block format; query support before creation.</summary>
        R32G32UINT = 100,
        /// <summary>R32SFLOAT encoded pixel/block format; query support before creation.</summary>
        R32SFLOAT = 99,
        /// <summary>R32SINT encoded pixel/block format; query support before creation.</summary>
        R32SINT = 98,
        /// <summary>R32UINT encoded pixel/block format; query support before creation.</summary>
        R32UINT = 97,
        /// <summary>R4G4B4A4UNORMPACK16 encoded pixel/block format; query support before creation.</summary>
        R4G4B4A4UNORMPACK16 = 1,
        /// <summary>R4G4UNORMPACK8 encoded pixel/block format; query support before creation.</summary>
        R4G4UNORMPACK8 = 0,
        /// <summary>R5G5B5A1UNORMPACK16 encoded pixel/block format; query support before creation.</summary>
        R5G5B5A1UNORMPACK16 = 5,
        /// <summary>R5G6B5UNORMPACK16 encoded pixel/block format; query support before creation.</summary>
        R5G6B5UNORMPACK16 = 3,
        /// <summary>R64G64B64A64SFLOAT encoded pixel/block format; query support before creation.</summary>
        R64G64B64A64SFLOAT = 120,
        /// <summary>R64G64B64A64SINT encoded pixel/block format; query support before creation.</summary>
        R64G64B64A64SINT = 119,
        /// <summary>R64G64B64A64UINT encoded pixel/block format; query support before creation.</summary>
        R64G64B64A64UINT = 118,
        /// <summary>R64G64B64SFLOAT encoded pixel/block format; query support before creation.</summary>
        R64G64B64SFLOAT = 117,
        /// <summary>R64G64B64SINT encoded pixel/block format; query support before creation.</summary>
        R64G64B64SINT = 116,
        /// <summary>R64G64B64UINT encoded pixel/block format; query support before creation.</summary>
        R64G64B64UINT = 115,
        /// <summary>R64G64SFLOAT encoded pixel/block format; query support before creation.</summary>
        R64G64SFLOAT = 114,
        /// <summary>R64G64SINT encoded pixel/block format; query support before creation.</summary>
        R64G64SINT = 113,
        /// <summary>R64G64UINT encoded pixel/block format; query support before creation.</summary>
        R64G64UINT = 112,
        /// <summary>R64SFLOAT encoded pixel/block format; query support before creation.</summary>
        R64SFLOAT = 111,
        /// <summary>R64SINT encoded pixel/block format; query support before creation.</summary>
        R64SINT = 110,
        /// <summary>R64UINT encoded pixel/block format; query support before creation.</summary>
        R64UINT = 109,
        /// <summary>R8G8B8A8SINT encoded pixel/block format; query support before creation.</summary>
        R8G8B8A8SINT = 41,
        /// <summary>R8G8B8A8SNORM encoded pixel/block format; query support before creation.</summary>
        R8G8B8A8SNORM = 37,
        /// <summary>R8G8B8A8SRGB encoded pixel/block format; query support before creation.</summary>
        R8G8B8A8SRGB = 42,
        /// <summary>R8G8B8A8SSCALED encoded pixel/block format; query support before creation.</summary>
        R8G8B8A8SSCALED = 39,
        /// <summary>R8G8B8A8UINT encoded pixel/block format; query support before creation.</summary>
        R8G8B8A8UINT = 40,
        /// <summary>R8G8B8A8UNORM encoded pixel/block format; query support before creation.</summary>
        R8G8B8A8UNORM = 36,
        /// <summary>R8G8B8A8USCALED encoded pixel/block format; query support before creation.</summary>
        R8G8B8A8USCALED = 38,
        /// <summary>R8G8B8SINT encoded pixel/block format; query support before creation.</summary>
        R8G8B8SINT = 27,
        /// <summary>R8G8B8SNORM encoded pixel/block format; query support before creation.</summary>
        R8G8B8SNORM = 23,
        /// <summary>R8G8B8SRGB encoded pixel/block format; query support before creation.</summary>
        R8G8B8SRGB = 28,
        /// <summary>R8G8B8SSCALED encoded pixel/block format; query support before creation.</summary>
        R8G8B8SSCALED = 25,
        /// <summary>R8G8B8UINT encoded pixel/block format; query support before creation.</summary>
        R8G8B8UINT = 26,
        /// <summary>R8G8B8UNORM encoded pixel/block format; query support before creation.</summary>
        R8G8B8UNORM = 22,
        /// <summary>R8G8B8USCALED encoded pixel/block format; query support before creation.</summary>
        R8G8B8USCALED = 24,
        /// <summary>R8G8SINT encoded pixel/block format; query support before creation.</summary>
        R8G8SINT = 20,
        /// <summary>R8G8SNORM encoded pixel/block format; query support before creation.</summary>
        R8G8SNORM = 16,
        /// <summary>R8G8SRGB encoded pixel/block format; query support before creation.</summary>
        R8G8SRGB = 21,
        /// <summary>R8G8SSCALED encoded pixel/block format; query support before creation.</summary>
        R8G8SSCALED = 18,
        /// <summary>R8G8UINT encoded pixel/block format; query support before creation.</summary>
        R8G8UINT = 19,
        /// <summary>R8G8UNORM encoded pixel/block format; query support before creation.</summary>
        R8G8UNORM = 15,
        /// <summary>R8G8USCALED encoded pixel/block format; query support before creation.</summary>
        R8G8USCALED = 17,
        /// <summary>R8SINT encoded pixel/block format; query support before creation.</summary>
        R8SINT = 13,
        /// <summary>R8SNORM encoded pixel/block format; query support before creation.</summary>
        R8SNORM = 9,
        /// <summary>R8SRGB encoded pixel/block format; query support before creation.</summary>
        R8SRGB = 14,
        /// <summary>R8SSCALED encoded pixel/block format; query support before creation.</summary>
        R8SSCALED = 11,
        /// <summary>R8UINT encoded pixel/block format; query support before creation.</summary>
        R8UINT = 12,
        /// <summary>R8UNORM encoded pixel/block format; query support before creation.</summary>
        R8UNORM = 8,
        /// <summary>R8USCALED encoded pixel/block format; query support before creation.</summary>
        R8USCALED = 10,
        /// <summary>S8UINT encoded pixel/block format; query support before creation.</summary>
        S8UINT = 126,
        /// <summary>X8D24UNORMPACK32 encoded pixel/block format; query support before creation.</summary>
        X8D24UNORMPACK32 = 124,
    }
    /// <summary>Texture allocation, sampling and transfer roles; unsupported role combinations fail creation.</summary>
    [Flags]
    public enum TextureUsageBits
    {
        /// <summary>Allows use as a copy source or CPU readback source.</summary>
        CanCopyFrom = 128,
        /// <summary>Allows image region copies and selected mip/layer clears as a destination.</summary>
        CanCopyTo = 256,
        /// <summary>Allows replacement of one complete layer mip chain.</summary>
        CanUpdate = 64,
        /// <summary>Allows a color attachment in an executable framebuffer.</summary>
        ColorAttachment = 2,
        /// <summary>Allows explicit synchronous or queued CPU readback.</summary>
        CPURead = 32,
        /// <summary>Requires depth resolve attachment integration.</summary>
        DepthResolveAttachment = 4096,
        /// <summary>Allows a depth/stencil attachment with supported native sampling.</summary>
        DepthStencilAttachment = 4,
        /// <summary>Allows ordered single-sample or multisample framebuffer input reads.</summary>
        InputAttachment = 512,
        /// <summary>Allows shader sampling with a sampler.</summary>
        Sampling = 1,
        /// <summary>Checks signed or unsigned R32 storage-image atomic capability; Image bindings also require Storage.</summary>
        StorageAtomic = 16,
        /// <summary>Allows read/write compute access where the format supports simultaneous access.</summary>
        Storage = 8,
    }
    /// <summary>Sampled component sources; the current profile accepts identity mappings.</summary>
    public enum TextureSwizzle
    {
        /// <summary>Selects the alpha component.</summary>
        A = 6,
        /// <summary>Selects the blue component.</summary>
        B = 5,
        /// <summary>Selects the green component.</summary>
        G = 4,
        /// <summary>Preserves the component owning this swizzle.</summary>
        Identity = 0,
        /// <summary>Nonselectable upper bound; DataFormat.Max also means no view override.</summary>
        Max = 7,
        /// <summary>Selects constant one; requires a view adapter.</summary>
        One = 2,
        /// <summary>Selects the red component.</summary>
        R = 3,
        /// <summary>Selects constant zero; requires a view adapter.</summary>
        Zero = 1,
    }
    /// <summary>Filtering within a mip level or between mip levels.</summary>
    public enum SamplerFilter
    {
        /// <summary>Interpolates neighboring texels or adjacent mip levels.</summary>
        Linear = 1,
        /// <summary>Selects the nearest texel or mip level.</summary>
        Nearest = 0,
    }
    /// <summary>Addressing outside the normalized zero-to-one coordinate interval.</summary>
    public enum SamplerRepeatMode
    {
        /// <summary>Requires border sampling shader integration; creation currently rejects it.</summary>
        ClampToBorder = 3,
        /// <summary>Uses the closest edge texel outside the image.</summary>
        ClampToEdge = 2,
        /// <summary>Nonselectable upper bound; DataFormat.Max also means no view override.</summary>
        Max = 5,
        /// <summary>Repeats the image with alternating mirrored intervals.</summary>
        MirroredRepeat = 1,
        /// <summary>Requires mirror-clamp shader integration; creation currently rejects it.</summary>
        MirrorClampToEdge = 4,
        /// <summary>Repeats the image in every normalized coordinate interval.</summary>
        Repeat = 0,
    }
    /// <summary>Two-dimensional texture storage roles.</summary>
    public enum TextureType
    {
        /// <summary>One two-dimensional image.</summary>
        Texture = 1,
        /// <summary>An array of two-dimensional images.</summary>
        TextureArray = 5,
        /// <summary>Nonselectable upper bound.</summary>
        Max = 7
    }
}
