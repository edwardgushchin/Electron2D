include(FetchContent)

set(zlib_revision 99623ef7097352da7993ba614161fd6a311e046e)
FetchContent_Declare(Zlib
    GIT_REPOSITORY https://github.com/madler/zlib.git
    GIT_TAG ${zlib_revision}
    GIT_CONFIG core.autocrlf=false core.longpaths=true)

set(ZLIB_BUILD_TESTING OFF CACHE BOOL "" FORCE)
set(ZLIB_BUILD_SHARED OFF CACHE BOOL "" FORCE)
set(ZLIB_BUILD_STATIC ON CACHE BOOL "" FORCE)
set(ZLIB_INSTALL OFF CACHE BOOL "" FORCE)

# Upstream's cache flags do not track the source revision. Regenerate the
# templates so an existing build cannot retain the previous zconf.h ABI.
unset(ZLIB_CONF_WRITTEN CACHE)
unset(ZLIB_DEF_WRITTEN CACHE)
unset(ZLIB_MAP_WRITTEN CACHE)
