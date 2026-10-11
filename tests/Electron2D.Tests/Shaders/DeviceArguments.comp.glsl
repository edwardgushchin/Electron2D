#version 450
layout(local_size_x=1) in;
layout(set=0,binding=0,std430) buffer Commands { uint words[]; } commands;
void main() {
    commands.words[0]=3; commands.words[1]=1; commands.words[2]=0; commands.words[3]=0;
    commands.words[8]=3; commands.words[9]=1; commands.words[10]=0; commands.words[11]=0;
}
