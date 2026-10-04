namespace Electron2D;

public sealed partial class SceneTree
{
    internal void DispatchCommittedText(Viewport viewport, string text)
    {
        using var scope = SelectGUI(viewport);
        ArgumentNullException.ThrowIfNull(text);
        EnsureTextDeliveryAvailable(viewport);
        BeginExecution(); _isDispatchingInput = true; _inputHandled = false;
        List<Exception>? errors = null;
        try
        {
            var focused = EligibleTextFocus(viewport, ref errors);
            if (focused is not null)
                try { focused.DispatchTextInput(text); }
                catch (Exception error) { CollectException(ref errors, error); }
        }
        finally { _inputHandled = false; _isDispatchingInput = false; EndExecution(); }
        ThrowCollected("Scene text input callbacks failed.", errors);
    }

    internal void DispatchIMEComposition(Viewport viewport, string text, Vector2i selection)
    {
        using var scope = SelectGUI(viewport);
        ArgumentNullException.ThrowIfNull(text);
        EnsureTextDeliveryAvailable(viewport);
        BeginExecution(); _isDispatchingInput = true; _inputHandled = false;
        List<Exception>? errors = null;
        try
        {
            try { Notify(Node.NotificationOsImeUpdate); }
            catch (Exception error) { CollectException(ref errors, error); }
            var focused = EligibleTextFocus(viewport, ref errors);
            if (focused is not null)
                try { focused.DispatchIMEComposition(text, selection); }
                catch (Exception error) { CollectException(ref errors, error); }
        }
        finally { _inputHandled = false; _isDispatchingInput = false; EndExecution(); }
        ThrowCollected("Scene IME composition callbacks failed.", errors);
    }

    private void EnsureTextDeliveryAvailable(Viewport viewport)
    {
        ThrowIfDisposed(); EnsureOwnerThread(); EnsureAcceptingWork(); EnsureExecutionAvailable();
        if (!ReferenceEquals(Root, viewport) || !ReferenceEquals(viewport.Tree, this))
            throw new InvalidOperationException("Text delivery requires the root viewport of this tree.");
    }

    private Control? EligibleTextFocus(Viewport viewport, ref List<Exception>? errors)
    {
        using var guiScope = SelectGUI(viewport);
        var focused = _gui.GuiFocus;
        if (focused is not null && (focused.IsDisposed || !ReferenceEquals(focused.Tree, this) ||
            !focused.IsVisibleInTree || !focused.CanReceiveGUIFocus || !ReferenceEquals(focused.GetViewport(), viewport)))
        {
            try { ReleaseGUIFocus(focused); } catch (Exception error) { CollectException(ref errors, error); }
            focused = null;
        }
        if (focused is SubViewportContainer container)
            for (var i = container.GetChildCount() - 1; i >= 0; i--)
                if (container.GetChild(i) is SubViewport { GUIDisableInput: false } child && EligibleTextFocus(child, ref errors) is { } inner) return inner;
        return focused is { IsDisposed: false } && focused.CanProcess() ? focused : null;
    }
}
