using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    internal static bool CanvasEncodedColor(DataFormat format) => format is
        DataFormat.R8UNORM or
        DataFormat.R8G8UNORM or
        DataFormat.R8G8B8A8UNORM or
        DataFormat.B8G8R8A8UNORM or
        DataFormat.BC1RGBAUNORMBLOCK or
        DataFormat.BC2UNORMBLOCK or
        DataFormat.BC3UNORMBLOCK or
        DataFormat.BC4UNORMBLOCK or
        DataFormat.BC5UNORMBLOCK or
        DataFormat.BC7UNORMBLOCK or
        DataFormat.ASTC4X4UNORMBLOCK or
        DataFormat.ASTC5X4UNORMBLOCK or
        DataFormat.ASTC5X5UNORMBLOCK or
        DataFormat.ASTC6X5UNORMBLOCK or
        DataFormat.ASTC6X6UNORMBLOCK or
        DataFormat.ASTC8X5UNORMBLOCK or
        DataFormat.ASTC8X6UNORMBLOCK or
        DataFormat.ASTC8X8UNORMBLOCK or
        DataFormat.ASTC10X5UNORMBLOCK or
        DataFormat.ASTC10X6UNORMBLOCK or
        DataFormat.ASTC10X8UNORMBLOCK or
        DataFormat.ASTC10X10UNORMBLOCK or
        DataFormat.ASTC12X10UNORMBLOCK or
        DataFormat.ASTC12X12UNORMBLOCK;

    private static SDL.GPUTextureFormat NativeFormat(DataFormat format) => format switch
    {
        DataFormat.R8UNORM => SDL.GPUTextureFormat.R8Unorm,
        DataFormat.R8G8UNORM => SDL.GPUTextureFormat.R8G8Unorm,
        DataFormat.R8G8B8A8UNORM => SDL.GPUTextureFormat.R8G8B8A8Unorm,
        DataFormat.R16UNORM => SDL.GPUTextureFormat.R16Unorm,
        DataFormat.R16G16UNORM => SDL.GPUTextureFormat.R16G16Unorm,
        DataFormat.R16G16B16A16UNORM => SDL.GPUTextureFormat.R16G16B16A16Unorm,
        DataFormat.A2B10G10R10UNORMPACK32 => SDL.GPUTextureFormat.R10G10B10A2Unorm,
        DataFormat.B8G8R8A8UNORM => SDL.GPUTextureFormat.B8G8R8A8Unorm,
        DataFormat.BC1RGBAUNORMBLOCK => SDL.GPUTextureFormat.BC1RGBAUnorm,
        DataFormat.R8SNORM => SDL.GPUTextureFormat.R8Snorm,
        DataFormat.R8G8SNORM => SDL.GPUTextureFormat.R8G8Snorm,
        DataFormat.R8G8B8A8SNORM => SDL.GPUTextureFormat.R8G8B8A8Snorm,
        DataFormat.R16SNORM => SDL.GPUTextureFormat.R16Snorm,
        DataFormat.R16G16SNORM => SDL.GPUTextureFormat.R16G16Snorm,
        DataFormat.R16G16B16A16SNORM => SDL.GPUTextureFormat.R16G16B16A16Snorm,
        DataFormat.R16SFLOAT => SDL.GPUTextureFormat.R16Float,
        DataFormat.R16G16SFLOAT => SDL.GPUTextureFormat.R16G16Float,
        DataFormat.R16G16B16A16SFLOAT => SDL.GPUTextureFormat.R16G16B16A16Float,
        DataFormat.R32SFLOAT => SDL.GPUTextureFormat.R32Float,
        DataFormat.R32G32SFLOAT => SDL.GPUTextureFormat.R32G32Float,
        DataFormat.R32G32B32A32SFLOAT => SDL.GPUTextureFormat.R32G32B32A32Float,
        DataFormat.R8UINT => SDL.GPUTextureFormat.R8Uint,
        DataFormat.R8G8UINT => SDL.GPUTextureFormat.R8G8Uint,
        DataFormat.R8G8B8A8UINT => SDL.GPUTextureFormat.R8G8B8A8Uint,
        DataFormat.R16UINT => SDL.GPUTextureFormat.R16Uint,
        DataFormat.R16G16UINT => SDL.GPUTextureFormat.R16G16Uint,
        DataFormat.R16G16B16A16UINT => SDL.GPUTextureFormat.R16G16B16A16Uint,
        DataFormat.R32UINT => SDL.GPUTextureFormat.R32Uint,
        DataFormat.R32G32UINT => SDL.GPUTextureFormat.R32G32Uint,
        DataFormat.R32G32B32A32UINT => SDL.GPUTextureFormat.R32G32B32A32Uint,
        DataFormat.R8SINT => SDL.GPUTextureFormat.R8Int,
        DataFormat.R8G8SINT => SDL.GPUTextureFormat.R8G8Int,
        DataFormat.R8G8B8A8SINT => SDL.GPUTextureFormat.R8G8B8A8Int,
        DataFormat.R16SINT => SDL.GPUTextureFormat.R16Int,
        DataFormat.R16G16SINT => SDL.GPUTextureFormat.R16G16Int,
        DataFormat.R16G16B16A16SINT => SDL.GPUTextureFormat.R16G16B16A16Int,
        DataFormat.R32SINT => SDL.GPUTextureFormat.R32Int,
        DataFormat.R32G32SINT => SDL.GPUTextureFormat.R32G32Int,
        DataFormat.R32G32B32A32SINT => SDL.GPUTextureFormat.R32G32B32A32Int,
        DataFormat.R8G8B8A8SRGB => SDL.GPUTextureFormat.R8G8B8A8UnormSRGB,
        DataFormat.B8G8R8A8SRGB => SDL.GPUTextureFormat.B8G8R8A8UnormSRGB,
        DataFormat.BC1RGBASRGBBLOCK => SDL.GPUTextureFormat.BC1RGBAUnormSRGB,
        DataFormat.B5G6R5UNORMPACK16 => SDL.GPUTextureFormat.B5G6R5Unorm,
        DataFormat.B5G5R5A1UNORMPACK16 => SDL.GPUTextureFormat.B5G5R5A1Unorm,
        DataFormat.B4G4R4A4UNORMPACK16 => SDL.GPUTextureFormat.B4G4R4A4Unorm,
        DataFormat.B10G11R11UFLOATPACK32 => SDL.GPUTextureFormat.R11G11B10UFloat,
        DataFormat.BC2UNORMBLOCK => SDL.GPUTextureFormat.BC2RGBAUnorm,
        DataFormat.BC3UNORMBLOCK => SDL.GPUTextureFormat.BC3RGBAUnorm,
        DataFormat.BC4UNORMBLOCK => SDL.GPUTextureFormat.BC4RUnorm,
        DataFormat.BC5UNORMBLOCK => SDL.GPUTextureFormat.BC5RGUnorm,
        DataFormat.BC7UNORMBLOCK => SDL.GPUTextureFormat.BC7RGBAUnorm,
        DataFormat.BC6HSFLOATBLOCK => SDL.GPUTextureFormat.BC6HRGBFloat,
        DataFormat.BC6HUFLOATBLOCK => SDL.GPUTextureFormat.BC6HRGBUFloat,
        DataFormat.BC2SRGBBLOCK => SDL.GPUTextureFormat.BC2RGBAUnormSRGB,
        DataFormat.BC3SRGBBLOCK => SDL.GPUTextureFormat.BC3RGBAUnormSRGB,
        DataFormat.BC7SRGBBLOCK => SDL.GPUTextureFormat.BC7RGBAUnormSRGB,
        DataFormat.ASTC4X4UNORMBLOCK => SDL.GPUTextureFormat.ASTC4X4Unorm,
        DataFormat.ASTC4X4SRGBBLOCK => SDL.GPUTextureFormat.ASTC4X4UnormSRGB,
        DataFormat.ASTC5X4UNORMBLOCK => SDL.GPUTextureFormat.ASTC5X4Unorm,
        DataFormat.ASTC5X4SRGBBLOCK => SDL.GPUTextureFormat.ASTC5X4UnormSRGB,
        DataFormat.ASTC5X5UNORMBLOCK => SDL.GPUTextureFormat.ASTC5X5Unorm,
        DataFormat.ASTC5X5SRGBBLOCK => SDL.GPUTextureFormat.ASTC5X5UnormSRGB,
        DataFormat.ASTC6X5UNORMBLOCK => SDL.GPUTextureFormat.ASTC6X5Unorm,
        DataFormat.ASTC6X5SRGBBLOCK => SDL.GPUTextureFormat.ASTC6X5UnormSRGB,
        DataFormat.ASTC6X6UNORMBLOCK => SDL.GPUTextureFormat.ASTC6X6Unorm,
        DataFormat.ASTC6X6SRGBBLOCK => SDL.GPUTextureFormat.ASTC6X6UnormSRGB,
        DataFormat.ASTC8X5UNORMBLOCK => SDL.GPUTextureFormat.ASTC8X5Unorm,
        DataFormat.ASTC8X5SRGBBLOCK => SDL.GPUTextureFormat.ASTC8X5UnormSRGB,
        DataFormat.ASTC8X6UNORMBLOCK => SDL.GPUTextureFormat.ASTC8X6Unorm,
        DataFormat.ASTC8X6SRGBBLOCK => SDL.GPUTextureFormat.ASTC8X6UnormSRGB,
        DataFormat.ASTC8X8UNORMBLOCK => SDL.GPUTextureFormat.ASTC8X8Unorm,
        DataFormat.ASTC8X8SRGBBLOCK => SDL.GPUTextureFormat.ASTC8X8UnormSRGB,
        DataFormat.ASTC10X5UNORMBLOCK => SDL.GPUTextureFormat.ASTC10X5Unorm,
        DataFormat.ASTC10X5SRGBBLOCK => SDL.GPUTextureFormat.ASTC10X5UnormSRGB,
        DataFormat.ASTC10X6UNORMBLOCK => SDL.GPUTextureFormat.ASTC10X6Unorm,
        DataFormat.ASTC10X6SRGBBLOCK => SDL.GPUTextureFormat.ASTC10X6UnormSRGB,
        DataFormat.ASTC10X8UNORMBLOCK => SDL.GPUTextureFormat.ASTC10X8Unorm,
        DataFormat.ASTC10X8SRGBBLOCK => SDL.GPUTextureFormat.ASTC10X8UnormSRGB,
        DataFormat.ASTC10X10UNORMBLOCK => SDL.GPUTextureFormat.ASTC10X10Unorm,
        DataFormat.ASTC10X10SRGBBLOCK => SDL.GPUTextureFormat.ASTC10X10UnormSRGB,
        DataFormat.ASTC12X10UNORMBLOCK => SDL.GPUTextureFormat.ASTC12X10Unorm,
        DataFormat.ASTC12X10SRGBBLOCK => SDL.GPUTextureFormat.ASTC12X10UnormSRGB,
        DataFormat.ASTC12X12UNORMBLOCK => SDL.GPUTextureFormat.ASTC12X12Unorm,
        DataFormat.ASTC12X12SRGBBLOCK => SDL.GPUTextureFormat.ASTC12X12UnormSRGB,
        DataFormat.ASTC4X4SFLOATBLOCK => SDL.GPUTextureFormat.ASTC4X4Float,
        DataFormat.ASTC5X4SFLOATBLOCK => SDL.GPUTextureFormat.ASTC5X4Float,
        DataFormat.ASTC5X5SFLOATBLOCK => SDL.GPUTextureFormat.ASTC5X5Float,
        DataFormat.ASTC6X5SFLOATBLOCK => SDL.GPUTextureFormat.ASTC6X5Float,
        DataFormat.ASTC6X6SFLOATBLOCK => SDL.GPUTextureFormat.ASTC6X6Float,
        DataFormat.ASTC8X5SFLOATBLOCK => SDL.GPUTextureFormat.ASTC8X5Float,
        DataFormat.ASTC8X6SFLOATBLOCK => SDL.GPUTextureFormat.ASTC8X6Float,
        DataFormat.ASTC8X8SFLOATBLOCK => SDL.GPUTextureFormat.ASTC8X8Float,
        DataFormat.ASTC10X5SFLOATBLOCK => SDL.GPUTextureFormat.ASTC10X5Float,
        DataFormat.ASTC10X6SFLOATBLOCK => SDL.GPUTextureFormat.ASTC10X6Float,
        DataFormat.ASTC10X8SFLOATBLOCK => SDL.GPUTextureFormat.ASTC10X8Float,
        DataFormat.ASTC10X10SFLOATBLOCK => SDL.GPUTextureFormat.ASTC10X10Float,
        DataFormat.ASTC12X10SFLOATBLOCK => SDL.GPUTextureFormat.ASTC12X10Float,
        DataFormat.ASTC12X12SFLOATBLOCK => SDL.GPUTextureFormat.ASTC12X12Float,
        DataFormat.D16UNORM => SDL.GPUTextureFormat.D16Unorm,
        DataFormat.X8D24UNORMPACK32 => SDL.GPUTextureFormat.D24Unorm,
        DataFormat.D32SFLOAT => SDL.GPUTextureFormat.D32Float,
        DataFormat.D24UNORMS8UINT => SDL.GPUTextureFormat.D24UnormS8Uint,
        DataFormat.D32SFLOATS8UINT => SDL.GPUTextureFormat.D32FloatS8Uint,
        _ => SDL.GPUTextureFormat.Invalid
    };
    private static (uint Width, uint Height) BlockDimensions(SDL.GPUTextureFormat format) => format switch
    {
        SDL.GPUTextureFormat.ASTC4X4Unorm => (4, 4),
        SDL.GPUTextureFormat.ASTC4X4UnormSRGB => (4, 4),
        SDL.GPUTextureFormat.ASTC4X4Float => (4, 4),
        SDL.GPUTextureFormat.ASTC5X4Unorm => (5, 4),
        SDL.GPUTextureFormat.ASTC5X4UnormSRGB => (5, 4),
        SDL.GPUTextureFormat.ASTC5X4Float => (5, 4),
        SDL.GPUTextureFormat.ASTC5X5Unorm => (5, 5),
        SDL.GPUTextureFormat.ASTC5X5UnormSRGB => (5, 5),
        SDL.GPUTextureFormat.ASTC5X5Float => (5, 5),
        SDL.GPUTextureFormat.ASTC6X5Unorm => (6, 5),
        SDL.GPUTextureFormat.ASTC6X5UnormSRGB => (6, 5),
        SDL.GPUTextureFormat.ASTC6X5Float => (6, 5),
        SDL.GPUTextureFormat.ASTC6X6Unorm => (6, 6),
        SDL.GPUTextureFormat.ASTC6X6UnormSRGB => (6, 6),
        SDL.GPUTextureFormat.ASTC6X6Float => (6, 6),
        SDL.GPUTextureFormat.ASTC8X5Unorm => (8, 5),
        SDL.GPUTextureFormat.ASTC8X5UnormSRGB => (8, 5),
        SDL.GPUTextureFormat.ASTC8X5Float => (8, 5),
        SDL.GPUTextureFormat.ASTC8X6Unorm => (8, 6),
        SDL.GPUTextureFormat.ASTC8X6UnormSRGB => (8, 6),
        SDL.GPUTextureFormat.ASTC8X6Float => (8, 6),
        SDL.GPUTextureFormat.ASTC8X8Unorm => (8, 8),
        SDL.GPUTextureFormat.ASTC8X8UnormSRGB => (8, 8),
        SDL.GPUTextureFormat.ASTC8X8Float => (8, 8),
        SDL.GPUTextureFormat.ASTC10X5Unorm => (10, 5),
        SDL.GPUTextureFormat.ASTC10X5UnormSRGB => (10, 5),
        SDL.GPUTextureFormat.ASTC10X5Float => (10, 5),
        SDL.GPUTextureFormat.ASTC10X6Unorm => (10, 6),
        SDL.GPUTextureFormat.ASTC10X6UnormSRGB => (10, 6),
        SDL.GPUTextureFormat.ASTC10X6Float => (10, 6),
        SDL.GPUTextureFormat.ASTC10X8Unorm => (10, 8),
        SDL.GPUTextureFormat.ASTC10X8UnormSRGB => (10, 8),
        SDL.GPUTextureFormat.ASTC10X8Float => (10, 8),
        SDL.GPUTextureFormat.ASTC10X10Unorm => (10, 10),
        SDL.GPUTextureFormat.ASTC10X10UnormSRGB => (10, 10),
        SDL.GPUTextureFormat.ASTC10X10Float => (10, 10),
        SDL.GPUTextureFormat.ASTC12X10Unorm => (12, 10),
        SDL.GPUTextureFormat.ASTC12X10UnormSRGB => (12, 10),
        SDL.GPUTextureFormat.ASTC12X10Float => (12, 10),
        SDL.GPUTextureFormat.ASTC12X12Unorm => (12, 12),
        SDL.GPUTextureFormat.ASTC12X12UnormSRGB => (12, 12),
        SDL.GPUTextureFormat.ASTC12X12Float => (12, 12),
        >= SDL.GPUTextureFormat.BC1RGBAUnorm and <= SDL.GPUTextureFormat.BC6HRGBUFloat => (4, 4),
        >= SDL.GPUTextureFormat.BC1RGBAUnormSRGB and <= SDL.GPUTextureFormat.BC7RGBAUnormSRGB => (4, 4),
        _ => (1, 1)
    };
    private static uint TextureBytes(SDL.GPUTextureFormat format, uint width, uint height)
    {
        var block = BlockDimensions(format);
        var blocks = ((ulong)width + block.Width - 1) / block.Width * (((ulong)height + block.Height - 1) / block.Height);
        var bytes = SDL.GPUTextureFormatTexelBlockSize(format);
        if (bytes == 0 || blocks > (ulong)int.MaxValue / bytes) throw new ArgumentOutOfRangeException(nameof(width), "A mip exceeds managed transfer capacity.");
        return (uint)(blocks * bytes);
    }
}
