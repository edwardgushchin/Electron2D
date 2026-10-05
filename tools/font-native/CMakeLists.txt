cmake_minimum_required(VERSION 3.16)
project(Electron2DFreeType LANGUAGES C CXX)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
include(FetchContent)

FetchContent_Declare(Zlib
    GIT_REPOSITORY https://github.com/madler/zlib.git
    GIT_TAG 51b7f2abdade71cd9bb0e7a373ef2610ec6f9daf)
FetchContent_Declare(Png
    GIT_REPOSITORY https://github.com/pnggroup/libpng.git
    GIT_TAG ed217e3e601d8e462f7fd1e04bed43ac42212429)
FetchContent_Declare(Brotli
    GIT_REPOSITORY https://github.com/google/brotli.git
    GIT_TAG ed738e842d2fbdf2d6459e39267a633c4a9b2f5d)
FetchContent_Declare(HarfBuzz
    GIT_REPOSITORY https://github.com/harfbuzz/harfbuzz.git
    GIT_TAG 4a1d891c6317d2c83e5f3c2607ec5f5ccedffcde)
FetchContent_Declare(FreeType
    GIT_REPOSITORY https://github.com/freetype/freetype.git
    GIT_TAG 42608f77f20749dd6ddc9e0536788eaad70ea4b5)

set(BUILD_SHARED_LIBS OFF)
set(ZLIB_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(PNG_SHARED OFF CACHE BOOL "" FORCE)
set(PNG_TESTS OFF CACHE BOOL "" FORCE)
set(PNG_TOOLS OFF CACHE BOOL "" FORCE)
set(SKIP_INSTALL_ALL ON CACHE BOOL "" FORCE)
set(BROTLI_DISABLE_TESTS ON CACHE BOOL "" FORCE)
set(HB_BUILD_SUBSET OFF CACHE BOOL "" FORCE)
set(HB_HAVE_FREETYPE OFF CACHE BOOL "" FORCE)
set(HB_HAVE_CORETEXT OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(Zlib)
set(ZLIB_LIBRARY zlibstatic CACHE STRING "" FORCE)
set(ZLIB_INCLUDE_DIR "${zlib_SOURCE_DIR}" CACHE PATH "" FORCE)
set(ZLIB_INCLUDE_DIRS "${zlib_SOURCE_DIR};${zlib_BINARY_DIR}")
add_library(ZLIB::ZLIB ALIAS zlibstatic)
FetchContent_MakeAvailable(Png Brotli HarfBuzz)

set(PNG_LIBRARY png_static CACHE STRING "" FORCE)
set(PNG_PNG_INCLUDE_DIR "${png_SOURCE_DIR}" CACHE PATH "" FORCE)
set(BROTLIDEC_LIBRARIES brotlidec CACHE STRING "" FORCE)
set(BROTLIDEC_INCLUDE_DIRS "${brotli_SOURCE_DIR}/c/include" CACHE PATH "" FORCE)
set(HarfBuzz_LIBRARY harfbuzz CACHE STRING "" FORCE)
set(HarfBuzz_INCLUDE_DIR "${harfbuzz_SOURCE_DIR}/src" CACHE PATH "" FORCE)
set(FT_REQUIRE_ZLIB ON CACHE BOOL "" FORCE)
set(FT_REQUIRE_PNG ON CACHE BOOL "" FORCE)
set(FT_REQUIRE_BROTLI ON CACHE BOOL "" FORCE)
set(FT_REQUIRE_HARFBUZZ ON CACHE BOOL "" FORCE)
set(FT_DISABLE_BZIP2 ON CACHE BOOL "" FORCE)
set(BUILD_SHARED_LIBS ON)
FetchContent_MakeAvailable(FreeType)
set_property(TARGET freetype PROPERTY VERSION)
set_property(TARGET freetype PROPERTY SOVERSION)
set_target_properties(freetype PROPERTIES OUTPUT_NAME Electron2DFreeType
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}" RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}"
    SKIP_BUILD_RPATH TRUE)
if(APPLE)
    set_target_properties(freetype PROPERTIES INSTALL_NAME_DIR "@rpath" BUILD_WITH_INSTALL_NAME_DIR TRUE)
endif()
