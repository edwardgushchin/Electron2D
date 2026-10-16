namespace Electron2D;

public sealed partial class Image
{
    private static Format KTXFormat(uint version, uint format) => version switch
    {
        1 => KTXGLFormat(format),
        2 => KTXVKFormat(format),
        _ => throw new InvalidDataException("KTX container version is invalid.")
    };

    private static Format KTXGLFormat(uint format) => format switch
    {
        0x1909 => Format.L8, // GL_LUMINANCE
        0x190A => Format.La8, // GL_LUMINANCE_ALPHA
        0x8051 => Format.Rgb8, // GL_RGB8
        0x8054 => Format.Rgb16, // GL_RGB16
        0x8056 => Format.Rgba4444, // GL_RGBA4
        0x8058 => Format.Rgba8, // GL_RGBA8
        0x805B => Format.Rgba16, // GL_RGBA16
        0x8229 => Format.R8, // GL_R8
        0x822A => Format.R16, // GL_R16
        0x822B => Format.Rg8, // GL_RG8
        0x822C => Format.Rg16, // GL_RG16
        0x822D => Format.Rh, // GL_R16F
        0x822E => Format.Rf, // GL_R32F
        0x822F => Format.Rgh, // GL_RG16F
        0x8230 => Format.Rgf, // GL_RG32F
        0x8232 => Format.R8, // GL_R8UI
        0x8234 => Format.R16I, // GL_R16UI
        0x823A => Format.Rg16I, // GL_RG16UI
        0x83F0 => Format.Dxt1, // GL_COMPRESSED_RGB_S3TC_DXT1_EXT
        0x83F1 => Format.Dxt1, // GL_COMPRESSED_RGBA_S3TC_DXT1_EXT
        0x83F2 => Format.Dxt3, // GL_COMPRESSED_RGBA_S3TC_DXT3_EXT
        0x83F3 => Format.Dxt5, // GL_COMPRESSED_RGBA_S3TC_DXT5_EXT
        0x8814 => Format.Rgbaf, // GL_RGBA32F
        0x8815 => Format.Rgbf, // GL_RGB32F
        0x881A => Format.Rgbah, // GL_RGBA16F
        0x881B => Format.Rgbh, // GL_RGB16F
        0x8C3D => Format.Rgbe9995, // GL_RGB9_E5
        0x8C41 => Format.Rgb8, // GL_SRGB8
        0x8C43 => Format.Rgba8, // GL_SRGB8_ALPHA8
        0x8D62 => Format.Rgb565, // GL_RGB565
        0x8D64 => Format.Etc, // GL_ETC1_RGB8_OES
        0x8D76 => Format.Rgba16I, // GL_RGBA16UI
        0x8D77 => Format.Rgb16I, // GL_RGB16UI
        0x8DBB => Format.RgtcR, // GL_COMPRESSED_RED_RGTC1
        0x8DBD => Format.RgtcRg, // GL_COMPRESSED_RG_RGTC2
        0x8E8C => Format.BptcRgba, // GL_COMPRESSED_RGBA_BPTC_UNORM
        0x8E8E => Format.BptcRgbf, // GL_COMPRESSED_RGB_BPTC_SIGNED_FLOAT
        0x8E8F => Format.BptcRgbfu, // GL_COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT
        0x9270 => Format.Etc2R11, // GL_COMPRESSED_R11_EAC
        0x9272 => Format.Etc2Rg11, // GL_COMPRESSED_RG11_EAC
        0x9274 => Format.Etc2Rgb8, // GL_COMPRESSED_RGB8_ETC2
        0x9278 => Format.Etc2Rgba8, // GL_COMPRESSED_RGBA8_ETC2_EAC
        0x93B0 => Format.Astc4X4Hdr, // GL_COMPRESSED_RGBA_ASTC_4x4_KHR
        0x93B7 => Format.Astc8X8Hdr, // GL_COMPRESSED_RGBA_ASTC_8x8_KHR
        0x93D0 => Format.Astc4X4, // GL_COMPRESSED_SRGB8_ALPHA8_ASTC_4x4_KHR
        0x93D7 => Format.Astc8X8, // GL_COMPRESSED_SRGB8_ALPHA8_ASTC_8x8_KHR
        _ => throw new NotSupportedException($"Unsupported GL storage format {format} in KTX.")
    };

