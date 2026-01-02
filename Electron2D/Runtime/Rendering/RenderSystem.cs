using System.Numerics;
using SDL3;

namespace Electron2D;

internal sealed class RenderSystem : IDisposable
{
    #region Constants

    private const float RadToDeg = 180f / MathF.PI;

    // Used as a safe lower bound for camera ortho size / PPU to avoid division-by-zero.
    private const float MinPositiveFloat = 0.0001f;

    private const int DefaultRenderQueueCapacity = 2048;

    // Fixed-size open-addressing table for "unique textures per frame" counter.
    // Must be a power of two (masking).
    private const int UniqueTextureTableSize = 1024;
    private const int UniqueTextureTableMask = UniqueTextureTableSize - 1;

    // Knuth multiplicative hash constant (64-bit).
    private const ulong UniqueTextureHashMul = 11400714819323198485UL;

    #endregion

    #region Fields

    private readonly RenderQueue _queue = new(initialCapacity: DefaultRenderQueueCapacity);

    private nint _handle; // SDL_Renderer*
    private bool _ownsHandle;

    private nint _lastTextureThisFrame;

    private readonly nint[] _uniqueTextureTable = new nint[UniqueTextureTableSize]; // open addressing
    private int _uniqueTextureCount;

    private SDL.Vertex[] _spriteBatchVertices = [];
    private int[] _spriteBatchIndices = [];
    private int _spriteBatchVertexCount;
    private int _spriteBatchIndexCount;
    private int _spriteBatchSpriteCount;
    private nint _spriteBatchTexture; // SDL_Texture*

    private SDL.Vertex[] _gridGeomVertices = [];
    private int[] _gridGeomIndices = [];

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

    internal nint Handle => _handle;

    internal VSyncMode EffectiveVSync { get; private set; } = VSyncMode.Disabled;
    internal int EffectiveVSyncInterval { get; private set; } = 1;

    // If VSync was requested but couldn't be enabled, an external FPS cap can be suggested (0 = none).
    internal int SuggestedMaxFps { get; private set; }

    public RenderQueue Queue => _queue;

    #endregion

    #region Lifecycle

    /// <summary>
    /// Initializes the renderer system by creating an SDL_Renderer* for the provided SDL_Window*.
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

        // Frame-local command queue: no accumulation across frames.
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

        // Optional: pre-ensure capacity to avoid growth during a frame.
        //_queue.EnsureCapacity(renderers.Length);

