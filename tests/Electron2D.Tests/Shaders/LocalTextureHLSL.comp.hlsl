Texture2D<float4> source_image : register(t7, space3);
SamplerState source_sampler : register(s7, space3);
RWTexture2D<float4> destination : register(u2, space5);
RWStructuredBuffer<uint> counts : register(u4, space9);
[numthreads(8,8,1)]
void main(uint3 id : SV_DispatchThreadID) {
    uint width,height; destination.GetDimensions(width,height);
    if(id.x>=width||id.y>=height) return;
    destination[id.xy]=source_image.SampleLevel(source_sampler,(float2(id.xy)+.5)/float2(width,height),0)+float4(.25,0,0,0);
    InterlockedAdd(counts[0],1);
}
