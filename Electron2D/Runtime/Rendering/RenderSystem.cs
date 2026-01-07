using System.Numerics;
using System.Runtime.CompilerServices;
using SDL3;

namespace Electron2D;

#region RenderSystem

/// <summary>
/// Система рендера (SDL_Renderer): собирает команды, при необходимости сортирует, батчит спрайты и выводит кадр.
/// </summary>
/// <remarks>
/// Потокобезопасность не гарантируется. Предполагается использование из главного потока (ограничение SDL).
/// </remarks>
internal sealed class RenderSystem : IDisposable
{
    #region Constants

    // Безопасная нижняя граница для OrthoSize/PPU, чтобы избежать деления на ноль и неустойчивых состояний.
    private const float MinPositiveFloat = 0.0001f;

    private const int DefaultRenderQueueCapacity = 2048;

    // Таблица фиксированного размера с открытой адресацией для счётчика "уникальные текстуры за кадр".
    // Размер должен быть степенью двойки (для маскирования).
    private const int UniqueTextureTableSize = 1024;
    private const int UniqueTextureTableMask = UniqueTextureTableSize - 1;

    // Мультипликативная хэш-константа Кнута (64-bit).
    private const ulong UniqueTextureHashMul = 11400714819323198485UL;

    #endregion

    #region Instance fields

    private readonly RenderQueue _queue = new(initialCapacity: DefaultRenderQueueCapacity);

    private nint _rendererHandle; // SDL_Renderer*
    private bool _ownsRendererHandle;

    private nint _lastTextureThisFrame;

    private readonly nint[] _uniqueTextureTable = new nint[UniqueTextureTableSize]; // открытая адресация
    private int _uniqueTextureCount;

    private SDL.Vertex[] _spriteBatchVertices = Array.Empty<SDL.Vertex>();
    private int[] _spriteBatchIndices = Array.Empty<int>();
    private int _spriteBatchVertexCount;
    private int _spriteBatchIndexCount;
    private int _spriteBatchSpriteCount;
    private nint _spriteBatchTexture; // SDL_Texture*

    private SDL.Vertex[] _gridGeomVertices = Array.Empty<SDL.Vertex>();
    private int[] _gridGeomIndices = Array.Empty<int>();

    private SceneTree? _scene;

    private bool _viewValidThisFrame;
    private ViewState _view;

    private float _fallbackOrthoSize = 5f;

    private bool _debugGridEnabled;
    private Color _debugGridBackground;
    private Color _debugGridLine;
    private Color _debugGridAxis;
    
    private FilterMode _defaultTextureFilter = FilterMode.Linear;
    private SDL.ScaleMode _spriteBatchScaleMode = SDL.ScaleMode.Linear;
    
    private bool _logicalPresentationEnabled;
    private int _logicalW;
    private int _logicalH;
    
    private const int SceneTargetGutterPx = 2; // padding для subpixel composite (используем только если logical presentation выключен)

    private nint _sceneTarget;     // SDL_Texture*
    private int _sceneTargetW;
    private int _sceneTargetH;

    private bool _sceneTargetThisFrame;
    private float _rtOffsetX;      // смещение всех screen-space координат при рендере в target (gutter)
    private float _rtOffsetY;

    #endregion

    #region Properties

    internal nint Handle => _rendererHandle;

    internal VSyncMode EffectiveVSync { get; private set; } = VSyncMode.Disabled;
    internal int EffectiveVSyncInterval { get; private set; } = 1;

    /// <summary>
    /// Если VSync был запрошен, но включить его не удалось, можно предложить внешний FPS cap (0 = не требуется).
    /// </summary>
    internal int SuggestedMaxFps { get; private set; }

    public RenderQueue Queue => _queue;

    #endregion

    #region Public API

    /// <summary>
    /// Инициализирует RenderSystem, создавая SDL_Renderer* для переданного SDL_Window*.
    /// </summary>
    /// <remarks>Только главный поток (SDL).</remarks>
    public void Initialize(nint windowHandle, EngineConfig config)
    {
        if (windowHandle == 0)
            throw new ArgumentOutOfRangeException(nameof(windowHandle));

        if (_rendererHandle != 0)
            throw new InvalidOperationException("RenderSystem.Initialize: already initialized.");

        var renderer = SDL.CreateRenderer(windowHandle, name: null);
        if (renderer == 0)
            throw new InvalidOperationException($"SDL.CreateRenderer failed. {SDL.GetError()}");

        _rendererHandle = renderer;
        _ownsRendererHandle = true;

        _queue.Preallocate(config.RenderQueueCapacity);
        
        _defaultTextureFilter = config.TextureFilter;
        if (_defaultTextureFilter == FilterMode.Inherit)
            _defaultTextureFilter = FilterMode.Linear;

        ConfigureDebugGrid(config);
        ApplyVSync(config);
        ApplyPresentation(config); // новый метод

        _fallbackOrthoSize = 5f;
    }

