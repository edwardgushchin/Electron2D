using System.Numerics;
using SDL3;

namespace Electron2D;

internal sealed class RenderSystem : IDisposable
{
    private const float RadToDeg = 180f / MathF.PI;

    private readonly RenderQueue _queue = new(initialCapacity: 2048);

    private nint _handle; // SDL_Renderer*
    private bool _ownsHandle;
    
    private nint _lastTextureThisFrame;
    private readonly nint[] _uniqueTextureTable = new nint[1024]; // open addressing
    private int _uniqueTextureCount;

    private SceneTree? _scene;

    private bool _viewValidThisFrame;
    private ViewState _view;

    private float _fallbackOrthoSize = 5f;

    private bool _debugGridEnabled;
    private Color _debugGridBackground;
    private Color _debugGridLine;
    private Color _debugGridAxis;

    internal nint Handle => _handle;

    internal VSyncMode EffectiveVSync { get; private set; } = VSyncMode.Disabled;
    internal int EffectiveVSyncInterval { get; private set; } = 1;

    // Если VSync запрошен, но реально отключился (не поддержан) — можно предложить cap.
    internal int SuggestedMaxFps { get; private set; } = 0;

    public RenderQueue Queue => _queue;

    /// <summary>
    /// Инициализирует систему рендера, создавая SDL_Renderer* для указанного SDL_Window*.
    /// </summary>
    /// <remarks>Main thread only (SDL).</remarks>
    public void Initialize(nint windowHandle, EngineConfig cfg)
    {
        if (windowHandle == 0)
            throw new ArgumentOutOfRangeException(nameof(windowHandle));

        if (_handle != 0)
            throw new InvalidOperationException("RenderSystem.Initialize: already initialized.");

        var renderer = SDL.CreateRenderer(windowHandle, name: null);
        if (renderer == 0)
            throw new InvalidOperationException($"SDL.CreateRenderer failed. {SDL.GetError()}");

        _handle = renderer;
        _ownsHandle = true;

        _queue.Preallocate(cfg.RenderQueueCapacity);

        ConfigureDebugGrid(cfg);
        ApplyVSync(cfg);

        _fallbackOrthoSize = 5f;
    }

    public void BeginFrame(SceneTree scene)
    {
        ThrowIfNotInitialized();
        ArgumentNullException.ThrowIfNull(scene);
        
        using var _ = Profiler.Sample(ProfilerSampleId.RenderBeginFrame);

        _scene = scene;
        _viewValidThisFrame = false;

        // Очередь команд frame-local: без накопления draw calls между кадрами.
        _queue.Clear();
        
        _lastTextureThisFrame = 0;
        Array.Clear(_uniqueTextureTable, 0, _uniqueTextureTable.Length);
        _uniqueTextureCount = 0;

        var bg = _debugGridEnabled ? _debugGridBackground : scene.ClearColor;
        SDL.SetRenderDrawColor(_handle, bg.Red, bg.Green, bg.Blue, bg.Alpha);
        SDL.RenderClear(_handle);
        Profiler.AddCounter(ProfilerCounterId.RenderClears, 1);
    }

    public void BuildRenderQueue(SceneTree sceneTree, ResourceSystem resources)
    {
        ThrowIfNotInitialized();
        ArgumentNullException.ThrowIfNull(sceneTree);
        ArgumentNullException.ThrowIfNull(resources);
        
        using var _ = Profiler.Sample(ProfilerSampleId.RenderBuildQueue);

        _scene = sceneTree;

        ref readonly var view = ref EnsureView();

        var renderers = sceneTree.SpriteRenderers;

        // Полезно: заранее гарантировать ёмкость под "все рендереры"
        // чтобы TryPush не триггерил рост/аренду во время кадра.
        _queue.EnsureCapacity(renderers.Length);

        for (var i = 0; i < renderers.Length; i++)
            renderers[i].PrepareRender(_queue, resources, in view.Cull);
    }

