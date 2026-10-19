#include <rtc/rtc.hpp>
#include <atomic>
#include <cstring>
#include <deque>
#include <memory>
#include <mutex>
#include <optional>
#include <stdexcept>
#include <string>
#include <vector>
#ifdef _WIN32
#define API extern "C" __declspec(dllexport)
#else
#define API extern "C" __attribute__((visibility("default")))
#endif
namespace {
thread_local std::string error;
template<class F, class T> T attempt(F function, T failure) noexcept {
    error.clear(); try { return function(); } catch (const std::exception& e) { error=e.what(); } catch (...) { error="Unknown native transport failure"; } return failure;
}
struct Channel {
    std::shared_ptr<rtc::DataChannel> value;
    std::mutex gate;
    std::deque<rtc::message_variant> packets;
    size_t bytes=0;
    bool overflow=false,closed=false;
    explicit Channel(std::shared_ptr<rtc::DataChannel> source):value(std::move(source)){}
    ~Channel() { try { value->close(); } catch (...) {} }
};
struct Event { int kind; std::string first,second; std::shared_ptr<Channel> channel; };
struct Peer {
    std::shared_ptr<rtc::PeerConnection> value;
    std::mutex gate;
    std::deque<Event> events;
    std::vector<std::weak_ptr<Channel>> channels;
    bool closed=false,overflow=false;
    std::string proposal,proposalType;
    void push(Event item) { std::lock_guard lock(gate); if(closed)return; if(events.size()>=4096){overflow=true;return;} events.push_back(std::move(item)); }
    void close() {
        std::vector<std::shared_ptr<Channel>> live;
        {std::lock_guard lock(gate);if(closed)return;closed=true;events.clear();for(auto& weak:channels)if(auto c=weak.lock())live.push_back(std::move(c));channels.clear();}
        for(auto& c:live){{std::lock_guard lock(c->gate);c->closed=true;c->packets.clear();c->bytes=0;}c->value->close();}
        value->close();
    }
    ~Peer(){try{close();}catch(...){}}
};
struct PeerHandle {std::shared_ptr<Peer> value;};
struct ChannelHandle {std::shared_ptr<Channel> value;};
size_t size(const rtc::message_variant& message) {return std::visit([](auto& value){return value.size();},message);}
std::shared_ptr<Channel> wrapChannel(const std::shared_ptr<Peer>& peer,std::shared_ptr<rtc::DataChannel> value) {
    auto result=std::make_shared<Channel>(std::move(value));std::weak_ptr<Channel> weak=result;
    result->value->onMessage([weak](rtc::message_variant message){if(auto c=weak.lock()){std::lock_guard lock(c->gate);if(c->closed)return;auto length=size(message);if(c->packets.size()>=1024||length>4*1024*1024-c->bytes){c->overflow=true;return;}c->bytes+=length;c->packets.push_back(std::move(message));}});
    {std::lock_guard lock(peer->gate);if(peer->closed)throw std::runtime_error("Peer is closed");peer->channels.push_back(result);}
    return result;
}
std::shared_ptr<Peer> peer(void* handle){if(!handle)throw std::invalid_argument("Null peer");return static_cast<PeerHandle*>(handle)->value;}
std::shared_ptr<Channel> channel(void* handle){if(!handle)throw std::invalid_argument("Null channel");return static_cast<ChannelHandle*>(handle)->value;}
}
API const char* e2d_rtc_error(){return error.c_str();}
API void* e2d_rtc_create(const char** urls,const char** users,const char** passwords,int count){return attempt([&]()->void*{
    if(count<0||count>1024)throw std::invalid_argument("Invalid ICE server count");
    rtc::Configuration config;config.disableAutoNegotiation=true;config.maxMessageSize=65536;
    for(int i=0;i<count;i++){rtc::IceServer server(urls[i]);if(users[i])server.username=users[i];if(passwords[i])server.password=passwords[i];config.iceServers.push_back(std::move(server));}
    auto state=std::make_shared<Peer>();state->value=std::make_shared<rtc::PeerConnection>(config);std::weak_ptr<Peer> weak=state;
    state->value->onLocalCandidate([weak](rtc::Candidate candidate){if(auto p=weak.lock())p->push({2,candidate.mid(),candidate.candidate(),nullptr});});
    state->value->onDataChannel([weak](std::shared_ptr<rtc::DataChannel> data){if(auto p=weak.lock()){try{p->push({3,"","",wrapChannel(p,std::move(data))});}catch(...){std::lock_guard lock(p->gate);p->overflow=true;}}});
    return new PeerHandle{state};},static_cast<void*>(nullptr));}
