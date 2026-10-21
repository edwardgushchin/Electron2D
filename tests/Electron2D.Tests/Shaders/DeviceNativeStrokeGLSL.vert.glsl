#version 450
layout(location=0) in vec2 position;
layout(location=1) in vec4 color;
layout(location=2) in vec2 uv;
layout(set=1,binding=0,std140) uniform Frame { vec2 size; };
layout(location=0) out vec4 outColor;
layout(location=1) out vec2 outUV;
void main() { gl_PointSize = 1.0;
    vec2 p = position + uv;
    gl_Position = vec4(p.x / size.x * 2 - 1, 1 - p.y / size.y * 2, 0, 1);
    outColor = color; outUV = vec2(0);
}
