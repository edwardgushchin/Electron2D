using System.Globalization;
using System.Numerics;

namespace Electron2D;

/// <summary>
/// Спрайтшит с нарезкой по сетке (grid slicing) + билдерами клипов.
/// </summary>
/// <remarks>
/// Это не "визуальный" инструмент — он сокращает код конфигурации анимаций,
/// позволяя описывать кадры диапазонами или короткой строковой нотацией.
/// </remarks>
public sealed class SpriteSheet
{
    private readonly Texture _texture;
    private readonly SpriteSheetGrid _grid;

    public SpriteImportDefaults Defaults { get; private set; } = SpriteImportDefaults.Default;
    public Texture Texture => _texture;
    public SpriteSheetGrid Grid => _grid;

    private SpriteSheet(Texture texture, SpriteSheetGrid grid)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if (!texture.IsValid)
            throw new ArgumentOutOfRangeException(nameof(texture), "Texture must be valid.");

        _texture = texture;
        _grid = grid;
    }

    /// <summary>
    /// Создаёт SpriteSheet по сетке.
    /// </summary>
    public static SpriteSheet GridSheet(Texture texture, int cellW, int cellH, int margin = 0, int spacing = 0)
        => new(texture, new SpriteSheetGrid(cellW, cellH, margin, spacing));

    /// <summary>
    /// Загружает текстуру по id (через <see cref="Resources"/>) и применяет дефолты из .meta, если они есть.
    /// </summary>
    public static SpriteSheet Load(string textureId, int cellW, int cellH, int margin = 0, int spacing = 0)
    {
        var id = string.IsNullOrWhiteSpace(textureId)
            ? throw new ArgumentException("textureId is empty.", nameof(textureId))
            : textureId;

        var tex = Resources.GetTexture(id);
        var sheet = GridSheet(tex, cellW, cellH, margin, spacing);
        sheet.Defaults = Resources.GetSpriteImportDefaults(id);
        return sheet;
    }

    public SpriteSheet WithDefaults(SpriteImportDefaults defaults)
    {
        Defaults = defaults;
        return this;
    }

    public Sprite Cell(int row, int col)
    {
        if (row < 0) throw new ArgumentOutOfRangeException(nameof(row), row, "Row must be >= 0.");
        if (col < 0) throw new ArgumentOutOfRangeException(nameof(col), col, "Col must be >= 0.");

        var x = _grid.Margin + col * (_grid.CellWidth + _grid.Spacing);
        var y = _grid.Margin + row * (_grid.CellHeight + _grid.Spacing);

        var r = new Rect(x, y, _grid.CellWidth, _grid.CellHeight);
        return new Sprite(
            texture: _texture,
            pixelsPerUnit: Defaults.PixelsPerUnit,
            pivot: Defaults.Pivot,
            rect: r,
            textureRect: r,
            filterMode: Defaults.FilterMode);
    }

    public SpriteSheetRow Row(int row) => new(this, row);

    public AnimationClip ClipCell(string name, int row, int col, float fps = 1f, bool loop = true)
        => new(name, new[] { Cell(row, col) }, fps, loop);

    public AnimationClip ClipRow(string name, int row, int startCol, int count, float fps, bool loop = true)
        => Clip(name, fps, loop, new SpriteSheetSpan(row, startCol, count));

    public AnimationClip Clip(string name, float fps, bool loop, params SpriteSheetSpan[] spans)
    {
        ArgumentNullException.ThrowIfNull(spans);
        if (spans.Length == 0)
            throw new ArgumentException("spans must be non-empty.", nameof(spans));

        var total = 0;
        for (var i = 0; i < spans.Length; i++)
        {
            var s = spans[i];
            if (s.Row < 0) throw new ArgumentOutOfRangeException(nameof(spans), s.Row, "Row must be >= 0.");
            if (s.StartCol < 0) throw new ArgumentOutOfRangeException(nameof(spans), s.StartCol, "StartCol must be >= 0.");
            if (s.Count <= 0) throw new ArgumentOutOfRangeException(nameof(spans), s.Count, "Count must be > 0.");
            total += s.Count;
        }

        var frames = new Sprite[total];
        var k = 0;

        for (var i = 0; i < spans.Length; i++)
        {
            var s = spans[i];
            for (var c = 0; c < s.Count; c++)
                frames[k++] = Cell(s.Row, s.StartCol + c);
        }

        return new AnimationClip(name, frames, fps, loop);
    }

    /// <summary>
    /// Создаёт клип из короткой нотации кадров.
    /// Примеры:
    /// "0:0-5"; "2:0-7"; "5:2-7;6:0-3"; "3:6".
    /// </summary>
    public AnimationClip Clip(string name, float fps, bool loop, string framesSpec)
        => Clip(name, fps, loop, SpriteSheetFrames.Parse(framesSpec));
}