    private void Flush()
    {
        ThrowIfNotInitialized();
        
        using var _ = Profiler.Sample(ProfilerSampleId.RenderFlush);

        var cmds = _queue.CommandsMutable;

        // Если нет команд и сетка выключена — нечего делать.
        if (cmds.Length == 0 && !_debugGridEnabled)
            return;
        
        // bulk counters (sprites ~= draw calls for sprites in SDL_Renderer path)
        if (cmds.Length > 0)
        {
            Profiler.AddCounter(ProfilerCounterId.RenderSprites, cmds.Length);
            Profiler.AddCounter(ProfilerCounterId.RenderDrawCalls, cmds.Length);
            Profiler.AddCounter(ProfilerCounterId.RenderTextureColorMods, cmds.Length);
            Profiler.AddCounter(ProfilerCounterId.RenderTextureAlphaMods, cmds.Length);
        }

        if (cmds.Length > 1 && _queue.NeedsSort)
        {
            Profiler.AddCounter(ProfilerCounterId.RenderSortTriggered, 1);
            Profiler.SetCounter(ProfilerCounterId.RenderSortCommands, cmds.Length);

            using (Profiler.Sample(ProfilerSampleId.RenderSort))
                SpriteCommandSorter.Sort(cmds);
        }

        ref readonly var view = ref EnsureView();

        // Фон: сетка рисуется ДО спрайтов.
        if (_debugGridEnabled)
            DrawDebugGrid1Unit(in view);

        for (var i = 0; i < cmds.Length; i++)
            DrawSprite(in cmds[i], in view);
    }

    public void EndFrame()
    {
        ThrowIfNotInitialized();

        Flush();

        // finalize render-frame counters
        Profiler.SetCounter(ProfilerCounterId.RenderUniqueTextures, _uniqueTextureCount);

        using (Profiler.Sample(ProfilerSampleId.RenderPresent))
        {
            SDL.RenderPresent(_handle);
            Profiler.AddCounter(ProfilerCounterId.RenderPresents, 1);
        }
    }

    public void Shutdown() => Dispose();

    public void Dispose()
    {
        _queue.Dispose();

        if (_ownsHandle && _handle != 0)
            SDL.DestroyRenderer(_handle);

        _ownsHandle = false;
        _handle = 0;

        _scene = null;
        _viewValidThisFrame = false;
        _view = default;
    }

    // -----------------------------
    // View
    // -----------------------------

    private ref readonly ViewState EnsureView()
    {
        if (_viewValidThisFrame)
            return ref _view;

        _view = BuildViewState();
        _viewValidThisFrame = true;
        return ref _view;
    }

    private ViewState BuildViewState()
    {
        SDL.GetRenderOutputSize(_handle, out var outW, out var outH);

        // На всякий случай (в т.ч. при сворачивании/ресайзе).
        if (outW <= 0) outW = 1;
        if (outH <= 0) outH = 1;

        var halfW = outW * 0.5f;
        var halfH = outH * 0.5f;

        var cam = _scene?.EnsureCurrentCamera();

        Vector2 camPos;
        float camRot;

        if (cam is null)
        {
            camPos = Vector2.Zero;
            camRot = 0f;
        }
        else
        {
            camPos = cam.Transform.WorldPosition;
            camRot = cam.Transform.WorldRotation;
        }

        var hasRot = camRot != 0f;

        // Храним cos/sin(camRot) и применяем поворот на -camRot формулой [c s; -s c].
        var c = hasRot ? MathF.Cos(camRot) : 1f;
        var s = hasRot ? MathF.Sin(camRot) : 0f;

        var orthoSize = cam?.OrthoSize ?? _fallbackOrthoSize;
        if (!(orthoSize > 0f)) orthoSize = 0.0001f;

        // Pixels per 1 world-unit по вертикали.
        var ppu = outH / (2f * orthoSize);
        if (!(ppu > 0f)) ppu = 0.0001f;

        // Cull rect (world units)
        var invPpu = 1f / ppu;
        var pad = 2f * invPpu; // ~2px safety to avoid edge popping

        var halfWWorld = halfW * invPpu;
        var halfHWorld = halfH * invPpu;

        if (hasRot)
        {
            var r = MathF.Sqrt(halfWWorld * halfWWorld + halfHWorld * halfHWorld);
            halfWWorld = r;
            halfHWorld = r;
        }

        var cull = new ViewCullRect(
            camPos.X - halfWWorld - pad,
            camPos.Y - halfHWorld - pad,
            camPos.X + halfWWorld + pad,
            camPos.Y + halfHWorld + pad
        );

        return new ViewState(
            halfW: halfW,
            halfH: halfH,
            ppu: ppu,
            camPos: camPos,
            camRot: camRot,
            cos: c,
            sin: s,
            hasRot: hasRot,
            cull: cull
        );
    }

