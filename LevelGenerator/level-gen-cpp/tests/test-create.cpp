#include <catch2/catch_test_macros.hpp>
#include "level_gen.h"

SCENARIO( "level generators can be created", "[levelgen][creation]" ) {

    GIVEN( "Nothing" ) {
        WHEN( "a level generator is created" ) {
            THEN( "it exists" ) {
                LevelGenerator gen;
                REQUIRE_NOTHROW(gen = {});
            }
        }
    }
    GIVEN( "A level generator pointer" ) {
        std::unique_ptr<LevelGenerator> gen_ptr;

        REQUIRE_NOTHROW(gen_ptr.reset(new LevelGenerator{}));

        WHEN( "it is deleted" ) {
            THEN( "it no longer exists" ) {
                REQUIRE_NOTHROW(gen_ptr = nullptr);
            }
        }
    }
}
