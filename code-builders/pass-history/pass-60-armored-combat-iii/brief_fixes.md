# Brief for the fixing agent of pass 60

Read `brief_build.md` beside this file first; it binds you (Rules alone decides rules; absent-safety and ruling R60.3; the shell rules; the build and test scripts; no commit). Then apply the five fix lists beside this file, in this order: `fixes_boundary.md`, `fixes_referee1.md`, `fixes_referee2.md`, `fixes_table.md`, `fixes_ui.md`. Where two lists name the same fix, it is one fix (each list says so). The rulings R60.24 to R60.27 at the heads of the lists bind you like the design's.

Each item names a file and member with an approximate line: read the code before changing it, and read the rule on its page (`E:/Archive/GitHub/dlandi/asl-gate-work/rulebook-pages/pNNN.txt`) for every rule item. Add a test for every fix that changes a result (Rules tests in `ScenarioA1Pass60RulesTests`, Play tests in `BacklogPass60Tests`, page tests in the MapStudio test project, a new page test class calling `context.UseViewport()` and naming its game after itself). Format every test file you touch with `dotnet format whitespace <csproj> --include <files>`.

Building and testing: `build_studio.sh` must end with exit 0. Run the test classes of the pass (`ScenarioA1Pass60RulesTests`, `BacklogPass60Tests`), the classes of the code each fix touches, the MapStudio page classes you touch (`PlayPageVehicleTests`, `PlayPageOrdnanceTests`, `PlayPageFireTests`, `PlayRecords*`), and, since the boundary fix 1 changes a fact record's shape and the C11.3 fix a state record, the Units project whole. One build or test at a time.

The live test games `p60-*` in `src/ASL/boards/units/live/...`: do not touch them. Say in your report which of them a fix makes stale (a recorded fact whose shape changed, a result that changed): the session records them again.

Report (closing message, under 1,200 words): first what you did not fix or fixed otherwise than the list says, with the reason; then each list's items as fixed, one line each with the member; the build's result and each test run's exact counts; what an older record does after each fix that touches a record; which `p60-*` games are stale; anything else you saw outside the lists.
