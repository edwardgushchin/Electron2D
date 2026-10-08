#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsBody.inc.glsl"
layout(local_size_x = 64) in;
#include "PhysicsContact.inc.glsl"
struct ContactInput { vec4 ids; vec4 mass; vec4 material; vec4 warm; vec4 source; vec4 offset; vec4 surfaceA; vec4 surfaceB; };
struct Manifold { vec4 normal; vec4 anchor1; vec4 point1; vec4 anchor2; vec4 point2; };
struct Joint {
    vec4 ids; vec4 mass; vec4 frameA; vec4 frameB; vec4 geometry;
    vec4 soft; vec4 spring; vec4 motor; vec4 impulses; vec4 limits; vec4 poseA; vec4 poseB; vec4 policyBias; vec4 policyForce;
};
layout(std430, set = 1, binding = 0) buffer Bodies { Body bodies[]; };
layout(std430, set = 1, binding = 1) buffer Contacts { Contact contacts[]; };
layout(std430, set = 1, binding = 2) buffer Joints { Joint joints[]; };
layout(std430, set = 0, binding = 0) readonly buffer Inputs { ContactInput inputs[]; };
layout(std430, set = 0, binding = 1) readonly buffer Manifolds { Manifold manifolds[]; };
layout(std430, set = 0, binding = 2) readonly buffer Fallbacks { Manifold fallbacks[]; };
layout(std430, set = 0, binding = 3) readonly buffer Matched { ContactHistory matched[]; };
layout(std140, set = 2, binding = 0) uniform Step { vec4 step; vec4 control; vec4 solve; vec4 preparation; };

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
vec2 relativeVelocity(vec3 a, vec3 b, vec2 ra, vec2 rb) { precise vec2 r = (b.xy + angular(b.z, rb)) - (a.xy + angular(a.z, ra)); return r; }