API void e2d_rtc_destroy(void* handle){delete static_cast<PeerHandle*>(handle);}
API int e2d_rtc_close(void* handle){return attempt([&]{peer(handle)->close();return 0;},-1);}
API int e2d_rtc_state(void* handle,int kind){return attempt([&]{auto p=peer(handle);if(kind==0)return int(p->value->state());if(kind==1)return int(p->value->gatheringState());if(kind==2){std::lock_guard lock(p->gate);return p->closed?5:int(p->value->signalingState());}throw std::invalid_argument("Invalid state kind");},-1);}
API int e2d_rtc_offer(void* handle){return attempt([&]{auto p=peer(handle);auto description=p->value->createOffer();p->proposal=std::string(description);p->proposalType="offer";p->push({1,"offer",p->proposal,nullptr});return 0;},-1);}
API int e2d_rtc_local(void* handle,const char* type,const char* sdp){return attempt([&]{auto p=peer(handle);if(p->proposalType!=type||std::string(rtc::Description(p->proposal,type))!=std::string(rtc::Description(sdp,type)))throw std::invalid_argument("Local SDP is not the current emitted proposal");p->value->setLocalDescription(rtc::Description::stringToType(type));p->proposal.clear();p->proposalType.clear();return 0;},-1);}
API int e2d_rtc_remote(void* handle,const char* type,const char* sdp){return attempt([&]{auto p=peer(handle);p->value->setRemoteDescription(rtc::Description(sdp,type));if(std::string(type)=="offer"){auto description=p->value->createAnswer();p->proposal=std::string(description);p->proposalType="answer";p->push({1,"answer",p->proposal,nullptr});}return 0;},-1);}
API int e2d_rtc_candidate(void* handle,const char* mid,const char* candidate){return attempt([&]{peer(handle)->value->addRemoteCandidate(rtc::Candidate(candidate,mid));return 0;},-1);}
API void* e2d_rtc_event(void* handle){return attempt([&]()->void*{auto p=peer(handle);std::lock_guard lock(p->gate);if(p->overflow)throw std::runtime_error("Peer event queue overflow");if(p->events.empty())return nullptr;auto value=new Event(std::move(p->events.front()));p->events.pop_front();return value;},static_cast<void*>(nullptr));}
API int e2d_rtc_event_kind(void* handle){return static_cast<Event*>(handle)->kind;}
API const char* e2d_rtc_event_string(void* handle,int part){auto e=static_cast<Event*>(handle);return part?e->second.c_str():e->first.c_str();}
API void* e2d_rtc_event_channel(void* handle){auto e=static_cast<Event*>(handle);return attempt([&]()->void*{return e->channel?new ChannelHandle{std::move(e->channel)}:nullptr;},static_cast<void*>(nullptr));}
API void e2d_rtc_event_release(void* handle){delete static_cast<Event*>(handle);}
API void* e2d_rtc_channel_create(void* handle,const char* label,const char* protocol,int ordered,int negotiated,int id,int lifetime,int retransmits){return attempt([&]()->void*{
    auto p=peer(handle);rtc::DataChannelInit init;init.protocol=protocol;init.negotiated=negotiated!=0;if(id>=0)init.id=static_cast<uint16_t>(id);init.reliability.unordered=ordered==0;
    if(lifetime>=0)init.reliability.maxPacketLifeTime=std::chrono::milliseconds(lifetime);if(retransmits>=0)init.reliability.maxRetransmits=static_cast<unsigned>(retransmits);
    return new ChannelHandle{wrapChannel(p,p->value->createDataChannel(label,init))};},static_cast<void*>(nullptr));}
API void e2d_rtc_channel_destroy(void* handle){delete static_cast<ChannelHandle*>(handle);}
API int e2d_rtc_channel_close(void* handle){return attempt([&]{auto c=channel(handle);{std::lock_guard lock(c->gate);c->closed=true;c->packets.clear();c->bytes=0;}c->value->close();return 0;},-1);}
API int e2d_rtc_channel_poll(void* handle){return attempt([&]{auto c=channel(handle);bool overflow;{std::lock_guard lock(c->gate);overflow=c->overflow;}if(overflow){c->value->close();throw std::runtime_error("Channel receive queue overflow");}return 0;},-1);}
API int e2d_rtc_channel_info(void* handle,int kind){return attempt([&]{auto c=channel(handle);auto value=c->value;switch(kind){case 0:return value->isClosed()?3:value->isOpen()?1:0;case 1:return value->id()?int(*value->id()):65535;case 2:return int(value->maxMessageSize());case 3:return int(value->bufferedAmount());case 4:return value->reliability().unordered?0:1;case 5:return value->reliability().maxPacketLifeTime?int(value->reliability().maxPacketLifeTime->count()):65535;case 6:return value->reliability().maxRetransmits?int(*value->reliability().maxRetransmits):65535;case 7:{std::lock_guard lock(c->gate);return int(c->packets.size());}}throw std::invalid_argument("Invalid channel info kind");},-1);}
API const char* e2d_rtc_channel_string(void* handle,int kind){static thread_local std::string text;return attempt([&]()->const char*{auto c=channel(handle);text=kind?c->value->protocol():c->value->label();return text.c_str();},static_cast<const char*>(nullptr));}
API int e2d_rtc_channel_size(void* handle){return attempt([&]{auto c=channel(handle);std::lock_guard lock(c->gate);return c->packets.empty()?-1:int(size(c->packets.front()));},-2);}
API int e2d_rtc_channel_read(void* handle,void* destination,int capacity,int* text){return attempt([&]{auto c=channel(handle);std::lock_guard lock(c->gate);if(c->packets.empty())throw std::runtime_error("No packet available");auto& packet=c->packets.front();auto length=size(packet);if(capacity<0||size_t(capacity)<length)throw std::invalid_argument("Packet destination is too small");*text=std::holds_alternative<rtc::string>(packet)?1:0;std::visit([&](auto& value){if(length)std::memcpy(destination,value.data(),length);},packet);c->bytes-=length;c->packets.pop_front();return int(length);},-1);}
API int e2d_rtc_channel_send(void* handle,const void* source,int length,int text){return attempt([&]{auto c=channel(handle);if(length<0||size_t(length)>c->value->maxMessageSize())throw std::invalid_argument("Packet exceeds remote capacity");auto bytes=static_cast<const char*>(source);if(text)c->value->send(std::string(bytes?bytes:"",size_t(length)));else{rtc::binary data(size_t(length),std::byte{0});if(length)std::memcpy(data.data(),source,size_t(length));c->value->send(std::move(data));}return 0;},-1);}
