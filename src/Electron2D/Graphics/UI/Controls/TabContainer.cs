/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace Electron2D;

/// <summary>
/// Displays direct child controls as selectable tab pages.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>TabContainer</c> is a runtime UI container. Each direct child
/// <see cref="Control"/> becomes a tab page; non-control children remain in the
/// node tree but are ignored by tab layout and tab metadata queries.
/// </para>
/// <para>
/// The 0.1-preview implementation stores tab titles, icons, disabled state
/// and hidden state, routes pointer presses on the tab header and fits the
/// selected page into the remaining rectangle.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate tab containers on the main
/// scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Container"/>
/// <seealso cref="Control"/>
public class TabContainer : Container
{
    private const int DefaultTabHeight = 24;
    private const int DefaultTabWidth = 64;
    private readonly Dictionary<Control, TabData> tabData = new();
    private int currentTab = -1;
    private int previousTab = -1;
    private bool tabsVisible = true;

    /// <summary>
    /// Identifies how tabs are aligned inside the tab header.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The preview stores this value and uses it when calculating tab header
    /// hit rectangles.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This enum is immutable and is safe to use from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TabAlignment"/>
    public enum AlignmentModeEnum
    {
        /// <summary>
        /// Places tabs at the beginning of the header.
        /// </summary>
        ///
        /// <remarks>
        /// Use this value when the first tab should start at the left edge.
        /// </remarks>
        ///
        /// <since>
        /// This value is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="AlignmentModeEnum"/>
        Left = 0,

        /// <summary>
        /// Centers tabs inside the header.
        /// </summary>
        ///
        /// <remarks>
        /// Centering affects hit testing and drawing only when the header is
        /// wider than all visible tab buttons.
        /// </remarks>
        ///
        /// <since>
        /// This value is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="AlignmentModeEnum"/>
        Center = 1,

        /// <summary>
        /// Places tabs at the end of the header.
        /// </summary>
        ///
        /// <remarks>
        /// Use this value when tab buttons should be right-aligned.
        /// </remarks>
        ///
        /// <since>
        /// This value is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="AlignmentModeEnum"/>
        Right = 2
    }

    /// <summary>
    /// Identifies where the tab header is placed.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The header can be above or below the selected page. Page layout is
    /// recalculated during the container sort pass.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This enum is immutable and is safe to use from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TabsPosition"/>
    public enum TabPositionEnum
    {
        /// <summary>
        /// Places the tab header above the selected page.
        /// </summary>
        ///
        /// <remarks>
        /// This is the default tab position.
        /// </remarks>
        ///
        /// <since>
        /// This value is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TabPositionEnum"/>
        Top = 0,

        /// <summary>
        /// Places the tab header below the selected page.
        /// </summary>
        ///
        /// <remarks>
        /// Use this value when page content should occupy the top of the
        /// container.
        /// </remarks>
        ///
        /// <since>
        /// This value is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="TabPositionEnum"/>
        Bottom = 1
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TabContainer"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The constructor registers the <c>tab_changed</c> signal and enables
    /// clipping so tab pages cannot draw outside the container rectangle.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the main scene
    /// thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TabContainer"/>
    public TabContainer()
    {
        ClipContents = true;
        AddUserSignal("tab_changed");
    }

    /// <summary>
    /// Gets or sets the current selected tab index.
    /// </summary>
    ///
    /// <value>
    /// The current tab index, or <c>-1</c> when no tab is selected.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Setting this property updates page visibility and emits
    /// <c>tab_changed</c> when the selected tab changes.
    /// </para>
    /// <para>
    /// Disabled and hidden tabs are not selected. Assigning <c>-1</c> is
    /// accepted only when <see cref="DeselectEnabled"/> is <c>true</c>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned index is outside the tab range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetCurrentTabControl"/>
    /// <seealso cref="GetPreviousTab"/>
    public int CurrentTab
    {
        get
        {
            ThrowIfFreed();
            EnsureCurrentTab();
            return currentTab;
        }
        set
        {
            ThrowIfFreed();
            EnsureCurrentTab();
            SetCurrentTab(value, emitSignal: true);
        }
    }