vec3 makeSoft(float hertz, float damping)
{
    if (hertz == 0) return vec3(0);
    precise float omega = 2.0 * 3.14159265359 * hertz;
    precise float a1 = 2.0 * damping + step.z * omega;
    precise float a2 = step.z * omega * a1;
    precise float a3 = divideRefined(1.0, 1.0 + a2);
    precise vec3 result = vec3(divideRefined(omega, a1), a2 * a3, a3);
    return result;
}
void preparePoint(Contact c, vec3 a, vec3 b, vec4 anchors, inout vec4 params, inout vec4 impulses, float warm)
{
    vec2 n = c.normal.xy, t = vec2(n.y, -n.x), ra = anchors.xy, rb = anchors.zw;
    precise float rnA = cross2(ra, n), rnB = cross2(rb, n);
    precise float kn = c.mass.x + c.mass.y + c.mass.z * rnA * rnA + c.mass.w * rnB * rnB;
    precise float rtA = cross2(ra, t), rtB = cross2(rb, t);
    precise float kt = c.mass.x + c.mass.y + c.mass.z * rtA * rtA + c.mass.w * rtB * rtB;
    params.x = kn > 0 ? divideRefined(1.0, kn) : 0; params.y = kt > 0 ? divideRefined(1.0, kt) : 0;
    params.z -= dot2(rb - ra, n); params.w = dot2(n, relativeVelocity(a, b, ra, rb));
    impulses.xy *= warm; impulses.z = 0;
}
void prepareContact(uint index)
{
    ContactInput packet = inputs[index];
    int source = int(packet.source.x);
    Manifold m;
    if (source >= 0) m = manifolds[source]; else m = fallbacks[-source - 1];
    bool first = source < 0 || packet.source.z == m.anchor1.w;
    bool second = source >= 0 && packet.source.w == m.anchor1.w;
    vec4 a1 = first ? m.anchor1 : m.anchor2, b1 = first ? m.point1 : m.point2;
    vec4 a2 = second ? m.anchor1 : m.anchor2, b2 = second ? m.point1 : m.point2;
    Contact c;
    c.surfaceA = vec4(packet.surfaceA.xyz, 0); c.surfaceB = vec4(packet.surfaceB.xyz, 0);
    c.ids = packet.ids; c.mass = packet.mass; c.normal = vec4(m.normal.xy, packet.material.xy);
    c.rolling = vec4(packet.material.z, 0, packet.source.y, packet.material.w); c.soft = vec4(0);
    c.anchors1 = vec4(a1.xy, b1.xy) - packet.offset; c.anchors2 = vec4(a2.xy, b2.xy) - packet.offset;
    c.params1 = vec4(0, 0, a1.z, 0); c.params2 = vec4(0, 0, a2.z, 0);
    c.impulses1 = vec4(packet.warm.xy, 0, packet.source.z); c.impulses2 = vec4(packet.warm.zw, 0, packet.source.w);
    if (source >= 0)
    {
        ContactHistory history = matched[source];
        c.impulses1.xy = first ? history.impulses.xy : history.impulses.zw;
        c.impulses2.xy = second ? history.impulses.xy : history.impulses.zw;
        c.rolling.z = history.features.w;
    }
    precise float hertz = min(preparation.x, 0.125 * solve.y), damping = preparation.y;
    if (c.ids.x < 0 || c.ids.y < 0) hertz *= 2.0;
    else if (c.ids.w == 0 && preparation.z != 0)
    {
        precise float ratio = 1;
        if (c.mass.x < c.mass.y) ratio = max(0.5, c.mass.x / c.mass.y);
        else if (c.mass.y < c.mass.x) ratio = max(0.5, c.mass.y / c.mass.x);
        hertz *= ratio; damping *= ratio;
    }
    c.soft.xyz = makeSoft(hertz, damping);
    precise float k = c.mass.z + c.mass.w;
    c.rolling.y = k > 0 ? divideRefined(1.0, k) : 0;
    float warm = (uint(preparation.w) & 1u) != 0 ? 1 : 0;
    c.rolling.z *= warm;
    vec3 a = readBody(int(c.ids.x)).velocity.xyz + c.surfaceA.xyz, b = readBody(int(c.ids.y)).velocity.xyz + c.surfaceB.xyz;
    preparePoint(c, a, b, c.anchors1, c.params1, c.impulses1, warm);
    if (c.ids.z > 1) preparePoint(c, a, b, c.anchors2, c.params2, c.impulses2, warm);
    else { c.anchors2 = vec4(0); c.params2 = vec4(0); c.impulses2 = vec4(0); }
    contacts[index] = c;
}
void prepareJoint(uint index)
{
    Joint j = joints[index];
    j.soft.xyz = j.policyBias.x >= 0 ? vec3(j.policyBias.x * solve.y, 1, 0) : makeSoft(min(j.soft.x, 0.25 * solve.y), j.soft.y);
    if (j.ids.z == 0) { joints[index] = j; return; }
    j.spring.xyz = makeSoft(j.spring.x, j.spring.y);
    j.frameA.xy = rotate(j.poseA.xy, j.frameA.xy - j.poseA.zw);
    j.frameA.zw = rotate(j.poseA.xy, j.frameA.zw);
    j.frameB.xy = rotate(j.poseB.xy, j.frameB.xy - j.poseB.zw);
    j.frameB.zw = rotate(j.poseB.xy, j.frameB.zw);
    precise vec2 delta = j.geometry.zw - j.geometry.xy;
    precise float angularMass = j.mass.z + j.mass.w;
    angularMass = angularMass > 0 ? divideRefined(1.0, angularMass) : 0;
    j.geometry = vec4(delta, angularMass, 0);
    if (j.ids.z == 2)
    {
        vec2 ra = j.frameA.xy, rb = j.frameB.xy;
        precise vec2 d = delta + (rb - ra);
        vec2 axis = j.frameA.zw, perpendicular = vec2(-axis.y, axis.x);
        precise float s1 = cross2(d + ra, perpendicular), s2 = cross2(rb, perpendicular);
        precise float kp = j.mass.x + j.mass.y + j.mass.z * s1 * s1 + j.mass.w * s2 * s2;
        precise float a1 = cross2(d + ra, axis), a2 = cross2(rb, axis);
        precise float ka = j.mass.x + j.mass.y + j.mass.z * a1 * a1 + j.mass.w * a2 * a2;
        j.geometry.z = ka > 0 ? divideRefined(1.0, ka) : 0; j.geometry.w = kp > 0 ? divideRefined(1.0, kp) : 0;
        j.soft.w = angularMass;
    }
    if ((uint(preparation.w) & 2u) == 0) { j.impulses = vec4(0); j.limits = vec4(0); }
    joints[index] = j;
}

