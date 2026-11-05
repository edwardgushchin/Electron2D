#include "bridge.cpp"
#include <cassert>
#include <cstdio>
#include <thread>
extern "C" void e2d_test_arm(size_t);
extern "C" size_t e2d_test_attempts(void);
static uint64_t current() { E2DMemorySnapshot s{};e2d_memory_snapshot(&s);return s.nativeCurrent; }
static void native_peer() {
    std::unique_ptr<PeerHandle> handle(static_cast<PeerHandle*>(e2d_rtc_create(nullptr,nullptr,nullptr,0)));
    if(!handle)assert(*e2d_rtc_error());
}

static void cohort() {
    std::unique_ptr<PeerHandle> handle(new PeerHandle(e2d_rtc::shared<Peer>()));
    auto p=handle->value;
    auto c=e2d_rtc::shared<Channel>(nullptr);
    p->channels.push_back(c);
    p->proposal.assign(4096,'p');
    p->push({1,std::string(2048,'a'),std::string(4096,'b'),c});
    std::unique_ptr<Event> event(static_cast<Event*>(e2d_rtc_event(handle.get())));
    if(!event) {
        assert(*e2d_rtc_error()&&p->events.size()==1&&p->events.front().first.size()==2048);
        e2d_test_arm(0);event.reset(static_cast<Event*>(e2d_rtc_event(handle.get())));assert(event);
    }
    assert(event->first.size()==2048&&event->second.size()==4096);
    std::unique_ptr<ChannelHandle> channel(static_cast<ChannelHandle*>(e2d_rtc_event_channel(event.get())));
    if(!channel) {
        assert(*e2d_rtc_error()&&event->channel==c);
        e2d_test_arm(0);channel.reset(static_cast<ChannelHandle*>(e2d_rtc_event_channel(event.get())));assert(channel);
    }
    assert(!event->channel&&channel->value==c);
    for(int i=0;i<100;++i)c->push(std::string(17,'x'));
    char bytes[17];int text=-1;
    while(!c->packets.empty())assert(e2d_rtc_channel_read(channel.get(),bytes,sizeof(bytes),&text)==17&&text==1&&bytes[0]=='x');
    assert(c->bytes==0);
    p->close();assert(c->closed&&p->channels.empty());
}

int main() {
    const auto baseline=current();
    e2d_test_arm(0);native_peer();const auto peerRequests=e2d_test_attempts();assert(peerRequests>=2&&current()==baseline);
    for(size_t i=1;i<=peerRequests;++i){
        E2DMemorySnapshot before{},after{};e2d_memory_snapshot(&before);
        e2d_test_arm(i);native_peer();e2d_memory_snapshot(&after);
        assert(after.failures==before.failures+1&&current()==baseline);
        e2d_test_arm(0);native_peer();assert(current()==baseline);
    }
    {
        auto h=new PeerHandle(nullptr);assert(current()==baseline+sizeof(PeerHandle));delete h;
        auto e=new Event(2,std::string(4096,'x'),std::string(2048,'y'),nullptr);
        assert(current()==baseline+sizeof(Event)+e->first.capacity()+e->second.capacity()+2);delete e;
        std::weak_ptr<Channel> weak;
        {auto c=e2d_rtc::shared<Channel>(nullptr);weak=c;}
        assert(weak.expired()&&current()>baseline);weak.reset();assert(current()==baseline);
    }
    e2d_test_arm(0);cohort();const auto requests=e2d_test_attempts();assert(requests>10&&current()==baseline);
    for(size_t i=1;i<=requests;++i) {
        E2DMemorySnapshot before{},after{};e2d_memory_snapshot(&before);
        e2d_test_arm(i);try{cohort();}catch(const std::bad_alloc&){}
        e2d_memory_snapshot(&after);assert(after.failures==before.failures+1);
        assert(current()==baseline);
        e2d_test_arm(0);cohort();assert(current()==baseline);
    }
    size_t growth=0;
    {
        Channel c(nullptr);
        for(size_t i=1;i<=1024;++i){e2d_test_arm(0);c.push(std::string(17,'x'));if(e2d_test_attempts()){growth=i;break;}}
        assert(growth>0);
    }
    {
        auto c=e2d_rtc::shared<Channel>(nullptr);
        for(size_t i=1;i<growth;++i)c->push(std::string(17,'x'));
        const auto bytes=c->bytes,count=c->packets.size(),live=current();
        e2d_test_arm(1);c->push(std::string(17,'x'));
        assert(e2d_test_attempts()==1&&c->overflow&&c->bytes==bytes&&c->packets.size()==count&&current()==live);
        c->push(std::string(17,'x'));assert(c->bytes==bytes&&c->packets.size()==count);
        e2d_test_arm(0);
        ChannelHandle handle(c);assert(e2d_rtc_channel_close(&handle)==0&&e2d_rtc_channel_info(&handle,0)==3&&c->bytes==0);
    }
    {
        Peer p;auto first=e2d_rtc::shared<Channel>(nullptr),second=e2d_rtc::shared<Channel>(nullptr);
        p.channels.push_back(first);p.channels.push_back(second);p.push({3,"","",first});
        first->push(std::string(17,'x'));second->push(std::string(17,'y'));
        e2d_test_arm(1);p.close();
        assert(e2d_test_attempts()==0&&first->closed&&second->closed&&first->bytes==0&&second->bytes==0&&p.events.empty());
        p.failed();assert(!p.overflow);
        e2d_test_arm(0);p.close();
    }
    {
        std::runtime_error failure(std::string(4096,'e'));
        e2d_test_arm(1);assert(attempt([&]()->int{throw failure;},-1)==-1);
        assert(errorStorageFailed&&std::strcmp(e2d_rtc_error(),"Native transport diagnostic allocation failed")==0);
        e2d_test_arm(0);assert(attempt([]{return 7;},-1)==7&&!*e2d_rtc_error());
    }
    std::thread worker([] {
        std::runtime_error failure(std::string(4096,'t'));
        assert(attempt([&]()->int{throw failure;},-1)==-1&&std::strlen(e2d_rtc_error())==4096);
    });worker.join();
    assert(current()==baseline);
    std::printf("WebRTC owned storage: %zu native peer and %zu container calibrated refusals/drain/retries; exact handle/event bytes, weak lifetime, queue atomicity, allocation-free close and TLS retirement passed.\n",peerRequests,requests);
}
