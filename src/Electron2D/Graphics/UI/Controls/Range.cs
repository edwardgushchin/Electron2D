/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace Electron2D;

/// <summary>
/// Provides a numeric value with minimum, maximum, step and ratio state.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>Range</c> is the shared base for controls such as <see cref="Slider"/>
/// and <see cref="ProgressBar"/>. It emits <c>value_changed</c> when
/// <see cref="Value"/> changes through the normal setter.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate ranges on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Slider"/>
/// <seealso cref="ProgressBar"/>
public class Range : Control
{
    private double minValue;
    private double maxValue = 100d;
    private double value;
    private double step = 1d;
    private double page;
    private bool allowGreater;
    private bool allowLesser;

    /// <summary>
    /// Initializes a new instance of the <see cref="Range"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The constructor registers the <c>value_changed</c> signal.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Range"/>
    public Range()
    {
        AddUserSignal("value_changed");
    }

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    /// <value>The lower bound used by <see cref="Value"/> and <see cref="Ratio"/>.</value>
    /// <remarks>If assigned above <see cref="MaxValue"/>, the maximum is moved to the same value.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not finite.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="MaxValue"/>
    public double MinValue
    {
        get => minValue;
        set
        {
            minValue = ValidateFinite(value, nameof(MinValue));
            if (maxValue < minValue)
            {
                maxValue = minValue;
            }

            SetValueCore(this.value, emitSignal: false);
        }
    }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    /// <value>The upper bound used by <see cref="Value"/> and <see cref="Ratio"/>.</value>
    /// <remarks>If assigned below <see cref="MinValue"/>, the minimum is moved to the same value.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is not finite.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="MinValue"/>
    public double MaxValue
    {
        get => maxValue;
        set
        {
            maxValue = ValidateFinite(value, nameof(MaxValue));
            if (minValue > maxValue)
            {
                minValue = maxValue;
            }

            SetValueCore(this.value, emitSignal: false);
        }
    }

    /// <summary>
    /// Gets or sets the step used when snapping values.
    /// </summary>
    /// <value><c>0</c> to disable snapping, or a positive finite step.</value>
    /// <remarks>Non-zero values snap to the nearest step from <see cref="MinValue"/>.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is negative or not finite.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Value"/>
    public double Step
    {
        get => step;
        set
        {
            if (!double.IsFinite(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(Step), value, "Step must be finite and non-negative.");
            }

            step = value;
            SetValueCore(this.value, emitSignal: false);
        }
    }

    /// <summary>
    /// Gets or sets the page size associated with this range.
    /// </summary>
    /// <value>A finite non-negative page value.</value>
    /// <remarks>The preview stores page size for API consumers; sliders do not render page indicators.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is negative or not finite.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Range"/>
    public double Page
    {
        get => page;
        set
        {
            if (!double.IsFinite(value) || value < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(Page), value, "Page must be finite and non-negative.");
            }

            page = value;
        }
    }

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    /// <value>The normalized value after clamping and snapping.</value>
    /// <remarks>Changing this property emits <c>value_changed</c> when the normalized value changes.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is not finite.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="SetValueNoSignal(double)"/>
    public double Value
    {
        get => value;
        set => SetValueCore(value, emitSignal: true);
    }

    /// <summary>
    /// Gets or sets the value as a ratio between minimum and maximum.
    /// </summary>
    /// <value>A value in the range <c>0.0</c> through <c>1.0</c>.</value>
    /// <remarks>Assigning this property maps the ratio to <see cref="Value"/>.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is not finite.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Value"/>
    public double Ratio
    {
        get
        {
            var span = maxValue - minValue;
            return span <= 0d ? 0d : Math.Clamp((value - minValue) / span, 0d, 1d);
        }
        set
        {
            var ratio = ValidateFinite(value, nameof(Ratio));
            ratio = Math.Clamp(ratio, 0d, 1d);
            Value = minValue + ((maxValue - minValue) * ratio);
        }
    }

    /// <summary>
    /// Gets or sets whether values are rounded to integers after step snapping.
    /// </summary>
    /// <value><c>true</c> to round values; otherwise, <c>false</c>.</value>
    /// <remarks>Rounding uses midpoint-away-from-zero behavior.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Value"/>
    public bool Rounded { get; set; }

    /// <summary>
    /// Gets or sets whether values above <see cref="MaxValue"/> are allowed.
    /// </summary>
    /// <value><c>true</c> to allow greater values; otherwise, <c>false</c>.</value>
    /// <remarks>Disabling this property clamps the current value if needed.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="AllowLesser"/>
    public bool AllowGreater
    {
        get => allowGreater;
        set
        {
            allowGreater = value;
            SetValueCore(this.value, emitSignal: false);
        }
    }

    /// <summary>
    /// Gets or sets whether values below <see cref="MinValue"/> are allowed.
    /// </summary>
    /// <value><c>true</c> to allow lesser values; otherwise, <c>false</c>.</value>
    /// <remarks>Disabling this property clamps the current value if needed.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="AllowGreater"/>
    public bool AllowLesser
    {
        get => allowLesser;
        set
        {
            allowLesser = value;
            SetValueCore(this.value, emitSignal: false);
        }
    }

    /// <summary>
    /// Gets or sets whether exponential editing is requested by UI controls.
    /// </summary>
    /// <value><c>true</c> when exponential editing is requested; otherwise, <c>false</c>.</value>
    /// <remarks>The preview stores this flag for API consumers; slider input remains linear.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Slider"/>
    public bool ExpEdit { get; set; }

    /// <summary>
    /// Sets <see cref="Value"/> without emitting <c>value_changed</c>.
    /// </summary>
    /// <param name="value">The value to assign.</param>
    /// <remarks>The assigned value is still clamped and snapped.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is not finite.</exception>
    /// <threadsafety>This method is not synchronized. Call it on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Value"/>
    public void SetValueNoSignal(double value)
    {
        SetValueCore(value, emitSignal: false);
    }

    internal void OffsetValue(double delta)
    {
        Value += delta;
    }

    private void SetValueCore(double rawValue, bool emitSignal)
    {
        var normalized = NormalizeValue(rawValue);
        if (this.value.Equals(normalized))
        {
            return;
        }

        this.value = normalized;
        if (emitSignal)
        {
            EmitSignal("value_changed", this.value);
        }

        QueueRedraw();
    }

    private double NormalizeValue(double rawValue)
    {
        var normalized = ValidateFinite(rawValue, nameof(Value));
        if (!AllowLesser)
        {
            normalized = Math.Max(minValue, normalized);
        }

        if (!AllowGreater)
        {
            normalized = Math.Min(maxValue, normalized);
        }

        if (step > 0d)
        {
            normalized = minValue + (Math.Round((normalized - minValue) / step, MidpointRounding.AwayFromZero) * step);
        }

        if (Rounded)
        {
            normalized = Math.Round(normalized, MidpointRounding.AwayFromZero);
        }

        if (!AllowLesser)
        {
            normalized = Math.Max(minValue, normalized);
        }

        if (!AllowGreater)
        {
            normalized = Math.Min(maxValue, normalized);
        }

        return normalized;
    }

    private static double ValidateFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
        }

        return value;
    }
}
