using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Los;

/// <summary>
/// A read-only LOS check between two locations (LOS Design, sections 3 and 5; LOS Result Design, sections 2 and 3),
/// reproducing VASL's <c>VASL.LOS.Map.Map.LOS</c> for a map without counters, overlays, or night: the same walk over
/// the terrain grid, the same rules in the same order, and the same result, with VASL's hindrance breakdown. Rules this
/// step does not reproduce make the result unsupported, with the rule's name, at the point where VASL would apply them;
/// nothing falls back to a simpler rule.
/// </summary>
/// <remarks>
/// A location is one of a hex's center locations (its up and down chain, at the hex center), or one of its hexside
/// locations, as <c>Hex.createLocations</c> makes them: level 0, the LOS point at the hexside's first vertex (clockwise
/// from the top-left one), the auxiliary point at the next, the hexside's terrain and depression terrain. A location
/// whose board or hex is not on the map, whose hex another board owns, whose level is not in the hex's location chain,
/// or a hexside location not at level 0, is refused with an <see cref="ArgumentException"/>.
/// </remarks>
public static class LosCalculator
{
    /// <summary>Checks LOS from the source location to the target location on the map, from and to their LOS points.</summary>
    public static LosResult Check(LosMap map, BoardLocation source, BoardLocation target) =>
        Check(map, source, LosAim.LosPoint, target, LosAim.LosPoint);

