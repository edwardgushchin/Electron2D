namespace Electron2D;

public sealed class Camera : Node
{
    private float _orthoSize = 5f;
    private bool _current;

    public Camera(string name) : base(name) { }

    /// <summary>
    /// Unity-like orthographic size: половина высоты видимого мира в world-units.
    /// </summary>
    public float OrthoSize
    {
        get => _orthoSize;
        set => _orthoSize = value > 0f ? value : 0.0001f;
    }

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

            if (_current && Tree is not null)
                Tree.SetCurrentCamera(this);
        }
    }

    protected override void EnterTree()
    {
        Tree!.RegisterCamera(this);

        // Если камера помечена Current — делаем её текущей.
        // Если текущей камеры ещё нет — можно выбрать первую вошедшую.
        if (_current || Tree.CurrentCamera is null)
            Tree.SetCurrentCamera(this);
    }

    protected override void ExitTree()
    {
        // Тут Tree ещё доступен
        Tree!.UnregisterCamera(this);
    }
}