    public void BeginFrame(SceneTree scene)
    {
        ThrowIfNotInitialized();
        ArgumentNullException.ThrowIfNull(scene);

        using var _ = Profiler.Sample(ProfilerSampleId.RenderBeginFrame);

        _scene = scene;
        _viewValidThisFrame = false;

        // Очередь команд — строго на кадр, без накопления между кадрами.
        _queue.Clear();

        _lastTextureThisFrame = 0;
        Array.Clear(_uniqueTextureTable, 0, _uniqueTextureTable.Length);
        _uniqueTextureCount = 0;

        // --- Patch B: решаем, будем ли рендерить в offscreen target ---
        // Важно: EnsureView() зовём ДО Clear, чтобы понимать режим камеры и размеры.
        ref readonly var view = ref EnsureView();

        _sceneTargetThisFrame = ShouldUseSceneTarget(in view);

        var bg = _debugGridEnabled ? _debugGridBackground : scene.ClearColor;

        if (_sceneTargetThisFrame)
        {
            // Размер виртуального экрана (render-space).
            
            var outW = SnapPixel(view.HalfW * 2f, PixelSnapMode.Round);
            var outH = SnapPixel(view.HalfH * 2f, PixelSnapMode.Round);

            // Если logical presentation включён — не используем gutter (иначе рискуем попасть в неожиданный scale/viewport в SDL).
            // В этом режиме сглаживание будет работать, но по краям возможен лёгкий border при сильных subpixel.
            var gutter = _logicalPresentationEnabled ? 0 : SceneTargetGutterPx;

            EnsureSceneTarget((int)outW + gutter * 2, (int)outH + gutter * 2);

            _rtOffsetX = gutter;
            _rtOffsetY = gutter;

            if (!SDL.SetRenderTarget(_rendererHandle, _sceneTarget))
                throw new InvalidOperationException($"SDL.SetRenderTarget failed. {SDL.GetError()}");

            SDL.SetRenderDrawColor(_rendererHandle, bg.Red, bg.Green, bg.Blue, bg.Alpha);
            SDL.RenderClear(_rendererHandle);
            Profiler.AddCounter(ProfilerCounterId.RenderClears);
            return;
        }

        // Обычный путь: рендер в backbuffer
        _rtOffsetX = 0f;
        _rtOffsetY = 0f;

        if (!SDL.SetRenderTarget(_rendererHandle, 0))
            throw new InvalidOperationException($"SDL.SetRenderTarget(reset) failed. {SDL.GetError()}");

        SDL.SetRenderDrawColor(_rendererHandle, bg.Red, bg.Green, bg.Blue, bg.Alpha);
        SDL.RenderClear(_rendererHandle);
        Profiler.AddCounter(ProfilerCounterId.RenderClears);
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

        // Опционально: заранее обеспечить ёмкость, чтобы исключить рост массива в течение кадра.
        //_queue.EnsureCapacity(renderers.Length);

        var uiCull = new ViewCullRect(0f, 0f, view.HalfW * 2f, view.HalfH * 2f);

        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer.Space == RenderSpace.Screen)
                renderer.PrepareRender(_queue, resources, in uiCull);
            else
                renderer.PrepareRender(_queue, resources, in view.Cull);
        }
    }

    public void EndFrame()
    {
        ThrowIfNotInitialized();

        Flush();

        // Финализация счётчиков кадра.
        Profiler.SetCounter(ProfilerCounterId.RenderUniqueTextures, _uniqueTextureCount);

        using (Profiler.Sample(ProfilerSampleId.RenderPresent))
        {
            // Нужен один раз — и для композита, и для crosshair.
            ref readonly var view = ref EnsureView();

            if (_sceneTargetThisFrame)
            {
                // Возвращаемся в backbuffer
                if (!SDL.SetRenderTarget(_rendererHandle, 0))
                    throw new InvalidOperationException($"SDL.SetRenderTarget(reset) failed. {SDL.GetError()}");

                // Очищаем backbuffer
                var bg = _debugGridEnabled ? _debugGridBackground : _scene!.ClearColor;
                SDL.SetRenderDrawColor(_rendererHandle, bg.Red, bg.Green, bg.Blue, bg.Alpha);
                SDL.RenderClear(_rendererHandle);

                var outW = (int)MathF.Round(view.HalfW * 2f);
                var outH = (int)MathF.Round(view.HalfH * 2f);

                var camPxX = view.CamPos.X * view.Ppu;
                var camPxY = view.CamPos.Y * view.Ppu;

                var snappedX = view.CamPosPxSnapped.X;
                var snappedY = view.CamPosPxSnapped.Y;

                var fracX = camPxX - snappedX;
                var fracYInv = snappedY - camPxY;

                var gutter = (_logicalPresentationEnabled ? 0 : SceneTargetGutterPx);

                SDL.SetTextureScaleMode(_sceneTarget, SDL.ScaleMode.PixelArt);

                if (gutter > 0)
                {
                    var src = new SDL.FRect
                    {
                        X = gutter + fracX,
                        Y = gutter + fracYInv,
                        W = outW,
                        H = outH
                    };

                    var dst = new SDL.FRect
                    {
                        X = 0f,
                        Y = 0f,
                        W = outW,
                        H = outH
                    };

                    SDL.RenderTexture(_rendererHandle, _sceneTarget, src, dst);
                }
                else
                {
                    var dst = new SDL.FRect
                    {
                        X = -fracX,
                        Y = + (camPxY - snappedY),
                        W = outW,
                        H = outH
                    };

                    SDL.RenderTexture(_rendererHandle, _sceneTarget, IntPtr.Zero, dst);
                }
            }

            // Рисуем линии поверх ВСЕГО (и поверх композита тоже)
            //DrawCenterCrosshair(in view);

            SDL.RenderPresent(_rendererHandle);
            Profiler.AddCounter(ProfilerCounterId.RenderPresents);
        }

    }


    public void Shutdown() => Dispose();

    public void Dispose()
    {
        _queue.Dispose();

        DestroySceneTarget();

        if (_ownsRendererHandle && _rendererHandle != 0)
            SDL.DestroyRenderer(_rendererHandle);

        _ownsRendererHandle = false;
        _rendererHandle = 0;

        _scene = null;
        _viewValidThisFrame = false;
        _view = default;
    }

    #endregion

    #region Render execution

    private void Flush()
    {
        ThrowIfNotInitialized();

        using var _ = Profiler.Sample(ProfilerSampleId.RenderFlush);

        var commands = _queue.CommandsMutable;

        // Если команд нет и сетка выключена — делать нечего.
        if (commands.Length == 0 && !_debugGridEnabled)
            return;

        if (commands.Length > 1 && _queue.NeedsSort)
        {
            Profiler.AddCounter(ProfilerCounterId.RenderSortTriggered);
            Profiler.SetCounter(ProfilerCounterId.RenderSortCommands, commands.Length);

            using (Profiler.Sample(ProfilerSampleId.RenderSort))
                SpriteCommandSorter.Sort(commands);
        }

        ref readonly var worldView = ref EnsureView();
        var uiView = BuildUiView(in worldView);

        if (_debugGridEnabled)
            DrawDebugGrid(in worldView);

        EnsureSpriteBatchCapacity(commands.Length);

        DrawSpriteCommands(commands, in worldView, RenderSpace.World);
        DrawSpriteCommands(commands, in uiView, RenderSpace.Screen);


        // Фон: сетка рисуется ДО спрайтов.
        if (_debugGridEnabled)
            DrawDebugGrid(in worldView);

        EnsureSpriteBatchCapacity(commands.Length);

        _spriteBatchTexture = 0;
        _spriteBatchScaleMode = SDL.ScaleMode.Linear;
        _spriteBatchVertexCount = 0;
        _spriteBatchIndexCount = 0;
        _spriteBatchSpriteCount = 0;

        for (var i = 0; i < commands.Length; i++)
        {
            ref readonly var cmd = ref commands[i];

            var tex = cmd.Texture;
            if (!tex.IsValid)
                continue;

            var textureHandle = tex.Handle;

            // 1) Разрешаем режим фильтрации для команды
            var resolvedFilter = ResolveFilter(cmd.FilterMode, tex.FilterMode);

            // 2) Конвертим в реальный SDL scale mode (Pixelart -> Nearest)
            var sdlScaleMode = ToSDLScaleMode(resolvedFilter);

            // 3) Батч брейк: либо текстура сменилась, либо scale mode сменилась
            if (_spriteBatchTexture != 0 &&
                (textureHandle != _spriteBatchTexture || sdlScaleMode != _spriteBatchScaleMode))
            {
                FlushSpriteBatch();
            }

            // 4) Начинаем новый батч
            if (_spriteBatchTexture == 0)
            {
                _spriteBatchTexture = textureHandle;
                _spriteBatchScaleMode = sdlScaleMode;

                // Уникальные текстуры за кадр (считаем по handle, не по режиму)
                TrackUniqueTexture(textureHandle);

                // Эвристика "bind" по handle (оставляем твою семантику)
                if (textureHandle != _lastTextureThisFrame)
                {
                    _lastTextureThisFrame = textureHandle;
                    Profiler.AddCounter(ProfilerCounterId.RenderTextureBinds);
                }

                // Важно: scale mode задаётся на SDL_Texture*, т.е. это состояние текстуры.
                SDL.SetTextureScaleMode(_spriteBatchTexture, _spriteBatchScaleMode);
            }

            // Pixel snap (только для Nearest/Pixelart и только без rotation):
            // избавляет от sub-pixel jitter при движении.
            var snapScaleModeOk =
                sdlScaleMode is SDL.ScaleMode.Nearest or SDL.ScaleMode.PixelArt;

            var snapToPixel =
                worldView.PixelSnap &&
                snapScaleModeOk &&
                MathF.Abs(cmd.Rotation) < 1e-6f;

            EmitSpriteQuad(in cmd, in worldView, snapToPixel);
        }

        FlushSpriteBatch();
    }

    private void FlushSpriteBatch()
    {
        if (_spriteBatchSpriteCount == 0)
        {
            _spriteBatchTexture = 0;
            _spriteBatchVertexCount = 0;
            _spriteBatchIndexCount = 0;
            return;
        }

        // Счётчики: 1 drawcall на батч.
        Profiler.AddCounter(ProfilerCounterId.RenderDrawCalls);
        Profiler.AddCounter(ProfilerCounterId.RenderSprites, _spriteBatchSpriteCount);

        // ПРИМЕЧАНИЕ: Это добавляет texture-binds на батч, дополнительно к эвристике в Flush().
        // Оставлено без изменений, чтобы сохранить текущую семантику профилирования.
        //Profiler.AddCounter(ProfilerCounterId.RenderTextureBinds);

        SDL.RenderGeometry(
            _rendererHandle,
            _spriteBatchTexture,
            _spriteBatchVertices,
            _spriteBatchVertexCount,
            _spriteBatchIndices,
            _spriteBatchIndexCount);

        _spriteBatchTexture = 0;
        _spriteBatchVertexCount = 0;
        _spriteBatchIndexCount = 0;
        _spriteBatchSpriteCount = 0;
    }

    private void EmitSpriteQuad(in SpriteCommand cmd, in ViewState view, bool snapToPixel)
    {
        var tex = cmd.Texture;

        var sizeWorld = cmd.SizeWorld;
        if (sizeWorld is not { X: > 0f, Y: > 0f }) return;

        var wPx = sizeWorld.X * view.Ppu;
        var hPx = sizeWorld.Y * view.Ppu;
        if (!(wPx > 0f) || !(hPx > 0f)) return;

        var originPxX = cmd.OriginWorld.X * view.Ppu;
        var originPxY = (sizeWorld.Y - cmd.OriginWorld.Y) * view.Ppu;

        float pivotX, pivotY;

        if (cmd.Space == RenderSpace.Screen)
        {
            // UI: cmd.PositionWorld трактуем как screen coords (render-space pixels)
            pivotX = cmd.PositionWorld.X;
            pivotY = cmd.PositionWorld.Y;
        }
        else if (view is { PixelSnap: true, HasRot: false })
        {
            // ваш текущий snapped camera путь
        }
        else
        {

            // --- Patch B: стабильный pixel-space путь для snapped камеры без rotation ---
            if (view is { PixelSnap: true, HasRot: false })
            {
                var posPxX = cmd.PositionWorld.X * view.Ppu;
                var posPxY = cmd.PositionWorld.Y * view.Ppu;

                var relPxX = posPxX - view.CamPosPxSnapped.X;
                var relPxY = posPxY - view.CamPosPxSnapped.Y;

                pivotX = view.HalfW + relPxX;
                pivotY = view.HalfH - relPxY;
            }
            else
            {
                // World -> view (camera space).
                var rel = cmd.PositionWorld - view.CamPos;

                float vx, vy;
                if (view.HasRot)
                {
                    vx = rel.X * view.Cos + rel.Y * view.Sin;
                    vy = -rel.X * view.Sin + rel.Y * view.Cos;
                }
                else
                {
                    vx = rel.X;
                    vy = rel.Y;
                }

                // View -> screen (pivot).
                pivotX = view.HalfW + vx * view.Ppu;
                pivotY = view.HalfH - vy * view.Ppu;
            }

            if (snapToPixel)
            {
                pivotX = SnapPixel(pivotX, view.SnapMode);
                pivotY = SnapPixel(pivotY, view.SnapMode);

                originPxX = SnapPixel(originPxX, view.SnapMode);
                originPxY = SnapPixel(originPxY, view.SnapMode);

                wPx = SnapPixel(wPx, view.SnapMode);
                hPx = SnapPixel(hPx, view.SnapMode);
            }

            // --- Patch B: оффсет при рендере в render target (gutter) ---
            pivotX += _rtOffsetX;
            pivotY += _rtOffsetY;

            // Локальные углы вокруг pivot (screen space, y-down).
            var x0 = -originPxX;
            var y0 = -originPxY; // top-left
            var x1 = wPx - originPxX;
            var y1 = -originPxY; // top-right
            var x2 = wPx - originPxX;
            var y2 = hPx - originPxY; // bottom-right
            var x3 = -originPxX;
            var y3 = hPx - originPxY; // bottom-left

            var rot = view.HasRot ? (cmd.Rotation - view.CamRot) : cmd.Rotation;
            var angle = -rot;

            float p0x, p0y, p1x, p1y, p2x, p2y, p3x, p3y;

            if (angle != 0f)
            {
                var c = MathF.Cos(angle);
                var s = MathF.Sin(angle);

                var rx0 = x0 * c - y0 * s;
                var ry0 = x0 * s + y0 * c;
                var rx1 = x1 * c - y1 * s;
                var ry1 = x1 * s + y1 * c;
                var rx2 = x2 * c - y2 * s;
                var ry2 = x2 * s + y2 * c;
                var rx3 = x3 * c - y3 * s;
                var ry3 = x3 * s + y3 * c;

                p0x = pivotX + rx0;
                p0y = pivotY + ry0;
                p1x = pivotX + rx1;
                p1y = pivotY + ry1;
                p2x = pivotX + rx2;
                p2y = pivotY + ry2;
                p3x = pivotX + rx3;
                p3y = pivotY + ry3;
            }
            else
            {
                p0x = pivotX + x0;
                p0y = pivotY + y0;
                p1x = pivotX + x1;
                p1y = pivotY + y1;
                p2x = pivotX + x2;
                p2y = pivotY + y2;
                p3x = pivotX + x3;
                p3y = pivotY + y3;
            }

            var invW = 1f / tex.Width;
            var invH = 1f / tex.Height;

            var u0 = cmd.SrcRect.X * invW;
            var v0 = cmd.SrcRect.Y * invH;
            var u1 = (cmd.SrcRect.X + cmd.SrcRect.Width) * invW;
            var v1 = (cmd.SrcRect.Y + cmd.SrcRect.Height) * invH;

            if ((cmd.FlipMode & FlipMode.Horizontal) != 0) (u0, u1) = (u1, u0);
            if ((cmd.FlipMode & FlipMode.Vertical) != 0) (v0, v1) = (v1, v0);

            var col = new SDL.FColor(
                cmd.Color.Red / 255f,
                cmd.Color.Green / 255f,
                cmd.Color.Blue / 255f,
                cmd.Color.Alpha / 255f);

            var vBase = _spriteBatchVertexCount;
            var iBase = _spriteBatchIndexCount;

            _spriteBatchVertices[vBase + 0].Position = new SDL.FPoint { X = p0x, Y = p0y };
            _spriteBatchVertices[vBase + 1].Position = new SDL.FPoint { X = p1x, Y = p1y };
            _spriteBatchVertices[vBase + 2].Position = new SDL.FPoint { X = p2x, Y = p2y };
            _spriteBatchVertices[vBase + 3].Position = new SDL.FPoint { X = p3x, Y = p3y };

            _spriteBatchVertices[vBase + 0].TexCoord = new SDL.FPoint { X = u0, Y = v0 };
            _spriteBatchVertices[vBase + 1].TexCoord = new SDL.FPoint { X = u1, Y = v0 };
            _spriteBatchVertices[vBase + 2].TexCoord = new SDL.FPoint { X = u1, Y = v1 };
            _spriteBatchVertices[vBase + 3].TexCoord = new SDL.FPoint { X = u0, Y = v1 };

            _spriteBatchVertices[vBase + 0].Color = col;
            _spriteBatchVertices[vBase + 1].Color = col;
            _spriteBatchVertices[vBase + 2].Color = col;
            _spriteBatchVertices[vBase + 3].Color = col;

            _spriteBatchIndices[iBase + 0] = vBase + 0;
            _spriteBatchIndices[iBase + 1] = vBase + 1;
            _spriteBatchIndices[iBase + 2] = vBase + 2;
            _spriteBatchIndices[iBase + 3] = vBase + 2;
            _spriteBatchIndices[iBase + 4] = vBase + 3;
            _spriteBatchIndices[iBase + 5] = vBase + 0;

            _spriteBatchVertexCount += 4;
            _spriteBatchIndexCount += 6;
            _spriteBatchSpriteCount++;
        }
    }

    
    
    private void ApplyPresentation(EngineConfig config)
    {
        var p = config.Presentation;

        var mode = p.Mode switch
        {
            PresentationMode.Disabled     => SDL.RendererLogicalPresentation.Disabled,
            PresentationMode.Stretch      => SDL.RendererLogicalPresentation.Stretch,
            PresentationMode.Letterbox    => SDL.RendererLogicalPresentation.Letterbox,
            PresentationMode.Overscan     => SDL.RendererLogicalPresentation.Overscan,
            PresentationMode.IntegerScale => SDL.RendererLogicalPresentation.IntegerScale,
            _ => SDL.RendererLogicalPresentation.Disabled
        };

        if (mode == SDL.RendererLogicalPresentation.Disabled)
        {
            // Важно: корректно “сбрасываем” logical presentation.
            SDL.SetRenderLogicalPresentation(_rendererHandle, 0, 0, SDL.RendererLogicalPresentation.Disabled);

            _logicalPresentationEnabled = false;
            _logicalW = 0;
            _logicalH = 0;
            return;
        }

        var vw = p.VirtualWidth;
        var vh = p.VirtualHeight;

        // Если virtual size не задан — берём текущий размер вывода на момент инициализации.
        if (vw <= 0 || vh <= 0)
        {
            SDL.GetRenderOutputSize(_rendererHandle, out var outW, out var outH);
            if (outW <= 0) outW = 1;
            if (outH <= 0) outH = 1;

            vw = outW;
            vh = outH;
        }

        SDL.SetRenderLogicalPresentation(_rendererHandle, vw, vh, mode);

        _logicalPresentationEnabled = true;
        _logicalW = vw;
        _logicalH = vh;
    }

    #endregion

    #region View

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
        int outW, outH;

        if (_logicalPresentationEnabled && _logicalW > 0 && _logicalH > 0)
        {
            // В logical presentation все координаты рендера должны быть в virtual space.
            outW = _logicalW;
            outH = _logicalH;
        }
        else
        {
            SDL.GetRenderOutputSize(_rendererHandle, out outW, out outH);

            // Safety (например, свёрнутое окно / транзит ресайза).
            if (outW <= 0) outW = 1;
            if (outH <= 0) outH = 1;
        }

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

        // Pixel-perfect override (фиксируем PPU и, опционально, запрещаем rotation)
        // + опциональный render-time pixel snapping (для pixel-art).
        // стало:
        float ppu;
        var pixelSnap = false;
        var snapMode = PixelSnapMode.Round;
        var camPosPxSnapped = Vector2.Zero;

        if (cam is PixelPerfectCamera ppcam)
        {
            if (ppcam.EnforceNoRotation)
                camRot = 0f;

            ppu = MathF.Max(1f, ppcam.PixelsPerUnit);

            // обновляем “что получилось”
            ppcam.UpdateEffectiveOrthoSize(outH);

            pixelSnap = ppcam.SnapPosition;
            snapMode = ppcam.SnapMode;

            // ВАЖНО: camPos НЕ снапаем в world-space.
        }
        else
        {
            var orthoSize = cam?.OrthoSize ?? _fallbackOrthoSize;
            if (!(orthoSize > 0f))
                orthoSize = MinPositiveFloat;

            ppu = outH / (2f * orthoSize);
            if (!(ppu > 0f))
                ppu = MinPositiveFloat;
        }

        var hasRot = camRot != 0f;

        // При вращении камеры снап вершин к целым пикселям даёт артефакты,
        // поэтому отключаем его жёстко.
        if (hasRot)
            pixelSnap = false;

        // Сохраняем cos/sin(camRot), применяем поворот на -camRot матрицей [c s; -s c].
        var c = hasRot ? MathF.Cos(camRot) : 1f;
        var s = hasRot ? MathF.Sin(camRot) : 0f;

        // Cull rect (world units).
        var invPpu = 1f / ppu;
        var pad = 2f * invPpu; // ~2px safety, чтобы избежать popping на границе

        var halfWWorld = halfW * invPpu;
        var halfHWorld = halfH * invPpu;

        if (hasRot)
        {
            var r = MathF.Sqrt(halfWWorld * halfWWorld + halfHWorld * halfHWorld);
            halfWWorld = r;
            halfHWorld = r;
        }
        
        if (pixelSnap)
        {
            camPosPxSnapped = new Vector2(
                SnapPixel(camPos.X * ppu, snapMode),
                SnapPixel(camPos.Y * ppu, snapMode));
        }

        var cull = new ViewCullRect(
            camPos.X - halfWWorld - pad,
            camPos.Y - halfHWorld - pad,
            camPos.X + halfWWorld + pad,
            camPos.Y + halfHWorld + pad);

        return new ViewState(
            halfW: halfW,
            halfH: halfH,
            ppu: ppu,
            camPos: camPos,
            camRot: camRot,
            cos: c,
            sin: s,
            hasRot: hasRot,
            pixelSnap: pixelSnap,
            snapMode: snapMode,
            camPosPxSnapped: camPosPxSnapped,
            cull: cull);
    }
    
    private static ViewState BuildUiView(in ViewState worldView)
    {
        var w = worldView.HalfW * 2f;
        var h = worldView.HalfH * 2f;
        var cull = new ViewCullRect(0f, 0f, w, h);

        return new ViewState(
            halfW: worldView.HalfW,
            halfH: worldView.HalfH,
            ppu: 1f,                 // UI: пиксели
            camPos: default,
            camRot: 0f,
            cos: 1f,
            sin: 0f,
            hasRot: false,
            pixelSnap: true,         // snapping включён, но реально сработает только для Nearest/Pixelart и rot≈0
            snapMode: worldView.SnapMode,
            camPosPxSnapped: default,
            cull: cull);
    }

    private void DrawSpriteCommands(Span<SpriteCommand> commands, in ViewState view, RenderSpace pass)
    {
        _spriteBatchTexture = 0;
        _spriteBatchScaleMode = SDL.ScaleMode.Linear;
        _spriteBatchVertexCount = 0;
        _spriteBatchIndexCount = 0;
        _spriteBatchSpriteCount = 0;

        for (var i = 0; i < commands.Length; i++)
        {
            ref readonly var cmd = ref commands[i];
            if (cmd.Space != pass)
                continue;

            // дальше — ваш текущий код батчинга/фильтра/EmitSpriteQuad(...)
            // (без изменений)
        }

        FlushSpriteBatch();
    }


    private void EnsureSpriteBatchCapacity(int spritesNeeded)
    {
        var verticesNeeded = spritesNeeded * 4;
        var indicesNeeded = spritesNeeded * 6;

        if (_spriteBatchVertices.Length < verticesNeeded)
            _spriteBatchVertices = new SDL.Vertex[GrowCapacity(_spriteBatchVertices.Length, verticesNeeded)];

        if (_spriteBatchIndices.Length < indicesNeeded)
            _spriteBatchIndices = new int[GrowCapacity(_spriteBatchIndices.Length, indicesNeeded)];
    }

    #endregion

    #region Drawing (debug / legacy)

    private void DrawDebugGrid(in ViewState view)
    {
        // Полуразмеры в world-units.
        var viewHalfW = view.HalfW / view.Ppu;
        var viewHalfH = view.HalfH / view.Ppu;

        // Для повёрнутой камеры берём ограничивающий квадрат по диагонали, чтобы покрыть экран.
        var halfDiag = MathF.Sqrt(viewHalfW * viewHalfW + viewHalfH * viewHalfH);

        var xMin = view.CamPos.X - halfDiag;
        var xMax = view.CamPos.X + halfDiag;
        var yMin = view.CamPos.Y - halfDiag;
        var yMax = view.CamPos.Y + halfDiag;

        var xi0 = (int)MathF.Floor(xMin);
        var xi1 = (int)MathF.Ceiling(xMax);
        var yi0 = (int)MathF.Floor(yMin);
        var yi1 = (int)MathF.Ceiling(yMax);

        // --------------------------------------------------------------------
        // 1) Обычные линии (кроме осей) — RenderGeometry
        // --------------------------------------------------------------------

        var vLines = xi1 - xi0 + 1;
        if (0 >= xi0 && 0 <= xi1) vLines--; // исключаем ось X=0

        var hLines = yi1 - yi0 + 1;
        if (0 >= yi0 && 0 <= yi1) hLines--; // исключаем ось Y=0

        var totalLines = Math.Max(0, vLines) + Math.Max(0, hLines);

        if (totalLines > 0)
        {
            var maxVertices = totalLines * 4;
            var maxIndices = totalLines * 6;

            EnsureGridGeometryCapacity(maxVertices, maxIndices);

            var c = new SDL.FColor(
                _debugGridLine.Red / 255f,
                _debugGridLine.Green / 255f,
                _debugGridLine.Blue / 255f,
                _debugGridLine.Alpha / 255f);

            var v = 0;
            var i = 0;
            var emittedLines = 0;

            var state = view;

            void EmitWorldLineQuad(float wx1, float wy1, float wx2, float wy2)
            {
                WorldToScreen(wx1, wy1, out var sx1, out var sy1, in state);
                WorldToScreen(wx2, wy2, out var sx2, out var sy2, in state);

                // Patch B: gutter-offset при рендере в target
                sx1 += _rtOffsetX; sy1 += _rtOffsetY;
                sx2 += _rtOffsetX; sy2 += _rtOffsetY;

                // В pixel-snap режиме сетка должна быть строго пиксельная.
                if (state is { PixelSnap: true, HasRot: false })
                {
                    sx1 = SnapPixel(sx1, state.SnapMode);
                    sy1 = SnapPixel(sy1, state.SnapMode);
                    sx2 = SnapPixel(sx2, state.SnapMode);
                    sy2 = SnapPixel(sy2, state.SnapMode);

                    // Вертикальная линия
                    if (MathF.Abs(sx2 - sx1) < 1e-4f)
                    {
                        var x = sx1;
                        var yTop = MathF.Min(sy1, sy2);
                        var yBot = MathF.Max(sy1, sy2);

                        _gridGeomVertices[v + 0].Position = new SDL.FPoint { X = x,     Y = yTop };
                        _gridGeomVertices[v + 1].Position = new SDL.FPoint { X = x + 1, Y = yTop };
                        _gridGeomVertices[v + 2].Position = new SDL.FPoint { X = x + 1, Y = yBot };
                        _gridGeomVertices[v + 3].Position = new SDL.FPoint { X = x,     Y = yBot };

                        _gridGeomVertices[v + 0].Color = c;
                        _gridGeomVertices[v + 1].Color = c;
                        _gridGeomVertices[v + 2].Color = c;
                        _gridGeomVertices[v + 3].Color = c;

                        _gridGeomVertices[v + 0].TexCoord = default;
                        _gridGeomVertices[v + 1].TexCoord = default;
                        _gridGeomVertices[v + 2].TexCoord = default;
                        _gridGeomVertices[v + 3].TexCoord = default;

                        _gridGeomIndices[i + 0] = v + 0;
                        _gridGeomIndices[i + 1] = v + 1;
                        _gridGeomIndices[i + 2] = v + 2;
                        _gridGeomIndices[i + 3] = v + 2;
                        _gridGeomIndices[i + 4] = v + 3;
                        _gridGeomIndices[i + 5] = v + 0;

                        v += 4; i += 6; emittedLines++;
                        return;
                    }

                    // Горизонтальная линия
                    if (MathF.Abs(sy2 - sy1) < 1e-4f)
                    {
                        var y = sy1;
                        var xLeft = MathF.Min(sx1, sx2);
                        var xRight = MathF.Max(sx1, sx2);

                        _gridGeomVertices[v + 0].Position = new SDL.FPoint { X = xLeft,  Y = y };
                        _gridGeomVertices[v + 1].Position = new SDL.FPoint { X = xRight, Y = y };
                        _gridGeomVertices[v + 2].Position = new SDL.FPoint { X = xRight, Y = y + 1 };
                        _gridGeomVertices[v + 3].Position = new SDL.FPoint { X = xLeft,  Y = y + 1 };

                        _gridGeomVertices[v + 0].Color = c;
                        _gridGeomVertices[v + 1].Color = c;
                        _gridGeomVertices[v + 2].Color = c;
                        _gridGeomVertices[v + 3].Color = c;

                        _gridGeomVertices[v + 0].TexCoord = default;
                        _gridGeomVertices[v + 1].TexCoord = default;
                        _gridGeomVertices[v + 2].TexCoord = default;
                        _gridGeomVertices[v + 3].TexCoord = default;

                        _gridGeomIndices[i + 0] = v + 0;
                        _gridGeomIndices[i + 1] = v + 1;
                        _gridGeomIndices[i + 2] = v + 2;
                        _gridGeomIndices[i + 3] = v + 2;
                        _gridGeomIndices[i + 4] = v + 3;
                        _gridGeomIndices[i + 5] = v + 0;

                        v += 4; i += 6; emittedLines++;
                        return;
                    }

                    // на всякий случай, если попадёт не axis-aligned
                    // (в вашей сетке это не должно случаться)
                }

                // Старый normal-based путь (оставляем для non-snap или rotated камеры)
                var dx = sx2 - sx1;
                var dy = sy2 - sy1;

                var lenSq = dx * dx + dy * dy;
                if (lenSq <= 1e-6f)
                    return;

                const float halfThickness = 0.5f;
                var invLen = halfThickness / MathF.Sqrt(lenSq);

                var nx = -dy * invLen;
                var ny = dx * invLen;

                _gridGeomVertices[v + 0].Position = new SDL.FPoint { X = sx1 + nx, Y = sy1 + ny };
                _gridGeomVertices[v + 1].Position = new SDL.FPoint { X = sx2 + nx, Y = sy2 + ny };
                _gridGeomVertices[v + 2].Position = new SDL.FPoint { X = sx2 - nx, Y = sy2 - ny };
                _gridGeomVertices[v + 3].Position = new SDL.FPoint { X = sx1 - nx, Y = sy1 - ny };

                _gridGeomVertices[v + 0].Color = c;
                _gridGeomVertices[v + 1].Color = c;
                _gridGeomVertices[v + 2].Color = c;
                _gridGeomVertices[v + 3].Color = c;

                _gridGeomVertices[v + 0].TexCoord = default;
                _gridGeomVertices[v + 1].TexCoord = default;
                _gridGeomVertices[v + 2].TexCoord = default;
                _gridGeomVertices[v + 3].TexCoord = default;

                _gridGeomIndices[i + 0] = v + 0;
                _gridGeomIndices[i + 1] = v + 1;
                _gridGeomIndices[i + 2] = v + 2;
                _gridGeomIndices[i + 3] = v + 2;
                _gridGeomIndices[i + 4] = v + 3;
                _gridGeomIndices[i + 5] = v + 0;

                v += 4;
                i += 6;
                emittedLines++;
            }

            for (var x = xi0; x <= xi1; x++)
            {
                if (x == 0) continue;
                EmitWorldLineQuad(x, yMin, x, yMax);
            }

            for (var y = yi0; y <= yi1; y++)
            {
                if (y == 0) continue;
                EmitWorldLineQuad(xMin, y, xMax, y);
            }

            if (emittedLines > 0)
            {
                Profiler.AddCounter(ProfilerCounterId.RenderDebugLines, emittedLines);
                Profiler.AddCounter(ProfilerCounterId.RenderDrawCalls);

                // texture = null (0): рисуем чистую цветную геометрию.
                SDL.RenderGeometry(_rendererHandle, 0, _gridGeomVertices, v, _gridGeomIndices, i);
            }
        }

        // --------------------------------------------------------------------
        // 2) Оси (ярче) — RenderLine (как требуется)
        // --------------------------------------------------------------------
        SetDrawColor(_debugGridAxis);

        if (0 >= xi0 && 0 <= xi1) DrawWorldLine(0f, yMin, 0f, yMax, in view);
        if (0 >= yi0 && 0 <= yi1) DrawWorldLine(xMin, 0f, xMax, 0f, in view);
    }

    private void DrawWorldLine(float x1, float y1, float x2, float y2, in ViewState view)
    {
        WorldToScreen(x1, y1, out var sx1, out var sy1, in view);
        WorldToScreen(x2, y2, out var sx2, out var sy2, in view);

        // Patch B: gutter-offset при рендере в target
        sx1 += _rtOffsetX; sy1 += _rtOffsetY;
        sx2 += _rtOffsetX; sy2 += _rtOffsetY;

        if (view is { PixelSnap: true, HasRot: false })
        {
            sx1 = SnapPixel(sx1, view.SnapMode);
            sy1 = SnapPixel(sy1, view.SnapMode);
            sx2 = SnapPixel(sx2, view.SnapMode);
            sy2 = SnapPixel(sy2, view.SnapMode);
        }

        Profiler.AddCounter(ProfilerCounterId.RenderDebugLines);
        Profiler.AddCounter(ProfilerCounterId.RenderDrawCalls);

        SDL.RenderLine(_rendererHandle, sx1, sy1, sx2, sy2);
    }
    
    private void DrawCenterCrosshair(in ViewState view)
    {
        // Рисуем в текущем render-space (logical или output).
        var w = view.HalfW * 2f;
        var h = view.HalfH * 2f;

        // Чтобы линия была максимально "пиксельно" стабильной, снапнем центр.
        // Если размеры нечётные — неизбежно будет выбор между x=...5 и x=...0, тут берём ближайший.
        var cx = SnapPixel(view.HalfW, PixelSnapMode.Round);
        var cy = SnapPixel(view.HalfH, PixelSnapMode.Round);

        // Вертикальная красная: сверху вниз
        SDL.SetRenderDrawColor(_rendererHandle, 255, 0, 0, 255);
        SDL.RenderLine(_rendererHandle, cx, 0f, cx, h);

        // Горизонтальная синяя: слева направо
        SDL.SetRenderDrawColor(_rendererHandle, 0, 0, 255, 255);
        SDL.RenderLine(_rendererHandle, 0f, cy, w, cy);

        // Профайлер (по аналогии с DrawWorldLine)
        Profiler.AddCounter(ProfilerCounterId.RenderDebugLines, 2);
        Profiler.AddCounter(ProfilerCounterId.RenderDrawCalls, 2);
    }


    private static void WorldToScreen(float wx, float wy, out float sx, out float sy, in ViewState view)
    {
        // Быстрый и стабильный путь для pixel-snap камеры без rotation:
        // работаем прямо в pixel-space относительно snapped camera px.
        if (view is { PixelSnap: true, HasRot: false })
        {
            var px = wx * view.Ppu - view.CamPosPxSnapped.X;
            var py = wy * view.Ppu - view.CamPosPxSnapped.Y;

            sx = view.HalfW + px;
            sy = view.HalfH - py;
        }
        else
        {
            var rx = wx - view.CamPos.X;
            var ry = wy - view.CamPos.Y;

            var vx = rx * view.Cos + ry * view.Sin;
            var vy = -rx * view.Sin + ry * view.Cos;

            sx = view.HalfW + vx * view.Ppu;
            sy = view.HalfH - vy * view.Ppu;
        }

        // Patch B: смещение в render target (gutter).
        // NOTE: RenderSystem._rtOffsetX/Y недоступны в static методе.
        // Поэтому этот оффсет добавляется в вызывающих местах (DrawWorldLine / EmitWorldLineQuad),
        // либо делай WorldToScreen НЕ static и используй поля напрямую.
    }


    #endregion

    #region Private helpers

    private void TrackUniqueTexture(nint handle)
    {
        if (handle == 0)
            return;

        var key = (ulong)handle;
        var idx = (int)((key * UniqueTextureHashMul) & UniqueTextureTableMask);

        for (var probe = 0; probe < UniqueTextureTableSize; probe++)
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

            idx = (idx + 1) & UniqueTextureTableMask;
        }

        // Переполнение таблицы: игнорируем (на практике для типичных 2D-нагрузок крайне маловероятно).
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float SnapPixel(float v, PixelSnapMode mode)
    {
        const float eps = 1e-4f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float RoundAwayFromZeroEps(float x)
            => x >= 0f ? MathF.Floor(x + 0.5f + eps) : MathF.Ceiling(x - 0.5f - eps);

        return mode switch
        {
            PixelSnapMode.Floor => MathF.Floor(v + eps),
            PixelSnapMode.Ceil  => MathF.Ceiling(v - eps),
            _                   => RoundAwayFromZeroEps(v),
        };
    }

    private void EnsureGridGeometryCapacity(int verticesNeeded, int indicesNeeded)
    {
        if (_gridGeomVertices.Length < verticesNeeded)
            _gridGeomVertices = new SDL.Vertex[GrowCapacity(_gridGeomVertices.Length, verticesNeeded)];

        if (_gridGeomIndices.Length < indicesNeeded)
            _gridGeomIndices = new int[GrowCapacity(_gridGeomIndices.Length, indicesNeeded)];
    }

    private static int GrowCapacity(int current, int needed)
    {
        // Избегаем точного Resize под нужный размер каждый раз, чтобы не ловить аллокационный "джиттер"
        // (например, при зуме/панорамировании).
        var cap = current > 0 ? current : 256;
        while (cap < needed)
            cap <<= 1;

        return cap;
    }

    private void ConfigureDebugGrid(EngineConfig config)
    {
        _debugGridEnabled = config.DebugGridEnabled;
        _debugGridBackground = config.DebugGridColor;
        _debugGridLine = config.DebugGridLineColor;

        // Оси делаем чуть ярче без добавления нового публичного параметра.
        _debugGridAxis = _debugGridLine.AddRGB(0x22);
    }

    private void ApplyVSync(EngineConfig config)
    {
        EffectiveVSync = config.VSync;
        EffectiveVSyncInterval = Math.Max(1, config.VSyncInterval);
        SuggestedMaxFps = 0;

        var requested = config.VSync switch
        {
            VSyncMode.Disabled => 0,
            VSyncMode.Adaptive => -1,
            VSyncMode.Enabled => Math.Max(1, config.VSyncInterval),
            _ => 0
        };

        if (SDL.SetRenderVSync(_rendererHandle, requested))
            return;

        // Цепочка деградации: Adaptive -> 1 -> 0, Interval>1 -> 1 -> 0
        if (requested == -1)
        {
            if (SDL.SetRenderVSync(_rendererHandle, 1))
            {
                EffectiveVSync = VSyncMode.Enabled;
                EffectiveVSyncInterval = 1;
                return;
            }

            SDL.SetRenderVSync(_rendererHandle, 0);
            EffectiveVSync = VSyncMode.Disabled;
            SuggestedMaxFps = 60;
            return;
        }

        if (requested > 1)
        {
            if (SDL.SetRenderVSync(_rendererHandle, 1))
            {
                EffectiveVSync = VSyncMode.Enabled;
                EffectiveVSyncInterval = 1;
                return;
            }

            SDL.SetRenderVSync(_rendererHandle, 0);
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
        if (_rendererHandle == 0)
            throw new InvalidOperationException("RenderSystem is not initialized.");
    }

    private void SetDrawColor(in Color c)
        => SDL.SetRenderDrawColor(_rendererHandle, c.Red, c.Green, c.Blue, c.Alpha);
    
    private FilterMode ResolveFilter(FilterMode spriteMode, FilterMode textureMode)
    {
        // Sprite -> Texture -> Engine default
        if (spriteMode != FilterMode.Inherit)
            return spriteMode;

        if (textureMode != FilterMode.Inherit)
            return textureMode;

        return _defaultTextureFilter;
    }
    
    private static SDL.ScaleMode ToSDLScaleMode(FilterMode mode)
    {
        // Улучшение качества пиксель-арта делается не сэмплингом, а integer-scale + snap.
        return mode switch
        {
            FilterMode.Nearest => SDL.ScaleMode.Nearest,
            FilterMode.Linear => SDL.ScaleMode.Linear,
            FilterMode.Pixelart => SDL.ScaleMode.PixelArt,
            _ => SDL.ScaleMode.Linear
        };
    }
    
    private static bool ShouldUseSceneTarget(in ViewState view)
    {
        // Patch B применяется только для pixel-snap камеры без rotation.
        return view is { PixelSnap: true, HasRot: false };
    }

    private void EnsureSceneTarget(int w, int h)
    {
        if (w <= 0) w = 1;
        if (h <= 0) h = 1;

        if (_sceneTarget != 0 && _sceneTargetW == w && _sceneTargetH == h)
            return;

        DestroySceneTarget();

        // NOTE: точные enum’ы PixelFormat/TextureAccess зависят от твоего SDL3 биндинга.
        // Смысл: создать SDL_Texture* с доступом Target.
        _sceneTarget = SDL.CreateTexture(
            _rendererHandle,
            SDL.PixelFormat.ABGR8888,
            SDL.TextureAccess.Target,
            w, h);

        if (_sceneTarget == 0)
            throw new InvalidOperationException($"SDL.CreateTexture(Target) failed. {SDL.GetError()}");

        _sceneTargetW = w;
        _sceneTargetH = h;

        // Обычно полезно для корректного композита (если target содержит альфу).
        // Если у тебя нет BlendMode в биндинге — можно удалить.
        SDL.SetTextureBlendMode(_sceneTarget, SDL.BlendMode.Blend);
    }

    private void DestroySceneTarget()
    {
        if (_sceneTarget != 0)
            SDL.DestroyTexture(_sceneTarget);

        _sceneTarget = 0;
        _sceneTargetW = 0;
        _sceneTargetH = 0;
    }

    #endregion
}

#endregion
