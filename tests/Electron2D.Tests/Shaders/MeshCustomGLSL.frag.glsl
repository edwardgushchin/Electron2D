#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=2) in vec4 instanceCustom;
layout(location=4) in vec2 uv2;
layout(location=5) in vec3 normal;
layout(location=6) in vec4 tangent;
layout(location=7) in vec4 custom0;
layout(location=8) in vec4 custom1;
layout(location=9) in vec4 custom2;
layout(location=10) in vec4 custom3;
layout(location=0) out vec4 outputColor;
void main(){
    vec4 xs=vec4(custom0.x,custom1.x,custom2.x,custom3.x);
    bool padding=all(equal(custom0.yzw,vec3(0,0,1)))&&all(equal(custom1.yzw,vec3(0,0,1)))&&all(equal(custom2.yzw,vec3(0,0,1)))&&all(equal(custom3.yzw,vec3(0,0,1)));
    padding=padding&&color.a==1&&uv.x>=0&&all(equal(instanceCustom,vec4(0)))&&all(equal(uv2,vec2(0)))&&normal.z==-1&&tangent.w==1;
    outputColor=padding&&(all(equal(xs,vec4(1)))||all(equal(xs,vec4(1,-1,1,1))))?vec4(0,1,0,1):padding&&all(equal(xs,vec4(0)))?vec4(1,0,1,1):vec4(1,0,0,1);
}
