#version 450
layout(location=0) in vec2 position;
layout(location=1) in vec4 color;
layout(location=2) in vec2 uv;
layout(set=1,binding=0,std140) uniform Frame { vec2 size; vec2 options; } frame;
layout(location=0) out vec4 outColor;
layout(location=1) out vec2 outUV;
void main() {
    vec2 p = position + (frame.options.x == 2.0 ? uv : vec2(0));
    gl_Position = vec4(p.x / frame.size.x * 2.0 - 1.0, 1.0 - p.y / frame.size.y * 2.0, 0, 1);
    gl_PointSize = 1.0;
    outColor = color;
    outUV = frame.options.x == 1.0 ? uv : vec2(0);
}
