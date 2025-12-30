using System.Buffers;
using System.Numerics;
using SDL3;

namespace Electron2D;

internal sealed class RenderSystem : IDisposable
{
    private readonly RenderQueue _queue = new(initialCapacity: 2048);

    private nint _handle; // SDL_Renderer*
    private bool _ownsHandle;
    private const float RadToDeg = 180 / MathF.PI;

    internal nint Handle => _handle;
    
    internal VSyncMode EffectiveVSync { get; private set; } = VSyncMode.Disabled;
    internal int EffectiveVSyncInterval { get; private set; } = 1;

// Если VSync запрошен, но реально отключился (не поддержан), можно предложить cap.
    internal int SuggestedMaxFps { get; private set; } = 0;

    
    public Color ClearColor { get; set; } = new(0x000000FF);
    
    private SceneTree? _scene;

    private float _ppu;        // pixels per 1 world unit (computed from camera+viewport)
    private float _halfW;
    private float _halfH;
    private Vector2 _camPos;

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

        // APPLY VSYNC (SDL3)
        // ВАЖНО: cfg не мутируем — фиксируем фактическое состояние в RenderSystem.
        EffectiveVSync = cfg.VSync;
        EffectiveVSyncInterval = Math.Max(1, cfg.VSyncInterval);
        SuggestedMaxFps = 0;

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

    public void BeginFrame()
    {
        _queue.Clear();

        SDL.SetRenderDrawColor(_handle, ClearColor.Red, ClearColor.Green, ClearColor.Blue, ClearColor.Alpha);
        SDL.RenderClear(_handle);
    }

    public void Flush()
    {
        var span = _queue.CommandsMutable;
        if (span.Length == 0)
            return;

        if (_queue.NeedsSort)
            SpriteCommandSorter.Sort(span);
        
        PrepareView();

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
        var pivotX = _halfW + (cmd.PositionWorld.X - _camPos.X) * _ppu;
        var pivotY = _halfH - (cmd.PositionWorld.Y - _camPos.Y) * _ppu;

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
        var angleDeg = -cmd.Rotation * RadToDeg;

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

    
    public void BuildRenderQueue(SceneTree sceneTree, ResourceSystem resources)
    {
        ArgumentNullException.ThrowIfNull(sceneTree);
        ArgumentNullException.ThrowIfNull(resources);

        // Предположение: корень дерева.
        var root = sceneTree.Root;
        _scene = sceneTree;

        var pool = ArrayPool<Node>.Shared;
        var stack = pool.Rent(128);
        var sp = 0;

        stack[sp++] = root;

        try
        {
            while (sp > 0)
            {
                var node = stack[--sp];
                stack[sp] = null!; // важно: не держим ссылки в ArrayPool

                // 1) Компоненты
                // Предположение: node.Components доступен и содержит IComponent.
                var comps = node.Components;
                for (var i = 0; i < comps.Length; i++)
                {
                    if (comps[i] is SpriteRenderer sr)
                        sr.PrepareRender(_queue, resources);
                }

                // 2) Дети
                var children = node.Children;
                for (var i = 0; i < children.Count; i++)
                {
                    if (sp == stack.Length)
                    {
                        // Grow stack (редко, прогревается).
                        var newArr = pool.Rent(stack.Length * 2);
                        Array.Copy(stack, 0, newArr, 0, sp);
                        pool.Return(stack, clearArray: false);
                        stack = newArr;
                    }

                    stack[sp++] = children[i];
                }
            }
        }
        finally
        {
            pool.Return(stack, clearArray: false);
        }
    }
    
    private void PrepareView()
    {
        // Размер backbuffer (важно для fullscreen/HiDPI)
        SDL.GetRenderOutputSize(_handle, out var outW, out var outH);
        _halfW = outW * 0.5f;
        _halfH = outH * 0.5f;

        // Камера
        var cam = _scene?.EnsureCurrentCamera();
        _camPos = cam is null ? Vector2.Zero : cam.Transform.WorldPosition;

        var orthoSize = cam?.OrthoSize ?? _fallbackOrthoSize;
        if (!(orthoSize > 0f)) orthoSize = 0.0001f;

        // Pixels per 1 world-unit по вертикали
        _ppu = outH / (2f * orthoSize);
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