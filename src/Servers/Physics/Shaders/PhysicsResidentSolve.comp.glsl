#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
#include "PhysicsResidentContact.inc.glsl"
#include "PhysicsMaterial.inc.glsl"
#include "PhysicsPairHash.inc.glsl"
layout(local_size_x=64) in;
struct History { uvec4 pair; uvec4 features; uvec4 epochs; uvec4 geometry; vec4 impulse; };
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
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; vec4 time; vec4 policy; uvec4 history; };
const uint none=0xffffffffu;
void fail(){atomicOr(status.x,1u);}
uint flags(vec2 material) {return (material.x<0?1u:0u)|(material.y<0?2u:0u);}
uint historyHash(ContactPoint p)
{
    return pairHash(p.pair.xy)^pairHash(p.pair.zw)^pairHash(p.features.xy)^pairHash(p.features.zw);
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
    if(history.y==0u)return vec2(0);
    uint at=historyHash(p)&(history.x-1u);
    for(uint probe=0u;probe<history.x;probe++)
    {
        uint slot=table[at];if(slot==none)return vec2(0);
        if(slot>=history.y){fail();return vec2(0);}
        History old=records[slot];
        if(old.pair==p.pair&&old.features==p.features)
        {
            if(old.epochs!=epochs(a,b)||old.geometry!=geometryEpochs(a,b)||dot2(old.impulse.zw,p.normal.xy)<0.99)return vec2(0);
            vec2 impulse=policy.z*old.impulse.xy;
            impulse.y=clamp(impulse.y,-friction*impulse.x,friction*impulse.x);
            atomicAdd(status.y,1u);return impulse;
        }
        at=(at+1u)&(history.x-1u);
    }
    fail();return vec2(0);
}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=control.y)return;
    if(control.x==0u){heads[i]=uvec2(none,0u);corrections[i]=vec4(0);return;}
    if(control.x==5u){table[i]=none;return;}
    if(control.x==6u)
    {
        History h=records[i];if(h.pair.x==none)return;
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
        History h=History(uvec4(none),uvec4(0),uvec4(0),uvec4(0),vec4(0));
        if(c.bodies.x!=none&&impulses[i].physical.x>0)
        {
            Shape a=shapes[p.pair.x],b=shapes[p.pair.y];
            h=History(p.pair,p.features,epochs(a,b),geometryEpochs(a,b),vec4(impulses[i].physical.xy,p.normal.xy));
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
        vec2 ma=inverseMass(a),mb=inverseMass(b);
        if(ma.x+mb.x==0){constraints[i]=c;return;}
        vec2 ra=rotate(a.pose.zw,p.anchors.xy-centers[ai]),rb=rotate(b.pose.zw,p.anchors.zw-centers[bi]),n=p.normal.xy,t=vec2(n.y,-n.x);
        float an=cross2(ra,n),bn=cross2(rb,n),at=cross2(ra,t),bt=cross2(rb,t);
        float kn=ma.x+mb.x+ma.y*an*an+mb.y*bn*bn,kt=ma.x+mb.x+ma.y*at*at+mb.y*bt*bt;
        vec2 material=mixSurfaceMaterial(abs(sa.material.xy),abs(sb.material.xy),flags(sa.material.xy),flags(sb.material.xy),80u);
        float vn=dot2(velocity(b,rb)-velocity(a,ra),n);
        float target=p.normal.z>0?-p.normal.z*time.y:0;
        float correction=min(time.w,time.z*max(-p.normal.z-policy.x,0)*time.y);
        if(p.normal.z<=0&&vn< -policy.y)target=max(target,-material.y*vn);
        c.normal=vec4(n,an,bn);c.tangent=vec4(material.x,0,at,bt);
        c.parameters=vec4(kn>0?1/kn:0,kt>0?1/kt:0,target,correction);
        vec2 warm=previousImpulse(p,sa,sb,material.x);vec4 impulse=vec4(warm,warm);
        if(!finite4(c.normal)||!finite4(c.tangent)||!finite4(c.parameters)||!finite4(impulse)||!finite4(vec4(kn,kt,0,0))){fail();return;}
        c.bodies=uvec4(ai,bi,none,none);
        if(ma.x>0){c.bodies.z=atomicExchange(heads[ai].x,2u*i);atomicAdd(heads[ai].y,1u);}
        if(mb.x>0){c.bodies.w=atomicExchange(heads[bi].x,2u*i+1u);atomicAdd(heads[bi].y,1u);}
        constraints[i]=c;impulses[i].physical=impulse;return;
    }
}
