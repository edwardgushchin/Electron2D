#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentJoint.inc.glsl"
#include "PhysicsPairHash.inc.glsl"
layout(local_size_x=64) in;
struct Edit { uvec4 target; ResidentJoint value; };
struct ExceptionEdit { uvec4 target; uvec4 pair; };
layout(std430,set=0,binding=0) readonly buffer Edits { Edit edits[]; };
layout(std430,set=0,binding=1) readonly buffer ExceptionEdits { ExceptionEdit exceptionEdits[]; };
layout(std430,set=1,binding=0) buffer Joints { ResidentJoint joints[]; };
layout(std430,set=1,binding=1) buffer States { JointState states[]; };
layout(std430,set=1,binding=2) buffer Filters { uint filters[]; };
layout(std430,set=1,binding=3) buffer Status { uvec2 status; };
layout(std430,set=1,binding=4) buffer Exceptions { uvec4 exceptions[]; };
layout(std430,set=1,binding=5) buffer FilterPairs { uvec4 filterPairs[]; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 work; uvec4 extra; };
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=work.y)return;
    if(work.x==0u)
    {
        Edit e=edits[i];if(e.target.x>=work.z){atomicOr(status.x,1u);return;}
        joints[e.target.x]=e.value;states[e.target.x]=JointState(uvec4(0),vec4(0),vec4(0),vec4(0));return;
    }
    if(work.x==1u)
    {
        ExceptionEdit e=exceptionEdits[i];if(e.target.x>=extra.x){atomicOr(status.x,1u);return;}
        exceptions[e.target.x]=e.pair;return;
    }
    if(work.x==2u)
    {
        filters[i]=0xffffffffu;
        if(i>=work.z+extra.x)return;
        uvec4 pair=uvec4(0xffffffffu);
        if(i<work.z)
        {
            ResidentJoint j=joints[i];
            if(j.ids.y!=3u&&j.ids.w!=0xffffffffu&&(j.identity.z&4u)!=0u)pair=uvec4(j.ids.zw,j.identity.xy);
        }
        else pair=exceptions[i-work.z];
        filterPairs[i]=pair.x<=pair.y?pair:pair.yxwz;return;
    }
    uvec4 pair=filterPairs[i];if(pair.x==0xffffffffu)return;
    uint at=pairHash(pair.xy)&(work.w-1u);
    for(uint probe=0u;probe<work.w;probe++)
    {
        uint old=atomicCompSwap(filters[at],0xffffffffu,i);if(old==0xffffffffu)return;
        if(old>=work.z+extra.x){atomicOr(status.x,1u);return;}
        if(pair==filterPairs[old])return;
        at=(at+1u)&(work.w-1u);
    }
    atomicOr(status.x,1u);
}
