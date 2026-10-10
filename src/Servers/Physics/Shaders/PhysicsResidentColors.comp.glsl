#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentConstraint.inc.glsl"
layout(local_size_x=64) in;
layout(std430,set=0,binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=0,binding=1) readonly buffer Constraints { Constraint constraints[]; };
layout(std430,set=1,binding=0) buffer Colors { uint colors[]; };
layout(std430,set=1,binding=1) buffer Owners { uvec2 owners[]; };
layout(std430,set=1,binding=2) buffer Status { uvec4 status; };
layout(std430,set=1,binding=3) buffer Buckets { uint buckets[]; };
layout(std430,set=1,binding=4) buffer Order { uint order[]; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; uvec4 iteration; };
const uint none=0xffffffffu;
uint priority(uint i)
{
    i+=iteration.x*0x9e3779b9u;i^=i>>16;i*=0x7feb352du;i^=i>>15;i*=0x846ca68bu;return i^(i>>16);
}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=control.y)return;
    if(control.x==7u){buckets[i]=0u;buckets[32u+i]=0u;buckets[64u+i]=0u;return;}
    if(control.x==0u){owners[i]=uvec2(0u,none);return;}
    if(control.x==1u)
    {
        bool valid=constraints[i].bodies.x!=none;colors[i]=valid?none:none-1u;
        if(valid)atomicAdd(status.x,1u);return;
    }
    if(control.x>=5u)
    {
        if(status.x!=0u||(status.z&1u)!=0u)return;
        if(control.x==5u){owners[i].x=0u;return;}
        if(control.x==8u)
        {
            uint offset=0u;for(uint c=0u;c<32u;c++){buckets[32u+c]=offset;offset+=buckets[c];}return;
        }
        Constraint c=constraints[i];if(c.bodies.x==none)return;
        uint color=colors[i];if(color>=32u||c.tangent.y!=0){atomicOr(status.w,1u);return;}
        if(control.x==9u){order[buckets[32u+color]+atomicAdd(buckets[64u+color],1u)]=i;return;}
        atomicAdd(buckets[color],1u);
        uint bit=1u<<color;
        if(inverseMass(bodies[c.bodies.x]).x>0&&(atomicOr(owners[c.bodies.x].x,bit)&bit)!=0u)atomicOr(status.w,1u);
        if(c.bodies.y!=none&&inverseMass(bodies[c.bodies.y]).x>0&&(atomicOr(owners[c.bodies.y].x,bit)&bit)!=0u)atomicOr(status.w,1u);
        return;
    }
    if(status.x==0u||(status.z&1u)!=0u)return;
    if(control.x==2u){owners[i].y=none;return;}
    if(colors[i]!=none)return;
    Constraint c=constraints[i];uint a=c.bodies.x,b=c.bodies.y;
    bool da=inverseMass(bodies[a]).x>0,db=b!=none&&inverseMass(bodies[b]).x>0;
    uint key=priority(i);
    if(control.x==3u)
    {
        if(da)atomicMin(owners[a].y,key);if(db)atomicMin(owners[b].y,key);return;
    }
    if((da&&owners[a].y!=key)||(db&&owners[b].y!=key))return;
    uint used=(da?owners[a].x:0u)|(db?owners[b].x:0u);
    if(used==none){atomicOr(status.z,1u);return;}
    uint color=uint(findLSB(~used)),bit=1u<<color;
    if(da)atomicOr(owners[a].x,bit);if(db)atomicOr(owners[b].x,bit);
    colors[i]=color;
    if(atomicAdd(status.x,none)==1u)atomicOr(status.z,(iteration.x+1u)<<8);
    atomicMax(status.y,color+1u);
}
