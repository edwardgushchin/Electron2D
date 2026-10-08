#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentConstraint.inc.glsl"
layout(local_size_x=64) in;
layout(std430,set=0,binding=0) readonly buffer Constraints { Constraint constraints[]; };
layout(std430,set=0,binding=1) readonly buffer Heads { uvec2 heads[]; };
layout(std430,set=0,binding=2) readonly buffer Impulses { ContactImpulse impulses[]; };
layout(std430,set=1,binding=0) buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=1,binding=1) buffer Corrections { vec4 corrections[]; };
layout(std430,set=1,binding=2) buffer Status { uvec2 status; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; vec4 time; vec4 policy; uvec4 history; };
const uint none=0xffffffffu;
void fail(){atomicOr(status.x,1u);}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=control.y||status.x!=0u)return;
    ResidentBody b=bodies[i];if(b.flags.w==0u||b.flags.y<2u||(b.flags.z&16u)!=0u)return;
    uvec2 list=heads[i];uint at=list.x;vec3 total=vec3(0),correction=vec3(0);
    for(uint visited=0u;visited<list.y;visited++)
    {
        if(at==none||(at>>1)>=control.w){fail();return;}
        Constraint c=constraints[at>>1];ContactImpulse p=impulses[at>>1];bool second=(at&1u)!=0u;
        if((second?c.bodies.y:c.bodies.x)!=i){fail();return;}
        vec2 n=c.normal.xy,t=vec2(n.y,-n.x),impulse=n*p.physical.z+t*p.physical.w;
        float an=second?c.normal.w:c.normal.z,armT=second?c.tangent.w:c.tangent.z,sign=second?1:-1;
        total+=sign*vec3(impulse,an*p.physical.z+armT*p.physical.w);
        vec2 positionImpulse=n*p.correction.y;correction+=sign*vec3(positionImpulse,an*p.correction.y);at=second?c.bodies.w:c.bodies.z;
    }
    if(at!=none){fail();return;}
    vec2 m=inverseMass(b);b.velocity.xyz+=vec3(m.x*total.xy,m.y*total.z);
    vec4 position=corrections[i]+vec4(m.x*correction.xy,m.y*correction.z,0);
    if(!finite4(b.velocity)||!finite4(position)){fail();return;}
    bodies[i].velocity=b.velocity;corrections[i]=position;
}
