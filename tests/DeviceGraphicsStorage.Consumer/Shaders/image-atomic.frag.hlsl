[[vk::binding(2,0)]] RWTexture2D<uint> U;
[[vk::binding(3,0)]] RWTexture2D<int> I;
[[vk::binding(4,0)]] RWStructuredBuffer<uint2> result;
[[vk::constant_id(0)]] const uint factor=1;
void atomics(int2 p,uint index){
 uint u=0,oldU;InterlockedExchange(U[p],40,oldU);u+=oldU;InterlockedAdd(U[p],2,oldU);u+=oldU;InterlockedMin(U[p],10,oldU);u+=oldU;InterlockedMax(U[p],20,oldU);u+=oldU;InterlockedAnd(U[p],15,oldU);u+=oldU;InterlockedOr(U[p],8,oldU);u+=oldU;InterlockedXor(U[p],3,oldU);u+=oldU;InterlockedCompareExchange(U[p],15,100,oldU);u+=oldU;InterlockedCompareExchange(U[p],15,999,oldU);u+=oldU;InterlockedAdd(U[p],1,oldU);u+=oldU;
 int i=0,oldI;InterlockedExchange(I[p],-40,oldI);i+=oldI;InterlockedAdd(I[p],2,oldI);i+=oldI;InterlockedMin(I[p],-50,oldI);i+=oldI;InterlockedMax(I[p],-20,oldI);i+=oldI;InterlockedAnd(I[p],15,oldI);i+=oldI;InterlockedOr(I[p],16,oldI);i+=oldI;InterlockedXor(I[p],3,oldI);i+=oldI;InterlockedCompareExchange(I[p],31,-100,oldI);i+=oldI;InterlockedCompareExchange(I[p],31,999,oldI);i+=oldI;InterlockedAdd(I[p],1,oldI);i+=oldI;
 uint w,h,iw,ih;U.GetDimensions(w,h);I.GetDimensions(iw,ih);result[index]=w==4&&h==4&&iw==4&&ih==4&&U[p]==101&&I[p]==-99 ? uint2(u,uint(i))*factor : uint2(0,0);
}
float4 main(float4 position:SV_Position):SV_Target0{int2 p=int2(position.xy);atomics(p,4+uint(p.x+4*p.y));return 1;}
