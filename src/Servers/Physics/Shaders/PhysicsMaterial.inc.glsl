// SPDX-License-Identifier: MIT
vec2 mixSurfaceMaterial(vec2 a, vec2 b, uint flagsA, uint flagsB, uint options)
{
    uint modeF = (options >> 3) & 3u, modeB = (options >> 5) & 3u;
    precise float friction = 0, bounce = 0;
    if (modeF == 1u) { precise float product = a.x*b.x; friction = sqrt(product); }
    else if (modeF == 2u)
        friction = abs(min((flagsA & 1u) != 0 ? -a.x : a.x,
                           (flagsB & 1u) != 0 ? -b.x : b.x));
    if (modeB == 1u) bounce = a.y > b.y ? a.y : b.y;
    else if (modeB == 2u)
    {
        bounce = ((flagsA & 2u) != 0 ? -a.y : a.y) +
                 ((flagsB & 2u) != 0 ? -b.y : b.y);
        if (bounce < 0) bounce = 0; else if (bounce > 1) bounce = 1;
    }
    return vec2(friction, bounce);
}
