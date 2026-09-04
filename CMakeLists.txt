cmake_minimum_required(VERSION 3.16.0)

set(CMAKE_USER_MAKE_RULES_OVERRIDE "${CMAKE_SOURCE_DIR}/cmake/CMakeUserMakeRulesOverride.cmake")

project(Scmpoo C)

if(MSVC)
  set(CMAKE_RC_FLAGS "/c 65001 /l 0x0804")
elseif(MINGW)
  set(CMAKE_RC_FLAGS "--codepage=65001 --language=0804")
endif()

add_subdirectory(Scmpoo)
