#include "PhysicsPairHash.inc.glsl"
struct OneWayState { uvec4 pair; uvec4 pieces; uvec4 revisions; };
layout(std430,set=0,binding=ONE_WAY_STATE_BINDING) readonly buffer OneWayRecords { OneWayState oneWayRecords[]; };
layout(std430,set=0,binding=ONE_WAY_TABLE_BINDING) readonly buffer OneWayTable { uint oneWayTable[]; };
uvec2 oneWayInfo;
uint oneWayHash(uvec4 pair,uvec2 pieces)
{ return pairHash(pair.xy)^pairHash(pair.zw)^pairHash(pieces); }
uint previousOneWay(uvec4 pair,uvec2 pieces,uvec4 revisions)
{
    if(oneWayInfo.x==0u)return 0u;
    uint at=oneWayHash(pair,pieces)&(oneWayInfo.y-1u);
    for(uint probe=0u;probe<oneWayInfo.y;probe++)
    {
        uint index=oneWayTable[at];if(index==0xffffffffu)return 0u;
        if(index>=oneWayInfo.x){fail();return 1u;}
        OneWayState old=oneWayRecords[index];
        if(old.pair==pair&&old.pieces.xy==pieces)return old.revisions==revisions?old.pieces.z:0u;
        at=(at+1u)&(oneWayInfo.y-1u);
    }
    fail();return 1u;
}
bool facesOneWay(Shape a,Shape b,ResidentBody ba,ResidentBody bb,vec2 normal)
{
    return ((a.policy.w&4u)==0u||dot2(normal,rotate(ba.pose.zw,rotate(a.pose.zw,a.oneWay.xy))) < -1e-6)
        && ((b.policy.w&4u)==0u||dot2(normal,rotate(bb.pose.zw,rotate(b.pose.zw,b.oneWay.xy))) > 1e-6);
}
