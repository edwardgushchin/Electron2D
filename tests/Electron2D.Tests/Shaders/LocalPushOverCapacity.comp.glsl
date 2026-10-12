#version 450
layout(local_size_x=1) in;
layout(push_constant,std430) uniform Parameters { float values[32]; } pc;
layout(set=3,binding=7,std430) buffer Output { float result[]; };
layout(set=0,binding=0,std140) uniform A { vec4 a; };
layout(set=0,binding=1,std140) uniform B { vec4 b; };
layout(set=0,binding=2,std140) uniform C { vec4 c; };
layout(set=0,binding=3,std140) uniform D { vec4 d; };
void main() { result[0]=a.x+b.x+c.x+d.x+pc.values[0]+pc.values[31]; }
