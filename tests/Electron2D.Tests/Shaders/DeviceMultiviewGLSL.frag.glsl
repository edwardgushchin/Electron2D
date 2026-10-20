#version 450
#extension GL_EXT_multiview : require
layout(location=0) flat in uint view_index;
layout(location=0) out vec4 color;
layout(push_constant) uniform Settings { vec4 tint; };
void main() {
    uint index=uint(gl_ViewIndex);
    color=(view_index==index ? vec4(index==0 ? 1 : 0,index==1 ? 1 : 0,index==2 ? 1 : 0,1) : vec4(1))*tint;
}
