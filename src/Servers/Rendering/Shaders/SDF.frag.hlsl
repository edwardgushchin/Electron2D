StructuredBuffer<float4> lighting : register(t0, space2);
cbuffer Field : register(b0, space3) { float4 region; float4 dimensions; uint4 counts; };
float4 main(float4 position : SV_Position) : SV_Target0 {
    float2 samplePosition = position.xy * region.zw / dimensions.xy + region.xy;
    float closest = length(region.zw) * 2;
    bool inside = false, groupInside = false;
    int current = 0;
    for (uint i = 0; i < counts.y; i++) {
        float4 edge = lighting[counts.x * 9 + i * 2];
        float4 meta = lighting[counts.x * 9 + i * 2 + 1];
        int group = asint(meta.w);
        if (group != current) { if (current > 0) inside = inside || groupInside; current = group; groupInside = false; }
        if (group == 0) continue;
        float2 start = edge.xy, end = edge.zw, delta = end - start;
        float factor = saturate(dot(samplePosition - start, delta) / max(dot(delta, delta), 0.000001));
        closest = min(closest, length(samplePosition - start - delta * factor));
        if (group > 0 && (start.y > samplePosition.y) != (end.y > samplePosition.y) &&
            samplePosition.x < (end.x - start.x) * (samplePosition.y - start.y) / (end.y - start.y) + start.x) groupInside = !groupInside;
    }
    if (current > 0) inside = inside || groupInside;
    return float4(inside ? -closest : closest, 0, 0, 1);
}
