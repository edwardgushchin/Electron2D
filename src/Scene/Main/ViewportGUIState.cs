namespace Electron2D;

internal sealed class ViewportGUIState(Viewport? viewport)
{
    internal bool InputHandled;
    internal readonly Viewport? Viewport = viewport;
    internal ViewportGUIState Section = null!;
    internal readonly List<Control> GuiHoverChain = [];
    internal readonly List<Control> GuiHoverScratch = [];
    internal readonly List<List<Control>> GuiHoverPrevious = [];
    internal int GuiHoverChangeDepth;
    internal Control? GuiHoverTarget;
    internal Viewport? GuiHoverViewport;
    internal Vector2 GuiHoverPosition;
    internal bool GuiHoverKnown;
    internal bool UpdatingGUIHover;
    internal bool GuiHoverRefreshPending;
    internal readonly List<Node> InputTraversal = [];
    internal Control? GuiFocus;
    internal Control? GuiMouseCapture;
    internal uint GuiMouseCaptureMask;
    internal readonly Dictionary<int, WeakReference<Control>> GuiTouchCapture = [];
    internal readonly Stack<WeakReference<Control>> GuiTouchSlots = [];
    internal readonly List<int> GuiTouchReleaseKeys = [];
    internal bool GuiFocusHidden;
    internal Control? TooltipControl, TooltipOwner;
    internal CanvasLayer? TooltipLayer;
    internal PanelContainer? TooltipPanel;
    internal Vector2 TooltipPosition;
    internal string ShownTooltipText = string.Empty;
    internal double TooltipRemaining;
    internal bool TooltipScheduled;
    internal ulong TooltipGeneration;
    internal Control? MouseTarget;
    internal Viewport? DragViewport;
    internal DragPayload? GuiDragPayload;
    internal Control? GuiDragSource;
    internal Control? GuiDragHovered;
    internal Control? GuiDragPreview;
    internal CanvasLayer? GuiDragPreviewLayer;
    internal Vector2 GuiDragTravel, GuiDragPointer;
    internal bool GuiDragAttempted, GuiDragPreparing, GuiDragCompleting, GuiDragPossible, GuiDragSuccessful;
    internal uint GuiDragPreviewSerial;
}
