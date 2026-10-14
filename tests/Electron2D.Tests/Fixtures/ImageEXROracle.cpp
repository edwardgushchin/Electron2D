#include <OpenEXR/ImfInputFile.h>
#include <OpenEXR/ImfOutputFile.h>
#include <OpenEXR/ImfTiledOutputFile.h>
#include <OpenEXR/ImfChannelList.h>
#include <OpenEXR/ImfFrameBuffer.h>
#include <OpenEXR/ImfHeader.h>
#include <OpenEXR/ImfTileDescription.h>
#include <Imath/half.h>
#include <filesystem>
#include <fstream>
#include <vector>
#include <cstring>
#include <iostream>
using namespace OPENEXR_IMF_NAMESPACE;
using namespace IMATH_NAMESPACE;
static void generate(const std::string& root) {
    std::filesystem::create_directories(root);
    const char* names[] = {"R", "G", "B", "A"};
    for (int pixel=0;pixel<3;pixel++) for(int channels=1;channels<=4;channels++) for(int tiled=0;tiled<2;tiled++) for(int compression=0;compression<8;compression++) {
        const PixelType type=pixel==0?HALF:pixel==1?FLOAT:UINT;
        const int width=7,height=5; Box2i region(V2i(-4,-3),V2i(2,1));
        Header header(width,height);header.dataWindow()=region;header.displayWindow()=region;header.compression()=static_cast<Compression>(compression);
        if(tiled)header.setTileDescription(TileDescription(4,3,ONE_LEVEL));
        FrameBuffer frame; std::vector<std::vector<half>> halves(channels,std::vector<half>(width*height));
        std::vector<std::vector<float>> floats(channels,std::vector<float>(width*height));
        std::vector<std::vector<unsigned>> ints(channels,std::vector<unsigned>(width*height));
        for(int c=0;c<channels;c++) {
            header.channels().insert(names[c],Channel(type));
            for(int i=0;i<width*height;i++){floats[c][i]=(i%11-5)*0.25f+c*2.0f;halves[c][i]=floats[c][i];ints[c][i]=unsigned(i*19+c*31);}
            void* data=pixel==0?static_cast<void*>(halves[c].data()):pixel==1?static_cast<void*>(floats[c].data()):static_cast<void*>(ints[c].data());
            size_t size=pixel==0?2:4;
            frame.insert(names[c],Slice::Make(type,data,region,size,width*size));
        }
        const std::string path=root+"/native-"+std::to_string(pixel)+"-"+std::to_string(channels)+"-"+std::to_string(tiled)+"-"+std::to_string(compression)+".exr";
        if(tiled){TiledOutputFile file(path.c_str(),header);file.setFrameBuffer(frame);file.writeTiles(0,file.numXTiles()-1,0,file.numYTiles()-1);}
        else {OutputFile file(path.c_str(),header);file.setFrameBuffer(frame);file.writePixels(height);}
    }
}
static void decode(const char* path,const char* destination) {
    InputFile file(path);auto region=file.header().dataWindow();int width=region.max.x-region.min.x+1,height=region.max.y-region.min.y+1;
    const auto& channels=file.header().channels();const bool y=channels.findChannel("Y")!=nullptr;
    int count=y?(channels.findChannel("A")?4:3):(channels.findChannel("A")?4:channels.findChannel("B")?3:channels.findChannel("G")?2:1);
    bool halfType=false;for(auto it=channels.begin();it!=channels.end();++it)halfType|=it.channel().type==HALF;
    std::vector<float> data(width*height*count);FrameBuffer frame;const char* names[]={"R","G","B","A"};
    for(int c=0;c<count;c++)frame.insert(y&&c<3?"Y":names[c],Slice::Make(FLOAT,data.data()+c,region,count*4,width*count*4));
    // A monochrome source uses one slice, then expands explicitly below.
    if(y){frame=FrameBuffer();frame.insert("Y",Slice::Make(FLOAT,data.data(),region,count*4,width*count*4));if(count==4)frame.insert("A",Slice::Make(FLOAT,data.data()+3,region,count*4,width*count*4));}
    file.setFrameBuffer(frame);file.readPixels(region.min.y,region.max.y);
    if(y)for(int i=0;i<width*height;i++)data[i*count+1]=data[i*count+2]=data[i*count];
    std::ofstream out(destination,std::ios::binary);int precision=halfType?2:4;for(int value:{width,height,count,precision})out.write(reinterpret_cast<char*>(&value),4);
    for(float value:data){if(halfType){half h=value;auto bits=h.bits();out.write(reinterpret_cast<char*>(&bits),2);}else out.write(reinterpret_cast<char*>(&value),4);}
}
int main(int argc,char** argv){try{if(argc==3&&std::strcmp(argv[1],"generate")==0)generate(argv[2]);else if(argc==4&&std::strcmp(argv[1],"decode")==0)decode(argv[2],argv[3]);else return 2;return 0;}catch(const std::exception& e){std::cerr<<e.what()<<"\n";return 1;}}