    /// <summary>
    /// Gets or sets whether the tab header is visible.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to draw and hit-test the tab header; otherwise,
    /// <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// Hiding the header does not change the selected page.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TabsPosition"/>
    public bool TabsVisible
    {
        get
        {
            ThrowIfFreed();
            return tabsVisible;
        }
        set
        {
            ThrowIfFreed();
            if (tabsVisible == value)
            {
                return;
            }

            tabsVisible = value;
            QueueSort();
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets whether all tab buttons should be drawn in front.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to request all tabs in front; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// The preview stores this styling policy. Page visibility still follows
    /// <see cref="CurrentTab"/>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="_Draw"/>
    public bool AllTabsInFront { get; set; }

    /// <summary>
    /// Gets or sets whether the container may have no selected tab.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to allow <see cref="CurrentTab"/> to be <c>-1</c>;
    /// otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// When this value is <c>false</c>, the container selects the first
    /// available tab whenever possible.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="CurrentTab"/>
    public bool DeselectEnabled { get; set; }

    /// <summary>
    /// Gets or sets tab button alignment inside the header.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="AlignmentModeEnum"/>. The default is
    /// <see cref="AlignmentModeEnum.Left"/>.
    /// </value>
    ///
    /// <remarks>
    /// Changing alignment queues a redraw because tab button hit rectangles
    /// and drawing positions use the same calculation.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetTabIdxAtPoint(Vector2)"/>
    public AlignmentModeEnum TabAlignment { get; set; }

    /// <summary>
    /// Gets or sets the tab header position.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="TabPositionEnum"/>. The default is
    /// <see cref="TabPositionEnum.Top"/>.
    /// </value>
    ///
    /// <remarks>
    /// Changing this value queues a layout pass and redraw.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TabsVisible"/>
    public TabPositionEnum TabsPosition { get; set; }

    /// <summary>
    /// Gets or sets whether hidden tabs contribute to minimum size.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to measure hidden tab pages; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// This setting affects <see cref="_GetMinimumSize"/> only. Hidden tabs
    /// remain unavailable for selection.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetTabHidden(int, bool)"/>
    public bool UseHiddenTabsForMinSize { get; set; }

    /// <summary>
    /// Gets the number of direct child controls treated as tabs.
    /// </summary>
    ///
    /// <returns>
    /// The current tab count.
    /// </returns>
    ///
    /// <remarks>
    /// Non-control children are ignored by this count.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetTabControl(int)"/>
    public int GetTabCount()
    {
        ThrowIfFreed();
        EnsureCurrentTab();
        return GetTabs().Count;
    }

    /// <summary>
    /// Gets the control displayed by a tab.
    /// </summary>
    ///
    /// <param name="tabIndex">The zero-based tab index.</param>
    ///
    /// <returns>
    /// The direct child <see cref="Control"/> for the tab.
    /// </returns>
    ///
    /// <remarks>
    /// The returned control remains owned by the node tree. Removing or moving
    /// it changes subsequent tab indices.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tabIndex"/> is outside the tab range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetCurrentTabControl"/>
    public Control GetTabControl(int tabIndex)
    {
        ThrowIfFreed();
        return GetTab(tabIndex);
    }

    /// <summary>
    /// Gets the control displayed by the current tab.
    /// </summary>
    ///
    /// <returns>
    /// The selected tab control, or <c>null</c> when no tab is selected.
    /// </returns>
    ///
    /// <remarks>
    /// This method normalizes <see cref="CurrentTab"/> before returning the
    /// control.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="CurrentTab"/>
    public Control? GetCurrentTabControl()
    {
        ThrowIfFreed();
        EnsureCurrentTab();
        var tabs = GetTabs();
        return currentTab >= 0 && currentTab < tabs.Count ? tabs[currentTab] : null;
    }

    /// <summary>
    /// Gets the tab index that was selected before the current tab.
    /// </summary>
    ///
    /// <returns>
    /// The previous tab index, or <c>-1</c> when there was no previous tab.
    /// </returns>
    ///
    /// <remarks>
    /// The value is updated only when the selected tab changes.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="CurrentTab"/>
    public int GetPreviousTab()
    {
        ThrowIfFreed();
        return previousTab;
    }

    /// <summary>
    /// Sets the title displayed by a tab button.
    /// </summary>
    ///
    /// <param name="tabIndex">The zero-based tab index.</param>
    /// <param name="title">The title to display.</param>
    ///
    /// <remarks>
    /// Empty titles are valid. When no title is assigned, the child control
    /// name is used as a fallback by <see cref="GetTabTitle"/>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="title"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tabIndex"/> is outside the tab range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetTabTitle(int)"/>
    public void SetTabTitle(int tabIndex, string title)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(title);
        var data = GetData(GetTab(tabIndex));
        data.Title = title;
        data.HasTitle = true;
        QueueRedraw();
    }

    /// <summary>
    /// Gets the title displayed by a tab button.
    /// </summary>
    ///
    /// <param name="tabIndex">The zero-based tab index.</param>
    ///
    /// <returns>
    /// The assigned tab title, or the child control name when no title was
    /// assigned.
    /// </returns>
    ///
    /// <remarks>
    /// The returned value is never <c>null</c>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tabIndex"/> is outside the tab range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetTabTitle(int, string)"/>
    public string GetTabTitle(int tabIndex)
    {
        ThrowIfFreed();
        var tab = GetTab(tabIndex);
        var data = GetData(tab);
        return data.HasTitle ? data.Title : tab.Name;
    }

    /// <summary>
    /// Sets the icon displayed by a tab button.
    /// </summary>
    ///
    /// <param name="tabIndex">The zero-based tab index.</param>
    /// <param name="icon">The icon texture, or <c>null</c> to clear it.</param>
    ///
    /// <remarks>
    /// Icons are metadata for tab drawing and generated documentation. The
    /// preview renderer may omit icon drawing when no texture backend consumes
    /// the canvas command.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tabIndex"/> is outside the tab range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetTabIcon(int)"/>
    public void SetTabIcon(int tabIndex, Texture2D? icon)
    {
        ThrowIfFreed();
        GetData(GetTab(tabIndex)).Icon = icon;
        QueueRedraw();
    }

    /// <summary>
    /// Gets the icon displayed by a tab button.
    /// </summary>
    ///
    /// <param name="tabIndex">The zero-based tab index.</param>
    ///
    /// <returns>
    /// The icon texture, or <c>null</c> when no icon is assigned.
    /// </returns>
    ///
    /// <remarks>
    /// The returned texture is the same resource assigned by
    /// <see cref="SetTabIcon"/>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tabIndex"/> is outside the tab range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetTabIcon(int, Texture2D?)"/>
    public Texture2D? GetTabIcon(int tabIndex)
    {
        ThrowIfFreed();
        return GetData(GetTab(tabIndex)).Icon;
    }

    /// <summary>
    /// Sets whether a tab is disabled.
    /// </summary>
    ///
    /// <param name="tabIndex">The zero-based tab index.</param>
    /// <param name="disabled"><c>true</c> to disable the tab; otherwise, <c>false</c>.</param>
    ///
    /// <remarks>
    /// Disabled tabs remain visible but cannot be selected through pointer
    /// input or next/previous selection helpers.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tabIndex"/> is outside the tab range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="IsTabDisabled(int)"/>
    public void SetTabDisabled(int tabIndex, bool disabled)
    {
        ThrowIfFreed();
        EnsureCurrentTab();
        var tab = GetTab(tabIndex);
        GetData(tab).Disabled = disabled;
        if (disabled && currentTab == tabIndex)
        {
            SelectReplacementTab();
        }

        QueueRedraw();
    }

    /// <summary>
    /// Reports whether a tab is disabled.
    /// </summary>
    ///
    /// <param name="tabIndex">The zero-based tab index.</param>
    ///
    /// <returns>
    /// <c>true</c> when the tab is disabled; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Disabled tabs are still counted by <see cref="GetTabCount"/>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tabIndex"/> is outside the tab range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetTabDisabled(int, bool)"/>
    public bool IsTabDisabled(int tabIndex)
    {
        ThrowIfFreed();
        return GetData(GetTab(tabIndex)).Disabled;
    }

    /// <summary>
    /// Sets whether a tab is hidden.
    /// </summary>
    ///
    /// <param name="tabIndex">The zero-based tab index.</param>
    /// <param name="hidden"><c>true</c> to hide the tab; otherwise, <c>false</c>.</param>
    ///
    /// <remarks>
    /// Hidden tabs are not drawn in the header and cannot be selected. Their
    /// pages are also hidden unless selected again after being shown.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tabIndex"/> is outside the tab range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="IsTabHidden(int)"/>
    public void SetTabHidden(int tabIndex, bool hidden)
    {
        ThrowIfFreed();
        EnsureCurrentTab();
        var tab = GetTab(tabIndex);
        GetData(tab).Hidden = hidden;
        if (hidden && currentTab == tabIndex)
        {
            SelectReplacementTab();
        }
        else
        {
            ApplyTabVisibility();
        }

        QueueSort();
        QueueRedraw();
    }

    /// <summary>
    /// Reports whether a tab is hidden.
    /// </summary>
    ///
    /// <param name="tabIndex">The zero-based tab index.</param>
    ///
    /// <returns>
    /// <c>true</c> when the tab is hidden; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Hidden tabs remain part of the child list and keep their metadata.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="tabIndex"/> is outside the tab range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetTabHidden(int, bool)"/>
    public bool IsTabHidden(int tabIndex)
    {
        ThrowIfFreed();
        return GetData(GetTab(tabIndex)).Hidden;
    }

    /// <summary>
    /// Gets the tab index for a direct child control.
    /// </summary>
    ///
    /// <param name="control">The direct child control to query.</param>
    ///
    /// <returns>
    /// The tab index, or <c>-1</c> when <paramref name="control"/> is not a tab.
    /// </returns>
    ///
    /// <remarks>
    /// The method uses reference identity and does not search descendants.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="control"/> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetTabControl(int)"/>
    public int GetTabIdxFromControl(Control control)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(control);
        var tabs = GetTabs();
        return tabs.FindIndex(tab => ReferenceEquals(tab, control));
    }