/// <summary>
/// Параметры сетки спрайтшита.
/// </summary>
public readonly record struct SpriteSheetGrid
{
    public SpriteSheetGrid(int cellWidth, int cellHeight, int margin, int spacing) : this()
    {
        if (cellWidth <= 0) throw new ArgumentOutOfRangeException(nameof(cellWidth), cellWidth, "CellWidth must be > 0.");
        if (cellHeight <= 0) throw new ArgumentOutOfRangeException(nameof(cellHeight), cellHeight, "CellHeight must be > 0.");
        if (margin < 0) throw new ArgumentOutOfRangeException(nameof(margin), margin, "Margin must be >= 0.");
        if (spacing < 0) throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "Spacing must be >= 0.");

        CellWidth = cellWidth;
        CellHeight = cellHeight;
        Margin = margin;
        Spacing = spacing;
    }
    
    public  int CellWidth { get; }
    public int CellHeight { get; }
    public int Margin { get; }
    public int Spacing { get; }
}

/// <summary>
/// Диапазон кадров в одном ряду: (row, startCol, count).
/// </summary>
public readonly record struct SpriteSheetSpan(int Row, int StartCol, int Count);

/// <summary>
/// Fluent-хелпер для построения диапазонов в конкретном ряду.
/// </summary>
public readonly struct SpriteSheetRow
{
    private readonly SpriteSheet _sheet;
    private readonly int _row;

    internal SpriteSheetRow(SpriteSheet sheet, int row)
    {
        _sheet = sheet;
        _row = row;
    }

    /// <summary>
    /// Диапазон в ряду: startCol + count.
    /// </summary>
    public SpriteSheetSpan Range(int startCol, int count) => new(_row, startCol, count);

    /// <summary>
    /// Диапазон в ряду: startCol..endCol (включительно).
    /// </summary>
    public SpriteSheetSpan RangeInclusive(int startCol, int endCol)
        => new(_row, startCol, endCol - startCol + 1);
}

/// <summary>
/// Парсер короткой нотации кадров для SpriteSheet.
/// </summary>
internal static class SpriteSheetFrames
{
    public static SpriteSheetSpan[] Parse(string framesSpec)
    {
        if (string.IsNullOrWhiteSpace(framesSpec))
            throw new ArgumentException("framesSpec must be non-empty.", nameof(framesSpec));

        // Разрешаем разделители ';' и ',' чтобы было удобно писать.
        var parts = framesSpec
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
            throw new ArgumentException("framesSpec must contain at least one segment.", nameof(framesSpec));

        var spans = new SpriteSheetSpan[parts.Length];
        var idx = 0;

        for (var i = 0; i < parts.Length; i++)
        {
            var token = parts[i];
            var colon = token.IndexOf(':');
            if (colon <= 0 || colon == token.Length - 1)
                throw new FormatException($"Invalid framesSpec segment '{token}'. Expected 'row:col' or 'row:colA-colB'.");

            var rowStr = token[..colon].Trim();
            var colsStr = token[(colon + 1)..].Trim();

            if (!int.TryParse(rowStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var row) || row < 0)
                throw new FormatException($"Invalid row in segment '{token}'.");

            var dash = colsStr.IndexOf('-');
            if (dash < 0)
            {
                if (!int.TryParse(colsStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var col) || col < 0)
                    throw new FormatException($"Invalid col in segment '{token}'.");

                spans[idx++] = new SpriteSheetSpan(row, col, 1);
                continue;
            }

            var startStr = colsStr[..dash].Trim();
            var endStr = colsStr[(dash + 1)..].Trim();

            if (!int.TryParse(startStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var startCol) || startCol < 0)
                throw new FormatException($"Invalid start col in segment '{token}'.");

            if (!int.TryParse(endStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var endCol) || endCol < 0)
                throw new FormatException($"Invalid end col in segment '{token}'.");

            if (endCol < startCol)
                throw new FormatException($"Invalid range in segment '{token}'. End col must be >= start col.");

            spans[idx++] = new SpriteSheetSpan(row, startCol, endCol - startCol + 1);
        }

        return idx == spans.Length ? spans : spans[..idx];
    }
}
