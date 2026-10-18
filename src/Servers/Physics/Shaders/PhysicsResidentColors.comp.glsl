#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentConstraint.inc.glsl"
#include "PhysicsResidentContact.inc.glsl"
#include "PhysicsPairHash.inc.glsl"
layout(local_size_x=64) in;
layout(std430,set=0,binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=0,binding=1) readonly buffer Constraints { Constraint constraints[]; };
layout(std430,set=0,binding=2) readonly buffer Points { ContactPoint points[]; };
layout(std430,set=1,binding=0) buffer Colors { uint colors[]; };
layout(std430,set=1,binding=1) buffer Owners { uvec2 owners[]; };
layout(std430,set=1,binding=2) buffer Status { uvec4 status; };
layout(std430,set=1,binding=3) buffer Buckets { uint buckets[]; };
layout(std430,set=1,binding=4) buffer Order { uint order[]; };
struct CachedColor { uvec4 pair; uvec4 features; uvec4 schedule; };
layout(std430,set=1,binding=5) buffer CachedColors { CachedColor cached[]; };
layout(std430,set=1,binding=6) buffer CachedTable { uint cacheTable[]; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; uvec4 iteration; };
const uint none=0xffffffffu;
uint colorHash(ContactPoint p) {return pairHash(p.pair.xy)^pairHash(p.pair.zw)^pairHash(p.features.xy)^pairHash(p.features.zw);}
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
    if(control.x==12u)
    {
        colors[i]=none;
        if(constraints[i].bodies.x==none){colors[i]=none-1u;return;}
        ContactPoint p=points[i];uint at=colorHash(p)&(iteration.w-1u);
        for(uint probe=0u;probe<iteration.w;probe++)
        {
            uint slot=cacheTable[at];if(slot==none||slot>=iteration.z)return;
            CachedColor old=cached[slot];
            if(old.pair==p.pair&&old.features==p.features){colors[i]=old.schedule.x;return;}
            at=(at+1u)&(iteration.w-1u);
        }
        return;
    }
    if(control.x==13u){cacheTable[i]=none;return;}
    if(control.x==14u)
    {
        if(constraints[i].bodies.x==none||colors[i]>=32u)return;
        ContactPoint p=points[i];cached[i]=CachedColor(p.pair,p.features,uvec4(colors[i],0,0,0));
        uint at=colorHash(p)&(iteration.w-1u);
        for(uint probe=0u;probe<iteration.w;probe++)
        {
            if(atomicCompSwap(cacheTable[at],none,i)==none)return;
            at=(at+1u)&(iteration.w-1u);
        }
        return;
    }
    if(control.x==10u)
    {
        if(status.w==0u)return;
        if(i<control.z)owners[i]=uvec2(0u,none);
        if(i<control.w)
        {
            bool valid=constraints[i].bodies.x!=none;colors[i]=valid?none:none-1u;
            if(valid)atomicAdd(status.x,1u);
        }
        return;
    }
    if(control.x==11u)
    {if(status.w!=0u){status.w=0u;status.y=0u;}return;}
    if(control.x>=5u)
    {
        if(status.x!=0u||(status.z&1u)!=0u)return;
        if(control.x==5u){owners[i].x=0u;return;}
        if(control.x==8u)
        {
            uint offset=0u;for(uint c=0u;c<32u;c++){buckets[32u+c]=offset;offset+=buckets[c];}return;
        }
        Constraint c=constraints[i];if(c.bodies.x==none)return;
        uint color=colors[i];if(color>=32u||rowMode(c)!=0u){atomicOr(status.w,1u);return;}
        if(control.x==9u){order[buckets[32u+color]+atomicAdd(buckets[64u+color],1u)]=i;return;}
        if(iteration.y==0u)atomicAdd(buckets[color],1u);
        else atomicMax(status.y,color+1u);
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
