using Electron2D;

namespace StartGame;

public sealed class CloseConfirmControl() : Control("CloseConfirm")
{
    private bool _open;

    protected override void EnterTree()
    {
        // Временно — сразу в фокус, чтобы Space/Esc работали
        GrabFocus();
    }

    protected override void HandleGUIInput(InputEvent e)
    {
        if (!_open) return;

        switch (e)
        {
            case { Type: InputEventType.KeyDown, Code: (int)Key.Escape }:
                AcceptEvent();
                SceneTree!.Quit();
                Console.WriteLine("Выход...");
                break;
            case { Type: InputEventType.KeyDown, Code: (int)Key.Backspace }:
                AcceptEvent();
                _open = false;
                Console.WriteLine("Отмена выхода");
                break;
        }
    }

    public void Open()
    {
        _open = true;
        GrabFocus();
        
        Console.WriteLine("Вы действительно хотите выйти? Да - Escape, Нет - Backspace");
    }

    public void Close()
    {
        _open = false;
    }
}