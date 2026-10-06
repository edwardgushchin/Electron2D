using System.Text;

namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddColorPickerDefaults()
    {
        var paths = new Dictionary<string, string>
        {
            ["add_preset"] = "<path d='M8 2v12M2 8h12' stroke='white' stroke-width='2'/>",
            ["bar_arrow"] = "<path d='M2 2L8 8L2 14z' fill='white'/>",
            ["color_copy"] = "<path d='M2 2h9v10H2zM5 5h9v9H5z' fill='none' stroke='white'/>",
            ["color_script"] = "<path d='M5 3L1 8L5 13M11 3L15 8L11 13M9 2L7 14' fill='none' stroke='white'/>",
            ["expanded_arrow"] = "<path d='M2 5L8 11L14 5' fill='none' stroke='white' stroke-width='2'/>",
            ["folded_arrow"] = "<path d='M5 2L11 8L5 14' fill='none' stroke='white' stroke-width='2'/>",
            ["menu_option"] = "<circle cx='3' cy='8' r='1.5' fill='white'/><circle cx='8' cy='8' r='1.5' fill='white'/><circle cx='13' cy='8' r='1.5' fill='white'/>",
            ["overbright_indicator"] = "<path d='M1 1h14L1 15z' fill='#ffcf40'/><path d='M4 3v5M4 10v1' stroke='black'/>",
            ["picker_cursor"] = "<circle cx='8' cy='8' r='6' stroke='black' stroke-width='3' fill='none'/><circle cx='8' cy='8' r='6' stroke='white' stroke-width='1' fill='none'/>",
            ["picker_cursor_bg"] = "<circle cx='8' cy='8' r='5' fill='white'/>",
            ["sample_revert"] = "<path d='M6 2L2 6L6 10M2 6h7a4 4 0 010 8' fill='none' stroke='white' stroke-width='2'/>",
            ["screen_picker"] = "<path d='M9 1l6 6L5 15H1v-4zM6 4l6 6' fill='none' stroke='white' stroke-width='2'/>",
            ["shape_circle"] = "<circle cx='8' cy='8' r='6' fill='none' stroke='white' stroke-width='2'/>",
            ["shape_rect"] = "<rect x='2' y='2' width='12' height='12' fill='none' stroke='white' stroke-width='2'/>",
            ["shape_rect_wheel"] = "<circle cx='8' cy='8' r='7' fill='none' stroke='white'/><rect x='4' y='4' width='8' height='8' fill='none' stroke='white'/>",
        };
        foreach (var item in paths) _defaultTheme.SetIcon(item.Key, "ColorPicker", CreateIcon(Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'>" + item.Value + "</svg>")));
        var checker = CreateIcon("<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'><path d='M0 0h16v16H0z' fill='#999'/><path d='M0 0h8v8H0zM8 8h8v8H8z' fill='#ccc'/></svg>"u8);
        _defaultTheme.SetIcon("sample_bg", "ColorPicker", checker); _defaultTheme.SetIcon("bg", "ColorPickerButton", checker);
        var hue = CreateIcon("<svg xmlns='http://www.w3.org/2000/svg' width='256' height='16'><defs><linearGradient id='h'><stop offset='0' stop-color='#f00'/><stop offset='.166667' stop-color='#ff0'/><stop offset='.333333' stop-color='#0f0'/><stop offset='.5' stop-color='#0ff'/><stop offset='.666667' stop-color='#00f'/><stop offset='.833333' stop-color='#f0f'/><stop offset='1' stop-color='#f00'/></linearGradient></defs><path d='M0 0h256v16H0z' fill='url(#h)'/></svg>"u8);
        _defaultTheme.SetIcon("color_hue", "ColorPicker", hue);
        _defaultTheme.SetColor("focused_not_editing_cursor_color", "ColorPicker", new(1, 1, 1, .275f));
        foreach (var item in new[] { ("center_slider_grabbers", 1), ("h_width", 30), ("label_width", 10), ("margin", 4), ("sv_height", 256), ("sv_width", 256) }) _defaultTheme.SetConstant(item.Item1, "ColorPicker", item.Item2);
        var focus = new StyleBoxFlat { BGColor = new(0, 0, 0, 0), BorderColor = new(.4f, .65f, 1), DrawCenter = false, CornerDetail = 16 }; focus.SetBorderWidthAll(2); _owned.Add(focus);
        var circle = (StyleBoxFlat)focus.Duplicate(); circle.SetCornerRadiusAll(256); _owned.Add(circle);
        _defaultTheme.SetStyleBox("picker_focus_rectangle", "ColorPicker", focus); _defaultTheme.SetStyleBox("picker_focus_circle", "ColorPicker", circle); _defaultTheme.SetStyleBox("sample_focus", "ColorPicker", focus);
    }
}
