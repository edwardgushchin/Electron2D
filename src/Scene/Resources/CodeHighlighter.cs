using System.Text;

namespace Electron2D;

/// <summary>Highlights numeric literals, symbols, functions, members, keywords and delimited color regions.</summary>
/// <remarks>Configuration owns copied dictionaries. Region keys use a space between start and end delimiters;
/// a start-only key colors the remainder of the line. Longer start delimiters have priority.</remarks>
public class CodeHighlighter : SyntaxHighlighter
{
    private Dictionary<string, Color> _keywords = new(StringComparer.Ordinal), _members = new(StringComparer.Ordinal), _regionColors = new(StringComparer.Ordinal);
    private readonly List<Region> _regions = [];
    private readonly Dictionary<int, int> _regionEnds = [];
    private readonly record struct Region(string Start, string End, Color Color, bool LineOnly);
    private Color _number = Colors.Black, _symbol = Colors.Black, _function = Colors.Black, _member = Colors.Black;
    /// <summary>Creates a highlighter with black category colors and no keywords/regions.</summary>
    public CodeHighlighter() { }
    /// <summary>Gets or replaces copied ordinary keyword colors.</summary><value>An empty ordinal dictionary initially.</value>
    public Dictionary<string, Color> KeywordColors { get { CheckCode(); return new(_keywords, StringComparer.Ordinal); } set { CheckCode(); _keywords = CopyColors(value); ClearHighlightingCache(); } }
    /// <summary>Gets or replaces copied member keywords, recognized without preceding member access.</summary><value>An empty ordinal dictionary initially.</value>
    public Dictionary<string, Color> MemberKeywordColors { get { CheckCode(); return new(_members, StringComparer.Ordinal); } set { CheckCode(); _members = CopyColors(value); ClearHighlightingCache(); } }
    /// <summary>Gets or replaces copied delimiter-pair colors; start-only keys are line regions.</summary><value>An empty ordinal dictionary initially.</value>
    public Dictionary<string, Color> ColorRegions
    {
        get { CheckCode(); return new(_regionColors, StringComparer.Ordinal); }
        set { CheckCode(); var copy = CopyColors(value); var regions = new List<Region>(); foreach (var pair in copy) { var split = pair.Key.Split(' '); ValidateRegion(split[0], split.Length > 1 ? split[1] : ""); regions.Add(new(split[0], split.Length > 1 ? split[1] : "", pair.Value, split.Length == 1 || split[1].Length == 0)); } _regionColors = copy; _regions.Clear(); _regions.AddRange(regions.OrderByDescending(r => r.Start.Length)); ClearHighlightingCache(); }
    }
    /// <summary>Gets or sets the numeric literal color.</summary><value>Opaque black initially.</value>
    public Color NumberColor { get { CheckCode(); return _number; } set { CheckCode(); ValidateColor(value); _number = value; ClearHighlightingCache(); } }
    /// <summary>Gets or sets the symbol and string escape marker color.</summary><value>Opaque black initially.</value>
    public Color SymbolColor { get { CheckCode(); return _symbol; } set { CheckCode(); ValidateColor(value); _symbol = value; ClearHighlightingCache(); } }
    /// <summary>Gets or sets names followed by an opening parenthesis.</summary><value>Opaque black initially.</value>
    public Color FunctionColor { get { CheckCode(); return _function; } set { CheckCode(); ValidateColor(value); _function = value; ClearHighlightingCache(); } }
    /// <summary>Gets or sets member names following a dot.</summary><value>Opaque black initially.</value>
    public Color MemberVariableColor { get { CheckCode(); return _member; } set { CheckCode(); ValidateColor(value); _member = value; ClearHighlightingCache(); } }
    /// <summary>Adds or replaces one ordinary keyword color.</summary><param name="keyword">A nonempty token.</param><param name="color">Finite color.</param>
    public void AddKeywordColor(string keyword, Color color) { CheckCode(); ArgumentException.ThrowIfNullOrEmpty(keyword); ValidateColor(color); _keywords[keyword] = color; ClearHighlightingCache(); }
    /// <summary>Adds or replaces one member keyword color.</summary><param name="memberKeyword">A nonempty token.</param><param name="color">Finite color.</param>
    public void AddMemberKeywordColor(string memberKeyword, Color color) { CheckCode(); ArgumentException.ThrowIfNullOrEmpty(memberKeyword); ValidateColor(color); _members[memberKeyword] = color; ClearHighlightingCache(); }
    /// <summary>Removes a keyword if present.</summary><param name="keyword">Exact token.</param>
    public void RemoveKeywordColor(string keyword) { CheckCode(); ArgumentNullException.ThrowIfNull(keyword); _keywords.Remove(keyword); ClearHighlightingCache(); }
    /// <summary>Removes a member keyword if present.</summary><param name="memberKeyword">Exact token.</param>
    public void RemoveMemberKeywordColor(string memberKeyword) { CheckCode(); ArgumentNullException.ThrowIfNull(memberKeyword); _members.Remove(memberKeyword); ClearHighlightingCache(); }
    /// <summary>Reports exact ordinary keyword membership.</summary><param name="keyword">Exact token.</param><returns>Whether configured.</returns>
    public bool HasKeywordColor(string keyword) { CheckCode(); return _keywords.ContainsKey(keyword); }
    /// <summary>Reports exact member keyword membership.</summary><param name="memberKeyword">Exact token.</param><returns>Whether configured.</returns>
    public bool HasMemberKeywordColor(string memberKeyword) { CheckCode(); return _members.ContainsKey(memberKeyword); }
    /// <summary>Returns an existing keyword color.</summary><param name="keyword">Exact token.</param><returns>The configured finite color.</returns>
    public Color GetKeywordColor(string keyword) { CheckCode(); return _keywords[keyword]; }
    /// <summary>Returns an existing member keyword color.</summary><param name="memberKeyword">Exact token.</param><returns>The configured finite color.</returns>
    public Color GetMemberKeywordColor(string memberKeyword) { CheckCode(); return _members[memberKeyword]; }
    /// <summary>Clears ordinary keywords and highlighting cache.</summary>
    public void ClearKeywordColors() { CheckCode(); _keywords.Clear(); ClearHighlightingCache(); }
    /// <summary>Clears member keywords and highlighting cache.</summary>
    public void ClearMemberKeywordColors() { CheckCode(); _members.Clear(); ClearHighlightingCache(); }
    /// <summary>Adds a unique symbol-delimited color region.</summary><param name="startKey">Nonempty symbol sequence.</param><param name="endKey">Symbols, or empty for the remainder of the line.</param><param name="color">Finite color.</param><param name="lineOnly">Whether the region ends at a newline.</param>
    public void AddColorRegion(string startKey, string endKey, Color color, bool lineOnly = false)
    { CheckCode(); ValidateRegion(startKey, endKey); ValidateColor(color); if (HasColorRegion(startKey)) throw new ArgumentException("Duplicate color region.", nameof(startKey)); var region = new Region(startKey, endKey, color, lineOnly || endKey.Length == 0); var at = _regions.FindIndex(r => r.Start.Length < startKey.Length); _regions.Insert(at < 0 ? _regions.Count : at, region); _regionColors[startKey + (endKey.Length == 0 ? "" : " " + endKey)] = color; ClearHighlightingCache(); }
    /// <summary>Removes the region with an exact start key.</summary><param name="startKey">Exact start delimiter.</param>
    public void RemoveColorRegion(string startKey) { CheckCode(); var index = _regions.FindIndex(r => r.Start == startKey); if (index >= 0) { var region = _regions[index]; _regions.RemoveAt(index); _regionColors.Remove(region.Start + (region.End.Length == 0 ? "" : " " + region.End)); } ClearHighlightingCache(); }
    /// <summary>Reports whether a start delimiter is configured.</summary><param name="startKey">Exact start delimiter.</param><returns>Whether configured.</returns>
    public bool HasColorRegion(string startKey) { CheckCode(); return _regions.Any(r => r.Start == startKey); }
    /// <summary>Clears color regions and their continuation cache.</summary>
    public void ClearColorRegions() { CheckCode(); _regions.Clear(); _regionColors.Clear(); ClearHighlightingCache(); }
    private void CheckCode() { ThrowIfDisposed(); GetTextEdit()?.Tree?.EnsureOwnerThread(); }
    private static void ValidateColor(Color color) { if (!color.IsFinite()) throw new ArgumentException("Highlight colors must be finite.", nameof(color)); }
    private static Dictionary<string, Color> CopyColors(Dictionary<string, Color> colors) { ArgumentNullException.ThrowIfNull(colors); var copy = new Dictionary<string, Color>(colors, StringComparer.Ordinal); foreach (var item in copy) { ArgumentException.ThrowIfNullOrEmpty(item.Key); ValidateColor(item.Value); } return copy; }
    private static bool Symbol(int c) => c <= 32 || c is >= 33 and <= 47 or >= 58 and <= 64 or >= 91 and <= 94 or 96 or >= 123 and <= 126;
    private static void ValidateRegion(string start, string end) { ArgumentException.ThrowIfNullOrEmpty(start); ArgumentNullException.ThrowIfNull(end); if (start.EnumerateRunes().Any(r => !Symbol(r.Value)) || end.EnumerateRunes().Any(r => !Symbol(r.Value))) throw new ArgumentException("Region delimiters must contain symbols."); }
    /// <inheritdoc />
    protected override void OnClearHighlightingCache() { _regionEnds.Clear(); base.OnClearHighlightingCache(); }
    /// <inheritdoc />
    protected override IReadOnlyDictionary<int, Color> OnGetLineSyntaxHighlighting(int line)
    {
        var editor = GetTextEdit()!; var regionIndex = -1;
        for (var prior = 0; prior < line; prior++) { if (!_regionEnds.ContainsKey(prior)) ComputeLine(prior); }
        if (line > 0) regionIndex = _regionEnds[line - 1]; return ComputeLine(line, regionIndex);
    }
    private IReadOnlyDictionary<int, Color> ComputeLine(int line, int? continuing = null)
    {
        var editor = GetTextEdit()!; var text = editor.GetLineWithIME(line).EnumerateRunes().Select(r => r.Value).ToArray(); var map = new SortedDictionary<int, Color>();
        var current = continuing ?? (line > 0 && _regionEnds.TryGetValue(line - 1, out var prior) ? prior : -1); var normal = editor.GetThemeColor(editor.Editable ? "font_color" : "font_readonly_color"); var previous = normal; var i = 0;
        bool Match(int at, string key) { var k = at; foreach (var rune in key.EnumerateRunes()) { if (k >= text.Length || text[k++] != rune.Value) return false; } return true; }
        void Paint(int at, Color color) { if (at == 0 || color != previous) { map[at] = color; previous = color; } }
        while (i < text.Length)
        {
            if (current < 0 && Symbol(text[i])) for (var r = 0; r < _regions.Count; r++) if (Match(i, _regions[r].Start)) { current = r; Paint(i, _regions[r].Color); i += Control.CountScalars(_regions[r].Start); break; }
            if (current >= 0)
            {
                var region = _regions[current]; Paint(i, region.Color); var quote = region.Start is "\"" or "'";
                while (i < text.Length)
                {
                    if (text[i] == '\\') { if (quote) { Paint(i, _symbol); Paint(Math.Min(text.Length, i + 2), region.Color); } i = Math.Min(text.Length, i + 2); continue; }
                    if (region.End.Length > 0 && Match(i, region.End)) { i += Control.CountScalars(region.End); current = -1; break; }
                    i++;
                }
                if (i == text.Length && region.LineOnly) current = -1; if (current < 0) Paint(i, normal); continue;
            }
            if (text[i] is >= '0' and <= '9')
            {
                Paint(i, _number); var hex = i + 1 < text.Length && text[i] == '0' && text[i + 1] is 'x' or 'X'; i++;
                while (i < text.Length && (text[i] is >= '0' and <= '9' or '.' or '_' or 'e' or 'E' or 'f' || hex && text[i] is >= 'a' and <= 'f' or >= 'A' and <= 'F' or 'x' or 'X')) i++;
                Paint(i, normal); continue;
            }
            if (!Symbol(text[i]))
            {
                var start = i; while (i < text.Length && !Symbol(text[i])) i++; var word = string.Concat(text.AsSpan(start, i - start).ToArray().Select(c => new Rune(c).ToString())); var look = i; while (look < text.Length && text[look] is ' ' or '\t') look++; var before = start - 1; while (before >= 0 && text[before] is ' ' or '\t') before--;
                var color = _keywords.TryGetValue(word, out var keyword) ? keyword : _members.TryGetValue(word, out var member) && (before < 0 || text[before] != '.') ? member : look < text.Length && text[look] == '(' ? _function : before >= 0 && text[before] == '.' ? _member : normal;
                Paint(start, color); Paint(i, normal); continue;
            }
            Paint(i, _symbol); i++; Paint(i, normal);
        }
        _regionEnds[line] = current; return map;
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(CodeHighlighter) ? new CodeHighlighter() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { var copy = (CodeHighlighter)target; copy._keywords = new(_keywords, StringComparer.Ordinal); copy._members = new(_members, StringComparer.Ordinal); copy._regionColors = new(_regionColors, StringComparer.Ordinal); copy._regions.Clear(); copy._regions.AddRange(_regions); copy.ClearHighlightingCache(); copy._number = _number; copy._symbol = _symbol; copy._function = _function; copy._member = _member; }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat([
        new PropertyDescriptor<CodeHighlighter, Dictionary<string, Color>>(nameof(KeywordColors), h => h.KeywordColors, (h, v) => h.KeywordColors = v, _ => new(), stored: true),
        new PropertyDescriptor<CodeHighlighter, Dictionary<string, Color>>(nameof(MemberKeywordColors), h => h.MemberKeywordColors, (h, v) => h.MemberKeywordColors = v, _ => new(), stored: true),
        new PropertyDescriptor<CodeHighlighter, Dictionary<string, Color>>(nameof(ColorRegions), h => h.ColorRegions, (h, v) => h.ColorRegions = v, _ => new(), stored: true),
        new PropertyDescriptor<CodeHighlighter, Color>(nameof(NumberColor), h => h.NumberColor, (h, v) => h.NumberColor = v, _ => Colors.Black, stored: true),
        new PropertyDescriptor<CodeHighlighter, Color>(nameof(SymbolColor), h => h.SymbolColor, (h, v) => h.SymbolColor = v, _ => Colors.Black, stored: true),
        new PropertyDescriptor<CodeHighlighter, Color>(nameof(FunctionColor), h => h.FunctionColor, (h, v) => h.FunctionColor = v, _ => Colors.Black, stored: true),
        new PropertyDescriptor<CodeHighlighter, Color>(nameof(MemberVariableColor), h => h.MemberVariableColor, (h, v) => h.MemberVariableColor = v, _ => Colors.Black, stored: true)]);
}
