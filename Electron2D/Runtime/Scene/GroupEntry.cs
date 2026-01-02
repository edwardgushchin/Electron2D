namespace Electron2D;

/// <summary>
/// Описание группы объектов/узлов.
/// </summary>
internal struct GroupEntry(string name, bool persistent)
{
    #region Constants

    private const int UnregisteredTreeIndex = -1;

    #endregion

    #region Instance fields

    /// <summary>Имя группы.</summary>
    public string Name = name;

    /// <summary>Признак того, что группа должна сохраняться между сценами/перезагрузками.</summary>
    public bool Persistent = persistent;

    /// <summary>Индекс в SceneTree-индексе; <see cref="UnregisteredTreeIndex"/> если не зарегистрирован.</summary>
    public int TreeIndex = UnregisteredTreeIndex;

    #endregion
}