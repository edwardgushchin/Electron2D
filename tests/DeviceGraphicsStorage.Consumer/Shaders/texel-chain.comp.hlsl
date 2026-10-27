[[vk::binding(7,3)]] [[vk::image_format("rgba8")]]RWBuffer<float4> texels;
[[vk::binding(2,4)]]RWBuffer<uint> counter;
[numthreads(1,1,1)]void main(){texels[3]=float4(1,0,0,1);counter[2]=counter[0]+counter[1];}
