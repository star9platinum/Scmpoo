cmake_minimum_required(VERSION 3.16.0)

set(CMAKE_USER_MAKE_RULES_OVERRIDE "${CMAKE_SOURCE_DIR}/cmake/CMakeUserMakeRulesOverride.cmake")

project(Scmpoo VERSION 2.0.0 LANGUAGES C)

if(MSVC)
  set(CMAKE_RC_FLAGS "/c 65001 /l 0x0804")
elseif(MINGW)
  set(CMAKE_RC_FLAGS "--codepage=65001 --language=0804")
endif()

add_subdirectory(Scmpoo)

include(CTest)
if(BUILD_TESTING)
  foreach(test_name animation monitor)
    add_executable(${test_name}_tests tests/${test_name}_tests.c)
    target_link_libraries(${test_name}_tests PRIVATE user32 gdi32 shell32 winmm)
    if(MSVC)
      target_compile_options(${test_name}_tests PRIVATE /utf-8)
    endif()
    add_test(NAME ${test_name} COMMAND ${test_name}_tests)
  endforeach()
endif()
