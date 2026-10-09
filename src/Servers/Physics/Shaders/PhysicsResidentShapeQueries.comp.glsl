#version 450
#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_control_flow_attributes : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
layout(local_size_x=64) in;
struct Node { vec4 bounds; int typeMask; int proxy; int hasCategory; int shape; };
struct Mapping { uvec4 identity; uvec4 canvas; };
struct Query { vec4 ray; uvec4 policy; uvec4 extra; vec4 rotationMargin; uvec4 geometry; };
struct Hit { uvec4 identity; uvec4 owner; vec4 points; vec4 contact; vec4 motion; uvec4 objectID; };
layout(std430,set=0,binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=0,binding=1) readonly buffer Vertices { vec2 vertices[]; };
layout(std430,set=0,binding=2) readonly buffer Geometries { Geometry geometries[]; };
layout(std430,set=0,binding=3) readonly buffer Shapes { Shape shapes[]; };
layout(std430,set=0,binding=4) readonly buffer Nodes { Node nodes[]; };
layout(std430,set=0,binding=5) readonly buffer Mappings { Mapping mappings[]; };
layout(std430,set=0,binding=6) readonly buffer Inputs { Query queries[]; };
layout(std430,set=0,binding=7) readonly buffer Exclusions { uvec2 exclusions[]; };
layout(std430,set=1,binding=3) buffer Centers { vec2 centers[]; };
layout(std430,set=1,binding=0) buffer Results { Hit hits[]; };
layout(std430,set=1,binding=1) buffer Counts { uint counts[]; };
layout(std430,set=1,binding=2) buffer Status { uvec2 status; };
layout(std140,set=2,binding=0) uniform Settings { uint queryCount; uint leaves; uint shapeCount; uint bodyCount; };
const float epsilon=1.1920928955078125e-7;
const vec4 tolerances=vec4(0,0.5,0.00001,0);
#include "PhysicsCollisionMath.inc.glsl"
ResidentBody bodyA,bodyB;
float contactLimit=3.402823466e38;
void failQuery(uint flag){atomicOr(status.x,flag);}
void fail(){failQuery(1u);}
#include "PhysicsResidentCollision.inc.glsl"
#include "PhysicsQueryDistance.inc.glsl"
Query activeQuery;
Shape activeShape;
uvec4 activeKey;
uint used=0u;
float activeFraction=0;
bool lessKey(uvec4 a,uvec4 b){return a.y<b.y||(a.y==b.y&&(a.x<b.x||(a.x==b.x&&(a.z<b.z||(a.z==b.z&&a.w<b.w)))));}
void emitPoint(vec2 a,vec2 b,vec2 normal,uvec4 features)
{
    float depth=-dot2(b-a,normal);if(depth<-contactLimit-tolerances.z)return;
    uint mode=activeQuery.geometry.z,offset=activeQuery.policy.w,limit=activeQuery.policy.z,position=0u;
    if(mode==2u||mode==3u)
    {
        if(used!=0u)
        {
            Hit prior=hits[offset];float value=mode==2u?-depth:activeFraction,old=mode==2u?-prior.contact.z:prior.contact.w;
            if(value>old||(value==old&&!lessKey(activeKey,prior.identity)))return;
        }
    }
    else
    {
        while(position<used&&(lessKey(hits[offset+position].identity,activeKey)||hits[offset+position].identity==activeKey))position++;
        if(mode==0u)for(uint j=0u;j<used;j++)if(hits[offset+j].identity.xyz==activeKey.xyz)return;
        if(position>=limit)return;
        for(uint j=min(used,limit-1u);j>position;j--)hits[offset+j]=hits[offset+j-1u];
    }
    vec2 pointA=a+bodyA.pose.xy,pointB=b+bodyA.pose.xy;
    vec2 relative=b-(bodyB.pose.xy-bodyA.pose.xy+rotate(bodyB.pose.zw,centers[activeShape.owner.x]));
    vec3 speed=bodyB.velocity.xyz+bodyB.surface.xyz;
    vec2 velocity=(activeShape.policy.w&2u)!=0u?vec2(0):speed.xy+speed.z*vec2(-relative.y,relative.x);
    float safe=mode==3u?max(0,ceil(activeFraction*256)-1)/256:0,unsafe=mode==3u?min(1,safe+1.0/256):0;
    if(!finite2(pointA)||!finite2(pointB)||!finite2(normal)||!finite2(velocity)||!finite2(vec2(depth,activeFraction))){fail();return;}
    hits[offset+position]=Hit(activeKey,uvec4(activeShape.policy.x,activeShape.owner.xy,features.z),vec4(pointA,pointB),vec4(-normal,depth,activeFraction),vec4(velocity,safe,unsafe),uvec4(mappings[activeKey.w].canvas.zw,0,0));
    used=(mode==2u||mode==3u)?1u:min(used+1u,limit);
}
#include "PhysicsResidentManifold.inc.glsl"
#include "PhysicsDirectedQuery.inc.glsl"
bool piecesContact(Hull a,Hull b,Geometry ga,Geometry gb,vec2 motion,bool emit,uint pieceA,uint pieceB)
{
    bool boundary=a.boundary||b.boundary;
    if(boundary)
    {
        if(a.boundary&&b.boundary)return false;
        if(ga.data.z==6u){vec2 axis=vertex(a,1u)-vertex(a,0u);if(axis==vec2(0))return false;a.extension=normalized(axis)*activeQuery.rotationMargin.z;}
        if(gb.data.z==6u&&vertex(b,1u)==vertex(b,0u))return false;
    }
    if(!boundary&&ga.data.z==6u)return gb.data.z!=6u&&directedQuery(a,b,vec2(0),activeQuery.rotationMargin.z,ga.parameters.y!=0,false,motion,pieceA,pieceB,emit);
    if(!boundary&&gb.data.z==6u)return directedQuery(b,a,motion,0,gb.parameters.y!=0,true,vec2(0),pieceA,pieceB,emit);
    vec2 normal;float distance=queryDistance(a,b,vec2(0),vec2(0),normal),radii=a.radius+b.radius;
    bool initial=distance-radii<=0.05;
    if(activeQuery.geometry.z==3u&&initial)return false;
    float fraction=0;
    if(!initial)
    {
        if(motion==vec2(0)||!querySweep(a,b,motion,vec2(0),max(0.5,radii-0.5),0.25,fraction,normal))return false;
        a.pose.xy+=min(1,fraction+1/length(motion))*motion;
    }
    activeFraction=fraction;
    if(emit)
    {
        if(activeQuery.geometry.z==0u||activeQuery.geometry.z==3u){contactLimit=3.402823466e38;emitPoint(vec2(0),vec2(0),vec2(0),uvec4(0,0,pieceA,pieceB));}
        else{contactLimit=2;ordinary(a,b,uvec2(pieceA,pieceB));}
    }
    return true;
}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=queryCount)return;counts[i]=0u;
    activeQuery=queries[i];Query q=activeQuery;if(q.policy.z==0u||q.policy.x==0u||(q.policy.y&3u)==0u)return;
    Geometry ga=geometries[q.geometry.x];if(ga.data.w!=q.geometry.y){fail();return;}if(ga.data.y==0u)return;
    bodyA=ResidentBody(vec4(q.ray.xy,q.rotationMargin.xy),vec4(0),vec4(0),vec4(0),vec4(0),uvec4(0));
    Shape sa=Shape(vec4(0,0,1,0),uvec4(0),uvec4(0),vec2(0),uvec2(0),vec4(0));
    vec2 lo=vec2(3.402823466e38),hi=-lo;
    for(uint v=0u;v<ga.data.y;v++){vec2 p=rotate(q.rotationMargin.xy,vertices[ga.data.x+v]);lo=min(lo,p);hi=max(hi,p);}
    vec2 expansion=vec2((ga.data.z==7u?0:ga.parameters.x)+q.rotationMargin.z+2);
    vec2 envelope=q.ray.zw;
    if(ga.data.z==6u)
    {
        vec2 axis=normalized(rotate(q.rotationMargin.xy,vertices[ga.data.x+1u]-vertices[ga.data.x]));
        envelope=axis*max(0,dot2(axis,envelope));
    }
    vec4 bounds=vec4(lo+min(envelope,vec2(0))-expansion+q.ray.xy,hi+max(envelope,vec2(0))+expansion+q.ray.xy);
    if(!finiteField(bounds)){fail();return;}
    int at=1;
    while(at!=0)
    {
        Node node=nodes[at];
        if(node.hasCategory!=0&&(ga.data.z==7u||(node.typeMask&16)!=0||(all(lessThanEqual(node.bounds.xy,bounds.zw))&&all(greaterThanEqual(node.bounds.zw,bounds.xy)))))
        {
            if(at<int(leaves)){at*=2;continue;}
            uint index=uint(node.shape);if(index>=shapeCount){fail();return;}Shape sb=shapes[index];Mapping mapping=mappings[index];
            bool accepted=(sb.policy.y&q.policy.x)!=0u&&(q.policy.y&((sb.policy.w&2u)!=0u?2u:1u))!=0u;
            for(uint j=0u;accepted&&j<q.extra.y;j++)if(exclusions[q.extra.x+j]==mapping.identity.xy)accepted=false;
            if(accepted)
            {
                if(sb.owner.x>=bodyCount||mapping.identity.w!=sb.policy.x){fail();return;}
                bodyB=bodies[sb.owner.x];Geometry gb=geometries[sb.owner.z];
                if(bodyB.flags.x!=sb.owner.y||bodyB.flags.w==0u||gb.data.w!=sb.owner.w){fail();return;}
                if(gb.data.y!=0u)
                {
                    activeShape=sb;activeKey=uvec4(mapping.identity.xyz,index);
                    uint na=ga.data.z==5u?ga.data.y/2u:1u,nb=gb.data.z==5u?gb.data.y/2u:1u;
                    bool initialOverlap=false;
                    if(q.geometry.z==3u)for(uint pa=0u;pa<na&&!initialOverlap;pa++)for(uint pb=0u;pb<nb&&!initialOverlap;pb++)
                    {
                        Hull a=hull(sa,ga,bodyA,pa),b=hull(sb,gb,bodyB,pb);if(ga.data.z!=6u)a.radius+=q.rotationMargin.z;
                        if(ga.data.z==6u||gb.data.z==6u)initialOverlap=piecesContact(a,b,ga,gb,vec2(0),false,pa,pb);
                        else {vec2 normal;initialOverlap=queryDistance(a,b,vec2(0),vec2(0),normal)-a.radius-b.radius<=0.05;}
                    }
                    if(!initialOverlap)for(uint pa=0u;pa<na;pa++)for(uint pb=0u;pb<nb;pb++)
                    {
                        Hull a=hull(sa,ga,bodyA,pa),b=hull(sb,gb,bodyB,pb);
                        if(ga.data.z!=6u)a.radius+=q.rotationMargin.z;
                        if((ga.data.z==6u||gb.data.z==6u)&&q.geometry.z==3u)
                        {
                            if(piecesContact(a,b,ga,gb,vec2(0),false,pa,pb)||q.ray.zw==vec2(0)||!piecesContact(a,b,ga,gb,q.ray.zw,false,pa,pb))continue;
                            float low=0,high=1;
                            [[dont_unroll]] for(uint step=0u;step<8u;step++){float mid=(low+high)*0.5;if(piecesContact(a,b,ga,gb,mid*q.ray.zw,false,pa,pb))high=mid;else low=mid;}
                            activeFraction=high;contactLimit=3.402823466e38;emitPoint(vec2(0),vec2(0),vec2(0),uvec4(0,0,pa,pb));
                        }
                        else piecesContact(a,b,ga,gb,q.ray.zw,true,pa,pb);
                    }
                }
            }
        }
        while(at>1&&(at&1)!=0)at/=2;at=at==1?0:at+1;
    }
    counts[i]=used;
}
