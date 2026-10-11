namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime
{
    private bool _publishingFields;
    internal void PublishFields(bool completingStep = false)
    {
        if (_publishingFields) throw new InvalidOperationException("Resolved body fields cannot be acquired recursively.");
        var space = Space!;
        if (completingStep) space.BeginBackendPublication(); else space.BeginExtensionCallback();
        BeginExtension(); _publishingFields = true;
        try
        {
            var value = Backend.ReadResolvedFields(new(Gravity, LinearDamp, AngularDamp, FieldsInitialized));
            if (!value.Gravity.IsFinite() || !float.IsFinite(value.LinearDamp) || !float.IsFinite(value.AngularDamp))
                throw new InvalidOperationException("The selected implementation returned nonfinite body fields.");
            Gravity = value.Gravity; LinearDamp = value.LinearDamp; AngularDamp = value.AngularDamp; FieldsInitialized = value.Initialized;
        }
        finally { _publishingFields = false; EndExtension(); space.EndExtensionCallback(); }
    }
}
