namespace Electron2D;

public sealed partial class TreeItem
{
    private sealed record MetadataValue<T>(T Value);
    /// <summary>Stores a runtime cell payload under its exact generic type.</summary><typeparam name="T">Payload type.</typeparam><param name="column">Existing column.</param><param name="metadata">Borrowed payload.</param>
    public void SetMetadata<T>(int column, T metadata) { MutableItem(); At(column).Metadata = new MetadataValue<T>(metadata); }
    /// <summary>Retrieves a runtime cell payload of the exact generic type.</summary><typeparam name="T">Stored type.</typeparam><param name="column">Existing column.</param><returns>Borrowed payload.</returns>
    public T GetMetadata<T>(int column) => At(column).Metadata is MetadataValue<T> value ? value.Value : throw new KeyNotFoundException("No matching typed cell metadata.");
    /// <summary>Sets a cell foreground override.</summary><param name="column">Existing column.</param><param name="color">Finite color.</param>
    public void SetCustomColor(int column, Color color) { MutableItem(); if (!color.IsFinite()) throw new ArgumentException("Color must be finite.", nameof(color)); var cell = At(column); cell.Color = color; cell.HasColor = true; Changed(); }
    /// <summary>Returns the retained foreground color.</summary><param name="column">Existing column.</param><returns>Configured color.</returns>
    public Color GetCustomColor(int column) => At(column).Color;
    /// <summary>Disables the cell foreground override.</summary><param name="column">Existing column.</param>
    public void ClearCustomColor(int column) { MutableItem(); At(column).HasColor = false; Changed(); }
    /// <summary>Sets a cell background or outline override.</summary><param name="column">Existing column.</param><param name="color">Finite color.</param><param name="justOutline">Draws only an outline.</param>
    public void SetCustomBGColor(int column, Color color, bool justOutline = false) { MutableItem(); if (!color.IsFinite()) throw new ArgumentException("Color must be finite.", nameof(color)); var cell = At(column); cell.Background = color; cell.HasBackground = true; cell.BackgroundOutline = justOutline; Changed(); }
    /// <summary>Returns the retained background color.</summary><param name="column">Existing column.</param><returns>Configured color.</returns>
    public Color GetCustomBGColor(int column) => At(column).Background;
    /// <summary>Disables the background override.</summary><param name="column">Existing column.</param>
    public void ClearCustomBGColor(int column) { MutableItem(); At(column).HasBackground = false; Changed(); }
    /// <summary>Sets a draw callback borrowing this item and its local cell rectangle.</summary><param name="column">Existing column.</param><param name="callback">Callback, or null. Use GetTree().Draw* inside this scope.</param>
    public void SetCustomDrawCallback(int column, Action<TreeItem, Rect2>? callback) { MutableItem(); At(column).CustomDraw = callback; Changed(); }
    /// <summary>Returns the retained typed cell draw callback.</summary><param name="column">Existing column.</param><returns>Callback or null.</returns>
    public Action<TreeItem, Rect2>? GetCustomDrawCallback(int column) => At(column).CustomDraw;
    /// <summary>Configures finite numeric bounds, step and exponential editing scale without reclamping the retained value.</summary><param name="column">Existing column.</param><param name="min">Finite minimum.</param><param name="max">Finite maximum.</param><param name="step">Finite step; nonpositive disables snapping.</param><param name="expr">Exponential edit scale.</param>
    public void SetRangeConfig(int column, double min, double max, double step, bool expr = false) { MutableItem(); if (!double.IsFinite(min) || !double.IsFinite(max) || !double.IsFinite(step)) throw new ArgumentException("Range configuration must be finite."); var cell = At(column); cell.Min = min; cell.Max = max; cell.Step = step; cell.Exponential = expr; Changed(); }
    /// <summary>Returns the complete typed numeric configuration, including the configured scale policy.</summary><param name="column">Existing column.</param><returns>Minimum, maximum, step and exponential scale.</returns>
    public (double Min, double Max, double Step, bool Exponential) GetRangeConfig(int column) { var cell = At(column); return (cell.Min, cell.Max, cell.Step, cell.Exponential); }
    /// <summary>Sets the numeric value, snapping on an absolute zero-based grid then clamping.</summary><param name="column">Existing column.</param><param name="value">IEEE numeric value.</param>
    public void SetRange(int column, double value) { MutableItem(); var cell = At(column); if (cell.Step > 0 && double.IsFinite(value)) value = Math.Floor(value / cell.Step + .5) * cell.Step; if (value < cell.Min) value = cell.Min; if (value > cell.Max) value = cell.Max; if (cell.Value == value) return; cell.Value = value; Changed(); }
    /// <summary>Returns the retained numeric value.</summary><param name="column">Existing column.</param><returns>Numeric value, zero initially.</returns>
    public double GetRange(int column) => At(column).Value;
    /// <summary>Sets checkbox state and clears indeterminate state only when the checked state changes.</summary><param name="column">Existing column.</param><param name="value">Desired check state.</param>
    public void SetChecked(int column, bool value) { MutableItem(); var cell = At(column); if (cell.Checked == value) return; cell.Checked = value; cell.Indeterminate = false; Changed(); }
    /// <summary>Sets indeterminate state and clears checked state when this state changes.</summary><param name="column">Existing column.</param><param name="indeterminate">Desired tri-state state.</param>
    public void SetIndeterminate(int column, bool indeterminate) { MutableItem(); var cell = At(column); if (cell.Indeterminate == indeterminate) return; cell.Indeterminate = indeterminate; cell.Checked = false; Changed(); }
    /// <summary>Reports the checkbox state.</summary><param name="column">Existing column.</param><returns>Checked state.</returns>
    public bool IsChecked(int column) => At(column).Checked;
    /// <summary>Reports the indeterminate checkbox state.</summary><param name="column">Existing column.</param><returns>Indeterminate state.</returns>
    public bool IsIndeterminate(int column) => At(column).Indeterminate;
    /// <summary>Propagates a checkbox value through descendants and recomputes ancestor tri-state values.</summary><param name="column">Existing column.</param><param name="emitSignal">Publishes each propagated item through the owning Tree.</param>
    public void PropagateCheck(int column, bool emitSignal = true)
    {
        MutableItem(); var value = At(column).Checked; var affected = new List<TreeItem> { this }; for (var item = WalkNext(false, true); item != null && item.HasAncestor(this); item = item.WalkNext(false, true)) { item.SetChecked(column, value); affected.Add(item); }
        for (var parent = ParentItem; parent != null; parent = parent.ParentItem) { var checkedAny = false; var uncheckedAny = false; var mixed = false; foreach (var child in parent.Children) { var cell = child.At(column); checkedAny |= cell.Checked; uncheckedAny |= !cell.Checked; mixed |= cell.Indeterminate; } if (mixed || checkedAny && uncheckedAny) parent.SetIndeterminate(column, true); else if (parent.At(column).Indeterminate && !checkedAny) parent.SetIndeterminate(column, false); else parent.SetChecked(column, checkedAny); affected.Add(parent); }
        List<Exception>? errors = null; if (emitSignal) foreach (var item in affected) if (!item.IsDisposed) try { item.Owner?.PublishCheck(item, column); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Tree check propagation callbacks failed.", errors);
    }
    /// <summary>Selects this cell through its owning control.</summary><param name="column">Existing column.</param><param name="setAsCursor">Sets the selection cursor.</param>
    public void Select(int column, bool setAsCursor = true) { MutableItem(); At(column); if (Owner == null) throw new InvalidOperationException("Selection requires an owning Tree."); Owner.SelectItemCell(this, column, true, setAsCursor, false); }
    /// <summary>Deselects this cell through its owning control.</summary><param name="column">Existing column.</param>
    public void Deselect(int column) { MutableItem(); var cell = At(column); if (Owner == null) { cell.Selected = false; return; } Owner.SelectItemCell(this, column, false, false, false); }
    /// <summary>Reports this cell's selection state.</summary><param name="column">Existing column.</param><returns>Selection state.</returns>
    public bool IsSelected(int column) { var cell = At(column); return cell.Selectable && cell.Selected; }
}
