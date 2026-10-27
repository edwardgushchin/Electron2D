#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0,std430) buffer Output { double value; } target;
void main(){target.value=1.0000000000009094947017729282379150390625LF;}
