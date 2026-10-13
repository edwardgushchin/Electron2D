namespace Electron2D;

public sealed partial class AnimationNodeTransition
{
    private IEnumerable<PropertyDescriptor> InputFileProperties()
    {
        yield return new PropertyDescriptor<AnimationNodeTransition, int[]>("InputPolicies", n => n._inputData.Select(p => (p.AutoAdvance ? 1 : 0) | (p.BreakLoop ? 2 : 0) | (p.Reset ? 4 : 0)).ToArray(), (n, v) => n.SetFileInputPolicies(v), stored: true);
    }
    private void SetFileInputPolicies(int[] policies)
    {
        ArgumentNullException.ThrowIfNull(policies);
        if (policies.Length != GetInputCount() || policies.Any(p => p is < 0 or > 7)) throw new InvalidDataException("Invalid input policy table.");
        for (var i = 0; i < policies.Length; i++)
        { _inputData[i].AutoAdvance = (policies[i] & 1) != 0; _inputData[i].BreakLoop = (policies[i] & 2) != 0; _inputData[i].Reset = (policies[i] & 4) != 0; }
        EmitGraphChanged();
    }
}
