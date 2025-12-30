using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using SDL3;

namespace Electron2D;

internal sealed class RenderSystem : IDisposable
{
    private readonly RenderQueue _queue = new(initialCapacity: 2048);

    private nint _handle; // SDL_Renderer*
    private bool _ownsHandle;
    private const float RadToDeg = 180 / MathF.PI;
    private bool _viewPreparedThisFrame;
    private ViewCullRect _viewCull;


    internal nint Handle => _handle;
    
    internal VSyncMode EffectiveVSync { get; private set; } = VSyncMode.Disabled;
    internal int EffectiveVSyncInterval { get; private set; } = 1;

// Если VSync запрошен, но реально отключился (не поддержан), можно предложить cap.
    internal int SuggestedMaxFps { get; private set; } = 0;
    
    private bool _debugGridEnabled;
    private Color _debugGridBackground;
    private Color _debugGridLine;
    private Color _debugGridAxis;
    

    private SceneTree? _scene;

    private float _ppu;        // pixels per 1 world unit (computed from camera+viewport)
    private float _halfW;
    private float _halfH;

    private Vector2 _camPos;
    private float _camRot;     // world radians (CCW, Y-up)
    private float _camCos;     // cos(camRot)
    private float _camSin;     // sin(camRot)
    private bool  _camHasRot;


    // fallback если камеры нет (чтобы не делить на 0)
    private float _fallbackOrthoSize = 5f;


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

        // Прогрев очереди команд под конфиг, чтобы избежать роста в рантайме.
        _queue.Preallocate(cfg.RenderQueueCapacity);

        // APPLY VSYNC (SDL3)
        // ВАЖНО: cfg не мутируем — фиксируем фактическое состояние в RenderSystem.
        EffectiveVSync = cfg.VSync;
        EffectiveVSyncInterval = Math.Max(1, cfg.VSyncInterval);
        SuggestedMaxFps = 0;
        
        _debugGridEnabled = cfg.DebugGridEnabled;
        _debugGridBackground = cfg.DebugGridColor;
        _debugGridLine = cfg.DebugGridLineColor;
        // Оси чуть ярче обычной линии (без нового публичного параметра)
        _debugGridAxis = AddRgb(_debugGridLine, 0x22);

        var requestedVSync = cfg.VSync switch
        {
            VSyncMode.Disabled => 0,
            VSyncMode.Adaptive => -1,
            VSyncMode.Enabled  => Math.Max(1, cfg.VSyncInterval),
            _ => 0
        };

        if (!SDL.SetRenderVSync(_handle, requestedVSync))
        {
            var err = SDL.GetError();
            Console.WriteLine($"SDL_SetRenderVSync({requestedVSync}) failed: {err}");

            // Fallback chain: Adaptive -> 1 -> 0, Interval>1 -> 1 -> 0
            if (requestedVSync == -1)
            {
                if (SDL.SetRenderVSync(_handle, 1))
                {
                    EffectiveVSync = VSyncMode.Enabled;
                    EffectiveVSyncInterval = 1;
                }
                else
                {
                    SDL.SetRenderVSync(_handle, 0);
                    EffectiveVSync = VSyncMode.Disabled;
                    SuggestedMaxFps = 60;
                }
            }
            else if (requestedVSync > 1)
            {
                if (SDL.SetRenderVSync(_handle, 1))
                {
                    EffectiveVSync = VSyncMode.Enabled;
                    EffectiveVSyncInterval = 1;
                }
                else
                {
                    SDL.SetRenderVSync(_handle, 0);
                    EffectiveVSync = VSyncMode.Disabled;
                    SuggestedMaxFps = 60;
                }
            }
            else
            {
                // requestedVSync == 0 или 1, но SetRenderVSync всё равно не сработал
                EffectiveVSync = VSyncMode.Disabled;
                SuggestedMaxFps = 60;
            }
        }

