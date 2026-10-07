#version 450
layout(local_size_x = 64) in;
struct Node { vec4 bounds; int escape; int proxy; int hasCategory; int padding; };
struct Query { vec4 bounds; int proxy; int offset; int count; int capacity; };
layout(std430, set = 0, binding = 0) readonly buffer Tree { Node nodes[]; };
layout(std430, set = 1, binding = 0) buffer Queries { Query queries[]; };
layout(std430, set = 1, binding = 1) buffer Candidates { int candidates[]; };
layout(std140, set = 2, binding = 0) uniform Settings { ivec4 settings; };

void main()
{
    uint id = gl_GlobalInvocationID.x;
    if (id >= settings.x) return;
    Query q = queries[id];
    int count = 0;
    if (q.proxy != -1)
    {
        // Dynamic proxies query kinematic, static, then dynamic trees.
        // All other types query only dynamic proxies.
        int at = (q.proxy & 3) == 2 ? 0 : settings.y;
        while (at < settings.z)
        {
            Node n = nodes[at];
            bool overlap = all(lessThanEqual(n.bounds.xy, q.bounds.zw)) &&
                           all(lessThanEqual(q.bounds.xy, n.bounds.zw));
            if (n.hasCategory == 0 || !overlap) { at = n.escape; continue; }
            if (n.proxy != -1)
            {
                if (count < q.capacity) candidates[q.offset + count] = n.proxy;
                count++;
            }
            at++;
        }
    }
    queries[id].count = count;
}
