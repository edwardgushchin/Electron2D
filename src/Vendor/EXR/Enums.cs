#nullable disable warnings
namespace Electron2DCodecs.EXR
{
    /// <summary>
    /// Result codes returned by the TinyEXR v3 API.
    /// </summary>
    internal enum ExrResult
    {
        Success = 0,
        WouldBlock = 1,

        InvalidArgument = -1,
        InvalidFile = -2,
        Unsupported = -3,
        OutOfMemory = -4,
        IO = -5,
        Corrupt = -6,
    }

    internal enum PixelType
    {
        UInt = 0,
        Half = 1,
        Float = 2,
    }

    internal enum Compression
    {
        None = 0,
        RLE = 1,
        ZIPS = 2,
        ZIP = 3,
        PIZ = 4,
        PXR24 = 5,
        B44 = 6,
        B44A = 7,
        DWAA = 8,
        DWAB = 9,
        HTJ2K256 = 10,
        HTJ2K32 = 11,
    }

    internal enum LineOrder
    {
        IncreasingY = 0,
        DecreasingY = 1,
        RandomY = 2,
    }

    internal enum PartType
    {
        Scanline = 0,
        Tiled = 1,
        DeepScanline = 2,
        DeepTiled = 3,
    }

    internal enum TileLevelMode
    {
        OneLevel = 0,
        MipmapLevels = 1,
        RipmapLevels = 2,
    }

    internal enum TileRoundingMode
    {
        RoundDown = 0,
        RoundUp = 1,
    }

    internal enum ResizeFilter
    {
        Box = 0,
        Triangle = 1,
        CatmullRom = 2,
        Mitchell = 3,
    }

    internal enum EdgeMode
    {
        Clamp = 0,
        Reflect = 1,
        Wrap = 2,
    }

    internal enum ToneMapOperator
    {
        Reinhard = 0,
        ReinhardExtended = 1,
        Aces = 2,
        Hable = 3,
    }

    internal enum ColorSpace
    {
        Srgb = 0,
        Rec2020 = 1,
        AcesAp0 = 2,
        AcesAp1 = 3,
        Xyz = 4,
    }

    internal enum TransferFunction
    {
        Linear = 0,
        Srgb = 1,
        Gamma22 = 2,
        Gamma24 = 3,
        Rec709 = 4,
        Pq = 5,
        Hlg = 6,
    }

    internal enum LutInterpolation
    {
        Trilinear = 0,
        Tetrahedral = 1,
    }

    internal enum SpectralType
    {
        None = 0,
        Reflective = 1,
        Emissive = 2,
        Polarised = 3,
    }

    internal enum PixelConversionMode
    {
        Raw = 0,
        Normalized = 1,
    }
}
