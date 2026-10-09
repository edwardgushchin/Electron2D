#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsResidentBody.inc.glsl"
layout(local_size_x = 64) in;

struct Command { uvec4 header; ResidentBody body; vec4 center; vec4 impulse; vec4 transientForce; vec4 target; };
struct Snapshot { vec4 pose; vec4 velocity; vec4 fields; float clock; uint flags; uvec2 padding; };
layout(std430, set = 0, binding = 0) readonly buffer Commands { Command commands[]; };
layout(std430, set = 0, binding = 1) readonly buffer Requests { uvec4 requests[]; };
layout(std430, set = 0, binding = 2) readonly buffer Corrections { vec4 corrections[]; };
layout(std430, set = 1, binding = 0) buffer Bodies { ResidentBody bodies[]; };
layout(std430, set = 1, binding = 1) buffer Status { uint status; };
layout(std430, set = 1, binding = 2) buffer Results { Snapshot results[]; };
layout(std430,set=1,binding=3) buffer Centers { vec2 centers[]; };
layout(std430,set=1,binding=4) buffer TransientForces { vec4 transientForces[]; };
layout(std430,set=1,binding=5) buffer Targets { vec4 targets[]; };
layout(std430,set=1,binding=6) buffer Fields { vec4 fields[]; };
layout(std140, set = 2, binding = 0) uniform Settings { vec4 step; uvec4 control; };

bool finite4(vec4 v) { return !any(isnan(v)) && !any(isinf(v)); }
vec2 rotate(vec2 q,vec2 p) {return vec2(q.x*p.x-q.y*p.y,q.y*p.x+q.x*p.y);}
void fail(uint value) { atomicOr(status, value); }