        _fallbackOrthoSize = 5f;
    }

    public void BeginFrame(SceneTree scene)
    {
        _scene = scene;
        _viewPreparedThisFrame = false;
        
        // P0: очередь команд должна быть frame-local, иначе накапливаем draw calls по кадрам.
        _queue.Clear();

        var bg = _debugGridEnabled ? _debugGridBackground : scene.ClearColor;
        SDL.SetRenderDrawColor(_handle, bg.Red, bg.Green, bg.Blue, bg.Alpha);
        SDL.RenderClear(_handle);
    }


    public void Flush()
    {
        var span = _queue.CommandsMutable;

        // Если нет команд и сетка выключена — нечего делать.
        if (span.Length == 0 && !_debugGridEnabled)
            return;

        // Сортируем только если есть что сортировать.
        if (span.Length > 1 && _queue.NeedsSort)
            SpriteCommandSorter.Sort(span);

        PrepareView();

        // Фон: сетка рисуется ДО спрайтов.
        if (_debugGridEnabled)
            DrawDebugGrid1Unit();

        // Реальные draw-call’ы в SDL здесь.
        for (int i = 0; i < span.Length; i++)
            DrawSprite(in span[i]);
    }
    
    public void EndFrame()
    {
        Flush();
        SDL.RenderPresent(_handle);
    }

    private void DrawSprite(in SpriteCommand cmd)
    {
        var tex = cmd.Texture;
        if (!tex.IsValid) return;

        // World (0,0) в центре, Y вверх => SDL (0,0) слева-сверху, Y вниз
        var rel = cmd.PositionWorld - _camPos;

        float vx, vy;
        if (_camHasRot)
        {
            // rotate rel by -camRot: [ c  s; -s  c ]
            vx = rel.X * _camCos + rel.Y * _camSin;
            vy = -rel.X * _camSin + rel.Y * _camCos;
        }
        else
        {
            vx = rel.X;
            vy = rel.Y;
        }

        var pivotX = _halfW + vx * _ppu;
        var pivotY = _halfH - vy * _ppu;

        var wPx = cmd.SizeWorld.X * _ppu;
        var hPx = cmd.SizeWorld.Y * _ppu;

        if (!(wPx > 0f) || !(hPx > 0f)) return;

        // Origin задаётся из нижнего-левого (World Y-up).
        // Смещение до top-left по Y = (SizeY - OriginY).
        var originPxX = cmd.OriginWorld.X * _ppu;
        var originPxY = (cmd.SizeWorld.Y - cmd.OriginWorld.Y) * _ppu;

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
        var rot = _camHasRot ? (cmd.Rotation - _camRot) : cmd.Rotation;
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
            ToSdlFlip(cmd.FlipMode)
        );
        return;

        static SDL.FlipMode ToSdlFlip(FlipMode f)
        {
            var m = SDL.FlipMode.None;
            if ((f & FlipMode.Horizontal) != 0) m |= SDL.FlipMode.Horizontal;
            if ((f & FlipMode.Vertical) != 0)   m |= SDL.FlipMode.Vertical;
            return m;
        }
    }
    
    private void DrawDebugGrid1Unit()
    {
        // Половины в world-units (корректно даже при Camera.OrthoSize, т.к. _ppu пересчитан в PrepareView)
        var viewHalfW = _halfW / _ppu;
        var viewHalfH = _halfH / _ppu;

        // При вращении камеры берём bounding-square по диагонали, чтобы сетка точно закрыла экран
        var halfDiag = MathF.Sqrt(viewHalfW * viewHalfW + viewHalfH * viewHalfH);

        var xMin = _camPos.X - halfDiag;
        var xMax = _camPos.X + halfDiag;
        var yMin = _camPos.Y - halfDiag;
        var yMax = _camPos.Y + halfDiag;

        var xi0 = (int)MathF.Floor(xMin);
        var xi1 = (int)MathF.Ceiling(xMax);
        var yi0 = (int)MathF.Floor(yMin);
        var yi1 = (int)MathF.Ceiling(yMax);

        // 1) обычные линии (кроме осей)
        SetDrawColor(_debugGridLine);

        for (var x = xi0; x <= xi1; x++)
        {
            if (x == 0) continue;
            DrawWorldLine(x, yMin, x, yMax);
        }

        for (var y = yi0; y <= yi1; y++)
        {
            if (y == 0) continue;
            DrawWorldLine(xMin, y, xMax, y);
        }

        // 2) оси координат (чуть ярче)
        SetDrawColor(_debugGridAxis);

        if (0 >= xi0 && 0 <= xi1) DrawWorldLine(0f, yMin, 0f, yMax);
        if (0 >= yi0 && 0 <= yi1) DrawWorldLine(xMin, 0f, xMax, 0f);
    }

    private void DrawWorldLine(float x1, float y1, float x2, float y2)
    {
        WorldToScreen(x1, y1, out var sx1, out var sy1);
        WorldToScreen(x2, y2, out var sx2, out var sy2);

        SDL.RenderLine(_handle, sx1, sy1, sx2, sy2);
    }

    private void WorldToScreen(float wx, float wy, out float sx, out float sy)
    {
        var rx = wx - _camPos.X;
        var ry = wy - _camPos.Y;

        // _cos/_sin уже подготовлены в PrepareView() для rot = -_camRot
        var vx = rx * _camCos + ry * _camSin;
        var vy = -rx * _camSin + ry * _camCos;

        sx = _halfW + vx * _ppu;
        sy = _halfH - vy * _ppu;
    }

    private void SetDrawColor(in Color c)
        => SDL.SetRenderDrawColor(_handle, c.Red, c.Green, c.Blue, c.Alpha);

    private static Color AddRgb(in Color c, int delta)
    {
        var r = c.Red + delta; if (r > 255) r = 255;
        var g = c.Green + delta; if (g > 255) g = 255;
        var b = c.Blue + delta; if (b > 255) b = 255;

        // Color(uint) уже есть (раз используется new(0x000000FF)).
        return new Color((uint)((r << 24) | (g << 16) | (b << 8) | c.Alpha));
    }

    
    public void BuildRenderQueue(SceneTree sceneTree, ResourceSystem resources)
    {
        ArgumentNullException.ThrowIfNull(sceneTree);
        ArgumentNullException.ThrowIfNull(resources);

        _scene = sceneTree;

        EnsureViewPrepared();

        var renderers = sceneTree.SpriteRenderers;

        // опционально (но полезно): заранее гарантировать ёмкость под "все рендереры"
        // чтобы TryPush не триггерил Rent во время кадра при редком дефиците пула.
        _queue.EnsureCapacity(renderers.Length);

        for (var i = 0; i < renderers.Length; i++)
            renderers[i].PrepareRender(_queue, resources, in _viewCull);
    }
    
    private void PrepareView()
    {
        // Размер backbuffer (важно для fullscreen/HiDPI)
        SDL.GetRenderOutputSize(_handle, out var outW, out var outH);
        _halfW = outW * 0.5f;
        _halfH = outH * 0.5f;
        
        // Камера
        var cam = _scene?.EnsureCurrentCamera();

        if (cam is null)
        {
            _camPos = Vector2.Zero;
            _camRot = 0f;
            _camCos = 1f;
            _camSin = 0f;
            _camHasRot = false;
        }
        else
        {
            _camPos = cam.Transform.WorldPosition;

            _camRot = cam.Transform.WorldRotation;
            _camHasRot = _camRot != 0f;

            // Важно: для поворота world->view мы используем -camRot,
            // но можно хранить cos/sin(camRot) и применять формулу поворота на -a: [c s; -s c]
            _camCos = MathF.Cos(_camRot);
            _camSin = MathF.Sin(_camRot);
        }

        var orthoSize = cam?.OrthoSize ?? _fallbackOrthoSize;
        if (!(orthoSize > 0f)) orthoSize = 0.0001f;
        

        // Pixels per 1 world-unit по вертикали
        _ppu = outH / (2f * orthoSize);
    }
    
    private void EnsureViewPrepared()
    {
        if (_viewPreparedThisFrame)
            return;

        PrepareView();

        // half extents in world units
        var invPpu = _ppu > 0f ? (1f / _ppu) : 0f;
        var pad = 2f * invPpu; // ~2px safety to avoid edge popping
        var halfWWorld = _halfW * invPpu;
        var halfHWorld = _halfH * invPpu;

        // If camera rotated, conservative square that bounds the rotated view rect
        if (_camHasRot)
        {
            var r = MathF.Sqrt(halfWWorld * halfWWorld + halfHWorld * halfHWorld);
            halfWWorld = r;
            halfHWorld = r;
        }

        _viewCull = new ViewCullRect(
            _camPos.X - halfWWorld - pad,
            _camPos.Y - halfHWorld - pad,
            _camPos.X + halfWWorld + pad,
            _camPos.Y + halfHWorld + pad
        );

        _viewPreparedThisFrame = true;
    }


    public void Shutdown() => Dispose();
    
    public void Dispose()
    {
        _queue.Dispose();

        if (_ownsHandle && _handle != 0)
            SDL.DestroyRenderer(_handle);

        _ownsHandle = false;
        _handle = 0;
    }
}