#include "level_gen.h"
#include "clingo.hh"

#include <memory>
#include <unordered_map>
#include <sstream>

namespace {
    inline std::vector<unsigned> unsigned_args(const Clingo::Symbol& sym)
    {
        std::vector<unsigned> ret;
        if (sym.type() == Clingo::SymbolType::Function) {
            for (const auto& arg : sym.arguments()) {
                if (arg.type() != Clingo::SymbolType::Number) {
                    return {};
                }
                ret.push_back(static_cast<unsigned>(arg.number()));
            }
        }
        return ret;
    }

    inline auto find_room(const std::vector<Room>& rooms, Clingo::Symbol room_sym) {
        auto args = unsigned_args(room_sym);
        return std::find(rooms.cbegin(), rooms.cend(), Room{args[0], args[1], args[2], args[3], args[3] == 1U});
    }
} // unnamed namespace

bool operator==(const Room& first, const Room& second) {
    return first.x == second.x
        && first.y == second.y
        && first.w == second.w
        && first.h == second.h
        && first.is_corridor == second.is_corridor;
}


class Level::LevelImpl {
    public:
        LevelImpl(int64_t cost, const std::vector<uint64_t>& data) : cost(static_cast<int>(cost))
        {
            std::vector<MapSquare> square_vec;
            std::vector<Room> room_vec;
            std::vector<Adjacency> adjacency_vec;

            // Map symbols to simple data structures to return from the API
            for (const auto& sym_val : data) {
                const Clingo::Symbol sym{sym_val};
                if (sym.type() != Clingo::SymbolType::Function || sym.arguments().size() < 2) {
                    continue;
                }

                if (sym.match("room", 4)) {
                    const auto args = unsigned_args(sym);
                    const auto x = args[0];
                    const auto y = args[1];
                    const auto width = args[2];
                    const auto height = args[3];
                    room_vec.push_back({x, y, width, height, height == 1U});
                    continue;
                }

                SquareType type;
                if (sym.match("in_space", 2)) {
                    type = SquareType::Space;
                } else if (sym.match("hull", 2)) {
                    type = SquareType::Hull;
                } else if (sym.match("ship", 2)) {
                    type = SquareType::Ship;
                } else if (sym.match("corridor", 2)) {
                    type = SquareType::Corridor;
                } else if (sym.match("room_square", 6)) {
                    type = SquareType::Room;
                } else {
                    continue;  // Not a map square symbol
                }

                const auto args = unsigned_args(sym);
                square_vec.push_back({args[0], args[1], type});
            }

            // Third pass to get adjacencies pointing to already-created rooms
            for (const auto& sym_val : data) {
                const Clingo::Symbol sym{sym_val};
                const auto args = sym.arguments();
                if (sym.match("adjacent", 2)) {
                    const auto first = args[0];
                    const auto second = args[1];
                    if(!(first.match("room", 4) && second.match("room", 4))) {
                        continue;
                    }
                    const auto first_it = find_room(room_vec, first);
                    const auto second_it = find_room(room_vec, second);
                    if (first_it == room_vec.cend() || second_it == room_vec.cend()) {
                        continue;
                    }

                    adjacency_vec.push_back({&(*first_it), &(*second_it)});
                }
            }

            map_squares_iter = LevelPartIter<MapSquare>{square_vec};
            rooms_iter = LevelPartIter<Room>{room_vec};
            adjacencies_iter = LevelPartIter<Adjacency>{adjacency_vec};
        }

    private:
        LevelPartIter<MapSquare> map_squares_iter;
        LevelPartIter<Room> rooms_iter;
        LevelPartIter<Adjacency> adjacencies_iter;

        LevelPartIter<MapSquare> map_squares() {
            auto iter = map_squares_iter;
            iter.reset();
            return iter;
        }

        LevelPartIter<Room>* rooms() {
            rooms_iter.reset();
            return &rooms_iter;
        }

        LevelPartIter<Adjacency>& adjacencies() {
            adjacencies_iter.reset();
            return adjacencies_iter;
        }

        size_t num_map_squares() const
        {
            return map_squares_iter.count();
        }

        size_t num_rooms() const
        {
            return rooms_iter.count();
        }

        size_t num_adjacencies() const
        {
            return adjacencies_iter.count();
        }

        int cost;

        friend class Level;
};




LevelPartIter<MapSquare> Level::map_squares() const
{
    return impl->map_squares();
}

LevelPartIter<Room>* Level::rooms() const
{
    return impl->rooms();
}

LevelPartIter<Adjacency>& Level::adjacencies() const
{
    return impl->adjacencies();
}

size_t Level::num_map_squares() const
{
    return impl->num_map_squares();
}

size_t Level::num_rooms() const
{
    return impl->num_rooms();
}

size_t Level::num_adjacencies() const
{
    return impl->num_adjacencies();
}

int Level::get_cost() const
{
    return impl->cost;
}

Level::Level(int64_t cost, const std::vector<uint64_t>& data) : impl(std::make_unique<Level::LevelImpl>(cost, data)) {}

Level::Level(Level&& other) = default;

Level::~Level() = default;
