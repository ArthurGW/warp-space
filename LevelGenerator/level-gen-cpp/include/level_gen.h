#ifndef LEVEL_GEN_H
#define LEVEL_GEN_H

#ifdef LEVEL_GEN_EXPORT
  #define LEVEL_GEN_API __declspec(dllexport)
#else
  #define LEVEL_GEN_API  _declspec(dllimport)
#endif

#define CS_IGNORE

#include <cstdint>
#include <memory>
#include <string>

class LEVEL_GEN_API LevelGenerator {
    public:
        LevelGenerator(uint8_t width, uint8_t height);
        ~LevelGenerator();
        CS_IGNORE LevelGenerator(LevelGenerator&& other) noexcept;
        CS_IGNORE LevelGenerator& operator=(LevelGenerator&& other) noexcept;
        CS_IGNORE LevelGenerator(const LevelGenerator& other) = delete;
        CS_IGNORE LevelGenerator& operator=(const LevelGenerator& other) = delete;

        std::string solve();

    private:
        CS_IGNORE class LevelGenImpl;  // Internal implementation class
        CS_IGNORE std::unique_ptr<LevelGenImpl> impl;
};

#endif // LEVEL_GEN_H
