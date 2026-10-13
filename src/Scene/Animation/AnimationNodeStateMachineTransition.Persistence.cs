namespace Electron2D;

public sealed partial class AnimationNodeStateMachineTransition
{
    private static readonly PropertyDescriptor[] FileBindingProperties =
    [
        new PropertyDescriptor<AnimationNodeStateMachineTransition, string>("ConditionID", n => n.AdvanceCondition is { } key ? ResourceFileTypes.AnimationParameterBinding(key).ID : "", (n, v) => n.SetFileCondition(v), stored: true),
        new PropertyDescriptor<AnimationNodeStateMachineTransition, string>("PredicateID", n => ResourceFileTypes.AnimationPredicateID(n.AdvanceExpression), (n, v) => n.AdvanceExpression = ResourceFileTypes.AnimationPredicate(v), stored: true),
    ];
    private void SetFileCondition(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (id.Length == 0) { AdvanceCondition = null; return; }
        var key = ResourceFileTypes.AnimationParameterBinding(id).Parameter as AnimationParameter<bool>
            ?? throw new InvalidDataException("Transition condition requires a boolean key.");
        AdvanceCondition = key;
    }
}