void contactPoint(inout vec3 a, inout vec3 b, Contact c, vec4 anchors, vec4 params, inout vec4 impulses,
    inout float totalTangent, Body ba, Body bb, uint stage)
{
    vec2 ra = anchors.xy; vec2 rb = anchors.zw; vec2 n = c.normal.xy; vec2 t = vec2(n.y, -n.x);
    if (stage == 2)
    {
        precise vec2 p = impulses.x * n + impulses.y * t;
        applyImpulse(a, b, c.mass, ra, rb, p);
        impulses.z += impulses.x; totalTangent += impulses.y;
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
void contactFriction(inout vec3 a, inout vec3 b, Contact c, vec4 anchors, vec4 params, inout vec4 impulses, inout float totalTangent)
{
    vec2 t = vec2(c.normal.y, -c.normal.x);
    precise float speed = dot2(relativeVelocity(a, b, anchors.xy, anchors.zw), t) - c.normal.w;
    precise float limit = c.normal.z * impulses.x;
    precise float updated = clamp(impulses.y - params.y * speed, -limit, limit);
    precise float impulse = updated - impulses.y; impulses.y = updated; totalTangent += impulse;
    applyImpulse(a, b, c.mass, anchors.xy, anchors.zw, impulse * t);
}
void solveContact(uint index, uint stage)
{
    Contact c = contacts[index];
    Body ba = readBody(int(c.ids.x)); Body bb = readBody(int(c.ids.y));
    precise vec3 a = ba.velocity.xyz + c.surfaceA.xyz; precise vec3 b = bb.velocity.xyz + c.surfaceB.xyz;
    contactPoint(a, b, c, c.anchors1, c.params1, c.impulses1, c.surfaceA.w, ba, bb, stage);
    if (c.ids.z > 1) contactPoint(a, b, c, c.anchors2, c.params2, c.impulses2, c.surfaceB.w, ba, bb, stage);
    if (stage == 2)
    {
        a.z -= c.mass.z * c.rolling.z; b.z += c.mass.w * c.rolling.z;
    }
    else if (stage == 3 || stage == 4)
    {
        contactFriction(a, b, c, c.anchors1, c.params1, c.impulses1, c.surfaceA.w);
        if (c.ids.z > 1) contactFriction(a, b, c, c.anchors2, c.params2, c.impulses2, c.surfaceB.w);
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
    precise float a = divideRefined(min(ay, ax), max(ay, ax));
    precise float s = a * a; precise float c = s * a; precise float fourth = s * s;
    precise float r = 0.024840285 * fourth + 0.18681418;
    precise float t = -0.094097948 * fourth - 0.33213072;
    r = r * s + t; r = r * c + a;
    if (ay > ax) r = 1.57079637 - r;
    if (q.x < 0) r = 3.14159274 - r;
    if (q.y < 0) r = -r;
    return r;
}
float limitImpulse(float error, float velocity, float mass, inout float accumulated, vec3 softness, bool bias, float correction)
{
    precise float speedBias = 0; precise float scale = 1; precise float impulseScale = 0;
    if (error > 0) speedBias = error * solve.y;
    else if (bias) { speedBias = correction; scale = softness.y; impulseScale = softness.z; }
    precise float impulse = -scale * mass * (velocity + speedBias) - impulseScale * accumulated;
    precise float previous = accumulated;
    accumulated = max(previous + impulse, 0.0);
    return accumulated - previous;
}
void applyWheelImpulse(inout vec3 a, inout vec3 b, vec4 mass, vec2 axis, float leverA, float leverB, float impulse)
{
    precise vec3 va = a, vb = b;
    precise vec2 p = impulse * axis;
    precise float la = impulse * leverA, lb = impulse * leverB;
    va.xy -= mass.x * p; va.z -= mass.z * la;
    vb.xy += mass.y * p; vb.z += mass.w * lb;
    a = va; b = vb;
}
vec2 clampJointVector(vec2 value, float limit)
{
    if (limit == 3.402823466e38) return value;
    float size = length(value); return size > limit ? value * (limit / size) : value;
}
float jointImpulseLimit(float force) { return force == 3.402823466e38 ? force : min(3.402823466e38, force * step.z); }
void limitJoint(inout Joint j, bool wheel, out vec2 linearChange, out float angularChange)
{
    float axial = j.impulses.z + j.limits.x - j.limits.y;
    vec2 before = wheel ? vec2(axial, j.impulses.x) : j.impulses.xy;
    vec2 after = clampJointVector(before, jointImpulseLimit(j.policyForce.x));
    linearChange = after - before;
    if (wheel)
    {
        float scale = axial != 0 ? after.x / axial : 1;
        j.impulses.z *= scale; j.limits.xy *= scale; j.impulses.x = after.y;
        float cap = jointImpulseLimit(j.policyForce.y), previous = j.impulses.w;
        j.impulses.w = clamp(previous, -cap, cap); angularChange = j.impulses.w - previous;
    }
    else
    {
        j.impulses.xy = after;
        float total = axial + j.impulses.w, cap = jointImpulseLimit(j.policyForce.y);
        float scale = abs(total) > cap ? cap / abs(total) : 1;
        j.impulses.zw *= scale; j.limits.xy *= scale; angularChange = (scale - 1) * total;
    }
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
        vec2 ignoredLinear; float ignoredAngular; limitJoint(j, wheel, ignoredLinear, ignoredAngular);
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
                if (wheel) applyWheelImpulse(a, b, j.mass, axis, aa, ab, impulse);
                else { a.z -= j.mass.z * impulse; b.z += j.mass.w * impulse; }
            }
        }
        vec2 limitedBias = vec2(0);
        if (wheel && bias && j.policyBias.y < 3.402823466e38)
        {
            float error = (flags & 4u) != 0 ? position - clamp(position, j.motor.z, j.motor.w) : 0;
            limitedBias = clampJointVector(j.soft.x * vec2(dot2(perp, d), error), j.policyBias.y);
        }
        if ((flags & 4u) != 0 && (wheel || !fixedRotation))
        {
            precise float velocity = wheel ? dot2(axis, b.xy - a.xy) + ab * b.z - aa * a.z : b.z - a.z;
            precise float impulse = limitImpulse(position - j.motor.z, velocity, j.geometry.z, j.limits.x, j.soft.xyz, bias, wheel && j.policyBias.y < 3.402823466e38 ? limitedBias.y : clamp(j.soft.x * (position - j.motor.z), -j.policyBias.z, j.policyBias.z));
            if (wheel) applyWheelImpulse(a, b, j.mass, axis, aa, ab, impulse);
            else { a.z -= j.mass.z * impulse; b.z += j.mass.w * impulse; }
            velocity = wheel ? dot2(axis, a.xy - b.xy) + aa * a.z - ab * b.z : a.z - b.z;
            impulse = limitImpulse(j.motor.w - position, velocity, j.geometry.z, j.limits.y, j.soft.xyz, bias, wheel && j.policyBias.y < 3.402823466e38 ? -limitedBias.y : clamp(j.soft.x * (j.motor.w - position), -j.policyBias.z, j.policyBias.z));
            if (wheel) applyWheelImpulse(a, b, j.mass, axis, aa, ab, -impulse);
            else { a.z += j.mass.z * impulse; b.z -= j.mass.w * impulse; }
        }
        if (wheel)
        {
            precise float speedBias = bias ? (j.policyBias.y < 3.402823466e38 ? limitedBias.x : j.soft.x * dot2(perp, d)) : 0;
            precise float scale = bias ? j.soft.y : 1; precise float impulseScale = bias ? j.soft.z : 0;
            precise float velocity = dot2(perp, b.xy - a.xy) + pb * b.z - pa * a.z;
            precise float impulse = -scale * j.geometry.w * (velocity + speedBias) - impulseScale * j.impulses.x;
            j.impulses.x += impulse;
            applyWheelImpulse(a, b, j.mass, perp, pa, pb, impulse);
        }
        else
        {
            precise vec2 velocity = relativeVelocity(a, b, ra, rb);
            precise vec2 speedBias = bias ? clampJointVector(j.soft.x * ((bb.delta.xy - ba.delta.xy) + (rb - ra) + j.geometry.xy), j.policyBias.y) : vec2(0);
            precise float scale = bias ? j.soft.y : 1; precise float impulseScale = bias ? j.soft.z : 0;
            precise float k11 = j.mass.x + j.mass.y + ra.y * ra.y * j.mass.z + rb.y * rb.y * j.mass.w;
            precise float k12 = -ra.y * ra.x * j.mass.z - rb.y * rb.x * j.mass.w;
            precise float k22 = j.mass.x + j.mass.y + ra.x * ra.x * j.mass.z + rb.x * rb.x * j.mass.w;
            k11 += j.policyBias.w; k22 += j.policyBias.w;
            precise float det = k11 * k22 - k12 * k12; precise float inverse = det != 0 ? divideRefined(1.0, det) : 0;
            precise vec2 rhs = velocity + speedBias + j.policyBias.w * j.impulses.xy;
            precise vec2 solution = inverse * vec2(k22 * rhs.x - k12 * rhs.y, k11 * rhs.y - k12 * rhs.x);
            precise vec2 impulse = -scale * solution - impulseScale * j.impulses.xy;
            j.impulses.xy += impulse; applyImpulse(a, b, j.mass, ra, rb, impulse);
        }
    }
    if (stage != 6)
    {
        vec2 change; float angularChange; limitJoint(j, wheel, change, angularChange);
        if (wheel)
        {
            applyWheelImpulse(a, b, j.mass, axis, aa, ab, change.x);
            applyWheelImpulse(a, b, j.mass, perp, pa, pb, change.y);
            a.z -= j.mass.z * angularChange; b.z += j.mass.w * angularChange;
        }
        else
        {
            applyImpulse(a, b, j.mass, ra, rb, change);
            a.z -= j.mass.z * angularChange; b.z += j.mass.w * angularChange;
        }
    }
    writeVelocity(int(j.ids.x), a); writeVelocity(int(j.ids.y), b); joints[index] = j;
}
void processConstraint(uint index, uint stage) { if (stage < 6) solveContact(index, stage); else solveJoint(index, stage); }
void main()
{
    uint i = gl_GlobalInvocationID.x; uint stage = uint(control.y);
    if (stage == 9 || stage == 10)
    {
        if (i < uint(control.w))
        {
            if (stage == 9) prepareContact(uint(control.x) + i); else prepareJoint(uint(control.x) + i);
        }
        return;
    }
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
