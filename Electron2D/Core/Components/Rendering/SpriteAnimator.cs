namespace Electron2D;

/// <summary>
/// Проигрыватель flipbook-анимаций. По умолчанию автоматически управляет SpriteRenderer на том же Node.
/// </summary>
public sealed class SpriteAnimator : IComponent
{
    private const int UnregisteredSceneIndex = -1;

    // если будешь индексировать в SceneTree (как SpriteRenderer)
    internal int SceneIndex = UnregisteredSceneIndex;

    private Node? _owner;
    private SpriteRenderer? _renderer;

    private SpriteAnimationClip? _clip;
    private int _frameIndex;
    private float _timeLeft;

    private bool _playing = true;

    private float _speed = 1f; // <0 = reverse
    public float Speed
    {
        get => _speed;
        set
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Speed must be finite.");
            _speed = value;
        }
    }

    public bool Enabled { get; set; } = true;

    private Signal? _onFinished;
    public Signal OnFinished => _onFinished ??= new Signal();

    public SpriteAnimationClip? CurrentClip => _clip;

    public void OnAttach(Node owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        _owner = owner;
        SceneIndex = UnregisteredSceneIndex;

        // Автопривязка, если SpriteRenderer уже есть (порядок AddComponent не важен).
        owner.TryGetComponent<SpriteRenderer>(out _renderer);

        // Если клип уже назначен (редкий кейс) — применим кадр.
        ApplyFrameIfPossible();
    }

    public void OnDetach()
    {
        _owner = null;
        _renderer = null;

        _clip = null;
        _frameIndex = 0;
        _timeLeft = 0f;

        _playing = false;
        SceneIndex = UnregisteredSceneIndex;
    }

    /// <summary>
    /// Явно задать рендерер (не обязательно для стандартного кейса “на том же Node”).
    /// </summary>
    public void SetTarget(SpriteRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        _renderer = renderer;
        ApplyFrameIfPossible();
    }

    public void Play(SpriteAnimationClip clip, bool restart = true)
    {
        ArgumentNullException.ThrowIfNull(clip);

        _clip = clip;

        if (restart)
            ResetToStartFrame();

        _playing = true;
        ApplyFrameIfPossible();
    }

    public void Stop() => _playing = false;
    public void Pause() => _playing = false;
    public void Resume() => _playing = true;

    internal void InternalTick(float deltaSeconds)
    {
        if (!Enabled || !_playing)
            return;

        var clip = _clip;
        if (clip is null)
            return;

        var renderer = _renderer;

        // Если рендерера ещё нет (например, SpriteAnimator добавили раньше SpriteRenderer),
        // то он будет подхвачен через Node.AddComponentInstance (Patch 3). Здесь не сканируем каждый кадр.

        if (renderer is null)
            return;

        if (!(deltaSeconds > 0f) || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
            return;

        var speed = _speed;
        if (speed == 0f)
            return;

        var frames = clip.Frames;
        if (speed > 0f)
            TickForward(frames, deltaSeconds * speed, clip.Loop);
        else
            TickReverse(frames, deltaSeconds * -speed, clip.Loop);
    }

    internal void InternalBindIfEmpty(SpriteRenderer renderer)
    {
        if (_renderer is not null)
            return;

        _renderer = renderer;
        ApplyFrameIfPossible();
    }

    private void ResetToStartFrame()
    {
        var clip = _clip!;
        var frames = clip.Frames;

        _frameIndex = _speed < 0f ? frames.Length - 1 : 0;
        _timeLeft = frames[_frameIndex].DurationSeconds;
    }

    private void TickForward(SpriteAnimationFrame[] frames, float dt, bool loop)
    {
        var idx = _frameIndex;
        var timeLeft = _timeLeft;

        if (!(timeLeft > 0f) || float.IsNaN(timeLeft) || float.IsInfinity(timeLeft))
            timeLeft = frames[idx].DurationSeconds;

        while (dt >= timeLeft)
        {
            dt -= timeLeft;

            idx++;
            if (idx >= frames.Length)
            {
                if (!loop)
                {
                    idx = frames.Length - 1;
                    _frameIndex = idx;
                    _timeLeft = frames[idx].DurationSeconds;
                    _playing = false;
                    ApplyFrameIfPossible();
                    _onFinished?.Emit();
                    return;
                }

                idx = 0;
            }

            timeLeft = frames[idx].DurationSeconds;
            _frameIndex = idx;
            _timeLeft = timeLeft;
            ApplyFrameIfPossible();
        }

        _frameIndex = idx;
        _timeLeft = timeLeft - dt;
    }

    private void TickReverse(SpriteAnimationFrame[] frames, float dt, bool loop)
    {
        var idx = _frameIndex;
        var timeLeft = _timeLeft;

        if (!(timeLeft > 0f) || float.IsNaN(timeLeft) || float.IsInfinity(timeLeft))
            timeLeft = frames[idx].DurationSeconds;

        while (dt >= timeLeft)
        {
            dt -= timeLeft;

            idx--;
            if (idx < 0)
            {
                if (!loop)
                {
                    idx = 0;
                    _frameIndex = idx;
                    _timeLeft = frames[idx].DurationSeconds;
                    _playing = false;
                    ApplyFrameIfPossible();
                    _onFinished?.Emit();
                    return;
                }

                idx = frames.Length - 1;
            }

            timeLeft = frames[idx].DurationSeconds;
            _frameIndex = idx;
            _timeLeft = timeLeft;
            ApplyFrameIfPossible();
        }

        _frameIndex = idx;
        _timeLeft = timeLeft - dt;
    }

    private void ApplyFrameIfPossible()
    {
        var renderer = _renderer;
        var clip = _clip;

        if (renderer is null || clip is null)
            return;

        var frames = clip.Frames;
        if (frames.Length == 0)
            return;

        renderer.SetSprite(frames[_frameIndex].Sprite);
    }
}
