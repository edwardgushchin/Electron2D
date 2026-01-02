using System;
using System.Numerics;

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

    private const float RadToDeg = 180f / MathF.PI;

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

        ConfigureDebugGrid(config);
        ApplyVSync(config);

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

        var bg = _debugGridEnabled ? _debugGridBackground : scene.ClearColor;
        SDL.SetRenderDrawColor(_rendererHandle, bg.Red, bg.Green, bg.Blue, bg.Alpha);
        SDL.RenderClear(_rendererHandle);
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

        // Опционально: заранее обеспечить ёмкость, чтобы исключить рост массива в течение кадра.
        //_queue.EnsureCapacity(renderers.Length);

        for (var i = 0; i < renderers.Length; i++)
            renderers[i].PrepareRender(_queue, resources, in view.Cull);
    }

    public void EndFrame()
    {
        ThrowIfNotInitialized();

        Flush();

        // Финализация счётчиков кадра.
        Profiler.SetCounter(ProfilerCounterId.RenderUniqueTextures, _uniqueTextureCount);

        using (Profiler.Sample(ProfilerSampleId.RenderPresent))
        {
            SDL.RenderPresent(_rendererHandle);
            Profiler.AddCounter(ProfilerCounterId.RenderPresents, 1);
        }
    }

    public void Shutdown() => Dispose();

    public void Dispose()
    {
        _queue.Dispose();

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
            Profiler.AddCounter(ProfilerCounterId.RenderSortTriggered, 1);
            Profiler.SetCounter(ProfilerCounterId.RenderSortCommands, commands.Length);

            using (Profiler.Sample(ProfilerSampleId.RenderSort))
                SpriteCommandSorter.Sort(commands);
        }

        ref readonly var view = ref EnsureView();

        // Фон: сетка рисуется ДО спрайтов.
        if (_debugGridEnabled)
            DrawDebugGrid(in view);

        // Ёмкость под худший случай: один спрайт на одну команду.
        EnsureSpriteBatchCapacity(commands.Length);

        _spriteBatchTexture = 0;
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

            if (_spriteBatchTexture != 0 && textureHandle != _spriteBatchTexture)
                FlushSpriteBatch();

            if (_spriteBatchTexture == 0)
            {
                _spriteBatchTexture = textureHandle;

                // Уникальные текстуры за кадр.
                TrackUniqueTexture(textureHandle);

                // Эвристика бинд-счётчика: считаем "bind", когда handle меняется в потоке команд.
                // (Оставлено как есть; см. примечание в FlushSpriteBatch про счётчики.)
                if (textureHandle != _lastTextureThisFrame)
                {
                    _lastTextureThisFrame = textureHandle;
                    Profiler.AddCounter(ProfilerCounterId.RenderTextureBinds, 1);
                }
            }

            EmitSpriteQuad(in cmd, in view);
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
        Profiler.AddCounter(ProfilerCounterId.RenderDrawCalls, 1);
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

    private void EmitSpriteQuad(in SpriteCommand cmd, in ViewState view)
    {
        var tex = cmd.Texture;

        var sizeWorld = cmd.SizeWorld;
        if (sizeWorld is not { X: > 0f, Y: > 0f })
            return;

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
        var pivotX = view.HalfW + vx * view.Ppu;
        var pivotY = view.HalfH - vy * view.Ppu;

        var wPx = sizeWorld.X * view.Ppu;
        var hPx = sizeWorld.Y * view.Ppu;

        // Важно: условие в форме !(x > 0) корректно отсекает NaN (x <= 0 не отсекает NaN).
        if (!(wPx > 0f) || !(hPx > 0f))
            return;

        // Origin в пикселях (в текущей конвенции).
        var originPxX = cmd.OriginWorld.X * view.Ppu;
        var originPxY = (sizeWorld.Y - cmd.OriginWorld.Y) * view.Ppu;

        // Локальные углы вокруг pivot (screen space, y-down).
        var x0 = -originPxX;      var y0 = -originPxY;      // top-left
        var x1 = wPx - originPxX; var y1 = -originPxY;      // top-right
        var x2 = wPx - originPxX; var y2 = hPx - originPxY; // bottom-right
        var x3 = -originPxX;      var y3 = hPx - originPxY; // bottom-left

        // Итоговый угол (как в текущей логике RenderTextureRotated).
        var rot = view.HasRot ? (cmd.Rotation - view.CamRot) : cmd.Rotation;
        var angle = -rot; // радианы, clockwise в screen-y-down при стандартной формуле

        float p0x, p0y, p1x, p1y, p2x, p2y, p3x, p3y;

        if (angle != 0f)
        {
            var c = MathF.Cos(angle);
            var s = MathF.Sin(angle);

            var rx0 = x0 * c - y0 * s; var ry0 = x0 * s + y0 * c;
            var rx1 = x1 * c - y1 * s; var ry1 = x1 * s + y1 * c;
            var rx2 = x2 * c - y2 * s; var ry2 = x2 * s + y2 * c;
            var rx3 = x3 * c - y3 * s; var ry3 = x3 * s + y3 * c;

            p0x = pivotX + rx0; p0y = pivotY + ry0;
            p1x = pivotX + rx1; p1y = pivotY + ry1;
            p2x = pivotX + rx2; p2y = pivotY + ry2;
            p3x = pivotX + rx3; p3y = pivotY + ry3;
        }
        else
        {
            p0x = pivotX + x0; p0y = pivotY + y0;
            p1x = pivotX + x1; p1y = pivotY + y1;
            p2x = pivotX + x2; p2y = pivotY + y2;
            p3x = pivotX + x3; p3y = pivotY + y3;
        }

        // UV (нормализованные 0..1). Предположение: tex.Width/Height — кэшированы (без SDL-вызовов по кадру).
        var invW = 1f / tex.Width;
        var invH = 1f / tex.Height;

        var u0 = cmd.SrcRect.X * invW;
        var v0 = cmd.SrcRect.Y * invH;
        var u1 = (cmd.SrcRect.X + cmd.SrcRect.Width) * invW;
        var v1 = (cmd.SrcRect.Y + cmd.SrcRect.Height) * invH;

        // Flip: меняем UV местами.
        var flip = (SDL.FlipMode)cmd.FlipMode;

        if ((flip & SDL.FlipMode.Horizontal) != 0) (u0, u1) = (u1, u0);
        if ((flip & SDL.FlipMode.Vertical) != 0) (v0, v1) = (v1, v0);

        // Цвет вершины (SDL_FColor float [0..1]).
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

        // Два треугольника.
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
        SDL.GetRenderOutputSize(_rendererHandle, out var outW, out var outH);

        // Safety (например, свёрнутое окно / транзит ресайза).
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

        // Сохраняем cos/sin(camRot), применяем поворот на -camRot матрицей [c s; -s c].
        var c = hasRot ? MathF.Cos(camRot) : 1f;
        var s = hasRot ? MathF.Sin(camRot) : 0f;

        var orthoSize = cam?.OrthoSize ?? _fallbackOrthoSize;
        if (!(orthoSize > 0f))
            orthoSize = MinPositiveFloat;

        // Pixels per 1 world-unit (vertical).
        var ppu = outH / (2f * orthoSize);
        if (!(ppu > 0f))
            ppu = MinPositiveFloat;

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
            cull: cull);
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

            // Толщина линии как в RenderLine: 1px => half = 0.5.
            const float halfThickness = 0.5f;

            var v = 0;
            var i = 0;
            var emittedLines = 0;

            var state = view;

            void EmitWorldLineQuad(float wx1, float wy1, float wx2, float wy2)
            {
                WorldToScreen(wx1, wy1, out var sx1, out var sy1, in state);
                WorldToScreen(wx2, wy2, out var sx2, out var sy2, in state);

                var dx = sx2 - sx1;
                var dy = sy2 - sy1;

                var lenSq = dx * dx + dy * dy;
                if (lenSq <= 1e-6f)
                    return;

                var invLen = halfThickness / MathF.Sqrt(lenSq);

                // Перпендикуляр (нормаль) длиной halfThickness.
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

        Profiler.AddCounter(ProfilerCounterId.RenderDebugLines);
        Profiler.AddCounter(ProfilerCounterId.RenderDrawCalls);

        SDL.RenderLine(_rendererHandle, sx1, sy1, sx2, sy2);
    }

    private static void WorldToScreen(float wx, float wy, out float sx, out float sy, in ViewState view)
    {
        var rx = wx - view.CamPos.X;
        var ry = wy - view.CamPos.Y;

        // Поворот на -camRot: [c s; -s c].
        var vx = rx * view.Cos + ry * view.Sin;
        var vy = -rx * view.Sin + ry * view.Cos;

        sx = view.HalfW + vx * view.Ppu;
        sy = view.HalfH - vy * view.Ppu;
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

    #endregion
}

#endregion
