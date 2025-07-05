enable_language(CXX)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# ===================================================
# Set up clingo build
# ===================================================
set(CLINGO_SOURCE_DIR "../../external/clingo")
cmake_path(ABSOLUTE_PATH CLINGO_SOURCE_DIR NORMALIZE)

include(CheckCXXSourceCompiles)
list(APPEND CMAKE_MODULE_PATH "${CLINGO_SOURCE_DIR}/cmake")
include(clingo_functions)

set(CLINGO_BUILD_TESTS OFF)
set(CLINGO_BUILD_EXAMPLES OFF)
set(CLINGO_BUILD_APPS OFF)
set(CLINGO_BUILD_STATIC ON)
set(CLINGO_BUILD_SHARED OFF)
set(CLINGO_INSTALL_LIB OFF)
set(CLINGO_MAP_TYPE "hopscotch")

find_package(BISON "2.5")
find_package(RE2C "0.101")

add_subdirectory("${CLINGO_SOURCE_DIR}/third_party" "./clingo/third_party")
set(CLASP_BUILD_APP OFF)
set(CLASP_USE_LOCAL_LIB_POTASSCO ON)
add_subdirectory("${CLINGO_SOURCE_DIR}/clasp" "./clingo/clasp")

add_subdirectory("${CLINGO_SOURCE_DIR}/libreify" "./clingo/libreify")
add_subdirectory("${CLINGO_SOURCE_DIR}/libgringo" "./clingo/libgringo")
add_subdirectory("${CLINGO_SOURCE_DIR}/libclingo" "./clingo/libclingo")

# ===================================================
# Build the bindings C++ library
# ===================================================
add_library(bindings-cpp SHARED bindings.cpp)
add_dependencies(bindings-cpp libclingo)
target_include_directories(bindings-cpp PUBLIC include)
target_link_libraries(bindings-cpp PRIVATE libclingo)
target_compile_definitions(bindings-cpp PRIVATE BINDINGS_EXPORT)
