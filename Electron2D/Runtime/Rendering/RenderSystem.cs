using SDL3;

namespace Electron2D;

internal sealed class RenderSystem
{
    private RenderQueue _queue = null!;
    private nint _renderer;
    private float _fallbackOrthoSize;
    private SceneTree? _scene;
    

    internal nint Handle => _renderer;

    public void Initialize(WindowSystem window, EngineConfig cfg)
    {
        _queue = new RenderQueue(cfg.RenderQueueCapacity);

        var win = window.Handle;
        if (win == 0)
            throw new InvalidOperationException("RenderSystem.Initialize: WindowHandle is not created.");

        _renderer = SDL.CreateRenderer(win, null);
        if (_renderer == 0)
            throw new InvalidOperationException($"SDL.CreateRenderer failed. {SDL.GetError()}");

        SDL.GetRenderOutputSize(_renderer, out _, out var outH0);

        // PixelPerUnit остаётся как default Sprite PPU.
        // Если камер нет — делаем “зум” таким, чтобы 1 unit == PixelPerUnit px на старте.
        var spritePpu = cfg.PixelPerUnit > 0f ? cfg.PixelPerUnit : 100f;
        _fallbackOrthoSize = outH0 / (2f * spritePpu);
        if (_fallbackOrthoSize <= 0f) _fallbackOrthoSize = 5f;

        ApplyVSync(cfg.Window);
    }

    public void Shutdown()
    {
        if (_renderer != 0)
            SDL.DestroyRenderer(_renderer);

        _renderer = 0;
    }

    public void BeginFrame()
    {
        _queue.Clear();

        // Черный фон (минимальный базовый кадр)
        SDL.SetRenderDrawColor(_renderer, 0, 0, 0, 255);
        SDL.RenderClear(_renderer);
    }

    public void BuildRenderQueue(SceneTree scene, ResourceSystem resources)
    {
        _scene = scene;
        BuildNode(scene.Root, resources);
    }

    public void EndFrame()
    {
        SubmitSprites();
        SDL.RenderPresent(_renderer);
    }

    private void SubmitSprites()
    {
        // Размер backbuffer (важно для fullscreen/HiDPI)
        SDL.GetRenderOutputSize(_renderer, out var outW, out var outH);
        var halfW = outW * 0.5f;
        var halfH = outH * 0.5f;

        var sprites = _queue.Sprites;
        var cam = _scene?.EnsureCurrentCamera();
        var orthoSize = cam?.OrthoSize ?? _fallbackOrthoSize;
        var ppuOnScreen = outH / (2f * orthoSize);
        
        for (var i = 0; i < sprites.Length; i++)
        {
            ref readonly var cmd = ref sprites[i];
            var tex = cmd.Texture;
            if (!tex.IsValid) continue;
            
            // World (0,0) в центре, Y вверх => SDL (0,0) слева-сверху, Y вниз
            var wPx = cmd.SizeWorld.X * ppuOnScreen;
            var hPx = cmd.SizeWorld.Y * ppuOnScreen;

            var x = halfW + cmd.PosWorld.X * ppuOnScreen - wPx * 0.5f;
            var y = halfH - cmd.PosWorld.Y * ppuOnScreen - hPx * 0.5f;

            var dst = new SDL.FRect { X = x, Y = y, W = wPx, H = hPx };

            // Цвет (если захотите) — можно добавить SetTextureColorMod/AlphaMod здесь.

            // Рендер (src = null => вся текстура)
            if (cmd.Rotation != 0f)
            {
                var angleDeg = cmd.Rotation * (180.0 / Math.PI);
                var center = new SDL.FPoint { X = dst.W * 0.5f, Y = dst.H * 0.5f };
                SDL.RenderTextureRotated(_renderer, tex.Handle, IntPtr.Zero, in dst, angleDeg, in center, SDL.FlipMode.None);
            }
            else
            {
                SDL.RenderTexture(_renderer, tex.Handle, IntPtr.Zero, in dst);
            }
        }
    }

    private void BuildNode(Node node, ResourceSystem resources)
    {
        var comps = node.InternalComponents;
        for (var i = 0; i < comps.Length; i++)
        {
            if (comps[i] is SpriteRenderer sr)
                sr.PrepareRender(_queue, resources);
        }

        var childCount = node.ChildCount;
        for (var i = 0; i < childCount; i++)
            BuildNode(node.GetChildAt(i), resources);
    }

    private void ApplyVSync(WindowConfig cfg)
    {
        var desired = cfg.VSync switch
        {
            VSyncMode.Disabled => 0,
            VSyncMode.Enabled => Math.Max(1, cfg.VSyncInterval),
            VSyncMode.Adaptive => -1,
            _ => 0
        };

        if (!SDL.SetRenderVSync(_renderer, desired))
            SDL.SetRenderVSync(_renderer, 0);
    }
}