namespace Electron2D;

public sealed partial class ThemeDB
{
    private void AddFileDialogDefaults()
    {
        const string folder = """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20"><path d="M2 4h6l2 3h8v10H2z" fill="#deb85a"/></svg>""";
        const string file = """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20"><path d="M4 2h8l4 4v12H4z" fill="#ddd"/><path d="M7 9h6M7 12h6" fill="none" stroke="#555"/></svg>""";
        var folderIcon = CreateIcon(System.Text.Encoding.UTF8.GetBytes(folder)); var fileIcon = CreateIcon(System.Text.Encoding.UTF8.GetBytes(file));
        foreach (var name in new[] { "folder", "folder_thumbnail", "create_folder" }) _defaultTheme.SetIcon(name, "FileDialog", folderIcon);
        foreach (var name in new[] { "file", "file_thumbnail" }) _defaultTheme.SetIcon(name, "FileDialog", fileIcon);
        var glyphs = new Dictionary<string, string> { ["back_folder"] = "M14 4L6 10L14 16", ["forward_folder"] = "M6 4L14 10L6 16", ["parent_folder"] = "M4 13L10 6L16 13", ["favorite_up"] = "M4 13L10 6L16 13", ["favorite_down"] = "M4 7L10 14L16 7", ["reload"] = "M15 5A7 7 0 1 0 17 11M15 2V7H10", ["favorite"] = "M10 2L12 7L18 8L14 12L15 18L10 15L5 18L6 12L2 8L8 7Z", ["toggle_hidden"] = "M2 10Q10 1 18 10Q10 19 2 10ZM9 9h2v2H9z", ["list_mode"] = "M3 4H17M3 10H17M3 16H17", ["thumbnail_mode"] = "M3 3H8V8H3ZM12 3H17V8H12ZM3 12H8V17H3ZM12 12H17V17H12Z", ["sort"] = "M3 4H17M3 10H12M3 16H7", ["toggle_filename_filter"] = "M3 3H17L12 9V17L8 15V9Z" };
        foreach (var pair in glyphs) _defaultTheme.SetIcon(pair.Key, "FileDialog", CreateIcon(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"20\"><path d=\"" + pair.Value + "\" stroke=\"#ddd\" stroke-width=\"2\" fill=\"none\"/></svg>")));
        foreach (var name in new[] { "menu_copy_path", "menu_delete", "menu_new_folder", "menu_open_bundle", "menu_refresh", "menu_show_in_file_manager" }) _defaultTheme.SetIcon(name, "FileDialog", name is "menu_new_folder" or "menu_show_in_file_manager" ? folderIcon : fileIcon);
        _defaultTheme.SetConstant("thumbnail_size", "FileDialog", 64);
        _defaultTheme.SetColor("file_disabled_color", "FileDialog", new(1, 1, 1, .25f)); _defaultTheme.SetColor("file_icon_color", "FileDialog", Colors.White); _defaultTheme.SetColor("folder_icon_color", "FileDialog", Colors.White);
    }
}
