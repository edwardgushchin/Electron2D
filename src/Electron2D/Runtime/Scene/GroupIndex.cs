using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>
/// Индекс групп: сопоставляет имя группы со списком узлов.
/// Хранение плотное (remove через swap-with-last) для O(1) удаления по индексу.
/// </summary>
internal sealed class GroupIndex
{
    #region Constants

    private const int DefaultGroupListCapacity = 8;
    private const int UnregisteredIndex = -1;

    #endregion

    #region Instance fields

    private readonly Dictionary<string, List<Node>> _nodesByGroup = new(StringComparer.Ordinal);

    #endregion

    #region Public API

    /// <summary>
    /// Возвращает read-only span узлов группы (без аллокаций).
    /// </summary>
    /// <remarks>
    /// Важно: span ссылается на внутренний список. Его нельзя хранить дольше, чем живёт коллекция,
    /// и нельзя использовать одновременно с модификациями индекса (Add/Remove), иначе возможны
    /// некорректные результаты.
    /// </remarks>
    public ReadOnlySpan<Node> GetNodes(string group)
    {
        if (string.IsNullOrWhiteSpace(group))
            return ReadOnlySpan<Node>.Empty;

        return _nodesByGroup.TryGetValue(group, out var list)
            ? CollectionsMarshal.AsSpan(list)
            : ReadOnlySpan<Node>.Empty;
    }

    /// <summary>
    /// Добавляет <paramref name="node"/> в группу и возвращает индекс, который должен быть сохранён в узле.
    /// </summary>
    public int Add(string group, Node node)
    {
        // Предполагается, что валидность group проверяется на более высоком уровне
        // (например, при регистрации в группах). Здесь оставляем hot-path без лишних проверок.
        if (!_nodesByGroup.TryGetValue(group, out var list))
        {
            list = new List<Node>(DefaultGroupListCapacity);
            _nodesByGroup[group] = list;
        }

        int index = list.Count;
        list.Add(node);
        return index;
    }

    /// <summary>
    /// Удаляет узел по индексу из группы, поддерживая плотный массив (swap-with-last).
    /// </summary>
    /// <param name="group">Имя группы.</param>
    /// <param name="index">Индекс узла в группе.</param>
    /// <param name="removedNode">Узел, который удаляется (нужно обновить его индекс).</param>
    public void Remove(string group, int index, Node removedNode)
    {
        if (!_nodesByGroup.TryGetValue(group, out var list))
            return;

        var lastIndex = list.Count - 1;
        if ((uint)index > (uint)lastIndex)
            return;

        if (index != lastIndex)
        {
            var swappedNode = list[lastIndex];
            list[index] = swappedNode;

            // Обновляем индекс у переставленного узла.
            swappedNode.InternalUpdateGroupIndex(group, index);
        }

        list.RemoveAt(lastIndex);

        if (list.Count == 0)
            _nodesByGroup.Remove(group);

        removedNode.InternalUpdateGroupIndex(group, UnregisteredIndex);
    }

    #endregion
}