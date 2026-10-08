// Contact coefficients shared by preparation and the two iterative kernels.
struct Constraint
{
    uvec4 bodies; // Body A/B and incident next links A/B.
    vec4 normal; // Normal xy and cross(lever, normal) for A/B.
    vec4 tangent; // Friction, padding and cross(lever, tangent) for A/B; tangent=(ny,-nx).
    vec4 parameters; // Normal/tangent effective mass, physical/correction target speeds.
};
struct ContactImpulse
{
    vec4 physical; // Accumulated normal/tangent, then this iteration's deltas.
    vec4 correction; // Accumulated/delta positional impulse, then padding.
};
float dot2(vec2 a,vec2 b) {return a.x*b.x+a.y*b.y;}
float cross2(vec2 a,vec2 b) {return a.x*b.y-a.y*b.x;}
vec2 rotate(vec2 q,vec2 p) {return vec2(q.x*p.x-q.y*p.y,q.y*p.x+q.x*p.y);}
vec2 angular(float w,vec2 p) {return vec2(-w*p.y,w*p.x);}
bool finite4(vec4 v) {return !any(isnan(v))&&!any(isinf(v));}
vec2 inverseMass(ResidentBody b) {return b.flags.y>=2u?vec2(b.properties.x,(b.flags.z&4u)==0u?b.properties.y:0):vec2(0);}
vec2 velocity(ResidentBody b,vec2 r) {return b.velocity.xy+angular(b.velocity.z,r);}
