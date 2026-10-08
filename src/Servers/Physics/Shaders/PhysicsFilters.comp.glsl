// SPDX-License-Identifier: MIT
#version 450
layout(local_size_x = 64) in;
struct Shape { uvec4 bits; ivec4 bodyGroupJoints; ivec4 flags; };
struct Joint { ivec4 bodiesNext; ivec4 flags; };
struct Query { vec4 bounds; int proxy; int offset; int count; int capacity; ivec4 shape; };
layout(std430, set = 0, binding = 0) readonly buffer ShapeUpdates { Shape shapeUpdates[]; };
layout(std430, set = 0, binding = 1) readonly buffer JointUpdates { Joint jointUpdates[]; };
layout(std430, set = 0, binding = 2) readonly buffer Queries { Query queries[]; };
layout(std430, set = 1, binding = 0) buffer Shapes { Shape shapes[]; };
layout(std430, set = 1, binding = 1) buffer Joints { Joint joints[]; };
layout(std140, set = 2, binding = 0) uniform Settings { ivec4 countsEpoch; ivec4 operation; };
void main()
{
    uint id = gl_GlobalInvocationID.x;
    if (operation.x == 0)
    {
        if (id < countsEpoch.x) { Shape s = shapeUpdates[id]; shapes[s.flags.z] = s; }
        if (id < countsEpoch.y) { Joint j = jointUpdates[id]; joints[j.flags.y] = j; }
    }
    else if (id < countsEpoch.z && queries[id].proxy != -1)
        shapes[queries[id].shape.x].flags.y = countsEpoch.w;
}
