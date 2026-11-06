StructuredBuffer<float4> lighting : register(t0, space2);
cbuffer Shadow : register(b0, space3) { uint4 counts; float4 viewport; };
float cross2(float2 a, float2 b) { return a.x * b.y - a.y * b.x; }
float4 main(float4 position : SV_Position) : SV_Target0 {
    uint first = counts.w * 8;
    float4 kind = lighting[first];
    float4 origin = lighting[first + 2], parameters = lighting[first + 5];
    float4 membership = lighting[first + 6], range = lighting[first + 7];
    float2 start, ray;
    float lower = 0, upper = 3.402823466e38;
    bool directional = kind.w != 0;
    if (directional) {
        float2 center = viewport.xy * 0.5;
        ray = origin.zw;
        float2 perpendicular = float2(-ray.y, ray.x);
        float halfWidth = max(length(viewport.xy) * 0.5, 0.000001);
        float edgeDistance = dot(abs(ray), center);
        lower = dot(center, ray) - edgeDistance - max(0, parameters.z);
        upper = dot(center, ray) + edgeDistance;
        start = center + perpendicular * ((position.x / counts.z - 0.5) * halfWidth * 2);
        start -= ray * dot(start, ray);
    } else {
        float angle = (position.x / counts.z - 0.5) * 6.28318530718;
        ray = float2(cos(angle), sin(angle));
        start = origin.xy;
        lower = 0.00001;
    }
    float closest = 3.402823466e38;
    for (uint i = 0; i < counts.y; i++) {
        float4 edge = lighting[counts.x * 9 + i * 2];
        float4 meta = lighting[counts.x * 9 + i * 2 + 1];
        int layer = asint(meta.y);
        if ((asuint(meta.x) & asuint(membership.y)) == 0 || layer < asint(range.x) || layer > asint(range.y)) continue;
        float2 segment = edge.zw - edge.xy;
        float determinant = cross2(ray, segment);
        if (abs(determinant) < 0.000001) continue;
        // Visibility rays run toward the source; depth rays run away from it.
        if (meta.z == 1 && determinant > 0 || meta.z == 2 && determinant < 0) continue;
        float2 delta = edge.xy - start;
        float t = cross2(delta, segment) / determinant;
        float u = cross2(delta, ray) / determinant;
        if (t >= lower && t <= upper && u >= 0 && u <= 1) closest = min(closest, t);
    }
    return float4(closest, 0, 0, 1);
}
