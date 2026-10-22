// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi
// SPDX-License-Identifier: MIT
// Split-word arithmetic preserves the pair hash without shaderInt64.
uvec2 multiply64(uvec2 v, uvec2 c)
{
    // Half-word products also translate to shader backends without extended multiply.
    uint a0 = v.x & 0xffffu, a1 = v.x >> 16;
    uint b0 = c.x & 0xffffu, b1 = c.x >> 16;
    uint w0 = a0 * b0;
    uint t = a1 * b0 + (w0 >> 16);
    uint w1 = (t & 0xffffu) + a0 * b1;
    uint high = a1 * b1 + (t >> 16) + (w1 >> 16);
    uint low = (w1 << 16) | (w0 & 0xffffu);
    return uvec2(low, high + v.x*c.y + v.y*c.x);
}
uint pairHash(uvec2 key)
{
    key.x ^= key.y >> 1;
    key = multiply64(key, uvec2(0xed558ccd, 0xff51afd7));
    key.x ^= key.y >> 1;
    key = multiply64(key, uvec2(0x1a85ec53, 0xc4ceb9fe));
    return key.x ^ (key.y >> 1);
}
