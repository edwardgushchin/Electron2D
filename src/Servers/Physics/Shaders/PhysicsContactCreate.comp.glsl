// SPDX-License-Identifier: MIT
#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsContactSlot.inc.glsl"
#include "PhysicsMaterial.inc.glsl"
layout(local_size_x = 64) in;
struct Request { ivec4 a; ivec4 b; vec4 material; uvec4 flags; };
struct Created { ContactSlot slot; vec4 material; ivec4 identity; };
struct Change { ContactSlot slot; ivec4 command; };
layout(std430, set = 0, binding = 0) readonly buffer Requests { Request requests[]; };
layout(std430, set = 0, binding = 1) readonly buffer Changes { Change changes[]; };
layout(std430, set = 1, binding = 0) buffer Slots { ContactSlot slots[]; };
layout(std430, set = 1, binding = 1) buffer FreeIDs { int freeIDs[]; };
layout(std430, set = 1, binding = 2) buffer Pool { ivec4 pool; ivec4 basePool; };
layout(std430, set = 1, binding = 3) buffer Results { Created created[]; };
layout(std430, set = 1, binding = 4) buffer Scan { int scan[]; };
layout(std140, set = 2, binding = 0) uniform Settings { ivec4 settings; ivec4 level; };
// pool = next, free count, error, created count; basePool = allocation start.
int rankType(int type) { return type == 2 ? 3 : type == 3 ? 2 : type; }
void main()
{
    int index = int(gl_GlobalInvocationID.x), operation = settings.x, count = settings.y;
    int capacity = settings.z, leaf = settings.w;
    if (operation == 0)
    {
        if (index >= count) return;
        Change c = changes[index]; int id = c.command.x;
        if (id < 0 || id >= pool.x || pool.y + index >= capacity) { atomicMax(pool.z, 1); return; }
        freeIDs[pool.y + index] = id; slots[id] = c.slot;
        return;
    }
    if (operation == 1)
    {
        if (index == 0 && pool.z == 0) pool.y += count;
        return;
    }
    if (operation == 2)
    {
        if (index != 0) return;
        // ponytail: mixed external CPU allocations replay serially; ordinary
        // destruction uses parallel append above. Batch this if replay dominates.
        for (int i = 0; i < count && pool.z == 0; i++)
        {
            Change c = changes[i]; int id = c.command.x;
            if (id < 0 || id >= capacity) { pool.z = 2; return; }
            if (c.command.y != 0)
            {
                int expected = pool.y > 0 ? freeIDs[pool.y - 1] : pool.x;
                if (id != expected) { pool.z = 3; return; }
                if (pool.y > 0) pool.y--; else pool.x++;
            }
            else
            {
                if (id >= pool.x || pool.y >= capacity) { pool.z = 4; return; }
                freeIDs[pool.y++] = id;
            }
            slots[id] = c.slot;
        }
        return;
    }
    if (operation == 3)
    {
        if (index >= leaf) return;
        scan[leaf + index] = 0;
        if (index >= count) return;
        Request r = requests[index];
        Created result = Created(ContactSlot(ivec4(-1),uvec4(0)),vec4(0),ivec4(-1,0,0,0));
        int ta = rankType(r.a.z), tb = rankType(r.b.z);
        if (ta < 0 || ta > 4 || tb < 0 || tb > 4) { atomicMax(pool.z, 5); created[index] = result; return; }
        if (ta >= 3 && tb >= 3) { created[index] = result; return; }
        if (ta < tb)
        {
            ivec4 temp = r.a; r.a = r.b; r.b = temp;
            r.material = r.material.zwxy; r.flags = r.flags.yxwz;
        }
        uint flags = r.flags.x | r.flags.y;
        result.slot = ContactSlot(ivec4(r.a.x,r.b.x,r.a.y,r.b.y),
            uvec4(0,(flags & 1u) != 0 ? 4u : 0u,(flags & 2u) != 0 ? 0x200000u : 0u,r.a.w == 2 || r.b.w == 2 ? 2u : 1u));
        result.material.xy = mixSurfaceMaterial(r.material.xy,r.material.zw,r.flags.z,r.flags.w,uint(level.z));
        created[index] = result; scan[leaf + index] = 1;
        return;
    }
    if (operation == 4)
    {
        if (index >= level.y) return;
        int node = level.x + index;
        scan[node] = scan[2*node] + scan[2*node+1];
        return;
    }
    if (operation == 5)
    {
        if (index != 0 || pool.z != 0) return;
        int accepted = scan[1], reused = min(pool.y,accepted);
        if (pool.x + accepted - reused > capacity) { pool.z = 6; return; }
        basePool = ivec4(pool.xy,0,0);
        pool.x += accepted - reused; pool.y -= reused; pool.w = accepted;
        return;
    }
    if (index >= count || atomicAdd(pool.z, 0) != 0 || scan[leaf + index] == 0) return;
    // ponytail: each allocation walks O(log n) scan ancestors; use a workgroup
    // prefix scan if this part dominates creation cost.
    int prefix = 0;
    for (int node = leaf + index; node > 1; node /= 2)
        if ((node & 1) != 0) prefix += scan[node-1];
    int id = prefix < basePool.y ? freeIDs[basePool.y-prefix-1] : basePool.x+prefix-basePool.y;
    if (id < 0 || id >= capacity) { atomicMax(pool.z, 7); return; }
    Created result = created[index];
    result.slot.state.x = slots[id].state.x + 1u;
    result.identity.x = id;
    slots[id] = result.slot; created[index] = result;
}
