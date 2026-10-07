#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsBody.inc.glsl"
layout(local_size_x = 64) in;
struct Contact {
    vec4 ids; vec4 mass; vec4 normal; vec4 rolling; vec4 soft;
    vec4 anchors1; vec4 params1; vec4 impulses1;
    vec4 anchors2; vec4 params2; vec4 impulses2;
};
struct Joint {
    vec4 ids; vec4 mass; vec4 frameA; vec4 frameB; vec4 geometry;
    vec4 soft; vec4 spring; vec4 motor; vec4 impulses; vec4 limits;
};
layout(std430, set = 1, binding = 0) buffer Bodies { Body bodies[]; };
layout(std430, set = 1, binding = 1) buffer Contacts { Contact contacts[]; };
layout(std430, set = 1, binding = 2) buffer Joints { Joint joints[]; };
layout(std140, set = 2, binding = 0) uniform Step { vec4 step; vec4 control; vec4 solve; };

float cross2(vec2 a, vec2 b) { precise float r = a.x * b.y - a.y * b.x; return r; }
float dot2(vec2 a, vec2 b) { precise float r = a.x * b.x + a.y * b.y; return r; }
vec2 rotate(vec2 q, vec2 a) { precise vec2 r = vec2(q.x * a.x - q.y * a.y, q.y * a.x + q.x * a.y); return r; }
vec2 angular(float w, vec2 r) { return vec2(-w * r.y, w * r.x); }
Body readBody(int index)
{
    if (index >= 0) return bodies[index];
    return Body(vec4(0), vec4(0,0,1,0), vec4(0), vec4(0), vec4(0));
}
void writeVelocity(int index, vec3 velocity)
{
    if (index >= 0 && (uint(bodies[index].velocity.w) & 512u) != 0) bodies[index].velocity.xyz = velocity;
}
void applyImpulse(inout vec3 a, inout vec3 b, vec4 mass, vec2 ra, vec2 rb, vec2 impulse)
{
    precise vec3 va = a; precise vec3 vb = b;
    va.xy = va.xy - mass.x * impulse;
    va.z = va.z - mass.z * cross2(ra, impulse);
    vb.xy = vb.xy + mass.y * impulse;
    vb.z = vb.z + mass.w * cross2(rb, impulse);
    a = va; b = vb;
}
vec2 relativeVelocity(vec3 a, vec3 b, vec2 ra, vec2 rb) { return (b.xy + angular(b.z, rb)) - (a.xy + angular(a.z, ra)); }

void contactPoint(inout vec3 a, inout vec3 b, Contact c, vec4 anchors, vec4 params, inout vec4 impulses,
    Body ba, Body bb, uint stage)
{
    vec2 ra = anchors.xy; vec2 rb = anchors.zw; vec2 n = c.normal.xy; vec2 t = vec2(n.y, -n.x);
    if (stage == 2)
    {
        precise vec2 p = impulses.x * n + impulses.y * t;
        applyImpulse(a, b, c.mass, ra, rb, p);
        impulses.z += impulses.x;
        return;
    }
    precise float impulse;
    precise float vn = dot2(relativeVelocity(a, b, ra, rb), n);
    if (stage == 5)
    {
        if (c.rolling.w == 0 || params.w + solve.x > 0 || impulses.z == 0) return;
        impulse = params.x * (vn + c.rolling.w * params.w);
    }
    else
    {
        precise vec2 ds = (bb.delta.xy - ba.delta.xy) + (rotate(bb.delta.zw, rb) - rotate(ba.delta.zw, ra));
        precise float separation = dot2(n, ds) + params.z;
        precise float bias = 0; precise float massScale = 1; precise float impulseScale = 0;
        if (separation > 0) bias = separation * solve.y;
        else if (stage == 3)
        {
            bias = max(c.soft.y * c.soft.x * separation, -solve.z);
            massScale = c.soft.y; impulseScale = c.soft.z;
        }
        impulse = params.x * (massScale * vn + bias) + impulseScale * impulses.x;
    }
    precise float updated = max(impulses.x - impulse, 0.0);
    impulse = updated - impulses.x; impulses.x = updated; impulses.z += impulse;
    applyImpulse(a, b, c.mass, ra, rb, impulse * n);
}
void contactFriction(inout vec3 a, inout vec3 b, Contact c, vec4 anchors, vec4 params, inout vec4 impulses)
{
    vec2 t = vec2(c.normal.y, -c.normal.x);
    precise float speed = dot2(relativeVelocity(a, b, anchors.xy, anchors.zw), t) - c.normal.w;
    precise float limit = c.normal.z * impulses.x;
    precise float updated = clamp(impulses.y - params.y * speed, -limit, limit);
    precise float impulse = updated - impulses.y; impulses.y = updated;
    applyImpulse(a, b, c.mass, anchors.xy, anchors.zw, impulse * t);
}
void solveContact(uint index, uint stage)
{
    Contact c = contacts[index];
    Body ba = readBody(int(c.ids.x)); Body bb = readBody(int(c.ids.y));
    precise vec3 a = ba.velocity.xyz; precise vec3 b = bb.velocity.xyz;
    contactPoint(a, b, c, c.anchors1, c.params1, c.impulses1, ba, bb, stage);
    if (c.ids.z > 1) contactPoint(a, b, c, c.anchors2, c.params2, c.impulses2, ba, bb, stage);
    if (stage == 2)
    {
        a.z -= c.mass.z * c.rolling.z; b.z += c.mass.w * c.rolling.z;
    }
    else if (stage == 3 || stage == 4)
    {
        contactFriction(a, b, c, c.anchors1, c.params1, c.impulses1);
        if (c.ids.z > 1) contactFriction(a, b, c, c.anchors2, c.params2, c.impulses2);
        precise float limit = c.rolling.x * (c.impulses1.x + (c.ids.z > 1 ? c.impulses2.x : 0.0));
        precise float updated = clamp(c.rolling.z + c.rolling.y * (a.z - b.z), -limit, limit);
        precise float impulse = updated - c.rolling.z; c.rolling.z = updated;
        a.z -= c.mass.z * impulse; b.z += c.mass.w * impulse;
    }
    writeVelocity(int(c.ids.x), a); writeVelocity(int(c.ids.y), b);
    contacts[index] = c;
}

