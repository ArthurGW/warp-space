@call "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat"

%CMAKE_EXE% -DCMAKE_MAKE_PROGRAM=%NINJA_EXE% -G "Ninja Multi-Config" -S . -B ninja_build

%CMAKE_EXE% --build ninja_build --config Release

%CMAKE_EXE% --install ninja_build --config Release --prefix ../Assets --component bindings
