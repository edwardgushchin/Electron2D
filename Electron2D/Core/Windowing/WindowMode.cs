namespace Electron2D;

/// <summary>
/// <para>Режим отображения окна.</para>
/// <para>
/// Определяет, как окно взаимодействует с рабочим столом и видеорежимом дисплея:
/// в оконном режиме используется обычное окно, в полноэкранных режимах окно занимает весь экран.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// <see cref="WindowMode.BorderlessFullscreen"/> обычно не меняет видеорежим дисплея и
/// реализуется как окно без рамки, растянутое на весь экран.
/// </para>
/// <para>
/// <see cref="WindowMode.ExclusiveFullscreen"/> может переключать видеорежим (частота, разрешение и т.п.)
/// и чаще дает более «жесткий» fullscreen, но зависит от поддержки платформы/драйвера.
/// </para>
/// </remarks>
public enum WindowMode
{
    /// <summary>
    /// <para>Оконный режим.</para>
    /// <para>Окно располагается на рабочем столе, может иметь рамку и стандартные системные элементы управления.</para>
    /// </summary>
    Windowed = 0,
    
    /// <summary>
    /// <para>Полноэкранный режим без смены видеорежима (borderless fullscreen).</para>
    /// <para>Как правило, это окно без рамки, развернутое на весь экран текущего дисплея.</para>
    /// </summary>
    BorderlessFullscreen = 1,
    
    /// <summary>
    /// <para>Эксклюзивный полноэкранный режим (exclusive fullscreen).</para>
    /// <para>Обычно использует «настоящий» fullscreen и может переключать видеорежим дисплея.</para>
    /// </summary>
    ExclusiveFullscreen = 2,
}