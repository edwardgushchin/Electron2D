#define ELECTRON2D_PARTICLE_SUBEMISSION
#include "Includes/electron2d/particles.glslinc"
#ifdef ELECTRON2D_PARTICLE_MOTION
#include "ParticleNoise.inc.glsl"
#endif
layout(local_size_x = 64) in;
#ifdef ELECTRON2D_PARTICLE_MOTION
layout(set = 0, binding = 0) uniform sampler2D Points;
layout(set = 0, binding = 1) uniform sampler2D Normals;
layout(set = 0, binding = 2) uniform sampler2D Curve2;
layout(set = 0, binding = 3) uniform sampler2D Curve3;
layout(set = 0, binding = 4) uniform sampler2D Curve4;
layout(set = 0, binding = 5) uniform sampler2D Curve5;
layout(set = 0, binding = 6) uniform sampler2D Curve6;
layout(set = 0, binding = 7) uniform sampler2D Curve12;
layout(set = 0, binding = 8) uniform sampler2D Curve15;
layout(set = 0, binding = 9) uniform sampler2D Curve16;
layout(set = 0, binding = 10) uniform sampler2D Curve22;
layout(set = 0, binding = 11) uniform sampler2D SDF_TEXTURE;
#ifdef ELECTRON2D_CPU_PARTICLE_CURVES
layout(set = 0, binding = 12) uniform sampler2D Curve0;
#endif
#else
layout(set = 0, binding = 0) uniform sampler2D Points;
layout(set = 0, binding = 1) uniform sampler2D PointColors;
layout(set = 0, binding = 2) uniform sampler2D Curve1;
layout(set = 0, binding = 3) uniform sampler2D Curve7;
layout(set = 0, binding = 4) uniform sampler2D Curve8;
layout(set = 0, binding = 5) uniform sampler2D Curve9;
layout(set = 0, binding = 6) uniform sampler2D Curve10;
layout(set = 0, binding = 7) uniform sampler2D Curve11;
layout(set = 0, binding = 8) uniform sampler2D Curve17;
layout(set = 0, binding = 9) uniform sampler2D Curve18;
layout(set = 0, binding = 10) uniform sampler2D Curve19;
layout(set = 0, binding = 11) uniform sampler2D Curve20;
layout(set = 0, binding = 12) uniform sampler2D Curve21;
#ifdef ELECTRON2D_CPU_PARTICLE_CURVES
layout(set = 0, binding = 13) uniform sampler2D Curve23;
#endif
#endif
layout(std140, set = 2, binding = 1) uniform Parameters
{
    vec4 Ranges[18];
    vec4 Data[10];
    ivec4 SourceConfig;
    ivec4 SubConfig;
    uvec4 CurveOptions;
    float TIME;
};
const float PI = 3.14159265358979323846;
uint hashValue(uint value)
{
    value = ((value >> 16) ^ value) * 73244475u;
    value = ((value >> 16) ^ value) * 73244475u;
    return (value >> 16) ^ value;
}
float randomValue(uint seed, uint channel) { return float(hashValue(seed + channel * 747796405u) >> 8) / 16777216.0; }
float parameter(int channel, uint seed) { return mix(Ranges[channel].x, Ranges[channel].y, randomValue(seed, uint(channel))); }
bool hasCurve(int channel) { return (CurveOptions.x & (1u << uint(channel))) != 0; }
vec4 curve(int channel, float phase, vec4 fallback)
{
    if (!hasCurve(channel)) return fallback;
    switch (channel)
    {
#ifdef ELECTRON2D_PARTICLE_MOTION
#ifdef ELECTRON2D_CPU_PARTICLE_CURVES
        case 0: return textureLod(Curve0, vec2(phase), 0);
#endif
        case 2: return textureLod(Curve2, vec2(phase), 0);
        case 3: return textureLod(Curve3, vec2(phase), 0);
        case 4: return textureLod(Curve4, vec2(phase), 0);
        case 5: return textureLod(Curve5, vec2(phase), 0);
        case 6: return textureLod(Curve6, vec2(phase), 0);
        case 12: return textureLod(Curve12, vec2(phase, 0), 0);
        case 15: return textureLod(Curve15, vec2(phase), 0);
        case 16: return textureLod(Curve16, vec2(phase), 0);
        case 22: return textureLod(Curve22, vec2(phase), 0);
#else
        case 1: return textureLod(Curve1, vec2(phase), 0);
        case 7: return textureLod(Curve7, vec2(phase), 0);
        case 8: return textureLod(Curve8, vec2(phase), 0);
        case 9: return textureLod(Curve9, vec2(phase), 0);
        case 10: return textureLod(Curve10, vec2(phase), 0);
        case 11: return textureLod(Curve11, vec2(phase), 0);
        case 17: return textureLod(Curve17, vec2(phase, 0), 0);
        case 18: return textureLod(Curve18, vec2(phase), 0);
        case 19: return textureLod(Curve19, vec2(phase), 0);
        case 20: return textureLod(Curve20, vec2(phase), 0);
        case 21: return textureLod(Curve21, vec2(phase), 0);
#ifdef ELECTRON2D_CPU_PARTICLE_CURVES
        case 23: return textureLod(Curve23, vec2(phase), 0);
#endif
#endif
        default: return fallback;
    }
}
vec2 unitVector(vec2 value) { float size = length(value); return size > 0 ? value / size : vec2(0); }
vec2 emissionVector(vec2 value) { return vec2(dot(EmissionX.xy, value), dot(EmissionY.xy, value)); }
vec2 emissionPoint(vec2 value) { return emissionVector(value) + vec2(EmissionX.w, EmissionY.w); }
vec2 direction(uint seed, uint channel, float spread)
{
    float angle = (Data[0].x != 0.0 ? atan(Data[0].y, Data[0].x) : sign(Data[0].y) * 1.57079632679) + radians((randomValue(seed, channel) * 2.0 - 1.0) * spread);
    return vec2(cos(angle), sin(angle));
}
#ifdef ELECTRON2D_PARTICLE_MOTION
vec2 noiseDirection(vec2 position)
{
    vec4 coordinate = vec4(position * Data[7].y, 0, 0) + TIME * Data[6];
    vec3 value = curl_3d(coordinate, max(Data[7].x - 1.0, 0.0) * 70.0);
    if (any(isnan(value)) || any(isinf(value))) return vec2(0);
    return value.xy * mix(.9, 1.0, Data[7].x - 9.0);
}
vec2 initialPosition(uint seed, int shape, ivec2 point)
{
    vec2 value = vec2(0);
    if (shape == 1 || shape == 2)
    {
        float s = randomValue(seed, 24u) * 2.0 - 1.0;
        float angle = randomValue(seed, 25u) * 2.0 * PI;
        value = sqrt(max(0.0, 1.0 - s * s)) * Data[3].z * vec2(cos(angle), sin(angle));
        if (shape == 1) value *= randomValue(seed, 26u);
    }
    if (shape == 3) value = (vec2(randomValue(seed, 24u), randomValue(seed, 25u)) * 2.0 - 1.0) * Data[3].xy;
    if ((shape == 4 || shape == 5) && Data[9].x != 0) value = texelFetch(Points, point, 0).xy;
    if (shape == 6)
    {
        float angle = randomValue(seed, 24u) * 2.0 * PI;
        float radius = sqrt(mix(Data[4].x * Data[4].x, Data[3].w * Data[3].w, randomValue(seed, 25u)));
        value = vec2(cos(angle), sin(angle)) * radius;
    }
    value = value * Data[2].zw + Data[2].xy;
    if (Data[7].z != 0) value += noiseDirection(value) * parameter(14, seed);
    return emissionPoint(value);
}
bool collision(inout vec2 position, inout vec2 velocity, float radius, int mode)
{
    if (mode == 0) return false;
    vec2 uv = vec2(dot(SDFTransformX.xy, position) + SDFTransformX.w, dot(SDFTransformY.xy, position) + SDFTransformY.w);
    if (any(lessThanEqual(uv, vec2(0))) || any(greaterThanEqual(uv, vec2(1)))) return false;
    mat2 toCanvas = mat2(vec2(SDFTransformX.x / SDFRect.x, SDFTransformY.x / SDFRect.y), vec2(SDFTransformX.y / SDFRect.x, SDFTransformY.y / SDFRect.y));
    float radiusCanvas = length(toCanvas[1]) * radius;
    float distance = textureLod(SDF_TEXTURE, uv, 0).r;
    float depth = radiusCanvas - distance;
    if (depth <= .000001 * max(1.0, max(radiusCanvas, abs(distance)))) return false;
    if (mode >= 2) return true;
    if (abs(determinant(toCanvas)) < .00000001) return false;
    mat2 toLocal = inverse(toCanvas);
    vec2 texel = 1.0 / vec2(textureSize(SDF_TEXTURE, 0));
    vec2 gradient = vec2(textureLod(SDF_TEXTURE, uv + vec2(texel.x, 0), 0).r - textureLod(SDF_TEXTURE, uv - vec2(texel.x, 0), 0).r,
        textureLod(SDF_TEXTURE, uv + vec2(0, texel.y), 0).r - textureLod(SDF_TEXTURE, uv - vec2(0, texel.y), 0).r) / (2.0 * texel) * SDFRect.xy;
    bool fallback = dot(gradient, gradient) < .00000001;
    if (fallback)
    {
        float dx = textureLod(SDF_TEXTURE, uv + vec2(texel.x, 0), 0).r - distance;
        float dy = textureLod(SDF_TEXTURE, uv + vec2(0, texel.y), 0).r - distance;
        gradient = abs(dx) >= abs(dy) ? vec2(dx < 0 ? -1.0 : 1.0, 0) : vec2(0, dy < 0 ? -1.0 : 1.0);
    }
    vec2 displacement = toLocal * unitVector(gradient);
    vec2 normal = unitVector(displacement);
    position += displacement * depth;
    if (fallback)
    {
        vec2 correctedUV = vec2(dot(SDFTransformX.xy, position) + SDFTransformX.w, dot(SDFTransformY.xy, position) + SDFTransformY.w);
        float remaining = radiusCanvas - textureLod(SDF_TEXTURE, correctedUV, 0).r;
        if (remaining > 0) position += displacement * remaining;
    }
    float response = dot(normal, velocity);
    float bounce = Data[4].z * step(2.0 / clamp(Data[4].z + 1.0, 1.0, 2.0), abs(response));
    velocity = (velocity - normal * response) * (1.0 - clamp(Data[4].y, 0.0, 1.0)) - normal * response * bounce;
    return true;
}
#else
vec4 displayColor(uint seed, float phase, int shape, ivec2 point)
{
    vec4 color = Data[8] * curve(21, randomValue(seed, 27u), vec4(1)) * curve(20, phase, vec4(1));
    if ((shape == 4 || shape == 5) && Data[9].z != 0) color *= texelFetch(PointColors, point, 0);
    float angle = parameter(9, seed) * curve(9, phase, vec4(1)).r * PI * 2.0;
    float c = cos(angle), s = sin(angle); vec3 rgb = color.rgb;
    color.r = dot(vec3(.299 + .701*c + .168*s, .299 - .299*c - .328*s, .299 - .300*c + 1.250*s), rgb);
    color.g = dot(vec3(.587 - .587*c + .330*s, .587 + .413*c + .035*s, .587 - .588*c - 1.050*s), rgb);
    color.b = dot(vec3(.114 - .114*c - .497*s, .114 - .114*c + .292*s, .114 + .886*c - .203*s), rgb);
    color.a *= curve(18, phase, vec4(1)).r;
    color.rgb *= 1.0 + curve(19, phase, vec4(0)).r;
    return color;
}
#endif
void main()
{
    uint index = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * Options.w;
    if (index >= Counts.x || (PARTICLES[index].State.x & 1u) == 0) return;
    Electron2D_Particle particle = PARTICLES[index];
    bool newborn = (particle.State.x & 2u) != 0;
    uint supplied = (particle.State.x & ELECTRON2D_PARTICLE_EMIT_FLAGS_MASK) >> ELECTRON2D_PARTICLE_EMIT_FLAGS_SHIFT;
    uint seed = hashValue(particle.State.z ^ hashValue(particle.State.y));
    uint flags = uint(Data[1].w);
    float delta = uintBitsToFloat(particle.State.w);
    int shape = int(SourceConfig.x);
    ivec2 pointSize = textureSize(Points, 0);
    int count = min(int(SourceConfig.y), pointSize.x * pointSize.y);
    int pointIndex = count > 0 ? min(count - 1, int(randomValue(seed, 28u) * count)) : 0;
    ivec2 point = ivec2(pointIndex % pointSize.x, pointIndex / pointSize.x);
    vec2 position = vec2(particle.TransformX.w, particle.TransformY.w);
    vec2 physical = vec2(particle.TransformX.z, particle.TransformY.z);
#ifdef ELECTRON2D_PARTICLE_MOTION
    if (newborn)
    {
        if ((supplied & 16u) == 0) particle.Custom = vec4(0, 0, 0, 1.0 - Data[0].w * randomValue(seed, 29u));
        particle.VelocityAge.w = 0;
        if ((supplied & 1u) == 0) position = initialPosition(seed, shape, point);
        physical = (supplied & 4u) != 0 ? particle.VelocityAge.xy : direction(seed, 30u, Data[0].z) * parameter(0, seed);
        if ((supplied & 4u) == 0 && shape == 5 && Data[9].y != 0)
        {
            vec2 normal = texelFetch(Normals, point, 0).xy;
            physical = mat2(normal, normal.yx * vec2(1, -1)) * physical;
        }
        physical = emissionVector(physical) + EmitterVelocity.xy * Data[1].z;
        particle.VelocityAge.z += delta;
    }
    else position -= particle.VelocityAge.xy * delta;
    float previousAge = particle.Custom.y;
    particle.State.x &= ~(4u | 16u);
    if (newborn && previousAge == 0) particle.State.x |= 16u;
    particle.Custom.y = mix(previousAge + delta / Time.y, 1.0, Cycle.z);
    float life = 1.0 - Data[0].w * randomValue(seed, 29u);
    float phase = life > 0 ? particle.Custom.y / life : 1.0;
    vec2 diff = position - vec2(EmissionX.w, EmissionY.w);
    vec2 radial = unitVector(diff);
    vec2 force = Data[1].xy + unitVector(physical) * parameter(3, seed) * curve(3, phase, vec4(1)).r
        + radial * parameter(4, seed) * curve(4, phase, vec4(1)).r
        + vec2(-radial.y, radial.x) * parameter(5, seed) * curve(5, phase, vec4(1)).r;
    physical += force * delta;
#ifdef ELECTRON2D_CPU_PARTICLE_CURVES
    if (hasCurve(0)) physical = unitVector(physical) * curve(0, phase, vec4(0)).r;
#endif
    float damping = parameter(6, seed) * curve(6, phase, vec4(1)).r;
    float speed = length(physical);
    if (damping > 0 && speed > 0) physical *= max(0.0, speed - damping * delta * ((flags & 2u) != 0 ? speed * speed * .05 : 1.0)) / speed;
    vec2 controlled = vec2(0);
    float orbit = parameter(2, seed);
    vec4 orbitCurve = curve(2, phase, vec4(1)); orbit *= (CurveOptions.y & 4u) != 0 ? orbitCurve.r : orbitCurve.b;
    if (abs(orbit) >= .01 && delta >= .001)
    {
        float angle = orbit * 2.0 * PI * delta;
        controlled += (mat2(vec2(cos(angle), -sin(angle)), vec2(sin(angle), cos(angle))) * diff - diff) / delta;
    }
    vec2 toPivot = position - emissionPoint(Data[5].xy);
    float radialSpeed = parameter(15, seed) * curve(15, phase, vec4(1)).r;
    vec2 radialDirection = length(toPivot) > .01 ? unitVector(toPivot) : direction(seed, 31u, 360.0);
    if (radialSpeed < 0 && delta > 0) radialSpeed = -min(-radialSpeed, length(toPivot) / delta);
    controlled += radialDirection * radialSpeed;
    controlled += emissionVector(curve(16, phase, vec4(0)).xy * parameter(16, seed));
    float storedScale = .5 * (length(vec2(particle.TransformX.x, particle.TransformY.x)) + length(vec2(particle.TransformX.y, particle.TransformY.y)));
    float radius = EmitterVelocity.w * (Data[9].w != 0 ? storedScale : 1.0);
    int collisionPolicy = SourceConfig.w == 0 && SourceConfig.z == 3 ? 3 : SourceConfig.w;
    bool collided = collision(position, physical, radius, collisionPolicy);
    if (collided && int(SourceConfig.w) == 2) particle.State.x |= 32u;
    if (Data[7].z != 0 && !(collided && int(SourceConfig.w) == 1))
    {
        vec2 noise = unitVector(noiseDirection(position));
        float influence = delta > 0 ? clamp(parameter(13, seed) * curve(12, phase, vec4(1)).r, 0, 1) : 0;
        physical = mix(physical, noise * length(physical) * (1.0 + (1.0 - influence) * .2), influence);
        controlled = mix(controlled, noise * length(controlled) * (1.0 + (1.0 - influence) * .2), influence);
    }
    vec2 velocity = physical + controlled;
    speed = length(velocity);
    if (hasCurve(22) && speed > .001) velocity *= min(speed, abs(curve(22, phase, vec4(0)).r)) / speed;
    speed = length(velocity);
    position += velocity * delta;
    if (collided) particle.State.x |= 4u;
    particle.TransformX.zw = vec2(physical.x, position.x);
    particle.TransformY.zw = vec2(physical.y, position.y);
    particle.VelocityAge.xy = velocity;
#else
    vec2 velocity = particle.VelocityAge.xy;
    float speed = length(velocity);
    bool collided = (particle.State.x & 4u) != 0;
    float life = 1.0 - Data[0].w * randomValue(seed, 29u);
    float phase = life > 0 ? particle.Custom.y / life : 1.0;
    float baseScale = parameter(8, seed); baseScale = sign(baseScale) * max(abs(baseScale), .001);
    vec2 scale = vec2(baseScale) * curve(8, phase, vec4(1)).xy;
#ifdef ELECTRON2D_CPU_PARTICLE_CURVES
    scale *= curve(23, phase, vec4(1)).r;
#endif
    float angle = parameter(7, seed) * curve(7, phase, vec4(1)).r;
    if (hasCurve(1)) { particle.VelocityAge.w += curve(1, phase, vec4(1)).r * delta * parameter(1, seed); particle.Custom.x = radians(angle) + particle.VelocityAge.w; }
    else particle.Custom.x = radians(angle + particle.Custom.y * Time.y * parameter(1, seed));
    particle.Custom.z = (parameter(11, seed) + curve(11, phase, vec4(0)).r) + phase * parameter(10, seed) * curve(10, phase, vec4(1)).r;
    if (hasCurve(17))
    {
        float span = Ranges[17].y - Ranges[17].x;
        float coordinate = speed > .001 ? (span > 0 ? clamp(speed / span, 0, 1) : 1.0) : 0.0;
        scale *= curve(17, coordinate, vec4(1)).xy;
    }
    if ((flags & 4u) != 0) scale *= vec2(length(vec2(EmissionX.x, EmissionY.x)), length(vec2(EmissionX.y, EmissionY.y)));
    if (newborn) scale = mix(vec2(-1), vec2(1), greaterThanEqual(scale, vec2(0))) * max(abs(scale), vec2(.001));
    else scale = max(abs(scale), vec2(.001));
    vec2 yAxis = vec2(sin(particle.Custom.x), cos(particle.Custom.x));
    if ((flags & 1u) != 0) yAxis = length(velocity) > 0 ? unitVector(velocity) : unitVector(vec2(particle.TransformX.y, particle.TransformY.y));
    particle.TransformX = vec4(yAxis.y * scale.x, yAxis.x * scale.y, physical.x, position.x);
    particle.TransformY = vec4(-yAxis.x * scale.x, yAxis.y * scale.y, physical.y, position.y);
    particle.Color = displayColor(seed, phase, shape, point);
    particle.VelocityAge.xy = velocity;
    int subMode = int(SourceConfig.z), emitCount = 0;
    if (subMode == 1 && delta > 0)
    {
        float interval = 1.0 / Data[4].w;
        float from = particle.Custom.y * Time.y - delta;
        if (delta >= interval || delta >= interval - mod(from, interval)) emitCount = 1;
    }
    if (subMode == 2 && (particle.State.x & 8u) == 0 && particle.Custom.y / max(particle.Custom.w, .000001) * Time.y > Time.y - delta) { emitCount = SubConfig.y; particle.State.x |= 8u; }
    if (subMode == 3 && collided) emitCount = int(SubConfig.z);
    if (subMode == 4 && (particle.State.x & 16u) != 0) emitCount = int(SubConfig.x);
    for (int i = 0; i < emitCount; i++)
    {
        Electron2D_Particle child = particle; child.State.x = SubConfig.w != 0 ? 7u : 3u;
        child.TransformX.z = child.TransformY.z = 0; child.Custom = child.Color = vec4(0);
        if (!Electron2D_EmitSubparticle(child)) break;
    }
    if ((particle.State.x & 32u) != 0 || particle.Custom.y > particle.Custom.w || any(isnan(particle.TransformX)) || any(isinf(particle.TransformX)) || any(isnan(particle.TransformY)) || any(isinf(particle.TransformY)) || any(isnan(particle.Custom)) || any(isinf(particle.Custom)) || any(isnan(particle.Color)) || any(isinf(particle.Color)) || any(isnan(particle.VelocityAge)) || any(isinf(particle.VelocityAge))) particle.State.x &= ~1u;
#endif
    PARTICLES[index] = particle;
}
