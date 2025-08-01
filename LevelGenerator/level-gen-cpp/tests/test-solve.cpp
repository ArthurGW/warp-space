#include <catch2/catch_test_macros.hpp>
#include "level_gen.h"

SCENARIO( "level generators can be solved", "[levelgen][solve]" ) {

    GIVEN( "A level generator with valid params" ) {
        WHEN( "solve() is called" ) {
            THEN( "a solution is returned" ) {
                LevelGenerator gen{};
                std::string res;
                REQUIRE_NOTHROW(res = gen
                        .set_width(10)
                        .set_height(7)
                        .set_min_rooms(2)
                        .set_max_rooms(6)
                        .set_seed(1234)
                        .solve());
                REQUIRE(!res.empty());
            }

            THEN( "a best level exists" ) {
                LevelGenerator gen{};
                std::string res;
                REQUIRE_NOTHROW(res = gen
                        .set_width(10)
                        .set_height(7)
                        .set_min_rooms(2)
                        .set_max_rooms(6)
                        .set_seed(1234)
                        .solve());
                REQUIRE_NOTHROW(gen.best_level());
            }

            THEN( "the best level has valid symbols of each type" ) {
                LevelGenerator gen{};
                gen
                    .set_width(10)
                    .set_height(7)
                    .set_min_rooms(2)
                    .set_max_rooms(6)
                    .set_seed(1234)
                    .solve();
                auto level = gen.best_level();
                // At least one type per square (some can have multiple types i.e. ship and room)
                REQUIRE(level->num_map_squares() >= 10UL * 7UL);

                // Num rooms within specified limits
                REQUIRE(level->num_rooms() >= 2UL);
                REQUIRE(level->num_rooms() <= 6UL);

                // Every room must have at least one adjacency, theoretical limit is every room adjacent to every other
                REQUIRE(level->num_adjacencies() >= level->num_rooms());
                REQUIRE(level->num_adjacencies() <= level->num_rooms() * level->num_rooms());
            }
        }
    }
}
