#version 450
layout(location=0) in vec4 color; layout(location=1) in vec2 uv; layout(location=0) out vec4 outputColor;
layout(set=2,binding=0) uniform sampler2D value0;
layout(set=2,binding=1) uniform sampler2D value1;
layout(set=2,binding=2) uniform sampler2D value2;
layout(set=2,binding=3) uniform sampler2D value3;
layout(set=2,binding=4) uniform sampler2D value4;
layout(set=2,binding=5) uniform sampler2D value5;
layout(set=2,binding=6) uniform sampler2D value6;
layout(set=2,binding=7) uniform sampler2D value7;
layout(set=2,binding=8) uniform sampler2D value8;
layout(set=2,binding=9) uniform sampler2D value9;
layout(set=2,binding=10) uniform sampler2D value10;
layout(set=2,binding=11) uniform sampler2D value11;
layout(set=2,binding=12) uniform sampler2D value12;
layout(set=2,binding=13) uniform sampler2D value13;
layout(set=2,binding=14) uniform sampler2D value14;
layout(set=2,binding=15) uniform sampler2D value15;
void main(){outputColor=color*(texture(value0,uv)+texture(value1,uv)+texture(value2,uv)+texture(value3,uv)+texture(value4,uv)+texture(value5,uv)+texture(value6,uv)+texture(value7,uv)+texture(value8,uv)+texture(value9,uv)+texture(value10,uv)+texture(value11,uv)+texture(value12,uv)+texture(value13,uv)+texture(value14,uv)+texture(value15,uv))/16;}
