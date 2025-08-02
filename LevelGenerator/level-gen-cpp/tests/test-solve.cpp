#include <catch2/catch_test_macros.hpp>
#include <catch2/generators/catch_generators_all.hpp>
#include "level_gen.h"

namespace {
    template <class T>
    size_t count_parts(LevelPartIter<T> iter) {
        iter.reset();
        auto sum = 0UL;
        while(iter.move_next())
        {
            // Check retrieval
            auto current = iter.current();
            sum += 1UL;
        }
        return sum;
    }
}

SCENARIO( "level generators can be solved", "[levelgen][solve]" ) {

    GIVEN( "A level generator with valid params" ) {
        WHEN( "solve() is called" ) {
            THEN( "a solution is returned" ) {
                LevelGenerator gen{
                    2, 20, 9, 2, 6, 1234
                };
                const char* res;
                REQUIRE_NOTHROW(res = gen.solve());
                REQUIRE_FALSE(res == nullptr);
                REQUIRE_FALSE(std::string(res).empty());
                REQUIRE(gen.get_num_levels() == 2);
            }

            THEN( "a best level exists" ) {
                LevelGenerator gen{
                    1, 10, 7, 2, 6, 1234
                };
                REQUIRE_NOTHROW(gen.solve());
                REQUIRE_FALSE(gen.best_level()->get_cost() == std::numeric_limits<int>::max());
            }

            THEN( "the best level has the correct count of symbols and and cost" ) {
                LevelGenerator gen{
                    2, 10, 7, 2, 6, 1234
                };
                REQUIRE_NOTHROW(gen.solve());

                const auto level = gen.best_level();

                // These values have been determined empirically
                REQUIRE(level->get_cost() == 5);
                REQUIRE(level->get_num_map_squares() == 84UL);
                REQUIRE(level->get_num_rooms() == 5UL);
                REQUIRE(level->get_num_adjacencies() == 8UL);
            }

            THEN( "the best level can iterate over symbols" ) {
                LevelGenerator gen{
                        2, 10, 7, 2, 6, 1234
                };
                REQUIRE_NOTHROW(gen.solve());

                const auto* level = gen.best_level();

                // These values match the test above
                REQUIRE(count_parts(level->map_squares()) == 84UL);
                REQUIRE(count_parts(level->rooms()) == 5UL);
                REQUIRE(count_parts(level->adjacencies()) == 8UL);
            }
        }
    }
    GIVEN( "A level generator with other params" ) {
        // A fuzz-like test to try some other params and make sanity checks
        WHEN( "solve() is called" ) {
            auto width = GENERATE(range(12U, 16U, 2U));
            auto height = GENERATE(range(9U, 13U, 2U));
            auto min_rooms = GENERATE(range(3U, 5U));
            auto max_rooms = GENERATE(range(8U, 16U, 4U));
            auto seed = GENERATE(take(5, random(1U, 1000U)));

            THEN( "the best level has appropriate numbers of symbols" ) {
                if (min_rooms > max_rooms)
                {
                    SKIP("invalid room constraint");
                }

                LevelGenerator gen{
                        1, width, height, min_rooms, max_rooms, seed
                };
                REQUIRE_NOTHROW(gen.solve());

                const auto* level = gen.best_level();

                // At least one square type per grid square plus some duplicates
                REQUIRE(level->get_num_map_squares() > width * height);

                // Num rooms within specified limits
                const auto num_rooms = level->get_num_rooms();
                const auto num_restricted_rooms = num_rooms - level->get_num_corridors();
                REQUIRE(num_restricted_rooms >= min_rooms);
                REQUIRE(num_restricted_rooms <= max_rooms);

                // Every room must have at least one adjacency, theoretical limit is every room adjacent to every other
                REQUIRE(level->get_num_adjacencies() >= num_rooms);
                REQUIRE(level->get_num_adjacencies() <= num_rooms * num_rooms);
            }
        }
    }
}
