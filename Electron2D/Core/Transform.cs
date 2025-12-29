using System.Numerics;

namespace Electron2D;

public sealed class Transform
{
    private readonly Node _owner;
    private Transform? _parent;

    private Vector2 _localPosition;
    private float _localRotation;
    private Vector2 _localScale = Vector2.One;

    private bool _localDirty = true;
    private bool _worldDirty = true;

    private Matrix3x2 _localMatrix;
    private Matrix3x2 _worldMatrix;
    private Vector2 _worldPosition;
    private float _worldRotation;
    private Vector2 _worldScale = Vector2.One;

    internal Transform(Node owner)
    {
        _owner = owner;
    }

    internal Transform? Parent => _parent;

    public Vector2 LocalPosition
    {
        get => _localPosition;
        set
        {
            if (_localPosition == value) return;
            _localPosition = value;
            MarkLocalDirty();
        }
    }

    public float LocalRotation
    {
        get => _localRotation;
        set
        {
            if (_localRotation.Equals(value)) return;
            _localRotation = value;
            MarkLocalDirty();
        }
    }

    public Vector2 LocalScale
    {
        get => _localScale;
        set
        {
            if (_localScale == value) return;
            _localScale = value;
            MarkLocalDirty();
        }
    }

    public Vector2 WorldPosition
    {
        get
        {
            UpdateWorldIfNeeded();
            return _worldPosition;
        }
        set
        {
            if (_parent is null)
            {
                LocalPosition = value;
                return;
            }

            _parent.UpdateWorldIfNeeded();
            var inv = Matrix3x2.Invert(_parent._worldMatrix, out var invMatrix);
            if (!inv)
                throw new InvalidOperationException("Cannot invert parent transform matrix.");

            LocalPosition = Vector2.Transform(value, invMatrix);
        }
    }

    public float WorldRotation
    {
        get
        {
            UpdateWorldIfNeeded();
            return _worldRotation;
        }
        set
        {
            if (_parent is null)
            {
                LocalRotation = value;
                return;
            }

            _parent.UpdateWorldIfNeeded();
            LocalRotation = value - _parent._worldRotation;
        }
    }

    public Vector2 WorldScale
    {
        get
        {
            UpdateWorldIfNeeded();
            return _worldScale;
        }
        set
        {
            if (_parent is null)
            {
                LocalScale = value;
                return;
            }

            _parent.UpdateWorldIfNeeded();
            LocalScale = new Vector2(
                value.X / _parent._worldScale.X,
                value.Y / _parent._worldScale.Y);
        }
    }

    public Matrix3x2 LocalMatrix
    {
        get
        {
            UpdateLocalIfNeeded();
            return _localMatrix;
        }
    }

    public Matrix3x2 WorldMatrix
    {
        get
        {
            UpdateWorldIfNeeded();
            return _worldMatrix;
        }
    }

    public void Translate(Vector2 delta)
    {
        if (delta == Vector2.Zero) return;
        _localPosition += delta;
        MarkLocalDirty();
    }

    public void Rotate(float radians)
    {
        if (radians == 0f) return;
        _localRotation += radians;
        MarkLocalDirty();
    }

    public void Scale(Vector2 scale)
    {
        if (scale == Vector2.One) return;
        _localScale *= scale;
        MarkLocalDirty();
    }

    internal void SetParent(Transform? parent)
    {
        if (_parent == parent) return;
        _parent = parent;
        MarkWorldDirtyFromParent();
        _owner.MarkTransformDirtyFromParent();
    }

    internal void MarkWorldDirtyFromParent()
    {
        _worldDirty = true;
    }

    private void MarkLocalDirty()
    {
        _localDirty = true;
        _worldDirty = true;
        _owner.MarkTransformDirtyFromSelf();
    }

    private void UpdateLocalIfNeeded()
    {
        if (!_localDirty) return;
        var scale = Matrix3x2.CreateScale(_localScale);
        var rotation = Matrix3x2.CreateRotation(_localRotation);
        var translation = Matrix3x2.CreateTranslation(_localPosition);
        _localMatrix = scale * rotation * translation;
        _localDirty = false;
    }

    private void UpdateWorldIfNeeded()
    {
        if (!_worldDirty) return;
        UpdateLocalIfNeeded();

        if (_parent is null)
        {
            _worldMatrix = _localMatrix;
        }
        else
        {
            _parent.UpdateWorldIfNeeded();
            _worldMatrix = _localMatrix * _parent._worldMatrix;
        }

        _worldPosition = _worldMatrix.Translation;
        DecomposeAffine2D(_worldMatrix, out _worldScale, out _worldRotation);
        _worldDirty = false;
    }

    private static void DecomposeAffine2D(in Matrix3x2 matrix, out Vector2 scale, out float rotation)
    {
        var m11 = matrix.M11;
        var m12 = matrix.M12;
        var m21 = matrix.M21;
        var m22 = matrix.M22;

        var scaleX = MathF.Sqrt((m11 * m11) + (m12 * m12));
        var scaleY = MathF.Sqrt((m21 * m21) + (m22 * m22));

        if (scaleX == 0f)
        {
            rotation = 0f;
            scale = new Vector2(0f, scaleY);
            return;
        }

        rotation = MathF.Atan2(m12, m11);
        scale = new Vector2(scaleX, scaleY);
    }
}