    /// <summary>
    /// Checks LOS from the source location to the target location on the map, with an aim for each end: a hexside
    /// location's LOS point or auxiliary point (<c>Map.LOS</c>'s <c>useAuxSourceLOSPoint</c> and
    /// <c>useAuxTargetLOSPoint</c>). A center location has one point, so its aim changes nothing.
    /// </summary>
    public static LosResult Check(LosMap map, BoardLocation source, LosAim sourceAim, BoardLocation target, LosAim targetAim)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        var sourceEnd = Resolve(map, source, sourceAim, nameof(source));
        var targetEnd = Resolve(map, target, targetAim, nameof(target));
        var result = new Walk(map, sourceEnd, targetEnd).Run();
        return result.Status is LosStatus.Clear or LosStatus.Blocked && !map.IsDefinitive
            ? result with
            {
                Status = LosStatus.Nondefinitive
            }
            : result;
    }

    private static LosEnd Resolve(LosMap map, BoardLocation location, LosAim aim, string parameter)
    {
        if (map.Locate(location.Board, location.Hex) is not { } hex)
        {
            throw new ArgumentException($"{location.Board}:{location.Hex} is not on the map.", parameter);
        }

        if (map.OwnerOf(hex) is { } owner && owner != (location.Board, location.Hex))
        {
            throw new ArgumentException($"{location.Board}:{location.Hex} is a hex shared with {owner.Board}, which owns it as {owner.Board}:{owner.Hex}.", parameter);
        }

        var facts = map.FactsOf(hex);
        if (location.Side is { } side)
        {
            // Hex.createLocations: a hexside location is at level 0, with the hexside's terrain and depression terrain
            // (Hex.resetTerrain and resetHexsideTerrain), its LOS point at vertex side and its auxiliary point at the next,
            // truncated to integers.
            if (location.Level != 0)
            {
                throw new ArgumentException($"{location} is not a hexside location: hexside locations are at level 0.", parameter);
            }

            var hexside = facts.Hexsides[(int)side];
            var vertices = map.Geometry.Vertices(hex);
            var losVertex = vertices[(int)side];
            var auxVertex = vertices[((int)side + 1) % 6];
            var losPoint = new GridPoint((int)losVertex.X, (int)losVertex.Y);
            var auxPoint = new GridPoint((int)auxVertex.X, (int)auxVertex.Y);
            return new LosEnd(hex, new LocationFacts(0, hexside.Terrain, hexside.DepressionTerrain), (int)side, aim == LosAim.AuxiliaryPoint ? auxPoint : losPoint,
                losPoint, auxPoint);
        }

        var center = map.LosPoint(hex);
        return facts.Locations.FirstOrDefault(level => level.Level == location.Level) is { } found
            ? new LosEnd(hex, found, LosEnd.Center, center, center, center)
            : throw new ArgumentException(
                $"{location} is not in the hex's location chain ({string.Join(", ", facts.Locations.Select(level => level.Level))}).", parameter);
    }

    /// <summary>
    /// One end of an LOS check, as VASL's <c>Location</c> gives it: its hex, its level and terrain, its hexside
    /// (<see cref="Center"/> for a center location), the point the line starts or ends at, and its LOS and auxiliary
    /// points.
    /// </summary>
    private readonly record struct LosEnd(HexIndex Hex, LocationFacts Location, int Side, GridPoint Point, GridPoint LosPoint, GridPoint AuxPoint)
    {
        public const int Center = -1;

        // Location.isCenterLocation: the center location or one above or below it.
        public bool IsCenter => Side == Center;
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
        private const string HillockSummit = "Hillock Summit";
        private const string OrchardOutOfSeason = "Orchard, Out of Season";
        private const string StoneRubble = "Stone Rubble";
        private const string WoodenRubble = "Wooden Rubble";

        // Bridge.getShape and getRoadShape, for the single-hex bridges Hex.fixBridgesTunnelWater makes: a 32 by 48 pixel
        // rectangle at the hex center, unrotated, and its road 9 pixels in from each long side.
        private const int BridgeWidth = 32;
        private const int BridgeHeight = 48;
        private const int BridgeRoadInset = 9;

        private readonly LosMap map;
        private readonly HexIndex sourceHex;
        private readonly HexIndex targetHex;
        private readonly LocationFacts source;
        private readonly LocationFacts target;
        private readonly LosEnd sourceEnd;
        private readonly LosEnd targetEnd;
        private readonly bool sourceIsCenter;
        private readonly bool targetIsCenter;

        // The points the line runs between (LOSStatus.sourceX, sourceY, targetX, and targetY): an end's LOS point, or a
        // hexside location's auxiliary point when aimed there.
        private readonly GridPoint sourcePoint;
        private readonly GridPoint targetPoint;

        // The ends' hex centers (Hex.getHexCenter), which some rules measure from instead.
        private readonly GridPoint sourceCenter;
        private readonly GridPoint targetCenter;
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
        private readonly SortedDictionary<int, double> mapHindrances = [];
        private readonly bool exitsSourceDepression;
        private readonly bool entersTargetDepression;

        // LOSStatus.slopes: the slope rules (F2.3) apply, since the higher end is up-slope.
        private readonly bool slopes;

        // LOSStatus's hillock state (F6.4). Hillocks are LosMap.HillockOf's numbers. The hillock rule clears the two
        // flags while it tests a wall or hedge with the hexside rule.
        private readonly HashSet<int> crossedHillocks = [];
        private readonly int? sourceAdjacentHillock;
        private readonly int? targetAdjacentHillock;
        private bool startsOnHillock;
        private bool endsOnHillock;
        private int? crossingHillock;
        private (HexIndex Hex, int Side)? firstWallCrossed;
        private GridPoint firstWallPoint;
        private HexIndex? firstRubbleCrossed;
        private HexIndex? firstHalfLevelHindrance;

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
        private GridPoint? firstHindranceAt;

        private string? unsupported;

        // LOSStatus constructor
        public Walk(LosMap map, LosEnd sourceEnd, LosEnd targetEnd)
        {
            this.map = map;
            this.sourceEnd = sourceEnd;
            this.targetEnd = targetEnd;
            sourceHex = sourceEnd.Hex;
            targetHex = targetEnd.Hex;
            source = sourceEnd.Location;
            target = targetEnd.Location;
            sourceIsCenter = sourceEnd.IsCenter;
            targetIsCenter = targetEnd.IsCenter;
            sourcePoint = sourceEnd.Point;
            targetPoint = targetEnd.Point;
            sourceCenter = map.LosPoint(sourceHex);
            targetCenter = map.LosPoint(targetHex);
            sourceX = sourcePoint.X;
            sourceY = sourcePoint.Y;
            targetX = targetPoint.X;
            targetY = targetPoint.Y;
            colDir = targetX - sourceX < 0 ? -1 : 1;
            rowDir = targetY - sourceY < 0 ? -1 : 1;
            numCols = Math.Abs(targetX - sourceX) + 1;
            currentHex = sourceHex;
            tempHex = sourceHex;

            // setSourceAndTargetElevations: a hexside location in a depression hex is a level higher ("code to fix
            // vertex los").
            sourceElevation = Facts(sourceHex).BaseLevel + source.Level + (!sourceIsCenter && IsDepressionHex(sourceHex) ? 1 : 0);
            targetElevation = Facts(targetHex).BaseLevel + target.Level + (!targetIsCenter && IsDepressionHex(targetHex) ? 1 : 0);

            // The rules below count a cellar one level higher, and a rooftop half a level lower unless it is level 1 of
            // its hex, by the end's terrain.
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

            // setEnterExitHexsides: for a center end, the hexsides the line leaves the source by, and for the target their
            // opposites; for a hexside end, the hexsides of its hex the line touches.
            var exits = sourceIsCenter ? ExitFromCenterHexsides() : HexsidesTouched(sourceHex);
            sourceExitHexsides[0] = exits[0];
            sourceExitHexsides[1] = exits[1];
            if (targetIsCenter)
            {
                exits = ExitFromCenterHexsides();
                targetEnterHexsides[0] = Opposite(exits[0]);
                targetEnterHexsides[1] = Opposite(exits[1]);
            }
            else
            {
                exits = HexsidesTouched(targetHex);
                targetEnterHexsides[0] = exits[0];
                targetEnterHexsides[1] = exits[1];
            }

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

            // slope rules are in effect if the higher location is up-slope; an up-slope source counts as on a hillock
            slopes = (ExitsSlopeHexside() && sourceElevation >= targetElevation) || (EntersSlopeHexside() && targetElevation >= sourceElevation);
            startsOnHillock = source.Terrain?.Name == Hillock || slopes;
            endsOnHillock = target.Terrain?.Name == Hillock;
            if (startsOnHillock)
            {
                crossingHillock = map.HillockOf(sourceHex);
            }

            // setAdjacentToHillock: the hillock across the source's exit hexsides, or the target's entry hexsides (the
            // second hexside wins)
            if (!startsOnHillock)
            {
                sourceAdjacentHillock = AdjacentHillock(sourceHex, sourceExitHexsides);
            }

            if (!endsOnHillock)
            {
                targetAdjacentHillock = AdjacentHillock(targetHex, targetEnterHexsides);
            }
        }

        private int TargetEnterHexspine => losIs60Degree
            ? (colDir == 1 ? (rowDir == 1 ? 0 : 4) : (rowDir == 1 ? 1 : 3))
            : sourceExitHexspine == None ? None : (colDir == 1 ? 5 : 2);

        public LosResult Run()
        {
            // Map.LOS: the same location, whichever point each end is aimed at (never asked by the oracle, but VASL
            // answers it).
            if (sourceHex == targetHex && source.Level == target.Level && sourceEnd.Side == targetEnd.Side)
            {
                return new LosResult(LosStatus.Clear, false, 0, 0, null, string.Empty);
            }

            // Map.checkSameHexRule decides when either end is a center location; between two hexside locations of one
            // hex the walk goes on, at range 0.
            if (sourceHex == targetHex && (sourceIsCenter || targetIsCenter))
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
            var breakdown = mapHindrances.Select(entry => new LosHindrance(entry.Key, entry.Value)).ToArray();
            LosHindranceAt? first = null;
            if (firstHindranceAt is { } firstPoint)
            {
                var firstOwner = map.Locator.GridToHex(firstPoint.X, firstPoint.Y) is { } firstHex ? map.OwnerOf(firstHex) : null;
                first = new LosHindranceAt(firstPoint, firstOwner?.Board, firstOwner?.Hex);
            }

            if (!resultBlocked)
            {
                return new LosResult(LosStatus.Clear, false, resultRange, hindrance, null, string.Empty) { Hindrances = breakdown, FirstHindranceAt = first };
            }

            var point = blockedAtPoint!.Value;
            var hex = map.Locator.GridToHex(point.X, point.Y);
            var owner = hex is { } index ? map.OwnerOf(index) : null;
            return new LosResult(LosStatus.Blocked, true, resultRange, hindrance, new LosBlockedAt(point, owner?.Board, owner?.Hex), resultReason)
            {
                Hindrances = breakdown,
                FirstHindranceAt = first,
            };
        }

        // Map.checkSameHexRule, for one hex with a center location at either end: building levels, then a bridge at an
        // end with a center location at the other. Tunnels are not reproduced.
        private LosResult CheckSameHexRule()
        {
            if (source.Terrain is not { } sourceTerrain || target.Terrain is not { } targetTerrain)
            {
                return LosResult.Unsupported(0, LosUnsupportedRule.MissingLocationTerrain);
            }

            if (sourceTerrain.IsTunnel || targetTerrain.IsTunnel)
            {
                return LosResult.Unsupported(0, LosUnsupportedRule.Tunnel);
            }

            if (sourceTerrain.IsBuildingTerrain && targetTerrain.IsBuildingTerrain
                && (Math.Abs(source.Level - target.Level) > 1 || !Facts(sourceHex).Stairway))
            {
                return SameHexBlocked("Crosses building level or no stairway");
            }

            return (sourceTerrain.IsBridge && targetIsCenter) || (targetTerrain.IsBridge && sourceIsCenter)
                ? SameHexBlocked("Cannot see location under the bridge")
                : new LosResult(LosStatus.Clear, false, 0, 0, null, string.Empty);
        }

        // The blocked point is the source's LOS point, whichever point the source is aimed at.
        private LosResult SameHexBlocked(string why)
        {
            var point = sourceEnd.LosPoint;
            var hex = map.Locator.GridToHex(point.X, point.Y);
            var owner = hex is { } index ? map.OwnerOf(index) : null;
            return new LosResult(LosStatus.Blocked, true, 0, 0, new LosBlockedAt(point, owner?.Board, owner?.Hex), why);
        }

        // The parts of the LOSStatus constructor this step does not reproduce: special source and target locations.
        // Cellars, rooftops, factory and bridge locations (the depression location under a bridge among them), hillocks,
        // slopes, and depression ends are reproduced.
        private string? SetupProblem()
        {
            foreach (var location in new[] { source, target })
            {
                if (location.Terrain is not { } terrain)
                {
                    return LosUnsupportedRule.MissingLocationTerrain;
                }

                if (terrain.IsTunnel)
                {
                    return LosUnsupportedRule.Tunnel;
                }

                if (terrain.IsRoofless)
                {
                    return LosUnsupportedRule.Roofless;
                }

                if (terrain.IsEntrenchment)
                {
                    return LosUnsupportedRule.Entrenchment;
                }

                if (terrain.Name.Contains("Railroad, Embankment", StringComparison.Ordinal))
                {
                    return LosUnsupportedRule.RailroadEmbankment;
                }
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

        private int? AdjacentHillock(HexIndex hex, int[] sides)
        {
            int? hillock = null;
            foreach (var side in sides)
            {
                if (side != None && Adjacent(hex, side) is { } adjacent && CenterTerrain(adjacent)?.Name == Hillock)
                {
                    hillock = map.HillockOf(adjacent);
                }
            }

            return hillock;
        }

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
            // Only the hex's own center location, not its upper levels or a hexside location, takes the even-range
            // shortcut.
            var sourceIsHexCenter = sourceIsCenter && source.Level == Facts(sourceHex).Center.Level;
            if (map.Locator.ExtendedBorderContains(sourceHex, currentCol, currentRow)
                || map.Locator.ExtendedBorderContains(targetHex, currentCol, currentRow)
                || (sourceIsHexCenter && map.Geometry.Distance(sourceHex, rangeHex) % 2 == 0))
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

            // off the grid VASL reads the missing terrain here and fails
            if (currentTerrain is null)
            {
                return Refuse(map.Grid.TryGetCode(currentCol, currentRow, out _) ? LosUnsupportedRule.UnknownTerrain : LosUnsupportedRule.VaslFails);
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
            // Off the grid, as the LOS or auxiliary point of a hexside location of a half hex on the map edge is,
            // Map.getGridTerrain gives no terrain, and applyLOSRules' catch of the failure to read its height ends the
            // LOS there with the result so far: clear, at the full range.
            if (currentTerrain is null)
            {
                return map.Grid.TryGetCode(currentCol, currentRow, out _) ? Refuse(LosUnsupportedRule.UnknownTerrain) : true;
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

                SetHillockStatus();
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

            // The current hex is checked only between the source and target, or at the full range from an end that is a
            // hexside location. Test 3 needs the current hex to be the source hex, which test 1 excludes, so it never holds.
            var test1 = currentHex != sourceHex && (range != rangeToTarget || !sourceIsCenter);
            var test2 = currentHex != targetHex && (range != rangeToSource || !targetIsCenter);
            var test4 = currentHex == targetHex && !currentTerrain.IsOpen && !currentTerrain.IsHexsideTerrain && !targetIsCenter;
            var test5 = range == rangeToSource && (losIs60Degree || losIsHorizontal) && !currentTerrain.IsOpen && !currentTerrain.IsHexsideTerrain
                && !targetIsCenter;
            if (test1 && (test2 || test4 || test5))
            {
                // ignore inherent terrain that "spills" into an adjacent hex
                if (currentTerrain.IsInherent && CenterTerrain(currentHex)?.Code != currentTerrain.Code && InherentSpillTest())
                {
                    return false;
                }

                if (CheckBridgeHindranceRule() || CheckGroundLevelRule() || CheckSplitTerrainRule() || CheckHalfLevelTerrainRule() || CheckTerrainIsHigherRule()
                    || CheckTerrainHeightRule() || CheckBlindHexRule() || CheckHillockRule())
                {
                    return true;
                }
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
            // Bridges, factories, rowhouse walls, rubble, bocage, hillocks, and out-of-season orchards take the rules
            // below. Rooftop and cellar terrain met along the line has no rule of its own in VASL; only the ends' are
            // adjusted.
            var terrain = currentTerrain!;
            var name = terrain.Name;
            if (terrain.IsRoofless)
            {
                return LosUnsupportedRule.Roofless;
            }

            if (terrain.IsTunnel)
            {
                return LosUnsupportedRule.Tunnel;
            }

            if (terrain.IsEntrenchment)
            {
                return LosUnsupportedRule.Entrenchment;
            }

            if (name.Contains(PartialOrchard, StringComparison.Ordinal))
            {
                return LosUnsupportedRule.PartialOrchard;
            }

            if (name.Contains("Railroad, Embankment", StringComparison.Ordinal) || name.Contains("Rrembankment", StringComparison.Ordinal))
            {
                return LosUnsupportedRule.RailroadEmbankment;
            }

            if (name.Contains("Deir", StringComparison.Ordinal))
            {
                return LosUnsupportedRule.Deir;
            }

            if (name.Contains("Sand Dune", StringComparison.Ordinal) || name.Contains("Dune, Crest", StringComparison.Ordinal))
            {
                return LosUnsupportedRule.SandDune;
            }

            if (name.Contains("Volga Pier", StringComparison.Ordinal))
            {
                return LosUnsupportedRule.VolgaPier;
            }

            return Facts(currentHex).Center.Terrain is null ? LosUnsupportedRule.MissingLocationTerrain : null;
        }

        // Map.checkDepressionRule: the depression exit and entry restrictions (A6.3), checked in every hex, and the crest
        // line at a vertex (B19.51). VASL counts a hex whose center is a bridge as a depression hex a level lower; no
        // fixture reaches that, so it is refused.
        private bool CheckDepressionRule()
        {
            if ((exitsSourceDepression || entersTargetDepression) && CenterTerrain(currentHex) is { IsBridge: true })
            {
                return Refuse(LosUnsupportedRule.BridgeInDepression);
            }

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

                    if (SpecialTestDepressionGroundLevel(rangeToTarget, targetElevation, targetCenter))
                    {
                        test1 = false;
                    }
                }

                var test2 = !test1 && rangeToTarget == 1
                    && currentIsDepression && !(groundLevel == currentBase || finalSource >= groundLevel || finalTarget > groundLevel) && !followsDepiction
                    && LosMap.Distance(targetCenter.X, targetCenter.Y, cell.X, cell.Y) > LosMap.Distance(targetCenter.X, targetCenter.Y, currentCenter.X, currentCenter.Y);
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

                    if (SpecialTestDepressionGroundLevel(rangeToSource, sourceElevation, sourceCenter))
                    {
                        test1 = false;
                    }
                }

                var test2 = !test1 && rangeToSource == 1
                    && currentIsDepression && !(groundLevel == currentBase || finalTarget >= groundLevel || finalSource > groundLevel) && !followsDepiction
                    && LosMap.Distance(sourceCenter.X, sourceCenter.Y, cell.X, cell.Y) > LosMap.Distance(sourceCenter.X, sourceCenter.Y, currentCenter.X, currentCenter.Y);
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

        // Map.checkBuildingRestrictionRule: a building (A6.8), or a factory the LOS has not left when an end is in the
        // factory, which takes the factory rooftop test (isRooftopLOSBlocked) with VASL's rooftop adjustment of -1.
        private bool CheckBuildingRestrictionRule()
        {
            if (losLeavesBuilding || currentHex == sourceHex)
            {
                return false;
            }

            if (currentTerrain!.IsBuildingTerrain)
            {
                return !(sourceElevation > currentTerrainHgt + groundLevel) && sourceElevation != targetElevation
                    && Block("LOS must leave the building before leaving the source hex to see a location with a different elevation (A6.8 Example 2)");
            }

            // same-hex LOS, between two hexside locations, is not tested
            return (source.Terrain!.IsFactory || target.Terrain!.IsFactory) && sourceHex != targetHex && FactoryRooftopRule(RooftopRange() is null ? 0 : -1);
        }

        // The rooftop end whose adjustments the factory rules make, the target's over the source's: a rooftop that is
        // not level 1 of its hex (the rangehex of VASL's rules).
        private HexIndex? RooftopRange() =>
            targetIsRooftop && target.Level != 1 ? targetHex : sourceIsRooftop && source.Level != 1 ? sourceHex : null;

        // The factory test of checkBuildingRestrictionRule, for two ends in different hexes: isRooftopLOSBlocked for the
        // current hex, and along a hexside at an odd range, when the current hex blocks and an end is a rooftop, for the
        // hex across the hexside the line touches instead. The odd-range cases no fixture reaches are refused.
        private bool FactoryRooftopRule(int rooftopAdjustment)
        {
            if (!(losIs60Degree || losIsHorizontal) || rangeToSource % 2 == 0)
            {
                return IsRooftopLosBlocked(currentHex, currentTerrain!, currentTerrainHgt, rooftopAdjustment);
            }

            if (!IsRooftopLosBlocked(currentHex, currentTerrain!, currentTerrainHgt, rooftopAdjustment))
            {
                return false;
            }

            if (RooftopRange() is null)
            {
                return Refuse(LosUnsupportedRule.FactoryRooftop);
            }

            ClearBlocked();
            var first = (sourceExitHexspine + 1) % 6;
            var second = (sourceExitHexspine + 4) % 6;
            var touched = NearestSide(currentHex);
            if (touched != first && touched != second)
            {
                return Refuse(LosUnsupportedRule.FactoryRooftop);
            }

            if (Adjacent(currentHex, touched) is not { } testHex || CenterTerrain(testHex) is not { } testTerrain)
            {
                return Refuse(LosUnsupportedRule.VaslFails);
            }

            return IsRooftopLosBlocked(testHex, testTerrain, testTerrain.Height, rooftopAdjustment);
        }

        // Map.isRooftopLOSBlocked: LOS from a rooftop down, or up to one, within a factory. VASL adds the terrain's height
        // to the height it is given, and compares that with the target's elevation alone. Rubble, which clears the test,
        // and LOS up to a rooftop blocked beyond the first hex are refused: no fixture reaches them.
        private bool IsRooftopLosBlocked(HexIndex hex, TerrainType terrain, int terrainHeight, int rooftopAdjustment)
        {
            if (terrain.Name.Contains("Rubble", StringComparison.Ordinal))
            {
                return Refuse(LosUnsupportedRule.FactoryRooftop);
            }

            var hexIsRoofless = IsRooflessHex(hex);
            if (sourceIsRooftop && !targetIsRooftop && !hexIsRoofless && (hex != targetHex || range == 1))
            {
                return Block("LOS from/to Factory Rooftop within Factory only exists in same hex ground level location (B23.87)");
            }

            if (targetIsRooftop && !sourceIsRooftop
                && ((!hexIsRoofless && rangeToSource == 1 && !IsRooflessHex(sourceHex))
                    || (!hexIsRoofless && hex != targetHex && terrainHeight + terrain.Height >= targetElevation + rooftopAdjustment)))
            {
                return rangeToSource == 1
                    ? Block("LOS must leave the building before leaving the source hex to see a location with a different elevation (A6.8 Example 2)")
                    : Refuse(LosUnsupportedRule.FactoryRooftop);
            }

            return false;
        }

        // Whether a hex's center is roofless or gutted: no fixture has one, so the answer is refused when it is.
        private bool IsRooflessHex(HexIndex hex) => CenterTerrain(hex) is { IsRoofless: true } && Refuse(LosUnsupportedRule.Roofless);

        // LOSResult.resetreportingonly, with the status's blocked state
        private void ClearBlocked()
        {
            blocked = false;
            reason = string.Empty;
            resultBlocked = false;
            blockedAtPoint = null;
            resultReason = string.Empty;
        }

        // Map.checkHexsideTerrainRule, for rowhouse walls, walls, hedges, bocage, and cellars: no entrenchments or partial
        // orchards, which are refused. With an end on a hillock the hillock rule decides instead. A rowhouse wall takes
        // its own rule (B23.71) before anything else, cellars included. Bocage blind hexes, and the slope rules' leave to
        // see over hexside terrain, are refused: no fixture reaches them.
        private bool CheckHexsideTerrainRule()
        {
            if (startsOnHillock || endsOnHillock)
            {
                return false;
            }

            if (currentTerrain!.IsRowhouseFactoryWallOrBreach)
            {
                return CheckRowhouseFactoryWallAndBreach() && !Failed ? Block("Cannot see through rowhouse/factory wall (B23.71/O5.31)") : Failed;
            }

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
            if (ignore)
            {
                return false;
            }

            // bocage (B9.52): higher than both ends, as high as both and half a level high, or, without the slope rules,
            // as high as the higher end with the other lower; otherwise a blind hex
            if (currentTerrain.Name == Bocage)
            {
                var top = groundLevel + currentTerrainHgt;
                if ((top > sourceElevation && top > targetElevation)
                    || (top == sourceElevation && top == targetElevation && currentTerrain.IsHalfLevelHeight))
                {
                    return Block("Cannot see through/over bocage (B9.52)");
                }

                if (top == Math.Max(sourceElevation, targetElevation) && top > Math.Min(sourceElevation, targetElevation))
                {
                    return slopes ? Refuse(LosUnsupportedRule.SlopeCases) : Block("Cannot see through/over bocage (B9.52)");
                }

                // a blind hex behind bocage (B9.52) is refused: no fixture reaches it
                return IsBlindHex(currentTerrainHgt) && Refuse(LosUnsupportedRule.BocageBlindHex);
            }

            if (groundLevel == sourceElevation && groundLevel == targetElevation)
            {
                return slopes ? Refuse(LosUnsupportedRule.SlopeCases) : Block("Intervening hexside terrain (B9.2)");
            }

            return false;
        }

        // Map.checkRowhouseFactoryWallAndBreach, for a rowhouse wall (B23.71): whether it blocks at this point. Interior
        // factory walls and breaches, which no fixture has, are refused. A rooftop end that is not level 1 of its hex
        // counts a full level lower here.
        private bool CheckRowhouseFactoryWallAndBreach()
        {
            var terrain = currentTerrain!;
            if (terrain.Name.Contains("Interior Factory Wall", StringComparison.Ordinal) || terrain.Name.Contains("Breach", StringComparison.Ordinal))
            {
                return Refuse(LosUnsupportedRule.InteriorFactoryWall);
            }

            var sourceHeight = sourceElevation + (sourceIsRooftop && source.Level != 1 ? -1.0 : 0.0);
            var targetHeight = targetElevation + (targetIsRooftop && target.Level != 1 ? -1.0 : 0.0);
            var top = groundLevel + currentTerrainHgt;

            // have to handle rooftop LOS across the wall here
            var sourceHexsideTest = VaslAdjacent(sourceHex, sourceExitHexsides[0]) == currentHex;
            var targetHexsideTest = VaslAdjacent(targetHex, targetEnterHexsides[0]) == currentHex;
            if (source.Terrain!.IsRooftop && (rangeToSource == 0 || (rangeToSource == 1 && sourceHexsideTest)))
            {
                return false;
            }

            if (target.Terrain!.IsRooftop && (rangeToTarget == 0 || (rangeToTarget == 1 && targetHexsideTest)))
            {
                return false;
            }

            // as high as the lower end, with the other higher
            if (top < Math.Max(sourceHeight, targetHeight) && top == Math.Min(sourceHeight, targetHeight))
            {
                return false;
            }

            if (WallIsHigher(sourceHeight, targetHeight, top))
            {
                return true;
            }

            // otherwise a blind hex, counted from the hex across the hexside nearest the point when that is nearer the
            // source. VASL measures from the source for a rising line too.
            if (sourceElevation == targetElevation)
            {
                return false;
            }

            if (VaslAdjacent(currentHex, NearestSide(currentHex)) is not { } testHex)
            {
                return Refuse(LosUnsupportedRule.VaslFails);
            }

            var testRange = Range(sourceHex, testHex);
            bool blind;
            if (sourceElevation > targetElevation)
            {
                var oldRange = rangeToSource;
                rangeToSource = Math.Min(rangeToSource, testRange);
                blind = IsBlindHex(currentTerrainHgt);
                rangeToSource = oldRange;
            }
            else
            {
                var oldRange = rangeToTarget;
                rangeToTarget = Math.Min(rangeToTarget, testRange);
                blind = IsBlindHex(currentTerrainHgt);
                rangeToTarget = oldRange;
            }

            return blind;
        }

        // The wall test of checkRowhouseFactoryWallAndBreach: higher than both ends, as high as both and half a level
        // high (unless both ends are rooftops), or as high as the higher end with the other lower.
        private bool WallIsHigher(double sourceHeight, double targetHeight, int top) =>
            (top > sourceHeight && top > targetHeight)
            || (top == sourceHeight && top == targetHeight && !(source.Terrain!.IsRooftop && target.Terrain!.IsRooftop) && currentTerrain!.IsHalfLevelHeight)
            || (top == Math.Max(sourceHeight, targetHeight) && top > Math.Min(sourceHeight, targetHeight));

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

        // Map.checkBridgeHindranceRule: with both ends at the bridge's road level, a point on the bridge but off its road
        // is a hindrance. VASL's exception for a railroad embankment hexside, which no fixture has, is refused.
        private bool CheckBridgeHindranceRule()
        {
            if (Facts(currentHex).Bridge is not { } bridge || sourceElevation != targetElevation || sourceElevation != bridge.RoadLevel)
            {
                return false;
            }

            if (Facts(currentHex).Hexsides.Any(side => side.Terrain is { IsDepression: false } terrain && terrain.Name.Contains("Railroad, Embankment", StringComparison.Ordinal)))
            {
                return Refuse(LosUnsupportedRule.RailroadEmbankment);
            }

            return BridgeShapeContains(currentHex, BridgeWidth, 0) && !BridgeShapeContains(currentHex, BridgeWidth - (2 * BridgeRoadInset), BridgeRoadInset)
                && !currentTerrain!.IsRoad && AddHindranceHex();
        }

        // Shape.contains holds on the left and top edges and not on the right and bottom ones.
        private bool BridgeShapeContains(HexIndex hex, int width, int inset)
        {
            var center = map.LosPoint(hex);
            var left = center.X - (BridgeWidth / 2) + inset;
            var top = center.Y - (BridgeHeight / 2);
            return currentCol >= left && currentCol < left + width && currentRow >= top && currentRow < top + BridgeHeight;
        }

        // Map.checkGroundLevelRule, without railroad embankments or Deir. A hex whose ground level the LOS may ignore
        // because it follows a depression (losFollowsDepression), or where it crosses bridge terrain, does not block;
        // VASL keeps the bridge hex as the ignored hex until another replaces it.
        private bool CheckGroundLevelRule()
        {
            if (currentTerrain!.IsBridge)
            {
                ignoreGroundLevelHex = currentHex;
            }

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

        // Map.checkSplitTerrainRule. Only a cellar end is adjusted.
        private bool CheckSplitTerrainRule()
        {
            var terrain = currentTerrain!;
            if (terrain.HasSplit && groundLevel == sourceElevation + (sourceIsCellar ? 1.0 : 0.0) && groundLevel == targetElevation + (targetIsCellar ? 1.0 : 0.0))
            {
                // with the slope rules the upper part of the terrain counts
                if (slopes)
                {
                    return terrain.IsLosObstacle ? Block("This terrain blocks LOS to up-slope location") : AddHindranceHex();
                }

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

        // Map.checkHalfLevelTerrainRule, without railroad embankments or sand dunes. Where the hillock rules apply, or
        // from a hillock down, brush, grain, and in-season rice paddies give one hindrance at most, and nothing else
        // here counts. A rooftop end is adjusted only when the other end is not a rooftop, and a cellar end not at all.
        private bool CheckHalfLevelTerrainRule()
        {
            var terrain = currentTerrain!;
            if ((HillockRuleApplicable() && !slopes) || HillockHindranceToLowerElevation())
            {
                if (firstHalfLevelHindrance is null && terrain.Name is "Brush" or "Grain" or "Rice Paddy, In Season" && !(startsOnHillock && endsOnHillock))
                {
                    firstHalfLevelHindrance = currentHex;
                    return AddHindranceHex();
                }

                return false;
            }

            var sourceAdjustment = targetIsRooftop ? 0.0 : sourceRooftopAdjustment;
            var targetAdjustment = sourceIsRooftop ? 0.0 : targetRooftopAdjustment;
            return terrain.IsHalfLevelHeight && !terrain.IsHexsideTerrain
                && groundLevel + currentTerrainHgt == sourceElevation + sourceAdjustment && groundLevel + currentTerrainHgt == targetElevation + targetAdjustment
                && !slopes && ApplyHalfLevelTerrain();
        }

        // Map.hillockHindranceToLowerElevation: from a hillock end, terrain as high as it and above the other end.
        private bool HillockHindranceToLowerElevation()
        {
            var top = groundLevel + currentTerrainHgt;
            return (startsOnHillock && top == sourceElevation && top > targetElevation) || (endsOnHillock && top == targetElevation && top > sourceElevation);
        }

        // Map.hillockRuleApplicable: terrain as high as both ends, with a hillock at an end, next to one, or crossed.
        private bool HillockRuleApplicable()
        {
            var top = groundLevel + currentTerrainHgt;
            return top == sourceElevation && top == targetElevation
                && (crossingHillock is not null || startsOnHillock || endsOnHillock || sourceAdjacentHillock is not null || targetAdjacentHillock is not null);
        }

        // Map.applyHalfLevelTerrain
        private bool ApplyHalfLevelTerrain() => currentTerrain!.IsLosObstacle
            ? Block("Half level terrain is higher than both the source and/or the target (A6.2)")
            : AddHindranceHex();

        // Map.checkTerrainIsHigherRule, without railroad embankments, sand dunes, or Volga piers. A hillock end counts
        // half a level higher (a cellar's adjustment wins over it, and it over a rooftop's). With the slope rules VASL
        // adds no hindrance here, which no fixture reaches, so it is refused.
        private bool CheckTerrainIsHigherRule()
        {
            var terrain = currentTerrain!;
            var obstacleAdjustment = terrain.IsHalfLevelHeight && !terrain.IsHexsideTerrain ? 0.5 : 0.0;
            var sourceHeight = sourceElevation + (sourceIsCellar ? 1.0 : startsOnHillock ? 0.5 : sourceRooftopAdjustment);
            var targetHeight = targetElevation + (targetIsCellar ? 1.0 : endsOnHillock ? 0.5 : targetRooftopAdjustment);

            // split terrain at the level of both ends has its own rule
            if (terrain.HasSplit && groundLevel == sourceHeight && groundLevel == targetHeight)
            {
                return false;
            }

            // bocage, partial orchards, and hillocks have their own rules
            if (terrain.Name is Bocage or PartialOrchard or Hillock)
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

                // must be a hindrance: factory terrain hinders nothing inside the factory and blocks once the LOS has
                // left the building; LOS under a bridge or road above both ends is clear
                if (terrain.IsFactory)
                {
                    return losLeavesBuilding && Block("Terrain is higher than both the source and target (A6.2)");
                }

                if ((terrain.IsBridge || terrain.Name.Contains("Road", StringComparison.Ordinal)) && groundLevel > sourceElevation && groundLevel > targetElevation)
                {
                    return false;
                }

                return slopes ? Refuse(LosUnsupportedRule.SlopeCases) : AddHindranceHex();
            }

            return false;
        }

        // Map.checkTerrainHeightRule, without railroad embankments. The cellar and rooftop adjustments apply to the height
        // test only; the exceptions below use the plain elevations, as VASL's do. An out-of-season orchard counts a level
        // lower in the height test, and hinders where its full height is as high as the higher end only.
        private bool CheckTerrainHeightRule()
        {
            var terrain = currentTerrain!;
            var isOrchardOutOfSeason = terrain.Name == OrchardOutOfSeason;
            var obstacleAdjustment = isOrchardOutOfSeason ? -1.0 : terrain.IsHalfLevelHeight && terrain.IsBuilding ? 0.5 : 0.0;
            var height = groundLevel + currentTerrainHgt + obstacleAdjustment;
            var sourceHeight = sourceElevation + HeightAdjustment(sourceIsCellar, sourceRooftopAdjustment);
            var targetHeight = targetElevation + HeightAdjustment(targetIsCellar, targetRooftopAdjustment);
            if (height != Math.Max(sourceHeight, targetHeight) || !(height > Math.Min(sourceHeight, targetHeight)))
            {
                var top = groundLevel + currentTerrainHgt;
                return isOrchardOutOfSeason && top == Math.Max(sourceHeight, targetHeight) && top > Math.Min(sourceHeight, targetHeight) && AddHindranceHex();
            }

            // an out-of-season orchard hex whose ground is as high as the higher end: no fixture reaches it
            if (isOrchardOutOfSeason)
            {
                return Refuse(LosUnsupportedRule.OrchardOutOfSeasonCases);
            }

            var cell = (X: (double)currentCol, Y: (double)currentRow);
            var currentCenter = map.LosPoint(currentHex);

            // B10.2 EXC: ignore same level terrain in a lower adjacent hex, and the reverse
            if (rangeToSource == 1 && currentTerrainHgt + obstacleAdjustment == 0 && sourceElevation > Facts(currentHex).BaseLevel
                && LosMap.Distance(sourceCenter.X, sourceCenter.Y, cell.X, cell.Y) < LosMap.Distance(sourceCenter.X, sourceCenter.Y, currentCenter.X, currentCenter.Y))
            {
                return false;
            }

            if (rangeToTarget == 1 && currentTerrainHgt + obstacleAdjustment == 0 && targetElevation > Facts(currentHex).BaseLevel
                && LosMap.Distance(targetCenter.X, targetCenter.Y, cell.X, cell.Y) < LosMap.Distance(targetCenter.X, targetCenter.Y, currentCenter.X, currentCenter.Y))
            {
                return false;
            }

            // in-hex LOS hindrances in the source or target hex neither block nor hinder
            if ((rangeToSource == 0 || rangeToTarget == 0) && (terrain.IsLowerLosHindrance || terrain.IsLosHindrance))
            {
                return false;
            }

            // slopes and hillocks are left to the blind hex rule, and bocage to the hexside rule
            if (slopes || startsOnHillock || endsOnHillock || terrain.Name == Bocage)
            {
                return false;
            }

            // rowhouse and interior factory walls are left to the blind hex rule
            if (terrain.IsRowhouseFactoryWallOrBreach)
            {
                return false;
            }

            // within a factory an end is in, VASL's factory rooftop test decides; no fixture reaches it here
            if (!losLeavesBuilding && (source.Terrain!.IsFactory || target.Terrain!.IsFactory) && !terrain.IsOutsideFactoryWall)
            {
                return Refuse(LosUnsupportedRule.FactoryRooftop);
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

        // Map.checkBlindHexRule. A cliff hexside always takes the blind hex test (B10.23); bocage takes it in the hexside
        // rule.
        private bool CheckBlindHexRule()
        {
            var terrain = currentTerrain!;
            var height = groundLevel + currentTerrainHgt;
            var sourceHeight = sourceElevation + HeightAdjustment(sourceIsCellar, sourceRooftopAdjustment);
            var targetHeight = targetElevation + HeightAdjustment(targetIsCellar, targetRooftopAdjustment);
            var lowerEnd = Math.Min(sourceHeight, targetHeight);
            var higherEnd = Math.Max(sourceHeight, targetHeight);

            // terrain as high as the higher end: from an up-slope or hillock location only a blind hex blocks (F2.3)
            if (height == higherEnd && height > lowerEnd && (startsOnHillock || slopes) && IsBlindHex(currentTerrainHgt))
            {
                return Block("Source or Target location is in a blind hex from an up-slope location (F2.3)");
            }

            if (terrain.Name == Bocage)
            {
                return false;
            }

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

            const string blindHex = "Source or Target location is in a blind hex (A6.4)";
            if ((terrain.IsLosObstacle && !terrain.IsHexsideTerrain) || terrain.IsOutsideFactoryWall)
            {
                // VASL lets a roofless hex's obstacle, other than an outside factory wall, through; that is refused
                if (IsRooflessHex(currentHex))
                {
                    return true;
                }

                // an inherent obstacle that is not the hex's own terrain does not block here
                if (terrain.IsInherent && CenterTerrain(currentHex)?.Code != terrain.Code)
                {
                    return false;
                }

                // a hexside target counts as at an even range here
                if (!((losIs60Degree || losIsHorizontal) && terrain.IsBuilding) || rangeToTarget % 2 == 0 || !targetIsCenter || terrain.IsOutsideFactoryWall)
                {
                    return Block(blindHex);
                }

                // Along a hexside at an odd range the hex the line touches decides; VASL resets the result first.
                ClearBlocked();
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

                return IsRooflessHex(testHex) || Block(blindHex);
            }

            // LOS through a factory hex from or to another level outside the factory
            if (terrain.IsFactory && (losLeavesBuilding || targetElevation > currentTerrainHgt + terrain.Height)
                && !IsRooflessHex(currentHex) && !terrain.Name.Contains("Interior Factory Wall", StringComparison.Ordinal))
            {
                // along a hexside, which no fixture reaches, VASL tests the hexes either side
                return losIs60Degree || losIsHorizontal
                    ? Refuse(LosUnsupportedRule.FactoryRooftop)
                    : !(rangeToTarget == 1 && IsRooflessHex(targetHex)) && Block(blindHex);
            }

            // see if a cliff hexside, or ground level alone, creates a blind hex
            if ((isCliffHexside && !cliffAlongHexsideInThisHex)
                || (groundLevel > lowerEnd && groundLevel < higherEnd && IsBlindHex(0, false, null)))
            {
                return Block("Source or Target location is in a blind hex (B10.23)");
            }

            // a hindrance creates a "blind hex", if not the target or source hex; a cliff, a factory, and open ground do
            // not, and an out-of-season orchard only next to the target
            if (currentHex == targetHex || currentHex == sourceHex)
            {
                return false;
            }

            // VASL hinders only next to the target here; no fixture reaches it
            if (terrain.Name == OrchardOutOfSeason)
            {
                return Refuse(LosUnsupportedRule.OrchardOutOfSeasonCases);
            }

            return !isCliffHexside && !terrain.IsFactory && terrain.Category != LosCategory.Open && AddHindranceHex();
        }

        // LOSStatus.setHillockStatus, when the LOS enters a new hex: the hillock it crosses, the hillocks it has crossed
        // (the starting one included), and the first rubble hex (no counters, so only the hex's own rubble).
        private void SetHillockStatus()
        {
            var hillock = map.HillockOf(currentHex);
            if (crossingHillock is { } crossing && hillock is null)
            {
                crossedHillocks.Add(crossing);
                crossingHillock = null;
            }
            else if (crossingHillock is null && hillock is not null)
            {
                crossingHillock = hillock;
            }

            if (currentTerrain!.Name is StoneRubble or WoodenRubble && CenterTerrain(currentHex)?.Name == currentTerrain.Name)
            {
                firstRubbleCrossed ??= currentHex;
            }
        }

        // Map.checkHillockRule (F6.4), where the terrain is as high as both ends and a hillock is at an end, next to one,
        // or crossed.
        private bool CheckHillockRule()
        {
            if (!HillockRuleApplicable())
            {
                return false;
            }

            var terrain = currentTerrain!;

            // Both ends on a hillock: clear unless the LOS crosses a summit not next to either end. The outcomes no
            // fixture reaches (summits, hillocks between a hillock source and its target, a second wall or rubble hex)
            // are refused where VASL would block.
            if (startsOnHillock && endsOnHillock)
            {
                return terrain.Name == HillockSummit && Range(sourceHex, currentHex) != 1 && Range(targetHex, currentHex) != 1
                    && Refuse(LosUnsupportedRule.HillockCases);
            }

            if (startsOnHillock || endsOnHillock)
            {
                if (terrain.Name == HillockSummit)
                {
                    return Refuse(LosUnsupportedRule.HillockCases);
                }

                // intervening hillocks: from a hillock, the one next to the target is ignored and a slope counts as one
                if (startsOnHillock && crossedHillocks.Count - (targetAdjacentHillock is null ? 0 : 1) + (ExitsSlopeHexside() ? 1 : 0) > 2)
                {
                    return Refuse(LosUnsupportedRule.HillockCases);
                }

                if (endsOnHillock && crossedHillocks.Count - (sourceAdjacentHillock is null ? 0 : 1) + (EntersSlopeHexside() ? 1 : 0) > 1)
                {
                    return BlockByHillock();
                }

                if (terrain.Name is "Wall" or "Hedge")
                {
                    return CheckHillockWallRule();
                }

                // F6.412: past a second rubble hex only the hex next to the target can be seen
                return terrain.Name is StoneRubble or WoodenRubble && currentHex != firstRubbleCrossed && Range(currentHex, targetHex) != 1
                    && Refuse(LosUnsupportedRule.HillockCases);
            }

            // a hillock next to an end is ignored (no entrenchments: they are refused)
            if ((sourceAdjacentHillock is { } nearSource && map.HillockOf(currentHex) == nearSource)
                || (targetAdjacentHillock is { } nearTarget && map.HillockOf(currentHex) == nearTarget))
            {
                return false;
            }

            if (terrain.Name == HillockSummit)
            {
                return Refuse(LosUnsupportedRule.HillockCases);
            }

            return terrain.IsHalfLevelHeight && !terrain.IsHexsideTerrain && ApplyHalfLevelTerrain();
        }

        // The wall and hedge case of checkHillockRule: the first wall or hedge the LOS touches is remembered; another,
        // which is not the same hexside and which the hexside rule would block more than 15 pixels from the first, blocks
        // in VASL (refused here). The hexside rule's result is thrown away, as VASL gives it a new LOSResult.
        private bool CheckHillockWallRule()
        {
            var nearest = NearestSide(currentHex);
            if (firstWallCrossed is not { } first)
            {
                firstWallCrossed = (currentHex, nearest);
                firstWallPoint = new GridPoint(currentCol, currentRow);
                return false;
            }

            // the wall location and the location across it; VASL reads the hex itself for a center location
            if (VaslAdjacent(currentHex, nearest) is not { } oppositeHex)
            {
                return Refuse(LosUnsupportedRule.VaslFails);
            }

            var opposite = (oppositeHex, LocationSide(Opposite(nearest)));
            if (nearest != None && (first == (currentHex, nearest) || first == opposite))
            {
                return false;
            }

            // pretend neither end is on a hillock, and test the wall with the hexside rule and a throwaway result
            var (wasBlocked, wasAt, wasReason) = (resultBlocked, blockedAtPoint, resultReason);
            var (starts, ends) = (startsOnHillock, endsOnHillock);
            startsOnHillock = false;
            endsOnHillock = false;
            var hexsideBlocks = CheckHexsideTerrainRule();
            (resultBlocked, blockedAtPoint, resultReason) = (wasBlocked, wasAt, wasReason);
            if (Failed)
            {
                return true;
            }

            // a second wall or hedge more than 15 pixels from the first blocks in VASL; no fixture reaches it
            if (hexsideBlocks && LosMap.Distance(firstWallPoint.X, firstWallPoint.Y, currentCol, currentRow) > 15)
            {
                return Refuse(LosUnsupportedRule.HillockCases);
            }

            startsOnHillock = starts;
            endsOnHillock = ends;
            blocked = false;
            reason = string.Empty;
            return false;
        }

        // Map.blockByHillock
        private bool BlockByHillock() => Block("Intervening hillock (F6.4)");

        // Map.isBlindHex. Cellars are not adjusted here. A cliff
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

            // from an up-slope or hillock location, terrain as high as the higher end counts a level lower
            if ((slopes || startsOnHillock) && groundLevel + currentTerrainHgt == Math.Max(sourceElevation, targetElevation))
            {
                higher++;
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

            // EXC: the hex with a rowhouse or interior factory wall hexside is the first blind hex
            if (currentTerrain!.IsRowhouseFactoryWallOrBreach)
            {
                rangeToLower++;
                rangeFromHigher--;
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

            // Map.getBlind; a cliff makes one blind hex fewer. Bocage two or more levels below the higher end, which VASL
            // lets leave no blind hex, is refused: no fixture reaches it.
            if (currentTerrain.Name.Contains(Bocage, StringComparison.Ordinal) && higher - (ground + terrainHeight) >= 2)
            {
                return Refuse(LosUnsupportedRule.BocageBlindHex);
            }

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
                // Along a hexside only the hexside the line touches counts; a roofless hex across it is refused.
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
                    if (IsRooflessHex(testHex) || name.Contains("Light Woods", StringComparison.Ordinal))
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

            // LOSResult.addMapHindrance: the first point met (setFirstHindrance), and by range from the source hex, the
            // larger value kept
            firstHindranceAt ??= new GridPoint(currentCol, currentRow);
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

        // setEnterExitHexsides for a hexside end: the hexsides of its hex the line touches, in ascending order (the order
        // VASL's HashSet of small integers gives), the first two kept. With more than two, removeVertexHexsides drops
        // those whose LOS or auxiliary point is either end of the line.
        private int[] HexsidesTouched(HexIndex hex)
        {
            var touched = map.HexsidesCrossed(hex, sourcePoint, targetPoint).Select(side => (int)side).ToList();
            if (touched.Count > 2)
            {
                var vertices = map.Geometry.Vertices(hex);
                touched.RemoveAll(side => IsLineEnd(vertices[side]) || IsLineEnd(vertices[(side + 1) % 6]));
            }

            return [touched.Count > 0 ? touched[0] : None, touched.Count > 1 ? touched[1] : None];
        }

        private bool IsLineEnd(PixelPoint vertex)
        {
            var point = new GridPoint((int)vertex.X, (int)vertex.Y);
            return point == sourcePoint || point == targetPoint;
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
