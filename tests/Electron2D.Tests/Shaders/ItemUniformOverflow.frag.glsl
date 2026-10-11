#version 450
layout(set=3,binding=0,std140) uniform Instance { float value0; float value1; float value2; float value3; float value4; float value5; float value6; float value7; float value8; float value9; float value10; float value11; float value12; float value13; float value14; float value15; float value16; };
layout(location=0) out vec4 outputColor;
void main() { outputColor = vec4(value0+value1+value2+value3+value4+value5+value6+value7+value8+value9+value10+value11+value12+value13+value14+value15+value16); }
