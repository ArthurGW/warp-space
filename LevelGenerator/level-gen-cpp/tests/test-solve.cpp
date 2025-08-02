#include <catch2/catch_test_macros.hpp>
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
                REQUIRE(gen.num_levels() == 2);
            }

            THEN( "a best level exists" ) {
                LevelGenerator gen{
                    1, 10, 7, 2, 6, 1234
                };
                REQUIRE_NOTHROW(gen.solve());
                REQUIRE_FALSE(gen.best_level() == nullptr);
            }

            THEN( "the best level has the correct count of symbols and and cost" ) {
                LevelGenerator gen{
                    2, 10, 7, 2, 6, 1234
                };
                REQUIRE_NOTHROW(gen.solve());

                const auto* level = gen.best_level();

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

                // At least one square type per grid square plus some duplicates
                REQUIRE(level->get_num_map_squares() > 10UL * 7UL);

                // Num rooms within specified limits
                REQUIRE(level->get_num_rooms() >= 2UL);
                REQUIRE(level->get_num_rooms() <= 6UL);

                // Every room must have at least one adjacency, theoretical limit is every room adjacent to every other
                REQUIRE(level->get_num_adjacencies() >= level->get_num_rooms());
                REQUIRE(level->get_num_adjacencies() <= level->get_num_rooms() * level->get_num_rooms());
            }
        }
    }
}
