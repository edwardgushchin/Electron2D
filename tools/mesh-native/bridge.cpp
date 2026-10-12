#include "meshoptimizer.h"
#include <cmath>
#include <limits>
#include <new>
#if defined(_WIN32)
#define E2D_EXPORT extern "C" __declspec(dllexport)
#else
#define E2D_EXPORT extern "C" __attribute__((visibility("default")))
#endif
E2D_EXPORT float e2d_mesh_scale(const float* positions, size_t count) {
    return meshopt_simplifyScale(positions, count, 3 * sizeof(float));
}
E2D_EXPORT size_t e2d_mesh_simplify(unsigned int* output, const unsigned int* indices, size_t count,
    const float* positions, size_t vertices, const float* attributes, const float* weights,
    size_t attribute_count, size_t target, unsigned int options, float* error) {
    try {
        return meshopt_simplifyWithAttributes(output, indices, count, positions, vertices, 3 * sizeof(float),
            attributes, attribute_count * sizeof(float), weights, attribute_count, nullptr, target, 1.0f, options, error);
    } catch (const std::bad_alloc&) {
        *error = std::numeric_limits<float>::quiet_NaN();
        return size_t(-1);
    }
}
