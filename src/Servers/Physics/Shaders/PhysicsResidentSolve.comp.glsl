#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
#include "PhysicsResidentContact.inc.glsl"
#include "PhysicsMaterial.inc.glsl"
#include "PhysicsPairHash.inc.glsl"
#include "PhysicsContactPersistence.inc.glsl"
layout(local_size_x=64) in;
struct History { uvec4 pair; uvec4 features; uvec4 epochs; uvec4 geometry; vec4 impulse; vec4 anchors; };
#include "PhysicsResidentConstraint.inc.glsl"
layout(std430,set=0,binding=0) readonly buffer Points { ContactPoint points[]; };
layout(std430,set=0,binding=1) readonly buffer Shapes { Shape shapes[]; };
layout(std430,set=0,binding=2) readonly buffer Geometries { Geometry geometries[]; };
layout(std430,set=0,binding=3) readonly buffer Centers { vec2 centers[]; };
layout(std430,set=1,binding=0) buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=1,binding=1) buffer Constraints { Constraint constraints[]; };
layout(std430,set=1,binding=2) buffer Heads { uvec2 heads[]; };
layout(std430,set=1,binding=3) buffer Status { uvec2 status; };
layout(std430,set=1,binding=4) buffer HistoryRecords { History records[]; };
layout(std430,set=1,binding=5) buffer HistoryTable { uint table[]; };
layout(std430,set=1,binding=6) buffer Corrections { vec4 corrections[]; };
layout(std430,set=1,binding=7) buffer Impulses { ContactImpulse impulses[]; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; vec4 time; vec4 policy; uvec4 history; vec4 correctionPolicy; };
const uint none=0xffffffffu;
void fail(){atomicOr(status.x,1u);}
uint flags(vec2 material) {return (material.x<0?1u:0u)|(material.y<0?2u:0u);}
uint historyHash(ContactPoint p)
{
    return pairHash(p.pair.xy)^pairHash(p.pair.zw)^pairHash(p.features.zw);
}
uvec4 epochs(Shape a,Shape b)
{
    return uvec4(bodies[a.owner.x].flags.w,bodies[b.owner.x].flags.w,a.revision.x,b.revision.x);
}
uvec4 geometryEpochs(Shape a,Shape b)
{
    return uvec4(geometries[a.owner.z].revision,geometries[b.owner.z].revision,0,0);
}
vec2 previousImpulse(ContactPoint p,Shape a,Shape b,float friction)
{
    if(history.y==0u||correctionPolicy.z==0)return vec2(0);
    // Each failed claim consumes one candidate. History is immutable except for its claim word.
    for(uint attempt=0u;attempt<history.y;attempt++)
    {
        uint at=historyHash(p)&(history.x-1u),best=none;
        float distance=uintBitsToFloat(0x7f800000u);bool sameFeature=false,ended=false;
        for(uint probe=0u;probe<history.x;probe++)
        {
            uint slot=table[at];if(slot==none){ended=true;break;}
            if(slot>=history.y){fail();return vec2(0);}
            History old=records[slot];
            if(old.pair==p.pair&&old.features.zw==p.features.zw&&old.epochs==epochs(a,b)&&
                old.geometry.xy==geometryEpochs(a,b).xy&&dot2(old.impulse.zw,p.normal.xy)>=0.99&&
                atomicAdd(records[slot].geometry.z,0u)==0u)
            {
                float candidate=contactHistoryDistance(old.anchors,p.anchors,old.impulse.zw,
                    bodies[a.owner.x].pose,bodies[b.owner.x].pose,correctionPolicy.zw);
                bool feature=old.features.xy==p.features.xy;
                if(!isinf(candidate)&&(best==none||(feature&&!sameFeature)||(feature==sameFeature&&candidate<distance)))
                {best=slot;distance=candidate;sameFeature=feature;}
            }
            at=(at+1u)&(history.x-1u);
        }
        if(!ended){fail();return vec2(0);}
        if(best==none)return vec2(0);
        if(atomicCompSwap(records[best].geometry.z,0u,1u)!=0u)continue;
        vec2 impulse=policy.z*records[best].impulse.xy;
        impulse.y=clamp(impulse.y,-friction*impulse.x,friction*impulse.x);
        atomicAdd(status.y,1u);return impulse;
    }
    return vec2(0);
}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=control.y)return;
    if(control.x==0u){heads[i]=uvec2(none,0u);corrections[i]=vec4(0);return;}
    if(control.x==5u){table[i]=none;return;}
    if(control.x==6u)
    {
        History h=records[i];if(h.pair.x==none)return;
        records[i].geometry.z=0u;
        ContactPoint p=ContactPoint(h.pair,h.features,vec4(0),vec4(0));uint at=historyHash(p)&(history.x-1u);
        for(uint probe=0u;probe<history.x;probe++)
        {
            uint old=atomicCompSwap(table[at],none,i);if(old==none)return;
            at=(at+1u)&(history.x-1u);
        }
        fail();return;
    }
    if(control.x==4u)
    {
        Constraint c=constraints[i];ContactPoint p=points[i];
        History h=History(uvec4(none),uvec4(0),uvec4(0),uvec4(0),vec4(0),vec4(0));
        if(c.bodies.x!=none&&impulses[i].physical.x>0)
        {
            Shape a=shapes[p.pair.x],b=shapes[p.pair.y];
            h=History(p.pair,p.features,epochs(a,b),geometryEpochs(a,b),vec4(impulses[i].physical.xy,p.normal.xy),p.anchors);
        }
        records[i]=h;return;
    }
    if(control.x==1u)
    {
        Constraint c=Constraint(uvec4(none),vec4(0),vec4(0),vec4(0));
        impulses[i]=ContactImpulse(vec4(0),vec4(0));
        ContactPoint p=points[i];
        if(p.normal.w!=0){constraints[i]=c;return;}
        Shape sa=shapes[p.pair.x],sb=shapes[p.pair.y];
        uint ai=sa.owner.x,bi=sb.owner.x;
        if(sa.policy.x!=p.pair.z||sb.policy.x!=p.pair.w||ai>=control.z||bi>=control.z){fail();return;}
        ResidentBody a=bodies[ai],b=bodies[bi];
        if(a.flags.x!=sa.owner.y||b.flags.x!=sb.owner.y||a.flags.w==0u||b.flags.w==0u){fail();return;}
        bool continuous=(continuousMode(a)|continuousMode(b))!=0u;
        float contactThreshold=continuous?policy.w:0;
        if(p.normal.z>contactThreshold&&continuous){constraints[i]=c;return;}
        bool respondsA=(sa.policy.z&sb.policy.y)!=0u,respondsB=(sb.policy.z&sa.policy.y)!=0u;
        vec2 ma=respondsA?inverseMass(a):vec2(0),mb=respondsB?inverseMass(b):vec2(0);
        if(ma.x+mb.x==0){constraints[i]=c;return;}
        vec2 ra=rotate(a.pose.zw,p.anchors.xy-centers[ai]),rb=rotate(b.pose.zw,p.anchors.zw-centers[bi]),n=p.normal.xy,t=vec2(n.y,-n.x);
        float an=cross2(ra,n),bn=cross2(rb,n),at=cross2(ra,t),bt=cross2(rb,t);
        float kn=ma.x+mb.x+ma.y*an*an+mb.y*bn*bn,kt=ma.x+mb.x+ma.y*at*at+mb.y*bt*bt;
        vec2 material=mixSurfaceMaterial(abs(sa.material.xy),abs(sb.material.xy),flags(sa.material.xy),flags(sb.material.xy),80u);
        float vn=dot2(velocity(b,rb)-velocity(a,ra),n);
        float target=p.normal.z>0?-p.normal.z*time.y:0;
        float firstBias=uintBitsToFloat(sa.revision.y),secondBias=uintBitsToFloat(sb.revision.y);
        float bias=firstBias==0?(secondBias==0?time.z:secondBias):secondBias==0?firstBias:0.5*(firstBias+secondBias);
        float fraction=1-pow(1-bias,correctionPolicy.x);
        float correction=min(time.w,fraction*max(-p.normal.z-policy.x,0)*time.y);
        // Preserve impact velocity before speculative separation impulses remove it.
        bool reachesContact=p.normal.z<=contactThreshold||(!continuous&&p.normal.z+vn*time.x<=0);
        if(reachesContact&&vn< -policy.y)target=max(target,-material.y*vn);
        c.normal=vec4(n,an,bn);c.tangent=vec4(material.x,float((respondsA?0u:4u)|(respondsB?0u:8u)),at,bt);
        c.parameters=vec4(kn>0?1/kn:0,kt>0?1/kt:0,target,correction);
        vec2 warm=previousImpulse(p,sa,sb,material.x);vec4 impulse=vec4(warm,warm);
        if(!finite4(c.normal)||!finite4(c.tangent)||!finite4(c.parameters)||!finite4(impulse)||!finite4(vec4(kn,kt,0,0))){fail();return;}
        c.bodies=uvec4(ai,bi,none,none);
        if(ma.x>0){c.bodies.z=atomicExchange(heads[ai].x,2u*i);atomicAdd(heads[ai].y,1u);}
        if(mb.x>0){c.bodies.w=atomicExchange(heads[bi].x,2u*i+1u);atomicAdd(heads[bi].y,1u);}
        constraints[i]=c;impulses[i].physical=impulse;return;
    }
}
