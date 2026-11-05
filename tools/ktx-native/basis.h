#ifndef E2D_BASIS_H
#define E2D_BASIS_H
#include <cstdint>
#include "memory.hpp"

bool e2d_basis_align_slices(const uint8_t *source, uint32_t size,
    e2d_ktx::vector<uint8_t> &storage, const uint8_t *&data, uint32_t &length);
#endif
