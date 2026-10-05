cmake_minimum_required(VERSION 3.16)
project(Electron2DFreeType LANGUAGES C CXX)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
include(FetchContent)

FetchContent_Declare(Zlib
    GIT_REPOSITORY https://github.com/madler/zlib.git
    GIT_TAG da607da739fa6047df13e66a2af6b8bec7c2a498)
FetchContent_Declare(Png
    GIT_REPOSITORY https://github.com/pnggroup/libpng.git
    GIT_TAG cd952f49f95bb27154ae77dbb103032d95f6e580)
FetchContent_Declare(Brotli
    GIT_REPOSITORY https://github.com/google/brotli.git
    GIT_TAG 028fb5a23661f123017c060daa546b55cf4bde29)
FetchContent_Declare(HarfBuzz
    GIT_REPOSITORY https://github.com/harfbuzz/harfbuzz.git
    GIT_TAG eb033319fc2ebe723597fa497d4bb4331bab8b6b)
FetchContent_Declare(FreeType
    GIT_REPOSITORY https://github.com/freetype/freetype.git
    GIT_TAG 0a0221a1347e2f1e07c395263540026e9a0aa7c7)

set(BUILD_SHARED_LIBS OFF)
set(ZLIB_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(PNG_SHARED OFF CACHE BOOL "" FORCE)
set(PNG_STATIC ON CACHE BOOL "" FORCE)
set(PNG_FRAMEWORK OFF CACHE BOOL "" FORCE)
set(PNG_TESTS OFF CACHE BOOL "" FORCE)
set(PNG_TOOLS OFF CACHE BOOL "" FORCE)
set(SKIP_INSTALL_ALL ON CACHE BOOL "" FORCE)
set(BROTLI_DISABLE_TESTS ON CACHE BOOL "" FORCE)
set(HB_BUILD_SUBSET OFF CACHE BOOL "" FORCE)
set(HB_HAVE_FREETYPE OFF CACHE BOOL "" FORCE)
set(HB_HAVE_CORETEXT OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(Zlib)
if(EMSCRIPTEN)
    # Upstream also declares a shared target, which this toolchain converts to an archive.
    set_target_properties(zlib PROPERTIES OUTPUT_NAME z-unused EXCLUDE_FROM_ALL TRUE)
endif()
set(ZLIB_LIBRARY zlibstatic CACHE STRING "" FORCE)
configure_file("${zlib_SOURCE_DIR}/zlib.h" "${zlib_BINARY_DIR}/zlib.h" COPYONLY)
set(ZLIB_INCLUDE_DIR "${zlib_BINARY_DIR}" CACHE PATH "" FORCE)
set(ZLIB_INCLUDE_DIRS "${zlib_BINARY_DIR}")
add_library(ZLIB::ZLIB ALIAS zlibstatic)
FetchContent_MakeAvailable(Png Brotli)
if(EMSCRIPTEN)
    target_link_libraries(brotlidec brotlicommon)
endif()
if(ANDROID)
    set(BUILD_SHARED_LIBS ON)
endif()
FetchContent_MakeAvailable(HarfBuzz)
if(ANDROID OR ELECTRON2D_STATIC_NATIVE)
    set_target_properties(harfbuzz PROPERTIES OUTPUT_NAME Electron2DHarfBuzz
        LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}" ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}"
        SKIP_BUILD_RPATH TRUE)
    set_property(TARGET harfbuzz PROPERTY VERSION)
    set_property(TARGET harfbuzz PROPERTY SOVERSION)
endif()

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
if(ELECTRON2D_STATIC_NATIVE)
    set(BUILD_SHARED_LIBS OFF)
endif()
FetchContent_MakeAvailable(FreeType)
target_include_directories(freetype PRIVATE "${png_BINARY_DIR}")
set_property(TARGET freetype PROPERTY VERSION)
set_property(TARGET freetype PROPERTY SOVERSION)
set_target_properties(freetype PROPERTIES OUTPUT_NAME Electron2DFreeType
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}" RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}"
    ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}"
    SKIP_BUILD_RPATH TRUE)
if(APPLE)
    set_target_properties(freetype PROPERTIES INSTALL_NAME_DIR "@rpath" BUILD_WITH_INSTALL_NAME_DIR TRUE)
endif()

add_executable(Electron2DFreeTypeCheck EXCLUDE_FROM_ALL test_freetype.c)
target_link_libraries(Electron2DFreeTypeCheck PRIVATE freetype)
set_target_properties(Electron2DFreeTypeCheck PROPERTIES RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}")
if(EMSCRIPTEN)
    target_link_options(Electron2DFreeTypeCheck PRIVATE "--embed-file" "${ELECTRON2D_CHECK_FONT}@/font.woff2"
        "-sWASM_ASYNC_COMPILATION=0" "-sALLOW_MEMORY_GROWTH=1")
endif()
