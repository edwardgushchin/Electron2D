namespace Electron2D;

public sealed partial class ProjectSettings
{
    /// <summary>Defines the default ui_text_completion_query keyboard action.</summary><value>Permanent typed input/ui_text_completion_query definition.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCompletionQuery { get; } = CreateDefaultKeyAction("ui_text_completion_query", Key.Space, KeyModifierMask.Control);
    /// <summary>Defines the default ui_text_indent keyboard action.</summary><value>Permanent typed input/ui_text_indent definition.</value>
    public static ProjectSetting<InputActionSettings> InputUITextIndent { get; } = CreateDefaultKeyAction("ui_text_indent", Key.Tab, 0);
    /// <summary>Defines the default ui_text_dedent keyboard action.</summary><value>Permanent typed input/ui_text_dedent definition.</value>
    public static ProjectSetting<InputActionSettings> InputUITextDedent { get; } = CreateDefaultKeyAction("ui_text_dedent", Key.Tab, KeyModifierMask.Shift);
    /// <summary>Defines the default ui_text_completion_accept keyboard action.</summary><value>Permanent typed input/ui_text_completion_accept definition.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCompletionAccept { get; } = new("input/ui_text_completion_accept", new InputActionSettings { Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Tab, Modifiers = 0 }, new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Enter, Modifiers = 0 }, new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.KeypadEnter, Modifiers = 0 }] });
    /// <summary>Defines the default ui_text_completion_replace keyboard action.</summary><value>Permanent typed input/ui_text_completion_replace definition.</value>
    public static ProjectSetting<InputActionSettings> InputUITextCompletionReplace { get; } = new("input/ui_text_completion_replace", new InputActionSettings { Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Tab, Modifiers = KeyModifierMask.Shift }, new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Enter, Modifiers = KeyModifierMask.Shift }, new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.KeypadEnter, Modifiers = KeyModifierMask.Shift }] });
    /// <summary>Defines the default ui_text_newline_blank keyboard action.</summary><value>Permanent typed input/ui_text_newline_blank definition.</value>
    public static ProjectSetting<InputActionSettings> InputUITextNewlineBlank { get; } = new("input/ui_text_newline_blank", new InputActionSettings { Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Enter, Modifiers = KeyModifierMask.CommandOrControl }, new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.KeypadEnter, Modifiers = KeyModifierMask.CommandOrControl }] });
    /// <summary>Defines the default ui_text_newline_above keyboard action.</summary><value>Permanent typed input/ui_text_newline_above definition.</value>
    public static ProjectSetting<InputActionSettings> InputUITextNewlineAbove { get; } = new("input/ui_text_newline_above", new InputActionSettings { Bindings = [new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.Enter, Modifiers = KeyModifierMask.CommandOrControl | KeyModifierMask.Shift }, new InputBindingSettings { Kind = InputBindingKind.Key, Keycode = Key.KeypadEnter, Modifiers = KeyModifierMask.CommandOrControl | KeyModifierMask.Shift }] });
}
