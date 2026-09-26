using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Los;

/// <summary>
/// A read-only LOS check between two center locations (LOS Design, sections 3 and 5), reproducing VASL's
/// <c>VASL.LOS.Map.Map.LOS</c> for a map without counters, overlays, or night: the same walk over the terrain grid,
/// the same rules in the same order, and the same result. Rules this step does not reproduce make the result
/// unsupported, with the rule's name, at the point where VASL would apply them; nothing falls back to a simpler rule.
/// </summary>
/// <remarks>
/// Locations are the hex's center locations and their up and down chain, at the hex center, as VASL's center
/// locations use; hexside (bypass) locations are refused. A location whose board or hex is not on the map, whose hex
/// another board owns, or whose level is not in the hex's location chain is refused with an
/// <see cref="ArgumentException"/>.
/// </remarks>
public static class LosCalculator
{
    /// <summary>Checks LOS from the source location to the target location on the map.</summary>
    public static LosResult Check(LosMap map, BoardLocation source, BoardLocation target)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        var (sourceHex, sourceLevel) = Resolve(map, source, nameof(source));
        var (targetHex, targetLevel) = Resolve(map, target, nameof(target));
        var result = new Walk(map, sourceHex, sourceLevel, targetHex, targetLevel).Run();
        return result.Status is LosStatus.Clear or LosStatus.Blocked && !map.IsDefinitive
            ? result with
            {
                Status = LosStatus.Nondefinitive
            }
            : result;
    }

    private static (HexIndex Hex, LocationFacts Level) Resolve(LosMap map, BoardLocation location, string parameter)
    {
        if (location.Side is not null)
        {
            throw new ArgumentException($"{location} is a hexside location; LOS reads center locations only.", parameter);
        }

        if (map.Locate(location.Board, location.Hex) is not { } hex)
        {
            throw new ArgumentException($"{location.Board}:{location.Hex} is not on the map.", parameter);
        }

        if (map.OwnerOf(hex) is { } owner && owner != (location.Board, location.Hex))
        {
            throw new ArgumentException($"{location.Board}:{location.Hex} is a hex shared with {owner.Board}, which owns it as {owner.Board}:{owner.Hex}.", parameter);
        }

        var facts = map.FactsOf(hex);
        return facts.Locations.FirstOrDefault(level => level.Level == location.Level) is { } found
            ? (hex, found)
            : throw new ArgumentException(
                $"{location} is not in the hex's location chain ({string.Join(", ", facts.Locations.Select(level => level.Level))}).", parameter);
    }

    /// <summary>
    /// One LOS check: VASL's <c>LOSStatus</c> and <c>LOSResult</c> state, and <c>Map.LOS</c> with the rule methods it
    /// calls. Every rule method returns true when the walk must stop: when LOS is blocked, or when the check has become
    /// unsupported (<see cref="unsupported"/> is set).
    /// </summary>
    private sealed class Walk
    {
        private const int None = -1;
        private const string Bocage = "Bocage";
        private const string PartialOrchard = "PartialOrchard";
        private const string Hillock = "Hillock";

        private readonly LosMap map;
        private readonly HexIndex sourceHex;
        private readonly HexIndex targetHex;
        private readonly LocationFacts source;
        private readonly LocationFacts target;
        private readonly GridPoint sourcePoint;
        private readonly GridPoint targetPoint;
        private readonly int sourceX;
        private readonly int sourceY;
        private readonly int targetX;
        private readonly int targetY;
        private readonly int colDir;
        private readonly int rowDir;
        private readonly int numCols;
        private readonly double deltaY;
        private readonly int sourceElevation;
        private readonly int targetElevation;
        private readonly bool sourceIsCellar;
        private readonly bool targetIsCellar;
        private readonly bool sourceIsRooftop;
        private readonly bool targetIsRooftop;
        private readonly double sourceRooftopAdjustment;
        private readonly double targetRooftopAdjustment;
        private readonly int range;
        private readonly bool losIs60Degree;
        private readonly bool losIsHorizontal;
        private readonly int[] sourceExitHexsides = [None, None];
        private readonly int[] targetEnterHexsides = [None, None];
        private readonly Dictionary<int, double> mapHindrances = [];
        private readonly bool exitsSourceDepression;
        private readonly bool entersTargetDepression;

        // LOSStatus
        private double enter;
        private double exit;
        private int currentCol;
        private int currentRow;
        private TerrainType? currentTerrain;
        private int currentTerrainHgt;
        private int groundLevel;
        private HexIndex currentHex;
        private HexIndex? previousHex;
        private HexIndex tempHex;
        private int rangeToSource;
        private int rangeToTarget;
        private bool losLeavesBuilding;
        private HexIndex? ignoreGroundLevelHex;
        private bool blocked;
        private string reason = string.Empty;

        // LOSResult
        private int resultRange;
        private int sourceExitHexspine = None;
        private bool resultBlocked;
        private GridPoint? blockedAtPoint;
        private string resultReason = string.Empty;

        private string? unsupported;

        // LOSStatus constructor
        public Walk(LosMap map, HexIndex sourceHex, LocationFacts source, HexIndex targetHex, LocationFacts target)
        {
            this.map = map;
            this.sourceHex = sourceHex;
            this.targetHex = targetHex;
            this.source = source;
            this.target = target;
            sourcePoint = map.LosPoint(sourceHex);
            targetPoint = map.LosPoint(targetHex);
            sourceX = sourcePoint.X;
            sourceY = sourcePoint.Y;
            targetX = targetPoint.X;
            targetY = targetPoint.Y;
            colDir = targetX - sourceX < 0 ? -1 : 1;
            rowDir = targetY - sourceY < 0 ? -1 : 1;
            numCols = Math.Abs(targetX - sourceX) + 1;
            currentHex = sourceHex;
            tempHex = sourceHex;

            // setSourceAndTargetElevations, for center locations.
            sourceElevation = Facts(sourceHex).BaseLevel + source.Level;
            targetElevation = Facts(targetHex).BaseLevel + target.Level;

            // Cellars and rooftops are center locations, so they take no vertex adjustment. The rules below count a
            // cellar one level higher, and a rooftop half a level lower unless it is level 1 of its hex.
            sourceIsCellar = source.Terrain is { IsCellar: true };
            targetIsCellar = target.Terrain is { IsCellar: true };
            sourceIsRooftop = source.Terrain is { IsRooftop: true };
            targetIsRooftop = target.Terrain is { IsRooftop: true };
            sourceRooftopAdjustment = sourceIsRooftop && source.Level != 1 ? -0.5 : 0.0;
            targetRooftopAdjustment = targetIsRooftop && target.Level != 1 ? -0.5 : 0.0;
            range = map.Geometry.Distance(sourceHex, targetHex);
            rangeToTarget = range;
            losLeavesBuilding = CenterTerrain(sourceHex) is not { IsBuilding: true };
            deltaY = ((double)targetY - sourceY) / numCols;
            enter = sourceY;
            exit = enter + deltaY;
            resultRange = rangeToTarget;
            var line = new LosLine(sourcePoint, targetPoint);
            losIsHorizontal = line.IsHorizontal;
            losIs60Degree = line.Is60Degree(range);
            if (losIs60Degree)
            {
                sourceExitHexspine = colDir == 1 ? (rowDir == 1 ? 3 : 1) : (rowDir == 1 ? 4 : 0);
            }
            else if (line.Slope == 0.0)
            {
                sourceExitHexspine = colDir == 1 ? 2 : 5;
            }

            // setEnterExitHexsides for center locations: the target's are the opposite of the source's.
            var exits = ExitFromCenterHexsides();
            sourceExitHexsides[0] = exits[0];
            sourceExitHexsides[1] = exits[1];
            exits = ExitFromCenterHexsides();
            targetEnterHexsides[0] = Opposite(exits[0]);
            targetEnterHexsides[1] = Opposite(exits[1]);

            // exitsSourceDepression and entersTargetDepression: whether the depression restrictions (A6.3) apply to a
            // depression end. VASL lowers a rooftop a full level here, not the half level of the rules.
            double sourceHeight = sourceElevation + (sourceIsRooftop && source.Level != 1 ? -1 : 0);
            double targetHeight = targetElevation + (targetIsRooftop && target.Level != 1 ? -1 : 0);
            var alongHexside = losIs60Degree || losIsHorizontal;
            exitsSourceDepression = source.DepressionTerrain is not null
                && ((targetHeight - sourceHeight > 0 && targetHeight - sourceHeight <= range && range != 1)
                    || (alongHexside && ExitsDepressionTerrainHexside())
                    || targetHeight == sourceHeight
                    || sourceHeight > targetHeight);
            entersTargetDepression = target.DepressionTerrain is not null
                && ((sourceHeight - targetHeight > 0 && sourceHeight - targetHeight <= range && range != 1)
                    || (alongHexside && EntersDepressionTerrainHexside())
                    || targetHeight == sourceHeight
                    || targetHeight > sourceHeight);
        }

        private int TargetEnterHexspine => losIs60Degree
            ? (colDir == 1 ? (rowDir == 1 ? 0 : 4) : (rowDir == 1 ? 1 : 3))
            : sourceExitHexspine == None ? None : (colDir == 1 ? 5 : 2);

        public LosResult Run()
        {
            // Map.LOS: the same location (never asked by the oracle, but VASL answers it).
            if (sourceHex == targetHex && source.Level == target.Level)
            {
                return new LosResult(LosStatus.Clear, false, 0, 0, null, string.Empty);
            }

            if (sourceHex == targetHex)
            {
                return CheckSameHexRule();
            }

            if (SetupProblem() is { } problem)
            {
                return LosResult.Unsupported(range, problem);
            }

            WalkColumns();
            if (unsupported is not null)
            {
                return LosResult.Unsupported(range, unsupported);
            }

            return Result();
        }

        private LosResult Result()
        {
            var hindrance = Hindrance();
            if (!resultBlocked)
            {
                return new LosResult(LosStatus.Clear, false, resultRange, hindrance, null, string.Empty);
            }

            var point = blockedAtPoint!.Value;
            var hex = map.Locator.GridToHex(point.X, point.Y);
            var owner = hex is { } index ? map.OwnerOf(index) : null;
            return new LosResult(LosStatus.Blocked, true, resultRange, hindrance, new LosBlockedAt(point, owner?.Board, owner?.Hex), resultReason);
        }

        // Map.checkSameHexRule, for two center locations of one hex. Bridges and tunnels are not reproduced.
        private LosResult CheckSameHexRule()
        {
            if (source.Terrain is not { } sourceTerrain || target.Terrain is not { } targetTerrain)
            {
                return LosResult.Unsupported(0, LosUnsupportedRule.MissingLocationTerrain);
            }

            if (sourceTerrain.IsBridge || sourceTerrain.IsTunnel || targetTerrain.IsBridge || targetTerrain.IsTunnel)
            {
                return LosResult.Unsupported(0, LosUnsupportedRule.Bridge);
            }

            if (sourceTerrain.IsBuildingTerrain && targetTerrain.IsBuildingTerrain
                && (Math.Abs(source.Level - target.Level) > 1 || !Facts(sourceHex).Stairway))
            {
                var point = sourcePoint;
                var hex = map.Locator.GridToHex(point.X, point.Y);
                var owner = hex is { } index ? map.OwnerOf(index) : null;
                return new LosResult(LosStatus.Blocked, true, 0, 0, new LosBlockedAt(point, owner?.Board, owner?.Hex), "Crosses building level or no stairway");
            }

            return new LosResult(LosStatus.Clear, false, 0, 0, null, string.Empty);
        }

        // The parts of the LOSStatus constructor this step does not reproduce: special source and target locations,
        // slopes, and hillocks. Cellars, rooftops of buildings other than factories, and depression ends are reproduced.
        private string? SetupProblem()
        {
            foreach (var (location, hex) in new[] { (source, sourceHex), (target, targetHex) })
            {
                if (location.Terrain is not { } terrain)
                {
                    return LosUnsupportedRule.MissingLocationTerrain;
                }

                if (location.Level < 0 && !terrain.IsCellar)
                {
                    return LosUnsupportedRule.Cellar;
                }

                // A factory rooftop takes the factory rules (isRooftopLOSBlocked, B23.87).
                if (terrain.IsFactory || terrain.IsRoofless || (terrain.IsRooftop && CenterTerrain(hex) is { IsFactory: true } or { IsRoofless: true }))
                {
                    return LosUnsupportedRule.Factory;
                }

                if (terrain.IsEntrenchment)
                {
                    return LosUnsupportedRule.Entrenchment;
                }

                if (terrain.IsBridge || terrain.IsTunnel)
                {
                    return LosUnsupportedRule.Bridge;
                }

                if (terrain.Name.Contains("Railroad, Embankment", StringComparison.Ordinal))
                {
                    return LosUnsupportedRule.RailroadEmbankment;
                }

                if (terrain.Name == Hillock)
                {
                    return LosUnsupportedRule.Hillock;
                }
            }

            // slopes: the higher location is up-slope.
            if ((ExitsSlopeHexside() && sourceElevation >= targetElevation) || (EntersSlopeHexside() && targetElevation >= sourceElevation))
            {
                return LosUnsupportedRule.Slope;
            }

            // setAdjacentToHillock
            if (AdjacentHillock(sourceHex, sourceExitHexsides) || AdjacentHillock(targetHex, targetEnterHexsides))
            {
                return LosUnsupportedRule.Hillock;
            }

            return null;
        }

        private bool ExitsSlopeHexside() => HasSlope(sourceHex, sourceExitHexsides[0]) || HasSlope(sourceHex, sourceExitHexsides[1]);

        private bool EntersSlopeHexside() => HasSlope(targetHex, targetEnterHexsides[0]) || HasSlope(targetHex, targetEnterHexsides[1]);

        private bool HasSlope(HexIndex hex, int side) => side != None && Facts(hex).Hexsides[side].Slope;

        // LOSStatus.exitsDepressionTerrainHexside: never along a hexside, so its use in the setup is always false.
        private bool ExitsDepressionTerrainHexside() => !losIs60Degree && !losIsHorizontal
            && (SideIsDepression(sourceHex, sourceExitHexsides[0]) || SideIsDepression(sourceHex, sourceExitHexsides[1]));

        // LOSStatus.entersDepressionTerrainHexside
        private bool EntersDepressionTerrainHexside() => !losIs60Degree && !losIsHorizontal
            && (SideIsDepression(targetHex, targetEnterHexsides[0]) || SideIsDepression(targetHex, targetEnterHexsides[1]));

        private bool SideIsDepression(HexIndex hex, int side) => side != None && Facts(hex).Hexsides[side].DepressionTerrain is not null;

        // LOSStatus.exitHexsideIsCrest: the LOS leaves the source by a crest line hexside, where the base levels differ,
        // or where a depression hexside separates a depression hex from one that is not.
        private bool ExitHexsideIsCrest() => IsCrest(sourceHex, sourceExitHexsides[0]) || (!Failed && IsCrest(sourceHex, sourceExitHexsides[1]));

        // LOSStatus.enterHexsideIsCrest
        private bool EnterHexsideIsCrest() => IsCrest(targetHex, targetEnterHexsides[0]) || (!Failed && IsCrest(targetHex, targetEnterHexsides[1]));

        // One hexside of the crest tests. Java's precedence makes the last test independent of the hexside being set;
        // with no hexside VASL reads hexside 0 and the hex itself as its neighbor, so that test is then always false.
        private bool IsCrest(HexIndex hex, int side)
        {
            if (VaslAdjacent(hex, side) is not { } adjacent)
            {
                Refuse(LosUnsupportedRule.VaslFails);
                return false;
            }

            if (side != None && (Facts(hex).BaseLevel != Facts(adjacent).BaseLevel
                || (IsDepressionHex(hex) && SideIsDepression(hex, side) && !IsDepressionHex(adjacent))))
            {
                return true;
            }

            return !IsDepressionHex(hex) && SideIsDepression(hex, LocationSide(side)) && IsDepressionHex(adjacent);
        }

        private bool AdjacentHillock(HexIndex hex, int[] sides) =>
            sides.Any(side => side != None && Adjacent(hex, side) is { } adjacent && CenterTerrain(adjacent)?.Name == Hillock);

        // Map.LOS: column by column, and row by row within a column.
        private void WalkColumns()
        {
            currentCol = sourceX;
            for (var col = 0; col < numCols; col++)
            {
                currentRow = (int)enter;
                var numRows = Math.Abs((int)exit - (int)enter) + 1;
                for (var row = 0; row < numRows; row++)
                {
                    currentTerrain = map.Grid.TryGetCode(currentCol, currentRow, out var code) && map.Catalog.TryGet(code, out var type) ? type : null;
                    groundLevel = map.Geometry.ContainsCell(currentCol, currentRow) ? map.Grid.ElevationAt(currentCol, currentRow) : 0;

                    // temp hex is the hex the LOS point is in
                    if (map.Locator.ExtendedBorderContains(sourceHex, currentCol, currentRow))
                    {
                        tempHex = sourceHex;
                    }
                    else if (map.Locator.ExtendedBorderContains(targetHex, currentCol, currentRow))
                    {
                        tempHex = targetHex;
                    }
                    else if (map.Locator.GridToHex(currentCol, currentRow) is { } found)
                    {
                        tempHex = found;
                    }
                    else
                    {
                        return;
                    }

                    // LOS leaves the source building?
                    if (!losLeavesBuilding && currentTerrain is not null && !currentTerrain.IsBuilding)
                    {
                        losLeavesBuilding = true;
                    }

                    // If LOS is on a hexside fetch the two adjacent hexes
                    (HexIndex Top, HexIndex? Bottom)? adjacentHexes = null;
                    if (losIsHorizontal || losIs60Degree)
                    {
                        adjacentHexes = AdjacentHexes(tempHex);
                    }

                    if (adjacentHexes is not { } pair)
                    {
                        // LOS along a top or bottom edge of the map
                        if ((currentRow == 0 || currentRow + 1 == map.Geometry.GridHeight)
                            && map.Locator.ExtendedBorderContains(tempHex, currentCol, currentRow)
                            && CheckLosOnHexsideRule(tempHex))
                        {
                            return;
                        }

                        if (ApplyLosRules())
                        {
                            return;
                        }
                    }
                    else
                    {
                        // for LOS on a hexside both hexes are checked
                        if (map.Locator.ExtendedBorderContains(pair.Top, currentCol, currentRow) && CheckLosOnHexsideRule(pair.Top))
                        {
                            return;
                        }

                        if (pair.Bottom is { } bottom && map.Locator.ExtendedBorderContains(bottom, currentCol, currentRow) && CheckLosOnHexsideRule(bottom))
                        {
                            return;
                        }
                    }

                    currentRow += rowDir;
                }

                enter = exit;
                currentCol += colDir;
                exit = col + 1 == numCols ? targetY : exit + deltaY;
            }
        }

        // Map.getAdjacentHexes: the two hexes at a range touched by a line along a hexside; null if not on a hexside.
        private (HexIndex Top, HexIndex? Bottom)? AdjacentHexes(HexIndex rangeHex)
        {
            // Only the hex's own center location, not its upper levels, takes the even-range shortcut.
            var sourceIsCenter = source.Level == Facts(sourceHex).Center.Level;
            if (map.Locator.ExtendedBorderContains(sourceHex, currentCol, currentRow)
                || map.Locator.ExtendedBorderContains(targetHex, currentCol, currentRow)
                || (sourceIsCenter && map.Geometry.Distance(sourceHex, rangeHex) % 2 == 0))
            {
                return null;
            }

            for (var side = 0; side < 6; side++)
            {
                if (Adjacent(rangeHex, side) is { } other && map.Locator.ExtendedBorderContains(other, currentCol, currentRow)
                    && other != sourceHex && other != targetHex)
                {
                    return (rangeHex, other);
                }
            }

            var nearest = NearestSide(rangeHex);
            if (nearest != None && Adjacent(rangeHex, nearest) is { } across && across != sourceHex && across != targetHex)
            {
                return (rangeHex, across);
            }

            return null;
        }

        // Map.checkLOSOnHexsideRule
        private bool CheckLosOnHexsideRule(HexIndex hex)
        {
            tempHex = hex;

            // we can ignore source/target hex for this rule
            if (hex == sourceHex || hex == targetHex)
            {
                return false;
            }

            if (currentTerrain is null)
            {
                return Refuse(LosUnsupportedRule.UnknownTerrain);
            }

            if (CenterTerrain(hex) is not { } center)
            {
                return Refuse(LosUnsupportedRule.MissingLocationTerrain);
            }

            // force check of hexside terrain for hexes with inherent terrain (e.g. an orchard hex with a wall)
            if (currentTerrain.IsHexsideTerrain && center.IsInherent && ApplyLosRules())
            {
                return true;
            }

            // ensure inherent terrain is not missed, except PTO terrain along water
            if (center.IsInherent && !((center.Name == "Dense Jungle" || center.Name == "Bamboo") && currentTerrain.Category == LosCategory.Water))
            {
                currentTerrain = center;
            }

            // ensure partial orchards are not missed
            var side = NearestSide(tempHex);
            if (side != None && Facts(tempHex).Hexsides[side].PartialOrchard)
            {
                return Refuse(LosUnsupportedRule.PartialOrchard);
            }

            return ApplyLosRules();
        }

        // Map.applyLOSRules: the rules at one point; adjusts the status when the LOS enters a new hex.
        private bool ApplyLosRules()
        {
            if (currentTerrain is null)
            {
                return Refuse(LosUnsupportedRule.UnknownTerrain);
            }

            currentTerrainHgt = currentTerrain.Height;

            // are we in a new hex?
            if (tempHex != currentHex)
            {
                // the new hex may be the previous one again, around vertices or along hexsides
                var newEqualsPreviousHex = false;
                if (previousHex is null)
                {
                    previousHex = currentHex;
                }
                else if (previousHex != tempHex)
                {
                    previousHex = currentHex;
                }
                else
                {
                    newEqualsPreviousHex = true;
                }

                currentHex = tempHex;
                rangeToSource = map.Geometry.Distance(currentHex, sourceHex);
                rangeToTarget = map.Geometry.Distance(currentHex, targetHex);
                if (LosFollowsDepression(newEqualsPreviousHex))
                {
                    ignoreGroundLevelHex = currentHex;
                }
            }

            if (PointProblem() is { } problem)
            {
                return Refuse(problem);
            }

            // No counters, and not at night: the counter terrain, NVR, smoke, vehicle, and OBA rules do nothing.
            if (CheckDepressionRule())
            {
                return true;
            }

            if (CheckBuildingRestrictionRule())
            {
                return true;
            }

            if (currentTerrain.IsHexsideTerrain && !currentTerrain.IsCliff && CheckHexsideTerrainRule())
            {
                return true;
            }

            // checkRBrrembankments and checkPartialOrchards
            var nearest = NearestSide(currentHex);
            if (nearest != None && (Facts(currentHex).Hexsides[nearest].RailroadEmbankment || Facts(currentHex).Hexsides[nearest].PartialOrchard))
            {
                return Refuse(Facts(currentHex).Hexsides[nearest].RailroadEmbankment ? LosUnsupportedRule.RailroadEmbankment : LosUnsupportedRule.PartialOrchard);
            }

            // The current hex is checked only between the source and target. For center locations VASL's tests 3 to 5
            // never hold, which leaves tests 1 and 2.
            var test1 = currentHex != sourceHex && range != rangeToTarget;
            var test2 = currentHex != targetHex && range != rangeToSource;
            if (test1 && test2)
            {
                // ignore inherent terrain that "spills" into an adjacent hex
                if (currentTerrain.IsInherent && CenterTerrain(currentHex)?.Code != currentTerrain.Code && InherentSpillTest())
                {
                    return false;
                }

                // The bridge hindrance rule needs a bridge, which is refused.
                if (CheckGroundLevelRule() || CheckSplitTerrainRule() || CheckHalfLevelTerrainRule() || CheckTerrainIsHigherRule()
                    || CheckTerrainHeightRule() || CheckBlindHexRule())
                {
                    return true;
                }

                // The hillock rule applies only with hillocks, which are refused.
            }

            if (blocked)
            {
                SetBlocked(currentCol, currentRow, reason);
                return true;
            }

            return false;
        }

        // The terrain and hex features at this point whose rules are not reproduced.
        private string? PointProblem()
        {
            var terrain = currentTerrain!;
            var name = terrain.Name;
            if (terrain.IsBridge || terrain.IsTunnel)
            {
                return LosUnsupportedRule.Bridge;
            }

            // Rooftop and cellar terrain met along the line has no rule of its own in VASL; only the ends' are adjusted.
            if (terrain.IsFactory || terrain.IsRoofless || terrain.IsOutsideFactoryWall)
            {
                return LosUnsupportedRule.Factory;
            }

            if (terrain.IsEntrenchment)
            {
                return LosUnsupportedRule.Entrenchment;
            }

            if (terrain.IsRowhouseFactoryWallOrBreach || name.Contains("Gutted Building Wall", StringComparison.Ordinal))
            {
                return LosUnsupportedRule.RowhouseWall;
            }

            if (name.Contains(Bocage, StringComparison.Ordinal))
            {
                return LosUnsupportedRule.Bocage;
            }

            if (name.Contains(PartialOrchard, StringComparison.Ordinal))
            {
                return LosUnsupportedRule.PartialOrchard;
            }

            if (name.Contains("Railroad, Embankment", StringComparison.Ordinal) || name.Contains("Rrembankment", StringComparison.Ordinal))
            {
                return LosUnsupportedRule.RailroadEmbankment;
            }

            if (name.Contains(Hillock, StringComparison.Ordinal))
            {
                return LosUnsupportedRule.Hillock;
            }

            if (name.Contains("Rubble", StringComparison.Ordinal))
            {
                return LosUnsupportedRule.Rubble;
            }

            if (name.Contains("Deir", StringComparison.Ordinal))
            {
                return LosUnsupportedRule.Deir;
            }

            if (name == "Orchard, Out of Season")
            {
                return LosUnsupportedRule.OrchardOutOfSeason;
            }

            if (name.Contains("Sand Dune", StringComparison.Ordinal) || name.Contains("Dune, Crest", StringComparison.Ordinal))
            {
                return LosUnsupportedRule.SandDune;
            }

            if (name.Contains("Volga Pier", StringComparison.Ordinal))
            {
                return LosUnsupportedRule.VolgaPier;
            }

            var facts = Facts(currentHex);
            if (facts.Center.Terrain is not { } center)
            {
                return LosUnsupportedRule.MissingLocationTerrain;
            }

            if (facts.Bridge is not null || center.IsBridge || center.IsTunnel)
            {
                return LosUnsupportedRule.Bridge;
            }

            if (center.IsFactory || center.IsRoofless)
            {
                return LosUnsupportedRule.Factory;
            }

            return center.Name.Contains(Hillock, StringComparison.Ordinal) ? LosUnsupportedRule.Hillock : null;
        }

        // Map.checkDepressionRule: the depression exit and entry restrictions (A6.3), checked in every hex, and the crest
        // line at a vertex (B19.51). Its bridge adjustments need a bridge, which is refused.
        private bool CheckDepressionRule()
        {
            var finalSource = sourceElevation + RooftopHalfLevel(sourceIsRooftop, source);
            var finalTarget = targetElevation + RooftopHalfLevel(targetIsRooftop, target);
            var currentBase = Facts(currentHex).BaseLevel;
            var currentIsDepression = IsDepressionHex(currentHex);
            var cell = (X: (double)currentCol, Y: (double)currentRow);
            var currentCenter = map.LosPoint(currentHex);

            if (exitsSourceDepression)
            {
                var followsDepiction = LosCrossingBridgeDepiction() && !ExitsByRoadHexside();
                var test1 = rangeToTarget > finalTarget - finalSource && currentHex != targetHex
                    && !(currentIsDepression && (groundLevel == currentBase || finalSource >= groundLevel || followsDepiction))
                    && !(!currentIsDepression && groundLevel <= finalSource);
                if (test1)
                {
                    // adjacent hexes across a depression hexside
                    if (range == 1 && ExitsDepressionTerrainHexside())
                    {
                        test1 = false;
                    }

                    // same-level LOS that follows the depression
                    if (finalTarget - finalSource == 0 && currentHex != targetHex
                        && ((currentIsDepression && (groundLevel == currentBase || followsDepiction)) || (!currentIsDepression && groundLevel <= finalTarget)))
                    {
                        test1 = false;
                    }

                    if (SpecialTestDepressionGroundLevel(rangeToTarget, targetElevation, targetPoint))
                    {
                        test1 = false;
                    }
                }

                var test2 = !test1 && rangeToTarget == 1
                    && currentIsDepression && !(groundLevel == currentBase || finalSource >= groundLevel || finalTarget > groundLevel) && !followsDepiction
                    && LosMap.Distance(targetPoint.X, targetPoint.Y, cell.X, cell.Y) > LosMap.Distance(targetPoint.X, targetPoint.Y, currentCenter.X, currentCenter.Y);
                if (!test1 && !test2 && currentHex == sourceHex && currentTerrain!.IsCliff && CheckBlindHexRule())
                {
                    return true;
                }

                if (test1 || test2)
                {
                    return Block("Exits depression before range/elevation restrictions are satisfied (A6.3)");
                }

                if (ExitHexsideIsCrest() && (losIs60Degree || losIsHorizontal) && finalSource >= finalTarget && !source.Terrain!.IsBridge)
                {
                    return Block("Exits Crest Line - Depression hexside at vertex (B19.51)");
                }

                if (Failed)
                {
                    return true;
                }
            }

            if (entersTargetDepression)
            {
                var followsDepiction = LosCrossingBridgeDepiction() && !EntersByRoadHexside();
                var test1 = rangeToSource > finalSource - finalTarget && currentHex != sourceHex
                    && !(currentIsDepression && (groundLevel == currentBase || finalTarget >= groundLevel || followsDepiction))
                    && !(!currentIsDepression && groundLevel <= finalTarget);
                if (test1)
                {
                    // VASL tests the exit hexsides here too
                    if (range == 1 && ExitsDepressionTerrainHexside())
                    {
                        test1 = false;
                    }

                    if (finalSource - finalTarget == 0 && currentHex != sourceHex
                        && ((currentIsDepression && (groundLevel == currentBase || followsDepiction)) || (!currentIsDepression && groundLevel <= finalTarget)))
                    {
                        test1 = false;
                    }

                    if (SpecialTestDepressionGroundLevel(rangeToSource, sourceElevation, sourcePoint))
                    {
                        test1 = false;
                    }
                }

                var test2 = !test1 && rangeToSource == 1
                    && currentIsDepression && !(groundLevel == currentBase || finalTarget >= groundLevel || finalSource > groundLevel) && !followsDepiction
                    && LosMap.Distance(sourcePoint.X, sourcePoint.Y, cell.X, cell.Y) > LosMap.Distance(sourcePoint.X, sourcePoint.Y, currentCenter.X, currentCenter.Y);
                if (!test1 && !test2 && currentHex == targetHex && currentTerrain!.IsCliff && CheckBlindHexRule())
                {
                    return true;
                }

                if (test1 || test2)
                {
                    return Block("Does not enter depression while range/elevation restrictions are satisfied (A6.3)");
                }

                if (EnterHexsideIsCrest() && (losIs60Degree || losIsHorizontal) && finalTarget >= finalSource && !target.Terrain!.IsBridge)
                {
                    return Block("Enters Crest Line - Depression hexside at vertex (B19.51)");
                }

                if (Failed)
                {
                    return true;
                }
            }

            return false;
        }

        // The rooftop adjustment of the depression rules: half a level lower unless it is level 1 of its hex.
        private static double RooftopHalfLevel(bool isRooftop, LocationFacts location) => isRooftop && location.Level != 1 ? -0.5 : 0.0;

        // Map.specialtestDepressionGroundLevelOnExit and OnEntry: next to the other end, the ground level between that
        // end and the depression hex's center is ignored. The end's plain elevation is used.
        private bool SpecialTestDepressionGroundLevel(int rangeToEnd, int endElevation, GridPoint endPoint)
        {
            if (rangeToEnd != 1 || endElevation <= Facts(currentHex).BaseLevel || !IsDepressionHex(currentHex))
            {
                return false;
            }

            var center = map.LosPoint(currentHex);
            return LosMap.Distance(endPoint.X, endPoint.Y, currentCol, currentRow) < LosMap.Distance(endPoint.X, endPoint.Y, center.X, center.Y);
        }

        // Map.losCrossingBridgeDepiction: bridge or road terrain at this point.
        private bool LosCrossingBridgeDepiction() => currentTerrain!.IsBridge || currentTerrain.IsRoad;

        // Map.entersByRoadHexside: the LOS entered this hex from the previous one across a road hexside.
        private bool EntersByRoadHexside()
        {
            if (losIs60Degree || losIsHorizontal || previousHex is not { } previous)
            {
                return false;
            }

            for (var side = 0; side < 6; side++)
            {
                if (Adjacent(currentHex, side) == previous && Facts(currentHex).Hexsides[side].Terrain is { IsRoad: true })
                {
                    return true;
                }
            }

            return false;
        }

        // Map.exitsByRoadHexside: the first hexside of this hex the line crosses is a road hexside.
        private bool ExitsByRoadHexside()
        {
            if (losIs60Degree || losIsHorizontal)
            {
                return false;
            }

            var crossed = map.HexsidesCrossed(currentHex, sourcePoint, targetPoint);
            return crossed.Count > 0 && Facts(currentHex).Hexsides[(int)crossed[0]].Terrain is { IsRoad: true };
        }

        // Map.losFollowsDepression, when the LOS enters a new hex: whether the hex's ground level is ignored because the
        // LOS follows the depression across a depression or water hexside. Never along a hexside.
        private bool LosFollowsDepression(bool newEqualsPreviousHex)
        {
            if (newEqualsPreviousHex || losIsHorizontal || losIs60Degree)
            {
                return false;
            }

            var finalSource = sourceElevation + RooftopHalfLevel(sourceIsRooftop, source);
            var finalTarget = targetElevation + RooftopHalfLevel(targetIsRooftop, target);
            var nearest = NearestSide(currentHex);
            if (exitsSourceDepression && finalTarget > finalSource)
            {
                // the hexside crossed nearest the point
                foreach (var side in map.HexsidesCrossed(currentHex, sourcePoint, targetPoint))
                {
                    if ((int)side == nearest && IsDepressionOrWaterSide(currentHex, (int)side))
                    {
                        // VASL's range adjustment reduces the test to a difference of one level
                        return finalTarget - finalSource >= 1;
                    }
                }
            }

            if (entersTargetDepression && finalSource > finalTarget && currentHex != targetHex)
            {
                // the hexside crossed away from the point
                foreach (var side in map.HexsidesCrossed(currentHex, sourcePoint, targetPoint))
                {
                    if ((int)side != nearest && IsDepressionOrWaterSide(currentHex, (int)side))
                    {
                        return finalSource - finalTarget >= 1;
                    }
                }
            }

            return false;
        }

        private bool IsDepressionOrWaterSide(HexIndex hex, int side) =>
            Facts(hex).Hexsides[side].DepressionTerrain is not null || Facts(hex).Hexsides[side].Terrain is { IsWater: true };

        // Map.checkBuildingRestrictionRule, without factories (refused). Its rooftop adjustment is used only by the factory
        // rooftop test (isRooftopLOSBlocked), so a rooftop in another building takes the plain rule.
        private bool CheckBuildingRestrictionRule()
        {
            if (!losLeavesBuilding && currentHex != sourceHex && currentTerrain!.IsBuildingTerrain
                && !(sourceElevation > currentTerrainHgt + groundLevel) && sourceElevation != targetElevation)
            {
                return Block("LOS must leave the building before leaving the source hex to see a location with a different elevation (A6.8 Example 2)");
            }

            return false;
        }

        // Map.checkHexsideTerrainRule, for walls, hedges, and cellars: no hillocks, entrenchments, bocage, or partial
        // orchards, which are refused.
        private bool CheckHexsideTerrainRule()
        {
            // A cellar end takes the cellar rule (O6.3) in every hex, the source and target included, and never the
            // wall and hedge rule; the source's cellar is tested first.
            var sourceAdjustment = sourceIsCellar ? 1.0 : 0.0;
            var targetAdjustment = targetIsCellar ? 1.0 : 0.0;
            if (sourceIsCellar)
            {
                return range != 1 && rangeToSource != 1 && targetElevation + targetAdjustment <= sourceElevation + sourceAdjustment
                    && Block("Unit in cellar cannot see over hexside terrain to non-adjacent target (O6.3)");
            }

            if (targetIsCellar)
            {
                return range != 1 && rangeToTarget != 1 && targetElevation + targetAdjustment >= sourceElevation + sourceAdjustment
                    && Block("Unit in cellar cannot be seen over hexside terrain by non-adjacent target (O6.3)");
            }

            var nearest = NearestSide(currentHex);
            var ignore = IsIgnorableHexsideTerrain(sourceHex, currentHex, nearest, sourceExitHexspine)
                || IsIgnorableHexsideTerrain(targetHex, currentHex, nearest, TargetEnterHexspine);
            if (!ignore && groundLevel == sourceElevation && groundLevel == targetElevation)
            {
                return Block("Intervening hexside terrain (B9.2)");
            }

            return false;
        }

        // Map.isIgnorableHexsideTerrain: whether hexside terrain at a location can be ignored for a hex the LOS starts or
        // ends in. A location is a hex and a side; None is the center location.
        private bool IsIgnorableHexsideTerrain(HexIndex h, HexIndex locationHex, int locationHexside, int losHexspine)
        {
            // ignore if the location is not a hexside
            if (locationHexside == None)
            {
                return true;
            }

            var locationHexsideTerrain = Facts(locationHex).Hexsides[locationHexside].HexsideTerrain;
            if (locationHexsideTerrain is null)
            {
                if (!currentTerrain!.IsHexsideTerrain)
                {
                    return true;
                }
            }
            else if (currentTerrain!.IsHexsideTerrain && locationHexsideTerrain.Code != currentTerrain.Code)
            {
                return true;
            }

            // too far away?
            if (Range(h, locationHex) > 2)
            {
                return false;
            }

            // always ignore if in source or target hex
            if (currentHex == targetHex || currentHex == sourceHex)
            {
                return true;
            }

            // always ignore if adjacent
            if (IsAdjacentHexside(h, locationHex, locationHexside) && locationHexsideTerrain is not null && locationHexsideTerrain.Name != PartialOrchard)
            {
                return true;
            }

            // ignore hexspines unless bocage or partial orchards
            if (IsHexspine(h, locationHex, locationHexside) && locationHexsideTerrain is not null)
            {
                return locationHexsideTerrain.Name is not (Bocage or PartialOrchard);
            }

            // ignore hexside terrain in an adjacent hex that spills into an adjacent location
            if (Range(h, locationHex) == 1 && Facts(locationHex).Hexsides[locationHexside].Terrain is not { IsHexsideTerrain: true })
            {
                return true;
            }

            // for LOS along a hexspine, check hexside terrain at the far end of the hexspine
            if (losHexspine >= 0)
            {
                // for locations two hexes away use the corresponding location in the adjacent hex
                if (Range(h, locationHex) == 2)
                {
                    var oppositeHex = Adjacent(locationHex, locationHexside);
                    var oppositeHexside = Opposite(locationHexside);
                    if (oppositeHex is not { } opposite)
                    {
                        return true;
                    }

                    if (Range(h, opposite) > 1)
                    {
                        var otherEndHex = h == sourceHex ? targetHex : sourceHex;
                        if (HexsideTerrain(opposite, oppositeHexside) is not null && Range(locationHex, otherEndHex) == 2)
                        {
                            // a third hexside means it cannot be ignored
                            if (Adjacent(locationHex, losHexspine) is not { } thirdHex)
                            {
                                return true;
                            }

                            return !(HexsideTerrain(thirdHex, Opposite(losHexspine)) is not null && Range(locationHex, otherEndHex) == 2);
                        }

                        return false;
                    }

                    locationHex = opposite;
                    locationHexside = oppositeHexside;
                }

                var hexside = losHexspine == 0 ? 5 : losHexspine - 1;
                var hexspine = losHexspine < 2 ? losHexspine + 4 : losHexspine - 2;
                if (Adjacent(h, hexside) is not { } hex1 || Adjacent(h, losHexspine) is not { } hex2)
                {
                    return false;
                }

                var t1 = HexsideTerrain(hex2, hexspine);
                var t2 = HexsideTerrain(hex1, losHexspine);
                var t3 = HexsideTerrain(hex2, hexside);
                var isL2 = locationHex == hex1 && locationHexside == losHexspine;
                var isL3 = locationHex == hex2 && locationHexside == hexside;
                return t1 is not null && (isL2 || isL3) && (t2 is null || t3 is null);
            }

            return false;
        }

        // Map.isAdjacentHexside: the location is a hexside of the hex, or the opposite side of one.
        private bool IsAdjacentHexside(HexIndex h, HexIndex locationHex, int locationHexside)
        {
            if (locationHex == h)
            {
                return true;
            }

            for (var side = 0; side < 6; side++)
            {
                if (Adjacent(h, side) is { } h2 && h2 == locationHex && Opposite(side) == locationHexside)
                {
                    return true;
                }
            }

            return false;
        }

        // Map.isHexspine: the location is a hexside of an adjacent hex that meets the hex at a vertex.
        private bool IsHexspine(HexIndex h, HexIndex locationHex, int locationHexside)
        {
            for (var side = 0; side < 6; side++)
            {
                if (Adjacent(h, side) is { } h2 && h2 == locationHex
                    && (locationHexside == (side + 2) % 6 || locationHexside == (side + 4) % 6))
                {
                    return true;
                }
            }

            return false;
        }

        // Map.checkGroundLevelRule, without railroad embankments, bridges, or Deir. A hex whose ground level the LOS may
        // ignore because it follows a depression (losFollowsDepression) does not block.
        private bool CheckGroundLevelRule()
        {
            // along a cliff hexside the lower of the two hexes counts
            if (LowerCliffHexLevel() is { } lower && lower <= Math.Max(sourceElevation, targetElevation))
            {
                return false;
            }

            if (Failed)
            {
                return true;
            }

            return groundLevel > sourceElevation + HeightAdjustment(sourceIsCellar, sourceRooftopAdjustment)
                && groundLevel > targetElevation + HeightAdjustment(targetIsCellar, targetRooftopAdjustment)
                && ignoreGroundLevelHex != currentHex
                && Block("Ground level is higher than both the source and target (A6.2)");
        }

        // The cellar (+1) and rooftop (-0.5) adjustments the ground level, terrain higher, terrain height, and blind hex
        // rules make to an end's elevation.
        private static double HeightAdjustment(bool isCellar, double rooftopAdjustment) => isCellar ? 1.0 : rooftopAdjustment;

        // The cliff hexside test of the ground level and terrain height rules: along a hexside at an odd range, when the
        // hexside the line follows is a cliff, the lower base level of its two hexes, or the current hex's when the other
        // is the source or target hex; otherwise null.
        private int? LowerCliffHexLevel()
        {
            if (!(losIs60Degree || losIsHorizontal) || rangeToSource % 2 == 0)
            {
                return null;
            }

            var side = HexsideWhenLosAlongHexside();
            if (side == None || Facts(currentHex).Hexsides[side].Terrain is not { IsCliff: true })
            {
                return null;
            }

            if (Adjacent(currentHex, side) is not { } other)
            {
                Refuse(LosUnsupportedRule.VaslFails);
                return null;
            }

            return other != sourceHex && other != targetHex
                ? Math.Min(Facts(currentHex).BaseLevel, Facts(other).BaseLevel)
                : Facts(currentHex).BaseLevel;
        }

        // Map.checkSplitTerrainRule, without slopes. Only a cellar end is adjusted.
        private bool CheckSplitTerrainRule()
        {
            var terrain = currentTerrain!;
            if (terrain.HasSplit && groundLevel == sourceElevation + (sourceIsCellar ? 1.0 : 0.0) && groundLevel == targetElevation + (targetIsCellar ? 1.0 : 0.0))
            {
                if (terrain.IsLowerLosObstacle || (!losLeavesBuilding && terrain.IsOutsideFactoryWall))
                {
                    return Block("This terrain blocks LOS to same same elevation Source and Target");
                }

                if (terrain.IsLowerLosHindrance && AddHindranceHex())
                {
                    return true;
                }
            }

            return false;
        }

        // Map.checkHalfLevelTerrainRule, without hillocks, railroad embankments, sand dunes, or slopes. A rooftop end is
        // adjusted only when the other end is not a rooftop, and a cellar end not at all.
        private bool CheckHalfLevelTerrainRule()
        {
            var terrain = currentTerrain!;
            var sourceAdjustment = targetIsRooftop ? 0.0 : sourceRooftopAdjustment;
            var targetAdjustment = sourceIsRooftop ? 0.0 : targetRooftopAdjustment;
            return terrain.IsHalfLevelHeight && !terrain.IsHexsideTerrain
                && groundLevel + currentTerrainHgt == sourceElevation + sourceAdjustment && groundLevel + currentTerrainHgt == targetElevation + targetAdjustment
                && ApplyHalfLevelTerrain();
        }

        // Map.applyHalfLevelTerrain
        private bool ApplyHalfLevelTerrain() => currentTerrain!.IsLosObstacle
            ? Block("Half level terrain is higher than both the source and/or the target (A6.2)")
            : AddHindranceHex();

        // Map.checkTerrainIsHigherRule, without railroad embankments, hillocks, sand dunes, factories, bridges, Volga
        // piers, or slopes.
        private bool CheckTerrainIsHigherRule()
        {
            var terrain = currentTerrain!;
            var obstacleAdjustment = terrain.IsHalfLevelHeight && !terrain.IsHexsideTerrain ? 0.5 : 0.0;
            var sourceHeight = sourceElevation + HeightAdjustment(sourceIsCellar, sourceRooftopAdjustment);
            var targetHeight = targetElevation + HeightAdjustment(targetIsCellar, targetRooftopAdjustment);

            // split terrain at the level of both ends has its own rule
            if (terrain.HasSplit && groundLevel == sourceHeight && groundLevel == targetHeight)
            {
                return false;
            }

            var height = groundLevel + currentTerrainHgt + obstacleAdjustment;
            if (height > sourceHeight && height > targetHeight)
            {
                var oneLevelHindrance = terrain.Name is "Orchard" or "Palm Trees" or "Light Woods";
                if ((targetHex == currentHex || sourceHex == currentHex) && oneLevelHindrance)
                {
                    // LOSH terrain in the vertex hex does not block LOS from or to the vertex
                    return false;
                }
                else if ((losIs60Degree || losIsHorizontal) && rangeToSource % 2 != 0)
                {
                    var side = HexsideWhenLosAlongHexside();
                    if (side != None && Adjacent(currentHex, side) is { } other && (other == targetHex || other == sourceHex) && oneLevelHindrance)
                    {
                        return false;
                    }
                }

                if (terrain.IsLosObstacle && !terrain.Name.Contains("Light Woods", StringComparison.Ordinal))
                {
                    return Block("Terrain is higher than both the source and target (A6.2)");
                }

                // must be a hindrance; LOS under a road above both ends is clear
                if (terrain.Name.Contains("Road", StringComparison.Ordinal) && groundLevel > sourceElevation && groundLevel > targetElevation)
                {
                    return false;
                }

                return AddHindranceHex();
            }

            return false;
        }

        // Map.checkTerrainHeightRule, without railroad embankments, out-of-season orchards, slopes, hillocks, or factories.
        // The cellar and rooftop adjustments apply to the height test only; the exceptions below
        // use the plain elevations, as VASL's do.
        private bool CheckTerrainHeightRule()
        {
            var terrain = currentTerrain!;
            var obstacleAdjustment = terrain.IsHalfLevelHeight && terrain.IsBuilding ? 0.5 : 0.0;
            var height = groundLevel + currentTerrainHgt + obstacleAdjustment;
            var sourceHeight = sourceElevation + HeightAdjustment(sourceIsCellar, sourceRooftopAdjustment);
            var targetHeight = targetElevation + HeightAdjustment(targetIsCellar, targetRooftopAdjustment);
            if (height != Math.Max(sourceHeight, targetHeight) || !(height > Math.Min(sourceHeight, targetHeight)))
            {
                return false;
            }

            var cell = (X: (double)currentCol, Y: (double)currentRow);
            var currentCenter = map.LosPoint(currentHex);

            // B10.2 EXC: ignore same level terrain in a lower adjacent hex, and the reverse
            if (rangeToSource == 1 && currentTerrainHgt + obstacleAdjustment == 0 && sourceElevation > Facts(currentHex).BaseLevel
                && LosMap.Distance(sourcePoint.X, sourcePoint.Y, cell.X, cell.Y) < LosMap.Distance(sourcePoint.X, sourcePoint.Y, currentCenter.X, currentCenter.Y))
            {
                return false;
            }

            if (rangeToTarget == 1 && currentTerrainHgt + obstacleAdjustment == 0 && targetElevation > Facts(currentHex).BaseLevel
                && LosMap.Distance(targetPoint.X, targetPoint.Y, cell.X, cell.Y) < LosMap.Distance(targetPoint.X, targetPoint.Y, currentCenter.X, currentCenter.Y))
            {
                return false;
            }

            // in-hex LOS hindrances in the source or target hex neither block nor hinder
            if ((rangeToSource == 0 || rangeToTarget == 0) && (terrain.IsLowerLosHindrance || terrain.IsLosHindrance))
            {
                return false;
            }

            // along a cliff hexside, the lower hex decides
            if (LowerCliffHexLevel() is { } lower && lower < Math.Max(sourceElevation, targetElevation))
            {
                return false;
            }

            if (Failed)
            {
                return true;
            }

            // are the depression exit and entry restrictions satisfied? Terrain obstacles in a depression hex still
            // count (B19.21).
            if ((ignoreGroundLevelHex is { } ignored && map.Locator.ExtendedBorderContains(ignored, currentCol, currentRow))
                || (entersTargetDepression && IsDepressionHex(currentHex))
                || (exitsSourceDepression && terrain.IsDepression && !(IsDepressionHex(currentHex) && currentTerrainHgt >= 1)))
            {
                return false;
            }

            // Special case: a source adjacent to a water obstacle looking at a target in it ignores the bit of open
            // ground that extends into the first water hex.
            var waterException = CenterTerrain(currentHex) is { IsWater: true } && terrain.Height < 1
                && ((rangeToSource == 1 && sourceElevation > targetElevation && CenterTerrain(targetHex) is { IsWater: true })
                    || (rangeToTarget == 1 && targetElevation > sourceElevation && CenterTerrain(sourceHex) is { IsWater: true }));
            if (waterException)
            {
                return false;
            }

            return terrain.Name is "Huts" or "Tower Hindrance" ? AddHindranceHex() : Block("Must have a height advantage to see over this terrain (A6.2)");
        }

        // Map.checkBlindHexRule, without slopes, hillocks, bocage, factories, roofless buildings, or out-of-season
        // orchards. A cliff hexside always takes the blind hex test (B10.23).
        private bool CheckBlindHexRule()
        {
            var terrain = currentTerrain!;
            var height = groundLevel + currentTerrainHgt;
            var sourceHeight = sourceElevation + HeightAdjustment(sourceIsCellar, sourceRooftopAdjustment);
            var targetHeight = targetElevation + HeightAdjustment(targetIsCellar, targetRooftopAdjustment);
            var lowerEnd = Math.Min(sourceHeight, targetHeight);
            var higherEnd = Math.Max(sourceHeight, targetHeight);
            if (!terrain.IsCliff && !(height > lowerEnd && height < higherEnd))
            {
                return false;
            }

            // Along a cliff hexside at an odd range the ground level is the lower of the two hexes', unless the other
            // hex is the source or target hex.
            int? cliffGroundLevel = null;
            var cliffAlongHexsideInThisHex = false;
            if ((losIs60Degree || losIsHorizontal) && rangeToSource % 2 != 0)
            {
                var side = HexsideWhenLosAlongHexside();
                if (side != None && Facts(currentHex).Hexsides[side].Terrain is { IsCliff: true })
                {
                    cliffAlongHexsideInThisHex = true;
                    if (Adjacent(currentHex, side) is not { } other)
                    {
                        return Refuse(LosUnsupportedRule.VaslFails);
                    }

                    if (other != sourceHex && other != targetHex)
                    {
                        cliffGroundLevel = Math.Min(Facts(currentHex).BaseLevel, Facts(other).BaseLevel);
                    }
                }
            }

            // A cliff on the hexside by which the LOS leaves the source, seen from the hex across it, is not a cliff
            // hexside here.
            var isCliffHexside = false;
            if (terrain.IsCliff)
            {
                isCliffHexside = true;
                foreach (var exitSide in sourceExitHexsides)
                {
                    if (VaslAdjacent(sourceHex, exitSide) is not { } adjacent)
                    {
                        return Refuse(LosUnsupportedRule.VaslFails);
                    }

                    if (adjacent != currentHex)
                    {
                        continue;
                    }

                    var opposite = LocationSide(Opposite(exitSide));
                    var edge = map.EdgePoint(currentHex, (HexsideDirection)opposite);
                    var center = map.LosPoint(currentHex);
                    if (Facts(currentHex).Hexsides[opposite].Terrain is { IsCliff: true }
                        && LosMap.Distance(edge.X, edge.Y, currentCol, currentRow) < LosMap.Distance(center.X, center.Y, currentCol, currentRow))
                    {
                        isCliffHexside = false;
                        break;
                    }
                }
            }

            if (!IsBlindHex(currentTerrainHgt, isCliffHexside, cliffGroundLevel))
            {
                return false;
            }

            if (terrain.IsLosObstacle && !terrain.IsHexsideTerrain)
            {
                // an inherent obstacle that is not the hex's own terrain does not block here
                if (terrain.IsInherent && CenterTerrain(currentHex)?.Code != terrain.Code)
                {
                    return false;
                }

                if (!((losIs60Degree || losIsHorizontal) && terrain.IsBuilding) || rangeToTarget % 2 == 0)
                {
                    return Block("Source or Target location is in a blind hex (A6.4)");
                }

                // Along a hexside at an odd range the hex the line touches decides; VASL resets the result first.
                blocked = false;
                reason = string.Empty;
                resultBlocked = false;
                blockedAtPoint = null;
                resultReason = string.Empty;
                var first = (sourceExitHexspine + 1) % 6;
                var second = (sourceExitHexspine + 4) % 6;
                var touched = NearestSide(currentHex);
                if (touched != first && touched != second)
                {
                    return false;
                }

                if (Adjacent(currentHex, touched) is not { } testHex)
                {
                    return Refuse(LosUnsupportedRule.VaslFails);
                }

                return CenterTerrain(testHex) is { IsRoofless: true }
                    ? Refuse(LosUnsupportedRule.Factory)
                    : Block("Source or Target location is in a blind hex (A6.4)");
            }

            // see if a cliff hexside, or ground level alone, creates a blind hex
            if ((isCliffHexside && !cliffAlongHexsideInThisHex)
                || (groundLevel > lowerEnd && groundLevel < higherEnd && IsBlindHex(0, false, null)))
            {
                return Block("Source or Target location is in a blind hex (B10.23)");
            }

            // a hindrance creates a "blind hex", if not the target or source hex; a cliff and open ground do not
            if (currentHex != targetHex && currentHex != sourceHex && !isCliffHexside && terrain.Category != LosCategory.Open)
            {
                return AddHindranceHex();
            }

            return false;
        }

        // Map.isBlindHex, without slopes, hillocks, bocage, or rowhouse walls. Cellars are not adjusted here. A cliff
        // hexside crossed by the line counts from the top of the cliff and makes one blind hex fewer; along a cliff
        // hexside the ground level may be the lower hex's (cliffGroundLevel).
        private bool IsBlindHex(int terrainHeight, bool isCliffHexside = false, int? cliffGroundLevel = null)
        {
            double higher = sourceElevation;
            double lower = targetElevation;
            double rangeFromHigher = rangeToSource;
            double rangeToLower = rangeToTarget;
            var higherHex = sourceHex;
            var lowerHex = targetHex;
            var ground = groundLevel;

            // a half-level building counts half a level higher when an end is a rooftop
            var rooftopEnd = sourceRooftopAdjustment != 0.0 || targetRooftopAdjustment != 0.0;
            var terrainHeightAdjustment = rooftopEnd && currentTerrain!.IsBuilding && currentTerrain.IsHalfLevelHeight ? 0.5 : 0.0;

            // blind hex NA for same-level LOS
            if (higher == lower)
            {
                return false;
            }

            // the rooftop adjustment comes after the same-level test
            higher += sourceRooftopAdjustment;
            lower += targetRooftopAdjustment;

            // if LOS rising, swap source and target and use the same logic as LOS falling
            if (higher < lower)
            {
                (higher, lower) = (lower, higher);
                (rangeFromHigher, rangeToLower) = (rangeToLower, rangeFromHigher);
                (higherHex, lowerHex) = (lowerHex, higherHex);
            }

            // round the higher elevation down to a full level only if more than one level above the obstacle (A6.42)
            if (higher - (ground + terrainHeight) > 1)
            {
                higher = Math.Floor(higher);
            }

            if (isCliffHexside && !(losIs60Degree || losIsHorizontal))
            {
                // VASL's fix for cliff artwork: a cliff in a depression hex other than the target is ignored
                if (IsDepressionHex(currentHex) && currentHex != targetHex)
                {
                    return false;
                }

                // a cliff in the hex next to an end that was already tested (near the higher end, far from the lower)
                var nearest = NearestSide(currentHex);
                var nearestTerrain = nearest == None ? CenterTerrain(currentHex) : Facts(currentHex).Hexsides[nearest].Terrain;
                var nearestIsCliff = nearestTerrain is { IsCliff: true };
                var currentCenter = map.LosPoint(currentHex);
                var higherPoint = map.LosPoint(higherHex);
                var lowerPoint = map.LosPoint(lowerHex);
                if (rangeFromHigher == 1 && higher > terrainHeight && nearestIsCliff
                    && LosMap.Distance(higherPoint.X, higherPoint.Y, currentCol, currentRow) < LosMap.Distance(higherPoint.X, higherPoint.Y, currentCenter.X, currentCenter.Y))
                {
                    return false;
                }

                if (rangeToLower == 1 && higher > terrainHeight && nearestIsCliff
                    && LosMap.Distance(lowerPoint.X, lowerPoint.Y, currentCol, currentRow) > LosMap.Distance(lowerPoint.X, lowerPoint.Y, currentCenter.X, currentCenter.Y))
                {
                    return false;
                }

                // no blind hex when one end is above the cliff and the other at least level with its top
                var top = Facts(currentHex).BaseLevel;
                if ((higher >= top && lower > top) || (higher > top && lower >= top))
                {
                    return false;
                }

                // the ground level is the top of the cliff
                if (top > groundLevel)
                {
                    ground = top;
                }
            }
            else if (isCliffHexside && cliffGroundLevel is { } lowerHexLevel)
            {
                ground = lowerHexLevel;
            }

            if ((terrainHeight == 0 && !isCliffHexside) || (isCliffHexside && currentHex == sourceHex))
            {
                // EXC: non-cliff crest line
                var depressionAdjustment = 0;
                var crossed = map.HexsidesCrossed(currentHex, sourcePoint, targetPoint);
                if (crossed.Count > 0 && Facts(currentHex).Hexsides[(int)crossed[0]].DepressionTerrain is not null)
                {
                    depressionAdjustment = -1;
                }

                return rangeToLower <= Math.Max((2 * (ground + depressionAdjustment + terrainHeight + terrainHeightAdjustment)) + (rangeFromHigher / 5) - higher - lower, 0);
            }

            // Map.getBlind; a cliff makes one blind hex fewer
            var cliffAdjustment = isCliffHexside ? -1 : 0;
            return rangeToLower <= Math.Max((2 * (ground + terrainHeight + terrainHeightAdjustment)) + (rangeFromHigher / 5) - higher - lower + 1 + cliffAdjustment, 1 + 0);
        }

        // Map.addHindranceHex: a hindrance between the source and target, the largest at each range.
        private bool AddHindranceHex()
        {
            if (currentHex == sourceHex || currentHex == targetHex)
            {
                return false;
            }

            var total = Range(sourceHex, targetHex);
            if (!(Range(sourceHex, currentHex) < total && Range(targetHex, currentHex) < total))
            {
                return false;
            }

            var terrain = currentTerrain!;
            var name = terrain.Name;
            var lighter = name.Contains("Rice Paddy, In Season", StringComparison.Ordinal) || name.Contains("Rice Paddy Bank", StringComparison.Ordinal)
                || name.Contains("Light Grain", StringComparison.Ordinal);
            var hindrance = 1.0;
            if ((losIs60Degree || losIsHorizontal) && rangeToSource % 2 != 0)
            {
                // Along a hexside only the hexside the line touches counts; roofless factory debris is refused.
                if (terrain.IsRoofless)
                {
                    return Refuse(LosUnsupportedRule.Factory);
                }

                var first = (sourceExitHexspine + 1) % 6;
                var second = (sourceExitHexspine + 4) % 6;
                var touched = NearestSide(currentHex);
                if (touched != first && touched != second)
                {
                    // handles situations where a pixel is part of the wrong hexside
                    return false;
                }

                if (Adjacent(currentHex, touched) is { } testHex)
                {
                    if (CenterTerrain(testHex) is { IsRoofless: true })
                    {
                        return Refuse(LosUnsupportedRule.Factory);
                    }

                    if (name.Contains("Light Woods", StringComparison.Ordinal))
                    {
                        hindrance = 2;
                    }
                    else if (lighter)
                    {
                        hindrance = 0.5;
                    }
                }
            }
            else if (name.Contains("Light Woods", StringComparison.Ordinal))
            {
                hindrance = 2;
            }
            else if (lighter)
            {
                hindrance = 0.5;
            }
            else if (terrain.IsRoofless)
            {
                return Refuse(LosUnsupportedRule.Factory);
            }

            // LOSResult.addMapHindrance: keyed by range from the source hex, the larger value kept
            var atRange = Range(sourceHex, currentHex);
            if (!mapHindrances.TryGetValue(atRange, out var existing) || hindrance > existing)
            {
                mapHindrances[atRange] = hindrance;
            }

            // LOSResult.setBlockedByHindrance
            if (Hindrance() > 5)
            {
                SetBlocked(currentCol, currentRow, "Hindrance total of six or more (B.10)");
            }

            return resultBlocked;
        }

        // Map.inherentspilltest: inherent terrain in a half hex that is only a spill from the adjacent hex.
        private bool InherentSpillTest()
        {
            ReadOnlySpan<int> shiftCol = [0, 10, 10, 0, -10, -10];
            ReadOnlySpan<int> shiftRow = [-10, -10, 10, 10, 10, -10];
            for (var index = 0; index < 6; index++)
            {
                var x = currentCol + shiftCol[index];
                var y = currentRow + shiftRow[index];
                if (map.Locator.BorderContains(currentHex, x, y) && map.Grid.TryGetCode(x, y, out var code) && code == currentTerrain!.Code)
                {
                    return false;
                }
            }

            return true;
        }

        // LOSStatus.getExitFromCenterHexsides
        private int[] ExitFromCenterHexsides()
        {
            var slope = (double)(sourceY - targetY) / (sourceX - targetX);
            int[] exitHexsides = [None, None];
            if (double.IsPositiveInfinity(slope))
            {
                exitHexsides[0] = 0;
            }
            else if (double.IsNegativeInfinity(slope))
            {
                exitHexsides[0] = 3;
            }
            else if (losIsHorizontal)
            {
                (exitHexsides[0], exitHexsides[1]) = colDir == 1 ? (1, 2) : (4, 5);
            }
            else if (losIs60Degree)
            {
                (exitHexsides[0], exitHexsides[1]) = colDir == 1 ? (slope > 0 ? (2, 3) : (0, 1)) : (slope > 0 ? (5, 0) : (3, 4));
            }
            else
            {
                var crossed = map.HexsidesCrossed(sourceHex, sourcePoint, targetPoint);
                if (crossed.Count > 0)
                {
                    exitHexsides[0] = (int)crossed[0];
                }
            }

            return exitHexsides;
        }

        // Map.getHexsideWhenLOSAlongHexside, with its side effect of filling in a missing target entry hexside.
        private int HexsideWhenLosAlongHexside()
        {
            if (targetEnterHexsides[0] == None)
            {
                targetEnterHexsides[0] = FindMissingTargetEnterHexside(0);
            }

            if (targetEnterHexsides[1] == None)
            {
                targetEnterHexsides[1] = FindMissingTargetEnterHexside(1);
            }

            var pair = (targetEnterHexsides[0], targetEnterHexsides[1]);
            (int First, int Second)? sides = pair switch
            {
                (3, 4) or (4, 3) => (2, 5),
                (4, 5) or (5, 4) => (3, 0),
                (5, 0) or (0, 5) => (4, 1),
                (0, 1) or (1, 0) => (5, 2),
                (1, 2) or (2, 1) => (0, 3),
                (2, 3) or (3, 2) => (1, 4),
                _ => null,
            };
            return sides is { } found
                ? (int)map.NearestHexside(currentHex, currentCol, currentRow, (HexsideDirection)found.First, (HexsideDirection)found.Second)
                : None;
        }

        // Map.findMissingTargetEnterHexside
        private int FindMissingTargetEnterHexside(int missingSide)
        {
            var (known, match, find) = missingSide == 0 ? (targetEnterHexsides[1], sourceExitHexsides[1], sourceExitHexsides[0]) : (targetEnterHexsides[0], sourceExitHexsides[0], sourceExitHexsides[1]);
            var matchSide = match + 3;
            if (matchSide > 5)
            {
                matchSide -= 6;
            }

            if (matchSide != known)
            {
                return matchSide;
            }

            var findSide = find + 3;
            return findSide > 5 ? findSide - 6 : findSide;
        }

        private bool Block(string why)
        {
            reason = why;
            blocked = true;
            SetBlocked(currentCol, currentRow, why);
            return true;
        }

        private void SetBlocked(int x, int y, string why)
        {
            resultBlocked = true;
            blockedAtPoint = new GridPoint(x, y);
            resultReason = why;
        }

        private bool Refuse(string rule)
        {
            unsupported ??= rule;
            return true;
        }

        private bool Failed => unsupported is not null;

        private int Hindrance() => (int)Math.Floor(mapHindrances.Values.Sum());

        private bool IsDepressionHex(HexIndex hex) => map.FactsOf(hex).Center.DepressionTerrain is not null;

        // Map.getAdjacentHex, which gives the hex itself for no hexside.
        private HexIndex? VaslAdjacent(HexIndex hex, int side) => side == None ? hex : Adjacent(hex, side);

        // Hex.getHexsideLocation, which gives hexside 0 for no hexside.
        private static int LocationSide(int side) => side == None ? 0 : side;

        private HexFacts Facts(HexIndex hex) => map.FactsOf(hex);

        private TerrainType? CenterTerrain(HexIndex hex) => map.FactsOf(hex).Center.Terrain;

        private TerrainType? HexsideTerrain(HexIndex hex, int side) => map.FactsOf(hex).Hexsides[side].HexsideTerrain;

        private HexIndex? Adjacent(HexIndex hex, int side) => side is >= 0 and <= 5 ? map.Geometry.Neighbor(hex, (HexsideDirection)side) : null;

        private int Range(HexIndex from, HexIndex to) => map.Geometry.Distance(from, to);

        private int NearestSide(HexIndex hex) => map.NearestLocation(hex, currentCol, currentRow) is { } side ? (int)side : None;

        // Hex.getOppositeHexside
        private static int Opposite(int side) => side is >= 0 and <= 5 ? (side + 3) % 6 : None;
    }
}
