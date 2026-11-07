#version 450
layout(location=0) out float earlier;
layout(location=3) flat out uint unusedID;
layout(location=11) out float shade;

void main() {
    vec2 p = vec2((gl_VertexIndex << 1) & 2, gl_VertexIndex & 2);
    gl_Position = vec4(p * 2 - 1, 0, 1);
    earlier = p.x;
    unusedID = uint(gl_VertexIndex);
    shade = 0.25;
}
