#version 450
#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_control_flow_attributes : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
layout(local_size_x=64) in;
struct Node { vec4 bounds; int typeMask; int proxy; int hasCategory; int shape; };
struct Mapping { uvec4 identity; uvec4 associations; };
struct Query { vec4 pose; vec4 motionMargin; uvec4 body; uvec4 exclusions; uvec4 policy; };
struct Result { uvec4 identity; uvec4 shapes; uvec4 owner; uvec4 objectID; vec4 pointNormal; vec4 velocityDepth; vec4 travelRemainder; vec4 fractions; };
layout(std430,set=0,binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430,set=0,binding=1) readonly buffer Vertices { vec2 vertices[]; };
layout(std430,set=0,binding=2) readonly buffer Geometries { Geometry geometries[]; };
layout(std430,set=0,binding=3) readonly buffer Shapes { Shape shapes[]; };
layout(std430,set=0,binding=4) readonly buffer Nodes { Node nodes[]; };
layout(std430,set=0,binding=5) readonly buffer Mappings { Mapping mappings[]; };
layout(std430,set=0,binding=6) readonly buffer Inputs { Query queries[]; };
layout(std430,set=0,binding=7) readonly buffer Payload { uvec2 payload[]; };
layout(std430,set=1,binding=0) buffer Results { Result results[]; };
layout(std430,set=1,binding=1) buffer Counts { uint counts[]; };
layout(std430,set=1,binding=2) buffer Status { uvec2 status; };
layout(std430,set=1,binding=3) buffer Centers { vec2 centers[]; };
layout(std140,set=2,binding=0) uniform Settings { uint queryCount; uint leaves; uint shapeCount; uint bodyCount; };
const float epsilon=1.1920928955078125e-7;
const vec4 tolerances=vec4(0,0.5,0.00001,0);
#include "PhysicsCollisionMath.inc.glsl"
ResidentBody bodyA,bodyB;
float contactLimit=2;
void failQuery(uint flag){atomicOr(status.x,flag);}
void fail(){failQuery(1u);}
#include "PhysicsResidentCollision.inc.glsl"
#include "PhysicsQueryDistance.inc.glsl"
struct Contact { vec2 point; vec2 normal; float depth; bool valid; };
Contact pairContact;
void emitPoint(vec2 a,vec2 b,vec2 normal,uvec4 features)
{
    float depth=-dot2(b-a,normal);if(depth<-contactLimit-tolerances.z)return;
    if(!finite2(a)||!finite2(b)||!finite2(normal)||!finite2(vec2(depth))){fail();return;}
    if(!pairContact.valid||depth>pairContact.depth)pairContact=Contact(b,-normal,depth,true);
}
#include "PhysicsResidentManifold.inc.glsl"
#include "PhysicsDirectedQuery.inc.glsl"
Query q;
Shape shapeA,shapeB;
Mapping mapA,mapB;
uint indexA,indexB;
float safe=1,unsafeFraction=1;
vec2 recovery=vec2(0);
Result recoveryResult,motionResult;
bool recovered=false,blocked=false;
Contact evaluate(Hull a,Hull b,Geometry ga,Geometry gb,vec2 extension,float margin,bool requireOverlap)
{
    pairContact=Contact(vec2(0),vec2(0),0,false);contactLimit=2;
    if(a.boundary||b.boundary)
    {
        if(a.boundary&&b.boundary)return pairContact;
        if(ga.data.z==6u){vec2 axis=vertex(a,1u)-vertex(a,0u);if(axis==vec2(0))return pairContact;a.extension=normalized(axis)*margin;}
        if(gb.data.z==6u&&vertex(b,1u)==vertex(b,0u))return pairContact;
        a.pose.xy+=extension;vec2 normal;
        if(!requireOverlap||queryDistance(a,b,vec2(0),vec2(0),normal)-a.radius-b.radius<=0.05)ordinary(a,b,uvec2(0));
    }
    else if(ga.data.z==6u){if(gb.data.z!=6u)directedQuery(a,b,vec2(0),margin,ga.parameters.y!=0,false,extension,0u,0u,true);}
    else if(gb.data.z==6u)directedQuery(b,a,extension,0,gb.parameters.y!=0,true,vec2(0),0u,0u,true);
    else
    {
        vec2 normal;
        if(!requireOverlap||queryDistance(a,b,vec2(0),vec2(0),normal)-a.radius-b.radius<=0.05)ordinary(a,b,uvec2(0));
    }
    return pairContact;
}
vec2 passDirection(){return rotate(bodyB.pose.zw,rotate(shapeB.pose.zw,shapeB.oneWay.xy));}
bool oneWayContact(Contact c)
{
    return (shapeB.policy.w&4u)==0u||(dot2(c.normal,passDirection())<0&&c.depth<=max(shapeB.oneWay.z,q.motionMargin.z));
}
Result snapshot(Contact c)
{
    vec2 relative=c.point-(bodyB.pose.xy-bodyA.pose.xy+rotate(bodyB.pose.zw,centers[shapeB.owner.x]));
    vec3 speed=bodyB.velocity.xyz+bodyB.surface.xyz;
    vec2 velocity=speed.xy+speed.z*vec2(-relative.y,relative.x);
    if(!finite2(velocity)){fail();velocity=vec2(0);}
    return Result(uvec4(mapB.identity.xy,mapA.identity.z,mapB.identity.z),uvec4(indexA,shapeA.policy.x,indexB,shapeB.policy.x),
        uvec4(shapeB.owner.xy,1,0),uvec4(mapB.associations.zw,0,0),vec4(c.point+bodyA.pose.xy,c.normal),vec4(velocity,max(0,c.depth),0),vec4(0),vec4(0));
}
bool excluded(uvec2 key,uint start,uint count)
{for(uint i=0u;i<count;i++)if(payload[start+i]==key)return true;return false;}
bool candidate(Shape s,Mapping mapping)
{
    if(s.owner.x==q.body.x||(s.policy.w&2u)!=0u||(shapeA.policy.z&s.policy.y)==0u||(s.policy.z&shapeA.policy.y)==0u)return false;
    if(excluded(mapping.identity.xy,q.exclusions.x,q.exclusions.y)||excluded(s.owner.xy,q.policy.x,q.policy.y))return false;
    return mapping.associations.zw==uvec2(0)||!excluded(mapping.associations.zw,q.exclusions.z,q.exclusions.w);
}
void movingPair(Hull a,Hull b,Geometry ga,Geometry gb)
{
    vec2 motion=q.motionMargin.xy;
    if((shapeB.policy.w&4u)!=0u&&dot2(motion,passDirection())<=0)return;
    Contact initial=evaluate(a,b,ga,gb,vec2(0),0,true);
    if(ga.data.z==6u||gb.data.z==6u)
    {
        Contact full=evaluate(a,b,ga,gb,motion*safe,0,true);if(!full.valid)return;
        if(initial.valid&&initial.depth<=0.05&&dot2(motion,initial.normal)>=-0.0001)return;
        float low=0,high=initial.valid?0:safe;
        if(!initial.valid)
            [[dont_unroll]] for(uint step=0u;step<8u;step++)
            {
                float middle=(low+high)*0.5;Contact test=evaluate(a,b,ga,gb,motion*middle,0,true);
                if(test.valid)high=middle;else low=middle;
            }
        if(low>=safe&&blocked)return;
        Hull impact=a;impact.pose.xy+=high*motion;
        Contact contact=evaluate(impact,b,ga,gb,vec2(0),q.motionMargin.z,true);if(!contact.valid)contact=full;
        if(!oneWayContact(contact))return;
        safe=low;unsafeFraction=high;motionResult=snapshot(contact);blocked=true;return;
    }
    if(initial.valid)
    {
        if((initial.depth>0.05||dot2(motion,initial.normal)<-0.0001)&&oneWayContact(initial))
        {safe=unsafeFraction=0;motionResult=snapshot(initial);blocked=true;}
        return;
    }
    if(safe==0)return;
    float fraction;vec2 normal;
    if(!querySweep(a,b,motion*safe,vec2(0),max(0.5,a.radius+b.radius-0.5),0.25,fraction,normal))return;
    float low=max(0,ceil(fraction*256)-1)/256*safe,high=min(safe,low+safe/256);
    if(low>=safe)return;
    safe=low;unsafeFraction=high;
    Hull impact=a;impact.pose.xy+=min(1,high+1/length(motion))*motion;
    Contact contact=evaluate(impact,b,ga,gb,vec2(0),0,false);
    if(!contact.valid)
    {
        uint unused;vec2 toward=querySupportVertex(impact,normal),point=support(b,-normal,toward,unused)-b.radius*normal;
        contact=Contact(point,-normal,0,true);
    }
    motionResult=snapshot(contact);blocked=true;
}
vec4 queryBounds(Shape shape,Geometry g,bool recovering)
{
    vec2 lo=vec2(3.402823466e38),hi=-lo;
    for(uint i=0u;i<g.data.y;i++)
    {
        vec2 point=rotate(bodyA.pose.zw,shape.pose.xy+rotate(shape.pose.zw,vertices[g.data.x+i]));lo=min(lo,point);hi=max(hi,point);
    }
    vec2 motion=recovering?vec2(0):q.motionMargin.xy;
    vec2 grow=vec2((g.data.z==7u?0:g.parameters.x)+q.motionMargin.z+2);
    vec4 bounds=vec4(lo+min(motion,vec2(0))-grow,hi+max(motion,vec2(0))+grow);
    if(!recovering&&g.data.z==6u)
    {
        vec2 axis=normalized(rotate(bodyA.pose.zw,rotate(shape.pose.zw,vertices[g.data.x+1u]-vertices[g.data.x])));
        vec2 extension=axis*max(0,dot2(axis,motion));bounds.xy+=min(extension,vec2(0));bounds.zw+=max(extension,vec2(0));
    }
    bounds+=bodyA.pose.xyxy;if(!finiteField(bounds))fail();return bounds;
}
void scan(bool recovering)
{
    float bestDepth=0;vec2 bestNormal=vec2(0);Result best;
    for(uint own=0u;own<q.body.w;own++)
    {
        uvec2 token=payload[q.body.z+own];indexA=token.x;if(indexA>=shapeCount){fail();return;}
        shapeA=shapes[indexA];mapA=mappings[indexA];
        if(shapeA.policy.x!=token.y||shapeA.owner.xy!=q.body.xy||mapA.identity.w!=token.y){fail();return;}
        if((shapeA.policy.w&2u)!=0u)continue;
        Geometry ga=geometries[shapeA.owner.z];if(ga.data.w!=shapeA.owner.w){fail();return;}if(ga.data.y==0u)continue;
        if(!recovering&&ga.data.z==6u&&ga.parameters.y==0&&(q.policy.z&2u)==0u)continue;
        vec4 bounds=queryBounds(shapeA,ga,recovering);int at=1;
        while(at!=0)
        {
            Node node=nodes[at];
            if(node.hasCategory!=0&&(ga.data.z==7u||(node.typeMask&16)!=0||(all(lessThanEqual(node.bounds.xy,bounds.zw))&&all(greaterThanEqual(node.bounds.zw,bounds.xy)))))
            {
                if(at<int(leaves)){at*=2;continue;}
                indexB=uint(node.shape);if(indexB>=shapeCount){fail();return;}shapeB=shapes[indexB];mapB=mappings[indexB];
                if(candidate(shapeB,mapB))
                {
                    if(shapeB.owner.x>=bodyCount||mapB.identity.w!=shapeB.policy.x){fail();return;}
                    bodyB=bodies[shapeB.owner.x];Geometry gb=geometries[shapeB.owner.z];
                    if(bodyB.flags.x!=shapeB.owner.y||bodyB.flags.w==0u||gb.data.w!=shapeB.owner.w){fail();return;}
                    if(gb.data.y!=0u)
                    {
                        uint na=ga.data.z==5u?ga.data.y/2u:1u,nb=gb.data.z==5u?gb.data.y/2u:1u;
                        for(uint pa=0u;pa<na;pa++)for(uint pb=0u;pb<nb;pb++)
                        {
                            Hull a=hull(shapeA,ga,bodyA,pa),b=hull(shapeB,gb,bodyB,pb);
                            if(recovering)
                            {
                                if(ga.data.z!=6u)a.radius+=q.motionMargin.z;
                                Contact contact=evaluate(a,b,ga,gb,vec2(0),q.motionMargin.z,true);
                                if(contact.valid&&contact.depth>bestDepth&&oneWayContact(contact))
                                {bestDepth=contact.depth;bestNormal=contact.normal;best=snapshot(contact);}
                            }
                            else movingPair(a,b,ga,gb);
                        }
                    }
                }
            }
            while(at>1&&(at&1)!=0)at/=2;at=at==1?0:at+1;
        }
    }
    if(recovering&&bestDepth>0)
    {
        recovered=true;recoveryResult=best;
        recovery+=bestNormal*max(0,bestDepth-q.motionMargin.z*0.05)*0.4;
    }
}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=queryCount)return;counts[i]=1u;q=queries[i];
    if(q.body.x>=bodyCount){fail();return;}bodyA=bodies[q.body.x];
    if(bodyA.flags.x!=q.body.y||bodyA.flags.w==0u){fail();return;}
    bodyA.pose=q.pose;
    [[dont_unroll]] for(uint attempt=0u;attempt<4u;attempt++)
    {
        vec2 before=recovery;scan(true);if(before==recovery)break;bodyA.pose.xy=q.pose.xy+recovery;
    }
    if(q.motionMargin.xy!=vec2(0))scan(false);
    Result result=Result(uvec4(0),uvec4(0),uvec4(0),uvec4(0),vec4(0),vec4(0),vec4(0),vec4(0));
    if(blocked)result=motionResult;else if(recovered&&(q.policy.z&1u)!=0u)result=recoveryResult;
    result.travelRemainder=vec4(recovery+safe*q.motionMargin.xy,(1-safe)*q.motionMargin.xy);
    result.fractions=vec4(safe,unsafeFraction,recovery);
    if(!finiteField(result.travelRemainder)||!finiteField(result.pointNormal)||!finiteField(result.velocityDepth)||!finiteField(result.fractions)){fail();return;}
    results[i]=result;
}
