#version 450
layout(local_size_x=128) in;
layout(set=0,binding=0,std430) buffer Source { vec4 source[]; };
layout(set=0,binding=1,std430) buffer Destination { vec4 destination[]; };
layout(set=0,binding=2,std430) buffer Grid { int heads[]; };
layout(set=0,binding=3,std430) buffer Links { int next[]; };
struct DensitySample { vec2 pressure; vec2 padding; vec4 boundary; };
layout(set=0,binding=4,std430) buffer Densities { DensitySample density[]; };
layout(set=0,binding=5,std430) buffer Reactions { vec4 reaction[]; };
layout(set=0,binding=6,std430) buffer Reduction { vec4 reduced[]; };
layout(set=0,binding=7,std430) buffer Original { vec4 original[]; };
layout(set=1,binding=0,std140) uniform Parameters {
    vec4 meta; // population, operation, columns, rows
    vec4 world; // width, height, interval, smoothing radius (metres)
    vec4 material; // particle mass, areal rest density, particle capacity, padding
    vec4 pointer; // position, enabled, interaction radius
    vec4 integration; // grid top, velocity filter, padding
    vec4 drainData;
    vec4 poses[15]; vec4 motions[15]; vec4 details[15]; vec4 centers[15]; vec4 rotations[15];
};
const uint bodyCount=15;
shared vec4 bodySum[128];
const float pi=3.141592653589793;
ivec2 cell(vec2 p) { return clamp(ivec2(floor(vec2(p.x,p.y-integration.x)/world.w)),ivec2(0),ivec2(meta.zw)-1); }
vec2 rotate(vec2 p,float a) { float c=cos(a),s=sin(a);return vec2(c*p.x-s*p.y,s*p.x+c*p.y); }
vec2 rotateBody(vec2 p,int slot,bool inverse){float c=rotations[slot].x,s=inverse?-rotations[slot].y:rotations[slot].y;return vec2(c*p.x-s*p.y,s*p.x+c*p.y);}
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
vec3 boxDistance(vec2 p,vec2 halfSize) {
    vec2 q=abs(p)-halfSize,outside=max(q,vec2(0));float d=length(outside);
    vec2 n=d>1e-7?outside/d*sign(p):(q.x>q.y?vec2(p.x<0?-1:1,0):vec2(0,p.y<0?-1:1));
    return vec3(n,d+min(max(q.x,q.y),0));
}
vec3 nearer(vec3 a,vec3 b){return a.z<b.z?a:b;}
vec3 bodyDistance(vec2 p,int slot) {
    int kind=int(details[slot].z);
    if(kind==0)return duckDistance(p);if(kind==1)return boatDistance(p);
    if(kind==2)return nearer(boxDistance(p-vec2(0,.44),vec2(.57,.06)),nearer(boxDistance(p-vec2(-.52,0),vec2(.05,.45)),boxDistance(p-vec2(.52,0),vec2(.05,.45))));
    if(kind==3){
        vec3 nearest=circle(p,.16);
        for(int i=0;i<8;i++){float a=i*pi/4;vec3 d=boxDistance(rotate(p,-a)-vec2(.45,0),vec2(.25,.08));nearest=nearer(nearest,vec3(rotate(d.xy,a),d.z));}
        return nearest;
    }
    if(kind==4)return boxDistance(p,vec2(.06,1.8));
    if(kind==5)return circle(p,.28);
    if(kind==6)return boxDistance(p,vec2(.18));
    if(kind==7)return circle(p,.14);
    return boxDistance(p,vec2(.5,.06));
}
void collide(inout vec2 p,inout vec2 v,vec4 pose,vec4 motion,float invInertia,int slot,inout vec4 impulse) {
    if(pose.w<.5)return;
    vec2 origin=pose.xy;
    if(dot(p-origin,p-origin)>details[slot].y)return;
    vec2 local=rotateBody(p-origin,slot,true);
    vec3 sdf=bodyDistance(local,slot);
    float radius=sqrt(material.x/material.y)*.45;
    if(sdf.z>=radius)return;
    vec2 normal=rotateBody(sdf.xy,slot,false);
    p+=normal*(radius-sdf.z);
    vec2 center=centers[slot].xy;
    vec2 arm=p-center;
    vec2 velocity=motion.xy+motion.z*vec2(-arm.y,arm.x);
    float tangent=cross2(arm,normal);
    float j=max(0,-dot(v-velocity,normal))*material.x/(1+material.x*(motion.w+tangent*tangent*invInertia));
    vec2 action=normal*j;v+=action/material.x;
    impulse.xy-=action;impulse.z-=cross2(arm,action);
}
void project(inout vec2 p,vec4 pose,vec4 motion,float invInertia,int slot,inout vec4 impulse) {
    if(pose.w<.5)return;
    vec2 origin=pose.xy;
    if(dot(p-origin,p-origin)>details[slot].y)return;
    vec3 sdf=bodyDistance(rotateBody(p-origin,slot,true),slot);
    float radius=sqrt(material.x/material.y)*.45;
    if(sdf.z>=radius)return;
    vec2 n=rotateBody(sdf.xy,slot,false),arm=p-(centers[slot].xy);
    float r=cross2(arm,n);
    vec2 correction=n*(radius-sdf.z)/(1+material.x*(motion.w+r*r*invInertia));
    p+=correction;
    vec2 j=correction*(material.x/world.z);impulse.xy-=j;impulse.z-=cross2(arm,j);
}
vec4 boundaryAt(vec2 p) {
    vec4 nearest=vec4(0,0,world.w,0);
    for(int body=0;body<int(material.w);body++) {
        if(poses[body].w<.5)continue;
        vec2 origin=poses[body].xy;
        if(dot(p-origin,p-origin)>details[body].w)continue;
        vec3 d=bodyDistance(rotateBody(p-origin,body,true),body);
        if(d.z>=0 && d.z<nearest.z)nearest=vec4(rotateBody(d.xy,body,false),d.z,body+1);
    }
    return nearest;
}
void main() {
    uint id=gl_GlobalInvocationID.x,n=uint(meta.x),op=uint(meta.y),columns=uint(meta.z),rows=uint(meta.w);
    if(op==0) { if(id<columns*rows)heads[id]=-1;if(id==0)heads[columns*rows]=0;return; }
    if(op==8) {
        if(id>=columns*rows)return;int count=0;
        for(int j=heads[id];j!=-1;j=next[j])count++;
        if(count==0)return;int at=atomicAdd(heads[columns*rows],count);
        for(int j=heads[id];j!=-1;j=next[j])destination[at++]=source[j];return;
    }
    if(op==4) {
        uint lane=gl_LocalInvocationID.x,body=gl_WorkGroupID.y;
        bodySum[lane]=id<n?reaction[body*uint(material.z)+id]:vec4(0);
        barrier();
        for(uint stride=64;stride>0;stride>>=1) {
            if(lane<stride)bodySum[lane]+=bodySum[lane+stride];
            barrier();
        }
        if(lane==0)reduced[bodyCount*(gl_WorkGroupID.x+1)+body]=bodySum[0];
        return;
    }
    if(op==7) {
        uint lane=gl_LocalInvocationID.x,body=gl_WorkGroupID.x;vec4 sum=vec4(0);
        for(uint group=lane;group<(n+127)/128;group+=128)sum+=reduced[bodyCount*(group+1)+body];
        bodySum[lane]=sum;barrier();
        for(uint stride=64;stride>0;stride>>=1){if(lane<stride)bodySum[lane]+=bodySum[lane+stride];barrier();}
        if(lane==0)reduced[body]=bodySum[0];return;
    }
    if(id>=n)return;
    vec4 state=source[id];float h=world.w,h2=h*h,m=material.x;
    if(op==5) {
        original[id]=state;for(uint body=0;body<uint(material.w);body++)reaction[body*uint(material.z)+id]=vec4(0);
        vec2 acceleration=vec2(0,9.8),offset=pointer.xy-state.xy;
        if(pointer.z>.5 && dot(offset,offset)<pointer.w*pointer.w)acceleration+=offset*18;
        vec2 drain=drainData.xy-state.xy;
        if(drainData.z>.5 && dot(drain,drain)<2.56)acceleration+=drain*14;
        state.zw+=acceleration*world.z;state.xy+=state.zw*world.z;
        destination[id]=state;return;
    }
    ivec2 grid=cell(state.xy);
    if(op==1) {next[id]=atomicExchange(heads[grid.y*int(columns)+grid.x],int(id));return;}
    float kernel=4/(pi*h2*h2*h2*h2),gradient=30/(pi*h2*h2*h);
    vec4 boundary=op==2?boundaryAt(state.xy):density[id].boundary; if(op==2)density[id].boundary=boundary;
    float sum=0,denominator=0;vec2 ownGradient=vec2(0),correction=vec2(0),solidCorrection=vec2(0),viscosity=vec2(0);
    vec2 velocity=(state.xy-original[id].xy)/world.z;
    for(int y=max(0,grid.y-1);y<=min(int(rows)-1,grid.y+1);y++)
    for(int x=max(0,grid.x-1);x<=min(int(columns)-1,grid.x+1);x++)
    for(int j=heads[y*int(columns)+x];j!=-1;j=next[j]) {
        vec2 neighborGradient=vec2(0);
        for(int mirror=0;mirror<(boundary.w>0?5:(state.x<h || state.x>world.x-h || state.y>world.y-h)?4:1);mirror++) {
        if(op==6 && mirror>0)continue;
        vec2 neighbor=source[j].xy;
        if(mirror==1){if(state.x>=h)continue;neighbor.x=-neighbor.x;}
        if(mirror==2){if(state.x<=world.x-h)continue;neighbor.x=2*world.x-neighbor.x;}
        if(mirror==3){if(state.y<=world.y-h)continue;neighbor.y=2*world.y-neighbor.y;}
        if(mirror==4){float d=dot(neighbor-state.xy,boundary.xy)+boundary.z;if(d<0)continue;neighbor-=boundary.xy*(2*d);}
        vec2 offset=state.xy-neighbor;float r2=dot(offset,offset);if(r2>=h2)continue;
        float q=h2-r2,weight=kernel*q*q*q;sum+=m*weight;
        if(r2<1e-10)continue;
        float distance=sqrt(r2);
        vec2 grad=-offset*(m/material.y*gradient*(h-distance)*(h-distance)/distance);
        if(op==2){
            vec2 reflected=grad;
            if(mirror==1||mirror==2)reflected.x=-reflected.x;
            else if(mirror==3)reflected.y=-reflected.y;
            else if(mirror==4)reflected-=2*boundary.xy*dot(grad,boundary.xy);
            ownGradient+=int(id)==j?grad-reflected:grad;neighborGradient-=reflected;
        }
        else if(op==3){vec2 displacement=(density[id].pressure.y+density[j].pressure.y)*grad;correction+=displacement;if(mirror==4)solidCorrection+=displacement;}
        else if(op==6){vec2 otherVelocity=(source[j].xy-original[j].xy)/world.z;viscosity+=(otherVelocity-velocity)*(m/max(density[j].pressure.x,1e-6)*weight);}
        }
        if(op==2&&int(id)!=j)denominator+=dot(neighborGradient,neighborGradient);
    }
    if(op==2){float lambda=-max(0,sum/material.y-1)/(denominator+dot(ownGradient,ownGradient)+.01/h2);density[id].pressure=vec2(sum,lambda);return;}
    vec2 position=state.xy;float radius=sqrt(m/material.y)*.45;
    if(op==3) {
        float relaxation=min(.25,h*.2/max(length(correction),1e-8));
        if(boundary.w>0){
            int body=int(boundary.w)-1;uint address=uint(body)*uint(material.z)+id;
            vec2 impulse=solidCorrection*(relaxation*m/world.z);
            vec2 center=centers[body].xy;
            reaction[address].xy-=impulse;reaction[address].z-=cross2(position-center,impulse);
        }
        position+=correction*relaxation;position.x=clamp(position.x,radius,world.x-radius);position.y=min(position.y,world.y-radius);
        for(int body=0;body<int(material.w);body++) {
            uint address=uint(body)*uint(material.z)+id;vec4 action=reaction[address];
            project(position,poses[body],motions[body],details[body].x,body,action);reaction[address]=action;
        }
        position.x=clamp(position.x,radius,world.x-radius);position.y=min(position.y,world.y-radius);
        destination[id]=vec4(position,state.zw);
    } else {
        velocity+=viscosity*integration.y;
        for(int body=0;body<int(material.w);body++) {
            uint address=uint(body)*uint(material.z)+id;vec4 action=reaction[address];
            collide(position,velocity,poses[body],motions[body],details[body].x,body,action);reaction[address]=action;
        }
        position.x=clamp(position.x,radius,world.x-radius);position.y=min(position.y,world.y-radius);
        if((position.x<=radius && velocity.x<0)||(position.x>=world.x-radius && velocity.x>0))velocity.x=0;
        if(position.y>=world.y-radius && velocity.y>0)velocity.y=0;
        destination[id]=vec4(position,velocity);
    }
}
