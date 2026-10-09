#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
layout(local_size_x=64) in;
struct Node { vec4 bounds; int typeMask; int proxy; int hasCategory; int shape; };
struct Mapping { uvec4 identity; uvec4 canvas; };
struct Query { vec4 ray; uvec4 policy; uvec4 extra; };
struct Hit { uvec4 identity; uvec4 owner; vec4 pointNormal; vec4 fraction; };
layout(std430,set=0,binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=0,binding=1) readonly buffer Vertices { vec2 vertices[]; };
layout(std430,set=0,binding=2) readonly buffer Geometries { Geometry geometries[]; };
layout(std430,set=0,binding=3) readonly buffer Shapes { Shape shapes[]; };
layout(std430,set=0,binding=4) readonly buffer Nodes { Node nodes[]; };
layout(std430,set=0,binding=5) readonly buffer Mappings { Mapping mappings[]; };
layout(std430,set=0,binding=6) readonly buffer Inputs { Query queries[]; };
layout(std430,set=0,binding=7) readonly buffer Exclusions { uvec2 exclusions[]; };
layout(std430,set=1,binding=0) buffer Results { Hit hits[]; };
layout(std430,set=1,binding=1) buffer Counts { uint counts[]; };
layout(std430,set=1,binding=2) buffer Status { uvec2 status; };
layout(std140,set=2,binding=0) uniform Settings { uint queryCount; uint leaves; uint shapeCount; uint bodyCount; };
const float epsilon=1.1920928955078125e-7;
const vec4 tolerances=vec4(0,0.5,0.00001,0);
#include "PhysicsCollisionMath.inc.glsl"
ResidentBody bodyA;
float contactLimit=0;
void fail(){atomicOr(status.x,1u);}
#include "PhysicsResidentCollision.inc.glsl"
bool lessKey(uvec4 a,uvec4 b){return a.y<b.y||(a.y==b.y&&(a.x<b.x||(a.x==b.x&&(a.z<b.z||(a.z==b.z&&a.w<b.w)))));}
bool contains(Hull h)
{
    if(h.boundary){vec3 p=boundaryPlane(h);return p.z+h.radius>=0;}
    if(h.count==1u){vec2 d=vertex(h,0u);float squared=dot2(d,d),radius=h.radius*h.radius;if(!finite2(vec2(squared,radius))){fail();return false;}return squared<=radius;}
    if(h.count==2u){if(h.radius==0)return false;return length(closest(vec2(0),vertex(h,0u),vertex(h,1u)))<=h.radius;}
    for(uint i=0u;i<h.count;i++)if(dot2(-vertex(h,i),edgeNormal(h,i))>0)return false;
    return true;
}
bool circleHit(vec2 center,float radius,vec2 d,out float fraction,out vec2 normal)
{
    fraction=0;normal=vec2(0);float len=length(d);if(len==0)return false;
    vec2 direction=d/len;float projected=dot2(center,direction);
    vec2 closestPoint=-center+projected*direction;float squared=dot2(closestPoint,closestPoint),rr=radius*radius;
    if(!finite2(vec2(squared,rr))||!finite2(vec2(projected,len))){fail();return false;}
    if(squared>rr)return false;
    float distance=projected-sqrt(rr-squared);if(distance<0||distance>len)return false;
    fraction=distance/len;normal=normalized(-center+distance*direction);return true;
}
bool queryRay(Hull h,vec2 d,out float fraction,out vec2 normal)
{
    if(h.boundary){uint feature;return rayHit(h,vec2(0),d,fraction,normal,feature);}

    fraction=2;normal=vec2(0);
    if(h.count==1u)return circleHit(vertex(h,0u),h.radius,d,fraction,normal);
    if(h.count==2u)
    {
        vec2 a=vertex(h,0u),b=vertex(h,1u),axis=normalized(b-a),n=vec2(axis.y,-axis.x);
        float extent=dot2(b-a,axis),origin=dot2(-a,axis),speed=dot2(d,axis);
        for(int side=-1;side<=1;side+=2)
        {
            vec2 outNormal=float(side)*n;float denominator=dot2(d,outNormal);if(denominator>=0)continue;
            float t=(h.radius+dot2(a,outNormal))/denominator,along=origin+t*speed;
            if(t>=0&&t<=1&&along>=0&&along<=extent&&t<fraction){fraction=t;normal=outNormal;}
        }
        if(h.radius>0)
            for(uint i=0u;i<2u;i++)
            {
                float t;vec2 outNormal;if(!circleHit(i==0u?a:b,h.radius,d,t,outNormal))continue;
                float along=origin+t*speed;
                if((i==0u?along<=0:along>=extent)&&t<fraction){fraction=t;normal=outNormal;}
            }
        return fraction<=1;
    }
    float entry=0,exit=1;
    for(uint i=0u;i<h.count;i++)
    {
        vec2 n=edgeNormal(h,i);float distance=dot2(n,vertex(h,i)),speed=dot2(n,d);
        if(speed==0){if(distance<0)return false;continue;}
        float t=distance/speed;
        if(speed<0&&t>entry){entry=t;normal=n;}else if(speed>0)exit=min(exit,t);
        if(entry>exit)return false;
    }
    fraction=entry;return normal!=vec2(0)&&entry<=1;
}
bool overlapsRayBounds(vec4 bounds,Query q,bool ray)
{
    vec2 lo=bounds.xy-q.ray.xy,hi=bounds.zw-q.ray.xy;
    if(!ray)return all(lessThanEqual(lo,vec2(0)))&&all(greaterThanEqual(hi,vec2(0)));
    float entry=0,exit=1;
    for(int axis=0;axis<2;axis++)
    {
        float d=q.ray[axis+2];
        if(d==0){if(lo[axis]>0||hi[axis]<0)return false;continue;}
        float a=lo[axis]/d,b=hi[axis]/d;entry=max(entry,min(a,b));exit=min(exit,max(a,b));if(entry>exit)return false;
    }
    return true;
}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=queryCount)return;
    Query q=queries[i];bool ray=(q.policy.y&4u)!=0u;uint used=0u;counts[i]=0u;
    if(q.policy.z==0u||q.policy.x==0u||(q.policy.y&3u)==0u||(ray&&q.ray.zw==vec2(0)))return;
    bodyA=ResidentBody(vec4(q.ray.xy,1,0),vec4(0),vec4(0),vec4(0),vec4(0),uvec4(0));
    int at=1;
    while(at!=0)
    {
        Node n=nodes[at];
        if(n.hasCategory!=0&&((n.typeMask&16)!=0||overlapsRayBounds(n.bounds,q,ray))&&((q.policy.y&1u)!=0u||(n.typeMask&8)!=0))
        {
            if(at<int(leaves)){at*=2;continue;}
            uint index=uint(n.shape);if(index>=shapeCount){fail();return;}
            Shape s=shapes[index];Mapping mapping=mappings[index];bool sensor=(s.policy.w&2u)!=0u;
            bool accepted=(s.policy.y&q.policy.x)!=0u&&(q.policy.y&(sensor?2u:1u))!=0u&&(ray||q.extra.zw==mapping.canvas.xy);
            for(uint j=0u;accepted&&j<q.extra.y;j++)if(exclusions[q.extra.x+j]==mapping.identity.xy)accepted=false;
            if(accepted)
            {
                if(s.owner.x>=bodyCount||mapping.identity.w!=s.policy.x){fail();return;}
                Geometry g=geometries[s.owner.z];ResidentBody body=bodies[s.owner.x];
                if(body.flags.x!=s.owner.y||body.flags.w==0u||g.data.w!=s.owner.w){fail();return;}
                if(g.data.z!=6u&&g.data.y!=0u)
                {
                    uint pieces=g.data.z==5u?g.data.y/2u:1u;bool found=false,inside=false;float fraction=2;vec2 normal=vec2(0);
                    for(uint piece=0u;piece<pieces;piece++)
                    {
                        Hull h=hull(s,g,body,piece);
                        if(contains(h)){inside=true;if(!ray||(q.policy.y&8u)!=0u){found=true;fraction=0;normal=vec2(0);}break;}
                        if(ray){float t;vec2 direction;if(queryRay(h,q.ray.zw,t,direction)&&t<fraction){found=true;fraction=t;normal=direction;}}
                    }
                    if(ray&&inside&&(q.policy.y&8u)==0u)found=false;
                    if(found)
                    {
                        uvec4 key=uvec4(mapping.identity.xyz,index);uint position=used;
                        if(ray)
                        {position=0u;if(used!=0u&&(fraction>hits[q.policy.w].fraction.x||(fraction==hits[q.policy.w].fraction.x&&!lessKey(key,hits[q.policy.w].identity))))found=false;}
                        else
                        {
                            position=0u;while(position<used&&lessKey(hits[q.policy.w+position].identity,key))position++;
                            for(uint j=0u;j<used;j++)if(hits[q.policy.w+j].identity.xyz==key.xyz)found=false;
                        }
                        if(found&&position<q.policy.z)
                        {
                            // ponytail: ordered insertion is O(hits * limit); use a stable bounded heap if large requested caps dominate measured cost.
                            if(!ray)for(uint j=min(used,q.policy.z-1u);j>position;j--)hits[q.policy.w+j]=hits[q.policy.w+j-1u];
                            vec2 point=q.ray.xy+(ray?fraction*q.ray.zw:vec2(0));
                            if(!finite2(point)||!finite2(normal)||!finite2(vec2(fraction))){fail();return;}
                            hits[q.policy.w+position]=Hit(key,uvec4(s.policy.x,s.owner.xy,0),vec4(point,normal),vec4(fraction,0,0,0));
                            used=ray?1u:min(used+1u,q.policy.z);
                        }
                    }
                }
            }
        }
        while(at>1&&(at&1)!=0)at/=2;
        at=at==1?0:at+1;
    }
    counts[i]=used;
}
