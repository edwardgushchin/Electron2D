namespace Electron2D;

public sealed class SpriteAnimator : IComponent
{
    private const int UnregisteredSceneIndex = -1;
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

        owner.TryGetComponent<SpriteRenderer>(out _renderer);
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

    public void SetTarget(SpriteRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        _renderer = renderer;
        ApplyFrameIfPossible();
    }

    // ВАЖНО: restartIfSame, а не restart.
    // При смене клипа — всегда стартуем сначала.
    public void Play(SpriteAnimationClip clip, bool restartIfSame = false)
    {
        ArgumentNullException.ThrowIfNull(clip);

        if (ReferenceEquals(_clip, clip))
        {
            if (restartIfSame)
                ResetToStartFrame();

            _playing = true;
            ApplyFrameIfPossible();
            return;
        }

        _clip = clip;
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
        if (renderer is null)
            return;

        if (!(deltaSeconds > 0f) || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
            return;

        var speed = _speed;
        if (speed == 0f)
            return;

        var dt = deltaSeconds * MathF.Abs(speed);

        if (speed > 0f)
            TickForward(clip, dt);
        else
            TickReverse(clip, dt);
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
        _timeLeft = clip.FrameDurationSeconds;
    }

    private void TickForward(SpriteAnimationClip clip, float dt)
    {
        var frames = clip.Frames;
        var frameTime = clip.FrameDurationSeconds;

        var idx = _frameIndex;
        var timeLeft = _timeLeft > 0f ? _timeLeft : frameTime;

        timeLeft -= dt;

        if (timeLeft > 0f)
        {
            _timeLeft = timeLeft;
            return;
        }

        // продвигаемся по кадрам, учитывая, что dt мог "съесть" несколько кадров
        while (timeLeft <= 0f)
        {
            idx++;

            if (idx >= frames.Length)
            {
                if (!clip.Loop)
                {
                    idx = frames.Length - 1;
                    _frameIndex = idx;
                    _timeLeft = frameTime;
                    _playing = false;
                    ApplyFrameIfPossible();
                    _onFinished?.Emit();
                    return;
                }

                idx = 0;
            }

            timeLeft += frameTime;
        }

        _frameIndex = idx;
        _timeLeft = timeLeft;
        ApplyFrameIfPossible();
    }

    private void TickReverse(SpriteAnimationClip clip, float dt)
    {
        var frames = clip.Frames;
        var frameTime = clip.FrameDurationSeconds;

        var idx = _frameIndex;
        var timeLeft = _timeLeft > 0f ? _timeLeft : frameTime;

        timeLeft -= dt;

        if (timeLeft > 0f)
        {
            _timeLeft = timeLeft;
            return;
        }

        while (timeLeft <= 0f)
        {
            idx--;

            if (idx < 0)
            {
                if (!clip.Loop)
                {
                    idx = 0;
                    _frameIndex = idx;
                    _timeLeft = frameTime;
                    _playing = false;
                    ApplyFrameIfPossible();
                    _onFinished?.Emit();
                    return;
                }

                idx = frames.Length - 1;
            }

            timeLeft += frameTime;
        }

        _frameIndex = idx;
        _timeLeft = timeLeft;
        ApplyFrameIfPossible();
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

        var idx = _frameIndex;
        if ((uint)idx >= (uint)frames.Length)
            idx = _frameIndex = Math.Clamp(idx, 0, frames.Length - 1);

        renderer.SetSprite(frames[idx]);
    }
}
