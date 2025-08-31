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

    inline size_t find_room(const std::vector<Room>& rooms, Clingo::Symbol room_sym)
    {
        if (!room_sym.match("room", 4))
        {
            return 0;  // 0 means invalid
        }

        const auto args = unsigned_args(room_sym);
        const auto room = std::find(
            rooms.cbegin(),
            rooms.cend(),
            Room{args[0], args[1], args[2], args[3], args[3] == 1U ? RoomType::Corridor : RoomType::Room}
        );

        if (room == rooms.cend())
        {
            return 0;
        }
        return room->room_id;
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

#ifdef TEST_BUILD
    struct Connection
    {
        size_t first_id;
        size_t second_id;
    };
#endif
} // unnamed namespace

bool operator==(const Room& first, const Room& second)
{
    return first.x == second.x
           && first.y == second.y
           && first.w == second.w
           && first.h == second.h
           && first.type == second.type
           // ID may not be set, so allow one or both to be unset (zero), but if both set then compare
           && (first.room_id == 0 || second.room_id == 0 || first.room_id == second.room_id);
}

bool operator==(const Adjacency& first, const Adjacency& second)
{
    return first.first_id == second.first_id
           && first.second_id == second.second_id
           && first.is_portal == second.is_portal;
}

class Level::LevelImpl
{
    public:
        LevelImpl(unsigned width, unsigned height, int64_t cost, const std::vector<uint64_t>& data) 
        : cost(static_cast<int>(cost)), width(width), height(height), corridors(0), breaches(0), portals(0)
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

                    // Create rooms, with IDs starting from 1
                    room_vec.push_back({x, y, rw, rh, is_corridor ? RoomType::Corridor : RoomType::Room, room_vec.size() + 1});
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
                else if (sym.match("breach_square", 2))
                {
                    type = SquareType::AlienBreach;
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
                if (!pair.second && (uint8_t)pair.first->second < (uint8_t)type)
                {
                    square_lookup[key] = type;
                }
            }

            // Second pass to get adjacencies, breaches, and start/finish points, referring to already-created rooms
            for (const auto& sym_val : data)
            {
                const Clingo::Symbol sym{sym_val};
                const auto args = sym.arguments();
                if (sym.match("adjacent", 3))
                {
                    const auto first = find_room(room_vec, args[0]);
                    const auto second = find_room(room_vec, args[1]);
                    if (first == 0 || second == 0)
                    {
                        continue;
                    }

                    const auto is_portal = args[2].number() == 1;
                    if (is_portal) ++portals;

                    adjacency_vec.push_back({first, second, is_portal});
                }
#ifdef TEST_BUILD
                else if (sym.match("connected", 2))
                {
                    const auto first = find_room(room_vec, args[0]);
                    const auto second = find_room(room_vec, args[1]);
                    if (first == 0 || second == 0)
                    {
                        continue;
                    }

                    connection_vec.push_back({first, second});
                }
#endif
                else if (sym.match("start_room", 1))
                {
                    start_room_id = find_room(room_vec, args[0]);
                }
                else if (sym.match("finish_room", 1))
                {
                    finish_room_id = find_room(room_vec, args[0]);
                }
                else if (sym.match("alien_breach", 5))
                {
                    // Convert the breach to a room with a special room type, connected to the breached room bidirectionally
                    const auto x = static_cast<unsigned>(args[0].number());
                    const auto y = static_cast<unsigned>(args[1].number());
                    const auto w = static_cast<unsigned>(args[2].number());
                    const auto h = static_cast<unsigned>(args[3].number());
                    const auto breached_room_sym = args[4];
                    if (!(breached_room_sym.match("room", 4)))
                    {
                        continue;
                    }
                    const auto breached_room = find_room(room_vec, breached_room_sym);
                    if (breached_room == 0)
                    {
                        continue;
                    }

                    room_vec.push_back({x, y, w, h, RoomType::AlienBreach, room_vec.size() + 1});
                    adjacency_vec.push_back({breached_room, room_vec.back().room_id, false});
                    adjacency_vec.push_back({room_vec.back().room_id, breached_room, false});
                    ++breaches;
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

        size_t num_breaches() const
        {
            return breaches;
        }

        size_t start_room() const
        {
            return start_room_id;
        }

        size_t finish_room() const
        {
            return finish_room_id;
        }

        size_t num_rooms() const
        {
            return room_vec.size();
        }

        size_t num_adjacencies() const
        {
            return adjacency_vec.size();
        }

        size_t num_portals() const
        {
            return portals;
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
#ifdef TEST_BUILD
        std::vector<Connection> connection_vec;
#endif
        size_t corridors;
        size_t breaches;
        size_t portals;
        size_t start_room_id;
        size_t finish_room_id;
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

size_t Level::get_num_breaches() const
{
    return impl->num_breaches();
}

size_t Level::get_start_room() const
{
    return impl->start_room();
}

size_t Level::get_finish_room() const
{
    return impl->finish_room();
}


size_t Level::get_num_rooms() const
{
    return impl->num_rooms();
}

size_t Level::get_num_adjacencies() const
{
    return impl->num_adjacencies();
}

size_t Level::get_num_portals() const
{
    return impl->num_portals();
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