    // -----------------------------
    // Drawing
    // -----------------------------

    private void DrawSprite(in SpriteCommand cmd, in ViewState view)
    {
        var tex = cmd.Texture;
        if (!tex.IsValid) return;
        
        var size = cmd.SizeWorld;
        if (size is not { X: > 0f, Y: > 0f }) return;
        
        // texture bind heuristic: считаем "bind", когда меняется handle в последовательности команд
        var h = tex.Handle;
        if (h != _lastTextureThisFrame)
        {
            _lastTextureThisFrame = h;
            Profiler.AddCounter(ProfilerCounterId.RenderTextureBinds, 1);
        }

        // unique textures per frame (fixed hash table)
        TrackUniqueTexture(h);

        // World -> view (camera space)
        var rel = cmd.PositionWorld - view.CamPos;

        float vx, vy;
        if (view.HasRot)
        {
            // rotate rel by -camRot: [ c  s; -s  c ]
            vx = rel.X * view.Cos + rel.Y * view.Sin;
            vy = -rel.X * view.Sin + rel.Y * view.Cos;
        }
        else
        {
            vx = rel.X;
            vy = rel.Y;
        }

        // view -> screen
        var pivotX = view.HalfW + vx * view.Ppu;
        var pivotY = view.HalfH - vy * view.Ppu;

        var wPx = cmd.SizeWorld.X * view.Ppu;
        var hPx = cmd.SizeWorld.Y * view.Ppu;

        if (!(wPx > 0f) || !(hPx > 0f))
            return;

        // Origin задаётся из нижнего-левого (World Y-up).
        // Смещение до top-left по Y = (SizeY - OriginY).
        var originPxX = cmd.OriginWorld.X * view.Ppu;
        var originPxY = (cmd.SizeWorld.Y - cmd.OriginWorld.Y) * view.Ppu;

        var dst = new SDL.FRect
        {
            X = pivotX - originPxX,
            Y = pivotY - originPxY,
            W = wPx,
            H = hPx
        };

        var src = new SDL.FRect
        {
            X = cmd.SrcRect.X,
            Y = cmd.SrcRect.Y,
            W = cmd.SrcRect.Width,
            H = cmd.SrcRect.Height
        };

        // SDL angle — по часовой (Y вниз). В мире обычно CCW => минус.
        var rot = view.HasRot ? (cmd.Rotation - view.CamRot) : cmd.Rotation;
        var angleDeg = -rot * RadToDeg;

        var center = new SDL.FPoint { X = originPxX, Y = originPxY };

        SDL.SetTextureColorMod(tex.Handle, cmd.Color.Red, cmd.Color.Green, cmd.Color.Blue);
        SDL.SetTextureAlphaMod(tex.Handle, cmd.Color.Alpha);

        SDL.RenderTextureRotated(
            _handle,
            tex.Handle,
            in src,
            in dst,
            angleDeg,
            in center,
            (SDL.FlipMode)cmd.FlipMode
        );
    }
    
    private void TrackUniqueTexture(nint handle)
    {
        if (handle == 0) return;

        // table size = 1024 => mask
        const int mask = 1024 - 1;
        var key = (ulong)handle;
        var idx = (int)((key * 11400714819323198485UL) & mask);

        for (var probe = 0; probe < 1024; probe++)
        {
            var cur = _uniqueTextureTable[idx];
            if (cur == 0)
            {
                _uniqueTextureTable[idx] = handle;
                _uniqueTextureCount++;
                return;
            }
            if (cur == handle)
                return;

            idx = (idx + 1) & mask;
        }

        // таблица переполнена — игнорируем (в 2D это крайне маловероятно)
    }

