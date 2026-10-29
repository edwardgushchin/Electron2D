namespace Electron2D;

// A present endpoint (including None) marks sampled custom ranges; built-in frames keep their unfolded clock.
internal readonly record struct AnimationEventRange(double Lower, double Upper, bool Backward, bool Point = false)
{
    private const double Epsilon = 0.00001;
    private static bool Approximately(double left, double right) =>
        Mathf.IsEqualApprox(left, right, Epsilon * Math.Max(1, Math.Abs(left)));

    internal bool TryNext(List<double> times, ref int cursor, out int key)
    {
        key = -1;
        if (cursor == int.MinValue) return false;
        if (Point)
        {
            key = times.BinarySearch(Lower);
            cursor = int.MinValue;
            return key >= 0;
        }
        cursor = Backward
            ? cursor == times.Count ? Bound(times, false) - 1 : cursor - 1
            : cursor == -1 ? Bound(times, true) : cursor + 1;
        if (cursor < 0 || cursor >= times.Count || BeforeLower(times[cursor]) || AfterUpper(times[cursor]))
        { cursor = int.MinValue; return false; }
        key = cursor;
        return true;
    }

    internal int Last(List<double> times)
    {
        if (Point) return times.BinarySearch(Lower);
        var key = Backward ? Bound(times, true) : Bound(times, false) - 1;
        return key >= 0 && key < times.Count && !BeforeLower(times[key]) && !AfterUpper(times[key]) ? key : -1;
    }

    private bool BeforeLower(double time) => Backward
        ? time < Lower && !Approximately(time, Lower)
        : time <= Lower || Approximately(time, Lower);

    private bool AfterUpper(double time) => Backward
        ? time >= Upper || Approximately(time, Upper)
        : time > Upper && !Approximately(time, Upper);

    private int Bound(List<double> times, bool lower)
    {
        var begin = 0; var end = times.Count;
        while (begin < end)
        {
            var middle = begin + (end - begin) / 2;
            if (lower ? BeforeLower(times[middle]) : !AfterUpper(times[middle])) begin = middle + 1;
            else end = middle;
        }
        return begin;
    }

    internal static int Get(in AnimationMixFrame frame, Span<AnimationEventRange> ranges)
    {
        if (frame.Movement == 0) return 0;
        var from = frame.Time - frame.Movement;
        var to = frame.Time;
        Animation.Finite(from);
        var start = Math.Max(0, frame.Start);
        var end = frame.End < 0 ? frame.Animation.Length : Math.Min(frame.End, frame.Animation.Length);
        if (end <= start) return 0;
        var backward = from > to;
        if (backward) (from, to) = (to, from);
        switch (frame.Animation.LoopMode)
        {
            case SpriteFrames.LoopMode.None:
                from = Math.Clamp(from, 0, end);
                to = Math.Clamp(to, 0, end);
                break;
            case SpriteFrames.LoopMode.Linear:
                if (from > end || from < start) from = Mathf.PosMod(from, end);
                if (to > end || to < start) to = Mathf.PosMod(to, end);
                if (from > to)
                {
                    ranges[backward ? 1 : 0] = new(from, end + Epsilon, backward);
                    ranges[backward ? 0 : 1] = new(start - Epsilon, to, backward);
                    return 2;
                }
                if (frame.LoopedFlag != Animation.LoopedFlag.None &&
                    (!backward && Approximately(from, start) || backward && Approximately(to, end)))
                {
                    var edge = backward ? end : start;
                    ranges[0] = new(edge, edge, backward, Point: true);
                    ranges[1] = new(from, to, backward);
                    return 2;
                }
                break;
            case SpriteFrames.LoopMode.PingPong:
                if (from > end || from < start) from = Mathf.PingPong(from, frame.Animation.Length);
                if (to > end || to < start) to = Mathf.PingPong(to, frame.Animation.Length);
                if (frame.LoopedFlag == Animation.LoopedFlag.Start)
                {
                    ranges[0] = new(start, from, true);
                    ranges[1] = new(start, to, false);
                    return 2;
                }
                if (frame.LoopedFlag == Animation.LoopedFlag.End)
                {
                    ranges[0] = new(from, end, false);
                    ranges[1] = new(to, end, true);
                    return 2;
                }
                if (!backward && Approximately(to, end)) to -= Epsilon;
                else if (backward && Approximately(from, start)) from += Epsilon;
                break;
        }
        ranges[0] = new(from, to, backward);
        return 1;
    }

}
