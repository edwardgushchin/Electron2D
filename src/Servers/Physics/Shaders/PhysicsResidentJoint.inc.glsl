struct ResidentJoint
{
    uvec4 ids; // Generation, type, body A/B; type 3 is inactive, B may be the fixed world.
    uvec4 identity; // Body generations, flags (limit/motor/collision veto), padding.
    vec4 frameA; vec4 frameB; // Local anchor xy and sampled rotation basis zw.
    vec4 limits; // Translation lower/upper, angle lower/upper.
    vec4 motorSpring; // Motor speed, torque cap in N*m, rest length, stiffness.
    vec4 policy; // Axial damping; remaining fields reserved.
    vec4 solverPolicy; // Bias (zero inherits), correction speed cap, force cap, pin anchor softness.
};
struct JointState { uvec4 epochs; vec4 first; vec4 last; vec4 budget; }; // Five row impulses, previous dt; substep cap, spent linear/angular impulse, active solve marker.
