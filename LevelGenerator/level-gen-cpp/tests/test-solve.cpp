#include <catch2/catch_test_macros.hpp>
#include <catch2/generators/catch_generators_all.hpp>
#include "level_gen.h"

namespace
{
    template<class T>
    size_t count_parts(LevelPartIter<T> iter)
    {
        iter.reset();
        auto sum = 0UL;
        while (iter.move_next())
        {
            // Check retrieval
            auto current = iter.current();
            sum += 1UL;
        }
        return sum;
    }

    template<class T>
    size_t count_parts(LevelPartIter<T> iter, std::function<bool(const T&)> filter)
    {
        iter.reset();
        auto sum = 0UL;
        while (iter.move_next())
        {
            // Check retrieval
            auto current = iter.current();
            if (filter(current))
            {
                sum += 1UL;
            }
        }
        return sum;
    }

    template<class T>
    std::vector<T> accumulate_parts(LevelPartIter<T> iter, std::function<bool(const T&)> filter)
    {
        iter.reset();
        std::vector<T> ret;
        while (iter.move_next())
        {
            // Check retrieval
            auto current = iter.current();
            if (filter(current))
            {
                ret.push_back(current);
            }
        }
        return ret;
    }
}

SCENARIO("level generators can be solved", "[levelgen][solve]")
{

    GIVEN("A level generator with valid params")
    {
        WHEN("solve() is called")
        {
            THEN("a solution is returned")
            {
                LevelGenerator gen{
                        1, 9, 10, 1, 6, 1, 0, 1234
                };
                const char* res;
                REQUIRE_NOTHROW(res = gen.solve());
                REQUIRE_FALSE(res == nullptr);
                REQUIRE_FALSE(std::string(res).empty());
                REQUIRE(gen.get_num_levels() == 1);
            }

            THEN("a best level exists")
            {
                LevelGenerator gen{
                        1, 9,  10, 1, 6, 1, 0, 1234
                };
                REQUIRE_NOTHROW(gen.solve());
                const auto* level = gen.best_level();
                REQUIRE_FALSE(level == nullptr);
                REQUIRE_FALSE(level->get_cost() == std::numeric_limits<int>::max());
            }

            THEN("the best level has the correct count of symbols and and cost")
            {
                LevelGenerator gen{
                        1, 9, 10, 1, 6, 1, 0, 1234
                };
                REQUIRE_NOTHROW(gen.solve());

                const auto level = gen.best_level();
                REQUIRE_FALSE(level == nullptr);

                // These values have been determined empirically
                REQUIRE(level->get_cost() == 2);
                REQUIRE(level->get_num_map_squares() == 90UL);
                REQUIRE(level->get_num_rooms() == 10UL);
                REQUIRE(level->get_num_adjacencies() == 18UL);
            }

            THEN("the best level can iterate over symbols")
            {
                LevelGenerator gen{
                        1, 9, 10, 1, 6, 1, 0, 1234, true
                };
                REQUIRE_NOTHROW(gen.solve());

                const auto* level = gen.best_level();
                REQUIRE_FALSE(level == nullptr);

                // These values match the test above
                REQUIRE(count_parts(level->map_squares()) == 90UL);
                REQUIRE(count_parts(level->rooms()) == 10UL);
                REQUIRE(count_parts(level->adjacencies()) == 18UL);
            }

            THEN("the best level has the right number and location of breaches")
            {
                LevelGenerator gen{
                        1, 12, 10, 1, 6, 2, 0, 1234, true
                };
                REQUIRE_NOTHROW(gen.solve());

                const auto* level = gen.best_level();
                REQUIRE_FALSE(level == nullptr);

                auto room_iter = level->rooms();
                REQUIRE(count_parts<Room>(
                        room_iter,
                    [](const auto& rm) { return rm.type == RoomType::AlienBreach; }
                ) == 2UL);

                // Breaches are always added to the end of the room list, so get them from there to compare
                room_iter.reset();
                for (auto i = 0; i < room_iter.count() - 1; ++i) room_iter.move_next();
                const auto first_breach = room_iter.current();
                room_iter.move_next();
                const auto second_breach = room_iter.current();

                REQUIRE(first_breach == Room{7, 9, 1, 2, RoomType::AlienBreach});
                REQUIRE(second_breach == Room{11, 6, 2, 1, RoomType::AlienBreach});
            }

            THEN("the best level has the right number and location of portals")
            {
                LevelGenerator gen{
                        1, 9, 10, 1, 6, 1, 2, 1234, true
                };
                REQUIRE_NOTHROW(gen.solve());

                const auto* level = gen.best_level();
                REQUIRE_FALSE(level == nullptr);

                auto adj_iter = level->adjacencies();
                auto portals = accumulate_parts<Adjacency>(
                    adj_iter,
                    [](const auto& adj) { return adj.is_portal; }
                );

                REQUIRE(level->get_num_portals() == 4UL);   // num_portals * 2 since they are bidirectional
                REQUIRE(portals.size() == 4UL);
                REQUIRE(portals[0] == Adjacency{3, 1, true});
                REQUIRE(portals[1] == Adjacency{3, 2, true});
                REQUIRE(portals[2] == Adjacency{1, 3, true});
                REQUIRE(portals[3] == Adjacency{2, 3, true});
            }

            THEN("the best level has a start room and a finish room")
            {
                LevelGenerator gen{
                        1, 12, 10, 1, 6, 2, 1, 1234, true
                };
                REQUIRE_NOTHROW(gen.solve());

                const auto* level = gen.best_level();
                REQUIRE_FALSE(level == nullptr);
                REQUIRE(level->get_num_rooms() == 9UL);
                REQUIRE(level->get_start_room() == 4UL);
                REQUIRE(level->get_finish_room() == 2UL);
            }
        }
    }
}
