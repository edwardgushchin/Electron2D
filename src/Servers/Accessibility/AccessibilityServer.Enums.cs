namespace Electron2D;

public sealed partial class AccessibilityServer
{
    /// <summary>Identifies the semantic role of an accessible element.</summary>
    public enum AccessibilityRole
    {
        /// <summary>Identifies the unknown role.</summary>
        Unknown = 0,
        /// <summary>Identifies the default button role.</summary>
        DefaultButton = 1,
        /// <summary>Identifies the audio role.</summary>
        Audio = 2,
        /// <summary>Identifies the video role.</summary>
        Video = 3,
        /// <summary>Identifies the static text role.</summary>
        StaticText = 4,
        /// <summary>Identifies the container role.</summary>
        Container = 5,
        /// <summary>Identifies the panel role.</summary>
        Panel = 6,
        /// <summary>Identifies the button role.</summary>
        Button = 7,
        /// <summary>Identifies the link role.</summary>
        Link = 8,
        /// <summary>Identifies the check box role.</summary>
        CheckBox = 9,
        /// <summary>Identifies the radio button role.</summary>
        RadioButton = 10,
        /// <summary>Identifies the check button role.</summary>
        CheckButton = 11,
        /// <summary>Identifies the scroll bar role.</summary>
        ScrollBar = 12,
        /// <summary>Identifies the scroll view role.</summary>
        ScrollView = 13,
        /// <summary>Identifies the splitter role.</summary>
        Splitter = 14,
        /// <summary>Identifies the slider role.</summary>
        Slider = 15,
        /// <summary>Identifies the spin button role.</summary>
        SpinButton = 16,
        /// <summary>Identifies the progress indicator role.</summary>
        ProgressIndicator = 17,
        /// <summary>Identifies the text field role.</summary>
        TextField = 18,
        /// <summary>Identifies the multiline text field role.</summary>
        MultilineTextField = 19,
        /// <summary>Identifies the color picker role.</summary>
        ColorPicker = 20,
        /// <summary>Identifies the table role.</summary>
        Table = 21,
        /// <summary>Identifies the cell role.</summary>
        Cell = 22,
        /// <summary>Identifies the row role.</summary>
        Row = 23,
        /// <summary>Identifies the row group role.</summary>
        RowGroup = 24,
        /// <summary>Identifies the row header role.</summary>
        RowHeader = 25,
        /// <summary>Identifies the column header role.</summary>
        ColumnHeader = 26,
        /// <summary>Identifies the tree role.</summary>
        Tree = 27,
        /// <summary>Identifies the tree item role.</summary>
        TreeItem = 28,
        /// <summary>Identifies the list role.</summary>
        List = 29,
        /// <summary>Identifies the list item role.</summary>
        ListItem = 30,
        /// <summary>Identifies the list box role.</summary>
        ListBox = 31,
        /// <summary>Identifies the list box option role.</summary>
        ListBoxOption = 32,
        /// <summary>Identifies the tab bar role.</summary>
        TabBar = 33,
        /// <summary>Identifies the tab role.</summary>
        Tab = 34,
        /// <summary>Identifies the tab panel role.</summary>
        TabPanel = 35,
        /// <summary>Identifies the menu bar role.</summary>
        MenuBar = 36,
        /// <summary>Identifies the menu role.</summary>
        Menu = 37,
        /// <summary>Identifies the menu item role.</summary>
        MenuItem = 38,
        /// <summary>Identifies the menu item check box role.</summary>
        MenuItemCheckBox = 39,
        /// <summary>Identifies the menu item radio role.</summary>
        MenuItemRadio = 40,
        /// <summary>Identifies the image role.</summary>
        Image = 41,
        /// <summary>Identifies the window role.</summary>
        Window = 42,
        /// <summary>Identifies the title bar role.</summary>
        TitleBar = 43,
        /// <summary>Identifies the dialog role.</summary>
        Dialog = 44,
        /// <summary>Identifies the tooltip role.</summary>
        Tooltip = 45,
        /// <summary>Identifies the region role.</summary>
        Region = 46,
        /// <summary>Identifies the text run role.</summary>
        TextRun = 47,
    }

