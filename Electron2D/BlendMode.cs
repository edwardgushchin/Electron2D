using SDL3;

namespace Electron2D;

public enum BlendMode : uint
{
    /// <summary>
    /// no blending: dstRGBA = srcRGBA
    /// </summary>
    None = SDL.BlendMode.None,
        
    /// <summary>
    /// alpha blending: dstRGB = (srcRGB * srcA) + (dstRGB * (1-srcA)), dstA = srcA + (dstA * (1-srcA))
    /// </summary>
    Blend = SDL.BlendMode.Blend,
        
    /// <summary>
    /// pre-multiplied alpha blending: dstRGBA = srcRGBA + (dstRGBA * (1-srcA))
    /// </summary>
    BlendPremultiplied = SDL.BlendMode.BlendPremultiplied,
        
    /// <summary>
    /// additive blending: dstRGB = (srcRGB * srcA) + dstRGB, dstA = dstA
    /// </summary>
    Add =                   SDL.BlendMode.Add,
        
    /// <summary>
    /// pre-multiplied additive blending: dstRGB = srcRGB + dstRGB, dstA = dstA
    /// </summary>
    AddPremultiplied =     SDL.BlendMode.AddPremultiplied,
        
    /// <summary>
    /// color modulate: dstRGB = srcRGB * dstRGB, dstA = dstA
    /// </summary>
    Mod =                   SDL.BlendMode.Mod,
        
    /// <summary>
    /// color multiply: dstRGB = (srcRGB * dstRGB) + (dstRGB * (1-srcA)), dstA = dstA
    /// </summary>
    Mul =                   SDL.BlendMode.Mul,
    Invalid =               SDL.BlendMode.Invalid,
}