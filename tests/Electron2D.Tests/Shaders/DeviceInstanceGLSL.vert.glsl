#version 450
layout(location=0) in vec2 position;
layout(location=1) in vec4 color;
layout(location=2) in vec2 uv;
layout(set=1,binding=0,std140) uniform Frame { vec2 size; };
layout(set=1,binding=1,std140) uniform Parameters { int expected_first; };
layout(location=0) out vec4 outColor;
layout(location=1) out vec2 outUV;
void main() {
    gl_Position=vec4(position.x/size.x*2-1,1-position.y/size.y*2,0,1);
    outColor=(gl_InstanceIndex==expected_first+1 ? vec4(0,1,0,1) : vec4(1,0,0,1))*vec4(color.r+color.g+color.b);
    outUV=uv;
}
