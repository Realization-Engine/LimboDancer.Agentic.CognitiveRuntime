using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>An edit to a Feature Model (Model Design section 12). Applying one yields a new model and its inverse.</summary>
public abstract record AuthoringCommand
{
    public abstract string Description
    {
        get;
    }
}

public sealed record AddFeature(Feature Feature) : AuthoringCommand
{
    public override string Description => $"add {FeatureDescriptions.Kind(Feature)}";
}

/// <summary>Replaces the feature with the same id, for geometry, code, layer, or property changes.</summary>
public sealed record ReplaceFeature(Feature Feature) : AuthoringCommand
{
    public override string Description => $"change {FeatureDescriptions.Kind(Feature)}";
}

public sealed record RemoveFeature(string FeatureId) : AuthoringCommand
{
    public override string Description => "delete feature";
}

public sealed record SetStairway(HexIndex Hex, bool On) : AuthoringCommand
{
    public override string Description => On ? "add stairway" : "remove stairway";
}

public enum HexsideMark
{
    Slope,
    RailroadEmbankment,
    PartialOrchard,
}

/// <summary>Sets a metadata hexside mark on one hex's side, as <c>BoardMetadata.xml</c> records it.</summary>
public sealed record SetHexsideMark(HexsideMark Mark, HexsideRef Side, bool On) : AuthoringCommand
{
    public override string Description => $"{(On ? "set" : "clear")} {Mark}";
}

/// <summary>Several commands applied in order as one undoable step.</summary>
public sealed record CompositeCommand(IReadOnlyList<AuthoringCommand> Commands, string Label) : AuthoringCommand
{
    public override string Description => Label;
}

/// <summary>The new model and the command that undoes the edit, or the reason the command was refused.</summary>
public sealed record CommandResult(FeatureModel? Model, AuthoringCommand? Inverse, string? Error)
{
    public bool Succeeded => Model is not null;
}

public static class FeatureDescriptions
{
    public static string Kind(Feature feature) => feature switch
    {
        ElevationRegion => "elevation region",
        AreaTerrainFeature => "area terrain",
        LinearTerrainFeature => "linear terrain",
        BridgeFeature => "bridge",
        BuildingFeature => "building",
        HexsideTerrainFeature => "hexside terrain",
        FidelityPin => "fidelity pin",
        _ => "feature",
    };
}

/// <summary>Applies authoring commands. Commands are type-checked before they are applied (section 12).</summary>
public static class AuthoringCommands
{
    public static CommandResult Apply(FeatureModel model, AuthoringCommand command, TerrainCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(catalog);
        switch (command)
        {
            case AddFeature add:
                if (model.Features.Any(feature => feature.Id == add.Feature.Id))
                {
                    return Refuse($"A feature with id {add.Feature.Id} already exists.");
                }

                return Check(model, add.Feature, catalog) is { } addError
                    ? Refuse(addError)
                    : Done(model with
                    {
                        Features = [.. model.Features, add.Feature]
                    }, new RemoveFeature(add.Feature.Id));
            case ReplaceFeature replace:
                var index = IndexOf(model, replace.Feature.Id);
                if (index < 0)
                {
                    return Refuse($"No feature has id {replace.Feature.Id}.");
                }

                if (Check(model, replace.Feature, catalog) is { } replaceError)
                {
                    return Refuse(replaceError);
                }

                var replaced = model.Features.ToArray();
                var previous = replaced[index];
                replaced[index] = replace.Feature;
                return Done(model with
                {
                    Features = replaced
                }, new ReplaceFeature(previous));
            case RemoveFeature remove:
                var removeIndex = IndexOf(model, remove.FeatureId);
                if (removeIndex < 0)
                {
                    return Refuse($"No feature has id {remove.FeatureId}.");
                }

                var removed = model.Features[removeIndex];
                return Done(model with
                {
                    Features = model.Features.Where((_, position) => position != removeIndex).ToArray()
                }, new AddFeature(removed));
            case SetStairway stairway:
                if (!model.Geometry.Contains(stairway.Hex))
                {
                    return Refuse("The hex is not on the board.");
                }

                var stairways = model.Annotations.Stairways.ToHashSet();
                var had = stairways.Contains(stairway.Hex);
                _ = stairway.On ? stairways.Add(stairway.Hex) : stairways.Remove(stairway.Hex);
                return Done(model with
                {
                    Annotations = model.Annotations with
                    {
                        Stairways = stairways
                    }
                }, new SetStairway(stairway.Hex, had));
            case SetHexsideMark mark:
                return SetMark(model, mark);
            case CompositeCommand composite:
                var current = model;
                var inverses = new List<AuthoringCommand>();
                foreach (var part in composite.Commands)
                {
                    var result = Apply(current, part, catalog);
                    if (!result.Succeeded)
                    {
                        return result;
                    }

                    current = result.Model!;
                    inverses.Insert(0, result.Inverse!);
                }

                return Done(current, new CompositeCommand(inverses, "undo " + composite.Label));
            default:
                return Refuse($"Unknown command {command.GetType().Name}.");
        }
    }

