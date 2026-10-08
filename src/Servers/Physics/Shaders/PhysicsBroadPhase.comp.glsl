// SPDX-FileCopyrightText: 2023 Erin Catto
// SPDX-FileCopyrightText: 2025 Ikpil Choi
// SPDX-License-Identifier: MIT
#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsPairHash.inc.glsl"
layout(local_size_x = 64) in;
struct Node { vec4 bounds; int escape; int proxy; int hasCategory; int shape; };
struct Query { vec4 bounds; int proxy; int offset; int count; int capacity; ivec4 shape; };
struct Shape { uvec4 bits; ivec4 bodyGroupJoints; ivec4 flags; };
struct Joint { ivec4 bodiesNext; ivec4 flags; };
layout(std430, set = 0, binding = 0) readonly buffer Tree { Node nodes[]; };
layout(std430, set = 0, binding = 1) readonly buffer Shapes { Shape shapes[]; };
layout(std430, set = 0, binding = 2) readonly buffer Joints { Joint joints[]; };
layout(std430, set = 0, binding = 3) readonly buffer ExistingPairs { uint existing[]; };
layout(std430, set = 0, binding = 4) readonly buffer PairKeys { uvec2 pairKeys[]; };
layout(std430, set = 1, binding = 0) buffer Queries { Query queries[]; };
layout(std430, set = 1, binding = 1) buffer Candidates { int candidates[]; };
layout(std140, set = 2, binding = 0) uniform Settings { ivec4 settings; };

bool contactExists(int a, int b)
{
    uvec2 key = uvec2(max(a,b), min(a,b));
    uint mask = uint(settings.w), slot = pairHash(key) & mask;
    for (uint i = 0; i <= mask; i++)
    {
        uint id = existing[slot];
        if (id == 0) return false;
        if (id != 0xffffffffu && all(equal(pairKeys[id - 1], key))) return true;
        slot = (slot + 1) & mask;
    }
    return false;
}
bool acceptPair(Query q, Shape a, Node n)
{
    if (n.proxy == q.proxy) return false;
    Shape b = shapes[n.shape];
    bool otherMoved = (b.flags.x & 2) != 0;
    if ((q.proxy & 3) == 2)
    {
        if ((n.proxy & 3) == 2 && n.proxy < q.proxy && otherMoved) return false;
    }
    else if (otherMoved) return false;
    if (contactExists(q.shape.x, n.shape)) return false;
    if (a.bodyGroupJoints.x == b.bodyGroupJoints.x || ((a.flags.x | b.flags.x) & 1) != 0) return false;
    if (a.bodyGroupJoints.y != 0 && a.bodyGroupJoints.y == b.bodyGroupJoints.y)
    {
        if (a.bodyGroupJoints.y < 0) return false;
    }
    else if (!any(notEqual(a.bits.zw & b.bits.xy, uvec2(0))) ||
             !any(notEqual(a.bits.xy & b.bits.zw, uvec2(0)))) return false;

    // Walk the smaller adjacency list, including mixed collideConnected joints.
    bool first = a.bodyGroupJoints.w < b.bodyGroupJoints.w;
    int jointKey = first ? a.bodyGroupJoints.z : b.bodyGroupJoints.z;
    int otherBody = first ? b.bodyGroupJoints.x : a.bodyGroupJoints.x;
    while (jointKey != -1)
    {
        Joint j = joints[jointKey >> 1];
        bool edgeA = (jointKey & 1) == 0;
        if (j.flags.x == 0 && (edgeA ? j.bodiesNext.y : j.bodiesNext.x) == otherBody) return false;
        jointKey = edgeA ? j.bodiesNext.z : j.bodiesNext.w;
    }
    return true;
}

void main()
{
    uint id = gl_GlobalInvocationID.x;
    if (id >= settings.x) return;
    Query q = queries[id];
    int count = 0;
    if (q.proxy != -1)
    {
        Shape shape = shapes[q.shape.x];
        // Dynamic proxies query kinematic, static, then dynamic trees.
        // All other types query only dynamic proxies.
        int at = (q.proxy & 3) == 2 ? 0 : settings.y;
        while (at < settings.z)
        {
            Node n = nodes[at];
            bool overlap = all(lessThanEqual(n.bounds.xy, q.bounds.zw)) &&
                           all(lessThanEqual(q.bounds.xy, n.bounds.zw));
            if (n.hasCategory == 0 || !overlap) { at = n.escape; continue; }
            if (n.proxy != -1 && acceptPair(q, shape, n))
            {
                if (count < q.capacity) candidates[q.offset + count] = n.proxy;
                count++;
            }
            at++;
        }
    }
    queries[id].count = count;
}
