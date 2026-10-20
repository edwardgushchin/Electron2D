// Contact coefficients shared by preparation and the two iterative kernels.
struct Constraint
{
    uvec4 bodies; // Body A/B and incident next links A/B.
    vec4 normal; // Normal xy and cross(lever, normal) for A/B.
    vec4 tangent; // Friction, row mode (low two bits: contact/bilateral/physical-only; bits 2/3 disable response A/B) and cross(lever, tangent) for A/B; tangent=(ny,-nx).
    vec4 parameters; // Normal effective mass, tangent effective mass (joint softness), physical/correction target speeds.
};
struct ContactImpulse
{
    vec4 physical; // Accumulated normal/tangent, then this iteration's deltas.
    vec4 correction; // Accumulated/delta positional impulse, then joint impulse lower/upper bounds.
};
struct SolverBody { vec4 velocity; vec4 correction; vec4 surface; }; // Spare velocity/correction lanes store inverse mass/inertia.
float dot2(vec2 a,vec2 b) {return a.x*b.x+a.y*b.y;}
float cross2(vec2 a,vec2 b) {return a.x*b.y-a.y*b.x;}
vec2 rotate(vec2 q,vec2 p) {return vec2(q.x*p.x-q.y*p.y,q.y*p.x+q.x*p.y);}
vec2 angular(float w,vec2 p) {return vec2(-w*p.y,w*p.x);}
bool finite4(vec4 v) {return !any(isnan(v))&&!any(isinf(v));}
vec2 inverseMass(ResidentBody b) {return b.flags.y>=2u&&(b.flags.z&16u)==0u?vec2(b.properties.x,(b.flags.z&4u)==0u?b.properties.y:0):vec2(0);}
uint rowMode(Constraint c) {return uint(c.tangent.y)&3u;}
bool responds(Constraint c,bool second) {return (uint(c.tangent.y)&(second?8u:4u))==0u;}
vec2 velocity(ResidentBody b,vec2 r) {return b.velocity.xy+b.surface.xy+angular(b.velocity.z+b.surface.z,r);}

ResidentBody worldBody() {return ResidentBody(vec4(0,0,1,0),vec4(0),vec4(0),vec4(0),vec4(0),uvec4(0,0,0,1));}

vec2 contactImpulse(Constraint c,vec2 previous,vec2 speed,vec2 ma,vec2 mb)
{
    vec2 normal=c.normal.zw,tangent=c.tangent.zw,inertia=vec2(ma.y,mb.y);
    float mass=ma.x+mb.x,kn=mass+dot2(inertia,normal*normal),kt=mass+dot2(inertia,tangent*tangent);
    float coupling=dot2(inertia,normal*tangent);
    if(coupling==0)
    {
        float pn=max(0,previous.x+c.parameters.x*(c.parameters.z-speed.x));
        return vec2(pn,clamp(previous.y-c.parameters.y*speed.y,-c.tangent.x*pn,c.tangent.x*pn));
    }
    float scale=max(kn,kt);
    vec3 k=vec3(kn,kt,coupling)/scale;
    vec2 rhs=vec2(c.parameters.z-speed.x,-speed.y)/scale+vec2(k.x*previous.x+k.z*previous.y,k.z*previous.x+k.y*previous.y);
    if(rhs.x<=0)return vec2(0);
    vec2 scaledInertia=inertia/scale;float scaledMass=mass/scale,arm=cross2(normal,tangent);
    // Positive terms retain the determinant when large angular levers nearly align the rows.
    float determinant=scaledMass*(scaledMass+dot2(scaledInertia,normal*normal+tangent*tangent))+scaledInertia.x*scaledInertia.y*arm*arm;
    if(determinant>0)
    {
        vec2 sticking=vec2(k.y*rhs.x-k.z*rhs.y,k.x*rhs.y-k.z*rhs.x)/determinant;
        if(sticking.x>=0&&abs(sticking.y)<=c.tangent.x*sticking.x)return sticking;
        float direction=sign(k.x*rhs.y-k.z*rhs.x),denominator=k.x+direction*c.tangent.x*k.z;
        if(denominator>0)
        {
            float pn=rhs.x/denominator;return vec2(pn,direction*c.tangent.x*pn);
        }
    }
    // shortcut: unresolved single-precision blocks retain projected iteration; widen precision if accepted ranges need it.
    float pn=max(0,previous.x+c.parameters.x*(c.parameters.z-speed.x));
    float vt=speed.y+(pn-previous.x)*coupling;
    return vec2(pn,clamp(previous.y-c.parameters.y*vt,-c.tangent.x*pn,c.tangent.x*pn));
}