void main()
{
    uint i = gl_GlobalInvocationID.x;
    if (i >= control.y) return;
    if (control.x == 0u)
    {
        Command c = commands[i];
        uint index = c.header.x, generation = c.header.y, mask = c.header.z;
        if (index >= control.z) { fail(1u); return; }
        if ((mask & 2u) != 0u) { bodies[index] = ResidentBody(vec4(0), vec4(0), vec4(0), vec4(0), vec4(0), uvec4(generation, 0, 32, 0)); centers[index]=vec2(0); transientForces[index]=vec4(0); targets[index]=vec4(0); fields[index]=vec4(0); return; }
        ResidentBody b;
        if ((mask & 1u) != 0u) { b = c.body; centers[index] = c.center.xy; targets[index]=vec4(0); fields[index]=vec4(0); }
        else
        {
            b = bodies[index];
            if (b.flags.x != generation || b.flags.w == 0u) { fail(1u); return; }
        }
        vec4 pending=(mask&1u)!=0u?vec4(0):transientForces[index];
        if((mask&32768u)!=0u)pending.xyz+=c.transientForce.xyz;
        if(!finite4(pending)){fail(2u);return;}
        // A nonzero alive word also versions explicit pose/velocity/mass/role/policy edits for contact and joint history.
        if ((mask & 1u) == 0u && ((mask & (4096u|8192u|16384u|65536u)) != 0u || ((mask & 4u) != 0u && b.pose != c.body.pose) || ((mask & 8u) != 0u && b.velocity != c.body.velocity) || ((mask & 64u) != 0u && (centers[index] != c.center.xy || b.properties.xy != c.body.properties.xy)))) b.flags.w = b.flags.w == 0xffffffffu ? 1u : b.flags.w + 1u;
        if ((mask & 4096u) != 0u) b.flags.y=c.body.flags.y;
        if ((mask & 65536u) != 0u) b.surface.xyz=c.body.surface.xyz;
        if ((mask & 524288u) != 0u) b.surface.w=c.body.surface.w;
        if ((mask & 262144u) != 0u) b.flags.z&=~6144u;
        if ((mask & 131072u) != 0u) {targets[index]=c.target;b.flags.z|=2048u;}
        if ((mask & 8192u) != 0u)
        {
            b.force.w=c.body.force.w;b.properties.zw=c.body.properties.zw;
            b.flags.z=(b.flags.z&~50180u)|(c.body.flags.z&50180u);
        }
        if ((mask & 4u) != 0u) b.pose = c.body.pose;
        if ((mask & 8u) != 0u) b.velocity = c.body.velocity;
        if ((mask & 64u) != 0u) { centers[index] = c.center.xy; b.properties.xy = c.body.properties.xy; }
        if ((mask & 32u) != 0u) b.force.xyz = c.body.force.xyz;
        if ((mask & 1024u) != 0u) b.flags.z|=32u;
        if ((mask & 2048u) != 0u) b.flags.z=(b.flags.z&~768u)|(c.header.w&768u);
        if ((mask & 512u) != 0u) b.flags.z=(b.flags.z&~8u)|(c.header.w&8u);
        if ((mask & 128u) != 0u) {b.flags.z=(b.flags.z&~80u)|32u;b.velocity.w=0;}
        if ((mask & 256u) != 0u) {b.flags.z=b.flags.z|80u;b.velocity=vec4(0);}
        if ((mask & 1u) != 0u && (b.flags.z&16u)!=0u) b.velocity=vec4(0);
        if ((mask & 16384u) != 0u) b.velocity.z=0;
        if ((mask & 16u) != 0u && b.flags.y >= 2u)
        {
            b.velocity.xy += c.impulse.xy;
            if ((b.flags.z & 4u) == 0u) b.velocity.z += c.impulse.z;
        }
        if ((b.flags.z & 4u) != 0u) b.velocity.z = 0;
        if (!finite4(b.pose) || !finite4(b.velocity) || !finite4(vec4(b.velocity.xyz+b.surface.xyz,b.surface.w))) { fail(2u); return; }
        bodies[index] = b; transientForces[index]=pending;
    }
    else if (control.x == 1u || control.x == 3u || control.x == 4u)
    {
        ResidentBody b = bodies[i];
        if (b.flags.w == 0u) return;
        vec4 selected=fields[i];
        if((control.w&1u)!=0u&&(control.w&4u)==0u)
        {
            if(!resolveBodyFields(b,selected,vec4(step.xy,0,0),step.w)){fail(2u);return;}
            fields[i]=selected;
        }
        vec4 pending=transientForces[i];
        // Select once at outer-tick entry. A sleeper woken by this tick's contacts
        // retains its queued force until the next tick, as does a static body.
        if((control.w&1u)!=0u)pending.w=b.flags.y==1u||(b.flags.y>=2u&&(b.flags.z&16u)==0u)?1:0;
        if((control.w&1u)!=0u&&b.flags.y==1u)
        {
            b.velocity=vec4(0);
            if((b.flags.z&2048u)!=0u)
            {
                vec4 target=targets[i];
                vec2 oldCenter=b.pose.xy+rotate(b.pose.zw,centers[i]);
                vec2 newCenter=target.xy+rotate(target.zw,centers[i]);
                b.velocity.xy=(newCenter-oldCenter)/step.w;
                float cross=b.pose.z*target.w-b.pose.w*target.z;
                float dot=b.pose.z*target.z+b.pose.w*target.w;
                b.velocity.z=atan(cross,dot)/step.w;
                b.flags.z|=4096u;
            }
        }
        if(b.flags.y!=0u&&(b.flags.z&16u)==0u)
        {
        float dt = step.z;
        if (control.x != 4u && b.flags.y >= 2u)
        {
            bodyForces(b,dt,selected,step.w,pending.w!=0?pending.xyz:vec3(0));
        }
        if ((b.flags.z & 4u) != 0u) b.velocity.z = 0;
        if (control.x != 3u)
        {
        vec3 motion = b.velocity.xyz;
        if (control.x == 4u && step.w != 0) motion += corrections[i].xyz;
        if(dt>0&&motion!=vec3(0))
        {
        vec2 center = b.pose.xy + rotate(b.pose.zw,centers[i]) + dt * motion.xy;
        float angle = dt * motion.z;
        vec2 q = vec2(cos(angle), sin(angle));
        b.pose.zw = vec2(b.pose.z * q.x - b.pose.w * q.y, b.pose.w * q.x + b.pose.z * q.y);
        b.pose.zw *= inversesqrt(dot(b.pose.zw, b.pose.zw));
        b.pose.xy = center - rotate(b.pose.zw,centers[i]);
        }
        }
        }
        if((control.w&2u)!=0u)
        {
            if(pending.w!=0)pending=vec4(0);
            if(b.flags.y==1u&&(b.flags.z&4096u)!=0u){b.pose=targets[i];b.flags.z&=~6144u;}
        }
        if (!finite4(b.pose) || !finite4(b.velocity) || !finite4(vec4(b.velocity.xyz+b.surface.xyz,b.surface.w))) { fail(2u); return; }
        bodies[i] = b; transientForces[i]=pending;
    }
    else
    {
        uvec4 request = requests[i];
        if (request.x >= control.z) { fail(1u); return; }
        ResidentBody b = bodies[request.x];
        if (b.flags.w == 0u || b.flags.x != request.y) { fail(1u); return; }
        results[i] = Snapshot(b.pose, vec4(b.velocity.xyz+b.surface.xyz,0), fields[request.x], b.velocity.w, b.flags.z, uvec2(b.flags.y,0));
    }
}
