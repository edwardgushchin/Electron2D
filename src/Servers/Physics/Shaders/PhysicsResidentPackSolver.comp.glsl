#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentConstraint.inc.glsl"
layout(local_size_x=64) in;
layout(std430,set=0,binding=0) readonly buffer Constraints { Constraint constraints[]; };
layout(std430,set=0,binding=1) readonly buffer Order { uint order[]; };
layout(std430,set=0,binding=2) readonly buffer Heads { uvec2 heads[]; };
layout(std430,set=1,binding=0) buffer Impulses { ContactImpulse impulses[]; };
layout(std430,set=1,binding=1) buffer PackedConstraints { Constraint packedConstraints[]; };
layout(std430,set=1,binding=2) buffer PackedImpulses { ContactImpulse packedImpulses[]; };
layout(std430,set=1,binding=3) buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=1,binding=4) buffer Corrections { vec4 corrections[]; };
layout(std430,set=1,binding=5) buffer SolverBodies { SolverBody solverBodies[]; };
layout(std430,set=1,binding=6) buffer Status { uvec2 status; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; };
const uint none=0xffffffffu;
void fail(){atomicOr(status.x,1u);}
#include "PhysicsResidentDegree.inc.glsl"
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=control.y)return;
    if(control.x==2u)
    {
        ResidentBody b=bodies[i];vec2 m=inverseMass(b);
        vec4 surface=b.surface;
        surface.w=uintBitsToFloat(m.x>0&&control.w>control.z?jointDegree(i,control.z,control.w):0u);
        solverBodies[i]=SolverBody(vec4(b.velocity.xyz,m.x),vec4(corrections[i].xyz,m.y),surface);return;
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
