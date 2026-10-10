#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentConstraint.inc.glsl"
layout(local_size_x=64) in;
layout(std430,set=0,binding=0) readonly buffer Constraints { Constraint constraints[]; };
layout(std430,set=0,binding=1) readonly buffer Order { uint order[]; };
layout(std430,set=1,binding=0) buffer Impulses { ContactImpulse impulses[]; };
layout(std430,set=1,binding=1) buffer PackedConstraints { Constraint packedConstraints[]; };
layout(std430,set=1,binding=2) buffer PackedImpulses { ContactImpulse packedImpulses[]; };
layout(std430,set=1,binding=3) buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=1,binding=4) buffer Corrections { vec4 corrections[]; };
layout(std430,set=1,binding=5) buffer SolverBodies { SolverBody solverBodies[]; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; };
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=control.y)return;
    if(control.x==2u)
    {
        ResidentBody b=bodies[i];vec2 m=inverseMass(b);
        solverBodies[i]=SolverBody(vec4(b.velocity.xyz,m.x),vec4(corrections[i].xyz,m.y),b.surface);return;
    }
    if(control.x==3u)
    {bodies[i].velocity.xyz=solverBodies[i].velocity.xyz;corrections[i].xyz=solverBodies[i].correction.xyz;return;}
    uint source=order[i];
    if(control.x==0u)
    {
        packedConstraints[i]=constraints[source];
        ContactImpulse p=impulses[source];
        // The warm-start gather has already applied these deltas; joint gathers must not repeat them.
        p.physical.zw=vec2(0);p.correction.y=0;
        packedImpulses[i]=p;impulses[source]=p;
    }
    else impulses[source]=packedImpulses[i];
}
