using System.Numerics;
using System.Runtime.CompilerServices;

namespace Electron2D;

/// <summary>
/// Трансформация узла (2D TRS: position/rotation/scale) с ленивым пересчётом world-состояния и квантованием.
/// </summary>
public sealed class Transform
{
    #region Constants
    // Quantization:
    // Pos: 1e-4..1e-3 (0.1–1 мм), Rot: 1e-6..1e-5 рад, Scale: 1e-6
    private const float PosQuantum = 1e-4f;
    private const float RotQuantum = 1e-6f;
    private const float ScaleQuantum = 1e-6f;

    private const float InvPosQuantum = 1f / PosQuantum;
    private const float InvRotQuantum = 1f / RotQuantum;
    private const float InvScaleQuantum = 1f / ScaleQuantum;
    #endregion

    #region Instance fields
    private readonly Node _owner;

    private Transform? _parent;

    private Vector2 _localPosition;
    private float _localRotation;
    private Vector2 _localScale = Vector2.One;

    private bool _localMatrixDirty = true;
    private Matrix3x2 _localMatrix;

    // World TRS
    private bool _worldDirty = true;
    private Vector2 _worldPosition;
    private float _worldRotation;
    private Vector2 _worldScale = Vector2.One;

    // World matrix computed only when requested
    private bool _worldMatrixDirty = true;
    private Matrix3x2 _worldMatrix;

    private int _localVersion;
    private int _worldVersion;
    private int _parentWorldVersionAtCompute;
    #endregion

    #region Constructors
    internal Transform(Node owner) => _owner = owner;
    #endregion

    #region Properties
    internal Transform? Parent => _parent;

    internal int LocalVersion => _localVersion;

    /// <summary>
    /// Версия world TRS. Инкрементится, когда world (pos/rot/scale) реально пересчитан.
    /// Это и есть "dirt" для рендера/физики: потребители кешируют lastWorldVer.
    /// </summary>
    internal int WorldVersion
    {
        get
        {
            UpdateWorldIfNeeded();
            return _worldVersion;
        }
    }

    public Vector2 LocalPosition
    {
        get => _localPosition;
        set
        {
            var q = Quantize(value, InvPosQuantum, PosQuantum);
            if (_localPosition == q)
                return;

            _localPosition = q;
            MarkLocalDirty();
        }
    }

    public float LocalRotation
    {
        get => _localRotation;
        set
        {
            var q = QuantizeAngle(value);
            if (_localRotation == q)
                return;

            _localRotation = q;
            MarkLocalDirty();
        }
    }

    public Vector2 LocalScale
    {
        get => _localScale;
        set
        {
            var q = Quantize(value, InvScaleQuantum, ScaleQuantum);
            if (_localScale == q)
                return;

            _localScale = q;
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
            var parent = _parent;
            if (parent is null)
            {
                LocalPosition = value;
                return;
            }

            parent.UpdateWorldIfNeeded();

            // inverse TRS: local = inv(parent) * world
            // inv translate
            var v = value - parent._worldPosition;

            // inv rotate
            var parentRot = parent._worldRotation;
            if (parentRot != 0f)
                v = Rotate(v, -parentRot);

            // inv scale (guard)
            var parentScale = parent._worldScale;
            if (parentScale.X == 0f || parentScale.Y == 0f)
                throw new InvalidOperationException("Cannot set WorldPosition when parent WorldScale has zero component.");

            v = new Vector2(v.X / parentScale.X, v.Y / parentScale.Y);
            LocalPosition = v;
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
            var parent = _parent;
            if (parent is null)
            {
                LocalRotation = value;
                return;
            }

            parent.UpdateWorldIfNeeded();
            LocalRotation = value - parent._worldRotation;
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
            var parent = _parent;
            if (parent is null)
            {
                LocalScale = value;
                return;
            }

            parent.UpdateWorldIfNeeded();

            var parentScale = parent._worldScale;
            if (parentScale.X == 0f || parentScale.Y == 0f)
                throw new InvalidOperationException("Cannot set WorldScale when parent WorldScale has zero component.");

            LocalScale = new Vector2(value.X / parentScale.X, value.Y / parentScale.Y);
        }
    }

