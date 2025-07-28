cmake_minimum_required(VERSION 3.28)
project(LevelGenerator VERSION 0.0.2)

cmake_policy(SET CMP0077 NEW)  # For this project
set(CMAKE_POLICY_DEFAULT_CMP0077 NEW)  # For subprojects

include(GNUInstallDirs)

include(CTest)

add_subdirectory(level-gen-cpp)
add_subdirectory(level-gen-csharp)
