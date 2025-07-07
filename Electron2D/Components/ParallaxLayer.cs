using Electron2D.Graphics;
using Electron2D.Resources;

namespace Electron2D.Components;

/// <summary>
/// Вложенный класс, описывающий один слой параллакса
/// </summary>
internal class ParallaxLayer : Node
{
    private readonly List<Sprite> _sprites = [];
    private readonly float _speed;
    private readonly int _copies;
    private readonly float _spriteWidthUnits; // ширина спрайта в юнитах

    public ParallaxLayer(string name, Texture texture, float speed, int layerDepth, int copies, float overlapPixels = 1f) : base(name)
    {
        _speed = speed;
        _copies = copies;

        // Для точного позиционирования берём PixelsPerUnit из спрайта (берём один и используем его для всех)
        // Предположим, что PixelsPerUnit передаётся или по умолчанию 100
        // Создаём спрайты
        for (var i = -copies; i <= copies; i++)
        {
            var sprite = new Sprite($"layer_{i}", texture)
            {
                Layer = layerDepth,
                //ShowDebugRect = true
            };

            var baseX = i * (texture.Width - overlapPixels) / sprite.PixelsPerUnit; // уменьшаем ширину на overlapPixels
            sprite.Transform.LocalPosition = new Vector2(baseX, 0);

            _sprites.Add(sprite);
            AddChild(sprite);
        }

        // Ширина спрайта в юнитах с учётом перекрытия
        _spriteWidthUnits = (texture.Width - overlapPixels) / _sprites[0].PixelsPerUnit;
    }


    /// <summary>
    /// Обновить позицию слоя в зависимости от глобального сдвига.
    /// </summary>
    /// <param name="globalOffsetX"></param>
    public void UpdatePosition(float globalOffsetX)
    {
        // Для параллакса считаем смещение слоя, умножая глобальное смещение на скорость слоя
        var layerOffset = globalOffsetX * _speed;

        foreach (var sprite in _sprites)
        {
            // Базовая позиция по X - это исходная позиция (смещение от центра)
            var baseX = sprite.Transform.LocalPosition.X;

            // Новая позиция с учётом сдвига параллакса
            var newX = baseX + layerOffset;

            // Проверим, вышел ли спрайт за пределы, чтобы "перекинуть" его на другую сторону
            // Ширина всего слоя по X:
            var totalWidth = _spriteWidthUnits * _copies * 2;

            // Если спрайт уходит далеко вправо за правый край, переместим влево
            if (newX > _spriteWidthUnits * _copies + _spriteWidthUnits / 2f)
            {
                newX -= totalWidth + _spriteWidthUnits;
            }
            else if (newX < -_spriteWidthUnits * _copies - _spriteWidthUnits / 2f)
            {
                newX += totalWidth + _spriteWidthUnits;
            }

            sprite.Transform.LocalPosition = new Vector2(newX, sprite.Transform.LocalPosition.Y);
        }
    }

    public void ResetPosition()
    {
        // Считаем базовый сдвиг слоя (с учётом скорости параллакса)
        //var layerOffset = globalOffsetX * _speed;

        foreach (var sprite in _sprites)
        {
            // Начальная базовая позиция (без смещений)
            // Это как при создании: индекс * ширину
            var index = MathF.Round(sprite.Transform.LocalPosition.X / _spriteWidthUnits);
            var baseX = index * _spriteWidthUnits;

            // Просто добавляем глобальный сдвиг
            //var newX = baseX + layerOffset;

            sprite.Transform.LocalPosition = new Vector2(baseX, sprite.Transform.LocalPosition.Y);
        }
    }
}