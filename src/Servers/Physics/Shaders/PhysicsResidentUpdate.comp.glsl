#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentConstraint.inc.glsl"
layout(local_size_x=64) in;
layout(std430,set=0,binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=0,binding=1) readonly buffer Heads { uvec2 heads[]; };
layout(std430,set=0,binding=2) readonly buffer Corrections { vec4 corrections[]; };
layout(std430,set=0,binding=3) readonly buffer Constraints { Constraint constraints[]; };
layout(std430,set=0,binding=4) readonly buffer SolverBodies { SolverBody solverBodies[]; };
layout(std430,set=1,binding=0) buffer Impulses { ContactImpulse impulses[]; };
layout(std430,set=1,binding=1) buffer Status { uvec2 status; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; vec4 time; vec4 policy; uvec4 history; vec4 correctionPolicy; };
const uint none=0xffffffffu;
void fail(){atomicOr(status.x,1u);}
uint activeDegree(uint body,bool dynamicResponse)
{
    if(!dynamicResponse)return 0u;
    uvec2 list=heads[body];
    if((history.w&2u)==0u)return list.y;
    // Joint rows precede contacts; already completed colored rows are outside this Jacobi batch.
    uint at=list.x,count=0u;
    for(uint visited=0u;visited<list.y&&at!=none;visited++)
    {
        uint index=at>>1;if(index<history.z)return count;
        if(index>=control.w){fail();return list.y;}
        Constraint row=constraints[index];bool second=(at&1u)!=0u;
        if((second?row.bodies.y:row.bodies.x)!=body){fail();return list.y;}
        count++;at=second?row.bodies.w:row.bodies.z;
    }
    if(at!=none)fail();return count;
}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=control.y)return;
    if((history.w&2u)!=0u)i+=history.z;
    Constraint c=constraints[i];if(c.bodies.x==none)return;
    ContactImpulse p=impulses[i];
    ResidentBody a=bodies[c.bodies.x],b=c.bodies.y==none?worldBody():bodies[c.bodies.y];
    bool packed=(history.w&8u)!=0u;
    if(packed){a.velocity.xyz=solverBodies[c.bodies.x].velocity.xyz;if(c.bodies.y!=none)b.velocity.xyz=solverBodies[c.bodies.y].velocity.xyz;}
    bool joint=rowMode(c)!=0u;
    // A stationary surface can move contact points, but not the pin/guide's fixed anchor.
    if(joint){if(a.flags.y==0u)a.velocity=vec4(0);if(b.flags.y==0u)b.velocity=vec4(0);}
    else {a.velocity.xyz+=a.surface.xyz;b.velocity.xyz+=b.surface.xyz;}
    vec2 relative=b.velocity.xy-a.velocity.xy,n=c.normal.xy,t=vec2(n.y,-n.x);
    float vn=dot2(relative,n)+b.velocity.z*c.normal.w-a.velocity.z*c.normal.z;
    float vt=dot2(relative,t)+b.velocity.z*c.tangent.w-a.velocity.z*c.tangent.z;
    float softness=joint?c.parameters.y:0;
    float pn=p.physical.x+c.parameters.x*(c.parameters.z-vn-softness*p.physical.x);
    if(!finite4(vec4(vn,vt,pn,0))){fail();return;}
    pn=joint?clamp(pn,p.correction.z,p.correction.w):max(0,pn);
    float pt=0;
    if(!joint)
    {
        vec2 ma=responds(c,false)?inverseMass(a):vec2(0),mb=responds(c,true)?inverseMass(b):vec2(0);
        vec2 candidate=contactImpulse(c,p.physical.xy,vec2(vn,vt),ma,mb);pn=candidate.x;pt=candidate.y;
    }
    uint degree=max(activeDegree(c.bodies.x,responds(c,false)&&inverseMass(a).x>0),activeDegree(c.bodies.y,responds(c,true)&&inverseMass(b).x>0));
    // shortcut: degree-damped Jacobi handles joints and color overflow; improve its convergence for high-degree stacks.
    float weight=1.0/float(max(degree,1u));
    vec2 delta=weight*(vec2(pn,pt)-p.physical.xy);
    vec3 ca=corrections[c.bodies.x].xyz,cb=c.bodies.y==none?vec3(0):corrections[c.bodies.y].xyz;
    if(packed){ca=solverBodies[c.bodies.x].correction.xyz;cb=c.bodies.y==none?vec3(0):solverBodies[c.bodies.y].correction.xyz;}
    float correctionSpeed=dot2(cb.xy-ca.xy,n)+cb.z*c.normal.w-ca.z*c.normal.z;
    float nextCorrection=p.correction.x+c.parameters.x*(c.parameters.w-correctionSpeed-softness*p.correction.x);
    if(rowMode(c)!=2u&&!finite4(vec4(correctionSpeed,nextCorrection,0,0))){fail();return;}
    nextCorrection=joint?clamp(nextCorrection,p.correction.z,p.correction.w):max(0,nextCorrection);
    if(rowMode(c)==2u)nextCorrection=0;
    float correctionDelta=weight*(nextCorrection-p.correction.x);
    p.correction.xy=vec2(p.correction.x+correctionDelta,correctionDelta);
    p.physical=vec4(p.physical.xy+delta,delta);
    if(!finite4(p.physical)||!finite4(p.correction)){fail();return;}
    impulses[i]=p;return;
}
