Texture2D<float4> first : register(t7,space3);
Texture2D<float4> second : register(t11,space4);
Texture2D<float4> fetch_only : register(t2,space6);
Texture2D<uint4> unsigned_image : register(t1,space8);
Texture2D<int4> signed_image : register(t2,space10);
Texture2DArray<float4> layers : register(t3,space7);
SamplerState point_filter : register(s5,space2);
SamplerState linear_filter : register(s13,space5);
RWStructuredBuffer<float4> values : register(u4,space9);
float4 read_image(Texture2D<float4> image, SamplerState state, float2 uv) { return image.SampleLevel(state,uv,0); }
float4 choose_image(Texture2D<float4> a,Texture2D<float4> b,SamplerState x,SamplerState y,bool choose) { if(choose) return read_image(a,x,float2(.5,.5)); return read_image(b,y,float2(.5,.5)); }
[numthreads(1,1,1)] void main() {
 values[0]=read_image(first,point_filter,float2(.75,.25));
 values[1]=read_image(second,point_filter,float2(.75,.25));
 values[2]=read_image(first,linear_filter,float2(.5,.5));
 values[3]=fetch_only.Load(int3(0,0,0));
 uint w,h,a,b; first.GetDimensions(w,h); fetch_only.GetDimensions(a,b); values[4]=float4(w,h,a,b);
 values[5]=layers.SampleLevel(point_filter,float3(.5,.5,1),0);
 uint x,y,z; layers.GetDimensions(x,y,z); values[6]=float4(x,y,z,0);
 values[7]=choose_image(first,second,linear_filter,point_filter,values[2].r>.75);
 values[8]=float4(unsigned_image.SampleLevel(point_filter,float2(.5,.5),0));
 values[9]=float4(signed_image.SampleLevel(point_filter,float2(.5,.5),0));
}
