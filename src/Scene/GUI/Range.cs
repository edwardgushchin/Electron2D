namespace Electron2D;

/// <summary>Base control for a shared numeric range with snapping, ratio conversion and typed notifications.</summary>
/// <remarks>Bounds, value, step, page and allow/exponential policies can be shared. Rounded is local to each control.
/// Ordinary shared notifications run for attached owners; Share explicitly notifies its target even while detached.
/// Shared links and event snapshots are runtime state and are not packed.</remarks>
public abstract class Range : Control
{
    private sealed class Shared
    {
        internal double Min, Max = 100, Value, Step = .01, Page;
        internal bool ExpEdit, AllowGreater, AllowLesser;
        internal readonly List<WeakReference<Range>> Owners = [];
        internal readonly List<List<(Range Owner, ulong Generation)>> Snapshots = [];
        internal int Depth;
        internal Shared Copy() => new() { Min = Min, Max = Max, Value = Value, Step = Step, Page = Page, ExpEdit = ExpEdit, AllowGreater = AllowGreater, AllowLesser = AllowLesser };
        internal void Validate()
        {
            foreach (var weak in Owners) if (weak.TryGetTarget(out var owner) && !owner.IsDisposed) owner.EnsureMutable();
        }
        internal void Notify(bool value, bool signal = true)
        {
            var depth = Depth++;
            if (depth == Snapshots.Count) Snapshots.Add([]);
            var snapshot = Snapshots[depth];
            List<Exception>? errors = null;
            try
            {
                foreach (var weak in Owners)
                    if (weak.TryGetTarget(out var owner) && !owner.IsDisposed && owner.IsInsideTree)
                        snapshot.Add((owner, owner._generation));
                foreach (var entry in snapshot)
                    if (!entry.Owner.IsDisposed && entry.Owner.IsInsideTree && ReferenceEquals(entry.Owner._shared, this) && entry.Owner._generation == entry.Generation)
                        try { entry.Owner.Notify(value, signal); } catch (Exception error) { CollectException(ref errors, error); }
            }
            finally { snapshot.Clear(); Depth--; }
            ThrowCollected("Shared range callbacks failed.", errors);
        }
    }
    private Shared _shared = new();
    private bool _rounded;
    private ulong _generation;
    private WeakReference<Range> _owner;
    private static readonly PropertyDescriptor[] RangeProperties =
    [
        new PropertyDescriptor<Range, double>(nameof(MinValue), node => node.MinValue, (node, value) => node.MinValue = value, _ => 0, stored: true),
        new PropertyDescriptor<Range, double>(nameof(MaxValue), node => node.MaxValue, (node, value) => node.MaxValue = value, _ => 100, stored: true),
        new PropertyDescriptor<Range, double>(nameof(Step), node => node.Step, (node, value) => node.Step = value, _ => .01, stored: true),
        new PropertyDescriptor<Range, double>(nameof(Page), node => node.Page, (node, value) => node.Page = value, _ => 0, stored: true),
        new PropertyDescriptor<Range, bool>(nameof(AllowGreater), node => node.AllowGreater, (node, value) => node.AllowGreater = value, _ => false, stored: true),
        new PropertyDescriptor<Range, bool>(nameof(AllowLesser), node => node.AllowLesser, (node, value) => node.AllowLesser = value, _ => false, stored: true),
        new PropertyDescriptor<Range, bool>(nameof(ExpEdit), node => node.ExpEdit, (node, value) => node.ExpEdit = value, _ => false, stored: true),
        new PropertyDescriptor<Range, bool>(nameof(Rounded), node => node.Rounded, (node, value) => node.Rounded = value, _ => false, stored: true),
        new PropertyDescriptor<Range, double>(nameof(Value), node => node.Value, (node, value) => node.Value = value, _ => 0, stored: true),
    ];

    /// <summary>Initializes a detached range with min zero, max 100 and step 0.01.</summary>
    protected Range() { _owner = new(this); _shared.Owners.Add(_owner); }
    /// <summary>Occurs after min/max/page/step configuration changes, following any value clamp notification.</summary>
    public event Action? Changed;
    /// <summary>Occurs after a changed shared value, following the typed value hook.</summary>
    public event Action<double>? ValueChanged;
    /// <summary>Handles a changed value, including SetValueNoSignal redraw notifications.</summary>
    /// <param name="newValue">The current shared value at delivery time.</param>
    /// <remarks>Override for a concrete value consumer; the state has committed before this hook.</remarks>
    protected virtual void OnValueChanged(double newValue) { }
    private void Check() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void Write() { EnsureMutable(); _shared.Validate(); }
    private static void Finite(double value) { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }

