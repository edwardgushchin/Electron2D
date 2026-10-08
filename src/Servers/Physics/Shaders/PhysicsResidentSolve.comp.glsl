#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
#include "PhysicsResidentContact.inc.glsl"
#include "PhysicsMaterial.inc.glsl"
#include "PhysicsPairHash.inc.glsl"
layout(local_size_x=64) in;
struct History { uvec4 pair; uvec4 features; uvec4 epochs; uvec4 geometry; vec4 impulse; };
struct Constraint { uvec4 bodies; vec4 mass; vec4 anchors; vec4 normal; vec4 parameters; vec4 impulse; vec4 correction; };
layout(std430,set=0,binding=0) readonly buffer Points { ContactPoint points[]; };
layout(std430,set=0,binding=1) readonly buffer Shapes { Shape shapes[]; };
layout(std430,set=0,binding=2) readonly buffer Geometries { Geometry geometries[]; };
layout(std430,set=1,binding=0) buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=1,binding=1) buffer Constraints { Constraint constraints[]; };
layout(std430,set=1,binding=2) buffer Heads { uvec2 heads[]; };
layout(std430,set=1,binding=3) buffer Status { uvec2 status; };
layout(std430,set=1,binding=4) buffer HistoryRecords { History records[]; };
layout(std430,set=1,binding=5) buffer HistoryTable { uint table[]; };
layout(std430,set=1,binding=6) buffer Corrections { vec4 corrections[]; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 control; vec4 time; vec4 policy; uvec4 history; };
const uint none=0xffffffffu;
float dot2(vec2 a,vec2 b) {return a.x*b.x+a.y*b.y;}
float cross2(vec2 a,vec2 b) {return a.x*b.y-a.y*b.x;}
vec2 rotate(vec2 q,vec2 p) {return vec2(q.x*p.x-q.y*p.y,q.y*p.x+q.x*p.y);}
vec2 angular(float w,vec2 p) {return vec2(-w*p.y,w*p.x);}
bool finite4(vec4 v) {return !any(isnan(v))&&!any(isinf(v));}
void fail(){atomicOr(status.x,1u);}
vec2 inverseMass(ResidentBody b) {return b.flags.y>=2u?vec2(b.properties.x,(b.flags.z&4u)==0u?b.properties.y:0):vec2(0);}
vec2 velocity(ResidentBody b,vec2 r) {return b.velocity.xy+angular(b.velocity.z,r);}
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
        if(c.bodies.x!=none&&c.impulse.x>0)
        {
            Shape a=shapes[p.pair.x],b=shapes[p.pair.y];
            h=History(p.pair,p.features,epochs(a,b),geometryEpochs(a,b),vec4(c.impulse.xy,p.normal.xy));
        }
        records[i]=h;return;
    }
    if(control.x==1u)
    {
        Constraint c=Constraint(uvec4(none),vec4(0),vec4(0),vec4(0),vec4(0),vec4(0),vec4(0));
        ContactPoint p=points[i];
        if(p.normal.w!=0){constraints[i]=c;return;}
        Shape sa=shapes[p.pair.x],sb=shapes[p.pair.y];
        uint ai=sa.owner.x,bi=sb.owner.x;
        if(sa.policy.x!=p.pair.z||sb.policy.x!=p.pair.w||ai>=control.z||bi>=control.z){fail();return;}
        ResidentBody a=bodies[ai],b=bodies[bi];
        if(a.flags.x!=sa.owner.y||b.flags.x!=sb.owner.y||a.flags.w==0u||b.flags.w==0u){fail();return;}
        vec2 ma=inverseMass(a),mb=inverseMass(b);
        if(ma.x+mb.x==0){constraints[i]=c;return;}
        vec2 ra=rotate(a.pose.zw,p.anchors.xy),rb=rotate(b.pose.zw,p.anchors.zw),n=p.normal.xy,t=vec2(n.y,-n.x);
        float an=cross2(ra,n),bn=cross2(rb,n),at=cross2(ra,t),bt=cross2(rb,t);
        float kn=ma.x+mb.x+ma.y*an*an+mb.y*bn*bn,kt=ma.x+mb.x+ma.y*at*at+mb.y*bt*bt;
        vec2 material=mixSurfaceMaterial(abs(sa.material.xy),abs(sb.material.xy),flags(sa.material.xy),flags(sb.material.xy),80u);
        float vn=dot2(velocity(b,rb)-velocity(a,ra),n);
        float target=p.normal.z>0?-p.normal.z*time.y:0;
        float correction=min(time.w,time.z*max(-p.normal.z-policy.x,0)*time.y);
        if(p.normal.z<=0&&vn< -policy.y)target=max(target,-material.y*vn);
        c.mass=vec4(ma.x,mb.x,ma.y,mb.y);c.anchors=vec4(ra,rb);c.normal=vec4(n,material);
        c.parameters=vec4(kn>0?1/kn:0,kt>0?1/kt:0,target,correction);
        vec2 warm=previousImpulse(p,sa,sb,material.x);c.impulse=vec4(warm,warm);
        if(!finite4(c.mass)||!finite4(c.anchors)||!finite4(c.normal)||!finite4(c.parameters)||!finite4(c.impulse)||!finite4(vec4(kn,kt,0,0))){fail();return;}
        c.bodies=uvec4(ai,bi,none,none);
        if(ma.x>0){c.bodies.z=atomicExchange(heads[ai].x,2u*i);atomicAdd(heads[ai].y,1u);}
        if(mb.x>0){c.bodies.w=atomicExchange(heads[bi].x,2u*i+1u);atomicAdd(heads[bi].y,1u);}
        constraints[i]=c;return;
    }
    if(control.x==2u)
    {
        Constraint c=constraints[i];if(c.bodies.x==none)return;
        ResidentBody a=bodies[c.bodies.x],b=bodies[c.bodies.y];
        vec2 relative=velocity(b,c.anchors.zw)-velocity(a,c.anchors.xy),n=c.normal.xy,t=vec2(n.y,-n.x);
        float pn=max(0,c.impulse.x+c.parameters.x*(c.parameters.z-dot2(relative,n)));
        float limit=c.normal.z*pn;
        float pt=clamp(c.impulse.y-c.parameters.y*dot2(relative,t),-limit,limit);
        uint degree=max(c.mass.x>0?heads[c.bodies.x].y:0u,c.mass.y>0?heads[c.bodies.y].y:0u);
        // ponytail: degree-damped Jacobi avoids graph coloring; convergence in tall stacks is the measured ceiling.
        float weight=1.0/float(max(degree,1u));
        vec2 delta=weight*(vec2(pn,pt)-c.impulse.xy);
        vec3 ca=corrections[c.bodies.x].xyz,cb=corrections[c.bodies.y].xyz;
        vec2 relativeCorrection=(cb.xy+angular(cb.z,c.anchors.zw))-(ca.xy+angular(ca.z,c.anchors.xy));
        float nextCorrection=max(0,c.correction.x+c.parameters.x*(c.parameters.w-dot2(relativeCorrection,n)));
        float correctionDelta=weight*(nextCorrection-c.correction.x);
        c.correction=vec4(c.correction.x+correctionDelta,correctionDelta,0,0);
        c.impulse=vec4(c.impulse.xy+delta,delta);
        if(!finite4(c.impulse)||!finite4(c.correction)){fail();return;}
        constraints[i].impulse=c.impulse;constraints[i].correction=c.correction;return;
    }
    ResidentBody b=bodies[i];if(b.flags.w==0u||b.flags.y<2u)return;
    uvec2 list=heads[i];uint at=list.x;vec3 total=vec3(0),correction=vec3(0);
    for(uint visited=0u;visited<list.y;visited++)
    {
        if(at==none||(at>>1)>=control.w){fail();return;}
        Constraint c=constraints[at>>1];bool second=(at&1u)!=0u;
        if((second?c.bodies.y:c.bodies.x)!=i){fail();return;}
        vec2 n=c.normal.xy,t=vec2(n.y,-n.x),impulse=n*c.impulse.z+t*c.impulse.w;
        vec2 r=second?c.anchors.zw:c.anchors.xy;float sign=second?1:-1;
        total+=sign*vec3(impulse,cross2(r,impulse));
        vec2 positionImpulse=n*c.correction.y;correction+=sign*vec3(positionImpulse,cross2(r,positionImpulse));at=second?c.bodies.w:c.bodies.z;
    }
    if(at!=none){fail();return;}
    vec2 m=inverseMass(b);b.velocity.xyz+=vec3(m.x*total.xy,m.y*total.z);
    vec4 position=corrections[i]+vec4(m.x*correction.xy,m.y*correction.z,0);
    if(!finite4(b.velocity)||!finite4(position)){fail();return;}
    bodies[i].velocity=b.velocity;corrections[i]=position;
}
