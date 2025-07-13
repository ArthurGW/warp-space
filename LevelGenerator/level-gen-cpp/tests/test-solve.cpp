#include <catch2/catch_test_macros.hpp>
#include "level_gen.h"

SCENARIO( "level generators can be solved", "[levelgen][solve]" ) {

    GIVEN( "A level generator with a valid width and height" ) {
        WHEN( "solve() is called" ) {
            THEN( "a solution is returned" ) {
                LevelGenerator gen{4, 3};
                std::string res;
                REQUIRE_NOTHROW(res = gen.solve());
                REQUIRE(!res.empty());
            }
        }
    }
}