    /// <summary>Gets or sets the shared value, applying step, local integer rounding and bounds in that order.</summary>
    /// <value>Zero initially. NaN is retained; infinities clamp when the corresponding allow flag is false.</value>
    /// <remarks>Ordinary notifications target attached owners. Repeated NaN assignments do not replay ValueChanged.</remarks>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public double Value
    {
        get { Check(); return _shared.Value; }
        set { Write(); SetValue(value, true); }
    }
    private void SetValue(double value, bool signal)
    {
        var previous = _shared.Value;
        _shared.Value = Calculate(value);
        if (_shared.Value != previous && !(signal && double.IsNaN(_shared.Value) && double.IsNaN(previous))) _shared.Notify(true, signal);
    }
    private double Calculate(double value)
    {
        if (double.IsNaN(value)) return value;
        var step = _shared.Step;
        if (step > 0 && double.IsFinite(value))
            value = Math.Abs(_shared.Min) > step * 1e14 ? Snap(value, step) : Snap(value - _shared.Min, step) + _shared.Min;
        if (_rounded) value = Math.Round(value, MidpointRounding.AwayFromZero);
        if (!_shared.AllowGreater && value > _shared.Max - _shared.Page) value = _shared.Max - _shared.Page;
        if (!_shared.AllowLesser && value < _shared.Min) value = _shared.Min;
        return value;
    }
    private static double Snap(double value, double step)
    {
        // Decimal arithmetic preserves user-facing decimal steps without per-update strings or boxing.
        if (Math.Abs(value) <= 1e18 * step && Math.Abs(value) < 1e27 && step >= 1e-27 && step < 1e27)
        {
            var v = (decimal)value; var s = (decimal)step;
            if (s != 0 && Math.Abs(v / s) < 1e27m) return (double)(decimal.Floor(v / s + .5m) * s);
        }
        return Math.Floor(value / step + .5) * step;
    }
    /// <summary>Changes the shared value and redraws owners without emitting ValueChanged.</summary>
    /// <param name="value">The new value, using the same snapping/rounding/bounds as Value.</param>
    /// <remarks>The protected value hook still executes for attached owners.</remarks>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public void SetValueNoSignal(double value) { Write(); SetValue(value, false); }
    /// <summary>Gets or sets the finite shared MinValue configuration.</summary>
    /// <value>0 initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public double MinValue
    {
        get { Check(); return _shared.Min; }
        set
        {
            Finite(value); Write();
            if (_shared.Min == value) return;
            _shared.Min = value;
            _shared.Max = Math.Max(_shared.Max, value);
            _shared.Page = Math.Clamp(_shared.Page, 0, _shared.Max - _shared.Min);
            List<Exception>? errors = null;
            try { SetValue(_shared.Value, true); } catch (Exception error) { CollectException(ref errors, error); }
            try { _shared.Notify(false); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Range configuration callbacks failed.", errors);
        }
    }
    /// <summary>Gets or sets the finite shared MaxValue configuration.</summary>
    /// <value>100 initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public double MaxValue
    {
        get { Check(); return _shared.Max; }
        set
        {
            Finite(value); Write();
            value = Math.Max(value, _shared.Min);
            if (_shared.Max == value) return;
            _shared.Max = value;
            _shared.Page = Math.Clamp(_shared.Page, 0, _shared.Max - _shared.Min);
            List<Exception>? errors = null;
            try { SetValue(_shared.Value, true); } catch (Exception error) { CollectException(ref errors, error); }
            try { _shared.Notify(false); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Range configuration callbacks failed.", errors);
        }
    }
    /// <summary>Gets or sets the finite shared Step configuration.</summary>
    /// <value>.01 initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public double Step
    {
        get { Check(); return _shared.Step; }
        set
        {
            Finite(value); Write();
            if (_shared.Step == value) return;
            _shared.Step = value;
            _shared.Notify(false);
        }
    }
    /// <summary>Gets or sets the finite shared Page configuration.</summary>
    /// <value>0 initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public double Page
    {
        get { Check(); return _shared.Page; }
        set
        {
            Finite(value); Write();
            value = Math.Clamp(value, 0, _shared.Max - _shared.Min);
            if (_shared.Page == value) return;
            _shared.Page = value;
            List<Exception>? errors = null;
            try { SetValue(_shared.Value, true); } catch (Exception error) { CollectException(ref errors, error); }
            try { _shared.Notify(false); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Range configuration callbacks failed.", errors);
        }
    }
    /// <summary>Gets or sets the shared AllowGreater policy.</summary>
    /// <value>False initially; assignment does not resnap the existing value or emit Changed.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public bool AllowGreater { get { Check(); return _shared.AllowGreater; } set { Write(); _shared.AllowGreater = value; } }
    /// <summary>Gets or sets the shared AllowLesser policy.</summary>
    /// <value>False initially; assignment does not resnap the existing value or emit Changed.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public bool AllowLesser { get { Check(); return _shared.AllowLesser; } set { Write(); _shared.AllowLesser = value; } }
    /// <summary>Gets or sets the shared ExpEdit policy.</summary>
    /// <value>False initially; assignment does not resnap the existing value or emit Changed.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public bool ExpEdit { get { Check(); return _shared.ExpEdit; } set { Write(); _shared.ExpEdit = value; } }
    /// <summary>Gets or sets local integer rounding.</summary>
    /// <value>False initially; assignment does not resnap the existing value or emit Changed.</value>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public bool Rounded { get { Check(); return _rounded; } set { Write(); _rounded = value; } }
    /// <summary>Gets or sets the value's linear or exponential ratio between zero and one.</summary>
    /// <remarks>Equal/approximately equal bounds return one. Exponential mode applies only for nonnegative MinValue.
    /// Ratio assignment clamps to bounds before the normal value calculation; Page still caps the final value.</remarks>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public double Ratio
    {
        get
        {
            Check();
            if (Mathf.IsEqualApprox(_shared.Max, _shared.Min)) return 1;
            var value = Math.Clamp(_shared.Value, _shared.Min, _shared.Max);
            if (_shared.ExpEdit && _shared.Min >= 0)
            {
                var min = _shared.Min == 0 ? 0 : Math.Log2(_shared.Min);
                return Math.Clamp((Math.Log2(value) - min) / (Math.Log2(_shared.Max) - min), 0, 1);
            }
            return Math.Clamp((value - _shared.Min) / (_shared.Max - _shared.Min), 0, 1);
        }
        set
        {
            Write(); var result = 0d;
            if (_shared.ExpEdit && _shared.Min >= 0)
            {
                var min = _shared.Min == 0 ? 0 : Math.Log2(_shared.Min);
                result = Math.Pow(2, min + (Math.Log2(_shared.Max) - min) * value);
            }
            else
            {
                var percent = (_shared.Max - _shared.Min) * value;
                result = (_shared.Step > 0 ? Math.Round(percent / _shared.Step, MidpointRounding.AwayFromZero) * _shared.Step : percent) + _shared.Min;
            }
            SetValue(Math.Clamp(result, _shared.Min, _shared.Max), true);
        }
    }
    private void Notify(bool value, bool signal)
    {
        List<Exception>? errors = null;
        if (value)
        {
            try { OnValueChanged(_shared.Value); } catch (Exception error) { CollectException(ref errors, error); }
            try { if (signal) ValueChanged?.Invoke(_shared.Value); } catch (Exception error) { CollectException(ref errors, error); }
        }
        else try { Changed?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed) QueueRedraw();
        ThrowCollected("Range notifications failed.", errors);
    }
    /// <summary>Moves one target control into this control's shared range state and immediately notifies it.</summary>
    /// <param name="with">The live target range; other members of its old group remain there.</param>
    /// <remarks>Rounded remains local. Explicit target Changed and ValueChanged run even while detached.</remarks>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public void Share(Range with)
    {
        ArgumentNullException.ThrowIfNull(with); Write(); with.Write();
        if (!ReferenceEquals(_shared, with._shared)) { with._shared.Owners.Remove(with._owner); with._shared = _shared; _shared.Owners.Add(with._owner); with._generation++; }
        List<Exception>? errors = null;
        try { with.Notify(false, true); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!with.IsDisposed) with.Notify(true, true); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Range sharing callbacks failed.", errors);
    }
    /// <summary>Detaches this control into an independent copy of its shared configuration without notifications.</summary>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, mutation is capture-owned, or a shared peer cannot be mutated.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public void Unshare() { Write(); _shared.Owners.Remove(_owner); _shared = _shared.Copy(); _shared.Owners.Add(_owner); _generation++; }
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings() => ExpEdit && MinValue < 0 ? [.. base.GetConfigurationWarnings(), "Exponential editing requires a nonnegative minimum."] : base.GetConfigurationWarnings();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(RangeProperties);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _shared.Owners.Remove(_owner); _generation++;
        try { base.Dispose(disposing); }
        finally { Changed = null; ValueChanged = null; }
    }
}
