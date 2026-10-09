#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
#include "PhysicsResidentGeometry.inc.glsl"
layout(local_size_x = 64) in;
#include "PhysicsResidentContact.inc.glsl"
layout(std430, set=0, binding=0) readonly buffer Bodies { ResidentBody bodies[]; };
layout(std430, set=0, binding=1) readonly buffer Vertices { vec2 vertices[]; };
layout(std430, set=0, binding=2) readonly buffer Geometries { Geometry geometries[]; };
layout(std430, set=0, binding=3) readonly buffer Shapes { Shape shapes[]; };
layout(std430, set=0, binding=4) readonly buffer Pairs { uvec4 pairs[]; };
layout(std430, set=1, binding=0) buffer Contacts { ContactPoint contacts[]; };
layout(std430, set=1, binding=1) buffer Summary { uvec2 summary; };
layout(std140, set=2, binding=0) uniform Settings { uvec4 counts; vec4 tolerances; uvec4 history; uvec4 historyOutput; };
const float epsilon=1.1920928955078125e-7;
#include "PhysicsCollisionMath.inc.glsl"
uvec4 activePair;
ResidentBody bodyA,bodyB;
float contactLimit;
bool sensor;
void fail() { atomicOr(summary.x,1u); }
#include "PhysicsResidentCollision.inc.glsl"
#define ONE_WAY_STATE_BINDING 5
#define ONE_WAY_TABLE_BINDING 6
#include "PhysicsResidentOneWay.inc.glsl"
layout(std430,set=1,binding=2) buffer OneWayNext { OneWayState oneWayNext[]; };
layout(std430,set=1,binding=3) buffer OneWayNextTable { uint oneWayNextTable[]; };
layout(std430,set=1,binding=4) buffer OneWaySummary { uvec2 oneWaySummary; };
uvec2 checkedPieces=uvec2(0xffffffffu);
uvec4 oneWayRevisions;
uint pieceDecision;
Shape shapeA,shapeB;
bool oneWay;
void emitPoint(vec2 a,vec2 b,vec2 normal,uvec4 features)
{
    float separation=dot2(b-a,normal);
    if(separation>contactLimit+tolerances.z)return;
    vec2 localA=inverseRotate(bodyA.pose.zw,a);
    vec2 localB=inverseRotate(bodyB.pose.zw,b-(bodyB.pose.xy-bodyA.pose.xy));
    if(!finite2(localA)||!finite2(localB)||!finite2(normal)||!finite2(vec2(separation))||abs(dot2(normal,normal)-1)>0.001)
    {fail();return;}
    if(oneWay)
    {
        if(checkedPieces!=features.zw)
        {
            checkedPieces=features.zw;
            pieceDecision=previousOneWay(activePair,checkedPieces,oneWayRevisions);
            if(pieceDecision==0u)pieceDecision=facesOneWay(shapeA,shapeB,bodyA,bodyB,normal)?2u:1u;
            uint record=atomicAdd(oneWaySummary.x,1u);
            if(record==0xffffffffu){fail();return;}
            if(record<history.w)oneWayNext[record]=OneWayState(activePair,uvec4(checkedPieces,pieceDecision,0),oneWayRevisions);
        }
        if(pieceDecision==1u)return;
    }
    uint at=atomicAdd(summary.y,1u);
    if(at==0xffffffffu){fail();return;}
    if(at<counts.y)contacts[at]=ContactPoint(activePair,features,vec4(normal,separation,sensor?1:0),vec4(localA,localB));
}
#include "PhysicsResidentManifold.inc.glsl"
void rayContact(Shape ra,Geometry rg,ResidentBody rb,Shape other,Geometry og,ResidentBody ob,bool flip)
{
    Hull ray=hull(ra,rg,rb,0u);vec2 from=vertex(ray,0u),d=vertex(ray,1u)-from;
    if(d==vec2(0))return;
    vec2 axis=normalized(d);d+=axis*contactLimit;
    float nearest=2;vec2 hitNormal=vec2(0);uint hitFeature=0u,hitPiece=0u;
    uint pieceCount=og.data.z==5u?og.data.y/2u:1u;
    for(uint i=0u;i<pieceCount;i++)
    {
        float t;vec2 n;uint f;
        if(rayHit(hull(other,og,ob,i),from,d,t,n,f)&&t<nearest){nearest=t;hitNormal=n;hitFeature=f;hitPiece=i;}
    }
    if(nearest>1)return;
    vec2 a=from+d;float depth=(1-nearest)*length(d);
    vec2 n=rg.parameters.y!=0?-hitNormal:axis,b=a-depth*n;
    if(flip)emitPoint(b,a,-n,uvec4(hitFeature,2u,hitPiece,0u));
    else emitPoint(a,b,n,uvec4(2u,hitFeature,0u,hitPiece));
}
void main()
{
    uint index=gl_GlobalInvocationID.x;
    if(history.x==1u){if(index<historyOutput.x)oneWayNextTable[index]=0xffffffffu;return;}
    if(history.x==2u)
    {
        if(index>=min(oneWaySummary.x,history.w))return;
        OneWayState current=oneWayNext[index];uint at=oneWayHash(current.pair,current.pieces.xy)&(historyOutput.x-1u);
        for(uint probe=0u;probe<historyOutput.x;probe++)
        {
            if(atomicCompSwap(oneWayNextTable[at],0xffffffffu,index)==0xffffffffu)return;
            at=(at+1u)&(historyOutput.x-1u);
        }
        fail();return;
    }
    if(index>=counts.x)return;
    oneWayInfo=history.yz;
    activePair=pairs[index];
    if(activePair.x>=counts.z||activePair.y>=counts.z){fail();return;}
    Shape sa=shapes[activePair.x],sb=shapes[activePair.y];
    if(sa.policy.x!=activePair.z||sb.policy.x!=activePair.w||(sa.policy.w&1u)==0u||(sb.policy.w&1u)==0u||sa.owner.x>=counts.w||sb.owner.x>=counts.w){fail();return;}
    bodyA=bodies[sa.owner.x];bodyB=bodies[sb.owner.x];
    Geometry ga=geometries[sa.owner.z],gb=geometries[sb.owner.z];
    if(bodyA.flags.x!=sa.owner.y||bodyB.flags.x!=sb.owner.y||bodyA.flags.w==0u||bodyB.flags.w==0u||ga.data.w!=sa.owner.w||gb.data.w!=sb.owner.w){fail();return;}
    if(ga.data.y==0u||gb.data.y==0u)return;
    sensor=((sa.policy.w|sb.policy.w)&2u)!=0u;contactLimit=sensor?tolerances.w:tolerances.x;
    shapeA=sa;shapeB=sb;oneWay=!sensor&&((sa.policy.w|sb.policy.w)&4u)!=0u;
    oneWayRevisions=uvec4(sa.revision.x,sb.revision.x,ga.revision,gb.revision);
    if((!sensor&&ga.data.z==5u&&gb.data.z==5u)||(ga.data.z==6u&&gb.data.z==6u))return;
    if(sensor&&(ga.data.z==6u||gb.data.z==6u))contactLimit=0;
    if(ga.data.z==6u){rayContact(sa,ga,bodyA,sb,gb,bodyB,false);return;}
    if(gb.data.z==6u){rayContact(sb,gb,bodyB,sa,ga,bodyA,true);return;}
    uint na=ga.data.z==5u?ga.data.y/2u:1u,nb=gb.data.z==5u?gb.data.y/2u:1u;
    for(uint i=0u;i<na;i++)for(uint j=0u;j<nb;j++)ordinary(hull(sa,ga,bodyA,i),hull(sb,gb,bodyB,j),uvec2(i,j));
}
