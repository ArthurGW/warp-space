#include "level_gen.h"
#include "clingo.hh"

#include <memory>
#include <unordered_map>
#include <sstream>
#include <tl/optional.hpp>

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

    inline size_t find_room(const std::vector<Room>& rooms, unsigned room_x, unsigned room_y)
    {
        const auto room = std::find_if(
            rooms.cbegin(),
            rooms.cend(),
            [=](const auto& room) { return room.x == room_x && room.y == room_y; }
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

    inline tl::optional<Room> try_get_room(const Clingo::Symbol &sym, size_t next_id)
    {
        if (!sym.match("room", 4))
        {
            return tl::nullopt;
        }

        const auto args = unsigned_args(sym);
        const auto x = args[0];
        const auto y = args[1];
        const auto rw = args[2];
        const auto rh = args[3];
        const auto is_corridor = rh == 1U;

        return tl::make_optional<Room>(x, y, rw, rh, is_corridor ? RoomType::Corridor : RoomType::Room, next_id);
    }

    inline tl::optional<MapSquare> try_get_square(const Clingo::Symbol &sym)
    {
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
        else if (sym.match("room_square", 4))
        {
            type = SquareType::Room;
        }
        else if (sym.match("breach_square", 4))
        {
            type = SquareType::AlienBreach;
        }
        else
        {
            return tl::nullopt;  // Not a map square symbol
        }

        const auto args = unsigned_args(sym);
        const auto sqx = args[0];
        const auto sqy = args[1];

        return tl::make_optional<MapSquare>(sqx, sqy, type);
    }

    inline tl::optional<Adjacency> try_get_adjacency(const Clingo::Symbol &sym, const std::vector<Room>& room_vec)
    {
        bool is_portal = false;

        if (sym.match("portal", 4))
        {
            is_portal = true;
        }
        else if (!sym.match("connected", 4))
        {
            return tl::nullopt;
        }

        const auto args = unsigned_args(sym);

        const auto first = find_room(room_vec, args[0], args[1]);
        const auto second = find_room(room_vec, args[2], args[3]);
        if (first == 0 || second == 0)
        {
            return tl::nullopt;
        }

        return tl::make_optional<Adjacency>(first, second, is_portal);
    }

    inline tl::optional<std::tuple<Room, size_t>> try_get_breach(const Clingo::Symbol &sym, const std::vector<Room>& room_vec, size_t next_id)
    {
        if (!sym.match("alien_breach", 6))
        {
            return tl::nullopt;
        }

        const auto args = unsigned_args(sym);

        // Convert the breach to a room with a special room type, connected to the breached room
        auto breached_room = find_room(room_vec, args[4], args[5]);
        if (breached_room == 0)
        {
            return tl::nullopt;
        }

        return tl::make_optional<std::tuple<Room, size_t>>(
            std::make_tuple<Room, size_t>(
                Room{args[0], args[1], args[2], args[3], RoomType::AlienBreach, next_id},
                std::move(breached_room)
            )
        );
    }
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

                if (auto room = try_get_room(sym, room_vec.size() + 1))
                {
                    if (room->type == RoomType::Corridor)
                    {
                        ++corridors;
                    }

                    room_vec.emplace_back(room.value());
                    continue;
                }

                if (auto sq = try_get_square(sym))
                {
                    const auto key = square_pos_to_serial_index(sq->x, sq->y, width);
                    const auto pair = square_lookup.emplace(key, sq->type);
                    // If key already existed, insert if new type has higher precedence
                    if (!pair.second && (uint8_t) pair.first->second < (uint8_t) sq->type)
                    {
                        square_lookup[key] = sq->type;
                    }
                }
            }

            // Second pass to get connections, breaches, and start/finish points, referring to already-created rooms
            for (const auto& sym_val : data)
            {
                const Clingo::Symbol sym{sym_val};

                if (auto adj = try_get_adjacency(sym, room_vec))
                {
                    if (adj->is_portal) portals += 2;  // Bidirectional, so add 2

                    adjacency_vec.emplace_back(adj.value());
                    adjacency_vec.emplace_back(adj->second_id, adj->first_id, adj->is_portal);
                    continue;
                }

                if (auto breach = try_get_breach(sym, room_vec, room_vec.size() + 1))
                {
                    room_vec.emplace_back(std::get<0>(breach.value()));
                    adjacency_vec.emplace_back(std::get<1>(breach.value()), room_vec.back().room_id, false);
                    adjacency_vec.emplace_back(room_vec.back().room_id, std::get<1>(breach.value()), false);
                    ++breaches;
                    continue;
                }

                const auto args = unsigned_args(sym);

                if (sym.match("start_room", 2))
                {
                    start_room_id = find_room(room_vec, args[0], args[1]);
                    continue;
                }

                if (sym.match("finish_room", 2))
                {
                    finish_room_id = find_room(room_vec, args[0], args[1]);
                }
            }

            // Finally, convert the square lookup to a vector
            square_vec = std::vector<MapSquare>(square_lookup.size(), MapSquare(0, 0, SquareType::Unknown));
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