// The same polynomial and quadrant choices used by the compatibility solver.
float angleOf(vec2 q)
{
    if (q.x == 0 && q.y == 0) return 0;
    precise float ax = abs(q.x); precise float ay = abs(q.y);
    precise float a = min(ay, ax) / max(ay, ax);
    precise float s = a * a; precise float c = s * a; precise float fourth = s * s;
    precise float r = 0.024840285 * fourth + 0.18681418;
    precise float t = -0.094097948 * fourth - 0.33213072;
    r = r * s + t; r = r * c + a;
    if (ay > ax) r = 1.57079637 - r;
    if (q.x < 0) r = 3.14159274 - r;
    if (q.y < 0) r = -r;
    return r;
}
float limitImpulse(float error, float velocity, float mass, float accumulated, vec3 softness, bool bias)
{
    precise float speedBias = 0; precise float scale = 1; precise float impulseScale = 0;
    if (error > 0) speedBias = error * solve.y;
    else if (bias) { speedBias = softness.x * error; scale = softness.y; impulseScale = softness.z; }
    precise float impulse = -scale * mass * (velocity + speedBias) - impulseScale * accumulated;
    return max(accumulated + impulse, 0.0) - accumulated;
}
void solveJoint(uint index, uint stage)
{
    Joint j = joints[index];
    if (j.ids.z == 0) return;
    Body ba = readBody(int(j.ids.x)); Body bb = readBody(int(j.ids.y));
    precise vec3 a = ba.velocity.xyz; precise vec3 b = bb.velocity.xyz;
    vec2 ra = rotate(ba.delta.zw, j.frameA.xy); vec2 rb = rotate(bb.delta.zw, j.frameB.xy);
    precise vec2 d = ((bb.delta.xy - ba.delta.xy) + j.geometry.xy) + (rb - ra);
    bool wheel = j.ids.z == 2; bool bias = stage == 7;
    uint flags = uint(j.ids.w);
    vec2 axis = rotate(ba.delta.zw, j.frameA.zw); vec2 perp = vec2(-axis.y, axis.x);
    precise float aa = cross2(d + ra, axis); precise float ab = cross2(rb, axis);
    precise float pa = cross2(d + ra, perp); precise float pb = cross2(rb, perp);
    if (stage == 6)
    {
        precise float axial = j.impulses.z + j.limits.x - j.limits.y;
        if (!wheel) axial += j.impulses.w;
        precise vec2 p = wheel ? axial * axis + j.impulses.x * perp : j.impulses.xy;
        a.xy -= j.mass.x * p; b.xy += j.mass.y * p;
        a.z -= j.mass.z * (wheel ? axial * aa + j.impulses.x * pa + j.impulses.w : cross2(ra, p) + axial);
        b.z += j.mass.w * (wheel ? axial * ab + j.impulses.x * pb + j.impulses.w : cross2(rb, p) + axial);
    }
    else
    {
        bool fixedRotation = j.mass.z + j.mass.w == 0;
        precise float position = dot2(axis, d);
        if (!wheel)
        {
            vec2 qa = rotate(ba.delta.zw, j.frameA.zw); vec2 qb = rotate(bb.delta.zw, j.frameB.zw);
            position = angleOf(vec2(dot2(qa, qb), cross2(qa, qb)));
        }
        // Revolute spring precedes the motor; wheel spring follows it.
        for (int pass = 0; pass < 2; pass++)
        {
            if ((wheel && pass == 0) || (!wheel && pass == 1))
            {
                if ((flags & 2u) != 0 && !fixedRotation)
                {
                    precise float mass = wheel ? j.soft.w : j.geometry.z;
                    precise float impulse = -mass * (b.z - a.z - j.motor.y);
                    precise float limit = step.z * j.motor.x;
                    precise float updated = clamp(j.impulses.w + impulse, -limit, limit);
                    impulse = updated - j.impulses.w; j.impulses.w = updated;
                    a.z -= j.mass.z * impulse; b.z += j.mass.w * impulse;
                }
            }
            else if ((flags & 1u) != 0 && (wheel || !fixedRotation))
            {
                precise float error = wheel ? position : position - j.spring.w;
                if (!wheel) { if (error > 3.14159274) error -= 6.28318548; else if (error < -3.14159274) error += 6.28318548; }
                precise float velocity = wheel ? dot2(axis, b.xy - a.xy) + ab * b.z - aa * a.z : b.z - a.z;
                precise float impulse = -j.spring.y * j.geometry.z * (velocity + j.spring.x * error) - j.spring.z * j.impulses.z;
                j.impulses.z += impulse;
                if (wheel) applyImpulse(a, b, j.mass, vec2(0), vec2(0), impulse * axis);
                a.z -= j.mass.z * impulse * (wheel ? aa : 1.0); b.z += j.mass.w * impulse * (wheel ? ab : 1.0);
            }
        }
        if ((flags & 4u) != 0 && (wheel || !fixedRotation))
        {
            precise float velocity = wheel ? dot2(axis, b.xy - a.xy) + ab * b.z - aa * a.z : b.z - a.z;
            precise float impulse = limitImpulse(position - j.motor.z, velocity, j.geometry.z, j.limits.x, j.soft.xyz, bias);
            j.limits.x += impulse;
            if (wheel) { a.xy -= j.mass.x * impulse * axis; b.xy += j.mass.y * impulse * axis; }
            a.z -= j.mass.z * impulse * (wheel ? aa : 1.0); b.z += j.mass.w * impulse * (wheel ? ab : 1.0);
            velocity = wheel ? dot2(axis, a.xy - b.xy) + aa * a.z - ab * b.z : a.z - b.z;
            impulse = limitImpulse(j.motor.w - position, velocity, j.geometry.z, j.limits.y, j.soft.xyz, bias);
            j.limits.y += impulse;
            if (wheel) { a.xy += j.mass.x * impulse * axis; b.xy -= j.mass.y * impulse * axis; }
            a.z += j.mass.z * impulse * (wheel ? aa : 1.0); b.z -= j.mass.w * impulse * (wheel ? ab : 1.0);
        }
        if (wheel)
        {
            precise float speedBias = bias ? j.soft.x * dot2(perp, d) : 0;
            precise float scale = bias ? j.soft.y : 1; precise float impulseScale = bias ? j.soft.z : 0;
            precise float velocity = dot2(perp, b.xy - a.xy) + pb * b.z - pa * a.z;
            precise float impulse = -scale * j.geometry.w * (velocity + speedBias) - impulseScale * j.impulses.x;
            j.impulses.x += impulse;
            a.xy -= j.mass.x * impulse * perp; b.xy += j.mass.y * impulse * perp;
            a.z -= j.mass.z * impulse * pa; b.z += j.mass.w * impulse * pb;
        }
        else
        {
            precise vec2 velocity = relativeVelocity(a, b, ra, rb);
            precise vec2 speedBias = bias ? j.soft.x * ((bb.delta.xy - ba.delta.xy) + (rb - ra) + j.geometry.xy) : vec2(0);
            precise float scale = bias ? j.soft.y : 1; precise float impulseScale = bias ? j.soft.z : 0;
            precise float k11 = j.mass.x + j.mass.y + ra.y * ra.y * j.mass.z + rb.y * rb.y * j.mass.w;
            precise float k12 = -ra.y * ra.x * j.mass.z - rb.y * rb.x * j.mass.w;
            precise float k22 = j.mass.x + j.mass.y + ra.x * ra.x * j.mass.z + rb.x * rb.x * j.mass.w;
            precise float det = k11 * k22 - k12 * k12; precise float inverse = det != 0 ? 1.0 / det : 0;
            precise vec2 rhs = velocity + speedBias;
            precise vec2 solution = inverse * vec2(k22 * rhs.x - k12 * rhs.y, k11 * rhs.y - k12 * rhs.x);
            precise vec2 impulse = -scale * solution - impulseScale * j.impulses.xy;
            j.impulses.xy += impulse; applyImpulse(a, b, j.mass, ra, rb, impulse);
        }
    }
    writeVelocity(int(j.ids.x), a); writeVelocity(int(j.ids.y), b); joints[index] = j;
}
void processConstraint(uint index, uint stage) { if (stage < 6) solveContact(index, stage); else solveJoint(index, stage); }
void main()
{
    uint i = gl_GlobalInvocationID.x; uint stage = uint(control.y);
    if (stage < 2)
    {
        if (i >= uint(control.z)) return;
        Body b = bodies[i]; integrateBody(b, step, control); bodies[i] = b; return;
    }
    if (solve.w > 0)
    {
        if (i != 0) return;
        for (uint k = 0; k < uint(control.w); k++) processConstraint(uint(control.x) + k, stage);
    }
    else if (i < uint(control.w)) processConstraint(uint(control.x) + i, stage);
}
