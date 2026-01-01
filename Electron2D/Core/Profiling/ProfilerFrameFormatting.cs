using System;
using System.Globalization;
using System.Text;

namespace Electron2D;

public static class ProfilerFrameFormatting
{
    public static string ToPrettyString(this ProfilerFrame f, bool includeZeros = false)
    {
        if (!f.IsValid) return "<ProfilerFrame: invalid>";

        var inv = CultureInfo.InvariantCulture;

        // ===== Layout constants (line length is stable) =====
        const int TotalWidth   = 76;              // full line length incl. borders
        const int InnerWidth   = TotalWidth - 2;  // between left/right border chars
        const int LineTextW    = TotalWidth - 4;  // for "│ " + text + " │"
        const int CellW        = 22;              // 3 columns layout: "│ " + c1 + " │ " + c2 + " │ " + c3 + " │"
        const int NameW        = 13;
        const int ValW         = CellW - NameW - 1; // "Name(13) + space + Val(8)" => 22

        // ===== Build =====
        var sb = new StringBuilder(2048);

        sb.Append(Top("ProfilerFrame"));

        var header =
            $"Frame #{f.FrameIndex} | {f.FrameMs.ToString("F2", inv)} ms" +
            $" | alloc {Kb(f.AllocatedBytes, inv)} KB" +
            $" | GC Δ {f.Gen0Collections}/{f.Gen1Collections}/{f.Gen2Collections}" +
            $" | fixedSteps {f.FixedSteps}";
        Line(sb, header);

        sb.Append(Mid("Timings (ms)"));
        Row3(sb, MsCell("EventsPump",   f.EventsPumpMs),      MsCell("InputPoll",    f.InputPollMs),      MsCell("EventsSwap",   f.EventsSwapMs));
        Row3(sb, MsCell("HandleQuit",   f.HandleQuitCloseMs), MsCell("DispatchInput",f.DispatchInputMs),  MsCell("FlushFreeQ",   f.FlushFreeQueueMs));
        Row3(sb, MsCell("FixedStep",    f.FixedStepMs),       MsCell("Process",     f.ProcessMs),        MsCell("RenderTotal",  f.RenderTotalMs));

        sb.Append(Mid("Render timings (ms)"));
        Row3(sb, MsCell("BeginFrame",   f.RenderBeginFrameMs), MsCell("BuildQueue",  f.RenderBuildQueueMs), MsCell("Sort",       f.RenderSortMs));
        Row3(sb, MsCell("Flush",        f.RenderFlushMs),      MsCell("Present",    f.RenderPresentMs),     MsCell("FrameTotal", f.FrameMs));

        sb.Append(Mid("Events/Input"));
        Row2(sb,
            TextCell("Read e/w/i", $"{f.EventsEngineRead}/{f.EventsWindowRead}/{f.EventsInputRead}", hideIfZeroTriple: true),
            TextCell("Drop e/w/in",$"{f.EventsDroppedEngine}/{f.EventsDroppedWindow}/{f.InputDroppedEvents}", hideIfZeroTriple: true)
        );

        sb.Append(Mid("Render counters"));
        Row3(sb, IntCell("Sprites",     f.RenderSprites),        IntCell("DrawCalls",  f.RenderDrawCalls),      IntCell("UniqueTex",  f.RenderUniqueTextures));
        Row3(sb, IntCell("Binds",       f.RenderTextureBinds),   IntCell("DebugLines", f.RenderDebugLines),     IntCell("SortTrig",   f.RenderSortTriggered));
        Row3(sb, IntCell("Clears",      f.RenderClears),         IntCell("Presents",  f.RenderPresents),       IntCell("SortCmds",   f.RenderSortCommands));

        sb.Append(Bot());
        return sb.ToString();

        static string Kb(long bytes, CultureInfo inv)
            => (bytes / 1024.0).ToString("F1", inv);

        void Row2(StringBuilder sb, string c1, string c2)
        {
            Row3(sb, c1, c2, new string(' ', CellW));
        }

        void Row3(StringBuilder sb, string c1, string c2, string c3)
        {
            sb.Append("│ ").Append(c1).Append(" │ ").Append(c2).Append(" │ ").Append(c3).Append(" │\n");
        }

        void Line(StringBuilder sb, string text)
        {
            sb.Append('│').Append(' ')
                .Append(Clamp(text, LineTextW).PadRight(LineTextW))
                .Append(' ').Append('│')
                .Append('\n');
        }

        string TextCell(string name, string v, bool hideIfZeroTriple = false)
        {
            var shown = v;

            if (!includeZeros && hideIfZeroTriple && (v == "0/0/0" || v == "0/0/0/0"))
                shown = "—";

            if (shown.Length > ValW) shown = shown[..ValW];
            return PadOrTrim(Clamp(name, NameW), NameW) + " " + shown.PadLeft(ValW);
        }

        string IntCell(string name, int v)
        {
            // Счетчики часто полезнее видеть как 0, чем как "—".
            // Но если ты хочешь поведение как раньше — поменяй условие на (includeZeros || v != 0) ? v.ToString(...) : "—"
            var shown = v.ToString(inv);
            if (shown.Length > ValW) shown = shown[..ValW];
            return PadOrTrim(Clamp(name, NameW), NameW) + " " + shown.PadLeft(ValW);
        }

        string MsCell(string name, double v)
        {
            var shown = (includeZeros || v != 0.0)
                ? v.ToString("F2", inv)
                : "—";

            if (shown.Length > ValW) shown = shown[..ValW];
            return PadOrTrim(Clamp(name, NameW), NameW) + " " + shown.PadLeft(ValW);
        }

        static string PadOrTrim(string s, int w)
            => s.Length >= w ? s[..w] : s.PadRight(w);

        string Bot() => "└" + new string('─', InnerWidth) + "┘";

        string Mid(string title) => Border('├', '┤', title);

        string Top(string title) => Border('┌', '┐', title);

        string Border(char left, char right, string title)
        {
            var t = " " + title + " ";
            if (t.Length > InnerWidth) t = t[..InnerWidth];

            var padLeft  = (InnerWidth - t.Length) / 2;
            var padRight = InnerWidth - t.Length - padLeft;

            return left
                   + new string('─', padLeft)
                   + t
                   + new string('─', padRight)
                   + right
                   + "\n";
        }

        static string Clamp(string s, int max)
        {
            if (s.Length <= max) return s;
            if (max <= 1) return s[..max];
            return s[..(max - 1)] + "…";
        }
    }
}
