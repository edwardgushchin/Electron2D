using System.Numerics;
using System.Runtime.CompilerServices;

namespace Electron2D;

public sealed class Transform
{
    private readonly Node _owner;
    private Transform? _parent;

    private Vector2 _localPosition;
    private float   _localRotation;
    private Vector2 _localScale = Vector2.One;

    private bool _localMatrixDirty = true;

    // World TRS
    private bool   _worldDirty = true;
    private Vector2 _worldPosition;
    private float   _worldRotation;
    private Vector2 _worldScale = Vector2.One;

    // World matrix computed only when requested
    private bool _worldMatrixDirty = true;
    private Matrix3x2 _localMatrix;
    private Matrix3x2 _worldMatrix;

    private int _localVersion;
    private int _worldVersion;
    private int _parentWorldVersionAtCompute;

    internal Transform(Node owner) => _owner = owner;

    internal Transform? Parent => _parent;

    public int LocalVersion => _localVersion;

    /// <summary>
    /// Версия world TRS. Инкрементится, когда world (pos/rot/scale) реально пересчитан.
    /// Это и есть "dirt" для рендера/физики: потребители кешируют lastWorldVer.
    /// </summary>
    public int WorldVersion
    {
        get { UpdateWorldIfNeeded(); return _worldVersion; }
    }

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
            if (_localRotation == value) return;
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
        get { UpdateWorldIfNeeded(); return _worldPosition; }
        set
        {
            if (_parent is null)
            {
                LocalPosition = value;
                return;
            }

            _parent.UpdateWorldIfNeeded();

            // inverse TRS: local = inv(parent) * world
            // inv translate
            var v = value - _parent._worldPosition;

            // inv rotate
            var pr = _parent._worldRotation;
            if (pr != 0f) v = Rotate(v, -pr);

            // inv scale (guard)
            var ps = _parent._worldScale;
            if (ps.X == 0f || ps.Y == 0f)
                throw new InvalidOperationException("Cannot set WorldPosition when parent WorldScale has zero component.");

            v = new Vector2(v.X / ps.X, v.Y / ps.Y);
            LocalPosition = v;
        }
    }

    public float WorldRotation
    {
        get { UpdateWorldIfNeeded(); return _worldRotation; }
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
        get { UpdateWorldIfNeeded(); return _worldScale; }
        set
        {
            if (_parent is null)
            {
                LocalScale = value;
                return;
            }

            _parent.UpdateWorldIfNeeded();

            var ps = _parent._worldScale;
            if (ps.X == 0f || ps.Y == 0f)
                throw new InvalidOperationException("Cannot set WorldScale when parent WorldScale has zero component.");

            LocalScale = new Vector2(value.X / ps.X, value.Y / ps.Y);
        }
    }

    public Matrix3x2 LocalMatrix
    {
        get { UpdateLocalMatrixIfNeeded(); return _localMatrix; }
    }

    public Matrix3x2 WorldMatrix
    {
        get
        {
            UpdateWorldIfNeeded();
            UpdateWorldMatrixIfNeeded();
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

        // world зависит от родителя: пересчёт по demand
        _worldDirty = true;
        _worldMatrixDirty = true;
        _parentWorldVersionAtCompute = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MarkLocalDirty()
    {
        _localMatrixDirty = true;
        _worldDirty = true;
        _worldMatrixDirty = true;
        _localVersion++;
    }

    private void UpdateLocalMatrixIfNeeded()
    {
        if (!_localMatrixDirty) return;

        var sx = _localScale.X;
        var sy = _localScale.Y;
        var c  = MathF.Cos(_localRotation);
        var s  = MathF.Sin(_localRotation);

        _localMatrix = new Matrix3x2(
            c * sx,  s * sx,
           -s * sy,  c * sy,
            _localPosition.X, _localPosition.Y);

        _localMatrixDirty = false;
    }

    private void UpdateWorldIfNeeded()
    {
        _parent?.UpdateWorldIfNeeded();

        if (!_worldDirty)
        {
            var parentVer = _parent?._worldVersion ?? 0;
            if (_parentWorldVersionAtCompute == parentVer)
                return;
        }

        if (_parent is null)
        {
            _worldPosition = _localPosition;
            _worldRotation = _localRotation;
            _worldScale    = _localScale;
        }
        else
        {
            // TRS-composition без матричного умножения (быстро; подходит для 2D).
            // Важно: предполагаем TRS-модель без намеренного shear.
            var ps = _parent._worldScale;

            var v = new Vector2(_localPosition.X * ps.X, _localPosition.Y * ps.Y);

            var pr = _parent._worldRotation;
            if (pr != 0f) v = Rotate(v, pr);

            _worldPosition = _parent._worldPosition + v;
            _worldRotation = _localRotation + _parent._worldRotation;
            _worldScale    = _localScale * ps;
        }

        _worldDirty = false;
        _worldMatrixDirty = true;
        _parentWorldVersionAtCompute = _parent?._worldVersion ?? 0;
        _worldVersion++;
    }

    private void UpdateWorldMatrixIfNeeded()
    {
        if (!_worldMatrixDirty) return;

        var sx = _worldScale.X;
        var sy = _worldScale.Y;
        var c  = MathF.Cos(_worldRotation);
        var s  = MathF.Sin(_worldRotation);

        _worldMatrix = new Matrix3x2(
            c * sx,  s * sx,
           -s * sy,  c * sy,
            _worldPosition.X, _worldPosition.Y);

        _worldMatrixDirty = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 Rotate(Vector2 v, float radians)
    {
        var c = MathF.Cos(radians);
        var s = MathF.Sin(radians);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }
}
