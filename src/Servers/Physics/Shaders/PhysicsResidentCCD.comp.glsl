#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
layout(local_size_x=64) in;
layout(std430,set=0,binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=0,binding=1) readonly buffer Vertices { vec2 vertices[]; };
layout(std430,set=0,binding=2) readonly buffer Geometries { Geometry geometries[]; };
layout(std430,set=0,binding=3) readonly buffer Shapes { Shape shapes[]; };
layout(std430,set=0,binding=4) readonly buffer Pairs { uvec4 pairs[]; };
layout(std430,set=1,binding=1) buffer Centers { vec2 centers[]; };
layout(std430,set=0,binding=5) readonly buffer Corrections { vec4 corrections[]; };
layout(std430,set=1,binding=0) buffer Summary { uvec2 summary; };
layout(std140,set=2,binding=0) uniform Settings { uvec4 counts; vec4 tolerances; uvec4 history; };
const float epsilon=1.1920928955078125e-7;
#include "PhysicsCollisionMath.inc.glsl"
ResidentBody bodyA,bodyB;
float contactLimit=3.402823466e38;
void fail(){atomicOr(summary.x,1u);}
#include "PhysicsResidentCollision.inc.glsl"
#define ONE_WAY_STATE_BINDING 6
#define ONE_WAY_TABLE_BINDING 7
#include "PhysicsResidentOneWay.inc.glsl"
uvec4 activePair;
vec3 motion(ResidentBody body,uint index)
{
    if(body.flags.y==0u||(body.flags.z&16u)!=0u)return vec3(0);
    return body.velocity.xyz+(counts.w!=0u?corrections[index].xyz:vec3(0));
}
ResidentBody sampleBody(ResidentBody start,vec2 center,vec3 speed,float t)
{
    float angle=t*tolerances.w*speed.z;vec2 q=vec2(cos(angle),sin(angle));
    vec2 worldCenter=start.pose.xy+rotate(start.pose.zw,center)+t*tolerances.w*speed.xy;
    start.pose.zw=rotate(start.pose.zw,q);
    start.pose.xy=worldCenter-rotate(start.pose.zw,center);return start;
}
float extent(Shape shape,Geometry geometry,vec2 center)
{
    float radius=0;
    for(uint i=0u;i<geometry.data.y;i++)
    {
        vec2 v=vertices[geometry.data.x+i];
        if(geometry.data.z==1u||geometry.data.z==2u||geometry.data.z==5u)
        {
            uint first=i&~1u;vec2 a=vertices[geometry.data.x+first],b=vertices[geometry.data.x+first+1u];
            if(dot2(b-a,b-a)<=tolerances.y*tolerances.y)v=0.5*a+0.5*b;
        }
        radius=max(radius,length(shape.pose.xy+rotate(shape.pose.zw,v)-center));
    }
    return radius;
}
Hull asPoint(Hull h,vec2 point)
{
    h.pose.xy=point;h.pose.zw=vec2(0);h.count=1u;h.radius=0;h.midpoint=false;return h;
}
float closingSpeed(Hull a,Hull b,vec3 va,vec3 vb,vec2 ca,vec2 cb,vec2 n,bool rayA,bool rayB)
{
    float plane=project(b,n).x+b.radius,speed=0;
    vec2 centerA=rotate(bodyA.pose.zw,ca),centerB=bodyB.pose.xy-bodyA.pose.xy+rotate(bodyB.pose.zw,cb);
    for(uint i=0u;i<b.count;i++)
    {
        vec2 toward=vertex(b,i);if(abs(dot2(toward,n)-plane)>tolerances.z)continue;
        uint feature;
        vec2 pa=support(a,n,toward,feature),pb=support(b,-n,pa,feature);pa=support(a,n,pb,feature);
        float relative=dot2(va.xy-vb.xy,n)+(rayA?0:va.z*cross2(pa-centerA,n))-(rayB?0:vb.z*cross2(pb-centerB,n));
        speed=max(speed,relative);
    }
    return speed;
}
bool directed(Hull ray,Hull other)
{
    vec2 from=vertex(ray,0u),d=vertex(ray,1u)-from;float t;vec2 n;uint feature;
    if(d==vec2(0))return false;
    return rayHit(other,from,d+normalized(d)*(4*tolerances.x),t,n,feature);
}
float sweep(Shape sa,Geometry ga,Shape sb,Geometry gb,ResidentBody startA,ResidentBody startB,vec3 va,vec3 vb,
    vec2 ca,vec2 cb,float radiusA,float radiusB,uint pieceA,uint pieceB,bool rayA,bool rayB)
{
    bodyA=startA;bodyB=startB;
    Hull a=hull(sa,ga,bodyA,pieceA),b=hull(sb,gb,bodyB,pieceB);
    vec2 localA=vec2(0),localB=vec2(0),direction=normalized(va.xy-vb.xy);
    if((rayA||rayB)&&direction==vec2(0))return 1;
    uint feature;
    if(rayA)localA=support(a,direction,vertex(a,0u),feature)+a.radius*direction;
    if(rayB)localB=support(b,-direction,vertex(b,0u),feature)-b.radius*direction;
    float wa=rayA?0:abs(va.z),wb=rayB?0:abs(vb.z);
    float bound=tolerances.w*(wa*radiusA+wb*radiusB);
    float time=0;
    bool oneWay=((sa.policy.w|sb.policy.w)&4u)!=0u;
    uint decision=oneWay?previousOneWay(activePair,uvec2(pieceA,pieceB),uvec4(sa.revision.x,sb.revision.x,ga.revision,gb.revision)):0u;

    for(uint iteration=0u;iteration<256u;iteration++)
    {
        bodyA=sampleBody(startA,ca,va,time);bodyB=sampleBody(startB,cb,vb,time);
        a=hull(sa,ga,bodyA,pieceA);b=hull(sb,gb,bodyB,pieceB);
        Hull testA=rayA?asPoint(a,localA+time*tolerances.w*va.xy-bodyA.pose.xy):a,testB=rayB?asPoint(b,localB+time*tolerances.w*vb.xy-bodyA.pose.xy):b;
        Axis axis;if(!separatingAxis(testA,testB,axis))return 1;
        float gap=axis.separation;
        if(!finite2(vec2(gap,time))){fail();return 1;}
        float episodeMargin=max(4*tolerances.x,uintBitsToFloat(history.z))+2*tolerances.x;
        if(oneWay&&decision!=0u&&gap>episodeMargin)
        {
            // Publish separation before a later impact, so the next manifold pass retires the old side decision.
            if(time>0)return time;
            decision=0u;
        }
        if(oneWay&&decision==1u)
        {
            // Two translating convex pieces have one overlap interval; rotation can expose another side later.
            if(wa==0&&wb==0)return 1;
            float travelBound=tolerances.w*length(vb.xy-va.xy)+bound;
            float next=time+0.9*max(4*tolerances.x,episodeMargin-gap)/travelBound;
            if(next>1)return 1;
            if(next==time){atomicOr(summary.x,2u);return 1;}
            time=next;continue;
        }
        if(gap<=4*tolerances.x)
        {
            if(ga.data.z==6u&&!directed(a,b))return 1;
            if(gb.data.z==6u&&!directed(b,a))return 1;
            if(oneWay&&decision==0u)
            {
                decision=facesOneWay(sa,sb,bodyA,bodyB,axis.normal)?2u:1u;
                if(decision==1u)continue;
            }
            // Existing contact is handled by the ordinary solver, including overlap recovery.
            if(time==0)
            {
                float curvature=bound*tolerances.w*max(wa,wb);
                // Residual closing is a contact constraint, so it includes prescribed surface motion.
                // Geometric sampling and first-impact advancement above still use actual motion only.
                float speed=closingSpeed(testA,testB,va+startA.surface.xyz,vb+startB.surface.xyz,ca,cb,axis.normal,rayA,rayB)*tolerances.w;
                float allowance=0.5*max(gap,0.25*tolerances.x);
                float discriminant=speed*speed+2*curvature*allowance;
                if(!finite2(vec2(curvature,discriminant))){fail();return 1;}
                // Bound both residual contact closing and curvature before reevaluating the constraints.
                float denominator=speed+sqrt(discriminant);
                return denominator>0?min(1,2*allowance/denominator):1;
            }
            return time;
        }
        float closing=tolerances.w*max(0,-dot2(vb.xy-va.xy,axis.normal))+bound;
        if(closing==0)return 1;
        float advance=0.9*(gap-tolerances.x)/closing;
        float next=time+advance;
        if(next>1)return 1;
        if(next==time)
        {
            if(gap<=4*tolerances.x)return time;
            atomicOr(summary.x,2u);return 1;
        }
        time=next;
    }
    atomicOr(summary.x,4u);return 1;
}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=counts.x)return;
    oneWayInfo=history.xy;
    uvec4 pair=pairs[i];activePair=pair;
    if(pair.x>=counts.y||pair.y>=counts.y){fail();return;}
    Shape sa=shapes[pair.x],sb=shapes[pair.y];
    if(sa.owner.x>=counts.z||sb.owner.x>=counts.z||sa.policy.x!=pair.z||sb.policy.x!=pair.w){fail();return;}
    if(((sa.policy.w|sb.policy.w)&2u)!=0u)return;
    ResidentBody startA=bodies[sa.owner.x],startB=bodies[sb.owner.x];
    if(startA.flags.x!=sa.owner.y||startB.flags.x!=sb.owner.y||startA.flags.w==0u||startB.flags.w==0u){fail();return;}
    uint modeA=continuousMode(startA),modeB=continuousMode(startB);
    if((modeA|modeB)==0u)return;
    Geometry ga=geometries[sa.owner.z],gb=geometries[sb.owner.z];
    if(ga.data.w!=sa.owner.w||gb.data.w!=sb.owner.w){fail();return;}
    if(ga.data.y==0u||gb.data.y==0u||(ga.data.z==5u&&gb.data.z==5u)||(ga.data.z==6u&&gb.data.z==6u))return;
    vec3 va=motion(startA,sa.owner.x),vb=motion(startB,sb.owner.x);
    vec2 ca=centers[sa.owner.x],cb=centers[sb.owner.x];
    float radiusA=extent(sa,ga,ca),radiusB=extent(sb,gb,cb);
    float bound=tolerances.w*(abs(va.z)*radiusA+abs(vb.z)*radiusB);
    if(!finite2(vec2(bound))){fail();return;}if(bound==0&&va.xy==vb.xy)return;
    // Keep the entire sweep near the first body's initial origin, including rotational sampling.
    startB.pose.xy-=startA.pose.xy;startA.pose.xy=vec2(0);
    uint na=ga.data.z==5u?ga.data.y/2u:1u,nb=gb.data.z==5u?gb.data.y/2u:1u;
    float first=1;
    for(uint a=0u;a<na;a++)for(uint b=0u;b<nb;b++)
    {
        if(modeA==2u||modeB==2u)first=min(first,sweep(sa,ga,sb,gb,startA,startB,va,vb,ca,cb,radiusA,radiusB,a,b,false,false));
        else
        {
            if(modeA==1u)first=min(first,sweep(sa,ga,sb,gb,startA,startB,va,vb,ca,cb,radiusA,radiusB,a,b,true,false));
            if(modeB==1u)first=min(first,sweep(sa,ga,sb,gb,startA,startB,va,vb,ca,cb,radiusA,radiusB,a,b,false,true));
        }
    }
    if(first<0||first>1||!finite2(vec2(first))){fail();return;}
    atomicMin(summary.y,floatBitsToUint(first));
}
