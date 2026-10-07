struct Body { vec4 velocity; vec4 delta; vec4 force; vec4 properties; vec4 flags; };

// Correct the native approximate divide/root before stiff constraints amplify their error.
float divideRefined(float numerator, float denominator)
{
    precise float q = numerator / denominator;
    if (isinf(q) || isnan(q) || isinf(denominator)) return q;
    precise float residual = fma(-q, denominator, numerator);
    precise float result = q + residual / denominator;
    return result;
}
float sqrtRefined(float value)
{
    precise float s = sqrt(value);
    if (s == 0 || isinf(s) || isnan(s)) return s;
    precise float residual = fma(-s, s, value);
    precise float result = s + divideRefined(residual, 2.0 * s);
    return result;
}

// Preserve separate scalar multiply/add operations.
void integrateBody(inout Body b, vec4 step, vec4 control)
{
    precise vec2 v = b.velocity.xy;
    precise float w = b.velocity.z;
    uint locks = uint(b.velocity.w);
    if (uint(control.y) == 0)
    {
        precise float ld = divideRefined(1.0, 1.0 + step.z * b.properties.z);
        precise float ad = divideRefined(1.0, 1.0 + step.z * b.properties.w);
        precise vec2 dv = (step.z * b.force.w) * b.force.xy +
            (step.z * (b.force.w > 0.0 ? b.properties.y : 0.0)) * step.xy;
        v = dv + ld * v;
        w = step.z * b.properties.x * b.force.z + ad * w;
        precise float v2 = v.x * v.x + v.y * v.y;
        if (v2 > step.w * step.w)
        {
            v *= divideRefined(step.w, sqrtRefined(v2));
            b.flags.x = float(uint(b.flags.x) | 32u);
        }
        if (w * w > control.x * control.x && (uint(b.flags.x) & 128u) == 0)
        {
            w *= divideRefined(control.x, abs(w));
            b.flags.x = float(uint(b.flags.x) | 32u);
        }
    }
    if ((locks & 1u) != 0) v.x = 0;
    if ((locks & 2u) != 0) v.y = 0;
    if ((locks & 4u) != 0) w = 0;
    if (uint(control.y) == 1)
    {
        precise vec2 dp = b.delta.xy + step.z * v;
        b.delta.xy = dp;
        precise float angle = step.z * w;
        precise vec2 q = vec2(b.delta.z - angle * b.delta.w, b.delta.w + angle * b.delta.z);
        precise float magnitude = sqrtRefined(q.y * q.y + q.x * q.x);
        b.delta.zw = q * (magnitude > 0.0 ? divideRefined(1.0, magnitude) : 0.0);
    }
    b.velocity.xyz = vec3(v, w);
}