    /// <summary>
    /// Gets the tab index whose button contains a local point.
    /// </summary>
    ///
    /// <param name="point">The local point inside this container.</param>
    ///
    /// <returns>
    /// The tab index at the point, or <c>-1</c> when the point is not over an
    /// enabled visible tab button.
    /// </returns>
    ///
    /// <remarks>
    /// This method uses the same tab rectangles as pointer input. Hidden and
    /// disabled tabs are skipped.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="_GuiInput(InputEvent)"/>
    public int GetTabIdxAtPoint(Vector2 point)
    {
        ThrowIfFreed();
        if (!TabsVisible)
        {
            return -1;
        }

        var tabs = GetTabs();
        for (var index = 0; index < tabs.Count; index++)
        {
            if (!IsSelectableTab(index))
            {
                continue;
            }

            if (GetTabRect(index, tabs.Count).HasPoint(point))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>
    /// Selects the next available tab.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when a later available tab was selected; otherwise,
    /// <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Hidden and disabled tabs are skipped. This method does not wrap around
    /// to the beginning of the list.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SelectPreviousAvailable"/>
    public bool SelectNextAvailable()
    {
        ThrowIfFreed();
        EnsureCurrentTab();
        var next = FindSelectableTab(currentTab + 1, direction: 1);
        if (next < 0)
        {
            return false;
        }

        SetCurrentTab(next, emitSignal: true);
        return true;
    }

    /// <summary>
    /// Selects the previous available tab.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when an earlier available tab was selected; otherwise,
    /// <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Hidden and disabled tabs are skipped. This method does not wrap around
    /// to the end of the list.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SelectNextAvailable"/>
    public bool SelectPreviousAvailable()
    {
        ThrowIfFreed();
        EnsureCurrentTab();
        var previous = FindSelectableTab(currentTab - 1, direction: -1);
        if (previous < 0)
        {
            return false;
        }

        SetCurrentTab(previous, emitSignal: true);
        return true;
    }

    /// <summary>
    /// Handles GUI input routed to this tab container.
    /// </summary>
    ///
    /// <param name="inputEvent">The input event delivered by the viewport.</param>
    ///
    /// <remarks>
    /// Mouse and touch press over a tab header button select that tab. Disabled
    /// and hidden tabs are ignored.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Control._GuiInput(InputEvent)"/>
    public override void _GuiInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseButton:
                SelectTabAtPoint(mouseButton.GlobalPosition - GlobalPosition);
                break;
            case InputEventScreenTouch { Pressed: true, Canceled: false } touch:
                SelectTabAtPoint(touch.Position - GlobalPosition);
                break;
        }
    }

