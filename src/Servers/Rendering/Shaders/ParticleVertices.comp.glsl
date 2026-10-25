#version 450
layout(local_size_x = 64) in;
struct Particle { vec4 x; vec4 y; vec4 color; vec4 custom; vec4 velocityAge; uvec4 state; };
layout(std430, set = 0, binding = 0) readonly buffer Current { Particle current[]; };
layout(std430, set = 0, binding = 1) readonly buffer Previous { Particle previous[]; };
layout(std430, set = 0, binding = 2) readonly buffer HistoryData { Particle historyData[]; };
layout(std430, set = 0, binding = 3) readonly buffer Poses { vec4 poses[]; };
layout(std430, set = 0, binding = 4) readonly buffer Geometry { uint geometry[]; };
layout(std430, set = 0, binding = 5) readonly buffer Bones { uint bones[]; };
layout(std430, set = 0, binding = 6) readonly buffer Ordering { uvec2 ordering[]; };
layout(std430, set = 1, binding = 0) writeonly buffer Vertices { uint vertices[]; };
layout(std430, set = 1, binding = 1) writeonly buffer Draws { uint draws[]; };
#ifdef E2D_MODEL_MATRICES
layout(std430, set = 1, binding = 2) writeonly buffer Models { vec4 models[]; };
#endif
layout(std140, set = 2, binding = 0) uniform Parameters {
    vec4 canvasX; vec4 canvasY; vec4 modulation;
    uvec4 counts; // amount, vertices per mesh, source word offset, HDR/procedural/skin/group flags
    uvec4 history; // frame capacity, next frame, written frames, pose count
    uvec4 options; // align, axis, target width, row width
    vec4 timing; // interpolation fraction, history steps per second, trail seconds, target height
    vec4 sheet;
};
bool finiteValue(float value) { return !isnan(value) && !isinf(value); }
vec2 applyPose(vec4 x, vec4 y, vec2 p) { return vec2(dot(x.xy, p) + x.w, dot(y.xy, p) + y.w); }
void orient(inout vec4 x, inout vec4 y, Particle particle)
{
    if (options.x == 0) return;
    vec2 velocity = particle.velocityAge.xy;
    if (dot(velocity, velocity) > 0.00000001)
    {
        vec2 direction = normalize(velocity); float sx = length(vec2(x.x, y.x)), sy = length(vec2(x.y, y.y));
        if (options.y == 0) { x.xy = vec2(direction.x * sx, -direction.y * sy); y.xy = vec2(direction.y * sx, direction.x * sy); }
        else { x.xy = vec2(direction.y * sx, direction.x * sy); y.xy = vec2(-direction.x * sx, direction.y * sy); }
    }

}
void main()
{
    uint index = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * options.w;
    if (index >= counts.x * counts.y) return;
    uint instance = index / counts.y, vertex = index % counts.y;
    uint particleIndex = ordering[instance].y; bool alive = particleIndex < counts.x && (current[particleIndex].state.x & 1u) != 0;
    Particle particle = current[min(particleIndex, counts.x - 1)];
    if (vertex == 0)
    {
        draws[instance * 4] = alive ? counts.y : 0; draws[instance * 4 + 1] = 1; draws[instance * 4 + 2] = instance * counts.y; draws[instance * 4 + 3] = instance;
    }
    uint source = counts.z + vertex * 43, outputBase = index * 43;
    for (uint word = 0; word < 43; word++) vertices[outputBase + word] = geometry[source + word];
    vec4 x = particle.x, y = particle.y, color = particle.color, custom = particle.custom;
    if (timing.x < 1 && previous[particleIndex].state.y == particle.state.y && (previous[particleIndex].state.x & 1u) != 0)
    {
        x = mix(previous[particleIndex].x, x, timing.x); y = mix(previous[particleIndex].y, y, timing.x);
        color = mix(previous[particleIndex].color, color, timing.x); custom = mix(previous[particleIndex].custom, custom, timing.x);
    }
    orient(x, y, particle);
#ifdef E2D_MODEL_MATRICES
    if (vertex == 0) { models[instance*2]=vec4(x.x,y.x,x.y,y.y); models[instance*2+1]=vec4(x.w,y.w,0,0); }
#endif
    vec2 position = vec2(uintBitsToFloat(geometry[source]), uintBitsToFloat(geometry[source + 1]));
    vec2 transformed = applyPose(x, y, position);
    vec2 basisX = vec2(x.x, y.x), basisY = vec2(x.y, y.y);
    if ((counts.w & 4u) != 0 && history.w > 1 && history.z != 0)
    {
        uint original = geometry[source + 41]; vec2 sum = vec2(0); vec2 bx = vec2(0), by = vec2(0); vec4 trailColor = vec4(0), trailCustom = vec4(0);
        for (uint slot = 0; slot < 8; slot++)
        {
            float weight = uintBitsToFloat(bones[original * 16 + 8 + slot]); if (weight == 0) continue;
            uint bone = bones[original * 16 + slot];
            float delay = float(bone) / float(history.w - 1) * timing.z * timing.y;
            float sampleAge = clamp(delay + 1.0 - timing.x, 0.0, float(history.z - 1));
            uint ago = uint(sampleAge); uint frame = (history.y + history.x - 1 - ago) % history.x;
            uint olderFrame = (history.y + history.x - 1 - min(ago + 1, history.z - 1)) % history.x;
            Particle tail = historyData[frame * counts.x + particleIndex], older = historyData[olderFrame * counts.x + particleIndex];
            if (tail.state.y != particle.state.y || (tail.state.x & 1u) == 0) tail = particle;
            if (older.state.y != particle.state.y || (older.state.x & 1u) == 0) older = tail;
            float fraction = fract(sampleAge); vec4 tx = mix(tail.x, older.x, fraction), ty = mix(tail.y, older.y, fraction);
            tail.velocityAge = mix(tail.velocityAge, older.velocityAge, fraction); orient(tx, ty, tail);
            trailColor += mix(tail.color, older.color, fraction) * weight; trailCustom += mix(tail.custom, older.custom, fraction) * weight;
            vec4 px = poses[bone * 2], py = poses[bone * 2 + 1];
            sum += applyPose(tx, ty, applyPose(px, py, position)) * weight;
            bx += vec2(tx.x * px.x + tx.y * py.x, ty.x * px.x + ty.y * py.x) * weight;
            by += vec2(tx.x * px.y + tx.y * py.y, ty.x * px.y + ty.y * py.y) * weight;
        }
        transformed = sum; basisX = bx; basisY = by; color = trailColor; custom = trailCustom;
    }
    transformed = applyPose(canvasX, canvasY, transformed);
    if ((counts.w & 1u) != 0) color.rgb = mix(color.rgb / 12.92, pow(max((color.rgb + .055) / 1.055, vec3(0)), vec3(2.4)), greaterThan(color.rgb, vec3(.04045)));
    color *= modulation * vec4(uintBitsToFloat(geometry[source + 2]), uintBitsToFloat(geometry[source + 3]), uintBitsToFloat(geometry[source + 4]), uintBitsToFloat(geometry[source + 5]));
    if ((counts.w & 8u) != 0) color.rgb *= color.a;
    if (!alive || !finiteValue(transformed.x) || !finiteValue(transformed.y) || any(isnan(color)) || any(isinf(color))) { transformed = vec2(0); color = vec4(0); if (vertex == 0) draws[instance * 4] = 0; }
    vertices[outputBase + 42] = particleIndex;
    vertices[outputBase] = floatBitsToUint(transformed.x); vertices[outputBase + 1] = floatBitsToUint(transformed.y);
    for (uint lane = 0; lane < 4; lane++) { vertices[outputBase + 2 + lane] = floatBitsToUint(color[lane]); vertices[outputBase + 8 + lane] = floatBitsToUint(custom[lane]); }
    if ((counts.w & 2u) != 0)
    {
        vec2 bx = vec2(dot(canvasX.xy, basisX), dot(canvasY.xy, basisX)); vec2 by = vec2(dot(canvasX.xy, basisY), dot(canvasY.xy, basisY));
        vertices[outputBase + 37] = floatBitsToUint(bx.x); vertices[outputBase + 38] = floatBitsToUint(bx.y); vertices[outputBase + 39] = floatBitsToUint(by.x); vertices[outputBase + 40] = floatBitsToUint(by.y);
    }
    if (sheet.w != 0 && finiteValue(custom.z))
    {
        float frames = sheet.x * sheet.y; float frame = floor(custom.z * frames); frame = sheet.z != 0 ? mod(frame, frames) : clamp(frame, 0, frames - 1);
        vec2 uv = vec2(uintBitsToFloat(geometry[source + 6]), uintBitsToFloat(geometry[source + 7])); uv = (uv + vec2(mod(frame, sheet.x), floor(frame / sheet.x))) / sheet.xy;
        vertices[outputBase + 6] = floatBitsToUint(uv.x); vertices[outputBase + 7] = floatBitsToUint(uv.y);
    }
    if ((counts.w & 8u) != 0)
    {
        vec2 uv = transformed / vec2(float(options.z), timing.w);
        vertices[outputBase + 6] = floatBitsToUint(uv.x); vertices[outputBase + 7] = floatBitsToUint(uv.y);
    }
}
