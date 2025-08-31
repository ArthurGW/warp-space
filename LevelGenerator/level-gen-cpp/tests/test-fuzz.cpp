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

SCENARIO("other levels can be generated", "[levelgen][solve][fuzz]")
{
    GIVEN("A level generator with various params")
    {
        // A fuzz-like test to try some other params and make sanity checks
        WHEN("solve() is called")
        {
            auto width = GENERATE(range(10U, 16U, 2U));
            auto height = GENERATE(range(10U, 14U, 2U));
            auto min_rooms = GENERATE(range(3U, 5U));
            auto max_rooms = GENERATE(range(8U, 16U, 4U));
            auto num_breaches = GENERATE(range(1U, 2U));
            auto num_portals = GENERATE(range(1U, 2U));
            auto seed = GENERATE(take(2, random(1U, 1000U)));

            THEN("the best level has appropriate numbers of symbols")
            {
                LevelGenerator gen{
                        1, width, height, min_rooms, max_rooms, num_breaches, num_portals, seed, false
                };
                REQUIRE_NOTHROW(gen.solve());

                const auto* level = gen.best_level();
                REQUIRE_FALSE(level == nullptr);

                // At least one square type per grid square plus some duplicates
                REQUIRE(level->get_num_map_squares() == width * height);

                // Num rooms within specified limits
                const auto num_rooms = level->get_num_rooms();
                const auto num_restricted_rooms = num_rooms - level->get_num_corridors() - level->get_num_breaches();
                REQUIRE(num_restricted_rooms >= min_rooms);
                REQUIRE(num_restricted_rooms <= max_rooms);

                // Every room must have at least one adjacency, theoretical limit is every room adjacent to every other
                REQUIRE(level->get_num_adjacencies() >= num_rooms);
                REQUIRE(level->get_num_adjacencies() <= num_rooms * num_rooms);

                // Correct numbers of breaches and portals
                REQUIRE(level->get_num_breaches() == num_breaches);
                REQUIRE(level->get_num_portals() == num_portals * 2);  // One entry each way
            }
        }
    }
}
