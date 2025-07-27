#ifndef LEVEL_GEN_H
#define LEVEL_GEN_H

#ifdef LEVEL_GEN_EXPORT
  #define LEVEL_GEN_API __declspec(dllexport)
#else
  #define LEVEL_GEN_API  _declspec(dllimport)
#endif

// Defines used by CppSharp when creating bindings
#ifndef CS_IGNORE
    #define CS_IGNORE
#endif

#ifndef CS_VALUE_TYPE
    #define CS_VALUE_TYPE
#endif

#ifndef CS_FLAGS
    #define CS_FLAGS
#endif

#include <memory>
#include <string>
#include <vector>

enum class CS_FLAGS SquareType {
    Unknown = 1 << 0,
    Space = 1 << 1,
    Hull = 1 << 2,
    Ship = 1 << 3,
    Corridor = 1 << 4,
    Room = 1 << 5,
};

struct LEVEL_GEN_API CS_VALUE_TYPE Room {
    const unsigned x;
    const unsigned y;
    const unsigned w;
    const unsigned h;

    const bool is_corridor;
};

struct LEVEL_GEN_API CS_VALUE_TYPE Adjacency {
    const Room first;
    const Room second;
};

struct LEVEL_GEN_API CS_VALUE_TYPE MapSquare {
    const unsigned x;
    const unsigned y;

    const SquareType type;
};

class LEVEL_GEN_API Level {
public:
    virtual ~Level();
    CS_IGNORE Level(Level&& other) noexcept;
    CS_IGNORE Level& operator=(Level&& other) noexcept;
    CS_IGNORE Level(const Level& other) = delete;
    CS_IGNORE Level& operator=(const Level& other) = delete;

    int get_cost();

    const MapSquare* next_square();
    const Room* next_room();
    const Adjacency* next_adjacency();

private:
    CS_IGNORE class LevelImpl;  // Internal implementation class
    CS_IGNORE std::unique_ptr<LevelImpl> impl;

    explicit CS_IGNORE Level(std::unique_ptr<LevelImpl> impl);
    friend class LevelGenerator;
};

class LEVEL_GEN_API LevelGenerator {
public:
    LevelGenerator();
    virtual ~LevelGenerator();
    CS_IGNORE LevelGenerator(LevelGenerator&& other) noexcept;
    CS_IGNORE LevelGenerator& operator=(LevelGenerator&& other) noexcept;
    CS_IGNORE LevelGenerator(const LevelGenerator& other) = delete;
    CS_IGNORE LevelGenerator& operator=(const LevelGenerator& other) = delete;

    LevelGenerator& set_width(unsigned new_width);
    LevelGenerator& set_height(unsigned new_height);
    LevelGenerator& set_min_rooms(unsigned new_min_rooms);
    LevelGenerator& set_max_rooms(unsigned new_max_rooms);
    LevelGenerator& set_seed(unsigned new_seed);

    std::string solve();

    Level best_level() const;

private:
    CS_IGNORE class LevelGenImpl;  // Internal implementation class
    CS_IGNORE std::unique_ptr<LevelGenImpl> impl;
};

#endif // LEVEL_GEN_H
