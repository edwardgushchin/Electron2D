#version 450
#extension GL_GOOGLE_include_directive : require
#include "PhysicsPairHash.inc.glsl"
layout(local_size_x = 64) in;
layout(std430, set = 0, binding = 0) readonly buffer Updates { uvec4 updates[]; };
layout(std430, set = 1, binding = 0) buffer Keys { uvec2 keys[]; };
layout(std430, set = 1, binding = 1) buffer Slots { uint slots[]; };
layout(std430, set = 1, binding = 2) buffer Status { uint errors; };
layout(std140, set = 2, binding = 0) uniform Settings { uvec4 settings; uvec4 control; };
const uint tombstone = 0xffffffffu;

void insertKey(uint id)
{
    uvec2 key = keys[id];
    if (all(equal(key, uvec2(0)))) return;
    uint slot = pairHash(key) & settings.z;
    for (uint i = 0; i <= settings.z; i++)
    {
        uint prior = atomicCompSwap(slots[slot], 0, id + 1);
        if (prior == 0 || prior == id + 1) return;
        if (prior == tombstone)
        {
            prior = atomicCompSwap(slots[slot], tombstone, id + 1);
            if (prior == tombstone || prior == id + 1) return;
        }
        slot = (slot + 1) & settings.z;
    }
    atomicOr(errors, 2);
}

void main()
{
    uint at = gl_GlobalInvocationID.x;
    if (at >= settings.x) return;
    if (settings.w == 0)
    {
        slots[at] = 0;
        if (at == 0 && control.x != 0) errors = 0;
        return;
    }
    if (settings.w == 3) { insertKey(at); return; }
    uvec4 update = updates[at];
    uint id = update.x;
    if (id >= settings.y) { atomicOr(errors, 4); return; }
    if (settings.w == 2) { insertKey(id); return; }

    // Dirty contact IDs are unique. Remove old identity before replacing keys;
    // the next compute pass inserts all final live identities in parallel.
    uvec2 old = keys[id];
    if (any(notEqual(old, uvec2(0))))
    {
        uint slot = pairHash(old) & settings.z;
        bool found = false;
        for (uint i = 0; i <= settings.z; i++)
        {
            uint prior = atomicAdd(slots[slot], 0);
            if (prior == 0) break;
            if (prior == id + 1) { atomicExchange(slots[slot], tombstone); found = true; break; }
            slot = (slot + 1) & settings.z;
        }
        if (!found) atomicOr(errors, 1);
    }
    keys[id] = update.yz;
}