    private static Format KTXVKFormat(uint format) => format switch
    {
        0x2 => Format.Rgba4444, // VK_FORMAT_R4G4B4A4_UNORM_PACK16
        0x4 => Format.Rgb565, // VK_FORMAT_R5G6B5_UNORM_PACK16
        0x9 => Format.L8, // VK_FORMAT_R8_UNORM
        0xD => Format.R8, // VK_FORMAT_R8_UINT
        0x10 => Format.La8, // VK_FORMAT_R8G8_UNORM
        0x14 => Format.Rg8, // VK_FORMAT_R8G8_UINT
        0x17 => Format.Rgb8, // VK_FORMAT_R8G8B8_UNORM
        0x1B => Format.Rgb8, // VK_FORMAT_R8G8B8_UINT
        0x1D => Format.Rgb8, // VK_FORMAT_R8G8B8_SRGB
        0x25 => Format.Rgba8, // VK_FORMAT_R8G8B8A8_UNORM
        0x29 => Format.Rgba8, // VK_FORMAT_R8G8B8A8_UINT
        0x2B => Format.Rgba8, // VK_FORMAT_R8G8B8A8_SRGB
        0x46 => Format.R16, // VK_FORMAT_R16_UNORM
        0x4A => Format.R16I, // VK_FORMAT_R16_UINT
        0x4C => Format.Rh, // VK_FORMAT_R16_SFLOAT
        0x4D => Format.Rg16, // VK_FORMAT_R16G16_UNORM
        0x51 => Format.Rg16I, // VK_FORMAT_R16G16_UINT
        0x53 => Format.Rgh, // VK_FORMAT_R16G16_SFLOAT
        0x54 => Format.Rgb16, // VK_FORMAT_R16G16B16_UNORM
        0x58 => Format.Rgb16I, // VK_FORMAT_R16G16B16_UINT
        0x5A => Format.Rgbh, // VK_FORMAT_R16G16B16_SFLOAT
        0x5B => Format.Rgba16, // VK_FORMAT_R16G16B16A16_UNORM
        0x5F => Format.Rgba16I, // VK_FORMAT_R16G16B16A16_UINT
        0x61 => Format.Rgbah, // VK_FORMAT_R16G16B16A16_SFLOAT
        0x64 => Format.Rf, // VK_FORMAT_R32_SFLOAT
        0x67 => Format.Rgf, // VK_FORMAT_R32G32_SFLOAT
        0x6A => Format.Rgbf, // VK_FORMAT_R32G32B32_SFLOAT
        0x6D => Format.Rgbaf, // VK_FORMAT_R32G32B32A32_SFLOAT
        0x7B => Format.Rgbe9995, // VK_FORMAT_E5B9G9R9_UFLOAT_PACK32
        0x83 => Format.Dxt1, // VK_FORMAT_BC1_RGB_UNORM_BLOCK
        0x85 => Format.Dxt1, // VK_FORMAT_BC1_RGBA_UNORM_BLOCK
        0x87 => Format.Dxt3, // VK_FORMAT_BC2_UNORM_BLOCK
        0x89 => Format.Dxt5, // VK_FORMAT_BC3_UNORM_BLOCK
        0x8B => Format.RgtcR, // VK_FORMAT_BC4_UNORM_BLOCK
        0x8D => Format.RgtcRg, // VK_FORMAT_BC5_UNORM_BLOCK
        0x8F => Format.BptcRgbfu, // VK_FORMAT_BC6H_UFLOAT_BLOCK
        0x90 => Format.BptcRgbf, // VK_FORMAT_BC6H_SFLOAT_BLOCK
        0x91 => Format.BptcRgba, // VK_FORMAT_BC7_UNORM_BLOCK
        0x93 => Format.Etc2Rgb8, // VK_FORMAT_ETC2_R8G8B8_UNORM_BLOCK
        0x97 => Format.Etc2Rgba8, // VK_FORMAT_ETC2_R8G8B8A8_UNORM_BLOCK
        0x99 => Format.Etc2R11, // VK_FORMAT_EAC_R11_UNORM_BLOCK
        0x9B => Format.Etc2Rg11, // VK_FORMAT_EAC_R11G11_UNORM_BLOCK
        0x9D => Format.Astc4X4Hdr, // VK_FORMAT_ASTC_4x4_UNORM_BLOCK
        0x9E => Format.Astc4X4, // VK_FORMAT_ASTC_4x4_SRGB_BLOCK
        0xAB => Format.Astc8X8Hdr, // VK_FORMAT_ASTC_8x8_UNORM_BLOCK
        0xAC => Format.Astc8X8, // VK_FORMAT_ASTC_8x8_SRGB_BLOCK
        _ => throw new NotSupportedException($"Unsupported VK storage format {format} in KTX.")
    };
}
