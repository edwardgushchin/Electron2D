cmake_minimum_required(VERSION 3.24)
project(Electron2DOGV LANGUAGES C)
include(FetchContent)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(CMAKE_C_VISIBILITY_PRESET hidden)
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)
set(BUILD_TESTING OFF CACHE BOOL "" FORCE)
set(INSTALL_DOCS OFF CACHE BOOL "" FORCE)
FetchContent_Declare(Electron2DOGG
  URL https://downloads.xiph.org/releases/ogg/libogg-1.3.6.tar.xz
  URL_HASH SHA256=5c8253428e181840cd20d41f3ca16557a9cc04bad4a3d04cce84808677fa1061)
FetchContent_Declare(Electron2DVorbis
  URL https://downloads.xiph.org/releases/vorbis/libvorbis-1.3.7.tar.xz
  URL_HASH SHA256=b33cc4934322bcbf6efcbacf49e3ca01aadbea4114ec9589d1b1e9d20f72954b
  SOURCE_SUBDIR unused)
FetchContent_Declare(Electron2DTheora
  URL https://downloads.xiph.org/releases/theora/libtheora-1.2.0.tar.gz
  URL_HASH SHA256=279327339903b544c28a92aeada7d0dcfd0397b59c2f368cc698ac56f515906e
  SOURCE_SUBDIR unused)
FetchContent_MakeAvailable(Electron2DOGG Electron2DVorbis Electron2DTheora)
# Portable source lists follow the releases' lib/Makefile.am and lib/CMakeLists.txt.
set(vorbis_sources mdct smallft block envelope window lsp lpc analysis synthesis psy info floor1 floor0 res0 mapping0 registry codebook sharedbook lookup bitrate)
foreach(source IN LISTS vorbis_sources)
  list(APPEND vorbis_files "${electron2dvorbis_SOURCE_DIR}/lib/${source}.c")
endforeach()
include("${CMAKE_CURRENT_SOURCE_DIR}/vorbis-residue.cmake")
add_library(Electron2DVorbisCodec STATIC ${vorbis_files} "${CMAKE_CURRENT_BINARY_DIR}/vorbisenc.c")
target_include_directories(Electron2DVorbisCodec PUBLIC "${electron2dvorbis_SOURCE_DIR}/include" PRIVATE "${electron2dvorbis_SOURCE_DIR}/lib")
target_link_libraries(Electron2DVorbisCodec PUBLIC ogg)
set(theora_sources apiwrapper bitpack dequant fragment idct info internal state quant analyze fdct encfrag encapiwrapper encinfo encode enquant huffenc mathops mcenc rate tokenize)
foreach(source IN LISTS theora_sources)
  list(APPEND theora_files "${electron2dtheora_SOURCE_DIR}/lib/${source}.c")
endforeach()
add_library(Electron2DTheoraCodec STATIC ${theora_files})
target_include_directories(Electron2DTheoraCodec PUBLIC "${electron2dtheora_SOURCE_DIR}/include" PRIVATE "${electron2dtheora_SOURCE_DIR}/lib")
target_link_libraries(Electron2DTheoraCodec PUBLIC ogg)
set(kind SHARED)
if(ELECTRON2D_STATIC_NATIVE)
  set(kind STATIC)
endif()
add_library(Electron2DOGV ${kind} bridge.c)
target_compile_definitions(Electron2DOGV PRIVATE E2D_OGV_BUILD)
if(ELECTRON2D_STATIC_NATIVE)
  target_compile_definitions(Electron2DOGV PUBLIC E2D_OGV_STATIC)
endif()
target_link_libraries(Electron2DOGV PRIVATE Electron2DTheoraCodec Electron2DVorbisCodec)
if(NOT WIN32)
  target_link_libraries(Electron2DOGV PRIVATE m)
endif()
target_compile_features(Electron2DOGV PRIVATE c_std_11)
if(APPLE AND NOT ELECTRON2D_STATIC_NATIVE)
  set_target_properties(Electron2DOGV PROPERTIES INSTALL_NAME_DIR "@rpath" BUILD_WITH_INSTALL_NAME_DIR TRUE SKIP_BUILD_RPATH TRUE)
elseif(WIN32)
  target_sources(Electron2DOGV PRIVATE exports.def)
endif()
# Static application profiles consume one archive with its private codec closure.
if(ELECTRON2D_STATIC_NATIVE)
  set_property(TARGET Electron2DOGV APPEND PROPERTY LINK_DEPENDS "${CMAKE_CURRENT_SOURCE_DIR}/merge.cmake")
  add_custom_command(TARGET Electron2DOGV POST_BUILD
    COMMAND ${CMAKE_COMMAND} -DOUTPUT=$<TARGET_FILE:Electron2DOGV> -DOGG=$<TARGET_FILE:ogg>
      -DVORBIS=$<TARGET_FILE:Electron2DVorbisCodec> -DTHEORA=$<TARGET_FILE:Electron2DTheoraCodec>
      -DAR=${CMAKE_AR} -DAPPLE_HOST=${APPLE} -P "${CMAKE_CURRENT_SOURCE_DIR}/merge.cmake")
endif()
add_executable(Electron2DOGVCheck EXCLUDE_FROM_ALL test_bridge.c)
target_link_libraries(Electron2DOGVCheck PRIVATE Electron2DOGV)

if(NOT WIN32)
  target_link_libraries(Electron2DOGVCheck PRIVATE m)
endif()

add_executable(Electron2DVorbisResidueCheck EXCLUDE_FROM_ALL test_vorbis_residue.c)
target_link_libraries(Electron2DVorbisResidueCheck PRIVATE Electron2DVorbisCodec)
if(NOT WIN32)
  target_link_libraries(Electron2DVorbisResidueCheck PRIVATE m)
endif()
