using Electron2D.Resources;

namespace Electron2D.Components;

public class BackgroundParallax(string name) : Node(name)
{
    private readonly List<ParallaxLayer> _layers = [];
    
    private float _globalOffsetX;

    /// <summary>
    /// Добавляет слой параллакса.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="texture"></param>
    /// <param name="speed">Скорость параллакса (0 - неподвижный, 1 - движение как камера).</param>
    /// <param name="layerDepth">Глубина слоя для рендера (сортировка).</param>
    /// <param name="copies">Количество копий спрайта слева и справа (не включая центральный).</param>
    /// <param name="overlapPixels"></param>
    /// <param name="offsetY"></param>
    public void AddLayer(string name, Texture texture, float speed, int layerDepth, int copies, float overlapPixels = 1f, float offsetY = 0)
    {
        var layer = new ParallaxLayer(name, texture, speed, layerDepth, copies, overlapPixels)
        {
            Transform =
            {
                LocalPosition = new Vector2(0, offsetY)
            }
        };
        _layers.Add(layer);
        AddChild(layer);
    }

    /// <summary>
    /// Обновить глобальный сдвиг параллакса, например, в зависимости от движения камеры.
    /// </summary>
    /// <param name="offsetX">Смещение по X (мировые юниты)</param>
    public void SetOffset(float offsetX)
    {
        _globalOffsetX = offsetX;

        foreach (var layer in _layers)
        {
            layer.UpdatePosition(_globalOffsetX);
        }
    }

    public void ResetOffset()
    {
        foreach (var layer in _layers)
        {
            layer.ResetPosition();
        }
    }
}
