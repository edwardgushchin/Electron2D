namespace Electron2D;

/// <summary>
/// Компонент, который может быть присоединён к <see cref="Node"/>.
/// </summary>
public interface IComponent
{
    /// <summary>
    /// Вызывается при присоединении компонента к узлу.
    /// </summary>
    /// <param name="owner">Узел-владелец компонента.</param>
    void OnAttach(Node owner);

    /// <summary>
    /// Вызывается при отсоединении компонента от узла.
    /// </summary>
    void OnDetach();
}