    /// <summary>
    /// Draws the tab header and tab button labels.
    /// </summary>
    ///
    /// <remarks>
    /// Text drawing is skipped when no theme font is available. Page controls
    /// are drawn by their own canvas item callbacks.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="CanvasItem.DrawRect(Rect2, Color, bool, float, bool)"/>
    public override void _Draw()
    {
        if (!TabsVisible)
        {
            return;
        }

        EnsureCurrentTab();
        var tabs = GetTabs();
        var font = GetThemeFont("font");
        var fontSize = GetThemeFontSize("font_size");
        for (var index = 0; index < tabs.Count; index++)
        {
            if (IsTabHidden(index))
            {
                continue;
            }

            var rect = GetTabRect(index, tabs.Count);
            var color = index == currentTab
                ? (HasThemeColor("selected_color") ? GetThemeColor("selected_color") : new Color(0.22f, 0.34f, 0.56f, 1f))
                : new Color(0.15f, 0.16f, 0.18f, 1f);
            if (IsTabDisabled(index))
            {
                color = new Color(0.10f, 0.10f, 0.11f, 0.75f);
            }

            DrawRect(rect, color);
            if (font is not null)
            {
                var title = GetTabTitle(index);
                var textColor = IsTabDisabled(index) ? new Color(0.55f, 0.56f, 0.60f, 1f) : Color.White;
                var baseline = new Vector2(
                    rect.Position.X + 8f,
                    rect.Position.Y + MathF.Max(font.GetAscent(fontSize), ((rect.Size.Y - font.GetHeight(fontSize)) * 0.5f) + font.GetAscent(fontSize)));
                DrawString(font, baseline, title, HorizontalAlignment.Left, MathF.Max(0f, rect.Size.X - 16f), fontSize, textColor);
            }
        }
    }

