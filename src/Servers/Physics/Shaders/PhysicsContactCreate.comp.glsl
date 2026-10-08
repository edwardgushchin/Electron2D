// SPDX-License-Identifier: MIT
#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsContactSlot.inc.glsl"
#include "PhysicsMaterial.inc.glsl"
layout(local_size_x = 64) in;
struct Request { ivec4 a; ivec4 b; vec4 material; uvec4 flags; };
struct Created { ContactSlot slot; vec4 material; ivec4 identity; ivec4 links; };
struct LinkUpdate { ivec4 identity; ivec4 value; };
struct CollisionPair { ivec4 ids; vec4 poseA; vec4 poseB; vec4 offset; };
struct Removed { ivec4 identity; ivec4 a; ivec4 b; };
struct BodyLinks { int head; int count; int nextHead; int nextCount; };
struct Change { ContactSlot slot; ivec4 command; };
layout(std430, set = 0, binding = 0) readonly buffer Requests { Request requests[]; };
layout(std430, set = 0, binding = 1) readonly buffer Changes { Change changes[]; };
layout(std430, set = 0, binding = 2) readonly buffer LinkUpdates { LinkUpdate linkUpdates[]; };
layout(std430, set = 0, binding = 3) readonly buffer CollisionPairs { CollisionPair pairs[]; };
layout(std430, set = 1, binding = 0) buffer Slots { ContactSlot slots[]; };
layout(std430, set = 1, binding = 1) buffer FreeIDs { int freeIDs[]; };
layout(std430, set = 1, binding = 2) buffer Pool { ivec4 pool; ivec4 basePool; };
layout(std430, set = 1, binding = 3) buffer Results { Created created[]; };
layout(std430, set = 1, binding = 4) buffer Scan { int scan[]; };
layout(std430, set = 1, binding = 5) buffer Links { ivec4 links[]; };
layout(std430, set = 1, binding = 6) buffer Bodies { BodyLinks bodies[]; };
layout(std430, set = 1, binding = 7) buffer Removals { Removed removed[]; };
layout(std140, set = 2, binding = 0) uniform Settings { ivec4 settings; ivec4 level; };
// pool = next, free count, error, operation count; basePool = allocation start.
int rankType(int type) { return type == 2 ? 3 : type == 3 ? 2 : type; }
ivec2 endpoint(int i) { return ivec2(scan[2*i],scan[2*i+1]); }
void setEndpoint(int i, ivec2 value) { scan[2*i] = value.x; scan[2*i+1] = value.y; }
bool greater(ivec2 a, ivec2 b) { return a.x > b.x || (a.x == b.x && a.y > b.y); }
int edgeKey(int ordinal) { return 2*created[ordinal/2].identity.x + (ordinal&1); }
int scanPrefix(int index)
{
    int prefix = 0;
    for (int node = settings.w + index; node > 1; node /= 2)
        if ((node & 1) != 0) prefix += scan[node-1];
    return prefix;
}
bool ownsEdge(int key, int body)
{
    return key >= 0 && key/2 < pool.x && slots[key/2].shapeBody[2+(key&1)] == body;
}
shared ivec2 sortTile[64];
void mergeTile(int width, int offset, int index, int local)
{
    for (; offset > 0; offset /= 2)
    {
        ivec2 a = sortTile[local], b = sortTile[local ^ offset];
        bool takeMin = ((index & width) == 0) == ((local & offset) == 0);
        if (takeMin ? greater(a,b) : greater(b,a)) a = b;
        barrier(); sortTile[local] = a; barrier();
    }
}
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
        Created result = Created(ContactSlot(ivec4(-1),uvec4(0)),vec4(0),ivec4(-1,0,0,0),ivec4(-1,-1,0,0));
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
    if (operation == 7)
    {
        if (index >= count) return;
        LinkUpdate update = linkUpdates[index]; int id = update.identity.x;
        if (update.identity.y == 0) links[id] = update.value;
        else { bodies[id].head = update.value.x; bodies[id].count = update.value.y; }
        return;
    }
    if (operation == 8)
    {
        if (index >= 2*leaf) return;
        int request = index/2, side = index&1;
        int body = request < count && created[request].identity.x >= 0 ? created[request].slot.shapeBody[2+side] : 2147483647;
        setEndpoint(index,ivec2(body,index));
        return;
    }
    if (operation == 12)
    {
        int local = int(gl_LocalInvocationID.x);
        sortTile[local] = index < count ? endpoint(index) : ivec2(2147483647);
        barrier();
        if (level.y == 0)
            for (int width = 2; width <= min(64,count); width *= 2) mergeTile(width,width/2,index,local);
        else mergeTile(level.y,32,index,local);
        if (index < count) setEndpoint(index,sortTile[local]);
        return;
    }
    if (operation == 9)
    {
        if (index >= count) return;
        int other = index ^ level.x;
        if (other <= index) return;
        ivec2 a = endpoint(index), b = endpoint(other);
        bool ascending = (index & level.y) == 0;
        if (ascending ? greater(a,b) : greater(b,a)) { setEndpoint(index,b); setEndpoint(other,a); }
        return;
    }
    if (operation == 10)
    {
        if (index >= count || atomicAdd(pool.z,0) != 0) return;
        ivec2 e = endpoint(index); int body = e.x;
        if (body == 2147483647) return;
        if (body < 0 || body >= bodies.length()) { atomicMax(pool.z,8); return; }
        // Each body segment preserves pair order. Binary search finds its rank
        // without atomics whose order would depend on GPU scheduling.
        int low = 0, high = index;
        while (low < high) { int mid = low+(high-low)/2; if (endpoint(mid).x < body) low = mid+1; else high = mid; }
        int ordinal = e.y, side = ordinal&1, id = created[ordinal/2].identity.x, key = 2*id+side;
        int next = index > low ? edgeKey(endpoint(index-1).y) : bodies[body].head;
        if (next < -1 || (next >= 0 && (next/2 >= pool.x || slots[next/2].shapeBody[2+(next&1)] != body)))
        { atomicMax(pool.z,9); return; }
        bool last = index+1 == count || endpoint(index+1).x != body;
        int prev = last ? -1 : edgeKey(endpoint(index+1).y);
        links[id][2*side] = prev; links[id][2*side+1] = next;
        if (index == low && next >= 0) links[next/2][2*(next&1)] = key;
        int total = bodies[body].count + index-low+1;
        created[ordinal/2].links[side] = next;
        created[ordinal/2].links[2+side] = total;
        if (last) { bodies[body].nextHead = key; bodies[body].nextCount = total; }
        return;
    }
    if (operation == 11)
    {
        if (index >= count || atomicAdd(pool.z,0) != 0) return;
        int body = endpoint(index).x;
        if (body == 2147483647 || (index+1 < count && endpoint(index+1).x == body)) return;
        bodies[body].head = bodies[body].nextHead; bodies[body].count = bodies[body].nextCount;
        return;
    }
    if (operation == 13)
    {
        if (index >= count) return;
        ivec4 pair = pairs[index].ids;
        if ((pair.z & 1) != 0) return;
        if (pair.x < 0 || pair.x >= pool.x || slots[pair.x].state.x != uint(pair.y) || slots[pair.x].shapeBody.x < 0)
        { atomicMax(pool.z,10); return; }
        slots[pair.x].state.y |= 0x80000000u;
        return;
    }
    if (operation == 14)
    {
        if (index >= leaf) return;
        scan[leaf+index] = index < count && (slots[index].state.y & 0x80000000u) != 0 ? 1 : 0;
        return;
    }
    if (operation == 15)
    {
        if (index != 0 || pool.z != 0) return;
        if (scan[1] != count || pool.y + count > capacity) { pool.z = 11; return; }
        basePool = ivec4(pool.xy,0,0); pool.y += count; pool.w = count;
        return;
    }
    if (operation == 16)
    {
        if (index >= count || atomicAdd(pool.z,0) != 0 || scan[leaf+index] == 0) return;
        ContactSlot slot = slots[index];
        removed[scanPrefix(index)] = Removed(ivec4(index,int(slot.state.x),slot.shapeBody.zw),ivec4(-1),ivec4(-1));
        return;
    }
    if (operation == 17)
    {
        if (index >= 2*leaf) return;
        int request = index/2, side = index&1;
        int body = request < count ? removed[request].identity[2+side] : 2147483647;
        setEndpoint(index,ivec2(body,index));
        return;
    }
    if (operation == 18)
    {
        if (index >= count || atomicAdd(pool.z,0) != 0) return;
        int body = endpoint(index).x;
        if (body == 2147483647 || (index > 0 && endpoint(index-1).x == body)) return;
        if (body < 0 || body >= bodies.length()) { atomicMax(pool.z,12); return; }
        // ponytail: removals within one body run serially to preserve every
        // publication prefix; parallelize high-degree bodies if this dominates.
        for (int cursor = index; cursor < count && endpoint(cursor).x == body; cursor++)
        {
            int ordinal = endpoint(cursor).y, request = ordinal/2, side = ordinal&1;
            int id = removed[request].identity.x, key = 2*id+side;
            int prev = links[id][2*side], next = links[id][2*side+1];
            if ((prev == -1 ? bodies[body].head != key : !ownsEdge(prev,body) || links[prev/2][2*(prev&1)+1] != key) ||
                (next != -1 && (!ownsEdge(next,body) || links[next/2][2*(next&1)] != key)) || bodies[body].count <= 0)
            { atomicMax(pool.z,13); return; }
            int head = bodies[body].head == key ? next : bodies[body].head;
            int remaining = bodies[body].count - 1;
            if (side == 0) removed[request].a = ivec4(prev,next,head,remaining);
            else removed[request].b = ivec4(prev,next,head,remaining);
            if (prev >= 0) links[prev/2][2*(prev&1)+1] = next;
            if (next >= 0) links[next/2][2*(next&1)] = prev;
            links[id][2*side] = -1; links[id][2*side+1] = -1;
            bodies[body].head = head; bodies[body].count = remaining;
        }
        return;
    }
    if (operation == 19)
    {
        if (index >= count || atomicAdd(pool.z,0) != 0) return;
        int id = removed[index].identity.x;
        freeIDs[basePool.y+index] = id;
        slots[id].shapeBody = ivec4(-1); slots[id].state.y &= 0x7fffffffu;
        links[id] = ivec4(-1);
        return;
    }
    if (index >= count || atomicAdd(pool.z, 0) != 0 || scan[leaf + index] == 0) return;
    // ponytail: each allocation walks O(log n) scan ancestors; use a workgroup
    // prefix scan if this part dominates creation cost.
    int prefix = scanPrefix(index);
    int id = prefix < basePool.y ? freeIDs[basePool.y-prefix-1] : basePool.x+prefix-basePool.y;
    if (id < 0 || id >= capacity) { atomicMax(pool.z, 7); return; }
    Created result = created[index];
    result.slot.state.x = slots[id].state.x + 1u;
    result.identity.x = id;
    slots[id] = result.slot; created[index] = result;
}
