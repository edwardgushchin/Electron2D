namespace Electron2D;

public partial class RichTextLabel
{
    private readonly List<RichTextEffect> _scriptEffects = [];
    /// <summary>Constructs and installs a compiled C# effect resource from a Script asset.</summary>
    /// <param name="script">Live registered Script whose compiled type derives from RichTextEffect.</param>
    /// <returns>The label-owned effect instance, usable for typed configuration while the label retains it.</returns>
    /// <remarks>The source asset remains borrowed. The constructed effect runs through the ordinary virtual effect hook;
    /// disposing the Script does not unload compiled code. Removing owned effects from CustomEffects retires them.</remarks>
    /// <exception cref="InvalidOperationException">The script has no compatible factory or document mutation is currently forbidden.</exception>
    /// <exception cref="ObjectDisposedException">The label or source script is disposed.</exception>
    public RichTextEffect InstallEffect(Script script)
    {
        MutableRich(); ArgumentNullException.ThrowIfNull(script); var effect = script.New<RichTextEffect>();
        _scriptEffects.Add(effect);
        try { InstallEffect(effect); return effect; }
        catch { if (!_effects.Contains(effect)) { _scriptEffects.Remove(effect); effect.Dispose(); } throw; }
    }
    private void RetireScriptEffects(bool all = false)
    {
        List<Exception>? errors = null;
        for (var i = _scriptEffects.Count - 1; i >= 0; i--)
        {
            var effect = _scriptEffects[i]; if (!all && _effects.Contains(effect)) continue;
            _scriptEffects.RemoveAt(i); try { effect.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (errors != null) throw new AggregateException("Script effect cleanup failed.", errors);
    }
}