    /// <summary>Type validation for a feature against the model's geometry and catalog, or null when it is valid.</summary>
    public static string? Check(FeatureModel model, Feature feature, TerrainCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(feature);
        ArgumentNullException.ThrowIfNull(catalog);
        if (string.IsNullOrWhiteSpace(feature.Id))
        {
            return "A feature needs an id.";
        }

        if (TerrainKinds.CodeOf(feature) is { } code)
        {
            if (!catalog.TryGet(code, out var type))
            {
                return $"Terrain code {code} is not in the catalog.";
            }

            if (TerrainKinds.RequiredKind(feature) is { } required && !Allowed(required, type, model.BaseCode))
            {
                return $"{type.Name} is not {FeatureDescriptions.Kind(feature)}.";
            }
        }

        return feature switch
        {
            ElevationRegion region when region.Level is < -9 or > 9 => "Elevation levels run from -9 to 9.",
            ElevationRegion region => CheckShape(region.Shape),
            AreaTerrainFeature area => CheckShape(area.Shape),
            BridgeFeature bridge => CheckShape(bridge.Shape),
            FidelityPin pin when pin.Elevation is < -9 or > 9 => "Pin elevations run from -9 to 9.",
            FidelityPin pin => CheckShape(pin.Shape),
            LinearTerrainFeature { Centerline: null, Outline: null } => "Linear terrain needs a centerline or an outline.",
            LinearTerrainFeature { Centerline: { } path, Width.Raw: <= 0 } when path.Segments.Count > 0 => "A centerline needs a positive width.",
            LinearTerrainFeature { Centerline: { Segments.Count: 0 } } => "A centerline needs at least one segment.",
            LinearTerrainFeature { Outline: { } outline } => CheckShape(outline),
            LinearTerrainFeature => null,
            BuildingFeature { Footprints.Count: 0 } => "A building needs at least one footprint.",
            BuildingFeature building when string.IsNullOrWhiteSpace(building.BuildingId) => "A building needs a building id.",
            BuildingFeature building => building.Footprints.Select(CheckShape).FirstOrDefault(error => error is not null),
            HexsideTerrainFeature { Spans.Count: 0 } => "Hexside terrain needs at least one hexside.",
            HexsideTerrainFeature hexside => hexside.Spans.Select(span => CheckSpan(model.Geometry, span)).FirstOrDefault(error => error is not null),
            _ => $"Unknown feature kind {feature.GetType().Name}.",
        };
    }

    private static bool Allowed(TerrainKind required, TerrainType type, byte baseCode) =>
        TerrainKinds.KindOf(type) == required || (required == TerrainKind.Area && type.Code == baseCode);

    private static string? CheckShape(FeatureShape shape) =>
        shape.Rings.Count == 0 ? "A shape needs at least one ring." : shape.Rings.Any(ring => ring.Count < 3) ? "Every ring needs at least three points." : null;

    private static string? CheckSpan(BoardGeometry geometry, HexsideSpan span) => span switch
    {
        _ when !geometry.Contains(span.Side.Hex) => "A hexside's hex is not on the board.",
        _ when !HexsideDirections.IsDefined((int)span.Side.Side) => "A hexside direction runs from 0 to 5.",
        { From: < 0 } or { To: > 64 } => "A hexside extent runs from 0 to 64.",
        _ when span.To <= span.From => "A hexside extent must end after it starts.",
        { Width: < 1 or > 30 } => "A hexside width runs from 1 to 30 pixels.",
        _ => null,
    };

    private static CommandResult SetMark(FeatureModel model, SetHexsideMark command)
    {
        var geometry = model.Geometry;
        if (!geometry.Contains(command.Side.Hex))
        {
            return Refuse("The hex is not on the board.");
        }

        var hexsides = model.Annotations.Hexsides;
        var source = command.Mark switch
        {
            HexsideMark.Slope => hexsides.Slopes,
            HexsideMark.RailroadEmbankment => hexsides.RailroadEmbankments,
            _ => hexsides.PartialOrchards,
        };
        var name = geometry.NameOf(command.Side.Hex);
        var flags = source.ToDictionary(pair => pair.Key, pair => pair.Value.ToHashSet());
        var sides = flags.TryGetValue(name, out var existing) ? existing : [];
        var had = sides.Contains(command.Side.Side);
        _ = command.On ? sides.Add(command.Side.Side) : sides.Remove(command.Side.Side);
        flags[name] = sides;
        var updated = flags.Where(pair => pair.Value.Count > 0).ToDictionary(pair => pair.Key, pair => (IReadOnlySet<HexsideDirection>)pair.Value);
        var annotations = command.Mark switch
        {
            HexsideMark.Slope => hexsides with { Slopes = updated },
            HexsideMark.RailroadEmbankment => hexsides with { RailroadEmbankments = updated },
            _ => hexsides with { PartialOrchards = updated },
        };
        return Done(model with
        {
            Annotations = model.Annotations with
            {
                Hexsides = annotations
            }
        }, command with
        {
            On = had
        });
    }

    private static int IndexOf(FeatureModel model, string id)
    {
        for (var index = 0; index < model.Features.Count; index++)
        {
            if (model.Features[index].Id == id)
            {
                return index;
            }
        }

        return -1;
    }

    private static CommandResult Done(FeatureModel model, AuthoringCommand inverse) => new(model, inverse, null);

    private static CommandResult Refuse(string error) => new(null, null, error);
}
