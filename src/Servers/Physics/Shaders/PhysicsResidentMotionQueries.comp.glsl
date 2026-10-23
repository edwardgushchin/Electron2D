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
// Result records occupy the prefix; recovery planes and rejected keys stay device-only.
#ifdef E2D_MOTION_STAGE
layout(std430,set=1,binding=0) buffer Results { uvec4 outputWords[]; };
#define READ_FLOAT(index) uintBitsToFloat(outputWords[index])
#define WRITE_FLOAT(index,value) outputWords[index]=floatBitsToUint(value)
#define READ_UINT(index) outputWords[index]
#define WRITE_UINT(index,value) outputWords[index]=value
#else
layout(std430,set=1,binding=0) buffer Results { vec4 outputData[]; };
#define READ_FLOAT(index) outputData[index]
#define WRITE_FLOAT(index,value) outputData[index]=value
#define READ_UINT(index) floatBitsToUint(outputData[index])
#define WRITE_UINT(index,value) outputData[index]=uintBitsToFloat(value)
#endif
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
Contact pairContact,pairSecond;
uint recoveryCount=0u,rejectedCount=0u;
uint scratchStart;
bool oneWayContact(Contact c);
void retainRecovery(Contact c)
{
    if(c.depth<=0||!oneWayContact(c))return;
    vec4 plane=vec4(c.normal,c.depth,bodyB.surface.w);
    if(recoveryCount<32u){WRITE_FLOAT(scratchStart+recoveryCount++,plane);return;}
    uint shallowest=0u;
    [[dont_unroll]] for(uint j=1u;j<32u;j++)if(READ_FLOAT(scratchStart+j).z<READ_FLOAT(scratchStart+shallowest).z)shallowest=j;
    if(c.depth>READ_FLOAT(scratchStart+shallowest).z)WRITE_FLOAT(scratchStart+shallowest,plane);
}
float relativePriority(float value,float maximum)
{
    // Divide normalized significands so a reciprocal of a huge weight cannot flush to zero.
    uint a=floatBitsToUint(value),b=floatBitsToUint(maximum),ea=a>>23,eb=b>>23;
    float ma=float((a&0x7fffffu)|(ea==0u?0u:0x800000u)),mb=float((b&0x7fffffu)|(eb==0u?0u:0x800000u));
    return (ma/mb)*exp2(float(int(max(ea,1u))-int(max(eb,1u))));
}
void emitPoint(vec2 a,vec2 b,vec2 normal,uvec4 features)
{
    float depth=-dot2(b-a,normal);if(depth<-contactLimit-tolerances.z)return;
    if(!finite2(a)||!finite2(b)||!finite2(normal)||!finite2(vec2(depth))){fail();return;}
    Contact c=Contact(b,-normal,depth,true);
    if(!pairContact.valid||depth>pairContact.depth){pairSecond=pairContact;pairContact=c;}
    else if(!pairSecond.valid||depth>pairSecond.depth)pairSecond=c;
}
#include "PhysicsResidentManifold.inc.glsl"
#include "PhysicsDirectedQuery.inc.glsl"
Query q;
Shape shapeA,shapeB;
Mapping mapA,mapB;
uint indexA,indexB;
float safe=1,unsafeFraction=1;
vec2 recovery=vec2(0);
Result restResult;
uint motionShape=0u;
bool recovered=false,blocked=false,rested=false;
Contact evaluate(Hull a,Hull b,Geometry ga,Geometry gb,vec2 extension,float margin)
{
    pairContact=Contact(vec2(0),vec2(0),0,false);pairSecond=pairContact;contactLimit=2;
    bool boundary=a.boundary||b.boundary;
    if(boundary)
    {
        if(a.boundary&&b.boundary)return pairContact;
        if(ga.data.z==6u){vec2 axis=vertex(a,1u)-vertex(a,0u);if(axis==vec2(0))return pairContact;a.extension=normalized(axis)*margin;}
        if(gb.data.z==6u&&vertex(b,1u)==vertex(b,0u))return pairContact;
        a.pose.xy+=extension;
    }
    if(!boundary&&(ga.data.z==6u||gb.data.z==6u))
    {
        if(ga.data.z==6u&&gb.data.z==6u)return pairContact;
        bool flip=gb.data.z==6u;
        directedQuery(flip?b:a,flip?a:b,vec2(0),flip?0:margin,
            (flip?gb.parameters.y:ga.parameters.y)!=0,flip,flip?vec2(0):extension,0u,0u,true);
    }
    else ordinary(a,b,uvec2(0));
    return pairContact;
}
vec2 passDirection(){return rotate(bodyB.pose.zw,rotate(shapeB.pose.zw,shapeB.oneWay.xy));}
bool oneWayContact(Contact c)
{
    if(geometries[shapeA.owner.z].data.z==6u||(shapeB.policy.w&4u)==0u)return true;
    vec2 direction=passDirection();
    float allowance=max(shapeB.oneWay.z,q.motionMargin.z);
    if(bodyB.flags.y!=0u)allowance+=q.motionMargin.w*max(0,-dot2(bodyB.velocity.xy+bodyB.surface.xy,direction));
    // Cast roots and margin manifolds round independently at the allowance boundary.
    return dot2(c.normal,direction)<0&&c.depth<=allowance+tolerances.z;
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
    if(s.owner.x==q.body.x||(s.policy.w&2u)!=0u||(shapeA.policy.z&s.policy.y)==0u)return false;
    if(excluded(mapping.identity.xy,q.exclusions.x,q.exclusions.y)||excluded(s.owner.xy,q.policy.x,q.policy.y))return false;
    return mapping.associations.zw==uvec2(0)||!excluded(mapping.associations.zw,q.exclusions.z,q.exclusions.w);
}
void movingPair(Hull a,Hull b,Geometry ga,Geometry gb)
{
    bool ignored=ga.data.z==7u||(ga.data.z==5u&&gb.data.z!=7u);
    vec2 motion=ignored?vec2(0):q.motionMargin.xy;
    bool oneWay=ga.data.z!=6u&&(shapeB.policy.w&4u)!=0u;
    bool directed=ga.data.z==6u||gb.data.z==6u;
    // Keep one geometry call site across phases to limit driver inlining and cold compilation.
    const uint initialPhase=0u,reachPhase=1u,bisectPhase=2u,impactPhase=3u;
    uint phase=initialPhase,refinements=0u;
    float low=0,high=0,margin=0,middle=0,coefficient=0.5;vec2 extension=vec2(0),normal=vec2(0);
    Hull probe=a;Contact initial,full;
    [[dont_unroll]] for(;;)
    {
        Contact contact=evaluate(probe,b,ga,gb,extension,margin);
        contact.valid=contact.valid&&contact.depth>=0;
        if(phase==initialPhase)
        {
            initial=contact;
            if(contact.valid&&oneWay&&dot2(q.motionMargin.xy,passDirection())<0)return;
            if(directed){extension=motion;phase=reachPhase;continue;}
            if(contact.valid)
            {
                if((contact.depth>0.05||dot2(q.motionMargin.xy,contact.normal)<-0.0001)&&oneWayContact(contact))
                {safe=unsafeFraction=0;motionShape=mapA.identity.z;blocked=true;}
                return;
            }
            if(safe==0||ignored)return;
            float fraction;
            if(!querySweep(a,b,motion,vec2(0),a.radius+b.radius,0.00001,fraction,normal))return;
            vec2 bracket=querySweepBracket(fraction);low=bracket.x;high=bracket.y;
            if(low>=safe)return;
            if(oneWay){probe.pose.xy+=high*motion;probe.radius+=q.motionMargin.z;phase=impactPhase;continue;}
            safe=low;unsafeFraction=high;
            motionShape=mapA.identity.z;blocked=true;return;
        }
        if(phase==impactPhase)
        {
            if(!contact.valid){if(!directed)return;contact=full;}
            if(oneWay&&dot2(contact.normal,passDirection())>=0)return;
            safe=low;unsafeFraction=high;motionShape=mapA.identity.z;blocked=true;return;
        }
        if(phase==reachPhase)
        {
            full=contact;if(!full.valid)return;
            if(initial.valid&&initial.depth<=0.05&&dot2(q.motionMargin.xy,initial.normal)>=-0.0001)return;
            high=initial.valid?0:1;
            if(!initial.valid){middle=low+(high-low)*coefficient;extension=motion*middle;phase=bisectPhase;continue;}
        }
        else
        {
            if(contact.valid){high=middle;coefficient=refinements==0u||low>0?0.5:0.25;}
            else{low=middle;coefficient=refinements==0u||high<1?0.5:0.75;}
            if(++refinements<8u){middle=low+(high-low)*coefficient;extension=motion*middle;continue;}
        }
        if(low>=safe&&blocked)return;
        if(ga.data.z==6u&&gb.data.z!=7u&&!initial.valid)
        {
            float advance=dot2(normalized(vertex(a,1u)-vertex(a,0u)),motion);
            if(advance>0)high=clamp(1-full.depth/advance,low,high);
        }
        probe.pose.xy+=high*motion;extension=vec2(0);margin=q.motionMargin.z;phase=impactPhase;
    }
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
bool rejectedMotionPair()
{
    uvec4 key=uvec4(shapeA.owner.zw,indexB,shapeB.policy.x);
    for(uint i=0u;i<rejectedCount;i++)if(READ_UINT(scratchStart+32u+i)==key)return true;
    return false;
}
void scan(uint phase)
{
    bool recovering=phase==0u,resting=phase==2u;
    recoveryCount=0u;if(recovering)rejectedCount=0u;
    float bestDepth=resting?min(length(q.motionMargin.xy),q.motionMargin.z*0.05):0;Result best;
    bool found=false;
    for(uint own=0u;own<q.body.w;own++)
    {
        uvec2 token=payload[q.body.z+own];indexA=token.x;if(indexA>=shapeCount){fail();return;}
        shapeA=shapes[indexA];mapA=mappings[indexA];
        if(shapeA.policy.x!=token.y||shapeA.owner.xy!=q.body.xy||mapA.identity.w!=token.y){fail();return;}
        if((shapeA.policy.w&2u)!=0u)continue;
        Geometry ga=geometries[shapeA.owner.z];if(ga.data.w!=shapeA.owner.w){fail();return;}if(ga.data.y==0u)continue;
        if(!recovering&&!resting&&ga.data.z==6u&&ga.parameters.y==0&&(q.policy.z&2u)==0u)continue;
        if(resting&&blocked&&mapA.identity.z!=motionShape)continue;
        vec4 bounds=queryBounds(shapeA,ga,recovering||resting);int at=1;
        while(at!=0)
        {
            Node node=nodes[at];
            if(node.hasCategory!=0&&(ga.data.z==7u||(node.typeMask&16)!=0||(all(lessThanEqual(node.bounds.xy,bounds.zw))&&all(greaterThanEqual(node.bounds.zw,bounds.xy)))))
            {
                if(at<int(leaves)){at*=2;continue;}
                indexB=uint(node.shape);if(indexB>=shapeCount){fail();return;}shapeB=shapes[indexB];mapB=mappings[indexB];
                if(candidate(shapeB,mapB)&&(recovering||!rejectedMotionPair()))
                {
                    if(shapeB.owner.x>=bodyCount||mapB.identity.w!=shapeB.policy.x){fail();return;}
                    bodyB=bodies[shapeB.owner.x];Geometry gb=geometries[shapeB.owner.z];
                    if(bodyB.flags.x!=shapeB.owner.y||bodyB.flags.w==0u||gb.data.w!=shapeB.owner.w){fail();return;}
                    if(gb.data.y!=0u&&!(ga.data.z==5u&&gb.data.z==5u))
                    {
                        uint na=ga.data.z==5u?ga.data.y/2u:1u,nb=gb.data.z==5u?gb.data.y/2u:1u;
                        bool accepted=false,invalid=false;
                        for(uint pa=0u;pa<na;pa++)for(uint pb=0u;pb<nb;pb++)
                        {
                            Hull a=hull(shapeA,ga,bodyA,pa),b=hull(shapeB,gb,bodyB,pb);
                            if(recovering||resting)
                            {
                                if(ga.data.z!=6u)a.radius+=q.motionMargin.z;
                                evaluate(a,b,ga,gb,vec2(0),q.motionMargin.z);
                                for(uint point=0u;point<2u;point++)
                                {
                                    Contact contact=point==0u?pairContact:pairSecond;
                                    if(!contact.valid||contact.depth<0)continue;
                                    if(resting&&blocked&&unsafeFraction>0&&ga.data.z!=6u&&gb.data.z!=6u&&dot2(contact.normal,q.motionMargin.xy)>0)contact.normal=-contact.normal;
                                    if(!oneWayContact(contact)){invalid=true;continue;}
                                    accepted=true;
                                    if(recovering)retainRecovery(contact);
                                    if(contact.depth>bestDepth)
                                    {bestDepth=contact.depth;if(resting)best=snapshot(contact);found=true;}
                                }
                            }
                            else movingPair(a,b,ga,gb);
                        }
                        if(recovering&&invalid&&!accepted&&rejectedCount<32u)
                            WRITE_UINT(scratchStart+32u+rejectedCount++,uvec4(shapeA.owner.zw,indexB,shapeB.policy.x));
                    }
                }
            }
            while(at>1&&(at&1)!=0)at/=2;at=at==1?0:at+1;
        }
    }
    if(resting){rested=found;if(found)restResult=best;return;}
    if(recovering&&bestDepth>0)
    {
        recovered=true;
        float maximum=0,scaledTotal=0;
        [[dont_unroll]] for(uint j=0u;j<recoveryCount;j++)maximum=max(maximum,READ_FLOAT(scratchStart+j).w);
        if(maximum==0)return;
        [[dont_unroll]] for(uint j=0u;j<recoveryCount;j++)scaledTotal+=relativePriority(READ_FLOAT(scratchStart+j).w,maximum);
        bool tiny=maximum<0.00001/scaledTotal;
        vec2 correction=vec2(0);
        [[dont_unroll]] for(uint j=0u;j<recoveryCount;j++)
        {
            vec4 plane=READ_FLOAT(scratchStart+j);float depth=plane.z-dot2(plane.xy,correction)-q.motionMargin.z*0.05;
            float weight=tiny?plane.w:relativePriority(plane.w,maximum)*(float(recoveryCount)/scaledTotal);
            if(depth>0.00001)correction+=plane.xy*(depth*0.4*weight);
        }
        recovery+=correction;
    }
}
void main()
{
    uint i=gl_GlobalInvocationID.x;if(i>=queryCount)return;counts[i]=1u;q=queries[i];
    scratchStart=queryCount*8u+i*64u;
    if(q.body.x>=bodyCount){fail();return;}bodyA=bodies[q.body.x];
    if(bodyA.flags.x!=q.body.y||bodyA.flags.w==0u){fail();return;}
    bodyA.pose=q.pose;
#ifdef E2D_MOTION_STAGE
    // Persist only phase state in the existing result prefix between ordered device passes.
    uint stateAt=i*8u;
    bool converged=false;
#if E2D_MOTION_STAGE > 0
    uvec4 state=outputWords[stateAt+6u],fractions=outputWords[stateAt+7u];
    recovery=uintBitsToFloat(state.xy);rejectedCount=state.z;recovered=(state.w&1u)!=0u;converged=(state.w&2u)!=0u;
    safe=uintBitsToFloat(fractions.x);unsafeFraction=uintBitsToFloat(fractions.y);motionShape=fractions.z;blocked=fractions.w!=0u;
    bodyA.pose.xy=q.pose.xy+recovery;
#endif
#if E2D_MOTION_STAGE < 2
    if(!converged){vec2 before=recovery;scan(0u);converged=before==recovery;}
#elif E2D_MOTION_STAGE == 2
    if(q.motionMargin.xy!=vec2(0))scan(1u);
#else
    if(blocked||(recovered&&(q.policy.z&1u)!=0u))
    {bodyA.pose.xy+=unsafeFraction*q.motionMargin.xy;scan(2u);}
#endif
#if E2D_MOTION_STAGE < 3
    outputWords[stateAt+6u]=uvec4(floatBitsToUint(recovery),rejectedCount,(recovered?1u:0u)|(converged?2u:0u));
    outputWords[stateAt+7u]=uvec4(floatBitsToUint(vec2(safe,unsafeFraction)),motionShape,blocked?1u:0u);
    return;
#endif
    Result result=Result(uvec4(0),uvec4(0),uvec4(0),uvec4(0),vec4(0),vec4(0),vec4(0),vec4(0));
#else
    [[dont_unroll]] for(uint attempt=0u;attempt<4u;attempt++)
    {
        vec2 before=recovery;scan(0u);if(before==recovery)break;bodyA.pose.xy=q.pose.xy+recovery;
    }
    if(q.motionMargin.xy!=vec2(0))scan(1u);
    Result result=Result(uvec4(0),uvec4(0),uvec4(0),uvec4(0),vec4(0),vec4(0),vec4(0),vec4(0));
    if(blocked||(recovered&&(q.policy.z&1u)!=0u))
    {bodyA.pose.xy=q.pose.xy+recovery+unsafeFraction*q.motionMargin.xy;scan(2u);}
#endif
    if(rested)result=restResult;else safe=unsafeFraction=1;
    result.travelRemainder=vec4(recovery+safe*q.motionMargin.xy,(1-safe)*q.motionMargin.xy);
    result.fractions=vec4(safe,unsafeFraction,recovery);
    if(!finiteField(result.travelRemainder)||!finiteField(result.pointNormal)||!finiteField(result.velocityDepth)||!finiteField(result.fractions)){fail();return;}
    uint at=i*8u;
    WRITE_UINT(at,result.identity);WRITE_UINT(at+1u,result.shapes);
    WRITE_UINT(at+2u,result.owner);WRITE_UINT(at+3u,result.objectID);
    WRITE_FLOAT(at+4u,result.pointNormal);WRITE_FLOAT(at+5u,result.velocityDepth);
    WRITE_FLOAT(at+6u,result.travelRemainder);WRITE_FLOAT(at+7u,result.fractions);
}
