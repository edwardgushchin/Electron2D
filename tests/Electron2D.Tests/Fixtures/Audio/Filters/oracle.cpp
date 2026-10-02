#include "audio_filter_sw.h"
#include <cstdio>
int main(){
 const AudioFilterSW::Mode modes[]={AudioFilterSW::LOWPASS,AudioFilterSW::HIGHPASS,AudioFilterSW::BANDPASS,AudioFilterSW::NOTCH,AudioFilterSW::LOWSHELF,AudioFilterSW::HIGHSHELF};
 const char* names[]={"LowPass","HighPass","BandPass","Notch","LowShelf","HighShelf"};
 std::printf("["); bool comma=false;
 for(int k=0;k<6;k++)for(int stages=1;stages<=4;stages++)for(int profile=0;profile<3;profile++){
  if(comma)std::printf(",");comma=true;
  float cutoff=profile==0?2000:profile==1?3000:400;
  float resonance=profile==0?.5f:profile==1?.9f:0;
  float gain=profile==0?1:profile==1?2:0;
  AudioFilterSW filter;filter.set_mode(modes[k]);filter.set_cutoff(cutoff);filter.set_resonance(resonance);filter.set_gain(gain);filter.set_stages(stages);filter.set_sampling_rate(44100);
  AudioFilterSW::Processor processors[2][4];for(int c=0;c<2;c++)for(int s=0;s<4;s++){processors[c][s].set_filter(&filter);processors[c][s].update_coeffs();}
  AudioFilterSW::Coeffs coeffs;filter.prepare_coefficients(&coeffs);
  std::printf("{\"Kind\":\"%s\",\"Stages\":%d,\"Cutoff\":%.9g,\"Resonance\":%.9g,\"Gain\":%.9g,\"Coefficients\":[%.17g,%.17g,%.17g,%.17g,%.17g],\"PCM\":[",names[k],stages,cutoff,resonance,gain,coeffs.b0,coeffs.b1,coeffs.b2,coeffs.a1,coeffs.a2);
  for(int i=0;i<256;i++)for(int c=0;c<2;c++){
   float input = i==0 ? (c==0?.75f:-.5f) : float((i*37+c*11)%101-50)/128;
   for(int s=0;s<stages;s++)processors[c][s].process_one(input);
   std::printf("%s%.9g",i||c?",":"",input);
  }
  std::printf("]}");
 }
 std::printf("]\n");
}
