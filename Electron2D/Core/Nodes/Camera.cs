namespace Electron2D;

/// <summary>
/// Камера сцены (2D, ортографическая).
/// </summary>
public class Camera(string name) : Node(name)
{
    private const float MinOrthoSize = 0.0001f;

    private bool _isCurrent;
    
    /// <summary>Сколько экранных пикселей приходится на 1 world-unit по вертикали.</summary>
    public int PixelsPerUnit { get; set; } = 16;

    /// <summary>
    /// Unity-like orthographic size: половина высоты видимого мира в world-units.
    /// </summary>
    /// <remarks>
    /// Значение принудительно ограничивается снизу, чтобы избежать вырожденной математики в проекции.
    /// </remarks>
    public float OrthoSize
    {
        get;
        set => field = value > 0f ? value : MinOrthoSize;
    } = 5f;

    /// <summary>
    /// Если <see langword="true"/> — камера становится текущей (как Godot <c>Camera2D.current</c>).
    /// </summary>
    public bool Current
    {
        get => _isCurrent;
        set
        {
            if (_isCurrent == value)
                return;

            _isCurrent = value;

            var tree = SceneTree;
            if (tree is null)
                return;

            if (_isCurrent)
            {
                tree.SetCurrentCamera(this);
                return;
            }

            // Если текущую камеру явно выключили — сбрасываем current, дерево выберет другую позже.
            if (ReferenceEquals(tree.CurrentCamera, this))
                tree.UnregisterCamera(this);
        }
    }

    #region Internal API
    internal void SetCurrentFromTree(bool value) => _isCurrent = value;
    #endregion

    #region Node lifecycle
    protected override void EnterTree()
    {
        SceneTree!.RegisterCamera(this);

        // Если камера помечена Current — делаем её текущей.
        if (_isCurrent)
            SceneTree.SetCurrentCamera(this);
    }

    protected override void ExitTree()
    {
        // Здесь Tree ещё доступен.
        SceneTree!.UnregisterCamera(this);
    }
    #endregion
}