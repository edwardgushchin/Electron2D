namespace Electron2D.Components;

public class Camera(string name) : Node(name)
{
    private float _zoom = 1f;
    
    private int _screenWidth;
    
    private int _screenHeight;
    
    public static Camera? ActiveCamera { get; set; }

    /// <summary>
    /// Нужно вызвать при изменении размера окна, чтобы обновить AspectRatio и WorldUnit.
    /// </summary>
    internal void UpdateScreenSize(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        AspectRatio = screenWidth / (float)screenHeight;

        // Обновляем WorldUnit
        RecalculateWorldUnit();
    }
    
    /// <summary>
    /// Пересчёт WorldUnit.
    /// </summary>
    private void RecalculateWorldUnit()
    {
        // Берём разницу в пикселях между (0,0) и (1,0)
        WorldUnit = _screenWidth / (Size * AspectRatio);
    }

    /// <summary>
    /// Преобразует мировую позицию в экранные координаты (пиксели).
    /// </summary>
    public (float X, float Y) ConvertWorldToScreen(float x, float y)
    {
        var relativePos = (
            X: (x - Transform.LocalPosition.X) * Zoom, 
            Y: (y - Transform.LocalPosition.Y) * Zoom
        );

        var pixelsPerUnitY = _screenHeight / Size;
        var pixelsPerUnitX = _screenWidth / (Size * AspectRatio);

        var screenX = (_screenWidth / 2f) + relativePos.X * pixelsPerUnitX;
        var screenY = (_screenHeight / 2f) - relativePos.Y * pixelsPerUnitY;

        return (screenX, screenY);
    }

    public Vector2 ConvertWorldToScreen(Vector2 worldPos)
    {
        var newPos = ConvertWorldToScreen(worldPos.X, worldPos.Y);
        return new Vector2(newPos.X, newPos.Y);
    }

    /// <summary>
    /// Преобразует экранные координаты (пиксели) обратно в мировые координаты (юниты).
    /// </summary>
    public Vector2 ConvertScreenToWorld(Vector2 screenPos)
    {
        var pixelsPerUnitY = _screenHeight / Size;
        var pixelsPerUnitX = _screenWidth / (Size * AspectRatio);

        var relativeX = (screenPos.X - (_screenWidth / 2f)) / pixelsPerUnitX;
        var relativeY = ((_screenHeight / 2f) - screenPos.Y) / pixelsPerUnitY;

        return new Vector2(relativeX, relativeY) / Zoom + Transform.LocalPosition;
    }
    
    /// <summary>
    /// Размер видимой области камеры по вертикали в юнитах (высота).
    /// </summary>
    public float Size { get; set; } = 5f;

    /// <summary>
    /// Текущее значение соотношения сторон (ширина / высота) окна.
    /// Передаётся в методы конвертации из окна.
    /// </summary>
    public float AspectRatio { get; private set; }

    /// <summary>
    /// Количество пикселей на один юнит мира по горизонтали (рассчитывается).
    /// </summary>
    public float WorldUnit { get; private set; }
    
    public float Zoom
    {
        get => _zoom;
        set => _zoom = value > 0 ? value : 1f;
    }
}