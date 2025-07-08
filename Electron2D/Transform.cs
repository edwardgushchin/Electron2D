namespace Electron2D;

public class Transform(Node parent)
{
    private bool _transformDirty = true;

    private Vector2 _cachedGlobalPosition;
    
    private float _cachedGlobalRotation;
    
    private Vector2 _cachedGlobalScale;
    
    private Vector2 _localPosition = new(0, 0);
    
    private float _localRotation;
    
    private Vector2 _localScale = new(1, 1);

    /// <summary>
    /// Вызывается при изменении глобальной трансформации.
    /// Срабатывает только при пересчете после dirty.
    /// </summary>
    public event Action<Transform>? TransformChanged;
    
    /// <summary>
    /// Вызывается при изменении локальной трансформации (Position, Rotation, Scale).
    /// Срабатывает немедленно в сеттере.
    /// </summary>
    public event Action<Transform>? LocalTransformChanged;


    internal void MarkTransformDirty()
    {
        if (parent.IsStatic || _transformDirty) return;
        
        Interlocked.Exchange(ref _transformDirty, true);

        _transformDirty = true;

        foreach (var child in parent.GetChildren())
            child.Transform.MarkTransformDirty();
    }
    
    private void RecalculateTransform()
    {
        if (parent.Parent == null)
        {
            _cachedGlobalPosition = LocalPosition;
            _cachedGlobalRotation = LocalRotation;
            _cachedGlobalScale = LocalScale;
        }
        else
        {
            var parentPos = parent.Parent.Transform.GlobalPosition;
            var parentRot = parent.Parent.Transform.GlobalRotation;
            var parentScale = parent.Parent.Transform.GlobalScale;

            var scaledPos = new Vector2(
                LocalPosition.X * parentScale.X,
                LocalPosition.Y * parentScale.Y
            );

            var rotatedPos = Vector2.RotatePoint(scaledPos, parentRot);

            _cachedGlobalPosition = parentPos + rotatedPos;
            _cachedGlobalRotation = parentRot + LocalRotation;
            _cachedGlobalScale = new Vector2(
                parentScale.X * LocalScale.X,
                parentScale.Y * LocalScale.Y
            );
        }

        _transformDirty = false;
        TransformChanged?.Invoke(this);
    }
    
    public Vector2 LocalPosition
    {
        get => _localPosition;
        set
        {
            _localPosition = value;
            LocalTransformChanged?.Invoke(this);
            MarkTransformDirty();
        }
    }
    
    public float LocalRotation
    {
        get => _localRotation;
        set
        {
            _localRotation = value;
            LocalTransformChanged?.Invoke(this);
            MarkTransformDirty();
        }
    }
    
    public Vector2 LocalScale
    {
        get => _localScale;
        set
        {
            _localScale = value;
            LocalTransformChanged?.Invoke(this);
            MarkTransformDirty();
        }
    }
    
    // Глобальная позиция с учётом иерархии и поворота
    public Vector2 GlobalPosition
    {
        get
        {
            if (_transformDirty)
                RecalculateTransform();
            return _cachedGlobalPosition;
        }
    }
    
    // Глобальный угол (суммируем углы по иерархии)
    public float GlobalRotation
    {
        get
        {
            if (_transformDirty)
                RecalculateTransform();
            return _cachedGlobalRotation;
        }
    }
    
    // Глобальный масштаб (умножаем по компонентам)
    public Vector2 GlobalScale
    {
        get
        {
            if (_transformDirty)
                RecalculateTransform();
            return _cachedGlobalScale;
        }
    }
}