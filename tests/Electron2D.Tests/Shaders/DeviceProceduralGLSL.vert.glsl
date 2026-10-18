#version 450
layout(location=0) in vec2 position;
layout(location=1) in vec4 color;
layout(location=2) in vec2 uv;
layout(location=10) in vec4 custom0;
layout(set=1,binding=0,std140) uniform Frame { vec2 size; };
layout(set=1,binding=1,std140) uniform Parameters { vec2 generated[4]; vec2 offset; float homogeneous_w; };
layout(set=0,binding=0) uniform sampler2D position_map;
layout(location=0) out vec4 outColor;
layout(location=1) out vec2 outUV;
void main() {
    vec2 p = position + generated[gl_VertexIndex % 4] + offset + textureLod(position_map, uv, 0).xy;
    float w = 1 + homogeneous_w * (gl_VertexIndex % 4 + 1);
    gl_Position = vec4((p.x / size.x * 2 - 1) * w, (1 - p.y / size.y * 2) * w, 0, w);
    gl_PointSize = 1;
    outColor = color * vec4(1 - custom0.x, 1, 1, 1); outUV = uv;
}
