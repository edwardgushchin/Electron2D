namespace Electron2D;

public sealed class Camera(string name) : Node(name)
{
    private bool _current;

    /// <summary>
    /// Unity-like orthographic size: половина высоты видимого мира в world-units.
    /// </summary>
    public float OrthoSize
    {
        get;
        set => field = value > 0f ? value : 0.0001f;
    } = 5f;

    /// <summary>
    /// Если true — камера становится текущей (как Godot Camera2D.current).
    /// </summary>
    public bool Current
    {
        get => _current;
        set
        {
            if (_current == value) return;
            _current = value;

            if (_current && SceneTree is not null)
                SceneTree.SetCurrentCamera(this);
        }
    }

    protected override void EnterTree()
    {
        SceneTree!.RegisterCamera(this);

        // Если камера помечена Current — делаем её текущей.
        // Если текущей камеры ещё нет — можно выбрать первую вошедшую.
        if (_current || SceneTree.CurrentCamera is null)
            SceneTree.SetCurrentCamera(this);
    }

    protected override void ExitTree()
    {
        // Тут Tree ещё доступен
        SceneTree!.UnregisterCamera(this);
    }
}