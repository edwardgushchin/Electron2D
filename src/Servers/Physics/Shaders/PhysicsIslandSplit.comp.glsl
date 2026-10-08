// SPDX-License-Identifier: MIT
#version 450
layout(local_size_x = 64) in;
struct Edge { ivec4 body; ivec4 next; };
struct Group { ivec4 bodies; ivec4 contacts; ivec4 joints; };
layout(std430, set=0, binding=0) readonly buffer BodyInput { ivec4 bodies[]; };
layout(std430, set=0, binding=1) readonly buffer ContactInput { Edge contacts[]; };
layout(std430, set=0, binding=2) readonly buffer JointInput { Edge joints[]; };
layout(std430, set=1, binding=0) buffer BodyOutput { ivec4 nodes[]; };
layout(std430, set=1, binding=1) buffer ContactOutput { ivec4 contactNodes[]; };
layout(std430, set=1, binding=2) buffer JointOutput { ivec4 jointNodes[]; };
layout(std430, set=1, binding=3) buffer Groups { Group groups[]; };
layout(std430, set=1, binding=4) buffer Stack { int stack[]; };
layout(std430, set=1, binding=5) buffer Scan { int scan[]; };
layout(std430, set=1, binding=6) buffer Status { ivec4 status; };
layout(std140, set=2, binding=0) uniform Settings { ivec4 counts; ivec4 work; };
// node = minimum seed/root, previous native ID, next native ID, visited.
// status = unconverged, error, component count, reserved.
bool enabled(Edge e, bool joint) { return joint ? (e.body.w&1)!=0 && (e.body.w&6)!=0 && (e.body.w&8)==0 : e.body.w!=0; }
bool validEdge(Edge e) { return e.body.y>=-1 && e.body.z>=-1 && e.body.y<counts.y && e.body.z<counts.y && (e.body.y>=0 || e.body.z>=0); }
void main()
{
    int i=int(gl_GlobalInvocationID.x), op=counts.x, n=counts.y, nc=counts.z, nj=counts.w;
    if(op==0)
    {
        if(i==0) status=ivec4(0);
        if(i<n) { nodes[i]=ivec4(i,-1,-1,0); groups[i]=Group(ivec4(-1,-1,0,0),ivec4(-1,-1,0,0),ivec4(-1,-1,0,0)); }
        if(i<nc) contactNodes[i]=ivec4(-1,-1,-1,0);
        if(i<nj) jointNodes[i]=ivec4(-1,-1,-1,0);
        return;
    }
    if(op==1 || op==4)
    {
        if(i<nc+nj)
        {
            bool joint=i>=nc; Edge e;
            if(joint) e=joints[i-nc]; else e=contacts[i];
            if(enabled(e,joint))
            {
                if(!validEdge(e)) { atomicMax(status.y,1); return; }
                if(e.body.y>=0 && e.body.z>=0)
                {
                    int a=atomicAdd(nodes[e.body.y].x,0), b=atomicAdd(nodes[e.body.z].x,0);
                    if(op==1) { atomicMin(nodes[e.body.y].x,b); atomicMin(nodes[e.body.z].x,a); }
                    else if(a!=b) atomicOr(status.x,1);
                }
            }
        }
        if(op==4 && i<n)
        {
            int root=nodes[i].x;
            if(root<0 || root>=n) atomicMax(status.y,2);
            else if(nodes[root].x!=root) atomicOr(status.x,1);
        }
        return;
    }
    if(op==2)
    {
        if(i>=n) return;
        int root=atomicAdd(nodes[i].x,0);
        if(root<0 || root>=n) { atomicMax(status.y,2); return; }
        atomicMin(nodes[i].x,atomicAdd(nodes[root].x,0)); return;
    }
    if(op==3) { if(i==0) status.x=0; return; }
    if(status.x!=0 || atomicAdd(status.y,0)!=0) return;
    if(op==5)
    {
        if(i>=n) return;
        int root=nodes[i].x;
        atomicAdd(groups[root].bodies.z,1);
        if(root==i) atomicAdd(status.z,1);
        return;
    }
    if(op==6) { if(i<work.x) scan[work.x+i]=i<n?groups[i].bodies.z:0; return; }
    if(op==7)
    {
        int node=work.y+i; if(i<work.y) scan[node]=scan[2*node]+scan[2*node+1]; return;
    }
    if(i>=n || nodes[i].x!=i) return;
    int offset=0;
    for(int node=work.x+i;node>1;node/=2) if((node&1)!=0) offset+=scan[node-1];
    int capacity=groups[i].bodies.z, top=1, popped=0;
    stack[offset]=i; nodes[i].w=1;
    Group group=Group(ivec4(-1,-1,0,offset),ivec4(-1,-1,0,0),ivec4(-1,-1,0,0));
    int lastBody=-1, lastContact=-1, lastJoint=-1;
    // ponytail: DFS ordering is serial within each component to match publication
    // order; independent components run in parallel. Replace this if one large
    // component makes ordering dominate the split.
    while(top>0)
    {
        int body=stack[offset+--top];
        if(body<0 || body>=n || nodes[body].x!=i || ++popped>capacity) { atomicMax(status.y,3); return; }
        int nativeBody=bodies[body].x;
        nodes[body].y=lastBody<0?-1:bodies[lastBody].x; nodes[body].z=-1;
        if(lastBody>=0) nodes[lastBody].z=nativeBody; else group.bodies.x=nativeBody;
        group.bodies.y=nativeBody; group.bodies.z++; lastBody=body;
        for(int kind=0;kind<2;kind++)
        {
            int key=kind==0?bodies[body].y:bodies[body].z, walked=0, limit=kind==0?nc:nj;
            while(key>=0)
            {
                int id=key/2, side=key&1;
                if(id>=limit || ++walked>limit) { atomicMax(status.y,4); return; }
                Edge edge; ivec4 member;
                if(kind==0) { edge=contacts[id]; member=contactNodes[id]; }
                else { edge=joints[id]; member=jointNodes[id]; }
                key=edge.next[side];
                if(member.w!=0 || !enabled(edge,kind!=0)) continue;
                int other=edge.body[1+(side^1)];
                if(edge.body[1+side]!=body || other<-1 || other>=n || (other>=0 && nodes[other].x!=i)) { atomicMax(status.y,5); return; }
                if(other>=0 && nodes[other].w==0)
                {
                    if(top>=capacity) { atomicMax(status.y,6); return; }
                    stack[offset+top++]=other; nodes[other].w=1;
                }
                int nativeID=edge.body.x;
                if(kind==0)
                {
                    contactNodes[id]=ivec4(i,lastContact<0?-1:contacts[lastContact].body.x,-1,1);
                    if(lastContact>=0) contactNodes[lastContact].z=nativeID; else group.contacts.x=nativeID;
                    lastContact=id; group.contacts.y=nativeID; group.contacts.z++;
                }
                else
                {
                    jointNodes[id]=ivec4(i,lastJoint<0?-1:joints[lastJoint].body.x,-1,1);
                    if(lastJoint>=0) jointNodes[lastJoint].z=nativeID; else group.joints.x=nativeID;
                    lastJoint=id; group.joints.y=nativeID; group.joints.z++;
                }
            }
        }
    }
    if(popped!=capacity) { atomicMax(status.y,7); return; }
    groups[i]=group;
}
