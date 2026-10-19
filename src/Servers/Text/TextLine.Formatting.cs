using System.Text;

namespace Electron2D;

public sealed partial class TextLine
{
    private readonly int[] _serverSpacing = new int[4];
    internal string ServerPunctuation = string.Empty;
    private bool _serverFullFormatted, _serverFitMinimum;
    private TextLayout? _serverRawWork, _serverDisplayWork;
    internal int ServerEllipsis => Rune.TryGetRuneAt(_ellipsis, 0, out var rune) ? rune.Value : 0x2026;
    internal int ServerSpacing(TextSpacingType type) { Check(); return _serverSpacing[(int)type]; }
    internal void ServerSetSpacing(TextSpacingType type, int value)
    { CheckMutation(); if (_serverSpacing[(int)type] == value) return; _serverSpacing[(int)type] = value; _dirty = true; ServerChanged?.Invoke(); }
    internal void ServerSetPunctuation(string value)
    {
        CheckMutation(); ArgumentNullException.ThrowIfNull(value); if (value.Length > 33554432) throw new ArgumentException("Punctuation exceeds text limits.", nameof(value));
        if (ServerPunctuation == value) return; ServerPunctuation = value; _dirty = true; ServerChanged?.Invoke();
    }
    internal void ServerSetEllipsis(int scalar)
    { CheckMutation(); _ellipsis = new Rune(scalar).ToString(); }
    private void ServerCopyFormatSettings(TextLine copy)
    { _serverSpacing.CopyTo(copy._serverSpacing, 0); copy.ServerPunctuation = ServerPunctuation; }
    internal TextLayout ServerPreparedDisplay { get { Ensure(); return _display; } }
    internal int ServerTrimPosition(bool ellipsis)
    {
        Check(); if (_dirty || FontsChanged() || _display.LineCount == 0) return -1;
        return ellipsis ? _display.Lines[0].EllipsisPosition : _display.Lines[0].TrimPosition;
    }
    internal float ServerFit(float width, TextJustificationFlags flags)
    {
        Ensure(); _busy = true;
        try
        {
            BeginPreparedReads(); var constrain = flags.HasFlag(TextJustificationFlags.ConstrainEllipsis);
            if (constrain && (_display.LineCount == 0 || _display.Lines[0].TrimPosition < 0)) return MathF.Ceiling(_full.LineCount == 0 ? 0 : _full.Lines[0].Width);
            _serverRawWork ??= new(); _serverDisplayWork ??= new(); _full.ServerCopyFormatting(_serverRawWork); _display.ServerCopyFormatting(_serverDisplayWork);
            var result = constrain ? _serverRawWork.ServerFitVisible(_serverDisplayWork, width, flags) : _serverRawWork.ServerFit(width, flags);
            var minimum = MathF.Floor(result) > MathF.Floor(width);
            if (!constrain)
            {
                _serverRawWork.ServerCopyFormatting(_serverDisplayWork);
                _serverDisplayWork.ServerReapplyTrim(_display, _full);
            }
            if (!ReadsCurrent()) throw Font.UnsettledRead();
            (_full, _serverRawWork) = (_serverRawWork, _full); (_display, _serverDisplayWork) = (_serverDisplayWork, _display);
            _serverFitMinimum = minimum; _serverFullFormatted = true; ServerReleaseSnapshot(); _revision++; ServerChanged?.Invoke(); return MathF.Ceiling(result);
        }
        finally { try { EndReads(); } finally { _busy = false; } }
    }
    internal void ServerTrim(float width, TextOverrunFlags flags)
    {
        Ensure(); _busy = true;
        try
        {
            BeginPreparedReads(); _serverDisplayWork ??= new(); _full.ServerCopyFormatting(_serverDisplayWork); _serverDisplayWork.ServerTrim(width, flags, ServerEllipsis, _serverFitMinimum);
            if (!ReadsCurrent()) throw Font.UnsettledRead();
            (_display, _serverDisplayWork) = (_serverDisplayWork, _display);
            _revision++; ServerChanged?.Invoke();
        }
        finally { try { EndReads(); } finally { _busy = false; } }
    }
    internal void ServerLoadPreparedShape(TextLayout source, TextLayout display, int line, int from, int to)
    { Ensure(); source.ServerCopySlice(_full, from, to); display.ServerCopyDisplayLine(_display, source, line, from, to); _full.ServerAdoptFormattedDisplay(_display); _serverFullFormatted = true; _revision++; }
}
