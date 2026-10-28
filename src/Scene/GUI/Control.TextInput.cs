namespace Electron2D;

public partial class Control
{
    /// <summary>Occurs when the native input method commits one complete text string to this focused control.</summary>
    /// <remarks>A commit may contain multiple Unicode scalars. It is not attributed to a key press.</remarks>
    public event Action<string>? TextInput { add => _publisherTextInput.Add(value); remove => _publisherTextInput.Remove(value); }
    private EventPublisher<Action<string>> _publisherTextInput;

    /// <summary>Occurs when the focused control's uncommitted IME composition changes.</summary>
    /// <remarks>The selection uses Unicode-codepoint start and length, matching the current DisplayServer state.</remarks>
    public event Action<string, Vector2i>? IMECompositionChanged { add => _publisherIMECompositionChanged.Add(value); remove => _publisherIMECompositionChanged.Remove(value); }
    private EventPublisher<Action<string, Vector2i>> _publisherIMECompositionChanged;

    /// <summary>Receives one committed text string before <see cref="TextInput"/> subscribers.</summary>
    /// <param name="text">The complete committed text, including multiple Unicode scalars when supplied.</param>
    protected virtual void OnTextInput(string text) { }

    /// <summary>Receives an uncommitted composition update before <see cref="IMECompositionChanged"/> subscribers.</summary>
    /// <param name="text">The current preedit text; empty clears it.</param>
    /// <param name="selection">Unicode-codepoint selection start and length.</param>
    protected virtual void OnIMECompositionChanged(string text, Vector2i selection) { }

    internal void DispatchTextInput(string text)
    {
        ThrowIfDisposed();
        List<Exception>? errors = null;
        try { OnTextInput(text); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) ControlCallbacks?.TextInput?.Invoke(text); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) BeginEventEmission(_publisherTextInput.Handlers)?.Invoke(text); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Control text input callbacks failed.", errors);
    }

    internal void DispatchIMEComposition(string text, Vector2i selection)
    {
        ThrowIfDisposed();
        List<Exception>? errors = null;
        try { OnIMECompositionChanged(text, selection); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) ControlCallbacks?.IMECompositionChanged?.Invoke(text, selection); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) BeginEventEmission(_publisherIMECompositionChanged.Handlers)?.Invoke(text, selection); }
        catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Control IME composition callbacks failed.", errors);
    }
}