    private void DrawDebugGrid1Unit(in ViewState view)
    {
        // Половины в world-units
        var viewHalfW = view.HalfW / view.Ppu;
        var viewHalfH = view.HalfH / view.Ppu;

        // При вращении камеры берём bounding-square по диагонали, чтобы сетка точно закрыла экран
        var halfDiag = MathF.Sqrt(viewHalfW * viewHalfW + viewHalfH * viewHalfH);

        var xMin = view.CamPos.X - halfDiag;
        var xMax = view.CamPos.X + halfDiag;
        var yMin = view.CamPos.Y - halfDiag;
        var yMax = view.CamPos.Y + halfDiag;

        var xi0 = (int)MathF.Floor(xMin);
        var xi1 = (int)MathF.Ceiling(xMax);
        var yi0 = (int)MathF.Floor(yMin);
        var yi1 = (int)MathF.Ceiling(yMax);

        // 1) обычные линии (кроме осей)
        SetDrawColor(_debugGridLine);

        for (var x = xi0; x <= xi1; x++)
        {
            if (x == 0) continue;
            DrawWorldLine(x, yMin, x, yMax, in view);
        }

        for (var y = yi0; y <= yi1; y++)
        {
            if (y == 0) continue;
            DrawWorldLine(xMin, y, xMax, y, in view);
        }

        // 2) оси координат (чуть ярче)
        SetDrawColor(_debugGridAxis);

        if (0 >= xi0 && 0 <= xi1) DrawWorldLine(0f, yMin, 0f, yMax, in view);
        if (0 >= yi0 && 0 <= yi1) DrawWorldLine(xMin, 0f, xMax, 0f, in view);
    }

    private void DrawWorldLine(float x1, float y1, float x2, float y2, in ViewState view)
    {
        WorldToScreen(x1, y1, out var sx1, out var sy1, in view);
        WorldToScreen(x2, y2, out var sx2, out var sy2, in view);
        
        Profiler.AddCounter(ProfilerCounterId.RenderDebugLines, 1);
        Profiler.AddCounter(ProfilerCounterId.RenderDrawCalls, 1);

        SDL.RenderLine(_handle, sx1, sy1, sx2, sy2);
    }

    private static void WorldToScreen(float wx, float wy, out float sx, out float sy, in ViewState view)
    {
        var rx = wx - view.CamPos.X;
        var ry = wy - view.CamPos.Y;

        // поворот на -camRot: [c s; -s c]
        var vx = rx * view.Cos + ry * view.Sin;
        var vy = -rx * view.Sin + ry * view.Cos;

        sx = view.HalfW + vx * view.Ppu;
        sy = view.HalfH - vy * view.Ppu;
    }

    // -----------------------------
    // Init helpers
    // -----------------------------

    private void ConfigureDebugGrid(EngineConfig cfg)
    {
        _debugGridEnabled = cfg.DebugGridEnabled;
        _debugGridBackground = cfg.DebugGridColor;
        _debugGridLine = cfg.DebugGridLineColor;

        // Оси чуть ярче обычной линии (без нового публичного параметра)
        _debugGridAxis = _debugGridLine.AddRGB(0x22);
    }

    private void ApplyVSync(EngineConfig cfg)
    {
        EffectiveVSync = cfg.VSync;
        EffectiveVSyncInterval = Math.Max(1, cfg.VSyncInterval);
        SuggestedMaxFps = 0;

        var requested = cfg.VSync switch
        {
            VSyncMode.Disabled => 0,
            VSyncMode.Adaptive => -1,
            VSyncMode.Enabled => Math.Max(1, cfg.VSyncInterval),
            _ => 0
        };

        if (SDL.SetRenderVSync(_handle, requested))
            return;

        // Fallback chain: Adaptive -> 1 -> 0, Interval>1 -> 1 -> 0
        if (requested == -1)
        {
            if (SDL.SetRenderVSync(_handle, 1))
            {
                EffectiveVSync = VSyncMode.Enabled;
                EffectiveVSyncInterval = 1;
                return;
            }

            SDL.SetRenderVSync(_handle, 0);
            EffectiveVSync = VSyncMode.Disabled;
            SuggestedMaxFps = 60;
            return;
        }

        if (requested > 1)
        {
            if (SDL.SetRenderVSync(_handle, 1))
            {
                EffectiveVSync = VSyncMode.Enabled;
                EffectiveVSyncInterval = 1;
                return;
            }

            SDL.SetRenderVSync(_handle, 0);
            EffectiveVSync = VSyncMode.Disabled;
            SuggestedMaxFps = 60;
            return;
        }

        // requested == 0 или 1, но SetRenderVSync всё равно не сработал
        EffectiveVSync = VSyncMode.Disabled;
        SuggestedMaxFps = 60;
    }

    private void ThrowIfNotInitialized()
    {
        if (_handle == 0)
            throw new InvalidOperationException("RenderSystem is not initialized.");
    }

    private void SetDrawColor(in Color c)
        => SDL.SetRenderDrawColor(_handle, c.Red, c.Green, c.Blue, c.Alpha);
}