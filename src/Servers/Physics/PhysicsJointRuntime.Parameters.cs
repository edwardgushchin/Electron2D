namespace Electron2D;

internal sealed partial class PhysicsJointRuntime
{
    private const int SolverPolicy = 1, PinLimitsPolicy = 2, MotorEnabledPolicy = 4,
        MotorVelocityPolicy = 8, MotorTorquePolicy = 16, GrooveLimitsPolicy = 32;
    private int _pendingParameters;
    private bool _preparingParameters;

    private bool IsParameterPending(int policy) => (_pendingParameters & policy) != 0;

    private void PublishParameterPolicy(int policy)
    {
        if (!HasBackend) return;
        if (_pendingParameters == 0) Space!.ChangePendingJointPolicies(1);
        _pendingParameters |= policy;
        PreparePendingParameters(policy);
    }

    private void ClearPendingParameters()
    {
        if (_pendingParameters == 0) return;
        _pendingParameters = 0;
        Space!.ChangePendingJointPolicies(-1);
    }

    internal void RetryGrooveLimits() => PreparePendingParameters(GrooveLimitsPolicy);
    internal void PreparePendingParameters() => PreparePendingParameters(_pendingParameters);

    private void PreparePendingParameters(int policies)
    {
        policies &= _pendingParameters;
        if (policies == 0) return;
        if (_preparingParameters) throw new InvalidOperationException("Joint parameter preparation cannot reenter its own publication.");
        EnsureWriteAccess();
        _preparingParameters = true;
        try
        {
            for (var policy = SolverPolicy; policy <= GrooveLimitsPolicy; policy <<= 1)
            {
                if ((policies & policy) == 0) continue;
                switch (policy)
                {
                    case SolverPolicy: Backend.ApplySolverPolicy(this); break;
                    case PinLimitsPolicy: Backend.SetPinLimits(PinLimitEnabled, PinLimitLower, PinLimitUpper); break;
                    case MotorEnabledPolicy: Backend.SetPinMotorEnabled(PinMotorEnabled); break;
                    case MotorVelocityPolicy: Backend.SetPinMotorVelocity(PinMotorVelocity); break;
                    case MotorTorquePolicy: Backend.SetPinMotorMaxTorque(PinMotorMaxTorque); break;
                    case GrooveLimitsPolicy: Backend.SetGrooveLimits(LowerTranslation, UpperTranslation); break;
                }
                _pendingParameters &= ~policy;
                if (_pendingParameters == 0) Space!.ChangePendingJointPolicies(-1);
            }
        }
        finally { _preparingParameters = false; }
    }
}
