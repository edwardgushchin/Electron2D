using System.Numerics;

namespace Electron2D;

/// <summary>
/// Спрайт — участок текстуры (или атласа) с параметрами отображения.
/// </summary>
/// <remarks>
/// Это ассет-описание. Не владеет GPU-ресурсами; для получения текстуры используйте <see cref="TextureId"/> через <c>ResourceSystem</c>.
/// </remarks>
public sealed class Sprite
{
    private const float DefaultPixelsPerUnit = 100f;

    /// <summary>
    /// Идентификатор текстуры в ресурсах (если спрайт создан по id).
    /// </summary>
    public string? TextureId { get; set; }

    /// <summary>
    /// Текстура (если спрайт создан напрямую из <see cref="Texture"/>).
    /// </summary>
    public Texture Texture { get; set; }

    /// <summary>
    /// Pivot в нормализованных координатах относительно <see cref="TextureRect"/>: (0..1).
    /// Например, (0.5,0.5) — центр.
    /// </summary>
    public Vector2 Pivot { get; set; }

    /// <summary>
    /// Количество пикселей на один мировой юнит. Должно быть конечным и строго больше 0.
    /// </summary>
    public float PixelsPerUnit { get; set; }

    public Vector4 Border { get; set; }

    /// <summary>
    /// Rect спрайта (пиксели). Если не задан (Width/Height &lt;= 0), рендерер может трактовать как “вся текстура”.
    /// </summary>
    public Rect Rect { get; set; }

    /// <summary>
    /// Область в текстуре (пиксели), из которой берётся изображение.
    /// Если не задана (Width/Height &lt;= 0), рендерер использует “всю текстуру”.
    /// </summary>
    public Rect TextureRect { get; set; }

    public Vector2 TextureRectOffset { get; set; }

    public string? AssociatedAlphaTextureId { get; set; }

    public PackingMode PackingMode { get; set; }

    public FlipMode FlipMode { get; set; }

    public float AtlasTextureScale { get; set; }

    public SpriteMesh? Mesh { get; set; }

    /// <summary>
    /// Создаёт спрайт по идентификатору текстуры. По умолчанию — вся текстура.
    /// </summary>
    public Sprite(
        string textureId,
        float pixelsPerUnit = DefaultPixelsPerUnit,
        Vector2? pivot = null,
        Vector4 border = default,
        Rect? rect = null,
        Rect? textureRect = null,
        Vector2 textureRectOffset = default,
        string? associatedAlphaTextureId = null,
        PackingMode packingMode = PackingMode.None,
        FlipMode packingRotation = FlipMode.None,
        float atlasTextureScale = 1f,
        SpriteMesh? mesh = null)
    {
        ArgumentNullException.ThrowIfNull(textureId);

        Initialize(
            textureId: textureId,
            texture: default,
            pixelsPerUnit: pixelsPerUnit,
            pivot: pivot,
            border: border,
            rect: rect,
            textureRect: textureRect,
            textureRectOffset: textureRectOffset,
            associatedAlphaTextureId: associatedAlphaTextureId,
            packingMode: packingMode,
            flipMode: packingRotation,
            atlasTextureScale: atlasTextureScale,
            mesh: mesh);
    }

    /// <summary>
    /// Создаёт спрайт напрямую из текстуры. По умолчанию — вся текстура.
    /// </summary>
    public Sprite(
        Texture texture,
        float pixelsPerUnit = DefaultPixelsPerUnit,
        Vector2? pivot = null,
        Vector4 border = default,
        Rect? rect = null,
        Rect? textureRect = null,
        Vector2 textureRectOffset = default,
        string? associatedAlphaTextureId = null,
        PackingMode packingMode = PackingMode.None,
        FlipMode packingRotation = FlipMode.None,
        float atlasTextureScale = 1f,
        SpriteMesh? mesh = null)
    {
        if (!texture.IsValid)
            throw new ArgumentOutOfRangeException(nameof(texture), "Texture must be valid.");

        Initialize(
            textureId: null,
            texture: texture,
            pixelsPerUnit: pixelsPerUnit,
            pivot: pivot,
            border: border,
            rect: rect,
            textureRect: textureRect,
            textureRectOffset: textureRectOffset,
            associatedAlphaTextureId: associatedAlphaTextureId,
            packingMode: packingMode,
            flipMode: packingRotation,
            atlasTextureScale: atlasTextureScale,
            mesh: mesh);
    }

    private void Initialize(
        string? textureId,
        Texture texture,
        float pixelsPerUnit,
        Vector2? pivot,
        Vector4 border,
        Rect? rect,
        Rect? textureRect,
        Vector2 textureRectOffset,
        string? associatedAlphaTextureId,
        PackingMode packingMode,
        FlipMode flipMode,
        float atlasTextureScale,
        SpriteMesh? mesh)
    {
        // PROD: предсказуемый контракт. Ошибки лучше ловить при создании/конфигурировании ассета.
        if (!(pixelsPerUnit > 0f) || float.IsNaN(pixelsPerUnit) || float.IsInfinity(pixelsPerUnit))
            throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit), pixelsPerUnit, "PixelsPerUnit must be finite and > 0.");

        if (float.IsNaN(atlasTextureScale) || float.IsInfinity(atlasTextureScale))
            throw new ArgumentOutOfRangeException(nameof(atlasTextureScale), atlasTextureScale, "AtlasTextureScale must be finite.");

        var pv = pivot ?? new Vector2(0.5f, 0.5f);
        if (float.IsNaN(pv.X) || float.IsNaN(pv.Y) || float.IsInfinity(pv.X) || float.IsInfinity(pv.Y))
            throw new ArgumentOutOfRangeException(nameof(pivot), pivot, "Pivot must be finite.");

        TextureId = textureId;
        Texture = texture;

        // Если rect/textureRect не заданы, оставляем их "пустыми" (W/H <= 0) — SpriteRenderer заполнит по размерам текстуры.
        Rect = rect ?? default;
        TextureRect = textureRect ?? Rect;
        TextureRectOffset = textureRectOffset;

        Pivot = pv;
        Border = border;
        PixelsPerUnit = pixelsPerUnit;

        AssociatedAlphaTextureId = associatedAlphaTextureId;
        PackingMode = packingMode;
        FlipMode = flipMode;
        AtlasTextureScale = atlasTextureScale;

        Mesh = mesh;
    }
}