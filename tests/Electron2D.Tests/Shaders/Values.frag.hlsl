cbuffer Values : register(b0, space3) {
    float3 rgb;
    float tail;
    float3 rgbArray[2];
    float4 rectangle;
    float4 rectangles[2];
    uint2 pair;
    uint4 quad;
    uint2 pairs[2];
    uint4 quads[2];
};
float4 main() : SV_Target0 {
    bool valid = tail == 0.75 && all(rectangle == float4(0.125, 0.25, 0.5, 1))
        && all(rectangles[0] == float4(1, 2, 3, 4)) && all(rectangles[1] == float4(-1, -2, -3, -4))
        && all(pair == uint2(0x80000000u, 0xffffffffu))
        && all(quad == uint4(0xffffffffu, 0x80000000u, 0x7fffffffu, 123456789u))
        && all(pairs[0] == pair) && all(pairs[1] == uint2(0, 1))
        && all(quads[0] == quad) && all(quads[1] == uint4(1, 2, 3, 4));
    return valid ? float4(rgb * rgbArray[0] + rgbArray[1], 1) : float4(1, 0, 1, 1);
}
