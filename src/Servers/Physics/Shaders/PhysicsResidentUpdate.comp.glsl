#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentConstraint.inc.glsl"
layout(local_size_x=64) in;
layout(std430,set=0,binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=0,binding=1) readonly buffer Heads { uvec2 heads[]; };
layout(std430,set=0,binding=2) readonly buffer Corrections { vec4 corrections[]; };
layout(std430,set=0,binding=3) readonly buffer Constraints { Constraint constraints[]; };
layout(std430,set=1,binding=0) buffer Impulses { ContactImpulse impulses[]; };
layout(std430,set=1,binding=1) buffer Status { uvec2 status; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; vec4 time; vec4 policy; uvec4 history; };
const uint none=0xffffffffu;
void fail(){atomicOr(status.x,1u);}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=control.y)return;
    Constraint c=constraints[i];if(c.bodies.x==none)return;
    ContactImpulse p=impulses[i];
    ResidentBody a=bodies[c.bodies.x],b=bodies[c.bodies.y];
    vec2 relative=b.velocity.xy-a.velocity.xy,n=c.normal.xy,t=vec2(n.y,-n.x);
    float vn=dot2(relative,n)+b.velocity.z*c.normal.w-a.velocity.z*c.normal.z;
    float vt=dot2(relative,t)+b.velocity.z*c.tangent.w-a.velocity.z*c.tangent.z;
    float pn=max(0,p.physical.x+c.parameters.x*(c.parameters.z-vn));
    float limit=c.tangent.x*pn;
    float pt=clamp(p.physical.y-c.parameters.y*vt,-limit,limit);
    uint degree=max(inverseMass(a).x>0?heads[c.bodies.x].y:0u,inverseMass(b).x>0?heads[c.bodies.y].y:0u);
    // ponytail: degree-damped Jacobi avoids graph coloring; convergence in tall stacks is the measured ceiling.
    float weight=1.0/float(max(degree,1u));
    vec2 delta=weight*(vec2(pn,pt)-p.physical.xy);
    vec3 ca=corrections[c.bodies.x].xyz,cb=corrections[c.bodies.y].xyz;
    float correctionSpeed=dot2(cb.xy-ca.xy,n)+cb.z*c.normal.w-ca.z*c.normal.z;
    float nextCorrection=max(0,p.correction.x+c.parameters.x*(c.parameters.w-correctionSpeed));
    float correctionDelta=weight*(nextCorrection-p.correction.x);
    p.correction=vec4(p.correction.x+correctionDelta,correctionDelta,0,0);
    p.physical=vec4(p.physical.xy+delta,delta);
    if(!finite4(p.physical)||!finite4(p.correction)){fail();return;}
    impulses[i]=p;return;
}
