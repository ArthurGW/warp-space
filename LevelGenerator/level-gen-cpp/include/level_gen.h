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

#ifndef CS_FLAGS
#define CS_FLAGS
#endif

#include <memory>
#include <string>
#include <utility>
#include <vector>

/// Types of map squares
/// These numbers are in precedence order, i.e. where a position has more than one type, the higher-numbered type takes
/// priority
enum class CS_FLAGS SquareType : uint8_t
{
    Unknown = 1 << 0,
    Space = 1 << 1,
    Ship = 1 << 2,
    Hull = 1 << 3,
    Room = 1 << 4,
    Corridor = 1 << 5,
    AlienBreach = 1 << 6,
};

/// Types of rooms
enum class CS_FLAGS RoomType : uint8_t
{
    Unknown = 1 << 0,
    Corridor = 1 << 1,
    AlienBreach = 1 << 2,
    Room = 1 << 3,
};

struct LEVEL_GEN_API Room {
        unsigned x;
        unsigned y;
        unsigned w;
        unsigned h;

        RoomType type;
        size_t room_id = 0;

        friend bool operator==(const Room& first, const Room& second);
};

struct LEVEL_GEN_API Adjacency {
        size_t first_id;
        size_t second_id;
};

struct LEVEL_GEN_API MapSquare {
        unsigned x;
        unsigned y;

        SquareType type;
};

/// Template for creating a C#-style IEnumerator over a type of level part
template<class T>
struct LevelPartIter
{
        CS_IGNORE typedef typename std::vector<T> PartVec;

        T current() const
        {
            return (*parts)[static_cast<size_t>(pos)];
        }

        bool move_next()
        {
            if (pos < std::numeric_limits<intmax_t>::max())
            {
                pos += 1;
                return pos < count();
            }
            return false;
        };

        void reset()
        {
            pos = -1;
        }

        size_t count() const
        {
            return parts->size();
        }

        explicit CS_IGNORE LevelPartIter(PartVec* parts) : parts(parts) {}
        CS_IGNORE LevelPartIter(LevelPartIter&& other) noexcept = default;
        CS_IGNORE LevelPartIter& operator=(LevelPartIter&& other) = default;
        CS_IGNORE LevelPartIter(const LevelPartIter& other) = default;
        CS_IGNORE LevelPartIter& operator=(const LevelPartIter& other) = default;

    private:
        CS_IGNORE intmax_t pos = -1;
        CS_IGNORE PartVec* parts;
};

// Explicit instantiations for export in the API
template struct LEVEL_GEN_API
LevelPartIter<MapSquare>;

template struct LEVEL_GEN_API
LevelPartIter<Room>;

template struct LEVEL_GEN_API
LevelPartIter<Adjacency>;

class LEVEL_GEN_API Level {
    public:
        // Note - to avoid exposing clingo in the header here, we use a vector of clingo's numeric symbol representation,
        // rather than a more specific type

        CS_IGNORE Level(unsigned width, unsigned height, int64_t cost, const std::vector<uint64_t>& data);
        CS_IGNORE Level(Level && other) noexcept;
        CS_IGNORE Level& operator=(Level && other) = delete;
        CS_IGNORE Level(const Level& other) = delete;
        CS_IGNORE Level& operator=(const Level& other) = delete;

        virtual ~Level();

        int get_cost() const;
        unsigned get_width() const;
        unsigned get_height() const;

        LevelPartIter<MapSquare> map_squares() const;

        LevelPartIter<Room> rooms() const;

        LevelPartIter<Adjacency> adjacencies() const;

        size_t get_num_map_squares() const;

        size_t get_num_corridors() const;

        size_t get_num_breaches() const;

        size_t get_num_rooms() const;

        size_t get_num_adjacencies() const;

    private:
        CS_IGNORE class LevelImpl;  // Internal implementation class
        CS_IGNORE std::unique_ptr<LevelImpl> impl;
};

class LEVEL_GEN_API

LevelGenerator {
    public:

        LevelGenerator(
               unsigned max_num_levels,
               unsigned width,
               unsigned height,
               unsigned min_rooms,
               unsigned max_rooms,
               unsigned num_breaches,
               size_t seed = 0,  // Indicates "unset"
               bool load_prog_from_file = false,  // Load ASP program from file at runtime, for easier iteration during dev
               unsigned num_threads = 1
        );

        virtual ~LevelGenerator();

        CS_IGNORE LevelGenerator(LevelGenerator&& other) noexcept;
        CS_IGNORE LevelGenerator& operator=(LevelGenerator && other) noexcept;
        CS_IGNORE LevelGenerator(const LevelGenerator& other) = delete;
        CS_IGNORE LevelGenerator& operator=(const LevelGenerator& other) = delete;

        const char* solve();

        const char* solve_safe();

        /// Get a pointer to the best level - note this is only valid for the lifetime of the generator
        Level* best_level();

        size_t get_num_levels() const;

    private:
        CS_IGNORE class LevelGenImpl;  // Internal implementation class
        CS_IGNORE std::unique_ptr<LevelGenImpl> impl;
};

#endif // LEVEL_GEN_H
