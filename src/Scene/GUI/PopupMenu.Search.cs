using System.Text;

namespace Electron2D;

public partial class PopupMenu
{
    private readonly record struct SearchMatch(int Start, int End, int Misses, int Score);
    private static readonly string SearchBoundaries = "/\\-_.";
    private static bool Boundary(Rune[] target, int index) => index < 0 || index >= target.Length || SearchBoundaries.Contains((char)target[index].Value);
    private static int ExactIndex(Rune[] target, Rune[] token, int offset)
    { for (var i = offset; i <= target.Length - token.Length; i++) if (target.AsSpan(i, token.Length).SequenceEqual(token)) return i; return -1; }
    private bool SearchScore(string text, (Rune[] Text, int Order)[] tokens, out int score)
    {
        var original = text.EnumerateRunes().ToArray(); var target = original.Select(Rune.ToLowerInvariant).ToArray(); var budget = _misses; score = 0; var intervals = new List<SearchMatch>(); var starts = new int[tokens.Length]; var directory = Array.FindLastIndex(original, r => r.Value == '/');
        foreach (var entry in tokens)
        {
            var token = entry.Text;
            SearchMatch? best = null;
            for (var offset = 0; offset <= target.Length;)
            {
                var runs = new List<(int Start, int Length)>(); var misses = 0; var first = -1; var last = -1; var cursor = offset;
                if (!_fuzzy) { var index = ExactIndex(target, token, cursor); if (index < 0) break; first = index; last = index + token.Length - 1; runs.Add((index, token.Length)); }
                else
                {
                    var run = -1; var length = 0;
                    foreach (var c in token)
                    {
                        var index = Array.IndexOf(target, c, cursor); if (index < 0) { if (++misses > budget) break; continue; }
                        if (run < 0 || cursor != index) { if (run >= 0) runs.Add((run, length)); run = index; length = 1; } else length++;
                        if (first < 0) first = index; last = index; cursor = index + 1;
                    }
                    if (misses > budget) break; if (run >= 0) runs.Add((run, length));
                }
                var overlaps = first >= 0 && intervals.Any(i => i.Start >= 0 && last >= i.Start && first <= i.End);
                if (!overlaps)
                {
                    var candidate = -20 * misses; var insensitive = false;
                    foreach (var run in runs)
                    {
                        for (var i = run.Start; i < run.Start + run.Length; i++) if (original[i] != target[i]) insensitive = true;
                        var points = run.Length * run.Length; if (run.Start > directory) points *= 2; if (Boundary(original, run.Start - 1) || Boundary(original, run.Start + run.Length)) points += 4; if (run.Length == token.Length) points += 100; candidate += points;
                    }
                    if (insensitive) candidate -= 3;
                    if (best is null || best.Value.Score < candidate) best = new(first, last, misses, candidate);
                }
                if (first < 0) break; offset = first + 1;
            }
            if (best is null) return false; intervals.Add(best.Value); starts[entry.Order] = best.Value.Start; budget -= best.Value.Misses; score += best.Value.Score;
        }
        var ordered = true; for (var i = 1; i < starts.Length; i++) if (starts[i] < starts[i - 1]) { ordered = false; break; }
        if (ordered) score++;
        return true;
    }
    private void FilterChanged(string query)
    {
        if (IsDisposed) return; var tokens = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select((text, index) => (Text: text.EnumerateRunes().ToArray(), Order: index)).OrderByDescending(t => t.Text.Length).ToArray();
        FilterCore(query, tokens); _focused = -1; _dirty = true; Arrange();
    }
    private void FilterCore(string query, (Rune[] Text, int Order)[] tokens)
    {
        foreach (var item in _items) if (item.Submenu is { IsDisposed: false } child) child.FilterCore(query, tokens);
        var results = new List<(Item Item, int Score)>(); float total = 0; var maximum = 0;
        foreach (var item in _items)
        {
            item.Visible = query.Length == 0; if (query.Length == 0) continue;
            if (item.Submenu is { IsDisposed: false } submenu) foreach (var nested in submenu._items) if (nested.Visible) { item.Visible = true; break; }
            if (!item.Separator && SearchScore(item.Text, tokens, out var score)) { results.Add((item, score)); total += score; maximum = Math.Max(maximum, score); }
        }
        if (results.Count > 0)
        {
            var threshold = Math.Min(30, total / results.Count * .9f + maximum * .1f);
            foreach (var result in results) if (result.Score >= threshold) { result.Item.Visible = true; if (result.Item.Submenu is { IsDisposed: false } child) foreach (var nested in child._items) nested.Visible = true; }
        }
        _dirty = true;
    }
}
