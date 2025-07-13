cmake_minimum_required(VERSION 3.28)
project(LevelGenerator VERSION 0.0.2)

include(GNUInstallDirs)

include(CTest)

add_subdirectory(level-gen-cpp)
add_subdirectory(level-gen-csharp)
