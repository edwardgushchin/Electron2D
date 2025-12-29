namespace Electron2D;

/// <summary>
/// <para>Состояние окна (оконная оболочка / window manager state).</para>
/// <para>
/// Используется для чтения и установки текущего состояния через <see cref="Window.State"/>.
/// Состояние влияет на видимость и поведение окна, но не гарантирует конкретную геометрию:
/// фактический результат зависит от платформы и текущего режима (<see cref="WindowMode"/>).
/// </para>
/// </summary>
public enum WindowState
{
    /// <summary>
    /// <para>Обычное состояние (не свернуто и не развернуто).</para>
    /// </summary>
    Normal = 0,
    
    /// <summary>
    /// <para>Окно свернуто.</para>
    /// <para>
    /// В этом состоянии окно может быть не видно на экране, но продолжает существовать и получать события
    /// (в зависимости от платформы).
    /// </para>
    /// </summary>
    Minimized = 1,
    
    /// <summary>
    /// <para>Окно развернуто (максимизировано) в пределах рабочего пространства.</para>
    /// <para>
    /// В полноэкранных режимах (<see cref="WindowMode.BorderlessFullscreen"/> / <see cref="WindowMode.ExclusiveFullscreen"/>)
    /// максимизация может быть неприменима и будет проигнорирована либо приведёт только к снятию <see cref="Minimized"/>.
    /// </para>
    /// </summary>
    Maximized = 2,
}