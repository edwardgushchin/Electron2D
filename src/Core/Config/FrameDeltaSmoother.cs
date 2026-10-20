namespace Electron2D;

internal sealed class FrameDeltaSmoother
{
    private long _period = 16666, _remaining, _sampleSum, _measurementSum;
    private int _samples, _measurementFrames, _rate, _equal, _faster, _slower, _oneFaster, _oneSlower;
    private bool _ready, _locked, _allowed = true;

    internal void Reset()
    {
        _period = 16666; _remaining = _sampleSum = _measurementSum = 0;
        _samples = _measurementFrames = _rate = _equal = _faster = _slower = _oneFaster = _oneSlower = 0;
        _ready = _locked = false; _allowed = true;
    }
    internal long Smooth(long microseconds, bool enabled, bool classicalVSync, out bool applied)
    {
        applied = false;
        if (!enabled || !classicalVSync || microseconds < 0 || microseconds > 1_000_000) return microseconds;
        if (!Measure(microseconds) || microseconds < 1000) return microseconds;
        Estimate(microseconds);
        if (!_ready) return microseconds;
        applied = true;
        _remaining += microseconds;
        var units = Math.Max(1, _remaining / _period);
        var result = units * _period; _remaining -= result; return result;
    }
    private bool Measure(long delta)
    {
        _measurementSum += delta;
        if (++_measurementFrames == 64)
        {
            var average = _measurementSum / 64;
            if (_ready && average != 0)
            {
                var ratio = 1_000_000d / average / _rate;
                _allowed = ratio is > .95 and < 1.05;
            }
            _measurementSum = 0; _measurementFrames = 0;
        }
        return _allowed;
    }
    private void Estimate(long delta)
    {
        if (_locked) return;
        _sampleSum += delta;
        if (++_samples < 60) return;
        var average = _sampleSum / 60; _samples = 0; _sampleSum = 0;
        var observed = (int)Math.Round(1_000_000d / average, MidpointRounding.AwayFromZero);
        if (_rate == 0)
        {
            if (observed < 50) return;
            _rate = observed;
        }
        if (observed == _rate)
        {
            _equal++;
            if (_ready && _equal == 20) { _locked = true; return; }
            if (!_ready && _equal > 2) { _ready = true; _period = 1_000_000 / _rate; }
        }
        else if (observed < _rate)
        {
            if (observed == _rate - 1)
            {
                if (++_oneSlower > _equal && _oneSlower > 2) ChangeEstimate(_rate - 1);
            }
            else
            {
                _slower++;
                if (!(_ready && _equal > 10) && _slower / 8 > _equal && _slower > 2) ChangeEstimate(_rate - 1);
            }
        }
        else if (observed == _rate + 1)
        {
            if (++_oneFaster > _equal && _oneFaster > 1) ChangeEstimate(_rate + 1);
        }
        else if (++_faster > _equal && _faster > 1)
            ChangeEstimate(_rate + Math.Max(1, (observed - _rate) / 2));
    }
    private void ChangeEstimate(int rate)
    { _rate = rate; _equal = _faster = _slower = _oneFaster = _oneSlower = 0; _ready = false; }
}
