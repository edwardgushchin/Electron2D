#version 450
layout(local_size_x=64) in;
layout(std430,set=1,binding=0) buffer A { uint a[]; };
layout(std430,set=1,binding=1) buffer B { uint b[]; };
layout(std430,set=1,binding=2) buffer C { uint c[]; };
layout(std430,set=1,binding=3) buffer D { uint d[]; };
layout(std430,set=1,binding=4) buffer E { uint e[]; };
layout(std430,set=1,binding=5) buffer F { uint f[]; };
layout(std430,set=1,binding=6) buffer G { uint g[]; };
layout(std430,set=1,binding=7) buffer H { uint h[]; };
layout(std140,set=2,binding=0) uniform U0 { uvec4 u0; };
layout(std140,set=2,binding=1) uniform U1 { uvec4 u1; };
layout(std140,set=2,binding=2) uniform U2 { uvec4 u2; };
layout(std140,set=2,binding=3) uniform U3 { uvec4 u3; };
void main()
{
    if (gl_GlobalInvocationID.x != 0) return;
    uint offset=u0.y;
    a[offset]=uint(a.length()); a[offset+1]=uint(b.length()); a[offset+2]=uint(c.length()); a[offset+3]=uint(d.length());
    a[offset+4]=uint(e.length()); a[offset+5]=uint(f.length()); a[offset+6]=uint(g.length()); a[offset+7]=uint(h.length());
    a[offset+8]=u0.x+u1.x+u2.x+u3.x;
}
