#include "meshoptimizer.h"
#include <algorithm>
#include <cmath>
#include <iostream>
#include <vector>
int main() {
    std::cout << "{\"commit\":\"b22872835dbabc56a6e4a366ea9917f62b7daf1a\",\"records\":[";
    bool first_record = true;
    for (unsigned cells : {6u, 8u, 16u}) for (bool deformable : {false, true}) {
        size_t vertices = (cells + 1) * (cells + 1); std::vector<float> positions(vertices * 3), attributes(vertices * 6, 1); std::vector<unsigned> current;
        for (unsigned y = 0; y <= cells; ++y) for (unsigned x = 0; x <= cells; ++x) { unsigned i = y * (cells + 1) + x; positions[i * 3] = x * 2.f; positions[i * 3 + 1] = y * 2.f; attributes[i * 6] = attributes[i * 6 + 1] = 0; attributes[i * 6 + 2] = -1; }
        for (unsigned y = 0; y < cells; ++y) for (unsigned x = 0; x < cells; ++x) { unsigned a = y * (cells + 1) + x; for (unsigned i : {a,a+1,a+cells+2,a,a+cells+2,a+cells+1}) current.push_back(i); }
        if (!first_record) std::cout << ','; first_record = false;
        std::cout << "{\"cells\":" << cells << ",\"deformable\":" << (deformable ? "true" : "false") << ",\"levels\":[";
        float weights[6] = {1,1,1,1,1,1}; float error = 0; bool prune = true, first_level = true;
        while (current.size() > 24) {
            unsigned target = std::max(unsigned(current.size()/3/2*3), 12u); std::vector<unsigned> next(current.size()); float step = 0;
            unsigned options = meshopt_SimplifySparse | meshopt_SimplifyLockBorder | (prune ? meshopt_SimplifyPrune : 0) | (deformable ? meshopt_SimplifyRegularize : 0);
            size_t size = meshopt_simplifyWithAttributes(next.data(),current.data(),current.size(),positions.data(),vertices,12,attributes.data(),24,weights,6,nullptr,target,1.f,options,&step);
            if (!size && prune) {prune=false;continue;}
            error = std::max(error*1.5f,step);
            if (!size || size >= current.size()*.75f || error > 1.f) break;
            next.resize(size); current=next;
            if (!first_level) std::cout << ','; first_level=false;
            std::cout << '['; for (size_t i=0;i<current.size();++i) {if(i)std::cout<<',';std::cout<<current[i];} std::cout << ']';
        }
        std::cout << "]}";
    }
    std::cout << "]}\n";
}
