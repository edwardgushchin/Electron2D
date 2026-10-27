[[vk::binding(0,0)]] RWStructuredBuffer<uint64_t> cells;
[[vk::binding(1,0)]] RWStructuredBuffer<uint64_t> target;
struct Push { uint64_t base; }; [[vk::push_constant]] ConstantBuffer<Push> pc;
[numthreads(64,1,1)] void main(uint3 id:SV_GroupThreadID){
 uint64_t old;
 if(id.x==0){ InterlockedExchange(cells[0],pc.base,old); }
 DeviceMemoryBarrierWithGroupSync();
 InterlockedAdd(cells[0],uint64_t(1),old);target[64+id.x]=old;
 DeviceMemoryBarrierWithGroupSync();
 if(id.x==0){target[15]=cells[0];InterlockedCompareExchange(cells[0],pc.base+64,pc.base+71,old);target[0]=old;target[1]=cells[0];}
}
