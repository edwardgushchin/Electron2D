cmake_minimum_required(VERSION 3.16)
project(Electron2DAudio C)
# Fetch only the matching headers; the SDL library comes from NuGet.
if(NOT ELECTRON2D_SDL_VERSION STREQUAL "3.4.16")
    message(FATAL_ERROR "Update the pinned SDL headers and archive hash with the native package version.")
endif()
if(NOT EXISTS "${SDL3_LIBRARIES}")
    message(FATAL_ERROR "SDL3_LIBRARIES must name the selected RID's restored SDL3-CS native library.")
endif()
include(FetchContent)
FetchContent_Declare(SDL3Headers
    URL "https://www.libsdl.org/release/SDL3-3.4.16.tar.gz"
    URL_HASH SHA256=7322236cd12090c3eb40b9728be4d49c76f66ad17d04369584d4ecad5cf77c68
    SOURCE_SUBDIR "headers-only")
FetchContent_MakeAvailable(SDL3Headers)
set(SDL3_INCLUDE_DIRS "${sdl3headers_SOURCE_DIR}/include")
add_subdirectory("${CMAKE_CURRENT_SOURCE_DIR}/../../src/Vendor/FAudio" faudio)
target_sources(FAudio-shared PRIVATE output.c)
target_include_directories(FAudio-shared PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}/../../src/Vendor/FAudio/src")
set_target_properties(FAudio-shared PROPERTIES LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}" SKIP_BUILD_RPATH TRUE)
