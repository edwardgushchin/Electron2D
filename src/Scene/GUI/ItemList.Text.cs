namespace Electron2D;

public partial class ItemList
{
    /// <summary>Gets one item's text direction policy.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>Auto initially.</returns>
    public TextDirection GetItemTextDirection(int index) { ThrowIfDisposed(); return GetItem(index).Direction; }

    /// <summary>Changes one item's text direction and rebuilds its shaped text.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="direction">A defined direction or inherited layout direction.</param>
    public void SetItemTextDirection(int index, TextDirection direction)
    {
        EnsureMutable(); if (!Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(direction));
        var item = GetItem(index, negative: true);
        if (item.Direction == direction) return; item.Direction = direction; InvalidateListLayout();
    }

    /// <summary>Gets one item's language tag used for shaping.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>The stored tag, empty by default.</returns>
    public string GetItemLanguage(int index) { ThrowIfDisposed(); return GetItem(index).Language; }

    /// <summary>Changes one item's shaping language tag.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="language">The non-null language tag; empty uses the text service default.</param>
    public void SetItemLanguage(int index, string language)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(language);
        var item = GetItem(index, negative: true);
        if (item.Language == language) return; item.Language = language; InvalidateListLayout();
    }

    /// <summary>Gets one item's automatic translation policy.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>Inherit initially.</returns>
    public NodeAutoTranslateMode GetItemAutoTranslateMode(int index) { ThrowIfDisposed(); return GetItem(index).AutoTranslate; }

    /// <summary>Changes one item's automatic translation policy and rebuilds displayed text.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="mode">Inherit, Always or Disabled.</param>
    public void SetItemAutoTranslateMode(int index, NodeAutoTranslateMode mode)
    {
        EnsureMutable(); if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var item = GetItem(index, negative: true);
        if (item.AutoTranslate == mode) return; item.AutoTranslate = mode; InvalidateListLayout();
    }

    /// <summary>Gets one item's explicit tooltip text.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>The stored text, empty initially.</returns>
    public string GetItemTooltip(int index) { ThrowIfDisposed(); return GetItem(index).Tooltip; }

    /// <summary>Changes one item's explicit tooltip text.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="tooltip">The non-null tooltip text.</param>
    public void SetItemTooltip(int index, string tooltip)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(tooltip);
        var item = GetItem(index, negative: true);
        if (item.Tooltip == tooltip) return; item.Tooltip = tooltip; InvalidateListLayout();
    }

    /// <summary>Gets whether one item may show a tooltip.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>True initially.</returns>
    public bool IsItemTooltipEnabled(int index) { ThrowIfDisposed(); return GetItem(index).TooltipEnabled; }

    /// <summary>Changes whether one item may show a tooltip.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="enabled">The new policy.</param>
    public void SetItemTooltipEnabled(int index, bool enabled)
    {
        EnsureMutable(); GetItem(index, negative: true).TooltipEnabled = enabled;
    }

    /// <inheritdoc />
    protected override string OnGetTooltip(Vector2 atPosition)
    {
        var index = GetItemAtPosition(atPosition, exact: true);
        if (index >= 0)
        {
            var item = _items[index];
            if (!item.TooltipEnabled) return string.Empty;
            if (item.Tooltip.Length != 0) return item.Tooltip;
            if (item.Text.Length != 0) return item.Text;
        }
        return base.OnGetTooltip(atPosition);
    }
}