    /// <summary>
    /// Gets the minimum size requested by this tab container.
    /// </summary>
    ///
    /// <returns>
    /// A size that can contain the largest measured tab page and the tab
    /// header.
    /// </returns>
    ///
    /// <remarks>
    /// Hidden tabs contribute to the size only when
    /// <see cref="UseHiddenTabsForMinSize"/> is <c>true</c>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Control.GetMinimumSize"/>
    public override Vector2 _GetMinimumSize()
    {
        var tabs = GetTabs();
        var width = 0f;
        var height = 0f;
        for (var index = 0; index < tabs.Count; index++)
        {
            if (!UseHiddenTabsForMinSize && IsTabHidden(index))
            {
                continue;
            }

            var minimum = tabs[index].GetCombinedMinimumSize();
            width = MathF.Max(width, minimum.X);
            height = MathF.Max(height, minimum.Y);
        }

        if (TabsVisible)
        {
            width = MathF.Max(width, tabs.Count * GetTabWidth());
            height += GetTabHeight();
        }

        return new Vector2(width, height);
    }

    protected override void SortChildren()
    {
        EnsureCurrentTab();
        ApplyTabVisibility();
        var current = GetCurrentTabControl();
        if (current is null)
        {
            return;
        }

        var headerHeight = TabsVisible ? GetTabHeight() : 0f;
        var pageHeight = MathF.Max(0f, Size.Y - headerHeight);
        var pageY = TabsPosition == TabPositionEnum.Top ? headerHeight : 0f;
        FitChildInRect(current, new Rect2(0f, pageY, Size.X, pageHeight));
    }

    private void SelectTabAtPoint(Vector2 localPoint)
    {
        var index = GetTabIdxAtPoint(localPoint);
        if (index < 0)
        {
            return;
        }

        SetCurrentTab(index, emitSignal: true);
        AcceptEvent();
    }

    private void SetCurrentTab(int tabIndex, bool emitSignal)
    {
        var tabs = GetTabs();
        if (tabIndex == -1)
        {
            if (!DeselectEnabled && tabs.Count > 0)
            {
                tabIndex = FindSelectableTab(startIndex: 0, direction: 1);
            }
        }
        else if (tabIndex < 0 || tabIndex >= tabs.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(tabIndex), tabIndex, "Tab index is outside the container.");
        }
        else if (!IsSelectableTab(tabIndex))
        {
            return;
        }