    /// <summary>Selects an independently updated accessible state; values are selectors, not a bit mask.</summary>
    public enum AccessibilityFlags
    {
        /// <summary>Identifies the hidden state.</summary>
        Hidden = 0,
        /// <summary>Identifies the multiselectable state.</summary>
        Multiselectable = 1,
        /// <summary>Identifies the required state.</summary>
        Required = 2,
        /// <summary>Identifies the visited state.</summary>
        Visited = 3,
        /// <summary>Identifies the busy state.</summary>
        Busy = 4,
        /// <summary>Identifies the modal state.</summary>
        Modal = 5,
        /// <summary>Identifies the touch passthrough state.</summary>
        TouchPassthrough = 6,
        /// <summary>Identifies the readonly state.</summary>
        ReadOnly = 7,
        /// <summary>Identifies the disabled state.</summary>
        Disabled = 8,
        /// <summary>Identifies the clips children state.</summary>
        ClipsChildren = 9,
    }

    /// <summary>Identifies a compiled semantic action and its required callback payload.</summary>
    public enum AccessibilityAction
    {
        /// <summary>Activates the element.</summary>
        Click = 0,
        /// <summary>Requests focus.</summary>
        Focus = 1,
        /// <summary>Releases focus.</summary>
        Blur = 2,
        /// <summary>Requests collapsing a branch through a no-argument callback.</summary>
        Collapse = 3,
        /// <summary>Requests expanding a branch through a no-argument callback.</summary>
        Expand = 4,
        /// <summary>Requests one semantic numeric decrement using a no-argument callback.</summary>
        Decrement = 5,
        /// <summary>Requests hiding a tooltip through a no-argument callback.</summary>
        HideTooltip = 7,
        /// <summary>Requests presenting a tooltip through a no-argument callback.</summary>
        ShowTooltip = 8,
        /// <summary>Requests opening a context menu through a no-argument callback.</summary>
        ShowContextMenu = 21,
        /// <summary>Requests one semantic numeric increment using a no-argument callback.</summary>
        Increment = 6,
        /// <summary>Requests a numeric value using an Action&lt;double&gt; callback.</summary>
        SetValue = 20,
        /// <summary>Requests anchor and focus text-element RIDs and Unicode scalar offsets through a four-argument callback.</summary>
        SetTextSelection = 9,
        /// <summary>Replaces the current selection through a string callback.</summary>
        ReplaceSelectedText = 10,
        /// <summary>Scrolls backward through a no-argument callback.</summary>
        ScrollBackward = 11,
        /// <summary>Scrolls down by an item or page.</summary>
        ScrollDown = 12,
        /// <summary>Scrolls forward through a no-argument callback.</summary>
        ScrollForward = 13,
        /// <summary>Scrolls left by an item or page.</summary>
        ScrollLeft = 14,
        /// <summary>Scrolls right by an item or page.</summary>
        ScrollRight = 15,
        /// <summary>Scrolls up by an item or page.</summary>
        ScrollUp = 16,
        /// <summary>Reveals the element using a preferred viewport edge.</summary>
        ScrollIntoView = 17,
        /// <summary>Moves the element's top-left to a point relative to its native window by scrolling ancestors.</summary>
        ScrollToPoint = 18,
        /// <summary>Requests an absolute offset in the scrollable element's local coordinate space.</summary>
        SetScrollOffset = 19,

    }

    /// <summary>Selects a semantic item step or one viewport page.</summary>
    public enum AccessibilityScrollUnit
    {
        /// <summary>Scrolls one item, text line or source-defined small step.</summary>
        Item = 0,
        /// <summary>Scrolls one viewport page.</summary>
        Page = 1,
    }
    /// <summary>Suggests the target element's placement within its scrollable viewport.</summary>
    public enum AccessibilityScrollHint
    {
        /// <summary>Aligns the top and left edges.</summary>
        TopLeft = 0,
        /// <summary>Aligns the bottom and right edges.</summary>
        BottomRight = 1,
        /// <summary>Aligns the top edge.</summary>
        TopEdge = 2,
        /// <summary>Aligns the bottom edge.</summary>
        BottomEdge = 3,
        /// <summary>Aligns the left edge.</summary>
        LeftEdge = 4,
        /// <summary>Aligns the right edge.</summary>
        RightEdge = 5,
    }
}
