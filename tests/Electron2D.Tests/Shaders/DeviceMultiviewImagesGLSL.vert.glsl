#version 450
#extension GL_EXT_multiview : require
layout(location=0) flat out uint view_index;
void main() {
    vec2 p=vec2((gl_VertexIndex<<1)&2, gl_VertexIndex&2)*2.0-1.0;
    gl_Position=vec4(p,0.25,1.0);
    view_index=uint(gl_ViewIndex);
}
