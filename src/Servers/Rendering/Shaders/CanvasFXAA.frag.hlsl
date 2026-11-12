Texture2D<float4> sourceImage : register(t0, space2);
SamplerState sourceSampler : register(s0, space2);
cbuffer FXAA : register(b0, space3) { float4 parameters; };

float4 ReadPixel(float2 uv)
{
    return sourceImage.SampleLevel(sourceSampler, uv, 0);
}

float EdgeSignal(float4 pixel)
{
    float luma = dot(max(pixel.rgb, 0.0), float3(0.2126, 0.7152, 0.0722));
    if (parameters.z != 0.0) luma = sqrt(luma);
    return luma + 0.25 * pixel.a;
}

float4 main([[vk::location(1)]] float2 uv : TEXCOORD1) : SV_Target0
{
    float2 texel = parameters.xy;
    float4 center = ReadPixel(uv);
    float middle = EdgeSignal(center);
    float nw = EdgeSignal(ReadPixel(uv + texel * float2(-1, -1)));
    float ne = EdgeSignal(ReadPixel(uv + texel * float2( 1, -1)));
    float sw = EdgeSignal(ReadPixel(uv + texel * float2(-1,  1)));
    float se = EdgeSignal(ReadPixel(uv + texel * float2( 1,  1)));
    float lo = min(middle, min(min(nw, ne), min(sw, se)));
    float hi = max(middle, max(max(nw, ne), max(sw, se)));
    if (hi - lo < max(0.03125, hi * 0.125)) return center;

    // The filter follows the edge tangent inferred from the luminance gradient.
    float2 direction = float2(sw + se - nw - ne, nw + sw - ne - se);
    float reduction = max((nw + ne + sw + se) / 32.0, 1.0 / 128.0);
    direction /= min(abs(direction.x), abs(direction.y)) + reduction;
    direction = clamp(direction, -8.0, 8.0) * texel;
    float4 inner = (ReadPixel(uv - direction / 6.0) + ReadPixel(uv + direction / 6.0)) * 0.5;
    float4 outer = inner * 0.5 + (ReadPixel(uv - direction * 0.5) + ReadPixel(uv + direction * 0.5)) * 0.25;
    float signal = EdgeSignal(outer);
    return signal < lo || signal > hi ? inner : outer;
}
