#include "HalfTypes.hlsli"
struct Pair { CompactScalar x; CompactScalar y; };
[[vk::binding(0,0)]] StructuredBuffer<Pair> sourceData;
[[vk::binding(1,0)]] RWStructuredBuffer<CompactScalar> targetData;
[[vk::binding(0,1)]] cbuffer Parameters { vector<CompactScalar,2> scale; CompactScalar delta; }
[[vk::constant_id(0)]] const float factorSource=0.75;
static const CompactScalar factor=CompactScalar(factorSource);
[[vk::constant_id(1)]] const bool choose=true;
[[vk::constant_id(2)]] const float convertSource=0.333251953125;
struct Push {
 [[vk::offset(0)]] CompactScalar a; [[vk::offset(2)]] CompactScalar b;
 [[vk::offset(4)]] vector<CompactScalar,2> pair; [[vk::offset(8)]] CompactScalar values[3];
 [[vk::offset(16)]] CompactScalar column[8]; [[vk::offset(32)]] Pair nested; [[vk::offset(36)]] uint index;
 [[vk::offset(40)]] CompactScalar row[6]; [[vk::offset(52)]] CompactScalar tail[2]; [[vk::offset(126)]] CompactScalar last;
};[[vk::push_constant]] ConstantBuffer<Push> pc;
CompactScalar pairValue(Pair p){return p.x+p.y;}
CompactScalar vectorValue(vector<CompactScalar,2> v,uint i){return v[i];}
[numthreads(1,1,1)] void main(uint3 id:SV_DispatchThreadID){
 uint i=id.x,j=i*16;CompactScalar x=sourceData[i].x,y=sourceData[i].y;
 targetData[j]=x+factor+pc.a;targetData[j+1]=y*scale.x+vectorValue(pc.pair,i&1);
 targetData[j+2]=pc.column[pc.index*4+i%3];targetData[j+3]=pc.row[(i%3)*2+pc.index];
 targetData[j+4]=pc.values[i%3];targetData[j+5]=pairValue(pc.nested);targetData[j+6]=pc.tail[i&1];targetData[j+7]=pc.last;
 targetData[j+8]=x;targetData[j+9]=y;targetData[j+10]=factor;targetData[j+11]=choose?x:y;
 targetData[j+12]=scale.y+delta;targetData[j+13]=CompactScalar(convertSource);targetData[j+14]=pc.a;targetData[j+15]=pc.b;
}
