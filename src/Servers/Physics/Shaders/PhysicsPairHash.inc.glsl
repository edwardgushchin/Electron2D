// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi
// SPDX-License-Identifier: MIT
// Split-word arithmetic preserves the pair hash without shaderInt64.
uvec2 multiply64(uvec2 v, uvec2 c)
{
    uint high, low;
    umulExtended(v.x, c.x, high, low);
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
