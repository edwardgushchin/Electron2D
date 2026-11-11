#version 450
layout(local_size_x = 4, local_size_y = 4) in;
layout(set = 0, binding = 0, rgba8) writeonly uniform image2D target;

void main()
{
    imageStore(target, ivec2(gl_GlobalInvocationID.xy), vec4(128, 64, 192, 255) / 255.0);
}
