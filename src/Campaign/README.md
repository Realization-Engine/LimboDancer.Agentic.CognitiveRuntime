# Operation Goodwood: Formation Map to ASL

The complete Goodwood transition covers joined boards B/A/C and all 114 ground formations, producing 624 ASL unit/equipment instances with stable identities, positions, catalog references and 94 weapon/operator links. Eight aircraft remain explicit off-map support; supply and command markers remain in campaign context.

The existing ASL implementation owns setup and play validation. Manufactured game-default profiles are identified explicitly. Available combat actions follow that implementation's current capabilities.

Current acceptance and limitations: [Goodwood release report](docs/Goodwood%20Transition%20Release%20Report.md). Detailed chronological evidence: [transition coverage](docs/Goodwood%20Transition%20Coverage.md). Design: [Formation to ASL implementation plan](../ASL/docs/Formation%20to%20ASL%20Map%20Alignment%20Design%20and%20Implementation%20Plan.md).

## Build and run

Requires .NET 10. The included terrain catalog fixture allows this workflow without an external VASL checkout. Campaign projects and shared ASL settings treat warnings as errors.

From the repository root, build before starting the services:

```powershell
dotnet build src/Campaign/Campaign.slnx -c Release
```

Start Studio in one terminal:

```powershell
$campaignRoot = (Get-Location).Path
dotnet run --project src/ASL/LimboDancer.Domains.Asl.MapStudio -c Release --no-build --no-launch-profile -- --environment Development --Kestrel:Endpoints:Http:Url http://127.0.0.1:5178 --AslMaps:AuthoredCatalogPath "$campaignRoot/src/Campaign/LimboDancer.Campaign.Refinement/Fixtures/SharedBoardMetadata.xml" --AslMaps:BoardsRoot "$campaignRoot/artifacts/campaign-asl-studio/boards" --AslMaps:CacheRoot "$campaignRoot/artifacts/campaign-asl-studio/cache"
```

Start Campaign Atlas in another terminal:

```powershell
dotnet run --project src/Campaign/LimboDancer.Campaign.PilotHost -c Release --no-build --no-launch-profile
```

Open `http://127.0.0.1:8093/index.html?exercise=source&source=5`. The campaign host's `Campaign:AslStudioUrl` defaults to `http://127.0.0.1:5178`. Studio's `Campaign:AtlasOrigin` defaults to `http://127.0.0.1:8093`. Keep the configured storage folders between runs. Other ports and storage roots can be supplied explicitly for isolated acceptance tests.

## Use the complete transition

1. Finish the Goodwood formation deployment and open its Situation map.
2. Choose **Open complete ASL setup**. Review the formation/counter counts and individual issues. Existing incomplete saves offer **Place remaining counters**, which preserves accepted coordinates.
3. Prepare and open the ASL setup. After validation, follow **Open ASL map** and confirm the side you are viewing.
4. Inspect terrain, parent ownership, counter origin and operator links. Use zoom, Fit and optional Formation boundaries. Hex labels appear at readable zoom and remain upright when rotated. A typed location applies with Enter; the manual setup editor accepts a full board-qualified address.
5. Use the existing proposal/confirmation play workflow. The campaign context panel provides return navigation. Reopening the same handoff preserves subsequent ASL progress.

Generation and preparation run as jobs with progress and cancellation. A failure retains accepted data and reports the problem. Existing invalid positions require review, and are not silently moved.

## Save and transfer

Use **Campaign > Save campaign to file** and **Load campaign from file**. Full-Situation files include formation and squad deployment, supply adjustments, accepted terrain/seed, counter relationships, campaign context and subsequent ASL history. Keep both services running when capturing or restoring ASL progress.

Actual-interface acceptance restored the full game into empty service storage and resumed Turn 1 Allied Prep Fire. Terrain, metadata, footprint and history matched the original files exactly. Repeated handoff cannot roll back later matching progress; divergent records are rejected. Returning ASL battle outcomes to campaign adjudication remains outside this transition.

## Generate and verify terrain independently

```powershell
dotnet run --project src/Campaign/LimboDancer.Campaign.Refinement.Cli -c Release --no-build -- prototypes/europe-hex-map/sources/situations/panzer-leader-05.json artifacts/goodwood-full 00112233445566778899aabbccddeeff --situation
```

Omit the seed to create and persist a new 128-bit seed. Use another output directory for another world. Generation does not reroll a failed seed. The accepted profile retains nominal 250 m formation and 40 m tactical spacing. The common lattice has unique parent ownership and protected compilation margins. Roads, streams, crossings and elevation remain tied to source geography; permitted building detail varies deterministically.

The original bounded package remains available by omitting `--situation`. Selected-formation and eight-squad pilot saves retain their compatibility paths. Their historical budgets and results are in [Pilot release review](docs/Pilot-Release-Review.md).

## Verification and diagnostics

```powershell
dotnet test src/Campaign/Campaign.slnx -c Release
python src/Campaign/scripts/audit_goodwood.py --check
node --test src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/boardViewport.pointer.test.mjs
```

Run `test_campaign_file.cjs` and `test_asl_situation.cjs` with Node from `prototypes/europe-hex-map` for portable-save/controller checks.

`src/Campaign/scripts/Measure-GoodwoodHandoff.ps1` measures preparation and repeated launch on an explicitly chosen local acceptance host/save. Add `mapMetrics=1` to an ASL play URL for opt-in scene timing attributes. Measurement boundaries and raw figures are in [release metrics](docs/goodwood-release-metrics-2026-10-15.json).

Full-map cold opening and preparation remain slower than the bounded pilot. The release report records latency, process memory, package size and current rules/storage limitations.
