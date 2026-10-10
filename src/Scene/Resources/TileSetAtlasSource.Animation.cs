namespace Electron2D;

public sealed partial class TileSetAtlasSource
{
    /// <summary>Selects synchronized or stable cell-specific starting phases for retained frame animation.</summary>
    public enum TileAnimationMode
    {
        /// <summary>All cells use the same renderer clock phase.</summary>
        Default = 0,
        /// <summary>Each cell uses a stable pseudorandom phase based on its layer and position.</summary>
        RandomStartTimes = 1,
        /// <summary>Exclusive mode-count sentinel; cannot be assigned to a tile.</summary>
        Max = 2
    }
    /// <summary>Returns animation columns; zero lays all frames out horizontally.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    public int GetTileAnimationColumns(Vector2i atlasCoords) => GetTile(atlasCoords).Columns;
    /// <summary>Sets nonnegative columns after checking all frame reservations.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param><param name="frameColumns">Zero for one horizontal row, or a positive column count.</param>
    public void SetTileAnimationColumns(Vector2i atlasCoords, int frameColumns)
    { var tile = GetTile(atlasCoords); if (frameColumns < 0) throw new ArgumentOutOfRangeException(nameof(frameColumns)); RequireRoom(atlasCoords, tile, frameColumns, tile.FrameSeparation, tile.Durations.Length); if (tile.Columns == frameColumns) return; tile.Columns = frameColumns; Change(); }
    /// <summary>Returns nonnegative spacing between animation frames in atlas cells.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    public Vector2i GetTileAnimationSeparation(Vector2i atlasCoords) => GetTile(atlasCoords).FrameSeparation;
    /// <summary>Sets grid-unit frame spacing after checking the complete layout.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param><param name="separation">Nonnegative horizontal and vertical atlas-cell gaps.</param>
    public void SetTileAnimationSeparation(Vector2i atlasCoords, Vector2i separation)
    { var tile = GetTile(atlasCoords); Nonnegative(separation); RequireRoom(atlasCoords, tile, tile.Columns, separation, tile.Durations.Length); if (tile.FrameSeparation == separation) return; tile.FrameSeparation = separation; Change(); }
    /// <summary>Returns the positive playback speed multiplier.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    public float GetTileAnimationSpeed(Vector2i atlasCoords) => GetTile(atlasCoords).Speed;
    /// <summary>Sets a finite positive playback multiplier without changing stored durations.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param><param name="speed">Finite positive multiplier.</param>
    public void SetTileAnimationSpeed(Vector2i atlasCoords, float speed)
    { var tile = GetTile(atlasCoords); PositiveFinite(speed); if (tile.Speed == speed) return; tile.Speed = speed; Change(); }
    /// <summary>Returns the retained animation phase policy.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    public TileAnimationMode GetTileAnimationMode(Vector2i atlasCoords) => GetTile(atlasCoords).Mode;
    /// <summary>Sets synchronized or random-start animation; rejects the count sentinel.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param><param name="mode">A defined playable animation mode.</param>
    public void SetTileAnimationMode(Vector2i atlasCoords, TileAnimationMode mode)
    { var tile = GetTile(atlasCoords); ValidMode(mode); if (tile.Mode == mode) return; tile.Mode = mode; Change(); }
    /// <summary>Returns the positive animation frame count.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    public int GetTileAnimationFramesCount(Vector2i atlasCoords) => GetTile(atlasCoords).Durations.Length;
    /// <summary>Resizes frame reservations, retaining surviving durations and initializing new durations to one.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param><param name="framesCount">Positive frame count fitting the unoccupied atlas grid.</param>
    public void SetTileAnimationFramesCount(Vector2i atlasCoords, int framesCount)
    {
        var tile = GetTile(atlasCoords); if (framesCount < 1) throw new ArgumentOutOfRangeException(nameof(framesCount)); if (framesCount == tile.Durations.Length) return;
        RequireRoom(atlasCoords, tile, tile.Columns, tile.FrameSeparation, framesCount);
        var durations = new float[framesCount]; tile.Durations.AsSpan(0, Math.Min(framesCount, tile.Durations.Length)).CopyTo(durations); if (framesCount > tile.Durations.Length) Array.Fill(durations, 1, tile.Durations.Length, framesCount - tile.Durations.Length);
        ValidateDurations(durations); tile.Durations = durations; Change();
    }
    /// <summary>Returns a frame's unscaled positive duration.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param><param name="frameIndex">Zero-based animation frame.</param>
    public float GetTileAnimationFrameDuration(Vector2i atlasCoords, int frameIndex) { var tile = GetTile(atlasCoords); ValidateFrame(tile, frameIndex); return tile.Durations[frameIndex]; }
    /// <summary>Sets a finite positive duration before playback-speed scaling.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param><param name="frameIndex">Zero-based animation frame.</param><param name="duration">Finite positive duration; the total must fit a finite float.</param>
    public void SetTileAnimationFrameDuration(Vector2i atlasCoords, int frameIndex, float duration)
    { var tile = GetTile(atlasCoords); ValidateFrame(tile, frameIndex); PositiveFinite(duration); if (Total(tile) - tile.Durations[frameIndex] + duration > float.MaxValue) throw new ArgumentOutOfRangeException(nameof(duration)); if (tile.Durations[frameIndex] == duration) return; tile.Durations[frameIndex] = duration; Change(); }
    /// <summary>Returns the sum of stored frame durations, without dividing by playback speed.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    public float GetTileAnimationTotalDuration(Vector2i atlasCoords) => (float)Total(GetTile(atlasCoords));
    /// <summary>Tests whether every proposed frame fits the atlas without intersecting another tile.</summary>
    /// <param name="atlasCoords">Proposed nonnegative base coordinate.</param><param name="size">Positive frame size in atlas cells.</param><param name="animationColumns">Nonnegative columns; zero is horizontal.</param><param name="animationSeparation">Nonnegative frame gaps in atlas cells.</param><param name="framesCount">Positive frame count.</param><param name="ignoredTile">Existing base tile to exclude, or null for none.</param>
    public bool HasRoomForTile(Vector2i atlasCoords, Vector2i size, int animationColumns, Vector2i animationSeparation, int framesCount, Vector2i? ignoredTile = null)
    {
        ThrowIfDisposed(); if (atlasCoords.X < 0 || atlasCoords.Y < 0 || size.X <= 0 || size.Y <= 0 || animationColumns < 0 || animationSeparation.X < 0 || animationSeparation.Y < 0 || framesCount < 1) return false;
        var grid = GetAtlasGridSize(); if (!LayoutFits(atlasCoords, size, animationColumns, animationSeparation, framesCount, grid)) return false;
        for (var frame = 0; frame < framesCount; frame++)
        {
            var origin = FrameCoords(atlasCoords, size, animationColumns, animationSeparation, frame);
            foreach (var pair in _tiles)
            {
                if (pair.Key == ignoredTile) continue;
                for (var other = 0; other < pair.Value.Durations.Length; other++)
                {
                    var p = FrameCoords(pair.Key, pair.Value, other); var extent = pair.Value.Size;
                    if (origin.X < (long)p.X + extent.X && p.X < (long)origin.X + size.X && origin.Y < (long)p.Y + extent.Y && p.Y < (long)origin.Y + size.Y) return false;
                }
            }
        }
        return true;
    }
    /// <summary>Returns base coordinates that would no longer fit a proposed texture/grid policy, without changing this source.</summary>
    /// <param name="texture">Borrowed proposed texture, or null for an empty grid.</param><param name="margins">Nonnegative top/left pixels.</param><param name="separation">Nonnegative inter-cell pixel gaps.</param><param name="textureRegionSize">Positive cell pixel size.</param>
    public Vector2i[] GetTilesToBeRemovedOnChange(Texture? texture, Vector2i margins, Vector2i separation, Vector2i textureRegionSize)
    { ThrowIfDisposed(); Nonnegative(margins); Nonnegative(separation); TileSet.Positive(textureRegionSize); CheckSum(textureRegionSize, separation); if (texture is not null) ObjectDisposedException.ThrowIf(texture.IsDisposed, texture); var grid = Grid(texture, margins, separation, textureRegionSize); return _tiles.Where(p => !TileFits(p.Key, p.Value, grid)).Select(p => p.Key).ToArray(); }
    private static Vector2i Grid(Texture? texture, Vector2i margins, Vector2i separation, Vector2i region)
    { if (texture is null) return default; var size = texture.GetSize() - (Vector2)margins; if (size.X < region.X || size.Y < region.Y) return default; return new(1 + (int)((size.X - region.X) / (region.X + separation.X)), 1 + (int)((size.Y - region.Y) / (region.Y + separation.Y))); }
    private static Vector2i FrameCoords(Vector2i origin, Tile tile, int frame) => FrameCoords(origin, tile.Size, tile.Columns, tile.FrameSeparation, frame);
    private static Vector2i FrameCoords(Vector2i origin, Vector2i size, int columns, Vector2i separation, int frame) => new(checked((int)((long)origin.X + ((long)size.X + separation.X) * (columns > 0 ? frame % columns : frame))), checked((int)((long)origin.Y + ((long)size.Y + separation.Y) * (columns > 0 ? frame / columns : 0))));
    private static bool LayoutFits(Vector2i origin, Vector2i size, int columns, Vector2i separation, int count, Vector2i grid)
    {
        var x = columns > 0 ? Math.Min(count, columns) - 1 : count - 1; var y = columns > 0 ? (count - 1) / columns : 0;
        return (long)origin.X + ((long)size.X + separation.X) * x + size.X <= grid.X && (long)origin.Y + ((long)size.Y + separation.Y) * y + size.Y <= grid.Y;
    }
    private static bool TileFits(Vector2i coords, Tile tile, Vector2i grid) => LayoutFits(coords, tile.Size, tile.Columns, tile.FrameSeparation, tile.Durations.Length, grid);
    private void RequireRoom(Vector2i coords, Tile tile, int columns, Vector2i separation, int count) { if (!HasRoomForTile(coords, tile.Size, columns, separation, count, coords)) throw new ArgumentException("Animation frames overlap another tile or exceed the texture."); }
    private static void ValidateFrame(Tile tile, int frame) { if ((uint)frame >= (uint)tile.Durations.Length) throw new ArgumentOutOfRangeException(nameof(frame)); }
    private static void PositiveFinite(float value) { if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void ValidMode(TileAnimationMode mode) { if (mode is not (TileAnimationMode.Default or TileAnimationMode.RandomStartTimes)) throw new ArgumentOutOfRangeException(nameof(mode)); }
    private static double Total(Tile tile) { double sum = 0; foreach (var duration in tile.Durations) sum += duration; return sum; }
    private static void ValidateDurations(float[] durations) { ArgumentNullException.ThrowIfNull(durations); if (durations.Length == 0) throw new ArgumentException("An animation requires at least one frame."); double sum = 0; foreach (var d in durations) { PositiveFinite(d); sum += d; } if (sum > float.MaxValue) throw new ArgumentException("Animation total exceeds the finite duration range."); }
    private IEnumerable<PropertyDescriptor> AnimationProperties(Vector2i coords, string prefix)
    {
        yield return new PropertyDescriptor<TileSetAtlasSource, int>(prefix + "AnimationColumns", n => n.GetTile(coords).Columns, (n, v) => { if (v < 0) throw new ArgumentOutOfRangeException(nameof(v)); n.GetTile(coords).Columns = v; n.Change(); }, _ => 0, stored: true);
        yield return new PropertyDescriptor<TileSetAtlasSource, Vector2i>(prefix + "AnimationSeparation", n => n.GetTile(coords).FrameSeparation, (n, v) => { Nonnegative(v); n.GetTile(coords).FrameSeparation = v; n.Change(); }, _ => default, stored: true);
        yield return new PropertyDescriptor<TileSetAtlasSource, float>(prefix + "AnimationSpeed", n => n.GetTileAnimationSpeed(coords), (n, v) => n.SetTileAnimationSpeed(coords, v), _ => 1, stored: true);
        yield return new PropertyDescriptor<TileSetAtlasSource, TileAnimationMode>(prefix + "AnimationMode", n => n.GetTileAnimationMode(coords), (n, v) => n.SetTileAnimationMode(coords, v), _ => TileAnimationMode.Default, stored: true);
        yield return new PropertyDescriptor<TileSetAtlasSource, float[]>(prefix + "AnimationDurations", n => (float[])n.GetTile(coords).Durations.Clone(), (n, v) => { ValidateDurations(v); n.GetTile(coords).Durations = (float[])v.Clone(); n.Change(); }, _ => [1], stored: true);
    }
}
