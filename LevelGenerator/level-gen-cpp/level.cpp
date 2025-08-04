#include "level_gen.h"
#include "clingo.hh"

#include <memory>
#include <unordered_map>
#include <sstream>

namespace
{
    inline std::vector<unsigned> unsigned_args(const Clingo::Symbol& sym)
    {
        std::vector<unsigned> ret;
        if (sym.type() == Clingo::SymbolType::Function)
        {
            for (const auto& arg : sym.arguments())
            {
                if (arg.type() != Clingo::SymbolType::Number)
                {
                    return {};
                }
                ret.push_back(static_cast<unsigned>(arg.number()));
            }
        }
        return ret;
    }

    inline auto find_room(const std::vector<Room>& rooms, Clingo::Symbol room_sym)
    {
        auto args = unsigned_args(room_sym);
        return std::find(rooms.cbegin(), rooms.cend(), Room{args[0], args[1], args[2], args[3], args[3] == 1U});
    }

    /// Converts a one-indexed (x, y) coordinate to a zero-indexed serial grid index, in row-major style
    inline uint64_t square_pos_to_serial_index(unsigned x, unsigned y, unsigned width)
    {
        return (y - 1) * (uint64_t)width + (x - 1);
    }

    /// Converts zero-indexed serial grid index, in row-major style, to a one-indexed (x, y) coordinate
    inline std::tuple<unsigned, unsigned> serial_index_to_square_pos(uint64_t index, unsigned width)
    {
        return {static_cast<unsigned>(index % width) + 1, static_cast<unsigned>(index / width) + 1};
    }
} // unnamed namespace

bool operator==(const Room& first, const Room& second)
{
    return first.x == second.x
           && first.y == second.y
           && first.w == second.w
           && first.h == second.h
           && first.is_corridor == second.is_corridor
           // ID may not be set, so allow one or both to be unset (zero), but if both set then compare
           && (first.room_id == 0 || second.room_id == 0 || first.room_id == second.room_id);
}


class Level::LevelImpl
{
    public:
        LevelImpl(unsigned width, unsigned height, int64_t cost, const std::vector<uint64_t>& data) : cost(static_cast<int>(cost)), width(width), height(height), corridors(0)
        {
            std::unordered_map<uint64_t, SquareType> square_lookup;

            // Map symbols to simple data structures to return from the API
            for (const auto& sym_val : data)
            {
                const Clingo::Symbol sym{sym_val};
                if (sym.type() != Clingo::SymbolType::Function || sym.arguments().size() < 2)
                {
                    continue;
                }

                if (sym.match("room", 4))
                {
                    const auto args = unsigned_args(sym);
                    const auto x = args[0];
                    const auto y = args[1];
                    const auto rw = args[2];
                    const auto rh = args[3];
                    const auto is_corridor = rh == 1U;
                    if (is_corridor)
                    {
                        ++corridors;
                    }

                    room_vec.push_back({x, y, rw, rh, is_corridor, room_vec.size() + 1});
                    continue;
                }

                SquareType type;
                if (sym.match("in_space", 2))
                {
                    type = SquareType::Space;
                }
                else if (sym.match("hull", 2))
                {
                    type = SquareType::Hull;
                }
                else if (sym.match("ship", 2))
                {
                    type = SquareType::Ship;
                }
                else if (sym.match("corridor", 2))
                {
                    type = SquareType::Corridor;
                }
                else if (sym.match("room_square", 6))
                {
                    type = SquareType::Room;
                }
                else
                {
                    continue;  // Not a map square symbol
                }

                const auto args = unsigned_args(sym);
                const auto sqx = args[0];
                const auto sqy = args[1];
                const auto key = square_pos_to_serial_index(sqx, sqy, width);
                const auto pair = square_lookup.emplace(key, type);
                // If key already existed, insert if new type has higher precedence
                if (!pair.second && (uint8_t)pair.first->first < (uint8_t)type)
                {
                    square_lookup[key] = type;
                }
            }

            // Second pass to get adjacencies referring to already-created rooms
            for (const auto& sym_val : data)
            {
                const Clingo::Symbol sym{sym_val};
                const auto args = sym.arguments();
                if (sym.match("adjacent", 2))
                {
                    const auto first = args[0];
                    const auto second = args[1];
                    if (!(first.match("room", 4) && second.match("room", 4)))
                    {
                        continue;
                    }
                    const auto first_it = find_room(room_vec, first);
                    const auto second_it = find_room(room_vec, second);
                    if (first_it == room_vec.cend() || second_it == room_vec.cend())
                    {
                        continue;
                    }

                    adjacency_vec.push_back({first_it->room_id, second_it->room_id});
                }
            }

            // Finally, convert the square lookup to a vector
            square_vec = std::vector<MapSquare>(square_lookup.size());
            std::transform(square_lookup.cbegin(), square_lookup.cend(), square_vec.begin(), [=](const auto& entry) {
                const auto pos = serial_index_to_square_pos(entry.first, width);
                return MapSquare{std::get<0>(pos), std::get<1>(pos), entry.second};
            });
        }

    private:
        LevelPartIter<MapSquare> map_squares()
        {
            return LevelPartIter<MapSquare>{&square_vec};
        }

        LevelPartIter<Room> rooms()
        {
            return LevelPartIter<Room>{&room_vec};
        }

        LevelPartIter<Adjacency> adjacencies()
        {
            return LevelPartIter<Adjacency>{&adjacency_vec};
        }

        size_t num_map_squares() const
        {
            return square_vec.size();
        }

        size_t num_corridors() const
        {
            return corridors;
        }

        size_t num_rooms() const
        {
            return room_vec.size();
        }

        size_t num_adjacencies() const
        {
            return adjacency_vec.size();
        }

        int get_cost() const
        {
            return cost;
        }

        unsigned get_width() const
        {
            return width;
        }

        unsigned get_height() const
        {
            return height;
        }

        const int cost;

        std::vector<MapSquare> square_vec;
        std::vector<Room> room_vec;
        std::vector<Adjacency> adjacency_vec;
        size_t corridors;
        const unsigned width;
        const unsigned height;

        friend class Level;
};


LevelPartIter<MapSquare> Level::map_squares() const
{
    return impl->map_squares();
}

LevelPartIter<Room> Level::rooms() const
{
    return impl->rooms();
}

LevelPartIter<Adjacency> Level::adjacencies() const
{
    return impl->adjacencies();
}

size_t Level::get_num_map_squares() const
{
    return impl->num_map_squares();
}

size_t Level::get_num_corridors() const
{
    return impl->num_corridors();
}


size_t Level::get_num_rooms() const
{
    return impl->num_rooms();
}

size_t Level::get_num_adjacencies() const
{
    return impl->num_adjacencies();
}

int Level::get_cost() const
{
    return impl->get_cost();
}

unsigned Level::get_width() const
{
    return impl->get_width();
}

unsigned Level::get_height() const
{
    return impl->get_height();
}

Level::Level(unsigned width, unsigned height, int64_t cost, const std::vector<uint64_t>& data) : impl(std::make_unique<Level::LevelImpl>(width, height, cost, data))
{}

Level::Level(Level&& other) noexcept = default;

Level::~Level() = default;
