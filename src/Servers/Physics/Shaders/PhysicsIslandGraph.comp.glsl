// SPDX-License-Identifier: MIT
#version 450
layout(local_size_x=64) in;
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
// Change input: contact ID, generation, initial endpoint islands. Result: link, root, freed ID, reserved.
int root(int id)
{
    for(int depth=0;id>=0 && depth<sizes.w;depth++)
    {
        if(id>=sizes.w || islands[id].state.y!=id) { atomicMax(status.x,1); return -1; }
        int parent=islands[id].state.x;
        if(parent==id) return id;
        id=parent;
    }
    if(id>=0) atomicMax(status.x,2);
    return -1;
}
ivec4 node(int kind,int id) { if(kind==0) return bodies[id]; if(kind==1) return contacts[id]; return joints[id]; }
// Union has one writer; remap and retire passes assign one invocation per slot.
void mark(int kind,int id) { dirty[4*id+kind]=1; }
void put(int kind,int id,ivec4 value) { mark(kind+1,id); if(kind==0) bodies[id]=value; else if(kind==1) contacts[id]=value; else joints[id]=value; }
ivec4 list(int kind,int id) { if(kind==0) return islands[id].bodies; if(kind==1) return islands[id].contacts; return islands[id].joints; }
void putList(int kind,int id,ivec4 value) { mark(0,id); if(kind==0) islands[id].bodies=value; else if(kind==1) islands[id].contacts=value; else islands[id].joints=value; }
void merge(int big,int small)
{
    for(int kind=0;kind<3;kind++)
    {
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
    islands[big].state.z+=islands[small].state.z;
    islands[small].state.z=0; islands[small].state.x=big;
    status.y++;
}
void main()
{
    int i=int(gl_GlobalInvocationID.x);
    if(work.x==3) { if(i<4*max(max(sizes.x,sizes.y),max(sizes.z,sizes.w))) dirty[i]=0; return; }
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
        // ponytail: ordered union/list edits are serial to preserve contact event
        // ordering; member remapping is parallel and never walks merged lists.
        for(int op=0;op<work.y;op++)
        {
            Change change=changes[op]; int id=change.inputData.x;
            if(id<0 || id>=sizes.y || contacts[id].w!=1) { status.x=3; return; }
            int target;
            if(change.result.x!=0)
            {
                int a=root(change.inputData.z),b=root(change.inputData.w);
                if(status.x!=0 || (a<0 && b<0) || contacts[id].x!=-1) { status.x=4; return; }
                target=a<0?b:a;
                if(a>=0 && b>=0 && a!=b)
                {
                    int big=islands[a].bodies.z>=islands[b].bodies.z?a:b;
                    int small=big==a?b:a;
                    merge(big,small); change.result.z=small; target=big;
                }
                ivec4 group=islands[target].contacts;
                put(1,id,ivec4(target,-1,group.x,1));
                if(group.x>=0) { contacts[group.x].y=id; mark(2,group.x); } else group.y=id;
                group.x=id; group.z++; putList(1,target,group);
            }
            else
            {
                target=root(contacts[id].x);
                if(target<0 || status.x!=0) { status.x=5; return; }
                ivec4 member=contacts[id],group=islands[target].contacts;
                if(member.y>=0) { contacts[member.y].z=member.z; mark(2,member.y); } else group.x=member.z;
                if(member.z>=0) { contacts[member.z].y=member.y; mark(2,member.z); } else group.y=member.y;
                group.z--; putList(1,target,group); islands[target].state.z++;
                put(1,id,ivec4(-1,-1,-1,1));
            }
            change.result.y=target; changes[op]=change;
        }
        return;
    }
    if(atomicAdd(status.x,0)!=0) return;
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
        int parent=root(value.x);
        if(value.x!=parent) { value.x=parent; put(kind,i,value); }
    }
}