        for (var i = 0; i < renderers.Length; i++)
            renderers[i].PrepareRender(_queue, resources, in view.Cull);
    }

    public void EndFrame()
    {
        ThrowIfNotInitialized();

        Flush();

        // Finalize render-frame counters.
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

    #endregion

    #region Render execution

    private void Flush()
    {
        ThrowIfNotInitialized();

        using var _ = Profiler.Sample(ProfilerSampleId.RenderFlush);

        var cmds = _queue.CommandsMutable;

        // If there are no commands and grid is off, nothing to do.
        if (cmds.Length == 0 && !_debugGridEnabled)
            return;

        if (cmds.Length > 1 && _queue.NeedsSort)
        {
            Profiler.AddCounter(ProfilerCounterId.RenderSortTriggered, 1);
            Profiler.SetCounter(ProfilerCounterId.RenderSortCommands, cmds.Length);

            using (Profiler.Sample(ProfilerSampleId.RenderSort))
                SpriteCommandSorter.Sort(cmds);
        }

        ref readonly var view = ref EnsureView();

        // Background: grid is drawn BEFORE sprites.
        if (_debugGridEnabled)
            DrawDebugGrid(in view);

        // Ensure capacity for worst-case (one sprite per command).
        EnsureSpriteBatchCapacity(cmds.Length);

        _spriteBatchTexture = 0;
        _spriteBatchVertexCount = 0;
        _spriteBatchIndexCount = 0;
        _spriteBatchSpriteCount = 0;

        for (var i = 0; i < cmds.Length; i++)
        {
            ref readonly var cmd = ref cmds[i];
            var tex = cmd.Texture;
            if (!tex.IsValid)
                continue;

            var h = tex.Handle;

            if (_spriteBatchTexture != 0 && h != _spriteBatchTexture)
                FlushSpriteBatch();

            if (_spriteBatchTexture == 0)
            {
                _spriteBatchTexture = h;

                // Unique textures per frame.
                TrackUniqueTexture(h);

                // Bind heuristic: count when handle changes in the command stream.
                // (Kept as-is; see note in FlushSpriteBatch about counters.)
                if (h != _lastTextureThisFrame)
                {
                    _lastTextureThisFrame = h;
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

        // Counters: 1 drawcall per batch.
        Profiler.AddCounter(ProfilerCounterId.RenderDrawCalls, 1);
        Profiler.AddCounter(ProfilerCounterId.RenderSprites, _spriteBatchSpriteCount);

        // NOTE: This increments texture-binds per batch, in addition to the heuristic in Flush().
        // Kept unchanged to preserve existing profiling semantics.
        Profiler.AddCounter(ProfilerCounterId.RenderTextureBinds, 1);

        SDL.RenderGeometry(
            _handle,
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
        if (!(wPx > 0f) || !(hPx > 0f))
            return;

        // Origin in pixels (as in existing convention).
        var originPxX = cmd.OriginWorld.X * view.Ppu;
        var originPxY = (sizeWorld.Y - cmd.OriginWorld.Y) * view.Ppu;

        // Local corners around pivot (screen space, y-down).
        var x0 = -originPxX;      var y0 = -originPxY;      // top-left
        var x1 = wPx - originPxX; var y1 = -originPxY;      // top-right
        var x2 = wPx - originPxX; var y2 = hPx - originPxY; // bottom-right
        var x3 = -originPxX;      var y3 = hPx - originPxY; // bottom-left

        // Final angle (matches existing RenderTextureRotated logic).
        var rot = view.HasRot ? (cmd.Rotation - view.CamRot) : cmd.Rotation;
        var angle = -rot; // radians, clockwise in screen-y-down when using standard formula

        float c = 1f, s = 0f;
        if (angle != 0f)
        {
            c = MathF.Cos(angle);
            s = MathF.Sin(angle);
        }

        void RotateToScreen(float lx, float ly, out float sx, out float sy)
        {
            if (angle != 0f)
            {
                var rx = lx * c - ly * s;
                var ry = lx * s + ly * c;
                sx = pivotX + rx;
                sy = pivotY + ry;
            }
            else
            {
                sx = pivotX + lx;
                sy = pivotY + ly;
            }
        }

        RotateToScreen(x0, y0, out var p0x, out var p0y);
        RotateToScreen(x1, y1, out var p1x, out var p1y);
        RotateToScreen(x2, y2, out var p2x, out var p2y);
        RotateToScreen(x3, y3, out var p3x, out var p3y);

        // UV (normalized 0..1). Assumption: tex.Width/Height are cached (no SDL calls in-frame).
        var invW = 1f / tex.Width;
        var invH = 1f / tex.Height;

        var u0 = cmd.SrcRect.X * invW;
        var v0 = cmd.SrcRect.Y * invH;
        var u1 = (cmd.SrcRect.X + cmd.SrcRect.Width) * invW;
        var v1 = (cmd.SrcRect.Y + cmd.SrcRect.Height) * invH;

        // Flip: swap UVs.
        var flip = (SDL.FlipMode)cmd.FlipMode;

        if ((flip & SDL.FlipMode.Horizontal) != 0) (u0, u1) = (u1, u0);
        if ((flip & SDL.FlipMode.Vertical) != 0) (v0, v1) = (v1, v0);

        // Per-vertex color (SDL_FColor float [0..1]).
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

        // Two triangles.
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
        SDL.GetRenderOutputSize(_handle, out var outW, out var outH);

        // Safety (e.g., minimized window / resize transient).
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

        // Store cos/sin(camRot), apply rotation by -camRot using [c s; -s c].
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

    // NOTE: Not used by the current batched path (Flush -> EmitSpriteQuad -> RenderGeometry),
    // but kept as a reference / fallback path without changing public behavior.
    private void DrawSprite(in SpriteCommand cmd, in ViewState view)
    {
        var tex = cmd.Texture;
        if (!tex.IsValid)
            return;

        var size = cmd.SizeWorld;
        if (size is not { X: > 0f, Y: > 0f })
            return;

        // Texture bind heuristic: count "bind" when handle changes in the command stream.
        var h = tex.Handle;
        if (h != _lastTextureThisFrame)
        {
            _lastTextureThisFrame = h;
            Profiler.AddCounter(ProfilerCounterId.RenderTextureBinds, 1);
        }

        // Unique textures per frame (fixed hash table).
        TrackUniqueTexture(h);

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

        // View -> screen.
        var pivotX = view.HalfW + vx * view.Ppu;
        var pivotY = view.HalfH - vy * view.Ppu;

        var wPx = size.X * view.Ppu;
        var hPx = size.Y * view.Ppu;

        if (!(wPx > 0f) || !(hPx > 0f))
            return;

        // Origin is defined from bottom-left (World Y-up).
        var originPxX = cmd.OriginWorld.X * view.Ppu;
        var originPxY = (size.Y - cmd.OriginWorld.Y) * view.Ppu;

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

        // SDL angle is clockwise (Y down). World rotation is typically CCW => negate.
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
            (SDL.FlipMode)cmd.FlipMode);
    }

    private void DrawDebugGrid(in ViewState view)
    {
        // Half sizes in world-units.
        var viewHalfW = view.HalfW / view.Ppu;
        var viewHalfH = view.HalfH / view.Ppu;

        // For rotated camera, use bounding square by diagonal to cover screen.
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
        // 1) Regular lines (excluding axes) — RenderGeometry
        // --------------------------------------------------------------------

        var vLines = xi1 - xi0 + 1;
        if (0 >= xi0 && 0 <= xi1) vLines--; // exclude X=0 axis

        var hLines = yi1 - yi0 + 1;
        if (0 >= yi0 && 0 <= yi1) hLines--; // exclude Y=0 axis

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

            // Line thickness matching RenderLine: 1px => half = 0.5.
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

                // Perpendicular (normal) of length halfThickness.
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
                Profiler.AddCounter(ProfilerCounterId.RenderDrawCalls, 1);

                // texture = null (0): draw pure colored geometry.
                SDL.RenderGeometry(_handle, 0, _gridGeomVertices, v, _gridGeomIndices, i);
            }
        }

        // --------------------------------------------------------------------
        // 2) Axes (brighter) — RenderLine (as required)
        // --------------------------------------------------------------------
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

        // Rotate by -camRot: [c s; -s c].
        var vx = rx * view.Cos + ry * view.Sin;
        var vy = -rx * view.Sin + ry * view.Cos;

        sx = view.HalfW + vx * view.Ppu;
        sy = view.HalfH - vy * view.Ppu;
    }

    #endregion

    #region Internals / helpers

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

        // Table overflow: ignore (extremely unlikely in typical 2D workloads).
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
        // Avoid exact Resize every time to prevent allocation jitter (e.g., while zooming).
        var cap = current > 0 ? current : 256;
        while (cap < needed)
            cap <<= 1;
        return cap;
    }

    private void ConfigureDebugGrid(EngineConfig cfg)
    {
        _debugGridEnabled = cfg.DebugGridEnabled;
        _debugGridBackground = cfg.DebugGridColor;
        _debugGridLine = cfg.DebugGridLineColor;

        // Slightly brighter axes without adding a new public parameter.
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

        // requested == 0 or 1, but SetRenderVSync still failed
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

    #endregion
}
