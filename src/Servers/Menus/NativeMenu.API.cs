namespace Electron2D;

public sealed partial class NativeMenu
{
    /// <summary>Reports native-menu capabilities on the active display.</summary>
    public enum Feature
    {
        /// <summary>A system global menu bar.</summary>
        GlobalMenu = 0,
        /// <summary>Operating-system popup menus.</summary>
        PopupMenu = 1,
        /// <summary>Native open and close notification.</summary>
        OpenCloseCallback = 2,
        /// <summary>Native mouse-hover notification.</summary>
        HoverCallback = 3,
        /// <summary>Dedicated accelerator callbacks.</summary>
        KeyCallback = 4,
    }
    /// <summary>Reports whether a native display session exists; query Feature for its menu capabilities.</summary>
    /// <value>True while the display session is published; this observation does not reserve its lifetime.</value>
    public static bool IsAvailable => DisplayServer.IsAvailable;
    /// <summary>Tests one defined native capability.</summary>
    /// <param name="feature">One defined capability.</param>
    /// <returns>Whether its native producer executes on this display.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static bool HasFeature(Feature feature) { var owner = Require(); owner.Check(); if ((uint)feature > 4) throw new ArgumentOutOfRangeException(nameof(feature)); return owner._display.SupportsNativeMenus && feature is Feature.PopupMenu or Feature.OpenCloseCallback or Feature.HoverCallback; }
    /// <summary>Creates a caller-owned, initially empty native popup menu.</summary>
    /// <returns>A stable menu identity until FreeMenu or display retirement.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static RID CreateMenu() => Require().CreateCore();
    /// <summary>Checks the active owner without treating a stale RID as live.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <returns>Whether this exact menu is still owned.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static bool HasMenu(RID rid) { var owner = Require(); owner.Check(); return owner._menus.ContainsKey(rid); }
    /// <summary>Frees an unattached menu and its item/bitmap ownership; submenus remain live.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void FreeMenu(RID rid) => Require().FreeCore(rid);
    /// <summary>Removes every item without freeing detached submenus.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void Clear(RID rid) { var owner = Require(); owner.Writable(); owner.ClearCore(owner.Get(rid)); }
    /// <summary>Returns logical item count, including hidden items.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <returns>The current count.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int GetItemCount(RID rid) => Require().Get(rid).Items.Count;
    /// <summary>Returns the last measured native geometry; zero before native layout.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <returns>Measured width and height in desktop pixels.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static Vector2 GetSize(RID rid) => Require().Get(rid).Size;
    /// <summary>Reports actual native initialization/tracking state.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <returns>Whether the native menu loop has opened this menu.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static bool IsOpened(RID rid) => Require().Get(rid).Opened;
    /// <summary>Tracks a real native popup at an absolute desktop pixel position.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="position">Absolute desktop pixels, with native edge fitting.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void Popup(RID rid, Vector2i position) => Require().Present(rid, position);
    /// <summary>Selects the actual native popup layout direction.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="isRTL">True for right-to-left native layout.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetInterfaceDirection(RID rid, bool isRTL) { var owner = Require(); owner.Writable(); owner.Get(rid).RTL = isRTL; }
    /// <summary>Returns the original native open callback.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <returns>The registered delegate, or null.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static Action? GetPopupOpenCallback(RID rid) => Require().Get(rid).Open;
    /// <summary>Sets the native open notification; callbacks run on the display owner.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="callback">The callback, or null to clear it.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetPopupOpenCallback(RID rid, Action? callback) { var owner = Require(); owner.Writable(); owner.Get(rid).Open = callback; }
    /// <summary>Returns the original native close callback.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <returns>The registered delegate, or null.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static Action? GetPopupCloseCallback(RID rid) => Require().Get(rid).Close;
    /// <summary>Sets the native close notification; callbacks run on the display owner.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="callback">The callback, or null to clear it.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetPopupCloseCallback(RID rid, Action? callback) { var owner = Require(); owner.Writable(); owner.Get(rid).Close = callback; }
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddItem(RID rid, string label, Action? callback = null, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback(callback), null, 0, false, null, default, 0, 0, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="tag">A borrowed typed callback value.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddItem<T>(RID rid, string label, T tag, Action<T>? callback, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback<T>(callback), new TagValue<T>(tag), 0, false, null, default, 0, 0, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddCheckItem(RID rid, string label, Action? callback = null, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback(callback), null, 1, false, null, default, 0, 0, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="tag">A borrowed typed callback value.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddCheckItem<T>(RID rid, string label, T tag, Action<T>? callback, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback<T>(callback), new TagValue<T>(tag), 1, false, null, default, 0, 0, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddRadioCheckItem(RID rid, string label, Action? callback = null, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback(callback), null, 2, false, null, default, 0, 0, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="tag">A borrowed typed callback value.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddRadioCheckItem<T>(RID rid, string label, T tag, Action<T>? callback, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback<T>(callback), new TagValue<T>(tag), 2, false, null, default, 0, 0, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="icon">A live borrowed image-readable texture.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddIconItem(RID rid, Texture icon, string label, Action? callback = null, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback(callback), null, 0, false, icon, default, 0, 0, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="icon">A live borrowed image-readable texture.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="tag">A borrowed typed callback value.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddIconItem<T>(RID rid, Texture icon, string label, T tag, Action<T>? callback, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback<T>(callback), new TagValue<T>(tag), 0, false, icon, default, 0, 0, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="icon">A live borrowed image-readable texture.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddIconCheckItem(RID rid, Texture icon, string label, Action? callback = null, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback(callback), null, 1, false, icon, default, 0, 0, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="icon">A live borrowed image-readable texture.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="tag">A borrowed typed callback value.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddIconCheckItem<T>(RID rid, Texture icon, string label, T tag, Action<T>? callback, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback<T>(callback), new TagValue<T>(tag), 1, false, icon, default, 0, 0, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="icon">A live borrowed image-readable texture.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddIconRadioCheckItem(RID rid, Texture icon, string label, Action? callback = null, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback(callback), null, 2, false, icon, default, 0, 0, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="icon">A live borrowed image-readable texture.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="tag">A borrowed typed callback value.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddIconRadioCheckItem<T>(RID rid, Texture icon, string label, T tag, Action<T>? callback, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback<T>(callback), new TagValue<T>(tag), 2, false, icon, default, 0, 0, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="maxStates">Application state-count metadata.</param>
    /// <param name="defaultState">Initial application state; selection does not cycle it automatically.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddMultistateItem(RID rid, string label, int maxStates, int defaultState, Action? callback = null, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback(callback), null, 0, false, null, default, maxStates, defaultState, index);
    /// <summary>Inserts a native menu item and retains its exact typed callback state.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="label">Nonnull native UTF16 label, without embedded zero characters.</param>
    /// <param name="maxStates">Application state-count metadata.</param>
    /// <param name="defaultState">Initial application state; selection does not cycle it automatically.</param>
    /// <param name="tag">A borrowed typed callback value.</param>
    /// <param name="callback">The activation delegate, or null.</param>
    /// <param name="index">Insertion position; -1 appends and other values clamp to the current bounds.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddMultistateItem<T>(RID rid, string label, int maxStates, int defaultState, T tag, Action<T>? callback, int index = -1) => Require().Add(rid, label, callback is null ? null : new Callback<T>(callback), new TagValue<T>(tag), 0, false, null, default, maxStates, defaultState, index);
    /// <summary>Inserts a native separator.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">Insertion position; -1 appends.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddSeparator(RID rid, int index = -1) => Require().Add(rid, "", null, null, 0, true, null, default, 0, 0, index);
    /// <summary>Attaches an independent submenu with one parent and acyclic ownership.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="label">The native submenu label.</param>
    /// <param name="submenu">An unattached live submenu.</param>
    /// <param name="index">Insertion position; -1 appends.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddSubmenuItem(RID rid, string label, RID submenu, int index = -1) { if (!submenu.IsValid()) throw new ArgumentException("A submenu identity is required.", nameof(submenu)); return Require().Add(rid, label, null, null, 0, false, null, submenu, 0, 0, index); }
    /// <summary>Attaches an independent submenu with one parent and acyclic ownership.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="label">The native submenu label.</param>
    /// <param name="submenu">An unattached live submenu.</param>
    /// <param name="tag">Borrowed exact-type metadata.</param>
    /// <param name="index">Insertion position; -1 appends.</param>
    /// <returns>The inserted logical index.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int AddSubmenuItem<T>(RID rid, string label, RID submenu, T tag, int index = -1) { if (!submenu.IsValid()) throw new ArgumentException("A submenu identity is required.", nameof(submenu)); return Require().Add(rid, label, null, new TagValue<T>(tag), 0, false, null, submenu, 0, 0, index); }
    /// <summary>Returns the current item text value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The retained typed value; returned resources remain borrowed.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static string GetItemText(RID rid, int index) => Require().At(rid, index).Text;
    /// <summary>Returns the current item checked value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The retained typed value; returned resources remain borrowed.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static bool IsItemChecked(RID rid, int index) => Require().At(rid, index).Checked;
    /// <summary>Returns the current item disabled value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The retained typed value; returned resources remain borrowed.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static bool IsItemDisabled(RID rid, int index) => Require().At(rid, index).Disabled;
    /// <summary>Returns the current item hidden value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The retained typed value; returned resources remain borrowed.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static bool IsItemHidden(RID rid, int index) => Require().At(rid, index).Hidden;
    /// <summary>Returns the current item checkable value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The retained typed value; returned resources remain borrowed.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static bool IsItemCheckable(RID rid, int index) => Require().At(rid, index).Checkable != 0;
    /// <summary>Returns the current item radiocheckable value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The retained typed value; returned resources remain borrowed.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static bool IsItemRadioCheckable(RID rid, int index) => Require().At(rid, index).Checkable == 2;
    /// <summary>Returns the current item state value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The retained typed value; returned resources remain borrowed.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int GetItemState(RID rid, int index) => Require().At(rid, index).State;
    /// <summary>Returns the current item maxstates value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The retained typed value; returned resources remain borrowed.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int GetItemMaxStates(RID rid, int index) => Require().At(rid, index).MaxStates;
    /// <summary>Returns the current item submenu value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The retained typed value; returned resources remain borrowed.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static RID GetItemSubmenu(RID rid, int index) => Require().At(rid, index).Submenu?.RID ?? default;
    /// <summary>Returns the current item icon value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The retained typed value; returned resources remain borrowed.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static Texture? GetItemIcon(RID rid, int index) => Require().At(rid, index).Icon is { IsDisposed: false } icon ? icon : null;
    /// <summary>Changes the native item text value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="value">The new value; icon and submenu inputs remain borrowed.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemText(RID rid, int index, string value) => Require().SetTextCore(rid, index, value);
    /// <summary>Changes the native item checked value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="value">The new value; icon and submenu inputs remain borrowed.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemChecked(RID rid, int index, bool value) => Require().SetBool(rid, index, value, 0);
    /// <summary>Changes the native item disabled value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="value">The new value; icon and submenu inputs remain borrowed.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemDisabled(RID rid, int index, bool value) => Require().SetBool(rid, index, value, 1);
    /// <summary>Changes the native item hidden value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="value">The new value; icon and submenu inputs remain borrowed.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemHidden(RID rid, int index, bool value) => Require().HideCore(rid, index, value);
    /// <summary>Changes the native item checkable value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="value">The new value; icon and submenu inputs remain borrowed.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemCheckable(RID rid, int index, bool value) => Require().SetCheck(rid, index, value, 1);
    /// <summary>Changes the native item radiocheckable value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="value">The new value; icon and submenu inputs remain borrowed.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemRadioCheckable(RID rid, int index, bool value) => Require().SetCheck(rid, index, value, 2);
    /// <summary>Changes the native item icon value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="value">The new value; icon and submenu inputs remain borrowed.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemIcon(RID rid, int index, Texture? value) => Require().IconCore(rid, index, value);
    /// <summary>Changes the native item submenu value.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="value">The new value; icon and submenu inputs remain borrowed.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemSubmenu(RID rid, int index, RID value) => Require().SubmenuCore(rid, index, value);
    /// <summary>Changes explicit application state; native selection does not toggle it.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="value">The stored integer value.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemState(RID rid, int index, int value) { var owner = Require(); owner.Writable(); owner.At(rid, index).State = value; }
    /// <summary>Changes explicit application state; native selection does not toggle it.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="value">The stored integer value.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemMaxStates(RID rid, int index, int value) { var owner = Require(); owner.Writable(); owner.At(rid, index).MaxStates = value; }
    /// <summary>Moves an item while preserving command identity, callbacks and submenu ownership.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="targetIndex">A valid destination logical index.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemIndex(RID rid, int index, int targetIndex) => Require().Reorder(rid, index, targetIndex);
    /// <summary>Removes one item and shifts following logical indices.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void RemoveItem(RID rid, int index) { var owner = Require(); owner.Writable(); _ = owner.At(rid, index); owner.RemoveCore(owner.Get(rid), index); }
    /// <summary>Finds the first exact ordinal label.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="text">A nonnull label.</param>
    /// <returns>The logical index, or -1.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int FindItemIndexWithText(RID rid, string text) { Text(text); var menu = Require().Get(rid); for (var i = 0; i < menu.Items.Count; i++) if (menu.Items[i].Text == text) return i; return -1; }
    /// <summary>Finds the first attached submenu identity.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="submenu">A submenu identity.</param>
    /// <returns>The logical index, or -1.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int FindItemIndexWithSubmenu(RID rid, RID submenu) { var menu = Require().Get(rid); for (var i = 0; i < menu.Items.Count; i++) if (menu.Items[i].Submenu?.RID == submenu) return i; return -1; }
    /// <summary>Replaces metadata only when its exact type agrees with registered typed callbacks.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="value">The borrowed tag value.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemTag<T>(RID rid, int index, T value) { var owner = Require(); owner.Writable(); var item = owner.At(rid, index); ValidateTag(item, typeof(T)); item.Tag = new TagValue<T>(value); }
    /// <summary>Returns one exact-type metadata value.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The borrowed tag value.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    /// <exception cref="KeyNotFoundException">No tag of the exact requested generic type is stored.</exception>
    public static T GetItemTag<T>(RID rid, int index) => Require().At(rid, index).Tag is TagValue<T> value ? value.Value : throw new KeyNotFoundException("No tag of the requested exact type.");
    /// <summary>Finds metadata by exact generic type and typed equality.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="tag">The search value.</param>
    /// <returns>The logical index, or -1.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static int FindItemIndexWithTag<T>(RID rid, T tag) { var menu = Require().Get(rid); for (var i = 0; i < menu.Items.Count; i++) if (menu.Items[i].Tag is TagValue<T> value && EqualityComparer<T>.Default.Equals(value.Value, tag)) return i; return -1; }
    /// <summary>Returns the original delegate with its exact signature.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The activation delegate, or null.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static Action? GetItemCallback(RID rid, int index) { var callback = Require().At(rid, index).Callback; return callback is null ? null : callback is Callback typed ? typed.Value : throw new InvalidOperationException("The registered callback has another signature."); }
    /// <summary>Registers a native owner-thread callback with exact tag validation.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="callback">The delegate, or null to clear it.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemCallback(RID rid, int index, Action? callback) { var owner = Require(); owner.Writable(); var item = owner.At(rid, index); ICallback? value = callback is null ? null : new Callback(callback); ValidateCallback(item, value); item.Callback = value; }
    /// <summary>Registers a native owner-thread callback with exact tag validation.</summary>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="callback">The delegate, or null to clear it.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemHoverCallbacks(RID rid, int index, Action? callback) { var owner = Require(); owner.Writable(); var item = owner.At(rid, index); ICallback? value = callback is null ? null : new Callback(callback); ValidateCallback(item, value); item.Hover = value; }
    /// <summary>Returns the original delegate with its exact signature.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <returns>The activation delegate, or null.</returns>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static Action<T>? GetItemCallback<T>(RID rid, int index) { var callback = Require().At(rid, index).Callback; return callback is null ? null : callback is Callback<T> typed ? typed.Value : throw new InvalidOperationException("The registered callback has another signature."); }
    /// <summary>Registers a native owner-thread callback with exact tag validation.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="callback">The delegate, or null to clear it.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemCallback<T>(RID rid, int index, Action<T>? callback) { var owner = Require(); owner.Writable(); var item = owner.At(rid, index); ICallback? value = callback is null ? null : new Callback<T>(callback); ValidateCallback(item, value); item.Callback = value; }
    /// <summary>Registers a native owner-thread callback with exact tag validation.</summary>
    /// <typeparam name="T">The exact runtime tag and callback value type.</typeparam>
    /// <param name="rid">A live menu owned by the active display.</param>
    /// <param name="index">A nonnegative logical item index, including hidden items.</param>
    /// <param name="callback">The delegate, or null to clear it.</param>
    /// <exception cref="InvalidOperationException">The display, owner thread or active native presentation rejects the operation.</exception>
    /// <exception cref="ArgumentException">An identity, index, text or tag type is invalid.</exception>
    public static void SetItemHoverCallbacks<T>(RID rid, int index, Action<T>? callback) { var owner = Require(); owner.Writable(); var item = owner.At(rid, index); ICallback? value = callback is null ? null : new Callback<T>(callback); ValidateCallback(item, value); item.Hover = value; }
}
