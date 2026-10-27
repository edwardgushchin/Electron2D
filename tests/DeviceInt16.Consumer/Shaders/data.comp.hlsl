struct Pair { int16_t x; uint16_t y; };
[[vk::binding(0,0)]] StructuredBuffer<Pair> sourceData;
[[vk::binding(1,0)]] RWStructuredBuffer<uint> targetData;
[[vk::binding(0,1)]] cbuffer Parameters { uint16_t scale; uint16_t delta; }
[[vk::constant_id(0)]] const int factorSource=-7;
[[vk::constant_id(1)]] const uint limitSource=5;
struct Push {
 [[vk::offset(0)]] int16_t a; [[vk::offset(2)]] uint16_t b; [[vk::offset(4)]] vector<uint16_t,2> pair;
 [[vk::offset(8)]] int16_t values[3]; [[vk::offset(16)]] Pair nested; [[vk::offset(126)]] uint16_t last;
}; [[vk::push_constant]] ConstantBuffer<Push> pc;
int16_t pairValue(Pair p){return int16_t(p.x+int16_t(p.y));}
uint16_t vectorValue(vector<uint16_t,2> v,uint i){return v[i];}
[numthreads(1,1,1)] void main(uint3 id:SV_DispatchThreadID){
 uint i=id.x,j=i*16;int16_t x=sourceData[i].x;uint16_t y=sourceData[i].y;
 targetData[j]=uint(int(int16_t(x+int16_t(factorSource))));targetData[j+1]=uint(uint16_t(y+uint16_t(limitSource)));
 targetData[j+2]=uint(int(int16_t(x/int16_t(3))));targetData[j+3]=uint(int(int16_t(int16_t(y&uint16_t(32767))%int16_t(3))));
 targetData[j+4]=uint(int(int16_t(x>>3)));targetData[j+5]=uint(uint16_t(y<<3));
 targetData[j+6]=uint(y^uint16_t(limitSource));targetData[j+7]=x<int16_t(0)?1:0;
 targetData[j+8]=uint(int(pc.a));targetData[j+9]=uint(pc.b);targetData[j+10]=uint(vectorValue(pc.pair,i&1));
 targetData[j+11]=uint(int(pc.values[i%3]));targetData[j+12]=uint(int(pairValue(pc.nested)));targetData[j+13]=uint(pc.last);
 targetData[j+14]=uint(int(int16_t(x+int16_t(scale)-int16_t(2))));targetData[j+15]=uint(uint16_t(y+delta));
}
