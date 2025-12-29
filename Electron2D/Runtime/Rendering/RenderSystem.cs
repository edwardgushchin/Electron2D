using System.Numerics;
using SDL3;

namespace Electron2D;

internal sealed class RenderSystem
{
    private RenderQueue _queue = null!;
    private nint _renderer; // SDL_Renderer*
    private float _ppu;

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

        _ppu = cfg.PixelPerUnit;
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
        for (var i = 0; i < sprites.Length; i++)
        {
            ref readonly var cmd = ref sprites[i];
            var tex = cmd.Texture;
            if (tex is null || !tex.IsValid) continue;

            // World (0,0) в центре, Y вверх => SDL (0,0) слева-сверху, Y вниз
            var wPx = cmd.SizeWorld.X * _ppu;
            var hPx = cmd.SizeWorld.Y * _ppu;

            var x = halfW + cmd.PosWorld.X * _ppu - wPx * 0.5f;
            var y = halfH - cmd.PosWorld.Y * _ppu - hPx * 0.5f;

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

        var childCount = node.GetChildCount();
        for (var i = 0; i < childCount; i++)
            BuildNode(node.GetChild(i), resources);
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