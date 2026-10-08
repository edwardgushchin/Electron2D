RWStructuredBuffer<float> values : register(u7, space3);
cbuffer Settings : register(b4, space2) { float4 settings; }
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    if (id.x < (uint)settings.y) values[id.x] = values[id.x] * settings.x + settings.z;
}
