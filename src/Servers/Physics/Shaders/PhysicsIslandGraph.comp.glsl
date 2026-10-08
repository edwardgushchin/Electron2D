// SPDX-License-Identifier: MIT
#version 450
layout(local_size_x=64) in;
shared int groupError;
struct Island { ivec4 state; ivec4 bodies; ivec4 contacts; ivec4 joints; };
struct Change { ivec4 inputData; ivec4 result; };
struct Update { ivec4 key; Island value; };
layout(std430,set=0,binding=0) readonly buffer Updates { Update updates[]; };
layout(std430,set=1,binding=0) buffer Islands { Island islands[]; };
layout(std430,set=1,binding=1) buffer Bodies { ivec4 bodies[]; };
layout(std430,set=1,binding=2) buffer Contacts { ivec4 contacts[]; };
layout(std430,set=1,binding=3) buffer Joints { ivec4 joints[]; };
layout(std430,set=1,binding=4) buffer Changes { Change changes[]; };
layout(std430,set=1,binding=5) buffer Status { ivec4 status; };
layout(std430,set=1,binding=6) buffer Dirty { int dirty[]; };
layout(std430,set=1,binding=7) buffer Output { int outputData[]; };
layout(std140,set=2,binding=0) uniform Settings { ivec4 sizes; ivec4 work; };
// Member: island, previous ID, next ID, alive. Island state: parent, ID, removal count, reserved.
// Change input: contact ID, generation, initial endpoint islands. Result: link, winner/final removal root, freed ID, reserved.
// Tree nodes: old-big child, old-small child, first contact, original-list tail.
// Follow values are a next contact, -1/end, or -(parent+2) for the parent's follow.
// Follow and removal-skip pointer jumping use separate ping-pong scratch ranges.
int root(int id,int known)
{
    if(id==known) return known;
    for(int depth=0;id>=0 && depth<sizes.w;depth++)
    {
        if(id>=sizes.w || islands[id].state.y!=id) { atomicMax(status.x,1); return -1; }
        int parent=islands[id].state.x;
        if(parent==known || parent==id) return parent;
        id=parent;
    }
    if(id>=0) atomicMax(status.x,2);
    return -1;
}
int keyCount() { return 4*max(max(sizes.x,sizes.y),max(sizes.z,sizes.w)); }
int treeCount() { return sizes.w+work.y; }
int treeAt(int id) { return keyCount()+4*id; }
ivec4 tree(int id) { int at=treeAt(id); return ivec4(dirty[at],dirty[at+1],dirty[at+2],dirty[at+3]); }
void putTree(int id,ivec4 value) { int at=treeAt(id); for(int j=0;j<4;j++) dirty[at+j]=value[j]; }
int followAt(int parity,int id) { return keyCount()+4*treeCount()+parity*treeCount()+id; }
int skipAt(int parity,int id) { return keyCount()+6*treeCount()+parity*sizes.y+id; }
ivec4 node(int kind,int id) { if(kind==0) return bodies[id]; if(kind==1) return contacts[id]; return joints[id]; }
// Plain marks have one writer per slot in each pass. Removal counters use atomic marks.
void mark(int kind,int id) { dirty[4*id+kind]=1; }
void put(int kind,int id,ivec4 value) { mark(kind+1,id); if(kind==0) bodies[id]=value; else if(kind==1) contacts[id]=value; else joints[id]=value; }
ivec4 list(int kind,int id) { if(kind==0) return islands[id].bodies; if(kind==1) return islands[id].contacts; return islands[id].joints; }
void putList(int kind,int id,ivec4 value) { mark(0,id); if(kind==0) islands[id].bodies=value; else if(kind==1) islands[id].contacts=value; else islands[id].joints=value; }
void merge(int big,int small)
{
    for(int kind=0;kind<3;kind++)
    {
        if(kind==1) continue;
        ivec4 a=list(kind,big),b=list(kind,small);
        if(a.z==0) a=b;
        else if(b.z!=0)
        {
            ivec4 tail=node(kind,a.y),head=node(kind,b.x);
            tail.z=b.x; head.y=a.y; put(kind,a.y,tail); put(kind,b.x,head);
            a.y=b.y; a.z+=b.z;
        }
        putList(kind,big,a); putList(kind,small,ivec4(-1,-1,0,0));
    }
    islands[big].contacts.z+=islands[small].contacts.z;
    islands[small].contacts=ivec4(-1,-1,0,0);
    islands[big].state.z+=islands[small].state.z;
    islands[small].state.z=0; islands[small].state.x=big;
    status.y++;
}
void flushContactHead(int id,int treeRoot,int head,int count)
{
    if(id<0) return;
    islands[id].state.w=treeRoot; islands[id].contacts.x=head; islands[id].contacts.z=count; mark(0,id);
}
void main()
{
    int i=int(gl_GlobalInvocationID.x);
    if(work.x==3)
    {
        if(i<keyCount()) dirty[i]=0;
        if(i<treeCount())
        {
            putTree(i,ivec4(-1)); dirty[followAt(0,i)]=-1;
            if(i<sizes.w)
            {
                Island group=islands[i];
                putTree(i,ivec4(-1,-1,group.state.y<0?-1:group.contacts.x,group.contacts.y));
                islands[i].state.w=group.state.y>=0 && group.contacts.x>=0?i:-1;
            }
        }
        return;
    }
    if(work.x==4)
    {
        if(i>=work.z) return;
        Update update=updates[i]; int kind=update.key.x&3,id=update.key.x>>2;
        if(kind==0) islands[id]=update.value;
        else if(kind==1) bodies[id]=update.value.state;
        else if(kind==2) contacts[id]=update.value.state;
        else joints[id]=update.value.state;
        return;
    }
    if(work.x==6) { if(i==0) status.z=0; return; }
    if(work.x==5)
    {
        if(i>=4*max(max(sizes.x,sizes.y),max(sizes.z,sizes.w)) || dirty[i]==0) return;
        int kind=i&3,id=i>>2,size=kind==0?17:5;
        int at=atomicAdd(status.z,size);
        if(at+size>work.w) return;
        outputData[at++]=i;
        if(kind==0)
        {
            Island value=islands[id];
            for(int j=0;j<4;j++) { outputData[at+j]=value.state[j]; outputData[at+4+j]=value.bodies[j]; outputData[at+8+j]=value.contacts[j]; outputData[at+12+j]=value.joints[j]; }
        }
        else { ivec4 value=node(kind-1,id); for(int j=0;j<4;j++) outputData[at+j]=value[j]; }
        return;
    }
    if(work.x==0)
    {
        if(i!=0) return;
        status=ivec4(0);
        // ponytail: weighted union preserves exact winner/free-ID order. Contact
        // list construction and deletion run in parallel through ordered tree links.
        int known=-1,cachedTree=-1,cachedHead=-1,cachedCount=0;
        for(int op=0;op<work.y;op++)
        {
            Change change=changes[op]; if(change.result.x==0) continue;
            int id=change.inputData.x;
            if(id<0 || id>=sizes.y || contacts[id].w!=1 || contacts[id].x!=-1) { status.x=3; return; }
            int a=root(change.inputData.z,known),b=root(change.inputData.w,known);
            if(status.x!=0 || (a<0 && b<0)) { status.x=4; return; }
            int target=a<0?b:a,small=-1;
            if(a>=0 && b>=0 && a!=b)
            {
                target=islands[a].bodies.z>=islands[b].bodies.z?a:b;
                small=target==a?b:a;
            }
            int left=target==known?cachedTree:islands[target].state.w;
            int right=small<0?-1:small==known?cachedTree:islands[small].state.w;
            // Consecutive insertions commonly share the same large component.
            // Keep its head/count in registers; merge reads require a flush.
            if(small>=0 || target!=known)
            {
                flushContactHead(known,cachedTree,cachedHead,cachedCount);
                if(small>=0) { merge(target,small); change.result.z=small; }
                known=target; cachedCount=islands[target].contacts.z;
            }
            putTree(sizes.w+op,ivec4(left,right,id,-1));
            cachedTree=sizes.w+op; cachedHead=id; cachedCount++;
            change.result.y=target; changes[op]=change;
        }
        flushContactHead(known,cachedTree,cachedHead,cachedCount);
        return;
    }
    // Read the shared error flag once per workgroup, not once per member.
    // Errors remain monotonic; per-invocation checks may still raise a new error.
    if(gl_LocalInvocationID.x==0) groupError=atomicAdd(status.x,0);
    barrier();
    if(groupError!=0) return;
    if(work.x==7)
    {
        if(i>=work.y || changes[i].result.x==0) return;
        int parent=sizes.w+i; ivec4 value=tree(parent);
        if(value.x>=0) dirty[followAt(0,value.x)]=value.y>=0?tree(value.y).z:-parent-2;
        if(value.y>=0) dirty[followAt(0,value.y)]=-parent-2;
        return;
    }
    if(work.x==8)
    {
        if(i>=treeCount()) return;
        int value=dirty[followAt(work.z,i)];
        if(value<-1)
        {
            int parent=-value-2;
            if(parent>=treeCount()) { atomicMax(status.x,6); return; }
            value=dirty[followAt(work.z,parent)];
        }
        dirty[followAt(1-work.z,i)]=value; return;
    }
    if(work.x==9)
    {
        if(i>=treeCount()) return;
        ivec4 value=tree(i); if(value.z<0) return;
        int following=dirty[followAt(work.z,i)];
        if(following<-1 || following>=sizes.y) { atomicMax(status.x,7); return; }
        int id=i<sizes.w?value.w:value.z;
        if(id<0 || id>=sizes.y) { atomicMax(status.x,8); return; }
        int next=value.x>=0?tree(value.x).z:value.y>=0?tree(value.y).z:following;
        // Each original-list tail and each inserted contact is owned by one node.
        if(contacts[id].z!=next) { contacts[id].z=next; mark(2,id); }
        if(i>=sizes.w) { contacts[id].x=changes[i-sizes.w].result.y; mark(2,id); }
        return;
    }
    if(work.x==10)
    {
        if(i>=work.y || changes[i].result.x!=0) return;
        int id=changes[i].inputData.x;
        if(id<0 || id>=sizes.y || contacts[id].w!=1) { atomicMax(status.x,9); return; }
        int target=root(contacts[id].x,-1);
        if(target<0) { atomicMax(status.x,10); return; }
        contacts[id].w=2; mark(2,id); changes[i].result.y=target;
        atomicAdd(islands[target].contacts.z,-1); atomicAdd(islands[target].state.z,1);
        atomicOr(dirty[4*target],1); return;
    }
    if(work.x==11)
    {
        if(i<sizes.y) dirty[skipAt(0,i)]=contacts[i].x>=0?contacts[i].z:-1;
        return;
    }
    if(work.x==12)
    {
        if(i>=sizes.y) return;
        int next=dirty[skipAt(work.z,i)];
        if(next<-1 || next>=sizes.y) { atomicMax(status.x,11); return; }
        if(next>=0 && contacts[next].w==2) next=dirty[skipAt(work.z,next)];
        dirty[skipAt(1-work.z,i)]=next; return;
    }
    if(work.x==13)
    {
        if(i>=sizes.y || contacts[i].x<0 || contacts[i].w!=1) return;
        int next=dirty[skipAt(work.z,i)];
        if(contacts[i].z!=next) { contacts[i].z=next; mark(2,i); }
        return;
    }
    if(work.x==14)
    {
        if(i>=sizes.w || islands[i].state.y!=i) return;
        int head=islands[i].contacts.x;
        if(head>=0 && contacts[head].w==2) head=dirty[skipAt(work.z,head)];
        if(islands[i].contacts.x!=head) { islands[i].contacts.x=head; mark(0,i); }
        if(head<0) { if(islands[i].contacts.y!=-1) { islands[i].contacts.y=-1; mark(0,i); } }
        else if(contacts[head].y!=-1) { contacts[head].y=-1; mark(2,head); }
        return;
    }
    if(work.x==15)
    {
        if(i>=sizes.y || contacts[i].x<0 || contacts[i].w!=1) return;
        int next=contacts[i].z;
        if(next>=0)
        {
            if(contacts[next].y!=i) { contacts[next].y=i; mark(2,next); }
        }
        else
        {
            int group=contacts[i].x;
            if(islands[group].contacts.y!=i) { islands[group].contacts.y=i; mark(0,group); }
        }
        return;
    }
    if(work.x==16)
    {
        if(i<work.y && changes[i].result.x==0) put(1,changes[i].inputData.x,ivec4(-1,-1,-1,1));
        return;
    }
    if(work.x==2)
    {
        if(i<sizes.w && islands[i].state.y>=0 && islands[i].state.x!=i)
        { islands[i].state.xy=ivec2(-1); mark(0,i); }
        return;
    }
    for(int kind=0;kind<3;kind++)
    {
        if(i>=sizes[kind]) continue;
        ivec4 value=node(kind,i);
        if(value.w==0 || value.x<0) continue;
        int parent=root(value.x,-1);
        if(value.x!=parent) { value.x=parent; put(kind,i,value); }
    }
}
