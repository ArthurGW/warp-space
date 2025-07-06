# ===================================================
# Set up CppSharp build
# ===================================================
set(CPPSHARP_SOURCE_DIR "../../external/CppSharp")
cmake_path(ABSOLUTE_PATH CPPSHARP_SOURCE_DIR NORMALIZE)
cmake_path(APPEND CPPSHARP_SOURCE_DIR "bin" "Release" "CppSharp.CLI.exe" OUTPUT_VARIABLE CPPSHARP_EXE)

add_custom_command(
    OUTPUT "${CPPSHARP_EXE}"
    COMMAND build.sh ARGS generate -configuration Release -platform x64 -target-framework .netstandard2.1
    COMMAND build.sh ARGS -configuration Release -platform x64
    VERBATIM
    DEPENDS "${CPPSHARP_SOURCE_DIR}/version.json"
    WORKING_DIRECTORY "${CPPSHARP_SOURCE_DIR}/build")

add_custom_target(CppSharp
    COMMAND echo CppSharp target finished
    VERBATIM
    DEPENDS "${CPPSHARP_EXE}")

# ===================================================
# Build the bindings C# library using CppSharp
# ===================================================
set(OUTPUT_DIR "../../Assets/Bindings")
cmake_path(ABSOLUTE_PATH OUTPUT_DIR NORMALIZE)

cmake_path(APPEND CMAKE_CURRENT_BINARY_DIR "Bindings" "Bindings.dll" OUTPUT_VARIABLE TARGET_PATH)
cmake_path(APPEND CMAKE_CURRENT_BINARY_DIR "Bindings" OUTPUT_VARIABLE TARGET_DIR)

# https://cmake.org/cmake/help/v3.31/manual/cmake-generator-expressions.7.html#whitespace-and-quoting
add_custom_command(
    OUTPUT "${TARGET_PATH}"
        "${OUTPUT_DIR}/Bindings.dll"
    COMMAND "${CPPSHARP_EXE}"
        ARGS
            --iln=$<TARGET_FILE_BASE_NAME:bindings-cpp>
            "$<LIST:TRANSFORM,$<SHELL_PATH:$<TARGET_PROPERTY:bindings-cpp,INCLUDE_DIRECTORIES>>,PREPEND,-I=>"
            -L=$<SHELL_PATH:$<TARGET_FILE_DIR:bindings-cpp>>
            --output=$<SHELL_PATH:${TARGET_DIR}>
            $<$<CONFIG:Debug,RelWithDebInfo>:--debug>
            --compile
            --module=Bindings
            --outputnamespace=Bindings
            --arch=x64
            --platform=win
            --generator=csharp
            --checksymbols
            --unity
            -v
            $<SHELL_PATH:$<TARGET_PROPERTY:bindings-cpp,SOURCE_DIR>/include/bindings.h>
    COMMAND
        ${CMAKE_COMMAND} -E copy ARGS "${TARGET_PATH}" "${OUTPUT_DIR}"
    DEPENDS
        $<TARGET_FILE:bindings-cpp>
    COMMAND_EXPAND_LISTS
    VERBATIM
)

add_custom_target(bindings-cs
    COMMAND echo Bindings target finished
    VERBATIM
    DEPENDS "${TARGET_PATH}")

add_dependencies(bindings-cs bindings-cpp CppSharp)
