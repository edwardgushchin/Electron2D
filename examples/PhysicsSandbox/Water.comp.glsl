#version 450
layout(local_size_x=128) in;
layout(set=0,binding=0,std430) buffer Source { vec4 source[]; };
layout(set=0,binding=1,std430) buffer Destination { vec4 destination[]; };
layout(set=0,binding=2,std430) buffer Grid { int heads[]; };
layout(set=0,binding=3,std430) buffer Links { int next[]; };
layout(set=0,binding=4,std430) buffer Densities { vec2 density[]; };
layout(set=0,binding=5,std430) buffer Reactions { vec4 reaction[]; };
layout(set=0,binding=6,std430) buffer Reduction { vec4 reduced[]; };
layout(set=0,binding=7,std430) buffer Original { vec4 original[]; };
layout(set=1,binding=0,std140) uniform Parameters {
    vec4 meta; // population, operation, columns, rows
    vec4 world; // width, height, interval, smoothing radius (metres)
    vec4 material; // particle mass, areal rest density, padding
    vec4 pointer; // position, enabled, interaction radius
    vec4 duckPose; vec4 duckMotion;
    vec4 boatPose; vec4 boatMotion;
    vec4 integration; // grid top, padding, duck inverse inertia, boat inverse inertia
};
shared vec4 duckSum[128]; shared vec4 boatSum[128];
const float pi=3.141592653589793;
ivec2 cell(vec2 p) { return clamp(ivec2(floor(vec2(p.x,p.y-integration.x)/world.w)),ivec2(0),ivec2(meta.zw)-1); }
vec2 rotate(vec2 p,float a) { float c=cos(a),s=sin(a);return vec2(c*p.x-s*p.y,s*p.x+c*p.y); }
float cross2(vec2 a,vec2 b) { return a.x*b.y-a.y*b.x; }
vec3 circle(vec2 p,float r) { float d=length(p);return vec3(d>1e-7?p/d:vec2(0,-1),d-r); }
vec3 duckDistance(vec2 p) {
    vec2 q=p-vec2(-.05,0);q.x-=clamp(q.x,-.22,.22);
    vec3 body=circle(q,.32),head=circle(p-vec2(.31,-.32),.27);
    return body.z<head.z?body:head;
}
vec3 boatDistance(vec2 p) {
    const vec2 vertices[4]=vec2[4](vec2(-.84,-.12),vec2(.84,-.12),vec2(.57,.26),vec2(-.52,.26));
    float best=1e20;vec2 normal=vec2(0,-1);bool inside=true;
    for(int i=0;i<4;i++) {
        vec2 a=vertices[i],edge=vertices[(i+1)%4]-a;
        inside=inside && cross2(edge,p-a)>=0;
        vec2 offset=p-(a+edge*clamp(dot(p-a,edge)/dot(edge,edge),0,1));
        float distance=length(offset);
        if(distance<best) { best=distance; normal=distance>1e-7?offset/distance:normalize(vec2(edge.y,-edge.x)); }
    }
    return vec3(inside?-normal:normal,inside?-best:best);
}
void collide(inout vec2 p,inout vec2 v,vec4 pose,vec4 motion,float invInertia,bool duck,inout vec4 impulse) {
    if(pose.w<.5)return;
    vec2 origin=pose.xy+motion.xy*world.z;
    float angle=pose.z+motion.z*world.z;
    vec2 local=rotate(p-origin,-angle);
    vec3 sdf=duck?duckDistance(local):boatDistance(local);
    float radius=sqrt(material.x/material.y)*.45;
    if(sdf.z>=radius)return;
    vec2 normal=rotate(sdf.xy,angle);
    p+=normal*(radius-sdf.z);
    vec2 center=origin+rotate(duck?vec2(-.05,.10):vec2(0,.18),angle);
    vec2 arm=p-center;
    vec2 velocity=motion.xy+motion.z*vec2(-arm.y,arm.x);
    float tangent=cross2(arm,normal);
    float j=max(0,-dot(v-velocity,normal))*material.x/(1+material.x*(motion.w+tangent*tangent*invInertia));
    vec2 action=normal*j;v+=action/material.x;
    impulse.xy-=action;impulse.z-=cross2(arm,action);
}
void project(inout vec2 p,vec4 pose,vec4 motion,float invInertia,bool duck,inout vec4 impulse) {
    if(pose.w<.5)return;
    vec2 origin=pose.xy+motion.xy*world.z;float angle=pose.z+motion.z*world.z;
    vec3 sdf=duck?duckDistance(rotate(p-origin,-angle)):boatDistance(rotate(p-origin,-angle));
    float radius=sqrt(material.x/material.y)*.45;
    if(sdf.z>=radius)return;
    vec2 n=rotate(sdf.xy,angle),arm=p-(origin+rotate(duck?vec2(-.05,.1):vec2(0,.18),angle));
    float r=cross2(arm,n);
    vec2 correction=n*(radius-sdf.z)/(1+material.x*(motion.w+r*r*invInertia));
    p+=correction;
    vec2 j=correction*(material.x/world.z);impulse.xy-=j;impulse.z-=cross2(arm,j);
}
void main() {
    uint id=gl_GlobalInvocationID.x,n=uint(meta.x),op=uint(meta.y),columns=uint(meta.z),rows=uint(meta.w);
    if(op==0) { if(id<columns*rows)heads[id]=-1;return; }
    if(op==4) {
        uint lane=gl_LocalInvocationID.x;
        duckSum[lane]=id<n?reaction[2*id]:vec4(0);boatSum[lane]=id<n?reaction[2*id+1]:vec4(0);barrier();
        for(uint stride=64;stride>0;stride>>=1) {if(lane<stride){duckSum[lane]+=duckSum[lane+stride];boatSum[lane]+=boatSum[lane+stride];}barrier();}
        if(lane==0){reduced[2*gl_WorkGroupID.x]=duckSum[0];reduced[2*gl_WorkGroupID.x+1]=boatSum[0];}return;
    }
    if(id>=n)return;
    vec4 state=source[id];float h=world.w,h2=h*h,m=material.x;
    if(op==5) {
        original[id]=state;reaction[2*id]=vec4(0);reaction[2*id+1]=vec4(0);
        vec2 acceleration=vec2(0,9.8),offset=pointer.xy-state.xy;
        if(pointer.z>.5 && dot(offset,offset)<pointer.w*pointer.w)acceleration+=offset*18;
        state.zw+=acceleration*world.z;state.xy+=state.zw*world.z;
        destination[id]=state;return;
    }
    ivec2 grid=cell(state.xy);
    if(op==1) {next[id]=atomicExchange(heads[grid.y*int(columns)+grid.x],int(id));return;}
    float kernel=4/(pi*h2*h2*h2*h2),gradient=30/(pi*h2*h2*h);
    float sum=0,denominator=0;vec2 ownGradient=vec2(0),correction=vec2(0),viscosity=vec2(0);
    vec2 velocity=(state.xy-original[id].xy)/world.z;
    float reference=kernel*pow(h2*.91,3);
    for(int y=max(0,grid.y-1);y<=min(int(rows)-1,grid.y+1);y++)
    for(int x=max(0,grid.x-1);x<=min(int(columns)-1,grid.x+1);x++)
    for(int j=heads[y*int(columns)+x];j!=-1;j=next[j]) {
        for(int mirror=0;mirror<((state.x<h || state.x>world.x-h || state.y>world.y-h)?4:1);mirror++) {
        if(op==6 && mirror>0)continue;
        vec2 neighbor=source[j].xy;
        if(mirror==1){if(state.x>=h)continue;neighbor.x=-neighbor.x;}
        if(mirror==2){if(state.x<=world.x-h)continue;neighbor.x=2*world.x-neighbor.x;}
        if(mirror==3){if(state.y<=world.y-h)continue;neighbor.y=2*world.y-neighbor.y;}
        vec2 offset=state.xy-neighbor;float r2=dot(offset,offset);if(r2>=h2)continue;
        float q=h2-r2,weight=kernel*q*q*q;sum+=m*weight;
        if(r2<1e-10)continue;
        float distance=sqrt(r2);
        vec2 grad=-offset*(m/material.y*gradient*(h-distance)*(h-distance)/distance);
        if(op==2){ownGradient+=grad;denominator+=dot(grad,grad);}
        else if(op==3){float ratio=weight/reference;float artificial=0;correction+=(density[id].y+density[j].y+artificial)*grad;}
        else if(op==6){vec2 otherVelocity=(source[j].xy-original[j].xy)/world.z;viscosity+=(otherVelocity-velocity)*(m/max(density[j].x,1e-6)*weight);}
        }
    }
    if(op==2){float lambda=-max(0,sum/material.y-1)/(denominator+dot(ownGradient,ownGradient)+.01/h2);density[id]=vec2(sum,lambda);return;}
    vec2 position=state.xy;float radius=sqrt(m/material.y)*.45;vec4 a=reaction[2*id],b=reaction[2*id+1];
    if(op==3) {
        correction*=.25;float lengthCorrection=length(correction);if(lengthCorrection>h*.2)correction*=h*.2/lengthCorrection;
        position+=correction;position.x=clamp(position.x,radius,world.x-radius);position.y=min(position.y,world.y-radius);
        project(position,duckPose,duckMotion,integration.z,true,a);project(position,boatPose,boatMotion,integration.w,false,b);
        destination[id]=vec4(position,state.zw);
    } else {
        velocity+=viscosity*.1;
        collide(position,velocity,duckPose,duckMotion,integration.z,true,a);collide(position,velocity,boatPose,boatMotion,integration.w,false,b);
        destination[id]=vec4(position,velocity);
    }
    reaction[2*id]=a;reaction[2*id+1]=b;
}