    public Matrix3x2 LocalMatrix
    {
        get
        {
            UpdateLocalMatrixIfNeeded();
            return _localMatrix;
        }
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
    #endregion

    #region Public API
    public void Translate(Vector2 delta)
    {
        if (delta == Vector2.Zero)
            return;

        var q = Quantize(_localPosition + delta, InvPosQuantum, PosQuantum);
        if (_localPosition == q)
            return;

        _localPosition = q;
        MarkLocalDirty();
    }

    public void Translate(float x = 0f, float y = 0f) => Translate(new Vector2(x, y));

    public void TranslateX(float x) => Translate(new Vector2(x, 0f));

    public void TranslateY(float y) => Translate(new Vector2(0f, y));

    public void TranslateSelf(Vector2 delta)
    {
        if (delta == Vector2.Zero)
            return;

        // Обновим world TRS один раз, чтобы не дергать свойства несколько раз.
        UpdateWorldIfNeeded();

        // Переводим локальный сдвиг (в осях объекта) в world-сдвиг.
        var a = _worldRotation;
        var worldDelta = a == 0f ? delta : Rotate(delta, a);

        WorldPosition = _worldPosition + worldDelta;
    }

    public void TranslateSelf(float x = 0f, float y = 0f) => TranslateSelf(new Vector2(x, y));

    public void TranslateSelfX(float x) => TranslateSelf(new Vector2(x, 0f));

    public void TranslateSelfY(float y) => TranslateSelf(new Vector2(0f, y));

    public void Rotate(float radians)
    {
        if (radians == 0f)
            return;

        var q = QuantizeAngle(_localRotation + radians);
        if (_localRotation == q)
            return;

        _localRotation = q;
        MarkLocalDirty();
    }

    public void RotateRight(float radians) => Rotate(-radians);

    public void RotateLeft(float radians) => Rotate(+radians);

    public void Scale(Vector2 scale)
    {
        if (scale == Vector2.One)
            return;

        var q = Quantize(_localScale * scale, InvScaleQuantum, ScaleQuantum);
        if (_localScale == q)
            return;

        _localScale = q;
        MarkLocalDirty();
    }
    #endregion

    #region Internal helpers
    internal void GetWorldTRS(out Vector2 pos, out float rot, out Vector2 scale, out int worldVersion)
    {
        UpdateWorldIfNeeded();
        pos = _worldPosition;
        rot = _worldRotation;
        scale = _worldScale;
        worldVersion = _worldVersion;
    }

    internal void SetParent(Transform? parent)
    {
        if (_parent == parent)
            return;

        _parent = parent;

        // World зависит от родителя: пересчёт по demand.
        _worldDirty = true;
        _worldMatrixDirty = true;
        _parentWorldVersionAtCompute = -1;
    }
    #endregion

    #region Private helpers
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
        if (!_localMatrixDirty)
            return;

        var sx = _localScale.X;
        var sy = _localScale.Y;

        var (s, c) = MathF.SinCos(_localRotation);

        _localMatrix = new Matrix3x2(
            c * sx, s * sx,
            -s * sy, c * sy,
            _localPosition.X, _localPosition.Y);

        _localMatrixDirty = false;
    }

    private void UpdateWorldIfNeeded()
    {
        var parent = _parent;

        if (!_worldDirty)
        {
            if (parent is null)
                return;

            // Быстрый guard: если parent world не менялся с момента последнего вычисления.
            if (!parent._worldDirty && _parentWorldVersionAtCompute == parent._worldVersion)
                return;
        }

        parent?.UpdateWorldIfNeeded();

        if (parent is null)
        {
            _worldPosition = _localPosition;
            _worldRotation = _localRotation;
            _worldScale = _localScale;
        }
        else
        {
            // TRS-composition без матричного умножения (быстро; подходит для 2D).
            // Важно: предполагаем TRS-модель без намеренного shear.
            var parentScale = parent._worldScale;

            var v = new Vector2(_localPosition.X * parentScale.X, _localPosition.Y * parentScale.Y);

            var parentRot = parent._worldRotation;
            if (parentRot != 0f)
                v = Rotate(v, parentRot);

            _worldPosition = parent._worldPosition + v;
            _worldRotation = _localRotation + parent._worldRotation;
            _worldScale = _localScale * parentScale;
        }

        _worldDirty = false;
        _worldMatrixDirty = true;
        _parentWorldVersionAtCompute = parent?._worldVersion ?? 0;
        _worldVersion++;
    }

    private void UpdateWorldMatrixIfNeeded()
    {
        if (!_worldMatrixDirty)
            return;

        var sx = _worldScale.X;
        var sy = _worldScale.Y;

        var (s, c) = MathF.SinCos(_worldRotation);

        _worldMatrix = new Matrix3x2(
            c * sx, s * sx,
            -s * sy, c * sy,
            _worldPosition.X, _worldPosition.Y);

        _worldMatrixDirty = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 Rotate(Vector2 v, float radians)
    {
        var (s, c) = MathF.SinCos(radians);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Quantize(float v, float invQ, float q)
        => MathF.Round(v * invQ, MidpointRounding.AwayFromZero) * q;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 Quantize(Vector2 v, float invQ, float q)
        => new(
            MathF.Round(v.X * invQ, MidpointRounding.AwayFromZero) * q,
            MathF.Round(v.Y * invQ, MidpointRounding.AwayFromZero) * q);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float WrapAngle(float a)
    {
        const float TwoPi = MathF.PI * 2f;

        a %= TwoPi;

        if (a <= -MathF.PI)
            a += TwoPi;
        else if (a > MathF.PI)
            a -= TwoPi;

        return a;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float QuantizeAngle(float radians)
    {
        radians = WrapAngle(radians);
        return Quantize(radians, InvRotQuantum, RotQuantum);
    }
    #endregion
}
