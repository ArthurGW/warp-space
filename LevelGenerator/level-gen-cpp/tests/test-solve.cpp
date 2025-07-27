#include <catch2/catch_test_macros.hpp>
#include "level_gen.h"

SCENARIO( "level generators can be solved", "[levelgen][solve]" ) {

    GIVEN( "A level generator with a valid width and height" ) {
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
        }
    }
}
