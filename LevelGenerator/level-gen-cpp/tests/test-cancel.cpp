#include <catch2/catch_test_macros.hpp>
#include <catch2/generators/catch_generators_all.hpp>
#include <thread>
#include "level_gen.h"

namespace
{
    thread_local auto current_n = 1U;

    bool check_cancel()
    {
        return (--current_n) == 0U;
    }
}

SCENARIO("level generators can be cancelled", "[levelgen][cancel]")
{
    GIVEN("A level generator with valid params")
    {
        WHEN("solve() is called with a cancellation callback")
        {
            auto n = GENERATE(range(2U, 4U, 2U));

            THEN("solving can be cancelled")
            {
                LevelGenerator gen{
                        20, 15, 12, 1, 6, 1, 1, 123456
                };
                const char* res;

                // This `current_n` is used by `check_cancel` to cancel the run after `current_n` checks
                // It is n*2 because cancellation is checked twice per model iteration
                current_n = n * 2;
                REQUIRE_NOTHROW(res = gen.solve(check_cancel));
                REQUIRE_FALSE(res == nullptr);
                REQUIRE_FALSE(std::string(res).empty());
                REQUIRE(gen.get_num_levels() == n);
            }
        }

        WHEN("solve() is called")
        {
            THEN("solving can be interrupted")
            {
                LevelGenerator gen{
                        200, 10, 10, 1, 6, 1, 1, 1234
                };
                const char* res;

                std::thread sleeper([&]() {
                    std::this_thread::sleep_for(std::chrono::seconds(5));

                    gen.interrupt();
                });

                REQUIRE_NOTHROW(res = gen.solve());
                sleeper.join();  // This will only be hit once solve() has been interrupted by the sleeper thread

                REQUIRE_FALSE(res == nullptr);
                // We don't check the actual content of `res` as it is possible no valid models have been generated yet
            }

            THEN("calling interrupt_if_has_level returns true")
            {
                LevelGenerator gen{
                        1, 10, 10, 1, 6, 1, 1, 1234
                };

                const char* res;
                REQUIRE_NOTHROW(res = gen.solve());

                REQUIRE(gen.interrupt_if_has_level());
            }
        }

        WHEN("solve() has not been called")
        {
            THEN("calling interrupt_if_has_level returns false")
            {
                LevelGenerator gen{
                        200, 10, 10, 1, 6, 1, 1, 1234
                };

                REQUIRE_FALSE(gen.interrupt_if_has_level());
            }
        }
    }
}
