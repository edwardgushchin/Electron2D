using System.Text;

namespace Electron2D;

public sealed partial class VisualShader
{
    private static string ParticleInterface(DomainCompilation domain, List<VisualShaderBlockName> blocks)
    {
        var source = new StringBuilder("#define ELECTRON2D_PARTICLE_SUBEMISSION\n#define ELECTRON2D_PARTICLE_BIRTH_NUMBERS\n");
        var roles = new[] { "Electron2D_Particle", "ParticleFrame", "ParticleState", "ParticleSubemission", "ParticleBirthNumbers", "PARTICLES", "SUBPARTICLES", "BIRTH_NUMBERS", "Electron2D_EmitSubparticle", "ELECTRON2D_PARTICLE_EMIT_FLAGS_SHIFT", "ELECTRON2D_PARTICLE_EMIT_FLAGS_MASK" };
        foreach (var role in roles) source.Append("#define ").Append(role).Append(' ').Append(domain.Symbols[role]).AppendLine();
        source.Append("#define ELECTRON2D_PARTICLE_FRAME_INSTANCE ").AppendLine(domain.Symbols["e2d_frame"]);
        source.AppendLine("#include \"electron2d/particles.glslinc\"");
        foreach (var role in roles) source.Append("#undef ").AppendLine(role);
        source.AppendLine("#undef ELECTRON2D_PARTICLE_FRAME_INSTANCE");
        blocks.Add(new(Type.Process, domain.Symbols["ParticleFrame"], "ParticleFrame"));
        source.AppendLine("layout(local_size_x=64) in;");
        source.Append(RewriteExpression("""
            mat4 TRANSFORM, EMISSION_TRANSFORM;
            vec3 VELOCITY, COLLISION_NORMAL;
            vec4 COLOR, CUSTOM;
            bool ACTIVE, RESTART, RESTART_POSITION, RESTART_ROT_SCALE, RESTART_VELOCITY, RESTART_COLOR, RESTART_CUSTOM, COLLIDED;
            float DELTA, LIFETIME, COLLISION_DEPTH;
            uint NUMBER, INDEX, RANDOM_SEED, e2d_seed;
            """, domain.Symbols)).AppendLine();
        return source.ToString();
    }
    private string ParticleEntry(DomainCompilation domain, bool collision)
    {
        var source = new StringBuilder();
        if (collision) source.Append(ParticleCollision);
        source.AppendLine("""
            void main() {
                INDEX = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * e2d_frame.Options.w;
                if (INDEX >= e2d_frame.Counts.x || SUBPARTICLES.length() == 0 || BIRTH_NUMBERS.length() <= INDEX + 1u) return;
                Electron2D_Particle e2d_particle = PARTICLES[INDEX];
                if ((e2d_particle.State.x & 1u) == 0u) return;
                ACTIVE = true;
                RESTART = (e2d_particle.State.x & 2u) != 0u;
                uint supplied = (e2d_particle.State.x >> 8u) & 31u;
                RESTART_POSITION = RESTART && (supplied & 1u) == 0u;
                RESTART_ROT_SCALE = RESTART && (supplied & 2u) == 0u;
                RESTART_VELOCITY = RESTART && (supplied & 4u) == 0u;
                RESTART_COLOR = RESTART && (supplied & 8u) == 0u;
                RESTART_CUSTOM = RESTART && (supplied & 16u) == 0u;
                DELTA = uintBitsToFloat(e2d_particle.State.w);
                LIFETIME = e2d_frame.Time.y;
                NUMBER = BIRTH_NUMBERS[INDEX + 1u]; RANDOM_SEED = e2d_frame.Counts.z;
                COLOR = e2d_particle.Color; CUSTOM = e2d_particle.Custom;
                VELOCITY = vec3(e2d_particle.VelocityAge.xy, 0.0);
                TRANSFORM = mat4(vec4(e2d_particle.TransformX.x,e2d_particle.TransformY.x,0,0), vec4(e2d_particle.TransformX.y,e2d_particle.TransformY.y,0,0), vec4(0,0,1,0), vec4(e2d_particle.TransformX.w,e2d_particle.TransformY.w,0,1));
                EMISSION_TRANSFORM = mat4(vec4(e2d_frame.EmissionX.x,e2d_frame.EmissionY.x,0,0), vec4(e2d_frame.EmissionX.y,e2d_frame.EmissionY.y,0,0), vec4(0,0,1,0), vec4(e2d_frame.EmissionX.w,e2d_frame.EmissionY.w,0,1));
                COLLIDED = false; COLLISION_NORMAL = vec3(0); COLLISION_DEPTH = 0.0;
                if (RESTART) {
                    e2d_seed = e2d_hash(NUMBER + 1u + RANDOM_SEED);
            """);
        source.Append(domain.Symbols["e2d_stage_3"]).AppendLine("();");
        source.Append(domain.Symbols["e2d_stage_6"]).AppendLine("(); }");
        source.AppendLine("if (e2d_particle.VelocityAge.z >= LIFETIME) ACTIVE = false;");
        source.AppendLine("if (ACTIVE) {");
        if (collision) source.AppendLine("e2d_collision();");
        source.AppendLine("e2d_seed = e2d_hash(NUMBER + 1u + RANDOM_SEED);");
        if (HasStageEffect(domain, Type.Collide))
            source.Append("if (COLLIDED) ").Append(domain.Symbols["e2d_stage_5"]).Append("(); else ").Append(domain.Symbols["e2d_stage_4"]).AppendLine("();");
        else source.Append(domain.Symbols["e2d_stage_4"]).AppendLine("();");
        source.Append(domain.Symbols["e2d_stage_7"]).AppendLine("(); }");
        source.AppendLine("""
                e2d_particle.TransformX = vec4(TRANSFORM[0].x, TRANSFORM[1].x, e2d_particle.TransformX.z, TRANSFORM[3].x);
                e2d_particle.TransformY = vec4(TRANSFORM[0].y, TRANSFORM[1].y, e2d_particle.TransformY.z, TRANSFORM[3].y);
                e2d_particle.VelocityAge.xy = VELOCITY.xy;
                e2d_particle.Color = COLOR; e2d_particle.Custom = CUSTOM;
                e2d_particle.State.x = ACTIVE ? e2d_particle.State.x | 1u : e2d_particle.State.x & ~1u;
                PARTICLES[INDEX] = e2d_particle;
            }
            """);
        return RewriteExpression(source.ToString(), domain.Symbols);
    }
    private const string ParticleMath = """
        float e2d_random(inout uint seed) {
            int s = int(seed); if (s == 0) s = 305420679;
            int k = s / 127773;
            s = 16807 * (s - k * 127773) - 2836 * k;
            if (s < 0) s += 2147483647;
            seed = uint(s); return float(seed % 65536u) / 65535.0;
        }
        uint e2d_hash(uint x) {
            x = ((x >> 16u) ^ x) * 73244475u;
            x = ((x >> 16u) ^ x) * 73244475u;
            return (x >> 16u) ^ x;
        }
        float e2d_range(inout uint seed, float from, float to) { return e2d_random(seed) * (to - from) + from; }
        vec2 e2d_range(inout uint seed, vec2 from, vec2 to) { return vec2(e2d_range(seed,from.x,to.x),e2d_range(seed,from.y,to.y)); }
        vec3 e2d_range(inout uint seed, vec3 from, vec3 to) { return vec3(e2d_range(seed,from.x,to.x),e2d_range(seed,from.y,to.y),e2d_range(seed,from.z,to.z)); }
        vec4 e2d_range(inout uint seed, vec4 from, vec4 to) { return vec4(e2d_range(seed,from.x,to.x),e2d_range(seed,from.y,to.y),e2d_range(seed,from.z,to.z),e2d_range(seed,from.w,to.w)); }
        vec2 e2d_circle(inout uint seed, float radius, float inner) {
            vec2 direction = normalize(vec2(e2d_random(seed)*2.0-1.0,e2d_random(seed)*2.0-1.0));
            return direction * e2d_range(seed, inner, radius);
        }
        vec2 e2d_ring(inout uint seed, float radius, float inner) {
            float angle = e2d_random(seed) * 6.283185307179586;
            return vec2(sin(angle),cos(angle)) * e2d_range(seed,inner,radius);
        }
        vec2 e2d_box(inout uint seed, vec2 extent) { return vec2(e2d_range(seed,-extent.x*.5,extent.x*.5),e2d_range(seed,-extent.y*.5,extent.y*.5)); }
        mat3 e2d_rotation(vec3 axis, float angle) {
            axis = normalize(axis); float s = sin(angle), c = cos(angle), oc = 1.0-c;
            return mat3(vec3(oc*axis.x*axis.x+c,oc*axis.x*axis.y-axis.z*s,oc*axis.z*axis.x+axis.y*s),vec3(oc*axis.x*axis.y+axis.z*s,oc*axis.y*axis.y+c,oc*axis.y*axis.z-axis.x*s),vec3(oc*axis.z*axis.x-axis.y*s,oc*axis.y*axis.z+axis.x*s,oc*axis.z*axis.z+c));
        }
        vec3 e2d_cone(inout uint seed, vec3 direction, float degrees) {
            float spread = radians(degrees);
            float a = (e2d_random(seed)*2.0-1.0)*spread;
            float b = (e2d_random(seed)*2.0-1.0)*spread;
            a += direction.z != 0.0 ? atan(direction.x,direction.z) : sign(direction.x)*1.5707963267948966;
            b += direction.z != 0.0 ? atan(direction.y,abs(direction.z)) : (direction.x != 0.0 ? atan(direction.y,abs(direction.x)) : sign(direction.y)*1.5707963267948966);
            vec3 x = vec3(sin(a),0,cos(a)), y = vec3(0,sin(b),cos(b));
            y.z /= max(.0001,sqrt(abs(y.z)));
            return normalize(vec3(x.x*y.z,y.y,x.z*y.z));
        }
        vec3 e2d_accelerate(inout uint seed, int mode, vec3 velocity, vec3 displacement, vec3 amount, float randomness, vec3 axis) {
            vec3 direction = mode == 0 ? velocity : mode == 1 ? displacement : cross(normalize(displacement),normalize(axis));
            return length(direction) > 0.0 ? normalize(direction)*amount*mix(1.0,e2d_random(seed),randomness) : vec3(0);
        }
        void e2d_emit(mat4 transform, vec3 velocity, vec4 color, vec4 custom, uint flags) {
            Electron2D_Particle particle;
            particle.TransformX = vec4(transform[0].x,transform[1].x,0,transform[3].x);
            particle.TransformY = vec4(transform[0].y,transform[1].y,0,transform[3].y);
            particle.Color = color; particle.Custom = custom; particle.VelocityAge = vec4(velocity.xy,0,0);
            particle.State = uvec4(flags,0,0,0);
            Electron2D_EmitSubparticle(particle);
        }
        """;
    private const string ParticleCollision = """
        void e2d_collision() {
            if (any(equal(e2d_frame.SDFRect.xy,vec2(0)))) return;
            vec2 position = TRANSFORM[3].xy;
            vec2 uv = vec2(dot(e2d_frame.SDFTransformX.xy,position)+e2d_frame.SDFTransformX.w,dot(e2d_frame.SDFTransformY.xy,position)+e2d_frame.SDFTransformY.w);
            if (any(lessThanEqual(uv,vec2(0))) || any(greaterThanEqual(uv,vec2(1)))) return;
            mat2 toCanvas = mat2(vec2(e2d_frame.SDFTransformX.x/e2d_frame.SDFRect.x,e2d_frame.SDFTransformY.x/e2d_frame.SDFRect.y),vec2(e2d_frame.SDFTransformX.y/e2d_frame.SDFRect.x,e2d_frame.SDFTransformY.y/e2d_frame.SDFRect.y));
            float radius = length(toCanvas[1])*e2d_frame.EmitterVelocity.w;
            float distance = textureLod(SDF_TEXTURE,uv,0).r;
            float depth = radius-distance;
            if (depth <= .000001*max(1.0,max(radius,abs(distance))) || abs(determinant(toCanvas)) < .00000001) return;
            vec2 texel = 1.0/vec2(textureSize(SDF_TEXTURE,0));
            vec2 gradient = vec2(textureLod(SDF_TEXTURE,uv+vec2(texel.x,0),0).r-textureLod(SDF_TEXTURE,uv-vec2(texel.x,0),0).r,textureLod(SDF_TEXTURE,uv+vec2(0,texel.y),0).r-textureLod(SDF_TEXTURE,uv-vec2(0,texel.y),0).r)/(2.0*texel)*e2d_frame.SDFRect.xy;
            if (dot(gradient,gradient)<.00000001) {
                float dx = textureLod(SDF_TEXTURE,uv+vec2(texel.x,0),0).r-distance;
                float dy = textureLod(SDF_TEXTURE,uv+vec2(0,texel.y),0).r-distance;
                gradient = abs(dx)>=abs(dy) ? vec2(dx<0 ? -1:1,0) : vec2(0,dy<0 ? -1:1);
            }
            vec2 displacement = inverse(toCanvas)*normalize(gradient);
            COLLISION_NORMAL = vec3(normalize(displacement),0);
            COLLISION_DEPTH = length(displacement)*depth;
            COLLIDED = true;
        }
        """;
}
