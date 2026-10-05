cmake_minimum_required(VERSION 3.16)
project(Electron2DENet C)
set(base "${CMAKE_CURRENT_SOURCE_DIR}/../../src/Vendor")
set(ENET_ROOT "${base}/ENet")
find_package(ZLIB REQUIRED)
if(APPLE)
  include(FetchContent)
  FetchContent_Declare(Zstd
    URL https://github.com/facebook/zstd/releases/download/v1.5.7/zstd-1.5.7.tar.gz
    URL_HASH SHA256=eb33e51f49a15e023950cd7825ca74a4a2b43db8354825ac24fc1b7ee09e6fa3
    SOURCE_SUBDIR build/cmake)
  set(ZSTD_BUILD_SHARED OFF CACHE BOOL "" FORCE)
  set(ZSTD_BUILD_PROGRAMS OFF CACHE BOOL "" FORCE)
  set(ZSTD_BUILD_TESTS OFF CACHE BOOL "" FORCE)
  set(ZSTD_MULTITHREAD_SUPPORT OFF CACHE BOOL "" FORCE)
  set(CMAKE_POSITION_INDEPENDENT_CODE ON)
  FetchContent_MakeAvailable(Zstd)
  set(ZSTD_LIBRARY libzstd_static)
else()
  find_library(ZSTD_LIBRARY NAMES zstd REQUIRED)
endif()
add_library(Electron2DENet SHARED bridge.c
  "${base}/ENet/callbacks.c" "${base}/ENet/compress.c" "${base}/ENet/host.c"
  "${base}/ENet/list.c" "${base}/ENet/packet.c" "${base}/ENet/peer.c" "${base}/ENet/protocol.c"
  "${base}/FastLZ/fastlz.c")
target_include_directories(Electron2DENet PRIVATE "${base}/ENet/include" "${base}/FastLZ")
target_link_libraries(Electron2DENet PRIVATE ZLIB::ZLIB "${ZSTD_LIBRARY}")
set_target_properties(Electron2DENet PROPERTIES C_VISIBILITY_PRESET hidden LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}")

if(APPLE)
  set_target_properties(Electron2DENet PROPERTIES INSTALL_NAME_DIR "@rpath" BUILD_WITH_INSTALL_NAME_DIR TRUE SKIP_BUILD_RPATH TRUE)
  target_link_options(Electron2DENet PRIVATE "-Wl,-exported_symbols_list,${CMAKE_CURRENT_SOURCE_DIR}/exports-macos.txt")
elseif(WIN32)
  target_link_libraries(Electron2DENet PRIVATE bcrypt ws2_32)
else()
  target_link_options(Electron2DENet PRIVATE "-Wl,--no-undefined" "-Wl,-soname,libElectron2DENet.so")
endif()

# Test-only stock socket backend: independent of the engine routing/DTLS adapter.
add_executable(Electron2DENetOracle EXCLUDE_FROM_ALL oracle.c
  ${ENET_ROOT}/callbacks.c ${ENET_ROOT}/compress.c ${ENET_ROOT}/host.c
  ${ENET_ROOT}/list.c ${ENET_ROOT}/packet.c ${ENET_ROOT}/peer.c
  ${ENET_ROOT}/protocol.c ${ENET_ROOT}/unix.c)
target_include_directories(Electron2DENetOracle PRIVATE ${ENET_ROOT}/include)
target_compile_definitions(Electron2DENetOracle PRIVATE HAS_SOCKLEN_T=1 HAS_FCNTL=1 HAS_POLL=1 HAS_INET_PTON=1 HAS_INET_NTOP=1 HAS_MSGHDR_FLAGS=1)
