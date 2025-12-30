namespace Electron2D;

public class Camera(string name) : Node(name)
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
    
    internal void SetCurrentFromTree(bool value) => _current = value;
    
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

            var tree = SceneTree;
            if (tree is null) return;

            if (_current)
            {
                tree.SetCurrentCamera(this);
            }
            else
            {
                // Если текущую камеру явно выключили — сбрасываем current, дерево выберет другую позже.
                if (ReferenceEquals(tree.CurrentCamera, this))
                    tree.UnregisterCamera(this);
            }
        }
    }
    
    protected override void EnterTree()
    {
        SceneTree!.RegisterCamera(this);

        // Если камера помечена Current — делаем её текущей.
        if (_current)
            SceneTree.SetCurrentCamera(this);
    }

    protected override void ExitTree()
    {
        // Тут Tree ещё доступен
        SceneTree!.UnregisterCamera(this);
    }
}