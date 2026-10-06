namespace Electron2D;

public sealed partial class TreeItem
{
    private CellButton ButtonAt(int column, int index) { var cell = At(column); if ((uint)index >= cell.Buttons.Count) throw new ArgumentOutOfRangeException(nameof(index)); return cell.Buttons[index]; }
    /// <summary>Adds a borrowed icon button to a cell.</summary><param name="column">Existing column.</param><param name="button">Live texture.</param><param name="id">Signal ID, or -1 for the current button index.</param><param name="disabled">Disables activation.</param><param name="tooltipText">Source tooltip.</param><param name="description">Semantic description.</param>
    public void AddButton(int column, Texture button, int id = -1, bool disabled = false, string tooltipText = "", string description = "") { MutableItem(); ArgumentNullException.ThrowIfNull(button); ArgumentNullException.ThrowIfNull(tooltipText); ArgumentNullException.ThrowIfNull(description); if (button.IsDisposed) throw new ObjectDisposedException(nameof(button)); var cell = At(column); cell.Buttons.Add(new() { Texture = button, ID = id == -1 ? cell.Buttons.Count : id, Disabled = disabled, Tooltip = tooltipText, Description = description }); Changed(); }
    /// <summary>Clears all buttons on this row.</summary>
    public void ClearButtons() { MutableItem(); foreach (var cell in Cells) cell.Buttons.Clear(); Changed(); }
    /// <summary>Removes a button by its cell index.</summary><param name="column">Existing column.</param><param name="buttonIndex">Existing button index.</param>
    public void EraseButton(int column, int buttonIndex) { MutableItem(); ButtonAt(column, buttonIndex); At(column).Buttons.RemoveAt(buttonIndex); Changed(); }
    /// <summary>Returns cell button count.</summary><param name="column">Existing column.</param><returns>Count.</returns>
    public int GetButtonCount(int column) => At(column).Buttons.Count;
    /// <summary>Returns the first index with an exact signal ID.</summary><param name="column">Existing column.</param><param name="id">Signal ID.</param><returns>Index or -1.</returns>
    public int GetButtonByID(int column, int id) => At(column).Buttons.FindIndex(b => b.ID == id);
    /// <summary>Returns a button's retained signal ID.</summary><param name="column">Existing column.</param><param name="buttonIndex">Button index.</param><returns>Signal ID.</returns>
    public int GetButtonID(int column, int buttonIndex) => ButtonAt(column, buttonIndex).ID;
    /// <summary>Returns a live borrowed button texture.</summary><param name="column">Existing column.</param><param name="buttonIndex">Button index.</param><returns>Texture or null after disposal.</returns>
    public Texture? GetButton(int column, int buttonIndex) => ButtonAt(column, buttonIndex).Texture is { IsDisposed: false } texture ? texture : null;
    /// <summary>Replaces a button's borrowed texture.</summary><param name="column">Existing column.</param><param name="buttonIndex">Button index.</param><param name="button">Live texture.</param>
    public void SetButton(int column, int buttonIndex, Texture button) { MutableItem(); ArgumentNullException.ThrowIfNull(button); if (button.IsDisposed) throw new ObjectDisposedException(nameof(button)); ButtonAt(column, buttonIndex).Texture = button; Changed(); }
    /// <summary>Returns the button tint by button index.</summary><param name="column">Existing column.</param><param name="id">Button index despite the reference parameter name.</param><returns>Finite color.</returns>
    public Color GetButtonColor(int column, int id) => ButtonAt(column, id).Color;
    /// <summary>Sets a finite button tint.</summary><param name="column">Existing column.</param><param name="buttonIndex">Button index.</param><param name="color">Finite color.</param>
    public void SetButtonColor(int column, int buttonIndex, Color color) { MutableItem(); if (!color.IsFinite()) throw new ArgumentException("Color must be finite.", nameof(color)); ButtonAt(column, buttonIndex).Color = color; Changed(); }
    /// <summary>Sets button activation policy.</summary><param name="column">Existing column.</param><param name="buttonIndex">Button index.</param><param name="disabled">Disabled state.</param>
    public void SetButtonDisabled(int column, int buttonIndex, bool disabled) { MutableItem(); ButtonAt(column, buttonIndex).Disabled = disabled; Changed(); }
    /// <summary>Reports the button activation policy.</summary><param name="column">Existing column.</param><param name="buttonIndex">Button index.</param><returns>Disabled state.</returns>
    public bool IsButtonDisabled(int column, int buttonIndex) => ButtonAt(column, buttonIndex).Disabled;
    /// <summary>Returns source button tooltip text.</summary><param name="column">Existing column.</param><param name="buttonIndex">Button index.</param><returns>Source tooltip.</returns>
    public string GetButtonTooltipText(int column, int buttonIndex) => ButtonAt(column, buttonIndex).Tooltip;
    /// <summary>Sets source button tooltip text.</summary><param name="column">Existing column.</param><param name="buttonIndex">Button index.</param><param name="tooltip">Nonnull tooltip.</param>
    public void SetButtonTooltipText(int column, int buttonIndex, string tooltip) { MutableItem(); ArgumentNullException.ThrowIfNull(tooltip); ButtonAt(column, buttonIndex).Tooltip = tooltip; }
    /// <summary>Sets source button semantic description.</summary><param name="column">Existing column.</param><param name="buttonIndex">Button index.</param><param name="description">Nonnull description.</param>
    public void SetButtonDescription(int column, int buttonIndex, string description) { MutableItem(); ArgumentNullException.ThrowIfNull(description); ButtonAt(column, buttonIndex).Description = description; }
}
