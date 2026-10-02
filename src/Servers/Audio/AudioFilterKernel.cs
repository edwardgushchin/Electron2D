namespace Electron2D;

internal static class AudioFilterKernel
{
    internal enum Mode { LowPass, HighPass, BandPass, Notch, BandLimit, LowShelf, HighShelf }
    internal readonly record struct Settings(Mode Mode, float Cutoff, float Resonance, float Gain, int Stages);
    internal readonly record struct Coefficients(double B0, double B1, double B2, double A1, double A2);
    internal struct History
    {
        private float _input1, _input2, _output1, _output2;
        internal float Process(float input, Coefficients coefficients)
        {
            var output = (float)(input * coefficients.B0 + _input1 * coefficients.B1 + _input2 * coefficients.B2 + _output1 * coefficients.A1 + _output2 * coefficients.A2);
            _input2 = _input1; _input1 = input; _output2 = _output1; _output1 = output; return output;
        }
    }
    internal static Coefficients Prepare(Settings settings, float rate)
    {
        var cutoff = Math.Clamp((double)settings.Cutoff, 1, rate * .4999);
        var omega = Math.Tau * cutoff / rate; var sin = Math.Sin(omega); var cos = Math.Cos(omega);
        var quality = Math.Max(settings.Resonance, .0001); if (settings.Mode == Mode.BandPass) quality *= 2;
        var gain = Math.Max(settings.Gain, .001);
        if (settings.Stages > 1) { if (quality > 1) quality = Math.Pow(quality, 1d / settings.Stages); gain = Math.Pow(gain, 1d / (settings.Stages + 1)); }
        quality = Math.Min(quality, sin / (2e-12));
        var alpha = Math.Max(sin / (2 * quality), 1e-12); var a0 = 1 + alpha; double b0, b1, b2, a1, a2;
        switch (settings.Mode)
        {
            case Mode.LowPass:
                b0 = b2 = (1 - cos) / 2; b1 = 1 - cos; a1 = -2 * cos; a2 = 1 - alpha; break;
            case Mode.HighPass:
                b0 = b2 = (1 + cos) / 2; b1 = -(1 + cos); a1 = -2 * cos; a2 = 1 - alpha; break;
            case Mode.BandPass:
                b0 = alpha * Math.Sqrt(quality + 1); b1 = 0; b2 = -b0; a1 = -2 * cos; a2 = 1 - alpha; break;
            case Mode.Notch:
                b0 = b2 = 1; b1 = -2 * cos; a1 = -2 * cos; a2 = 1 - alpha; break;
            case Mode.BandLimit:
                var lower = Math.Clamp((double)settings.Resonance, .0001, cutoff * (1 - 1e-6)); var center = (cutoff + lower) / 2;
                var bandwidth = (Math.Log(center) - Math.Log(lower)) / Math.Log(2); omega = Math.Tau * center / rate;
                alpha = Math.Max(Math.Sin(omega) * Math.Sinh(Math.Log(2) / 2 * bandwidth * omega / Math.Sin(omega)), 1e-12); a0 = 1 + alpha;
                b0 = b2 = 1; b1 = -2 * Math.Cos(omega); a1 = b1; a2 = 1 - alpha; break;
            case Mode.LowShelf:
            case Mode.HighShelf:
                var beta = Math.Sqrt(gain) / Math.Max(Math.Sqrt(quality), .001); var sign = settings.Mode == Mode.LowShelf ? 1 : -1;
                var damping = Math.Max(beta * sin, (gain + 1 + sign * (gain - 1) * cos) * 1e-12);
                a0 = gain + 1 + sign * (gain - 1) * cos + damping;
                b0 = gain * (gain + 1 - sign * (gain - 1) * cos + damping);
                b1 = sign * 2 * gain * (gain - 1 - sign * (gain + 1) * cos);
                b2 = gain * (gain + 1 - sign * (gain - 1) * cos - damping);
                a1 = -sign * 2 * (gain - 1 + sign * (gain + 1) * cos);
                a2 = gain + 1 + sign * (gain - 1) * cos - damping; break;
            default: throw new ArgumentOutOfRangeException(nameof(settings));
        }
        var result = new Coefficients(b0 / a0, b1 / a0, b2 / a0, -a1 / a0, -a2 / a0);
        if (!double.IsFinite(result.B0) || !double.IsFinite(result.B1) || !double.IsFinite(result.B2) || !double.IsFinite(result.A1) || !double.IsFinite(result.A2)) throw new ArithmeticException("Filter coefficients are not finite.");
        return result;
    }
}
