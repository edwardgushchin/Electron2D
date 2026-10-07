namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddCodeEditDefaults()
    {
        _defaultTheme.SetColor("bookmark_color", "CodeEdit", new(0.5f, 0.64f, 1f, 0.8f));
        _defaultTheme.SetColor("brace_mismatch_color", "CodeEdit", new(1f, 0.2f, 0.2f, 1f));
        _defaultTheme.SetColor("breakpoint_color", "CodeEdit", new(0.9f, 0.29f, 0.3f, 1f));
        _defaultTheme.SetColor("code_folding_color", "CodeEdit", new(0.8f, 0.8f, 0.8f, 0.8f));
        _defaultTheme.SetColor("completion_background_color", "CodeEdit", new(0.17f, 0.16f, 0.2f, 1f));
        _defaultTheme.SetColor("completion_existing_color", "CodeEdit", new(0.87f, 0.87f, 0.87f, 0.13f));
        _defaultTheme.SetColor("completion_scroll_color", "CodeEdit", new(1f, 1f, 1f, 0.29f));
        _defaultTheme.SetColor("completion_scroll_hovered_color", "CodeEdit", new(1f, 1f, 1f, 0.4f));
        _defaultTheme.SetColor("completion_selected_color", "CodeEdit", new(0.26f, 0.26f, 0.27f, 1f));
        _defaultTheme.SetColor("executing_line_color", "CodeEdit", new(0.98f, 0.89f, 0.27f, 1f));
        _defaultTheme.SetColor("folded_code_region_color", "CodeEdit", new(0.68f, 0.46f, 0.77f, 0.2f));
        _defaultTheme.SetColor("line_length_guideline_color", "CodeEdit", new(0.3f, 0.5f, 0.8f, 0.1f));
        _defaultTheme.SetColor("line_number_color", "CodeEdit", new(0.67f, 0.67f, 0.67f, 0.4f));
        _defaultTheme.SetConstant("completion_lines", "CodeEdit", 7);
        _defaultTheme.SetConstant("completion_max_width", "CodeEdit", 50);
        _defaultTheme.SetConstant("completion_scroll_width", "CodeEdit", 6);
        _defaultTheme.SetIcon("bookmark", "CodeEdit", CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\"><path d=\"M4 2H12V14L8 10L4 14Z\" fill=\"#80a3ff\"/></svg>")));
        _defaultTheme.SetIcon("breakpoint", "CodeEdit", CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\"><circle cx=\"8\" cy=\"8\" r=\"6\" fill=\"#e64a4d\"/></svg>")));
        _defaultTheme.SetIcon("can_fold", "CodeEdit", CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\"><path d=\"M3 5L8 11L13 5Z\" fill=\"white\"/></svg>")));
        _defaultTheme.SetIcon("can_fold_code_region", "CodeEdit", CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\"><path d=\"M3 5L8 11L13 5Z\" fill=\"#ad75c4\"/></svg>")));
        _defaultTheme.SetIcon("completion_color_bg", "CodeEdit", CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\"><path d=\"M0 0H16V16H0Z\" fill=\"#555\"/><path d=\"M0 0H8V8H0ZM8 8H16V16H8Z\" fill=\"#999\"/></svg>")));
        _defaultTheme.SetIcon("executing_line", "CodeEdit", CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\"><path d=\"M3 2L13 8L3 14Z\" fill=\"#fae344\"/></svg>")));
        _defaultTheme.SetIcon("folded", "CodeEdit", CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\"><path d=\"M5 3L11 8L5 13Z\" fill=\"white\"/></svg>")));
        _defaultTheme.SetIcon("folded_code_region", "CodeEdit", CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\"><path d=\"M5 3L11 8L5 13Z\" fill=\"#ad75c4\"/></svg>")));
        _defaultTheme.SetIcon("folded_eol_icon", "CodeEdit", CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\"><circle cx=\"3\" cy=\"8\" r=\"1\" fill=\"white\"/><circle cx=\"8\" cy=\"8\" r=\"1\" fill=\"white\"/><circle cx=\"13\" cy=\"8\" r=\"1\" fill=\"white\"/></svg>")));
        _defaultTheme.SetStyleBox("completion", "CodeEdit", CreateButtonStyle(new(.17f, .16f, .2f), 4, 4, 4, 4));
    }
}