        if (tabIndex == currentTab)
        {
            ApplyTabVisibility();
            return;
        }

        previousTab = currentTab;
        currentTab = tabIndex;
        ApplyTabVisibility();
        QueueSort();
        QueueRedraw();
        if (emitSignal)
        {
            EmitSignal("tab_changed", currentTab);
        }
    }

    private void EnsureCurrentTab()
    {
        var tabs = GetTabs();
        PruneTabData(tabs);
        if (currentTab >= 0 && currentTab < tabs.Count && IsSelectableTab(currentTab))
        {
            ApplyTabVisibility();
            return;
        }

        var replacement = DeselectEnabled ? -1 : FindSelectableTab(startIndex: 0, direction: 1);
        SetCurrentTab(replacement, emitSignal: false);
    }

    private void SelectReplacementTab()
    {
        var replacement = FindSelectableTab(currentTab + 1, direction: 1);
        if (replacement < 0)
        {
            replacement = FindSelectableTab(currentTab - 1, direction: -1);
        }

        if (replacement < 0 && DeselectEnabled)
        {
            replacement = -1;
        }

        SetCurrentTab(replacement, emitSignal: true);
    }

    private int FindSelectableTab(int startIndex, int direction)
    {
        var tabs = GetTabs();
        if (direction == 0)
        {
            return -1;
        }

        for (var index = startIndex; index >= 0 && index < tabs.Count; index += direction)
        {
            if (IsSelectableTab(index))
            {
                return index;
            }
        }

        return -1;
    }

    private bool IsSelectableTab(int index)
    {
        var tabs = GetTabs();
        if (index < 0 || index >= tabs.Count)
        {
            return false;
        }

        var data = GetData(tabs[index]);
        return !data.Disabled && !data.Hidden;
    }

    private Control GetTab(int tabIndex)
    {
        var tabs = GetTabs();
        if (tabIndex < 0 || tabIndex >= tabs.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(tabIndex), tabIndex, "Tab index is outside the container.");
        }

        return tabs[tabIndex];
    }

    private List<Control> GetTabs()
    {
        return GetChildrenSnapshot().OfType<Control>().ToList();
    }

    private TabData GetData(Control tab)
    {
        if (!tabData.TryGetValue(tab, out var data))
        {
            data = new TabData();
            tabData.Add(tab, data);
        }

        return data;
    }

    private void PruneTabData(IReadOnlyCollection<Control> tabs)
    {
        if (tabData.Count == 0)
        {
            return;
        }

        var currentTabs = tabs.ToHashSet();
        foreach (var tab in tabData.Keys.Where(tab => !currentTabs.Contains(tab)).ToArray())
        {
            tabData.Remove(tab);
        }
    }

    private void ApplyTabVisibility()
    {
        var tabs = GetTabs();
        for (var index = 0; index < tabs.Count; index++)
        {
            tabs[index].Visible = index == currentTab && !GetData(tabs[index]).Hidden;
        }
    }

    private Rect2 GetTabRect(int tabIndex, int tabCount)
    {
        var tabWidth = GetTabWidth();
        var tabHeight = GetTabHeight();
        var totalWidth = tabWidth * tabCount;
        var startX = TabAlignment switch
        {
            AlignmentModeEnum.Center => MathF.Max(0f, (Size.X - totalWidth) * 0.5f),
            AlignmentModeEnum.Right => MathF.Max(0f, Size.X - totalWidth),
            _ => 0f
        };
        var y = TabsPosition == TabPositionEnum.Top ? 0f : MathF.Max(0f, Size.Y - tabHeight);
        return new Rect2(startX + (tabIndex * tabWidth), y, tabWidth, tabHeight);
    }

    private int GetTabHeight()
    {
        return Math.Max(1, GetThemeConstantOrDefault("tab_height", DefaultTabHeight));
    }

    private int GetTabWidth()
    {
        return Math.Max(1, GetThemeConstantOrDefault("tab_width", DefaultTabWidth));
    }

    private sealed class TabData
    {
        public string Title { get; set; } = string.Empty;

        public bool HasTitle { get; set; }

        public Texture2D? Icon { get; set; }

        public bool Disabled { get; set; }

        public bool Hidden { get; set; }
    }
}
