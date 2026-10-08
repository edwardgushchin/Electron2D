#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentJoint.inc.glsl"
#include "PhysicsPairHash.inc.glsl"
layout(local_size_x=64) in;
struct Edit { uvec4 target; ResidentJoint value; };
layout(std430,set=0,binding=0) readonly buffer Edits { Edit edits[]; };
layout(std430,set=1,binding=0) buffer Joints { ResidentJoint joints[]; };
layout(std430,set=1,binding=1) buffer States { JointState states[]; };
layout(std430,set=1,binding=2) buffer Filters { uint filters[]; };
layout(std430,set=1,binding=3) buffer Status { uvec2 status; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 work; };
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=work.y)return;
    if(work.x==0u)
    {
        Edit e=edits[i];if(e.target.x>=work.z){atomicOr(status.x,1u);return;}
        joints[e.target.x]=e.value;states[e.target.x]=JointState(uvec4(0),vec4(0),vec4(0));return;
    }
    if(work.x==1u){filters[i]=0xffffffffu;return;}
    ResidentJoint j=joints[i];
    if(j.ids.y==3u||j.ids.w==0xffffffffu||(j.identity.z&4u)==0u)return;
    uvec2 pair=uvec2(min(j.ids.z,j.ids.w),max(j.ids.z,j.ids.w));uint at=pairHash(pair)&(work.w-1u);
    for(uint probe=0u;probe<work.w;probe++)
    {
        uint old=atomicCompSwap(filters[at],0xffffffffu,i);if(old==0xffffffffu)return;
        if(old>=work.z){atomicOr(status.x,1u);return;}
        uvec2 other=joints[old].ids.zw;
        if(pair==uvec2(min(other.x,other.y),max(other.x,other.y)))return;
        at=(at+1u)&(work.w-1u);
    }
    atomicOr(status.x,1u);
}
