namespace Electron2D;

/// <summary>
/// Режим фильтрации текстуры при масштабировании (сглаживание).
/// </summary>
/// <remarks>
/// Для SDL_Renderer фактически доступны Nearest и Linear.
/// </remarks>
public enum FilterMode : byte
{
    /// <summary>
    /// Наследовать режим: Sprite -> Texture -> EngineConfig.DefaultTextureFilter
    /// </summary>
    Inherit = 0,

    /// <summary>Point/nearest sampling (pixel-perfect).</summary>
    Nearest = 1,

    /// <summary>Linear sampling (сглаживание).</summary>
    Linear = 2,
    
    /// <summary>
    /// nearest pixel sampling with improved scaling for pixel art
    /// </summary>
    Pixelart = 3
}